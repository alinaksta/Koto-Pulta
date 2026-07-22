using Itemworks.Core;

namespace Game.Items
{
    /// <summary>
    /// Interface that defines objects that can provide a random item.
    /// </summary>
    public interface IRandomItemDefinitionGiver
    {
        /// <summary>
        /// Provides a random <see cref="ItemDefinition"/>.
        /// </summary>
        /// <returns>Random <see cref="ItemDefinition"/></returns>
        ItemDefinition GetRandomItemDefinition();
    }
}
