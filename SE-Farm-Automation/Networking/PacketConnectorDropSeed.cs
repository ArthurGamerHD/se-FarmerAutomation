using ProtoBuf;

namespace FarmerAutomation
{
	[ProtoContract]
	class PacketConnectorDropSeed : MyEasyNetworkManager.IPacket
	{
		[ProtoMember(1)]
		public long BlockId { get; set; }

		
		// ReSharper disable once UnusedMember.Global
		public PacketConnectorDropSeed() { } // Required by ProtoBuf

		public PacketConnectorDropSeed(long blockId)
		{
			BlockId = blockId;
		}

		public int GetId()
		{
			return 1;
		}
	}
}