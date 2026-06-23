using Game.Items;
using Game.UI;
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
        [SerializeField] private string _itemId; // ItemRegistry ID
        [SerializeField] private UItemDistributionService _distributionService;
        
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClick);
            }

            if (_distributionService == null)
            {
                _distributionService = FindFirstObjectByType<UItemDistributionService>();
                if (_distributionService == null)
                {
                    Debug.LogError("UItemDistributionService not found in scene!");
                }
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
            if (string.IsNullOrEmpty(_itemId))
            {
                Debug.LogWarning("Item ID not assigned to button");
                return;
            }

            var item = Item.FromId(_itemId);
            if (!item.HasValue)
            {
                Debug.LogWarning($"Item with ID '{_itemId}' not found in registry");
                return;
            }

            if (_distributionService == null)
            {
                Debug.LogWarning("UItemDistributionService not assigned");
                return;
            }

            _distributionService.TryDistributeItem(item.Value);
        }
    }
}