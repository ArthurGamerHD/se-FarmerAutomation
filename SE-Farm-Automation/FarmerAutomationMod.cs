using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace FarmerAutomation
{
    // ReSharper disable once ClassNeverInstantiated.Global
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class FarmerAutomationMod : MySessionComponentBase
    {
        static bool _drawDebug;

        public static readonly List<MyFloatingObject> FloatingSeeds = new List<MyFloatingObject>();

        public static bool DrawDebug
        {
            get { return _drawDebug; }
            set
            {
                _drawDebug = value;
                DrawDebugChanged?.Invoke();
            }
        }

        public static event Action DrawDebugChanged;
        public static event Action<Vector3D> OnFloatingSeedAdded;
        
        public static MyEasyNetworkManager Network = new MyEasyNetworkManager(32161);

        public override void LoadData()
        {
            if (SessionUtil.IsClient)
            {
                MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
            }

            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Entities.OnEntityAdd += EntitiesOnEntityAdd;
                MyAPIGateway.Entities.OnEntityRemove += EntitiesOnEntityRemove;
                RefreshEntities();
            }
        }

        void RefreshEntities()
        {
            MyAPIGateway.Entities.GetEntities(null, entity =>
            {
                if (!IsSeed(entity as MyFloatingObject))
                    return false;

                FloatingSeeds.Add((MyFloatingObject)entity);
                return false;
            });
        }

        void EntitiesOnEntityRemove(IMyEntity obj)
        {
            if (!IsSeed(obj as MyFloatingObject))
                return;

            FloatingSeeds.Remove((MyFloatingObject)obj);
        }

        void EntitiesOnEntityAdd(IMyEntity obj)
        {
            if (!IsSeed(obj as MyFloatingObject))
                return;

            var seed = (MyFloatingObject)obj;
            MyLog.Default.Log(MyLogSeverity.Debug, $"{nameof(FarmerAutomation)}: Found seed item {seed.DisplayName} at {seed.PositionComp.GetPosition()}");

            // sadly "seed.IsPreview" doesn't work yet, the "correct" way of fix it, is subscribing to "seed.IsPreviewChanged",
            // but is less complex to just let the client code deal with that by just checking "if(!obj.IsPreview)"
            FloatingSeeds.Add(seed);

            if (OnFloatingSeedAdded == null) 
                return; 
            
            // wake planters in background
            MyAPIGateway.Parallel.StartBackground(() => OnFloatingSeedAdded.Invoke(seed.PositionComp.GetPosition()));
        }

        void OnMessageEntered(string text, ref bool others)
        {
            if (text.StartsWith("!pa"))
                others = false;
            else
                return;

            string message;
            
            if (text.StartsWith("!padebug")  && SessionUtil.IsClient)
            {
                DrawDebug = !DrawDebug;
                message = $"Draw debug {(DrawDebug ? "Enabled" : "Disabled")}";
            }
            else
            {
                message = "Unknown command";
            }
            
            MyAPIGateway.Utilities.ShowMessage(nameof(FarmerAutomation), message);
            MyLog.Default.Log(MyLogSeverity.Info, $"{nameof(FarmerAutomation)}: {message}");
        }

        public override void BeforeStart()
        {
            Network.Register();
            Network.OnReceivedPacket += OnPacketReceived;
        }

        void OnPacketReceived(MyEasyNetworkManager.PacketIn packetRaw)
        {
            if (packetRaw.PacketId == 1)
            {
                var packet = packetRaw.UnWrap<PacketConnectorDropSeed>();
                var block = MyEntities.GetEntityById(packet.BlockId) as IMyShipConnector;

                if (block != null)
                {
                    ConnectorLogicComponent.ThrowOutSingleItem(block);
                }
            }
        }

        protected override void UnloadData()
        {
            try
            {
                if (SessionUtil.IsClient)
                {
                    MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
                }

                if (SessionUtil.IsServer)
                {
                    MyAPIGateway.Entities.OnEntityAdd -= EntitiesOnEntityAdd;
                    MyAPIGateway.Entities.OnEntityRemove -= EntitiesOnEntityRemove;
                }

                DrawDebugChanged = null;
                OnFloatingSeedAdded = null;
                
                Network?.UnRegister();
                Network = null;
            }
            catch (Exception e)
            {
                MyLog.Default.Log(MyLogSeverity.Error, e.ToString());
            }
        }

        public override void UpdateAfterSimulation()
        {
        }

        public override void Draw()
        {
        }

        static bool IsSeed(MyFloatingObject obj) => obj?.Item.Content is MyObjectBuilder_SeedItem && 
                                                    obj.PositionComp != null;
    }
}