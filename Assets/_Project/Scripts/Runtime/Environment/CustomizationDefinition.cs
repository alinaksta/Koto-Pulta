using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Environment
{
    /// <summary>
    /// Shared shop-facing data for every environment customization asset.
    /// </summary>
    public abstract class CustomizationDefinition : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("_name")]
        private string _displayName;
        [SerializeField, Min(0)] private int _price;
        [SerializeField] private bool _unlockedByDefault;
        [SerializeField] private Sprite _lockedSprite;
        [SerializeField] private Sprite _unlockedSprite;

        /// <summary>
        /// Gets the display name shown in the customization shop.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Gets the purchase price for this customization.
        /// </summary>
        public int Price => _price;

        /// <summary>
        /// Gets whether this customization starts owned without purchase.
        /// </summary>
        public bool UnlockedByDefault => _unlockedByDefault;

        /// <summary>
        /// Gets the shop preview sprite used while this customization is locked.
        /// </summary>
        public Sprite LockedSprite => _lockedSprite;

        /// <summary>
        /// Gets the shop preview sprite used after this customization is unlocked.
        /// </summary>
        public Sprite UnlockedSprite => _unlockedSprite;

        /// <summary>
        /// Gets the customization category this definition belongs to.
        /// </summary>
        public abstract CustomizationCategory Category { get; }
    }
}
