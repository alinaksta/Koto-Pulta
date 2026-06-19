namespace Game.Items
{
    /// <summary>
    /// Describes a pending item transfer between two containers.
    /// </summary>
    public readonly struct TransferRequest
    {
        /// <summary>
        /// Gets the container supplying the item.
        /// </summary>
        public IContainer Source { get; }

        /// <summary>
        /// Gets the container receiving the item.
        /// </summary>
        public IContainer Destination { get; }

        /// <summary>
        /// Gets the item being transferred.
        /// </summary>
        public Item Item { get; }

        /// <summary>
        /// Creates a transfer request for container validation.
        /// </summary>
        /// <param name="source">Container supplying the item.</param>
        /// <param name="destination">Container receiving the item.</param>
        /// <param name="item">Item being transferred.</param>
        public TransferRequest(IContainer source, IContainer destination, Item item)
        {
            Source = source;
            Destination = destination;
            Item = item;
        }
    }
}
