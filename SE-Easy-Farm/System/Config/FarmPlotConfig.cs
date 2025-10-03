using System;
using System.Linq;
using EasyFarming.Helpers;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game;
using VRageMath;

namespace EasyFarming.System.Config
{
    [ProtoContract]
    public class FarmPlotConfig
    {
        // ReSharper disable once UnusedMember.Global
        public FarmPlotConfig() // Needed for Protobuf
        {
        }

        public FarmPlotConfig(IMyTerminalBlock block)
        {
            ParentGrid = block.CubeGrid.EntityId;
        }

        [ProtoMember(1)] public long ParentGrid { get; set; }
        
        [ProtoMember(2)] public bool AutomationEnabled { get; set; }

        [ProtoMember(3)] public long? InputBlock { get; set; }

        [ProtoMember(4)] public string InputGroup { get; set; }

        [ProtoMember(5)] public long? OutputBlock { get; set; }

        [ProtoMember(6)] public string OutputGroup { get; set; }
        
        [ProtoMember(7)] public string[] SelectedDefinition { get; set; } = Array.Empty<string>();

        [ProtoMember(8)] public long? AirSensor { get; set; }

        [ProtoMember(9)] public long? Assembler { get; set; }


        public MyDefinitionId[] SelectedItems
        {
            get
            {
                try
                {
                    return SelectedDefinition.Select(MyDefinitionId.Parse).ToArray();
                }
                catch (Exception e)
                {
                    ErrorHandlerHelper.LogError(e, this);
                }

                return Array.Empty<MyDefinitionId>();
            }
            set
            {
                SelectedDefinition = value.Select(a => a.ToString()).ToArray();
            }
        }

        public void CopyFrom(FarmPlotConfig newValue)
        {
            AutomationEnabled = newValue.AutomationEnabled;
            InputBlock = newValue.InputBlock;
            InputGroup = newValue.InputGroup;
            OutputBlock = newValue.OutputBlock;
            OutputGroup = newValue.OutputGroup;
            SelectedDefinition = newValue.SelectedDefinition;
        }
    }
}