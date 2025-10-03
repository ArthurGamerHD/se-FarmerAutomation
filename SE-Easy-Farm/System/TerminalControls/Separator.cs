using Sandbox.ModAPI.Interfaces.Terminal;

namespace EasyFarming.System.TerminalControls
{
    public class Separator : TerminalControlsWrapper
    {
        static int _currentId;

        public override IMyTerminalControl TerminalControl { get; }

        public Separator(int id = -1)
        {
            if (id == -1)
            {
                id = _currentId;
                _currentId++;
            }

            var separator = CreateControl<IMyTerminalControlSeparator>($"ChartFilterSeparator{id}");
            separator.Visible = Visible;
            TerminalControl = separator;
        }
    }
}