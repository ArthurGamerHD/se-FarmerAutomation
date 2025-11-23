using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SE_Farm_Automation.Extensions;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using Task = ParallelTasks.Task;

namespace FarmerAutomation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_FunctionalBlock), useEntityUpdate: true)]
    public class Planter : MyGameLogicComponent
    {
        const string DETECTOR_NAME = "detector_farmplot_001";
        const float TRIM_DISTANCE_SQUARED = 2.5f * 2.5f; // seeds more distant to this gets ignored
        const float SLEEP_DISTANCE_SQUARED = 3000f * 3000f; // if there's no seed in this area, block starts to sleep

        Task? _backgroundTask;

        MyFloatingObject _match;

        static readonly Dictionary<string, BoundingBoxD> LocalBoundingBoxCache = new Dictionary<string, BoundingBoxD>();

        static Color _debugPlantedColor = new Color(0, 255, 255, 128),
            _debugReadColor = new Color(0, 255, 0, 128),
            _debugSleepColor = new Color(255, 0, 0, 128);

        bool IsSleeping { get; set; }

        static object _lock = new object();

        IMyFunctionalBlock _planterBlock;
        IMyFarmPlotLogic _planterComponent;

        MyOrientedBoundingBoxD _obb;
        BoundingBoxD _detectionArea;
        bool _shouldSleep, _hasModelLoaded;

        Vector3D _pos;
        double _distance;

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

            FarmerAutomationMod.OnFloatingSeedAdded += SeedAdded;

            _planterBlock = block;

            MyLog.Default.Log(MyLogSeverity.Debug,
                $"{nameof(FarmerAutomation)}: Found planter block - Planted = {_planterComponent.IsPlantPlanted}");

            TryGetBoundingBox(); // will fail if the model is not yet loaded, but then it will try again later
        }

        void SeedAdded(Vector3D pos) => IsSleeping = IsSleeping &&
                                                     Vector3D.DistanceSquared(pos,
                                                         _planterBlock.WorldMatrix.Translation) >
                                                     SLEEP_DISTANCE_SQUARED;

        public override void Close()
        {
            base.Close();
            FarmerAutomationMod.DrawDebugChanged -= ApplyNeedsUpdate;
            FarmerAutomationMod.OnFloatingSeedAdded -= SeedAdded;
            NeedsUpdate = MyEntityUpdateEnum.NONE;
        }

        void ApplyNeedsUpdate()
        {
            var flag = MyEntityUpdateEnum.NONE;

            if (FarmerAutomationMod.DrawDebug && !SessionUtil.IsDedicatedServer)
                flag |= MyEntityUpdateEnum.EACH_FRAME;
            if (SessionUtil.IsServer)
                flag |= MyEntityUpdateEnum.EACH_100TH_FRAME;

            NeedsUpdate = flag;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            var matrix = _planterBlock.WorldMatrix;

            if (!_hasModelLoaded && !TryGetBoundingBox())
                return;

            var color = _planterComponent.IsPlantPlanted ? _debugPlantedColor :
                IsSleeping ? _debugSleepColor : _debugReadColor;

            MySimpleObjectDraw.DrawTransparentBox(ref matrix, ref _detectionArea, ref color,
                MySimpleObjectRasterizer.Solid, 1);
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if (_match != null)
            {
                PlantSeed();
                return;
            }

            if (IsSleeping || (_backgroundTask != null && !_backgroundTask.Value.IsComplete))
                return;

            _backgroundTask = MyAPIGateway.Parallel.StartBackground(UpdateItemDetector);
        }

        void UpdateItemDetector()
        {
            if (!SessionUtil.IsServer)
            {
                MyLog.Default.Log(MyLogSeverity.Warning,
                    $"{nameof(FarmerAutomation)}: Item detection should be handled on server side");
                ApplyNeedsUpdate();
                return;
            }

            if (!CanPlant(_planterComponent))
                return;

            if (!_hasModelLoaded && !TryGetBoundingBox())
                return;

            _obb = new MyOrientedBoundingBoxD(_detectionArea, _planterBlock.WorldMatrix);

            _shouldSleep = true;
            _match = null;

            foreach (var seed in FarmerAutomationMod.FloatingSeeds)
            {
                if (_match != null || seed.IsPreview)
                    continue;

                _pos = seed.PositionComp.GetPosition();
                _distance = Vector3D.DistanceSquared(_pos, _planterBlock.WorldMatrix.Translation);

                if (_shouldSleep && _distance < SLEEP_DISTANCE_SQUARED)
                    _shouldSleep = false;

                if (_distance > TRIM_DISTANCE_SQUARED || !_obb.Contains(ref _pos))
                    continue;

                if (seed.Item.Amount < _planterComponent.AmountOfSeedsRequired)
                    continue;

                _match = seed;
            }

            IsSleeping = _shouldSleep;
        }

        public void PlantSeed()
        {
            _planterComponent.RemovePlant(false);
            _planterComponent.PlantSeed(_match.Item.GetDefinitionId());
            _match.Item.Amount -= _planterComponent.AmountOfSeedsRequired;
            _match.UpdateInternalState();
            _match = null;
        }

        bool TryGetBoundingBox()
        {
            if (_planterBlock.Model == null) // plant-blocks serialized on world load will not have this data;
                return false;

            _hasModelLoaded = true;

            lock (_lock) // this can be called from multiple threads but the LocalBoundingBoxCache is Static and Shared
                GetDetectionBoxForBlock(_planterBlock, out _detectionArea);

            return true;
        }

        static void GetDetectionBoxForBlock(IMyTerminalBlock planterBlock, out BoundingBoxD localBox)
        {
            if (LocalBoundingBoxCache.TryGetValue(planterBlock.BlockDefinition.ToString(), out localBox))
                return;

            IDictionary<string, IMyModelDummy> dummies = new Dictionary<string, IMyModelDummy>();
            planterBlock.Model?.GetDummies(dummies);
            double halfScale = planterBlock.CubeGrid.GridSize * .5f;

            IMyModelDummy detector;
            if (dummies.TryGetValue(DETECTOR_NAME, out detector))
            {
                var referenceMatrix = detector.Matrix;
                localBox = CreateDetectionArea(referenceMatrix, halfScale);
            }
            else
            {
                localBox = new BoundingBoxD(
                    new Vector3D(-halfScale, -halfScale, -halfScale),
                    new Vector3D(halfScale, halfScale, halfScale)
                );
            }

            LocalBoundingBoxCache[planterBlock.BlockDefinition.ToString()] = localBox;
        }

        static BoundingBoxD CreateDetectionArea(MatrixD referenceMatrix, double height)
        {
            // Extract absolute scale for each axis, so we can define which axis is "forward" on this specific 3d model
            // based on the one with smallest lenght
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

        static bool CanPlant(IMyFarmPlotLogic comp) => !comp.IsAlive || !comp.IsPlantPlanted;
    }
}