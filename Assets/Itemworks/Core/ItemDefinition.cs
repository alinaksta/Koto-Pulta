using System.Collections.Generic;
using System;

namespace Itemworks.Core
{
    public partial class ItemDefinition
    {
        public readonly string Id;
        public IReadOnlyList<ItemProperty> Properties => _properties;

        private readonly ItemProperty[] _properties;
        private readonly Dictionary<Type, ItemProperty> _propertyCache = new();

        public static ItemDefinitionBuilder NewBuilder(string itemName)
            => new ItemDefinitionBuilder { Id = itemName };

        public TProperty GetProperty<TProperty>() where TProperty : ItemProperty
        {
            if (_propertyCache.TryGetValue(typeof(TProperty), out var property))
                return (TProperty)property;
            return null;
        }

        public bool TryGetProperty<TProperty>(out TProperty property) where TProperty : ItemProperty
        {
            bool result = _propertyCache.TryGetValue(typeof(TProperty), out var value);
            property = (TProperty)value;
            return result;
        }

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