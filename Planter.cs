using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using SpaceEngineers.Game.EntityComponents.GameLogic;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace FarmerAutomation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_FunctionalBlock), useEntityUpdate: true)]
    public class Planter : MyGameLogicComponent
    {
        IMyFunctionalBlock _planterBlock;
        IMyFarmPlotLogic _planterComponent;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            var block = Entity as IMyFunctionalBlock;

            if (block == null)
                return;

            foreach (var component in block.Components)
            {
                _planterComponent = component as IMyFarmPlotLogic;
                if (_planterComponent != null)
                    break;
            }

            if (_planterComponent == null)
                return;

            if (MyAPIGateway.Session.IsServer)
            {
                NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
            }

            _planterBlock = block;
            MyLog.Default.Log(MyLogSeverity.Debug, $"FarmerAutomation: Found planter block {_planterComponent.IsPlantPlanted}");
        }

        public bool CanPlant()
        {
            return !_planterComponent.IsAlive || !_planterComponent.IsPlantPlanted;
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if (!MyAPIGateway.Session.IsServer)
                return;

            if (!CanPlant())
                return;

            double halfScale = _planterBlock.CubeGrid.GridSizeEnum == MyCubeSize.Large ? 1.25 : 0.25;

            var localBox = new BoundingBoxD(
                new Vector3D(-halfScale, -halfScale, -halfScale),
                new Vector3D(halfScale, halfScale, halfScale)
            );

            var matrix = _planterBlock.WorldMatrix;
            var obb = new MyOrientedBoundingBoxD(localBox, matrix);
            BoundingBoxD broadAabb = obb.GetAABB();

            var candidates = MyAPIGateway.Entities.GetEntitiesInAABB(ref broadAabb);
            var match = candidates.FirstOrDefault(e =>
            {
                var floating = e as MyFloatingObject;
                if (floating == null || floating.IsPreview || e.PositionComp == null || !(floating.Item.Content is MyObjectBuilder_SeedItem))
                    return false;

                Vector3D pos = e.PositionComp.GetPosition();
                if (!obb.Contains(ref pos))
                    return false;

                return floating.Item.Amount >= _planterComponent.AmountOfSeedsRequired;
            }) as MyFloatingObject;

            if (match != null)
            {
                // HACK: Only the Player can plant, so i need to tell the Client to give the item to the player inv, and then call "PlantSeed()"
                foreach (var player in FarmerAutomationMod.Instance.players)
                {
                    if (player == null || !player.Character.HasInventory || player.IsBot)
                        continue;
                    if (player.Character.Parent != null) // skip players in cockpits, PlantSeed() fails on them
                        continue;
                    if (Vector3D.DistanceSquared(player.GetPosition(), _planterBlock.GetPosition()) > FarmerAutomationMod.Instance.maxDistanceSquared)
                        continue;

                    var inventory = player.Character.GetInventory();
                    if (!inventory.CanAddItemAmount(match.Item, _planterComponent.AmountOfSeedsRequired))
                        continue;

                    inventory.AddItems(_planterComponent.AmountOfSeedsRequired, match.Item.Content);
                    match.Item.Amount -= _planterComponent.AmountOfSeedsRequired;
                    match.UpdateInternalState();

                    var invItem = inventory.FindItem(match.Item.GetDefinitionId());
                    if (invItem != null)
                    {
                        FarmerAutomationMod.network.TransmitToPlayer(new PacketPlayerPlantSeed()
                        {
                            BlockId = _planterBlock.EntityId,
                            ItemDefinitionId = invItem.GetDefinitionId(),
                        }, player.SteamUserId, true);
                        break;
                    }
                }
            }
        }

        public bool TryPlantInventorySeedInFarmPlot(MyDefinitionId itemDefinitionId)
        {
            var playerInventory = MyAPIGateway.Session.Player.Character.GetInventory(0);
            var invItem = playerInventory.FindItem(itemDefinitionId);
            if (invItem == null) 
                return false;

            // Not 100% guaranteed that the server will plant from this request but
            // there's no way to know until the server reply with the new status of the Planter
            _planterComponent.PlantSeed(invItem); 
            
            /* MyFarmPlotLogic.PlantSeed_Server */

            return true;
        }
    }
}