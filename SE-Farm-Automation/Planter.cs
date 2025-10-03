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
                if (!(floating?.Item.Content is MyObjectBuilder_SeedItem) || floating.IsPreview || e.PositionComp == null)
                    return false;

                Vector3D pos = e.PositionComp.GetPosition();
                if (!obb.Contains(ref pos))
                    return false;

                return floating.Item.Amount >= _planterComponent.AmountOfSeedsRequired;
            }) as MyFloatingObject;

            if (match == null) 
                return;

            match.Item.Amount -= _planterComponent.AmountOfSeedsRequired;
            _planterComponent.PlantSeed(match.Item.GetDefinitionId());
            match.UpdateInternalState();
        }
    }
}