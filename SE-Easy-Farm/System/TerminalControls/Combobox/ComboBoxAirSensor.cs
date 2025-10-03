using System.Collections.Generic;
using System.Linq;
using EasyFarming.Helpers;
using EasyFarming.System.Config;
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
    public sealed class ComboBoxAirSensor : ComboboxWrapper
    {
        public override IMyTerminalControl TerminalControl { get; }
        public ComboBoxAirSensor()
        {
            var combobox = CreateControl<IMyTerminalControlCombobox>("AirSensor");
            combobox.ComboBoxContent = Content;
            combobox.Getter = Getter;
            combobox.Setter = Setter;
            combobox.Visible = Visible;
            combobox.Title = MyStringId.GetOrCompute("DisplayName_Block_AirVent");
            TerminalControl = combobox;
        }

        long Getter(IMyTerminalBlock arg)
        {
            var settings = ConfigManager.GetConfigForBlock(ReferenceBlock);

            if (settings == null)
                return -1;

            string group;
            long? block;

            block = settings.AirSensor;
            if (block != null)
            {
                return block.Value;
            }

            return -1;
        }

        void Content(List<MyTerminalControlComboBoxItem> blockList)
        {
            if (ReferenceBlock == null)
                return;

            blockList.AddRange(
                ReferenceBlock.CubeGrid.GetFatBlocks<IMyAirVent>()
                .Where(c => IsValidBlock(c, ReferenceBlock))
                .Select(a => ComboBoxItemHelper.GetOrComputeComboBoxItem(a.DisplayNameText, a.EntityId)));
        }

        bool IsValidBlock(IMyTerminalBlock block, IMyTerminalBlock referenceBlock)
        {
            return block.GetUserRelationToOwner(referenceBlock.OwnerId) <= MyRelationsBetweenPlayerAndBlock.FactionShare;
        }

        void Setter(IMyTerminalBlock b, long l)
        {
            var config = ConfigManager.GetConfigForBlock(b);

            if (config == null)
                return;
            
            config.AirSensor = l;

            ConfigManager.Sync(b, config);
            ConfigManager.GetInstanceForBlock(b).UpdateAirVent();
        }
    }
}