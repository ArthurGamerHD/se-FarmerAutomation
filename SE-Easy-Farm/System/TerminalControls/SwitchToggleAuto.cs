using System;
using EasyFarming.System.Config;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace EasyFarming.System.TerminalControls
{
    public class SwitchToggleAuto : TerminalControlsWrapper
    {
        public override IMyTerminalControl TerminalControl { get; }

        public event Action OnToggled;
        
        public SwitchToggleAuto()
        {
            var slider = CreateControl<IMyTerminalControlOnOffSwitch>("LinesSwitch");
            slider.Getter = Getter;
            slider.Setter = Setter;
            slider.Visible = Visible;
            slider.Title = MyStringId.GetOrCompute("RadialMenuGroupTitle_Automation");
            slider.OnText = MyStringId.GetOrCompute("HudInfoOn");
            slider.OffText = MyStringId.GetOrCompute("HudInfoOff");
            
            TerminalControl = slider;
        }

        void Setter(IMyTerminalBlock block, bool value)
        {
            var config = ConfigManager.GetConfigForBlock(block);
            if (config == null)
                return;

            config.AutomationEnabled = value;
            ConfigManager.Sync(block);
            ConfigManager.GetInstanceForBlock(block).UpdateAutomation();
            OnToggled?.Invoke();
        }

        bool Getter(IMyTerminalBlock myTerminalBlock)
        {
            var config = ConfigManager.GetConfigForBlock(myTerminalBlock);
            return config != null && config.AutomationEnabled;
        }
    }
}