namespace Game.Items
{
    /// <summary>
    /// Performs validated transfers between item containers.
    /// </summary>
    public static class ItemTransferUtility
    {
        /// <summary>
        /// Describes the outcome of a transfer attempt.
        /// </summary>
        public enum TransferResult
        {
            Success,

            InvalidContainer,
            SameContainer,
            SourceEmpty,

            SourceRejected,
            DestinationRejected
        }

        /// <summary>
        /// Tries to move the current item from one container into another.
        /// </summary>
        /// <param name="source">Container providing the item.</param>
        /// <param name="destination">Container receiving the item.</param>
        /// <returns>The result of the transfer attempt.</returns>
        public static TransferResult TryTransfer(IContainer source, IContainer destination)
        {
            if (source == null || destination == null) return TransferResult.InvalidContainer;

            if (ReferenceEquals(source, destination)) return TransferResult.SameContainer;

            if (source.IsEmpty)
                return TransferResult.SourceEmpty;

            var item = source.Item.Value;
            var request = new TransferRequest(source, destination, item);

            if (!source.CanRemove(request))
                return TransferResult.SourceRejected;

            if (!destination.CanInsert(request))
                return TransferResult.DestinationRejected;

            var removed = source.Remove();
            destination.Insert(removed.Value);
            return TransferResult.Success;
        }
    }
}
