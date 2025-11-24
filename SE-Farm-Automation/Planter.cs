using System;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using IMyInventoryItem = VRage.Game.ModAPI.IMyInventoryItem;

namespace FarmerAutomation
{
    /// <summary>
    /// This version is *DEAD* as update 1.208 (Core System Update, 24/11/2025)
    /// </summary>
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_FunctionalBlock), useEntityUpdate: true)]
    public class Planter : MyGameLogicComponent
    {
        Color _debugColor = new Color(0, 255, 0, 64);

        IMyFunctionalBlock _planterBlock;
        IMyFarmPlotLogic _planterComponent;

        Vector3D _offset = Vector3D.Zero;
        BoundingBoxD _localBox;
        double _halfScale;

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

            FarmerAutomationMod.DrawDebugChanged += ApplyNeedsUpdate;
            
            if (MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.IsServer)
                NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME; // Server doesn't need debug drawing, so always will be 100
            else
                ApplyNeedsUpdate();


            _planterBlock = block;

            _halfScale = _planterBlock.CubeGrid.GridSize / 2;
            _localBox = new BoundingBoxD(
                new Vector3D(-_halfScale, -_halfScale, -_halfScale),
                new Vector3D(_halfScale, _halfScale, _halfScale)
            );

            ApplyOffsetPerBlock(ref _offset, _planterBlock);

            MyLog.Default.Log(MyLogSeverity.Debug,
                $"{nameof(FarmerAutomation)}: Found planter block {_planterComponent.IsPlantPlanted}");
        }

        void ApplyNeedsUpdate() => NeedsUpdate = !FarmerAutomationMod.DrawDebug
            ? MyEntityUpdateEnum.EACH_100TH_FRAME
            : MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
        
        public override void Close()
        {
            base.Close();
            FarmerAutomationMod.DrawDebugChanged -= ApplyNeedsUpdate;
            NeedsUpdate = MyEntityUpdateEnum.NONE;
        }

        public bool CanPlant()
        {
            return !_planterComponent.IsAlive || !_planterComponent.IsPlantPlanted;
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            var matrix = GetMatrix();
            MySimpleObjectDraw.DrawTransparentBox(ref matrix, ref _localBox, ref _debugColor,
                MySimpleObjectRasterizer.Solid, 1);
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if (!MyAPIGateway.Session.IsServer)
                return;

            if (!CanPlant())
                return;

            var matrix = GetMatrix();

            var obb = new MyOrientedBoundingBoxD(_localBox, matrix);
            BoundingBoxD broadAabb = obb.GetAABB();

            var candidates = MyAPIGateway.Entities.GetEntitiesInAABB(ref broadAabb);
            var match = candidates.FirstOrDefault(e =>
            {
                var floating = e as MyFloatingObject;
                if (floating == null || floating.IsPreview || e.PositionComp == null ||
                    !(floating.Item.Content is MyObjectBuilder_SeedItem))
                    return false;

                Vector3D pos = e.PositionComp.GetPosition();
                if (!obb.Contains(ref pos))
                    return false;

                return floating.Item.Amount >= _planterComponent.AmountOfSeedsRequired;
            }) as MyFloatingObject;

            if (match != null)
            {
                // HACK: Only the Player can plant, so I need to give the item to the player inv, and then tell the Client to call "PlantSeed()"
                foreach (var player in FarmerAutomationMod.Instance.players)
                {
                    if (player == null || !player.Character.HasInventory || player.IsBot)
                        continue;

                    // skip players not controlling they character (cockpit, rc, turrets, replay tool, etc),
                    // PlantSeed() fails on them
                    if (player.Controller.ControlledEntity != player.Character)
                        continue;

                    if (Vector3D.DistanceSquared(player.GetPosition(), _planterBlock.GetPosition()) >
                        FarmerAutomationMod.Instance.maxDistanceSquared)
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

        public MatrixD GetMatrix()
        {
            var matrix = _planterBlock.WorldMatrix;

            if (_offset == Vector3D.Zero)
                return matrix;

            var realOffset = _offset;
            realOffset = Vector3D.Rotate(realOffset, matrix.GetOrientation());
            matrix = Matrix.Multiply(matrix, Matrix.CreateTranslation(realOffset));

            return matrix;
        }

        public bool TryPlantInventorySeedInFarmPlot(MyDefinitionId itemDefinitionId)
        {
            var playerInventory = MyAPIGateway.Session.Player.Character.GetInventory(0);
            var invItem = playerInventory.FindItem(itemDefinitionId);
            if (invItem == null)
                return false;

            // Not 100% guaranteed that the server will plant from this request but
            // there's no way to know until the server reply with the new status of the Planter
            
            // _planterComponent.PlantSeed(invItem); // We Dead...
            MyAPIGateway.Utilities.ShowMessage(
                "Planting Automation",
                "ERROR: We found a seed, but cannot locate the API method 'PlantSeed(IMyInventoryItem seed)'!\n" +
                "Switch to \"Planting Automation 2\" which utilizes the new API!"
            );

            return true;
        }

        /// <summary>
        /// Someday in the future, I may find a way to do it automatically, or load from a config file, who knows, until then, I will just add offsets here for mods that may need it 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="block"></param>
        public static void ApplyOffsetPerBlock(ref Vector3D offset, IMyFunctionalBlock block)
        {
            const string CUBE_FARM_PLOT = "farm_Block"; // https://steamcommunity.com/sharedfiles/filedetails/?id=3591076340
            
            if (block.BlockDefinition.SubtypeId == CUBE_FARM_PLOT)
                offset = Vector3D.Up * (block.CubeGrid.GridSize * .95);
        }
    }
}