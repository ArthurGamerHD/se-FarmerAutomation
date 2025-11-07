using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.ModAPI;
using SE_Farm_Automation.Extensions;
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
        static readonly BoundingBoxD EmptyBb = new BoundingBoxD();
        static readonly Dictionary<string, BoundingBoxD> BoundingBoxCache = new Dictionary<string, BoundingBoxD>();
        static Color _debugColor = new Color(255, 255, 255, 128);

        const string DETECTOR_NAME = "detector_farmplot_001";
        
        IMyFunctionalBlock _planterBlock;
        IMyFarmPlotLogic _planterComponent;
        
        BoundingBoxD _detectionArea = EmptyBb;

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
            ApplyNeedsUpdate();

            _planterBlock = block;
            MyLog.Default.Log(MyLogSeverity.Debug,
                $"{nameof(FarmerAutomation)}: Found planter block {_planterComponent.IsPlantPlanted}");

            _detectionArea = GetDetectionBoxForBlock(_planterBlock); 
            // May fail if the model is not yet loaded, UpdateItemDetector() will try again if the detectionArea is empty
        }

        public override void Close()
        {
            base.Close();
            FarmerAutomationMod.DrawDebugChanged -= ApplyNeedsUpdate;
            NeedsUpdate = MyEntityUpdateEnum.NONE;
        }

        void ApplyNeedsUpdate()
        {
            var flag = MyEntityUpdateEnum.NONE;

            if (FarmerAutomationMod.DrawDebug && !(MyAPIGateway.Session.IsServer && MyAPIGateway.Utilities.IsDedicated))
                flag |= MyEntityUpdateEnum.EACH_FRAME;
            if (MyAPIGateway.Session.IsServer)
                flag |= MyEntityUpdateEnum.EACH_100TH_FRAME;

            NeedsUpdate = flag;
        }

        public bool CanPlant()
        {
            return !_planterComponent.IsAlive || !_planterComponent.IsPlantPlanted;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            var matrix = _planterBlock.WorldMatrix;
            MySimpleObjectDraw.DrawTransparentBox(ref matrix, ref _detectionArea, ref _debugColor,
                MySimpleObjectRasterizer.Solid, 1);
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            UpdateItemDetector();
        }

        public void UpdateItemDetector()
        {
            if (!CanPlant())
                return;

            if (!MyAPIGateway.Session.IsServer)
            {
                MyLog.Default.Log(MyLogSeverity.Warning,
                    $"{nameof(FarmerAutomation)}: Item detection should be handled on server side");
                return;
            }

            if (_detectionArea == EmptyBb)
                _detectionArea = GetDetectionBoxForBlock(_planterBlock);

            var matrix = _planterBlock.WorldMatrix;
            var obb = new MyOrientedBoundingBoxD(_detectionArea, matrix);
            BoundingBoxD broadAabb = obb.GetAABB();

            var candidates = MyAPIGateway.Entities.GetEntitiesInAABB(ref broadAabb);
            var match = candidates.FirstOrDefault(e =>
            {
                var floating = e as MyFloatingObject;
                if (!(floating?.Item.Content is MyObjectBuilder_SeedItem) || floating.IsPreview ||
                    e.PositionComp == null)
                    return false;

                Vector3D pos = e.PositionComp.GetPosition();
                if (!obb.Contains(ref pos))
                    return false;

                return floating.Item.Amount >= _planterComponent.AmountOfSeedsRequired;
            }) as MyFloatingObject;

            if (match == null)
                return;

            _planterComponent.RemovePlant(false);
            _planterComponent.PlantSeed(match.Item.GetDefinitionId());
            match.Item.Amount -= _planterComponent.AmountOfSeedsRequired;
            match.UpdateInternalState();
        }

        static public BoundingBoxD GetDetectionBoxForBlock(IMyTerminalBlock planterBlock)
        {
            BoundingBoxD localBox;

            if (BoundingBoxCache.TryGetValue(planterBlock.BlockDefinition.ToString(), out localBox))
                return localBox;

            if (planterBlock.Model == null)
                return EmptyBb;

            IDictionary<string, IMyModelDummy> dummies = new Dictionary<string, IMyModelDummy>();

            planterBlock.Model?.GetDummies(dummies);
            
            IMyModelDummy detector;
            double halfScale = planterBlock.CubeGrid.GridSize * .5f;

            if (!dummies.TryGetValue(DETECTOR_NAME, out detector))
                return new BoundingBoxD(
                    new Vector3D(-halfScale, -halfScale, -halfScale),
                    new Vector3D(halfScale, halfScale, halfScale)
                );

            var referenceMatrix = detector.Matrix;
            localBox = CreateDetectionArea(referenceMatrix, halfScale);

            BoundingBoxCache[planterBlock.BlockDefinition.ToString()] = localBox;
            return localBox;

        }

        public static BoundingBoxD CreateDetectionArea(MatrixD referenceMatrix, double height)
        {
            // Extract absolute scale for each axis, so we can define which axis is "forward"
            // on this specific 3d model based on the one with smallest lenght
            double currentSize,
                x = Math.Abs(referenceMatrix.Scale.X),
                y = Math.Abs(referenceMatrix.Scale.Y),
                z = Math.Abs(referenceMatrix.Scale.Z),
                min = MathHelper.Min(x, MathHelper.Min(y, z));
            
            const double TOLERANCE = 1e-4;
            MatrixD matrix = referenceMatrix;
            Vector3D axisDir;

            if (Math.Abs(min - y) < TOLERANCE)
            {
                axisDir = matrix.Up; // local Y+
                currentSize = y;
            }
            else if (Math.Abs(min - x) < TOLERANCE)
            {
                axisDir = matrix.Right; // local X+
                currentSize = x;
            }
            else
            {
                axisDir = matrix.Forward; // local Z+
                currentSize = z;
            }

            var scaleFactor = height / currentSize;

            matrix = MatrixD.CreateScale(
                (Math.Abs(min - x) < TOLERANCE) ? scaleFactor : 1.0,
                (Math.Abs(min - y) < TOLERANCE) ? scaleFactor : 1.0,
                (Math.Abs(min - z) < TOLERANCE) ? scaleFactor : 1.0
            ); // Updates scale to increase the height of the Box (fixed to half the grid size)

            var finalMatrix = matrix * referenceMatrix;
            var offsetAmount = (height - currentSize) * 0.45;
            var offset = axisDir.Normalized() * offsetAmount;
            finalMatrix.Translation += offset;
            
            return finalMatrix.ToBoundingBox();
        }
    }
}