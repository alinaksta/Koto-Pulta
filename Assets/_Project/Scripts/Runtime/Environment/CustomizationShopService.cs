using Game.Lifecycle;
using Game.Progression;
using Game.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Environment
{
    /// <summary>
    /// Owns simple in-memory purchase and equip state for environment customizations.
    /// </summary>
    public class CustomizationShopService : MonoBehaviour, IBootstrapable
    {
        private readonly HashSet<CustomizationDefinition> _owned = new();

        private CustomizationService _customizationService;
        private BalanceService _balanceService;
        private FloorCustomization _equippedFloor;
        private WallCustomization _equippedWalls;
        private PanoramaCustomization _equippedPanorama;

        public event Action<CustomizationDefinition> OnPurchased = delegate { };
        public event Action<CustomizationDefinition> OnEquipped = delegate { };

        /// <inheritdoc/>
        public void Bootstrap()
        {
            _customizationService = ServiceLocator.Get<CustomizationService>();
            _balanceService = ServiceLocator.Get<BalanceService>();
            ServiceLocator.Register(this);
        }

        /// <summary>
        /// Registers an item with the shop, initializing default ownership/equipment.
        /// </summary>
        public void Register(CustomizationDefinition customization)
        {
            if (customization == null)
                return;

            if (!customization.UnlockedByDefault)
                return;

            _owned.Add(customization);

            if (GetEquipped(customization.Category) == null)
                TryEquip(customization);
        }

        public bool IsOwned(CustomizationDefinition customization)
        {
            return customization != null && _owned.Contains(customization);
        }

        public bool IsEquipped(CustomizationDefinition customization)
        {
            return customization != null && ReferenceEquals(
                customization,
                GetEquipped(customization.Category));
        }

        public bool CanAfford(CustomizationDefinition customization)
        {
            return customization != null
                && (customization.Price <= 0 || _balanceService.Balance >= customization.Price);
        }

        public bool TryPurchase(CustomizationDefinition customization)
        {
            if (customization == null || IsOwned(customization))
                return false;

            if (customization.Price > 0 && !_balanceService.TrySpend(customization.Price))
                return false;

            _owned.Add(customization);
            OnPurchased.Invoke(customization);
            return true;
        }

        public bool TryEquip(CustomizationDefinition customization)
        {
            if (customization == null || !IsOwned(customization))
                return false;

            switch (customization)
            {
                case FloorCustomization floor:
                    _equippedFloor = floor;
                    _customizationService.ApplyFloor(floor);
                    break;
                case WallCustomization walls:
                    _equippedWalls = walls;
                    _customizationService.ApplyWalls(walls);
                    break;
                case PanoramaCustomization panorama:
                    _equippedPanorama = panorama;
                    _customizationService.ApplyPanorama(panorama);
                    break;
                default:
                    return false;
            }

            OnEquipped.Invoke(customization);
            return true;
        }

        private CustomizationDefinition GetEquipped(CustomizationCategory category)
        {
            return category switch
            {
                CustomizationCategory.Floor => _equippedFloor,
                CustomizationCategory.Walls => _equippedWalls,
                CustomizationCategory.Panorama => _equippedPanorama,
                _ => null
            };
        }
    }
}
