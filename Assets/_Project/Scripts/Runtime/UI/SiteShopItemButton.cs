using Game.Interaction;
using Game.Items;
using Game.Services;
using Game.UI;
using Itemworks.UnityEngine;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// UI button component for the scrollable website.
    /// Distributes items to player hands when clicked.
    /// </summary>
    public class SiteShopItemButton : MonoBehaviour
    {
        private Button _button;
        private Image _buttonImage;
        [SerializeField] private bool isDefault;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite unlockedSprite;

        [SerializeField] private int price;
        private Button purchaseButton;

        private int playerBalance;
        private bool isUnlocked;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClick);
                _button.interactable = isDefault || isUnlocked;
            }
            _buttonImage = GetComponent<Image>();
            if (_buttonImage != null)
            {
                _buttonImage.sprite = isDefault || isUnlocked? unlockedSprite :lockedSprite;
            }

            purchaseButton = transform.Find("PurchaseButton").GetComponent<Button>();
            var priceText = transform.Find("PurchaseButton/Price").GetComponent<TextMeshProUGUI>();

            if (purchaseButton != null)
            {
                purchaseButton.onClick.AddListener(OnPurchaseClick);
                purchaseButton.gameObject.SetActive(!isDefault);
                if (priceText != null)
                    priceText.text = price.ToString();
            }
        }

        private void OnPurchaseClick()
        {
            //playerBalance = ServiceLocator.Get<IPlayerBalanceService>().GetBalance(); // пример
            
            if (playerBalance >= price)
            {
                //ServiceLocator.Get<IPlayerBalanceService>().Spend(price);
                
                UnlockVisuals();
                
                
                if (purchaseButton != null)
                    purchaseButton.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnButtonClick);
            
            if (purchaseButton != null)
                purchaseButton.onClick.RemoveListener(OnPurchaseClick);
        }

        private void OnButtonClick()
        {
            if (!isUnlocked)
            {
                UnlockVisuals();
            }
            else
            {
                ToggleVisuals();
            }
        }
        
        private void UnlockVisuals()
        {
            isUnlocked = true;
            _buttonImage.sprite = unlockedSprite;
            _button.interactable = true;
        }
        
        private void ToggleVisuals()
        {
            //TODO: Add visuals toggle 
        }
    }
}