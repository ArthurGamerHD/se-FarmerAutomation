using System.Collections.Generic;
using System.Linq;
using EasyFarming.Helpers;
using EasyFarming.System.Config;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.ModAPI;

namespace EasyFarming.System.TerminalControls.Listbox
{
    public sealed class ListboxAllowedItems : TerminalControlsListbox
    {
        public ListboxAllowedItems()
        {
            CreateListbox("AllowedItems", "Whitelist");
            ((IMyTerminalControlListbox)TerminalControl).ItemSelected = Setter;
        }

        protected override void Getter(IMyTerminalBlock b, List<MyTerminalControlListBoxItem> itemList,
            List<MyTerminalControlListBoxItem> selected)
        {
            var settings = ConfigManager.GetConfigForBlock(b);

            if (settings == null)
                return;

            var allSeeds = MyDefinitionManager.Static.GetPhysicalItemDefinitions().Where(WhiteList).ToList();
            itemList.Add(ListBoxItemHelper.GetOrComputeListBoxItem("*seeds*", "All known seeds", Planter.AllDefinition));
            itemList.AddRange(allSeeds.Select(a => ListBoxItemHelper.GetOrComputeListBoxItem(a.DisplayNameText,a.DescriptionText, a.Id)));

            foreach (var item in settings.SelectedItems)
            {
                var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(item);

                if (physicalItem == null)
                {

                    selected.Add(ListBoxItemHelper.GetOrComputeListBoxItem(item + " <!>",
                        item + "\n <!> WARNING: This item does NOT exists <!> ", 
                        item));
                    continue;
                }

                var existing = itemList.FirstOrDefault(a =>
                {
                    if (a.UserData is MyDefinitionId)
                        return ((MyDefinitionId)a.UserData).Equals(physicalItem.Id);
                    return false;
                });
                    
                selected.Add(existing ?? 
                             ListBoxItemHelper.GetOrComputeListBoxItem(physicalItem.DisplayNameText + " <!>",
                                 physicalItem.DescriptionText + "\n <!> WARNING: This item does NOT appears to be a valid seed <!> ", 
                                 physicalItem.Id));
            }
        }

        protected override void Setter(IMyTerminalBlock b, List<MyTerminalControlListBoxItem> selection)
        {
            DebuggerHelper.Break();
            
            var settings = ConfigManager.GetConfigForBlock(b);

            if (settings == null)
                return;
            var items = selection.Where(a => a.UserData is MyDefinitionId)
                .Select(a => (MyDefinitionId)a.UserData).ToArray();
            settings.SelectedItems = items;
        } 
        
        public bool WhiteList(object a)
        {
            var item = a as MyPhysicalItemDefinition;
            
            if(item == null)
                return false;

            var id = item.Id.ToString();
            return id.Contains("_SeedItem/");
        }
    }
}