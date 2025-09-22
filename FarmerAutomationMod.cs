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
        public static MyEasyNetworkManager network = new MyEasyNetworkManager(32161);
        private int nextUpdate = 0;
        public int maxDistanceSquared = 2000 * 2000;
        public List<IMyPlayer> players = new List<IMyPlayer>();

        public readonly List<PlantRequest> PendingRequests = new List<PlantRequest>();

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

                if(block != null)
                {
                    foreach (var component in block?.Components)
                    {
                        var planterComponent = component as Planter;
                        if (planterComponent != null)
                        {
                            planter = planterComponent;
                            break;
                        }
                    }
                }

                if (planter == null || itemDefinitionId == null)
                {
                    MyLog.Default.Log(MyLogSeverity.Warning,
                        $"FarmerAutomation: Client received plant seed packet with {(block != null ? "" : "NULL Block ")}{(itemDefinitionId != null ? "" : "NULL itemDefinitionId ")}{(planter != null ? "" : "NULL Planter component")}");
                    return;
                }

                var request = new PlantRequest(itemDefinitionId.Value, planter);
                PendingRequests.Add(request);
            }

            if (packetRaw.PacketId == 2)
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
            if (nextUpdate % 10 == 0 && PendingRequests.Any())
                HandlePlantRequest();

            nextUpdate--;
            if (nextUpdate-- > 0)
                return;
            nextUpdate = 600; // every 600 frames, about 10 seconds

            players.Clear();
            MyAPIGateway.Players.GetPlayers(players,
                (p) => !p.IsBot && p.Character != null && p.Character.HasInventory);

            var syncDistance = MyAPIGateway.Session.SessionSettings.SyncDistance - 100;
            maxDistanceSquared = syncDistance * syncDistance;
        }

        private void HandlePlantRequest()
        {
            for (var index = 0; index < PendingRequests.Count;)
            {
                var current = PendingRequests[index];

                if (current.CurrentTry > PlantRequest.MAX_RETRY)
                {
                    PendingRequests.RemoveAtFast(index);
                    MyLog.Default.Log(MyLogSeverity.Error,$"FarmerAutomation: Max retries exceeded {PlantRequest.MAX_RETRY}");
                    continue;
                }

                if (!current.Planter.TryPlantInventorySeedInFarmPlot(current.DefinitionId))
                {
                    MyLog.Default.Log(MyLogSeverity.Debug,"FarmerAutomation: ({0}x) Failed to find inventory item after adding it", current.CurrentTry);
                }

                if (!current.Planter.CanPlant())
                {
                    PendingRequests.RemoveAtFast(index);
                    continue;
                }

                current.CurrentTry++;
                index++;
            }
        }

        public override void Draw()
        {
        }
    }

    public class PlantRequest
    {
        public const int MAX_RETRY = 10;
        public int CurrentTry;
        public MyDefinitionId DefinitionId;
        public Planter Planter;

        public PlantRequest(MyDefinitionId definitionId, Planter planter)
        {
            DefinitionId = definitionId;
            Planter = planter;
        }
    }
}