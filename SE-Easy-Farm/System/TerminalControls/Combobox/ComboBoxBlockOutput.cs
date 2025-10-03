using EasyFarming.System.Config;
using VRage;
using VRage.Utils;

namespace EasyFarming.System.TerminalControls.Combobox
{
    public sealed class ComboBoxBlockOutput : GroupCombobox
    {
        public ComboBoxBlockOutput()
        {
            var strings = MyTexts.GetString("DisplayName_BlockGroup_InputOutputGroup").Split('/');
            var text = strings.Length == 2 ? MyStringId.GetOrCompute(strings[1]) : MyStringId.NullOrEmpty;
            CreateCombobox("OutputBlock", text);
        }

        protected override void SetConfig(FarmPlotConfig config, long? block, string groups)
        {
            config.OutputBlock = block;
            config.OutputGroup = groups;
        }

        protected override void LoadConfig(FarmPlotConfig config, out long? block, out string groups)
        {
            block = config.OutputBlock;
            groups = config.OutputGroup;
        }
    }
}