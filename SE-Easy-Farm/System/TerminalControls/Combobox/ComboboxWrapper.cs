using System;
using System.Collections.Generic;
using System.Linq;
using EasyFarming.Helpers;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace EasyFarming.System.TerminalControls.Combobox
{
    public abstract class ComboboxWrapper : TerminalControlsWrapper
    {
        public static IMyTerminalBlock ReferenceBlock;

        internal static MyTerminalControlComboBoxItem? None = new MyTerminalControlComboBoxItem { Key = 0L, Value = MyStringId.Get("None") };

        /// <summary>
        /// Caches the current select item for allowing it to be displayed even when it doesn't match the current filters
        /// </summary>
        protected object SelectedCache = null;
        
        SearchTextbox _searchTextbox;
        public SearchTextbox SearchTextbox
        {
            get { return _searchTextbox; }
            set
            {
                if (_searchTextbox != null)
                {
                    _searchTextbox.TextChanged -= SearchTextboxOnTextChanged;
                }
                
                _searchTextbox = value;
                
                if (_searchTextbox != null)
                {
                    _searchTextbox.TextChanged += SearchTextboxOnTextChanged;
                }
            }
        }
        
        protected virtual void Content(List<MyTerminalControlComboBoxItem> blockList)
        {
            if (None != null)
                blockList.Add(None.Value);
        }

        void SearchTextboxOnTextChanged(string obj) => TerminalControl.UpdateVisual();
    }
}