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

        /// <summary>
        /// Creates a mutable builder from this definition.
        /// </summary>
        /// <returns>A builder containing this definition's current values.</returns>
        public ItemDefinitionBuilder ToBuilder()
            => new ItemDefinitionBuilder { Id = Id, Properties = Properties.ToList()};

        /// <summary>
        /// Collects data used to build an <see cref="ItemDefinition"/>.
        /// </summary>
        [Serializable]
        public sealed class ItemDefinitionBuilder
        {
            /// <summary>
            /// Item id to assign to the built definition.
            /// </summary>
            public string Id;

            /// <summary>
            /// Properties to include on the built definition.
            /// </summary>
            public List<ItemProperty> Properties = new();

            /// <summary>
            /// Adds a property when it is not already in the builder.
            /// </summary>
            /// <param name="property">Property instance to add.</param>
            /// <returns>This builder.</returns>
            public ItemDefinitionBuilder AddProperty(ItemProperty property) { 
                if (!Properties.Contains(property))
                    Properties.Add(property);
                return this;
            }

            /// <summary>
            /// Removes a property when it is present in the builder.
            /// </summary>
            /// <param name="property">Property instance to remove.</param>
            /// <returns>This builder.</returns>
            public ItemDefinitionBuilder RemoveProperty(ItemProperty property)
            {
                if (Properties.Contains(property))
                    Properties.Remove(property);
                return this;
            }

            /// <summary>
            /// Builds an immutable definition from the current builder data.
            /// </summary>
            /// <returns>A new <see cref="ItemDefinition"/> instance.</returns>
            public ItemDefinition Build()
            {
                var item = new ItemDefinition(Id, Properties.ToArray());
                item.BuildCache();
                return item;
            }
        }
    }
}
