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
    public class FarmPlotConfig : ObservableConfig
    {
        long _parentGrid;
        bool _automationEnabled;
        long? _inputBlock;
        string _inputGroup;
        long? _outputBlock;
        string _outputGroup;
        string[] _selectedDefinition = Array.Empty<string>();
        long? _airSensor;
        long? _assembler;

        // ReSharper disable once UnusedMember.Global
        public FarmPlotConfig() // Needed for Protobuf
        {
        }

        public FarmPlotConfig(IMyTerminalBlock block)
        {
            ParentGrid = block.CubeGrid.EntityId;
        }

        [ProtoMember(1)]
        public long ParentGrid
        {
            get { return _parentGrid; }
            set { RaiseAndSetIfChanged(value, nameof(ParentGrid), ref _parentGrid); }
        }

        [ProtoMember(2)]
        public bool AutomationEnabled
        {
            get { return _automationEnabled; }
            set { RaiseAndSetIfChanged(value, nameof(AutomationEnabled), ref _automationEnabled); }
        }

        [ProtoMember(3)]
        public long? InputBlock
        {
            get { return _inputBlock; }
            set { RaiseAndSetIfChanged(value, nameof(InputBlock), ref _inputBlock); }
        }

        [ProtoMember(4)]
        public string InputGroup
        {
            get { return _inputGroup; }
            set { RaiseAndSetIfChanged(value, nameof(InputGroup), ref _inputGroup); }
        }

        [ProtoMember(5)]
        public long? OutputBlock
        {
            get { return _outputBlock; }
            set { RaiseAndSetIfChanged(value, nameof(OutputBlock), ref _outputBlock); }
        }

        [ProtoMember(6)]
        public string OutputGroup
        {
            get { return _outputGroup; }
            set { RaiseAndSetIfChanged(value, nameof(OutputGroup), ref _outputGroup); }
        }

        [ProtoMember(7)]
        public string[] SelectedDefinition
        {
            get { return _selectedDefinition; }
            set
            {
                _selectedDefinition = value;
                RaisePropertyChanged(nameof(SelectedDefinition));
            }
        }

        [ProtoMember(8)]
        public long? AirSensor
        {
            get { return _airSensor; }
            set { RaiseAndSetIfChanged(value, nameof(AirSensor), ref _airSensor); }
        }

        [ProtoMember(9)]
        public long? Assembler
        {
            get { return _assembler; }
            set { RaiseAndSetIfChanged(value, nameof(Assembler), ref _assembler); }
        }


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

        public override void CopyFrom(ObservableConfig newValue)
        {
            var newConfig = newValue as FarmPlotConfig;
            if(newConfig == null)
                return;

            _automationEnabled = newConfig.AutomationEnabled;
            _inputBlock = newConfig.InputBlock;
            _inputGroup = newConfig.InputGroup;
            _outputBlock = newConfig.OutputBlock;
            _outputGroup = newConfig.OutputGroup;
            _selectedDefinition = newConfig.SelectedDefinition;
            _airSensor = newConfig.AirSensor;
            _assembler = newConfig.Assembler;

            base.CopyFrom(newValue);
        }
    }

    public class ObservableConfig
    {
        public event Action<ObservableConfig, string> OnChanged;
        public event Action<ObservableConfig> OnSync;

        protected void RaisePropertyChanged(string propertyName) => OnChanged?.Invoke(this, propertyName);
        
        public virtual void CopyFrom(ObservableConfig newValue) => OnSync?.Invoke(this);

        protected void RaiseAndSetIfChanged(long newValue, string propertyName, ref long property)
        {
            if (property == newValue)
                return;
            property = newValue;
            RaisePropertyChanged(propertyName);
        }

        protected void RaiseAndSetIfChanged(long? newValue, string propertyName, ref long? property)
        {
            if (property == newValue)
                return;
            property = newValue;
            RaisePropertyChanged(propertyName);
        }

        protected void RaiseAndSetIfChanged(bool newValue, string propertyName, ref bool property)
        {
            if (property == newValue)
                return;
            property = newValue;
            RaisePropertyChanged(propertyName);
        }

        protected void RaiseAndSetIfChanged(string newValue, string propertyName, ref string property)
        {
            if (property == newValue)
                return;
            property = newValue;
            RaisePropertyChanged(propertyName);
        }
    }
}