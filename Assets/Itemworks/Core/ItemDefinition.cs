using System.Collections.Generic;
using System;

namespace Itemworks.Core
{
    /// <summary>
    /// Immutable item definition built from an id and property set.
    /// </summary>
    public partial class ItemDefinition
    {
        /// <summary>
        /// Gets the registry id for this item definition.
        /// </summary>
        public readonly string Id;

        /// <summary>
        /// Gets the properties assigned to this definition.
        /// </summary>
        public IReadOnlyList<ItemProperty> Properties => _properties;

        private readonly ItemProperty[] _properties;
        private readonly Dictionary<Type, ItemProperty> _propertyCache = new();

        /// <summary>
        /// Creates a builder preconfigured with the provided item id.
        /// </summary>
        /// <param name="itemName">Id to assign to the new builder.</param>
        /// <returns>A builder for assembling an item definition.</returns>
        public static ItemDefinitionBuilder NewBuilder(string itemName)
            => new ItemDefinitionBuilder { Id = itemName };

        /// <summary>
        /// Gets a property by its exact runtime type.
        /// </summary>
        /// <typeparam name="TProperty">Property type to retrieve.</typeparam>
        /// <returns>The matching property, or <see langword="null"/> when absent.</returns>
        public TProperty GetProperty<TProperty>() where TProperty : ItemProperty
        {
            if (_propertyCache.TryGetValue(typeof(TProperty), out var property))
                return (TProperty)property;
            return null;
        }

        /// <summary>
        /// Tries to get a property by its exact runtime type.
        /// </summary>
        /// <typeparam name="TProperty">Property type to retrieve.</typeparam>
        /// <param name="property">Receives the matching property when found.</param>
        /// <returns><see langword="true"/> when the property exists.</returns>
        public bool TryGetProperty<TProperty>(out TProperty property) where TProperty : ItemProperty
        {
            bool result = _propertyCache.TryGetValue(typeof(TProperty), out var value);
            property = (TProperty)value;
            return result;
        }

        /// <summary>
        /// Checks whether a property of the requested type exists.
        /// </summary>
        /// <typeparam name="TProperty">Property type to check.</typeparam>
        /// <returns><see langword="true"/> when the property exists.</returns>
        public bool ContainsProperty<TProperty>()
            => _propertyCache.ContainsKey(typeof(TProperty));

        private void BuildCache()
        {
            _propertyCache.Clear();

            foreach (var property in _properties)
                if (property != null)
                    _propertyCache[property.GetType()] = property;
        }
    }
}
