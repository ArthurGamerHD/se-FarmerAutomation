using System.Collections.Generic;
using System.Linq;
using EasyFarming.Helpers;
using EasyFarming.System.Config;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using IMyBlockGroup = Sandbox.ModAPI.Ingame.IMyBlockGroup;


namespace EasyFarming.System.TerminalControls.Combobox
{
    public abstract class GroupCombobox : ComboboxWrapper
    {
        readonly List<IMyCubeGrid> _grids = new List<IMyCubeGrid>();
        readonly List<IMyBlockGroup> _groups = new List<IMyBlockGroup>();

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
                return -1;

            string group;
            long? block;
            LoadConfig(settings, out block, out group);

            if (!string.IsNullOrEmpty(group))
            {
                long id;
                if (ComboBoxItemHelper.TryGetGroupId($"*{group}*", out id))
                    return id;

                return -1;
            }

            if (block != null)
            {
                return block.Value;
            }

            return -1;
        }

        protected virtual void Content(List<MyTerminalControlComboBoxItem> blockList)
        {
            if (ReferenceBlock == null)
                return;


            _grids.Clear();
            _groups.Clear();


            var referenceGrid = ReferenceBlock.CubeGrid;

            MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(ReferenceBlock.CubeGrid).GetBlockGroups(_groups);
            blockList.AddRange(_groups.Select(a => ComboBoxItemHelper.GetOrComputeComboBoxItem($"*{a.Name}*", -1L)));

            MyAPIGateway.GridGroups.GetGroup(referenceGrid, GridLinkTypeEnum.Logical, _grids);
            
            blockList.AddRange(referenceGrid.GetFatBlocks<IMyTerminalBlock>().Where(c => IsValidBlock(c, ReferenceBlock)).Select(a => ComboBoxItemHelper.GetOrComputeComboBoxItem(
                a.DisplayNameText, a.EntityId)));

            foreach (var grid in _grids)
            {
                if (grid == ReferenceBlock.CubeGrid)
                    continue;

                blockList.AddRange(referenceGrid.GetFatBlocks<IMyTerminalBlock>().Where(c => IsValidBlock(c, ReferenceBlock)).Select(a => ComboBoxItemHelper.GetOrComputeComboBoxItem(
                    $"@{a.DisplayNameText}@",
                    a.EntityId)));
                
            }
        }

        bool IsValidBlock(IMyTerminalBlock block, IMyTerminalBlock referenceBlock)
        {
            return block != null && // Check if is a Terminal block
                   block.HasInventory && // Checking block that have inventory
                   block.GetUserRelationToOwner(referenceBlock.OwnerId) <=
                   MyRelationsBetweenPlayerAndBlock.FactionShare;
        }

        protected virtual void Setter(IMyTerminalBlock b, long l)
        {
            var config = ConfigManager.GetConfigForBlock(b);

            if (config == null)
                return;

            string group;
            if (ComboBoxItemHelper.TryGetGroupName(l, out group))
                SetConfig(config, null, group.Substring(1, group.Length - 2));
            else
                SetConfig(config, l, null);

            ConfigManager.Sync(b, config);
        }

        protected abstract void SetConfig(FarmPlotConfig config, long? block, string groups);
        protected abstract void LoadConfig(FarmPlotConfig config, out long? block, out string groups);
    }
}