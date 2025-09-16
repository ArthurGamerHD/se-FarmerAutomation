using System;
using System.Collections.Generic;
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
        public static MyEasyNetworkManager network = new MyEasyNetworkManager(32161);
        private int nextUpdate = 0;
        public int maxDistanceSquared = 2000 * 2000;
        public List<IMyPlayer> players = new List<IMyPlayer>();

        public override void LoadData()
        {
            Instance = this;

            SetUpdateOrder(MyUpdateOrder.AfterSimulation);
        }

        public override void BeforeStart()
        {
            network.Register();
            network.OnReceivedPacket += OnPacketReceived;
        }

        public void OnPacketReceived(MyEasyNetworkManager.PacketIn packetRaw)
        {
            if (packetRaw.PacketId == 1)
            {
                var packet = packetRaw.UnWrap<PacketPlayerPlantSeed>();
                var block = MyEntities.GetEntityById(packet.BlockId) as IMyFunctionalBlock;
                var itemDefinitionId = packet.ItemDefinitionId;

                Planter planter = null;

                foreach (var component in block?.Components)
                {
                    var planeterComponent = component as Planter;
                    if (planeterComponent != null)
                    {
                        planter = planeterComponent;
                        break;
                    }
                }

                if (planter == null || itemDefinitionId == null)
                {
                    MyLog.Default.Log(MyLogSeverity.Warning, $"FarmerAutomation: Client received plant seed packet with {(block != null ? "" : "NULL Block ")}{(itemDefinitionId != null ? "" : "NULL itemDefinitionId ")}{(planter != null ? "" : "NULL Planter component")}");
                    return;
                }

                planter.PlantInventorySeedInFarmPlot(itemDefinitionId);
            }
        }

        protected override void UnloadData()
        {
            try
            {
                network?.UnRegister();
                network = null;
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
            nextUpdate--;
            if (nextUpdate-- > 0)
                return;
            nextUpdate = 600; // every 600 frames, about 10 seconds

            players.Clear();
            MyAPIGateway.Players.GetPlayers(players, (p) => !p.IsBot && p.Character != null && p.Character.HasInventory);

            var syncDistance = MyAPIGateway.Session.SessionSettings.SyncDistance - 100;
            maxDistanceSquared = syncDistance * syncDistance;
        }

        public override void Draw()
        {
        }
    }
}