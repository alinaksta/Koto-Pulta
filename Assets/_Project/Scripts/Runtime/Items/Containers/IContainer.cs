using System;

namespace Game.Items
{
    /// <summary>
    /// Stores a single item and participates in transfers.
    /// </summary>
    public interface IContainer
    {
        /// <summary>
        /// Gets the currently stored item, if any.
        /// </summary>
        Item? Item { get; }

        /// <summary>
        /// Gets whether the container currently has no item.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Raised after the stored item changes.
        /// </summary>
        event Action<Item?> OnItemChanged;

        /// <summary>
        /// Checks whether this container can remove its current item for a transfer.
        /// </summary>
        /// <param name="request">Transfer being evaluated.</param>
        /// <returns><see langword="true"/> when removal is allowed.</returns>
        bool CanRemove(in TransferRequest request);

        /// <summary>
        /// Checks whether this container can receive an item for a transfer.
        /// </summary>
        /// <param name="request">Transfer being evaluated.</param>
        /// <returns><see langword="true"/> when insertion is allowed.</returns>
        bool CanInsert(in TransferRequest request);

        /// <summary>
        /// Inserts an item into the container.
        /// </summary>
        /// <param name="item">Item to store.</param>
        void Insert(Item item);

        /// <summary>
        /// Removes and returns the current item.
        /// </summary>
        /// <returns>The removed item, or <see langword="null"/> when empty.</returns>
        Item? Remove();
    }

    /// <summary>
    /// Exposes a container owned by another object.
    /// </summary>
    public interface IContainerHolder
    {
        /// <summary>
        /// Gets the owned container.
        /// </summary>
        public IContainer Container { get; }
    }
}
