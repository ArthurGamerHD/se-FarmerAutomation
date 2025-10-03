using System;
using System.Linq;
using EasyFarming.Helpers;
using EasyFarming.Networking;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace EasyFarming.System.Config
{
    /// <summary>
    /// Ensures settings is correctly Saved/Loaded and Synced between clients
    /// </summary>
    public static class ConfigManager
    {
        public static NetworkManager NetworkManager;

        public static void Init()
        {
            MyLog.Default.Log(MyLogSeverity.Info,
                $"{nameof(EasyFarming)}: Setting up Network Manager using port {Constants.PORT}");
            NetworkManager = new NetworkManager(Constants.PORT);
            NetworkManager.Register();
        }

        public static void Close()
        {
            MyLog.Default.Log(MyLogSeverity.Info, $"{nameof(EasyFarming)}: Closing Network Manager");
            NetworkManager?.Dispose();
            NetworkManager = null;
        }

        public static void SaveAll()
        {
            try
            {
                foreach (var planters in Planter.Instances)
                    Save(planters.Block, planters.Config);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, typeof(ConfigManager));
            }
        }

        public static void SyncAll()
        {
            try
            {
                foreach (var planters in Planter.Instances)
                    Sync((IMyEntity)planters.Block, planters.Config);
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, typeof(ConfigManager));
            }
        }

        public static void Save(IMyEntity storageEntity, FarmPlotConfig providerConfig)
        {
            try
            {
                if (storageEntity.Storage == null)
                    return;

                var base64 = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(providerConfig));

                if (string.IsNullOrEmpty(base64))
                    throw new Exception("Invalid storage config");

                storageEntity.Storage[Constants.STORAGE_GUID] = base64;
            }
            catch (Exception e)
            {
                ErrorHandlerHelper.LogError(e, typeof(ConfigManager));
            }
        }

        public static void Sync(IMyEntity storageEntity, FarmPlotConfig providerConfig)
        {
            NetworkManager.TransmitToServer(new PacketSyncConfig(storageEntity.EntityId, providerConfig));
            Save(storageEntity, providerConfig);
        }

        public static void Sync(IMyTerminalBlock storageEntity) =>
            Sync(storageEntity, GetConfigForBlock(storageEntity));

        public static void LoadSettings(IMyTerminalBlock block, ref FarmPlotConfig config)
        {
            try
            {
                config = GetConfigForBlock(block);
                
                if(config != null)
                    return;
                
                var storageEntity = (IMyEntity)block;

                if (storageEntity.Storage == null)
                    storageEntity.Storage = new MyModStorageComponent();

                config = TryLoad(block);
                if (config != null)
                    return;

                CreateSettings(block, out config);
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification(
                    $"Fail to Load Settings for block {block.DisplayNameText}\n{e.Message}");
                ErrorHandlerHelper.LogError(e, typeof(ConfigManager));
                CreateSettings(block, out config);
            }
        }

        public static FarmPlotConfig TryLoad(IMyCubeBlock block)
        {
            string value;
            if (block.Storage.TryGetValue(Constants.STORAGE_GUID, out value) && !string.IsNullOrEmpty(value))
            {
                var settings = MyAPIGateway.Utilities.SerializeFromBinary<FarmPlotConfig>(Convert.FromBase64String(value));

                if (settings.ParentGrid != block.CubeGrid.EntityId)
                    settings.ParentGrid = block.CubeGrid.EntityId;

                return settings;
            }

            return null;
        }

        public static void CreateSettings(IMyTerminalBlock block, out FarmPlotConfig provider)
        {
            provider = CreateSettings(block);
            Save(block, provider);
        }

        public static FarmPlotConfig CreateSettings(IMyTerminalBlock block) => new FarmPlotConfig(block);

        public static Planter GetInstanceForBlock(IMyTerminalBlock block) => Planter.Instances.FirstOrDefault(a => a.Block.Equals(block));

        public static FarmPlotConfig GetConfigForBlock(IMyTerminalBlock block) => GetInstanceForBlock(block)?.Config;
    }
}