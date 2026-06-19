using Itemworks.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Itemworks.UnityEngine
{
    [CreateAssetMenu(fileName = "NewItemDefinition", menuName = "Itemworks/Item Definition")]
    public class ItemDefinitionAsset : ScriptableObject
    {
        public string Id;
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