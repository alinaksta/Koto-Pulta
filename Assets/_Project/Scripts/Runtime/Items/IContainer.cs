using System;

namespace Game.Items
{
    public interface IContainer
    {
        Item? Item { get; }
        bool IsEmpty { get; }

        event Action<Item?> OnItemChanged;

        bool CanRemove(in TransferRequest request);
        bool CanInsert(in TransferRequest request);

        void Insert(Item item);
        void Remove(Item item);
    }

    public interface IContainerHolder
    {
        public IContainer Container { get; }
    }
}