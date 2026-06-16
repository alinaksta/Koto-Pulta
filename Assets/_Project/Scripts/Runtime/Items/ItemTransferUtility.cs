namespace Game.Items
{
    public static class ItemTransferUtility
    {
        public enum TransferResult
        {
            Success,

            InvalidContainer,
            SameContainer,
            SourceEmpty,

            SourceRejected,
            DestinationRejected
        }

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