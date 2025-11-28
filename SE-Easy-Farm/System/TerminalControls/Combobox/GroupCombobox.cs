using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyFarming.Helpers;
using EasyFarming.System.Config;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.Utils;
using IMyBlockGroup = Sandbox.ModAPI.Ingame.IMyBlockGroup;
using IMyCubeGrid = VRage.Game.ModAPI.IMyCubeGrid;


namespace EasyFarming.System.TerminalControls.Combobox
{
    public abstract class GroupCombobox : ComboboxWrapper
    {
        readonly List<IMyCubeGrid> _grids = new List<IMyCubeGrid>();
        readonly List<IMyBlockGroup> _groups = new List<IMyBlockGroup>();
        
        Dictionary<string, bool> _inventoryTypeCache = new Dictionary<string, bool>();

        protected string[] AllowedTypes { get; set; }
        
        public override IMyTerminalControl TerminalControl => _terminalControl;
        IMyTerminalControl _terminalControl;

        protected void CreateCombobox(string id, MyStringId title)
        {
            var combobox = CreateControl<IMyTerminalControlCombobox>(id);
            combobox.ComboBoxContent = Content;
            combobox.Getter = Getter;
            combobox.Setter = Setter;
            combobox.Visible = Visible;
            combobox.Title = title;
            _terminalControl = combobox;
        }

        long Getter(IMyTerminalBlock arg)
        {
            var settings = ConfigManager.GetConfigForBlock(ReferenceBlock);

            if (settings == null)
                return 0;

            string group;
            long? block;
            LoadConfig(settings, out block, out group);

            if (!string.IsNullOrEmpty(group))
            {
                SelectedCache = group;

                long id;
                if (ComboBoxItemHelper.TryGetGroupId($"*{group}*", out id))
                    return id;
                
                return 0;
            }

            if (block == null)
                return 0;

            SelectedCache = block.Value;
            return (long)SelectedCache;

        }

        protected override void Content(List<MyTerminalControlComboBoxItem> items)
        {
            base.Content(items);
            var blockList = new List<MyTerminalControlComboBoxItem>();

            if (ReferenceBlock == null)
                return;


            _grids.Clear();
            _groups.Clear();


            var referenceGrid = ReferenceBlock.CubeGrid;

            MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(ReferenceBlock.CubeGrid)
                .GetBlockGroups(_groups);
            
            
            var filter = "";

            if (SearchTextbox != null)
                filter = SearchTextbox.TextBuilder.ToString();
            
            blockList.AddRange(_groups.Where(group => string.IsNullOrEmpty(filter) || group.Name.Equals(SelectedCache) || filter.Split(' ').All(a =>
                                                          group.Name.Split(' ').Any(b => b.StartsWith(a, StringComparison.InvariantCultureIgnoreCase))))
                .Select(a => ComboBoxItemHelper.GetOrComputeComboBoxItem($"*{a.Name}*", -1L)));

            MyAPIGateway.GridGroups.GetGroup(referenceGrid, GridLinkTypeEnum.Logical, _grids);

            blockList.AddRange(referenceGrid.GetFatBlocks<IMyTerminalBlock>()
                .Where(c => IsValidBlock(c, ReferenceBlock, filter)).Select(a =>
                    ComboBoxItemHelper.GetOrComputeComboBoxItem(
                        a.DisplayNameText, a.EntityId)));

            foreach (var grid in _grids)
            {
                if (grid == ReferenceBlock.CubeGrid)
                    continue;

                blockList.AddRange(grid.GetFatBlocks<IMyTerminalBlock>()
                    .Where(c => IsValidBlock(c, ReferenceBlock, filter)).Select(a =>
                        ComboBoxItemHelper.GetOrComputeComboBoxItem(
                            $"@{a.DisplayNameText}@",
                            a.EntityId)));
            }
            
            items.AddRange(blockList);
        }

        bool IsValidBlock(IMyTerminalBlock block, IMyTerminalBlock referenceBlock, string filter = "")
        {
            if(block == null || !block.HasInventory || !MyVisualScriptLogicProvider.IsConveyorConnected(block.Name, referenceBlock.Name))
                return false; // Check if is a Terminal block
            
            if(block.EntityId.Equals(SelectedCache))
                return true;
            
            
            if(!(block.GetUserRelationToOwner(referenceBlock.OwnerId) <=
                    MyRelationsBetweenPlayerAndBlock.FactionShare &&
                    (string.IsNullOrEmpty(filter) || block.CustomName == null ||
                     filter.Split(' ').All(a =>
                         block.CustomName.Split(' ').Any(b => b.StartsWith(a, StringComparison.InvariantCultureIgnoreCase))))))
               return false;
            
            // I May or may not spend way too much time on this and my craziness level is increasing steadily,
            // but for some random Klang forsaken reason, Blocks other than Assemblers is NOT returning Seeds/Plants
            // on its inventory whitelist, I will just hardcode to ignore this 
            if(block is IMyShipConnector || block is IMyCargoContainer || block is IMyConveyorSorter)
                return true;
            if(!(block is IMyAssembler))
                return false;
            
            var def = block.BlockDefinition.ToString();
            bool allowed;
            
            if (_inventoryTypeCache.TryGetValue(def, out allowed))
                return allowed;
            
            List<MyItemType> types = new List<MyItemType>();

            for (var i = 0; i < block.InventoryCount; i++)
            {
               var inv = block.GetInventory(i);
               inv.GetAcceptedItems(types);

               StringBuilder sb = new StringBuilder();

               sb.Append(def);
               sb.AppendLine( $" inventory index: {i}");
               
               foreach (var type in types)
               {
                   sb.AppendLine($"{type.TypeId} : {type.ToString()}");
               }
               
               allowed = types.Any(a => AllowedTypes.Any(b => a.TypeId.EndsWith(b)));
               types.Clear();
               
               sb.AppendLine();
               sb.Append($"Searching For: ");
               
               foreach (var type in AllowedTypes)
               {
                   sb.Append($"{type}, ");
               }
               
               sb.AppendLine($"");
               sb.AppendLine($"Found: {allowed}");
               
               MyLog.Default.Log(MyLogSeverity.Debug, sb.ToString());
               
               if(allowed)
                   break;
            }
            
            _inventoryTypeCache[def] = allowed;
            return allowed;
        }

        protected virtual void Setter(IMyTerminalBlock b, long l)
        {
            var config = ConfigManager.GetConfigForBlock(b);

            if (config == null)
                return;

            string group;
            if (ComboBoxItemHelper.TryGetGroupName(l, out group))
            {
                SelectedCache = group;
                SetConfig(config, null, group.Substring(1, group.Length - 2));
            }
            else if (l != -1)
            {
                SelectedCache = l;
                SetConfig(config,  l, null);
            }
            else
            {
                SetConfig(config, null, null);
            }

            ConfigManager.Sync(b, config);
        }

        protected abstract void SetConfig(FarmPlotConfig config, long? block, string groups);
        protected abstract void LoadConfig(FarmPlotConfig config, out long? block, out string groups);
    }
}