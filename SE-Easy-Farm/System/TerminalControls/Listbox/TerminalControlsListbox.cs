using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.ModAPI;
using VRage.Utils;

namespace EasyFarming.System.TerminalControls.Listbox
{
    public abstract class TerminalControlsListbox : TerminalControlsWrapper
    {
        public override IMyTerminalControl TerminalControl => _terminalControl;
        IMyTerminalControl _terminalControl;

        protected void CreateListbox(string id, string title)
        {
            var listbox = CreateControl<IMyTerminalControlListbox>(id);
            listbox.ListContent = Getter;
            listbox.ItemSelected = Setter;
            listbox.Visible = Visible;
            listbox.VisibleRowsCount = 8;
            listbox.Multiselect = true;
            listbox.Title = MyStringId.GetOrCompute(title);
            _terminalControl = listbox;
        }

        protected abstract void Getter(IMyTerminalBlock b, List<MyTerminalControlListBoxItem> itemList,
            List<MyTerminalControlListBoxItem> selected);

        protected abstract void Setter(IMyTerminalBlock b, List<MyTerminalControlListBoxItem> selection);
    }
}