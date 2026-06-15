using System;
using System.Collections.Generic;
using System.Linq;

namespace Itemworks.Core
{
    public partial class ItemDefinition
    {
        private ItemDefinition(string id, ItemProperty[] properties)
        {
            Id = id;
            _properties = properties;
        }

        public ItemDefinitionBuilder ToBuilder()
            => new ItemDefinitionBuilder { Id = Id, Properties = Properties.ToList()};

        [Serializable]
        public sealed class ItemDefinitionBuilder
        {
            public string Id;
            public List<ItemProperty> Properties = new();

            public ItemDefinitionBuilder AddProperty(ItemProperty property) { 
                if (!Properties.Contains(property))
                    Properties.Add(property);
                return this;
            }

            public ItemDefinitionBuilder RemoveProperty(ItemProperty property)
            {
                if (Properties.Contains(property))
                    Properties.Remove(property);
                return this;
            }

            public ItemDefinition Build()
            {
                var item = new ItemDefinition(Id, Properties.ToArray());
                item.BuildCache();
                return item;
            }
        }
    }
}