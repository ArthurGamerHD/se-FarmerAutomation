using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace EasyFarming.System.TerminalControls
{
    public sealed class Label : TerminalControlsWrapper
    {
        static int _currentId;

        public override IMyTerminalControl TerminalControl { get; }

        public Label(string labelText, int id = -1)
        {
            if (id == -1)
            {
                id = _currentId;
                _currentId++;
            }
            
            var label = CreateControl<IMyTerminalControlLabel>($"ChartFilterLabel{id}");
            label.Visible = Visible;
            label.Label = MyStringId.GetOrCompute(labelText);
            TerminalControl = label;
        }
    }
}