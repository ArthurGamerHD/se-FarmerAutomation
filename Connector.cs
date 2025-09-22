using System.Collections.Generic;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Ingame;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;


namespace FarmerAutomation
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ShipConnector), false)]
    public class ConnectorLogicComponent : MyGameLogicComponent
    {
        private const string ID_PREFIX = nameof(FarmerAutomation) + "_"; 
        static bool _initialized;
        
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            DoOnce();
        }

        public static void DoOnce()
        {
            if(_initialized)
                return;
            
            BuildTerminalControls();
            BuildActions();
            _initialized = true;
        }

        static void BuildTerminalControls()
        {
            var button = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyShipConnector>(ID_PREFIX + "DropOnceButton");
            button.Title = MyStringId.GetOrCompute("ToolTipTerminalInventory_ThrowOut");
            button.Tooltip = MyStringId.GetOrCompute("ToolTipTerminalInventory_ThrowOut");
            button.SupportsMultipleBlocks = true;
            button.Enabled = b => b.HasInventory && !b.GetInventory(0).Empty();

            button.Action = ThrowOutSingleItem;

            MyAPIGateway.TerminalControls.AddControl<IMyShipConnector>(button);
        }

        static void BuildActions()
        {
            {
                var a = MyAPIGateway.TerminalControls.CreateAction<IMyShipConnector>(ID_PREFIX + "DropOnceAction");

                a.Name = MyTexts.Get(MyStringId.GetOrCompute("ToolTipTerminalInventory_ThrowOut"));

                a.ValidForGroups = true;

                a.Icon = @"Textures\GUI\Icons\Actions\Start.dds";

                a.Action = ThrowOutSingleItem;
                
                a.Writer = (b, sb) =>
                {
                    sb.Append(b.GetInventory(0).Empty()
                        ? MyTexts.GetString("BlockPropertyProperties_WaterLevel_Empty")
                        : MyTexts.GetString("ScreenMedicals_RespawnShipReady"));
                };

                a.Enabled = b => true;

                MyAPIGateway.TerminalControls.AddAction<IMyShipConnector>(a);
            }
        }

        private static void ThrowOutSingleItem(IMyTerminalBlock obj) => ThrowOutSingleItem((IMyShipConnector)obj);

        public static void ThrowOutSingleItem(IMyShipConnector connector)
        {
            var inventory = connector.GetInventory(0);
            var items = new List<MyInventoryItem>();
            inventory.GetItems(items);
            if (!items.Any())
                return;
            
            var id = items.First().ItemId;
            var item = inventory.GetItemByID(id);
            if (item == null)
                return;
            
            var defId = item.Content.GetId();
            MyPhysicalItemDefinition def;
            if (!MyDefinitionManager.Static.TryGetPhysicalItemDefinition(defId, out def))
                return;
            
            MyFixedPoint dropAmount = 1;
            if (item.Amount < dropAmount)
                dropAmount = item.Amount;
            
            var content = (MyObjectBuilder_PhysicalObject)item.Content;
            var droppedItem = new MyPhysicalInventoryItem(dropAmount, content);
            
            inventory.RemoveItemsOfType(dropAmount, content);
            MatrixD wm = connector.WorldMatrix;
            
            float halfSize = def.Size.Max() * 0.5f;
            Vector3D spawnPos = connector.GetPosition() + wm.Forward * (connector.CubeGrid.GridSize * 0.5 + halfSize + 0.1);
            Vector3D forward = wm.Forward;
            Vector3D up = wm.Up;

            MyFloatingObjects.Spawn(droppedItem, spawnPos, forward, up, connector.CubeGrid?.Physics, entity =>
            {
                entity.Physics.LinearVelocity += (Vector3)(wm.Forward * 2f);
            });
        }
    }
}