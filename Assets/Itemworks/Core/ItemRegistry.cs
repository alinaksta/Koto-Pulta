using System.Collections.Generic;

namespace Itemworks.Core
{
    public class ItemRegistry
    {
        public static ItemRegistry Instance => _instance ??= new ItemRegistry();
        private static ItemRegistry _instance;

        public ILogger Logger { get; set; } = new NullLogger();

        private readonly Dictionary<string, ItemDefinition> items = new();
        public IReadOnlyDictionary<string, ItemDefinition> Items => items;

        public void RegisterItem(ItemDefinition definition)
        {
            if (items.ContainsKey(definition.Id))
            {
                Logger.LogWarning($"Item '{definition.Id}' is already registered. Skipping.");
                return;
            }

            Logger.Log($"Item '{definition.Id}' was successfully registered.");
            items[definition.Id] = definition;
        }

        public ItemDefinition Get(string fullId)
        {
            items.TryGetValue(fullId, out var definition);
            return definition;
        }

        public bool TryGet(string fullId, out ItemDefinition definition)
            => items.TryGetValue(fullId, out definition);

        public void Clear()
            => items.Clear();

        public static bool IsValidId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            foreach (char c in id)
                if (!char.IsLower(c) && !char.IsDigit(c) && c != '_' && c != ':')
                    return false;

            return true;
        }
    }
}