using Itemworks.Core;

namespace Game.Items
{
    public struct Item
    {
        public ItemDefinition Definition;
        public ItemInstance Instance;

        public bool IsInstance => Instance != null;
        
        public Item(ItemDefinition definition) 
        { 
            Definition = definition; 
            Instance = null; 
        }

        public Item(ItemInstance instance)
        {
            Instance = instance;
            Definition = instance.Definition;
        }

        public bool TryGetComponent<TComponent>(out TComponent component) where TComponent : ItemComponent
        {
            if (Instance != null)
                return Instance.TryGetComponent(out component);

            component = null;
            return false;
        }

        public TComponent GetComponent<TComponent>() where TComponent : ItemComponent
            => Instance != null ? Instance.GetComponent<TComponent>() : null;

        public static Item? FromId(string id)
        {
            if (ItemRegistry.Instance.TryGet(id, out var definition))
                return new Item(definition);

            return null;
        }
    }
}
