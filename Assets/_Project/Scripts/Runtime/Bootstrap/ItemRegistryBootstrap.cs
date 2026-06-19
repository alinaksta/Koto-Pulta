using Game.Items;
using Game.Lifecycle;
using Itemworks.Core;
using Itemworks.UnityEngine;
using System;
using UnityEngine;

/// <summary>
/// Loads item definitions from an asset list into the runtime registry.
/// </summary>
public class ItemRegistryBootstrap : MonoBehaviour, IBootstrapable
{
    [SerializeField] private ItemListAsset _itemListAsset;

    /// <inheritdoc/>
    /// <remarks>
    /// Registers every definition asset listed in the configured item list.
    /// </remarks>
    public void Bootstrap()
    {
        var serializer = new ItemDefinitionAssetSerializer();


        int counter = 0;
        foreach (ItemDefinitionAsset definitionAsset in _itemListAsset.ItemDefinitionAssets)
        {
            ItemDefinition definition = serializer.Deserialize(definitionAsset);
            ItemRegistry.Instance.RegisterItem(definition);
            counter++;
        }

        Debug.Log($"Registered {counter} items!");
    }
}
