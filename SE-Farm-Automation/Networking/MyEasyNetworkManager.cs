using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace FarmerAutomation
{
    public class MyEasyNetworkManager
    {

        private readonly ushort CommsId;
        public Action<PacketIn> OnReceivedPacket;
        public Action<PacketIn> ProcessPacket;

        public MyEasyNetworkManager(ushort CommsId)
        {
            this.CommsId = CommsId;
        }

        public void Register()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(CommsId, ReceivedPacket);
        }

        public void UnRegister()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(CommsId, ReceivedPacket);
        }

        public void TransmitToServer(IPacket data, bool SendToAllPlayers = true, bool SendToSender = false)
        {
            PacketBase packet = new PacketBase(data.GetId(), SendToAllPlayers, SendToSender);
            packet.Wrap(data);
            MyAPIGateway.Multiplayer.SendMessageToServer(CommsId, MyAPIGateway.Utilities.SerializeToBinary(packet));
        }

        public void TransmitToPlayer(IPacket data, ulong playerId, bool SendToSender = false)
        {
            PacketBase packet = new PacketBase(data.GetId(), false, SendToSender);
            packet.Wrap(data);
            MyAPIGateway.Multiplayer.SendMessageTo(CommsId, MyAPIGateway.Utilities.SerializeToBinary(packet), playerId);
        }

        private void ReceivedPacket(ushort handler, byte[] raw, ulong id, bool isFromServer)
        {
            try
            {
                PacketBase packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(raw);
                PacketIn packetIn = new PacketIn(packet.Id, packet.Data, id, isFromServer);

                ProcessPacket?.Invoke(packetIn);
                if (packetIn.IsCancelled)
                {
                    return;
                }

                if (packet.SendToAllPlayers && MyAPIGateway.Session.IsServer)
                {
                    TransmitPacketToAllPlayers(id, packet);
                }

                if ((!isFromServer && MyAPIGateway.Session.IsServer) ||
                    (isFromServer && (!MyAPIGateway.Session.IsServer || packet.SendToSender)))
                {
                    OnReceivedPacket?.Invoke(packetIn);
                }

            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"Malformed packet from {id}!");
                MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");

                if (MyAPIGateway.Session?.Player != null)
                    MyAPIGateway.Utilities.ShowNotification($"[ERROR: {GetType().FullName}: {e.Message} | Send SpaceEngineers.Log to mod author]", 10000, MyFontEnum.Red);
            }
        }

        private void TransmitPacketToAllPlayers(ulong sender, PacketBase packet)
        {
            var TempPlayers = new List<IMyPlayer>(MyAPIGateway.Session.SessionSettings.MaxPlayers);
            MyAPIGateway.Players.GetPlayers(TempPlayers);

            foreach (var p in TempPlayers)
            {
                if (p.IsBot || p.SteamUserId == MyAPIGateway.Multiplayer.ServerId || (!packet.SendToSender && p.SteamUserId == sender))
                    continue;

                MyAPIGateway.Multiplayer.SendMessageTo(CommsId, MyAPIGateway.Utilities.SerializeToBinary(packet), p.SteamUserId);
            }
        }

        [ProtoContract]
        private class PacketBase
        {
            [ProtoMember(1)]
            public readonly int Id;
            [ProtoMember(2)]
            public readonly bool SendToAllPlayers;
            [ProtoMember(3)]
            public readonly bool SendToSender;

            [ProtoMember(4)]
            public byte[] Data;

            public PacketBase() { }

            public PacketBase(int Id, bool SendToAllPlayers, bool SendToSender)
            {
                this.Id = Id;
                this.SendToAllPlayers = SendToAllPlayers;
                this.SendToSender = SendToSender;
            }

            public void Wrap(object data)
            {
                Data = MyAPIGateway.Utilities.SerializeToBinary(data);
            }
        }

        public interface IPacket
        {
            int GetId();
        }

        public class PacketIn {
            public bool IsCancelled { protected set; get; }
            public int PacketId { protected set; get; }
            public ulong SenderId { protected set; get; }
            public bool IsFromServer { protected set; get; }
            
            private readonly byte[] Data;

            public PacketIn(int packetId, byte[] data, ulong senderId, bool isFromServer)
            {
                this.PacketId = packetId;
                this.SenderId = senderId;
                this.IsFromServer = isFromServer;
                this.Data = data;
            }

            public T UnWrap<T>()
            {
                return MyAPIGateway.Utilities.SerializeFromBinary<T>(Data);
            }

            public void SetCancelled(bool value)
            {
                this.IsCancelled = value;
            }
        }

    }

}
