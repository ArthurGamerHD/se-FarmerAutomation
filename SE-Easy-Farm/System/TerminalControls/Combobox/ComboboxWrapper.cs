using Sandbox.ModAPI;

namespace EasyFarming.System.TerminalControls.Combobox
{
    public abstract class ComboboxWrapper : TerminalControlsWrapper
    {
        public static IMyTerminalBlock ReferenceBlock;

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

        void SearchTextboxOnTextChanged(string obj) => TerminalControl.UpdateVisual();
    }
}