using Itemworks.UnityEngine;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Items
{
    [CreateAssetMenu(fileName = "NewItemList", menuName = "Itemworks/Item List")]
    public class ItemListAsset : ScriptableObject 
    {
        [SerializeReference] public List<ItemDefinitionAsset> ItemDefinitionAssets;
    }
}