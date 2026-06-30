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

        private void Awake()
        {
            _button = GetComponent<Button>();

            // No need to make any global service, we actually don't need ComputerController here
            // The events allow us to inform ComputerController, so we don't need a reference
            //_computerController = ServiceLocator.Get<ComputerController>();
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

            OnItemChosen.Invoke(item.Value);
            Debug.Log("Event invoked");

            // Computer controller can subscibe to event, no need to call stuff manually
            //_computerController.GiveItemToPlayer(item.Value);
        }
    }
}