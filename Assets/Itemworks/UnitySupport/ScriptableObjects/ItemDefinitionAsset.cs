using Itemworks.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Itemworks.UnityEngine
{
    /// <summary>
    /// Unity asset used to author an item definition.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemDefinition", menuName = "Itemworks/Item Definition")]
    public class ItemDefinitionAsset : ScriptableObject
    {
        /// <summary>
        /// Registry id used when this asset is converted into an item definition.
        /// </summary>
        public string Id;

        /// <summary>
        /// Properties serialized into the resulting item definition.
        /// </summary>
        [SerializeReference] public List<ItemProperty> Properties;

        private void OnValidate()
        {
            if (!ItemRegistry.IsValidId(Id))
            {
                Id = Id.ToLower().Replace(" ", "_");
            }
        }
    }
}
