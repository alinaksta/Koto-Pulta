using Game.Items;
using Game.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Interaction
{
    public class ComputerController : MonoBehaviour
    {
        [SerializeField] private ComputerInteractable _computerInteractable;
        [SerializeField] private List<SiteItemButton> _itemButtons;

        /// <summary>
        /// Raised when an item is successfully distributed to a hand.
        /// </summary>
        public event Action<Item, HandType> OnItemDistributed = delegate { };
        
        /// <summary>
        /// Raised when distribution fails (no free hands available).
        /// </summary>
        public event Action<Item> OnDistributionFailed = delegate { };

        private void Start()
        {
            foreach (var button in  _itemButtons)
            {
                button.OnItemChosen += HandleItemChosen;
            }
        }

        private void OnDestroy()
        {
            foreach (var button in _itemButtons)
            {
                button.OnItemChosen -= HandleItemChosen;
            }
        }

        private void HandleItemChosen(Item item)
        {
            GiveItemToPlayer(item);
        }

        public void GiveItemToPlayer(Item item)
        {
            // Create Item, checks, etc.
            Debug.Log("Got Item");
            Debug.Log($"Has interactor: {_computerInteractable.HasInteractor}");
            if (_computerInteractable.HasInteractor && _computerInteractable.CurrentInteractor.TryGetFreeHand(out var hand))
            {
                Debug.Log("Success! Food in hand!");
                hand.Insert(item);
            }
            _computerInteractable.ExitComputer();
        }

        // No need to make it a Service or bootstrap at the beginning. It doesn't need any dependencies
        //public void Bootstrap()
        //{
        //    ServiceLocator.Register(this);
        //}
    }
}
