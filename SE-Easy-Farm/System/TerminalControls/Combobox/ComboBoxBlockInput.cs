using EasyFarming.System.Config;
using VRage;
using VRage.Utils;

namespace EasyFarming.System.TerminalControls.Combobox
{
    public sealed class ComboBoxBlockInput : GroupCombobox
    {
        public ComboBoxBlockInput()
        {
            var strings = MyTexts.GetString("DisplayName_BlockGroup_InputOutputGroup").Split('/');
            var text = strings.Length == 2 ? MyStringId.GetOrCompute(strings[0]) : MyStringId.NullOrEmpty;
            CreateCombobox("InputBlocks", text);
        }

        protected override void SetConfig(FarmPlotConfig config, long? block, string groups)
        {
            config.InputBlock = block;
            config.InputGroup = groups;
        }

        protected override void LoadConfig(FarmPlotConfig config, out long? block, out string groups)
        {
            block = config.InputBlock;
            groups = config.InputGroup;
        }
    }
}