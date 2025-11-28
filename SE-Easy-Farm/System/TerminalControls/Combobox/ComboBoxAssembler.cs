using System;
using System.Collections.Generic;
using System.Linq;
using EasyFarming.Helpers;
using EasyFarming.System.Config;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace EasyFarming.System.TerminalControls.Combobox
{
    public sealed class ComboBoxAssembler : ComboboxWrapper
    {
        static readonly Dictionary<string, bool> AssemblersAllowsSeeds = new Dictionary<string, bool>();

        readonly List<IMyCubeGrid> _grids = new List<IMyCubeGrid>();

        public override IMyTerminalControl TerminalControl { get; }

        public ComboBoxAssembler()
        {
            var combobox = CreateControl<IMyTerminalControlCombobox>("Assembler");
            combobox.ComboBoxContent = Content;
            combobox.Getter = Getter;
            combobox.Setter = Setter;
            combobox.Visible = Visible;
            combobox.Title = MyStringId.GetOrCompute("DisplayName_Block_FoodProcessor");
            TerminalControl = combobox;
        }

        long Getter(IMyTerminalBlock arg)
        {
            var settings = ConfigManager.GetConfigForBlock(ReferenceBlock);

            if (settings == null)
                return 0;

            var block = settings.Assembler;
            if (block == null) return 0;
            SelectedCache = block.Value;
            return block.Value;


        }

        protected override void Content(List<MyTerminalControlComboBoxItem> items)
        {
            base.Content(items);
            var blockList = new List<MyTerminalControlComboBoxItem>();
            
            if (ReferenceBlock == null)
                return;

            var config = ConfigManager.GetConfigForBlock(ReferenceBlock);

            if (config == null)
                return;

            _grids.Clear();

            var referenceGrid = ReferenceBlock.CubeGrid;

            MyAPIGateway.GridGroups.GetGroup(referenceGrid, GridLinkTypeEnum.Logical, _grids);

            var filter = "";
            if (SearchTextbox != null)
                filter = SearchTextbox.TextBuilder.ToString();

            blockList.AddRange(referenceGrid.GetFatBlocks<IMyAssembler>()
                .Where(c => IsValidBlock(c, ReferenceBlock, filter))
                .Select(a => ComboBoxItemHelper.GetOrComputeComboBoxItem(
                    a.DisplayNameText, a.EntityId)));

            foreach (var grid in _grids)
            {
                if (grid == ReferenceBlock.CubeGrid)
                    continue;

                blockList.AddRange(grid.GetFatBlocks<IMyAssembler>().Where(c => IsValidBlock(c, ReferenceBlock, filter))
                    .Select(a => ComboBoxItemHelper.GetOrComputeComboBoxItem(
                        $"@{a.DisplayNameText}@",
                        a.EntityId)));
            }
            
            blockList.Sort((a, b) => string.Compare(a.Value.String, b.Value.String, StringComparison.Ordinal));
            items.AddRange(blockList);
        }

        bool IsValidBlock(IMyAssembler block, IMyTerminalBlock referenceBlock, string filter = "")
        {
            return block != null &&
                   MyVisualScriptLogicProvider.IsConveyorConnected(block.Name, referenceBlock.Name) &&
                   (block.GetUserRelationToOwner(referenceBlock.OwnerId) <=
                    MyRelationsBetweenPlayerAndBlock.FactionShare &&
                    CanUseBlueprintFast(block) &&
                    (string.IsNullOrEmpty(filter) || block.CustomName == null ||
                     filter.Split(' ').All(a =>
                         block.CustomName.Split(' ')
                             .Any(b => b.StartsWith(a, StringComparison.InvariantCultureIgnoreCase))))
                    || block.EntityId.Equals(SelectedCache));
        }

        static bool CanUseBlueprintFast(IMyAssembler block)
        {
            var def = block.BlockDefinition.ToString();
            bool canUse;
            if (AssemblersAllowsSeeds.TryGetValue(def, out canUse))
                return canUse;

            canUse = Planter.SeedsBlueprints.Values.Any(block.CanUseBlueprint);
            AssemblersAllowsSeeds[def] = canUse;
            return canUse;
        }

        void Setter(IMyTerminalBlock b, long l)
        {
            var config = ConfigManager.GetConfigForBlock(b);

            if (config == null)
                return;

            config.Assembler = l;
            SelectedCache = l;

            ConfigManager.Sync(b, config);
            ConfigManager.GetInstanceForBlock(b).UpdateAssembler();
        }
    }
}