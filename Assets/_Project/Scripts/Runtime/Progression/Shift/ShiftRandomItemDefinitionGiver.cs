using Game.Items;
using Game.Items.Properties;
using Itemworks.Core;
using System.Collections.Generic;

namespace Game.Progression
{
    /// <summary>
    /// Provides a random item definition for the active shift.
    /// </summary>
    public sealed class ShiftRandomItemDefinitionGiver : IRandomItemDefinitionGiver
    {
        private readonly ShiftService _shiftService;
        private readonly List<ItemDefinition> _allowedItems;

        /// <summary>
        /// Creates a shift-aware random item definition provider.
        /// </summary>
        public ShiftRandomItemDefinitionGiver(ShiftService shiftService, List<ItemDefinition> allowedItems)
        {
            _shiftService = shiftService;
            _allowedItems = allowedItems;
        }

        /// <inheritdoc/>
        public ItemDefinition GetRandomItemDefinition()
        {
            var unlocked = _allowedItems
                .FindAll(item => item.TryGetProperty<ShiftProperty>(out var shiftProp)
                && shiftProp.RequiredShift <= _shiftService.ItemUnlockShiftIndex);
            return unlocked.Count > 0 ? unlocked[UnityEngine.Random.Range(0, unlocked.Count)] : null;
        }
    }
}
