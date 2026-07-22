using Game.Items;
using Game.Items.Properties;
using Itemworks.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// Selects an unlocked shift item using its upgrade-dependent tier weight.
    /// </summary>
    public sealed class ShiftRandomItemDefinitionGiver : IRandomItemDefinitionGiver
    {
        private readonly List<ItemDefinition> _items = new();
        private readonly List<int> _weights = new();
        private int _totalWeight;

        public ShiftRandomItemDefinitionGiver(List<ItemDefinition> allowedItems, int unlockedTier, UpgradeService upgrades)
        {
            int clampedUnlockedTier = Mathf.Clamp(unlockedTier, 1, 4);
            for (int i = 0; i < allowedItems.Count; i++)
            {
                ItemDefinition item = allowedItems[i];
                if (item == null || !item.TryGetProperty<ShiftProperty>(out var shiftProperty))
                    continue;

                int tier = shiftProperty.Tier;
                if (tier < 1 || tier > clampedUnlockedTier)
                    continue;

                int weight = upgrades.GetOrderWeight(tier);
                if (weight <= 0)
                    continue;

                _items.Add(item);
                _weights.Add(weight);
                _totalWeight += weight;
            }
        }

        public ItemDefinition GetRandomItemDefinition()
        {
            if (_items.Count == 0 || _totalWeight <= 0)
            {
                Debug.LogError("The current shift has no unlocked meals with a valid tier and weight.");
                return null;
            }

            int roll = Random.Range(0, _totalWeight);
            for (int i = 0; i < _items.Count; i++)
            {
                if (roll < _weights[i])
                    return _items[i];

                roll -= _weights[i];
            }

            return _items[_items.Count - 1];
        }
    }
}
