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

        public string DisplayName => _displayName;
        public int Price => _price;
        public bool UnlockedByDefault => _unlockedByDefault;
        public Sprite LockedSprite => _lockedSprite;
        public Sprite UnlockedSprite => _unlockedSprite;
        public abstract CustomizationCategory Category { get; }
    }
}
