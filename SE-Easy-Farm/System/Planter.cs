using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyFarming.Extensions;
using EasyFarming.Helpers;
using EasyFarming.System.Config;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using IMyInventory = VRage.Game.ModAPI.IMyInventory;

namespace EasyFarming.System
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_FunctionalBlock), useEntityUpdate: true)]
    public class Planter : MyGameLogicComponent
    {
        public static Dictionary<MyDefinitionId, MyBlueprintDefinitionBase> SeedsBlueprints;

        public static readonly List<Planter> Instances = new List<Planter>();

        public FarmPlotConfig Config;

        public IMyFunctionalBlock Block;
        public IMyAirVent AirVent;
        public IMyAssembler Assembler;
        IMyFarmPlotLogic _planterComponent;
        IMyResourceStorageComponent _storageComponent;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            var block = Entity as IMyFunctionalBlock;

            if (block == null)
                return;

            foreach (var component in block.Components)
            {
                if (_planterComponent == null)
                    _planterComponent = component as IMyFarmPlotLogic;
                if (_storageComponent == null)
                    _storageComponent = component as IMyResourceStorageComponent;
                if (_planterComponent != null && _storageComponent != null)
                    break;
            }

            if (_planterComponent == null || _storageComponent == null)
                return;

            Instances.Add(this);
            Block = block;

            ConfigManager.LoadSettings(Block, ref Config);
            
            if (MyAPIGateway.Session.IsServer)
            {
                NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
                
                if (SeedsBlueprints == null)
                {
                    SeedsBlueprints = new Dictionary<MyDefinitionId, MyBlueprintDefinitionBase>();

                    var seedBlueprints = MyDefinitionManager.Static.GetBlueprintDefinitions()
                        .Where(a => a.Results
                            .Any(b => b.Id.TypeId.ToString().EndsWith("_SeedItem")));

                    foreach (var blueprintDefinition in seedBlueprints)
                    {
                        var seed = blueprintDefinition.Results.First(b => b.Id.TypeId.ToString().EndsWith("_SeedItem"));
                        SeedsBlueprints.Add(seed.Id, blueprintDefinition);
                    }
                }
                
                
                MyLog.Default.Log(MyLogSeverity.Debug, $"{nameof(EasyFarming)}: Found planter block");
            }
            else
            {
                UpdateAssembler();
                UpdateAirVent();
            }

            Block.AppendingCustomInfo += OnBlockOnAppendingCustomInfo;
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if(!MyAPIGateway.Session.IsServer)
            {
                MyLog.Default.Log(MyLogSeverity.Error, "Cannot update planter block on Client");
                return;
            }
            
            if (!Config.AutomationEnabled && Config.SelectedItems.Length != 0)
                return;

            if (_planterComponent.IsPlantPlanted && !_planterComponent.IsAlive)
                _planterComponent.RemovePlant();

            if (_planterComponent.IsHarvestable)
                TryHarvest();

            if (CanPlant())
                TryPlant();

            Block.RefreshCustomInfo();
        }

        void OnBlockOnAppendingCustomInfo(IMyTerminalBlock terminalBlock, StringBuilder builder)
        {
            if (!Config.AutomationEnabled)
                return;

            builder.AppendLine();
            builder.AppendLine(Constants.NAME);
            builder.AppendLine();

            bool hasIssues = false;

            if (_storageComponent.FilledRatio < .5f)
            {
                builder.AppendError($"{MyTexts.GetString("BlockPropertyProperties_CauseOfDeath_LowWater")}");
                hasIssues = true;
            }
            
            if (!Config.SelectedItems.Any())
            {
                builder.AppendWarning(
                    $"{MyTexts.GetString("AIBlocks_HudMessage_Error")} - {MyTexts.GetString("AssemblerState_MissingItems")}",
                    $"\n   {MyTexts.GetString("ScreenDebugSpawnMenu_ItemType")}");
                hasIssues = true;
            }

            UpdateAirVent();
            UpdateAssembler();

            if (AirVent == null || Assembler == null)
            {
                builder.AppendWarning(
                    $"{MyTexts.GetString("AIBlocks_HudMessage_Error")} - {MyTexts.GetString("MissingBlock")}:", "");

                if (Assembler == null)
                    builder.AppendLine($"   {MyTexts.GetString("DisplayName_Block_FoodProcessor")}");

                if (AirVent == null)
                    builder.AppendLine($"   {MyTexts.GetString("DisplayName_Block_AirVent")}");

                hasIssues = true;
            }

            if (AirVent != null)
            {
                if (!AirVent.CanPressurize)
                {
                    if (AirVent.GetOxygenLevel() >= .5)
                    {
                        builder.AppendWarning(MyTexts.GetString("BlockPropertyProperties_CauseOfDeath_Exposure"));
                    }
                    else
                    {
                        builder.AppendError(MyTexts.GetString("BlockPropertyProperties_CauseOfDeath_Exposure"));
                    }

                    hasIssues = true;
                }

                if (AirVent.GetOxygenLevel() < .5)
                {
                    builder.AppendError(MyTexts.GetString("NotificationOxygenCritical"));
                    hasIssues = true;
                }

                if (AirVent.GetOxygenLevel() < .75)
                {
                    builder.AppendWarning(MyTexts.GetString("NotificationOxygenLow"));
                    hasIssues = true;
                }
            }

            if (!Block.IsFunctional)
            {
                builder.AppendError(MyTexts.GetString("BlockPropertyProperties_CauseOfDeath_Damaged"));
                hasIssues = true;
            }

            if (!hasIssues && !_planterComponent.IsPlantPlanted)
            {
                builder.AppendLine($"[Color=#FFFF7F00]{MyTexts.GetString("AssemblerState_MissingItems")}[/Color]");
                hasIssues = true;
            }

            if (hasIssues) 
                return;

            var functional = MyTexts.GetString("Functional").ToLower();
            builder.AppendLine(functional[0].ToString().ToUpper() + functional.Substring(1));
        }

        public bool CanPlant()
        {
            var valid = ((!_planterComponent.IsAlive || !_planterComponent.IsPlantPlanted) &&
                         _storageComponent.FilledRatio > .5f);


            if (!valid || Config.AirSensor == null)
                return valid;

            UpdateAirVent();
            return AirVent == null || AirVent.CanPressurize && AirVent.GetOxygenLevel() > .75;
        }

        public void TryHarvest()
        {
            if (!string.IsNullOrEmpty(Config.OutputGroup))
            {
                bool resolved = false;

                MyAPIGateway.TerminalActionsHelper?
                    .GetTerminalSystemForGrid(Block.CubeGrid)?
                    .GetBlockGroupWithName(Config.OutputGroup)?
                    .GetBlocks(null, block =>
                    {
                        if (block.HasInventory && !resolved)
                            resolved = TryHarvestToBlock(block);

                        return false;
                    });
            }
            else if (Config.OutputBlock != null)
            {
                var block = MyAPIGateway.Entities.GetEntityById(Config.InputBlock) as IMyTerminalBlock;
                if (block != null && block.HasInventory)
                    TryHarvestToBlock(block);
            }
        }

        bool TryHarvestToBlock(IMyTerminalBlock block)
        {
            try
            {
                var inventory = block.GetInventory();
                if (inventory == null || !inventory.CanItemsBeAdded(_planterComponent.OutputItemAmount,
                                          _planterComponent.OutputItem)
                                      || !block.GetInventory()
                                          .CanTransferItemTo(inventory, _planterComponent.OutputItem)) return false;

                _planterComponent.Harvest(inventory, true);

                return !_planterComponent.IsPlantPlanted;
            }
            catch (Exception e)
            {
                // bad game, receive no cookie
                MyLog.Default.Log(MyLogSeverity.Error, $"{nameof(EasyFarming)}: Error on defining Output amount {{0}}",
                    e.ToString());
                return false;
            }
        }

        public void TryPlant()
        {
            if (!string.IsNullOrEmpty(Config.InputGroup))
            {
                bool resolved = false;

                MyAPIGateway.TerminalActionsHelper?
                    .GetTerminalSystemForGrid(Block.CubeGrid)?
                    .GetBlockGroupWithName(Config.InputGroup)?
                    .GetBlocks(null, block =>
                    {
                        if (block.HasInventory && !resolved)
                            resolved = TryPlantFromBlock(block);
                        return false;
                    });
            }
            else if (Config.InputBlock != null)
            {
                var block = MyAPIGateway.Entities.GetEntityById(Config.InputBlock) as IMyTerminalBlock;
                if (block != null && block.HasInventory)
                    TryPlantFromBlock(block);
            }
        }

        public bool TryPlantFromBlock(IMyTerminalBlock block)
        {
            var reference = block.GetInventory();
            var seed = Config.SelectedItems.FirstOrDefault();

            var production = block as IMyProductionBlock;

            for (int i = 0; i < block.InventoryCount; i++)
            {
                var inventory = block.GetInventory(i);

                if (production?.InputInventory == inventory) // skip first slot for assembler/refinery/food-processor
                    continue;

                if (reference == null || inventory == null)
                    continue;

                var canTransfer = inventory.CanTransferItemTo(reference, seed);

                if (!canTransfer)
                    continue;

                TryPlantFromInventory(inventory);
                return _planterComponent.IsPlantPlanted;
            }

            return false;
        }

        public void TryPlantFromInventory(IMyInventory block)
        {
            foreach (var item in Config.SelectedItems)
            {
                var inventoryItem = block.FindItem(item);
                if (inventoryItem == null)
                    continue;

                if (inventoryItem.Amount < _planterComponent.AmountOfSeedsRequired)
                    continue;

                inventoryItem.Amount -= _planterComponent.AmountOfSeedsRequired;
                var def = inventoryItem.GetDefinitionId();
                _planterComponent.PlantSeed(def);
                TryProduceMoreSeeds(def);
            }
        }

        public void TryProduceMoreSeeds(MyDefinitionId def)
        {
            UpdateAssembler();
            Assembler?.AddQueueItem(SeedsBlueprints[def], _planterComponent.AmountOfSeedsRequired);
        }

        public void UpdateAssembler()
        {
            Assembler = null;
            if (!Config.Assembler.HasValue)
                return;

            var entity = MyAPIGateway.Entities.GetEntityById(Config.Assembler) as IMyAssembler;
            if (entity != null && entity.IsInSameLogicalGroupAs(Block) &&
                entity.GetUserRelationToOwner(Block.OwnerId) <= MyRelationsBetweenPlayerAndBlock.FactionShare)
                Assembler = entity;
        }

        public void UpdateAirVent()
        {
            AirVent = null;

            if (!Config.AirSensor.HasValue)
                return;

            var entity = MyAPIGateway.Entities.GetEntityById(Config.AirSensor) as IMyAirVent;
            if (entity != null && entity.CubeGrid == Block.CubeGrid &&
                entity.GetUserRelationToOwner(Block.OwnerId) <= MyRelationsBetweenPlayerAndBlock.FactionShare)
                AirVent = entity;
        }

        public void UpdateAutomation()
        {
            Block.RefreshCustomInfo();
        }
    }
}