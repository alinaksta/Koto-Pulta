using System.Collections.Generic;

namespace Itemworks.Core
{
    /// <summary>
    /// Stores item definitions by id for runtime lookup.
    /// </summary>
    public class ItemRegistry
    {
        /// <summary>
        /// Gets the shared item registry instance.
        /// </summary>
        public static ItemRegistry Instance => _instance ??= new ItemRegistry();
        private static ItemRegistry _instance;

        /// <summary>
        /// Gets or sets the logger used by registry operations.
        /// </summary>
        public ILogger Logger { get; set; } = new NullLogger();

        private readonly Dictionary<string, ItemDefinition> items = new();

        /// <summary>
        /// Gets the currently registered item definitions.
        /// </summary>
        public IReadOnlyDictionary<string, ItemDefinition> Items => items;

        /// <summary>
        /// Registers an item definition if its id is not already present.
        /// </summary>
        /// <param name="definition">Definition to register.</param>
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

        /// <summary>
        /// Gets a registered definition by its full id.
        /// </summary>
        /// <param name="fullId">Item id to look up.</param>
        /// <returns>The registered definition, or <see langword="null"/> when missing.</returns>
        public ItemDefinition Get(string fullId)
        {
            items.TryGetValue(fullId, out var definition);
            return definition;
        }

        /// <summary>
        /// Tries to get a registered definition by its full id.
        /// </summary>
        /// <param name="fullId">Item id to look up.</param>
        /// <param name="definition">Receives the registered definition when found.</param>
        /// <returns><see langword="true"/> when the id is registered.</returns>
        public bool TryGet(string fullId, out ItemDefinition definition)
            => items.TryGetValue(fullId, out definition);

        /// <summary>
        /// Removes all registered item definitions.
        /// </summary>
        public void Clear()
            => items.Clear();

        /// <summary>
        /// Checks whether an item id only uses supported registry characters.
        /// </summary>
        /// <param name="id">Id to validate.</param>
        /// <returns><see langword="true"/> when the id is non-empty and uses lowercase letters, digits, underscores, or colons.</returns>
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
