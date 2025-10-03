using System.Collections.Generic;
using System.Linq;
using EasyFarming.Helpers;
using EasyFarming.System.Config;
using Sandbox.Definitions;
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
        static Dictionary<string, bool> AssemblersAllowsSeeds = new Dictionary<string, bool>();

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
                return -1;

            long? block;

            block = settings.Assembler;
            if (block != null)
                return block.Value;

            return -1;
        }

        protected void Content(List<MyTerminalControlComboBoxItem> blockList)
        {
            if (ReferenceBlock == null)
                return;

            var config = ConfigManager.GetConfigForBlock(ReferenceBlock);

            if (config == null)
                return;

            _grids.Clear();

            var referenceGrid = ReferenceBlock.CubeGrid;

            MyAPIGateway.GridGroups.GetGroup(referenceGrid, GridLinkTypeEnum.Logical, _grids);

            blockList.AddRange(referenceGrid.GetFatBlocks<IMyAssembler>()
                .Where(c => IsValidBlock(c, ReferenceBlock))
                .Select(a => ComboBoxItemHelper.GetOrComputeComboBoxItem(
                    a.DisplayNameText, a.EntityId)));

            foreach (var grid in _grids)
            {
                if (grid == ReferenceBlock.CubeGrid)
                    continue;

                blockList.AddRange(grid.GetFatBlocks<IMyAssembler>().Where(c => IsValidBlock(c, ReferenceBlock))
                    .Select(a => ComboBoxItemHelper.GetOrComputeComboBoxItem(
                        $"@{a.DisplayNameText}@",
                        a.EntityId)));
            }
        }

        static bool IsValidBlock(IMyAssembler block, IMyTerminalBlock referenceBlock)
        {
            return block.GetUserRelationToOwner(referenceBlock.OwnerId) <=
                   MyRelationsBetweenPlayerAndBlock.FactionShare
                   && CanUseBlueprintFast(block);
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

            ConfigManager.Sync(b, config);
            ConfigManager.GetInstanceForBlock(b).UpdateAssembler();
        }
    }
}