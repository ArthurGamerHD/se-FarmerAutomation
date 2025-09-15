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

            MyLog.Default.Log(MyLogSeverity.Info,
                $"Found block {block?.CustomName} : {block?.Components.Contains(typeof(IMyFarmPlotLogic))}");

            foreach (var component in block.Components)
            {
                _planterComponent = component as IMyFarmPlotLogic;
                if (_planterComponent != null)
                    break;
            }

            if (_planterComponent == null)
                return;

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;


            _planterBlock = block;
            MyLog.Default.Log(MyLogSeverity.Info, $"Found planter block {_planterComponent.IsPlantPlanted}");
        }
        
        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if (_planterComponent.IsPlantPlanted && _planterComponent.IsAlive)
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
                Vector3D pos = e.PositionComp?.GetPosition() ?? Vector3D.Zero;

                var floating = e as MyFloatingObject;
                if(floating == null || !obb.Contains(ref pos) || floating.IsPreview)
                    return false;

                return floating.Item.Content is MyObjectBuilder_SeedItem && floating.Item.Amount >= _planterComponent.AmountOfSeedsRequired;
            }) as MyFloatingObject;
            
            if (match != null)
            {
                
                // HACK: Only the Player can plant, so i need to give the item to the player, and then call "PlantSeed()"
                var player = MyAPIGateway.Session.Player;
                if (player == null || !player.Character.HasInventory)
                {
                    MyAPIGateway.Utilities.ShowNotification("Player not found!");
                    return;
                }

                match.Item.Amount -= _planterComponent.AmountOfSeedsRequired;

                var playerInventory = MyAPIGateway.Session.Player.Character.GetInventory(0);
                playerInventory.AddItems(_planterComponent.AmountOfSeedsRequired, match.Item.Content);
                var items = playerInventory.GetItems();
                var inv = items.Last();
                _planterComponent.PlantSeed(inv); 
                
                match.UpdateInternalState();
            }
        }
    }
}