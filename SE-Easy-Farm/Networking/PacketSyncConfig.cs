using EasyFarming.System.Config;
using ProtoBuf;

namespace EasyFarming.Networking
{
    [ProtoContract]
    class PacketSyncConfig : NetworkPackage
    {
        public override PackageCode Code => PackageCode.SyncConfig;
        
        [ProtoMember(1)] public long BlockId { get; set; }
        [ProtoMember(2)] public FarmPlotConfig Config { get; set; }

        // ReSharper disable once UnusedMember.Global
        public PacketSyncConfig()// Needed for Protobuf
        {
        }

        public PacketSyncConfig(long senderId, FarmPlotConfig config)
        {
            BlockId = senderId;
            Config = config;
        }
    }
}