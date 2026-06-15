using Itemworks.Core;

namespace Game.Items
{
    public struct Item
    {
        public ItemDefinition Definition;
        
        public Item(ItemDefinition definition) { Definition = definition; }

        public static Item? FromId(string id)
        {
            if (ItemRegistry.Instance.TryGet(id, out var definition))
                return new Item(definition);

            return null;
        }
    }
}