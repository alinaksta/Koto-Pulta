using Game.Items;
using Game.Lifecycle;
using Game.Services;
using Itemworks.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Interaction
{
    public class ComputerController : MonoBehaviour, IBootstrapable
    {
        [SerializeField] private ComputerInteractable _computerInteractable;

        /// <summary>
        /// Raised when an item is successfully distributed to a hand.
        /// </summary>
        public event System.Action<Item, HandType> OnItemDistributed = delegate { };
        
        /// <summary>
        /// Raised when distribution fails (no free hands available).
        /// </summary>
        public event System.Action<Item> OnDistributionFailed = delegate { };

        public void GiveItemToPlayer(Item item)
        {
            // Create Item, checks, etc.
            Debug.Log("Got Item");
            if (_computerInteractable.HasInteractor && _computerInteractable.CurrentInteractor.TryGetFreeHand(out var hand))
            {
                Debug.Log("Success! Food in hand!");
                hand.Insert(item);
            }
        }

        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }
    }
}
