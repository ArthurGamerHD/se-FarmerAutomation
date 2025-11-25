using System;
using System.Text;
using EasyFarming.System.Config;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace EasyFarming.System.TerminalControls
{
    public class SearchTextbox : TerminalControlsWrapper
    {
        static int _currentId;
        
        public override IMyTerminalControl TerminalControl => _textbox;
        public StringBuilder TextBuilder {get; private set;}

        public event Action<string> TextChanged;

        IMyTerminalControlTextbox _textbox;
        
        public SearchTextbox(int id = -1)
        {
           
            if (id == -1)
            {
                id = _currentId;
                _currentId++;
            }
            
            _textbox = CreateControl<IMyTerminalControlTextbox>($"SearchBox{id}");
            _textbox.Setter = Setter;
            _textbox.Getter  = Getter;
            _textbox.Visible = Visible;
            _textbox.Title = MyStringId.GetOrCompute("WorkshopBrowser_Search");
            _textbox.Tooltip = _textbox.Title;
        }

        StringBuilder Getter(IMyTerminalBlock arg)
        {
            TextBuilder = TextBuilder ?? new StringBuilder();
            return TextBuilder;
        }

        void Setter(IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            TextBuilder = stringBuilder;
            TextChanged?.Invoke(TextBuilder.ToString());
        }
    }
}