using System;

namespace Game.Items
{
    /// <summary>
    /// Basic single-item container with no extra transfer rules.
    /// </summary>
    public class ItemContainer : IContainer
    {
        private Item? _item = null;

        /// <inheritdoc/>
        public Item? Item => _item;

        /// <inheritdoc/>
        public bool IsEmpty => !_item.HasValue;

        /// <inheritdoc/>
        public event Action<Item?> OnItemChanged = delegate { };

        /// <inheritdoc/>
        public bool CanInsert(in TransferRequest request) => IsEmpty;

        /// <inheritdoc/>
        public bool CanRemove(in TransferRequest request) => !IsEmpty;

        /// <inheritdoc/>
        public void Insert(Item item)
        {
            _item = item;
            OnItemChanged.Invoke(_item);
        }

        /// <inheritdoc/>
        public Item? Remove()
        {
            var removed = _item;
            _item = null;
            OnItemChanged.Invoke(_item);
            return removed;
        }
    }
}
