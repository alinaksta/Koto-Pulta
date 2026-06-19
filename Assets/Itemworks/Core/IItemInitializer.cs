namespace Itemworks.Core
{
    /// <summary>
    /// Initializes a newly created <see cref="ItemInstance"/>.
    /// </summary>
    public interface IItemInitializer
    {
        /// <summary>
        /// Runs after an <see cref="ItemInstance"/> is created from its definition.
        /// </summary>
        /// <param name="instance">Instance being initialized.</param>
        void OnInstanceCreated(ItemInstance instance);
    }
}
