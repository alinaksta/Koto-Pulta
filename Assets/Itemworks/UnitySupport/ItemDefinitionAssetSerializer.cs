using Itemworks.Core;
using Itemworks.UnityEngine;
using Itemworks.Serialization;
using UnityEngine;

namespace Itemworks.UnityEngine
{
    public struct ItemDefinitionAssetSerializer : IItemDefinitionSerializer<ItemDefinitionAsset>
    {
        public ItemDefinition Deserialize(ItemDefinitionAsset data)
        {
            var item = ItemDefinition.NewBuilder(data.Id);
            foreach (var property in data.Properties)
                item.AddProperty(property);

            return item.Build();

        }

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