using Game.Interaction;
using Game.Items;
using Game.Services;
using Game.UI;
using Itemworks.UnityEngine;
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
        private bool isUnlocked;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClick);
            }
            _buttonImage = GetComponent<Image>();
            if (_buttonImage != null)
            {
                _buttonImage.sprite = isDefault? unlockedSprite :lockedSprite;
            }
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
        }
        
        private void ToggleVisuals()
        {
            //TODO: Add visuals toggle 
        }
    }
}