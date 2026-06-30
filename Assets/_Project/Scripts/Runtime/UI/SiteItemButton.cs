using Game.Interaction;
using Game.Items;
using Game.Services;
using Game.UI;
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
        public event System.Action<Item> OnItemChosen = delegate { };
        private Button _button;
        private ComputerController _computerController;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _computerController = ServiceLocator.Get<ComputerController>();
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClick);
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

            if (_computerController == null)
            {
                Debug.LogError("Buttons Dont's see controller");
            }
            OnItemChosen.Invoke(Item.FromId(_itemAsset.Id).Value);
            Debug.Log("Event invoked");
            _computerController.GiveItemToPlayer(Item.FromId(_itemAsset.Id).Value);
            Debug.Log("Gave Item");
        }
    }
}