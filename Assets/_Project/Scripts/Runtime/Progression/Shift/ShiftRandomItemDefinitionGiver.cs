using Game.Items;
using Game.Items.Properties;
using Itemworks.Core;
using System.Collections.Generic;

namespace Game.Progression
{
    public sealed class ShiftRandomItemDefinitionGiver : IRandomItemDefinitionGiver
    {
        private readonly ShiftService _shiftService;
        private readonly List<ItemDefinition> _allowedItems;

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
                && shiftProp.RequiredShift <= _shiftService.ShiftIndex);
            return unlocked.Count > 0 ? unlocked[UnityEngine.Random.Range(0, unlocked.Count)] : null;
        }
    }
}
