using Game.Environment;
using Game.Progression;
using Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// UI card for buying and equipping one environment customization.
    /// </summary>
    public class SiteShopItemButton : MonoBehaviour
    {
        [SerializeField] private CustomizationDefinition _customization;
        [SerializeField] private Image _previewImage;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _equipButton;
        [SerializeField] private TMP_Text _priceLabel;
        [SerializeField] private TMP_Text _buyButtonLabel;
        [SerializeField] private TMP_Text _equipButtonLabel;

        private CustomizationShopService _shopService;
        private BalanceService _balanceService;

        private bool HasSeparateEquipButton => _equipButton != null && _equipButton != _buyButton;

        private void Awake()
        {
            if (_previewImage == null)
                _previewImage = GetComponent<Image>();

            if (_buyButton == null)
                _buyButton = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (ServiceLocator.TryGet(out _shopService))
            {
                _shopService.Register(_customization);
                _shopService.OnPurchased += HandleShopChanged;
                _shopService.OnEquipped += HandleShopChanged;
            }

            if (ServiceLocator.TryGet(out _balanceService))
                _balanceService.OnBalanceChanged += HandleBalanceChanged;

            if (_buyButton != null)
                _buyButton.onClick.AddListener(HandleBuyClicked);

            if (HasSeparateEquipButton)
                _equipButton.onClick.AddListener(HandleEquipClicked);

            Refresh();
        }

        private void OnDisable()
        {
            if (_shopService != null)
            {
                _shopService.OnPurchased -= HandleShopChanged;
                _shopService.OnEquipped -= HandleShopChanged;
            }

            if (_balanceService != null)
                _balanceService.OnBalanceChanged -= HandleBalanceChanged;

            if (_buyButton != null)
                _buyButton.onClick.RemoveListener(HandleBuyClicked);

            if (HasSeparateEquipButton)
                _equipButton.onClick.RemoveListener(HandleEquipClicked);
        }

        private void HandleBuyClicked()
        {
            if (_shopService == null || _customization == null)
                return;

            if (!_shopService.IsOwned(_customization))
            {
                _shopService.TryPurchase(_customization);
                Refresh();
                return;
            }

            if (!HasSeparateEquipButton)
                _shopService.TryEquip(_customization);

            Refresh();
        }

        private void HandleEquipClicked()
        {
            if (_shopService == null || _customization == null)
                return;

            _shopService.TryEquip(_customization);
            Refresh();
        }

        private void HandleShopChanged(CustomizationDefinition customization)
        {
            if (_customization == null)
                return;

            if (customization != null && customization.Category != _customization.Category)
                return;

            Refresh();
        }

        private void HandleBalanceChanged(int balance)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_shopService == null || _customization == null)
                return;

            bool owned = _shopService.IsOwned(_customization);
            bool equipped = _shopService.IsEquipped(_customization);
            bool canAfford = _shopService.CanAfford(_customization);

            Sprite previewSprite = owned
                ? _customization.UnlockedSprite
                : _customization.LockedSprite;

            if (_previewImage != null && previewSprite != null)
                _previewImage.sprite = previewSprite;

            if (_priceLabel != null)
                _priceLabel.text = owned ? string.Empty : _customization.Price.ToString();

            RefreshBuyButton(owned, equipped, canAfford);
            RefreshEquipButton(owned, equipped);
        }

        private void RefreshBuyButton(bool owned, bool equipped, bool canAfford)
        {
            if (_buyButton == null)
                return;

            if (HasSeparateEquipButton)
            {
                _buyButton.gameObject.SetActive(!owned);
                _buyButton.interactable = !owned && canAfford;

                if (_buyButtonLabel != null)
                    _buyButtonLabel.text = "Buy";

                return;
            }

            _buyButton.gameObject.SetActive(true);
            _buyButton.interactable = owned ? !equipped : canAfford;

            if (_buyButtonLabel == null)
                return;

            if (!owned)
                _buyButtonLabel.text = "Buy";
            else
                _buyButtonLabel.text = equipped ? "Equipped" : "Equip";
        }

        private void RefreshEquipButton(bool owned, bool equipped)
        {
            if (!HasSeparateEquipButton)
                return;

            _equipButton.gameObject.SetActive(owned);
            _equipButton.interactable = owned && !equipped;

            if (_equipButtonLabel != null)
                _equipButtonLabel.text = equipped ? "Equipped" : "Equip";
        }
    }
}
