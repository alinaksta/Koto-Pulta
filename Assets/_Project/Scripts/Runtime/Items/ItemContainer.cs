using System;

namespace Game.Items
{
    public class ItemContainer : IContainer
    {
        private Item? _item = null;

        public Item? Item => _item;
        public bool IsEmpty => !_item.HasValue;

        public event Action<Item?> OnItemChanged = delegate { };

        public bool CanInsert(in TransferRequest request) => IsEmpty;
        public bool CanRemove(in TransferRequest request) => !IsEmpty;

        public void Insert(Item item)
        {
            _item = item;
            OnItemChanged.Invoke(_item);
        }

        public void Remove(Item item)
        {
            _item = null;
            OnItemChanged.Invoke(_item);
        }
    }
}