using Itemworks.Core;

namespace Game.Items
{
    /// <summary>
    /// Represents an item definition and its optional runtime instance.
    /// </summary>
    public struct Item
    {
        /// <summary>
        /// Gets or sets the item definition backing this item.
        /// </summary>
        public ItemDefinition Definition;

        /// <summary>
        /// Gets or sets the runtime instance attached to this item, when one exists.
        /// </summary>
        public ItemInstance Instance;

        /// <summary>
        /// Gets whether this item has a runtime instance.
        /// </summary>
        public bool IsInstance => Instance != null;
        
        /// <summary>
        /// Creates an item from a definition only.
        /// </summary>
        /// <param name="definition">Definition to wrap.</param>
        public Item(ItemDefinition definition) 
        { 
            Definition = definition; 
            Instance = null; 
        }

        /// <summary>
        /// Creates an item from an existing runtime instance.
        /// </summary>
        /// <param name="instance">Runtime instance to wrap.</param>
        public Item(ItemInstance instance)
        {
            Instance = instance;
            Definition = instance.Definition;
        }

        /// <summary>
        /// Tries to get a runtime component from the item instance.
        /// </summary>
        /// <typeparam name="TComponent">Component type to retrieve.</typeparam>
        /// <param name="component">Receives the component when found.</param>
        /// <returns><see langword="true"/> when the item has an instance with the component.</returns>
        public bool TryGetComponent<TComponent>(out TComponent component) where TComponent : ItemComponent
        {
            if (Instance != null)
                return Instance.TryGetComponent(out component);

            component = null;
            return false;
        }

        /// <summary>
        /// Gets a runtime component from the item instance.
        /// </summary>
        /// <typeparam name="TComponent">Component type to retrieve.</typeparam>
        /// <returns>The component, or <see langword="null"/> when unavailable.</returns>
        public TComponent GetComponent<TComponent>() where TComponent : ItemComponent
            => Instance != null ? Instance.GetComponent<TComponent>() : null;

        /// <summary>
        /// Creates an item from a registered definition id.
        /// </summary>
        /// <param name="id">Definition id to resolve.</param>
        /// <returns>The created item, or <see langword="null"/> when the id is not registered.</returns>
        public static Item? FromId(string id)
        {
            if (ItemRegistry.Instance.TryGet(id, out var definition))
                return new Item(definition);

            return null;
        }
    }
}
