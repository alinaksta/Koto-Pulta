using Game.Items;
using Game.Interaction;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Global service for distributing items through UI interactions (website/computer).
    /// Registered as IBootstrapable on ServiceRoot.
    /// </summary>
    public class UItemDistributionService : MonoBehaviour
    {
        [SerializeField] private DualHandInteractor _dualHandInteractor;
        
        /// <summary>
        /// Raised when an item is successfully distributed to a hand.
        /// </summary>
        public event System.Action<Item, HandType> OnItemDistributed = delegate { };
        
        /// <summary>
        /// Raised when distribution fails (no free hands available).
        /// </summary>
        public event System.Action<Item> OnDistributionFailed = delegate { };

        private void Awake()
        {
            if (_dualHandInteractor == null)
            {
                _dualHandInteractor = FindFirstObjectByType<DualHandInteractor>();
                if (_dualHandInteractor == null)
                {
                    Debug.LogError("DualHandInteractor not found in scene!");
                }
            }
        }

        /// <summary>
        /// Attempts to distribute an item to a random free hand.
        /// </summary>
        /// <param name="item">Item to distribute (must have valid Definition).</param>
        /// <returns>True if item was distributed, false if both hands occupied or item invalid.</returns>
        public bool TryDistributeItem(Item item)
        {
            if (item.Definition == null || string.IsNullOrEmpty(item.Definition.Id))
            {
                Debug.LogWarning("Attempted to distribute invalid item");
                return false;
            }

            if (_dualHandInteractor == null)
            {
                Debug.LogError("DualHandInteractor not assigned!");
                return false;
            }

            var freeHands = GetFreeHands();
            
            if (freeHands.Count == 0)
            {
                Debug.Log($"Both hands occupied, cannot distribute item {item.Definition.Id}");
                OnDistributionFailed?.Invoke(item);
                return false;
            }

            Hand targetHand = freeHands[Random.Range(0, freeHands.Count)];
            HandType handType = targetHand == _dualHandInteractor.LeftHand ? HandType.Left : HandType.Right;
            
            targetHand.Insert(item);
            OnItemDistributed?.Invoke(item, handType);
            
            Debug.Log($"Item {item.Definition.Id} distributed to {handType} hand");
            return true;
        }

        private System.Collections.Generic.List<Hand> GetFreeHands()
        {
            var freeHands = new System.Collections.Generic.List<Hand>();
            
            if (_dualHandInteractor.LeftHand.IsEmpty)
                freeHands.Add(_dualHandInteractor.LeftHand);
                
            if (_dualHandInteractor.RightHand.IsEmpty)
                freeHands.Add(_dualHandInteractor.RightHand);
                
            return freeHands;
        }
    }
}