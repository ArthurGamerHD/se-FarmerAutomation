using ProtoBuf;
using VRage.Game.ModAPI;
using VRageMath;

namespace FarmerAutomation
{
    [ProtoContract]
    class PacketPlayerPlantSeed : MyEasyNetworkManager.IPacket
    {
        [ProtoMember(1)]
        public long BlockId { get; set; }
        [ProtoMember(2)]
        public long FloatingId { get; set; }

        public PacketPlayerPlantSeed() { }

        public PacketPlayerPlantSeed(long blockId, long floatingId)
        {
            this.BlockId = blockId;
            this.FloatingId = floatingId;
        }

        public int GetId()
        {
            return 1;
        }
    }
}