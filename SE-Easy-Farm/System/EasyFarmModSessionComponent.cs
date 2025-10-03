using System;
using System.Collections.Generic;
using System.Linq;
using EasyFarming.Helpers;
using EasyFarming.Networking;
using EasyFarming.System.Config;
using EasyFarming.System.TerminalControls;
using EasyFarming.System.TerminalControls.Combobox;
using EasyFarming.System.TerminalControls.Listbox;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace EasyFarming.System
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class EasyFarmModSessionComponent : MySessionComponentBase
    {
        public static List<TerminalControlsWrapper> Controls = new List<TerminalControlsWrapper>();

        protected override void UnloadData()
        {
            if (MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.IsServer)
                return;

            MyAPIGateway.TerminalControls.CustomControlGetter -= CustomControlGetter;
            Controls.Clear();
            
            ListBoxItemHelper.PerTypeCache.Clear();

            ConfigManager.Close();
        }

        public override void BeforeStart()
        {
            try
            {
                ConfigManager.Init();
                ConfigManager.NetworkManager.OnReceivedPacket += OnReceivedPacket;

                if (!MyAPIGateway.Utilities.IsDedicated || !MyAPIGateway.Session.IsServer) 
                    InitUi(); // dedicated doesn't need UI;
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        void InitUi()
        {
            MyAPIGateway.TerminalControls.CustomControlGetter += CustomControlGetter;
                
            Controls.Add(new Separator());
            Controls.Add(new Label(Constants.NAME));
            Controls.Add(new SwitchToggleAuto());
                
            Controls.Add(new Label("DisplayName_BlockGroup_InputOutputGroup"));
            Controls.Add(new ComboBoxBlockInput());
            Controls.Add(new ComboBoxBlockOutput());
            Controls.Add(new ListboxAllowedItems());
            Controls.Add(new Separator());

            Controls.Add(new Label("DisplayName_BlockGroup_AutomationGroup"));
            Controls.Add(new ComboBoxAssembler());
            Controls.Add(new ComboBoxAirSensor());
            Controls.Add(new Separator());
                
            MyLog.Default.Log(MyLogSeverity.Info,
                $"{nameof(EasyFarming)}: Setting up Terminal Controls, {Controls.Count} Elements");
        }

        void OnReceivedPacket(ReceivedPacketEventArgs packetRaw)
        {
            try
            {
                if(!packetRaw.IsFromServer)
                    return; // some script kiddo tried to mess up with the settings
                
                switch (packetRaw.Code)
                {
                    case PackageCode.SyncConfig:
                    {
                        var packet = packetRaw.UnWrap<PacketSyncConfig>();
                        var block = MyEntities.GetEntityById(packet.BlockId) as IMyFunctionalBlock;

                        if (block == null)
                            return;

                        var settings = ConfigManager.GetConfigForBlock(block);

                        if (settings == null)
                            return;

                        settings.CopyFrom(packet.Config);
                        ConfigManager.Save(block, settings);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        void CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (controls == null)
                return;

            try
            {
                foreach (var comp in block.Components)
                {
                    if (!(comp is IMyFarmPlotLogic))
                        continue;

                    ComboboxWrapper.ReferenceBlock = block;
                    SetupFarmPlotTerminal(controls);
                    return;
                }
                
                MyLog.Default.Log(MyLogSeverity.Warning,$"{nameof(EasyFarming)}: No Farm Block Found!");
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, this);
            }
        }

        void SetupFarmPlotTerminal(List<IMyTerminalControl> controls)
        {
            controls.AddRange(Controls.Select(control => control.TerminalControl));
        }
    }
}