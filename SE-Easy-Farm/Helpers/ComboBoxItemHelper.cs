using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.ModAPI;
using VRage.Utils;

namespace EasyFarming.Helpers
{
    public static class ComboBoxItemHelper
    {
        static Random rng = new Random();

        /// <summary>
        /// Per type Cache, can be <see cref="long"/> for Blocks, <see cref="string"/>. For Groups or Item Category, <see cref="MyDefinitionId"/> for Items, or others
        /// </summary>
        public static readonly Dictionary<long, MyTerminalControlComboBoxItem> Cache =
            new Dictionary<long, MyTerminalControlComboBoxItem>();

        public static readonly Dictionary<string, long> Groups = new Dictionary<string, long>();

        public static MyTerminalControlComboBoxItem GetOrComputeComboBoxItem(string text, long item)
        {
            MyTerminalControlComboBoxItem listBoxItem;

            // hack: Combobox is NOT intended to be used with anything else than entity id
            if (item == -1 && !Groups.TryGetValue(text, out item))
            {
                item = ((long)rng.Next() << 32) + rng.Next();
                Groups[text] = item;
            }

            if (Cache.TryGetValue(item, out listBoxItem))
            {
                listBoxItem.Value = MyStringId.GetOrCompute(text);
                return listBoxItem;
            }

            listBoxItem = new MyTerminalControlComboBoxItem
            {
                Value = MyStringId.GetOrCompute(text),
                Key = item
            };

            Cache[item] = listBoxItem;
            return listBoxItem;
        }

        public static bool TryGetComboBoxItem(long item, out MyTerminalControlComboBoxItem listBoxItem) =>
            Cache.TryGetValue(item, out listBoxItem);


        // hack: Combobox is NOT intended to be used with anything else than entity id
        public static bool TryGetGroupId(string key, out long value) => Groups.TryGetValue(key, out value);

        // hack: Combobox is NOT intended to be used with anything else than entity id
        public static bool TryGetGroupName(long value, out string key)
        {
            var keypair = Groups.ToArray().FirstOrDefault(a => a.Value == value);
            key = keypair.Key;
            return !string.IsNullOrEmpty(key);
        }
    }
}