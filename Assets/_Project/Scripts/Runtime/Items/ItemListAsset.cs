using Itemworks.UnityEngine;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// Holds a list of item definition assets to register at startup.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemList", menuName = "Itemworks/Item List")]
    public class ItemListAsset : ScriptableObject 
    {
        /// <summary>
        /// Item definition assets included in this list.
        /// </summary>
        [SerializeReference] public List<ItemDefinitionAsset> ItemDefinitionAssets;
    }
}
