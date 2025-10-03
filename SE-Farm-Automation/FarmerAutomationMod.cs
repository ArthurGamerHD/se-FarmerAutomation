using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace FarmerAutomation
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class FarmerAutomationMod : MySessionComponentBase
    {
        public static FarmerAutomationMod Instance;
        public static MyEasyNetworkManager Network = new MyEasyNetworkManager(32161);

        public override void LoadData()
        {
            Instance = this;
        }

        public override void BeforeStart()
        {
            Network.Register();
            Network.OnReceivedPacket += OnPacketReceived;
        }

        public void OnPacketReceived(MyEasyNetworkManager.PacketIn packetRaw)
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
                Network?.UnRegister();
                Network = null;
            }
            catch (Exception e)
            {
                MyLog.Default.Log(MyLogSeverity.Error, e.ToString());
            }
            finally
            {
                Instance = null;
            }
        }

        public override void UpdateAfterSimulation()
        {
        }

        public override void Draw()
        {
        }
    }
}