using Game.Items;
using Game.Items.Properties;
using Itemworks.UnityEngine;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// UI button component for the scrollable website.
    /// Distributes items to player hands when clicked.
    /// </summary>
    public class SiteItemButton : MonoBehaviour
    {
        [SerializeField] private ItemDefinitionAsset _itemAsset;
        [SerializeField] private Sprite lockedIcon;
        [SerializeField] private bool _isUnlocked = false;
        public event System.Action<Item> OnItemChosen = delegate { };
        public Button _button;
        private Image _icon;

        public int RequiredTier()
        {
            var definition = Item.FromId(_itemAsset.Id).Value.Definition;
            if (definition.TryGetProperty<ShiftProperty>(out var shiftProperty))
            {
                return shiftProperty.Tier;
            }
            return -1;
        }

        public int RequiredShift()
        {
            int tier = RequiredTier();
            return tier < 0 ? -1 : tier - 1;
        }


        private void Awake()
        {
            _button = GetComponent<Button>();
            _icon = GetComponent<Image>();
            // No need to make any global service, we actually don't need ComputerController here
            // The events allow us to inform ComputerController, so we don't need a reference
            //_computerController = ServiceLocator.Get<ComputerController>();
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClick);
            }
            if (_isUnlocked)
                RefreshIcon();
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClick);
            }
        }

        private void OnButtonClick()
        {
            if (_itemAsset == null)
            {
                Debug.LogWarning("Item ID not assigned to button");
                return;
            }

            var item = Item.FromId(_itemAsset.Id);
            if (!item.HasValue)
            {
                Debug.LogWarning($"Item with ID '{_itemAsset.Id}' not found in registry");
                return;
            }

            if (_isUnlocked)
            {
                OnItemChosen.Invoke(item.Value);
            }
            else
            {
                Debug.Log("Item is locked");
            }
        }

        public bool isUnlocked() {return _isUnlocked;}

        public void SetUnlocked(bool state)
        {
            _isUnlocked = state;
            RefreshIcon();
        }

        private void RefreshIcon()
        {
            if (_icon == null)
                return;

            if (!_isUnlocked)
            {
                _icon.sprite = lockedIcon;
                return;
            }

            var definition = Item.FromId(_itemAsset.Id).Value.Definition;
            _icon.sprite = definition.TryGetProperty<FoodProperty>(out var spriteProperty)
                ? spriteProperty.WorldSprite
                : null;
        }
    }
}
