using ProtoBuf;
using VRage.Game;
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
		private string _ItemDefinitionId { get; set; }
		public MyDefinitionId ItemDefinitionId
		{
			get
			{
				return MyDefinitionId.Parse(_ItemDefinitionId);
			}
			set
			{
				_ItemDefinitionId = value.ToString();
			}
		}

		public PacketPlayerPlantSeed() { }

		public PacketPlayerPlantSeed(long blockId, MyDefinitionId itemDefinitionId)
		{
			this.BlockId = blockId;
			this.ItemDefinitionId = itemDefinitionId;
		}

		public int GetId()
		{
			return 1;
		}
	}
}