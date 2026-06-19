using Itemworks.Core;
using Itemworks.UnityEngine;
using Itemworks.Serialization;
using UnityEngine;

namespace Itemworks.UnityEngine
{
    /// <summary>
    /// Converts between <see cref="ItemDefinition"/> instances and <see cref="ItemDefinitionAsset"/> assets.
    /// </summary>
    public struct ItemDefinitionAssetSerializer : IItemDefinitionSerializer<ItemDefinitionAsset>
    {
        /// <inheritdoc/>
        public ItemDefinition Deserialize(ItemDefinitionAsset data)
        {
            var item = ItemDefinition.NewBuilder(data.Id);
            foreach (var property in data.Properties)
                item.AddProperty(property);

            return item.Build();

        }

        /// <inheritdoc/>
        /// <remarks>
        /// Returns a newly created transient asset populated from the definition.
        /// </remarks>
        public ItemDefinitionAsset Serialize(ItemDefinition definition)
        {
            var data = ScriptableObject.CreateInstance<ItemDefinitionAsset>();
            data.Id = definition.Id;
            foreach (var property in definition.Properties)
                data.Properties.Add(property);

            return data;
        }
    }
}
