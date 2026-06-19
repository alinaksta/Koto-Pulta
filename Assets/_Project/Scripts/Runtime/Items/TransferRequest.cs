namespace Game.Items
{
    public readonly struct TransferRequest
    {
        public IContainer Source { get; }
        public IContainer Destination { get; }
        public Item Item { get; }

        public TransferRequest(IContainer source, IContainer destination, Item item)
        {
            Source = source;
            Destination = destination;
            Item = item;
        }
    }
}