using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Defines a customer seat and its interaction points.
    /// </summary>
    public class Seat : MonoBehaviour
    {
        [SerializeField] private Transform _customerSpawnOrigin;
        [SerializeField] private Transform _customerAskOrigin;
        [SerializeField] private bool _flipSprite;

        /// <summary>
        /// Gets the world position where the customer starts moving from.
        /// </summary>
        public Vector3 CustomerSpawnOrigin => _customerSpawnOrigin.position;
        /// <summary>
        /// Gets the world position where the waiter asks the customer.
        /// </summary>
        public Vector3 CustomerAskOrigin => _customerAskOrigin.position;
        /// <summary>
        /// Gets whether the seated customer sprite should be flipped.
        /// </summary>
        public bool FlipSprite => _flipSprite;

        /// <summary>
        /// Gets the direction a customer faces while being asked.
        /// </summary>
        public Vector3 CustomerAskDirection => (CustomerSpawnOrigin - CustomerAskOrigin).normalized;
        /// <summary>
        /// Gets the rotation a customer uses while being asked.
        /// </summary>
        public Quaternion CustomerAskRotation => Quaternion.FromToRotation(Vector3.forward, CustomerAskDirection);

        private void Awake()
        {
            if (_customerSpawnOrigin.position == _customerAskOrigin.position)
            {
                Debug.LogError($"Spawn origin and ask origin have the same position for this {nameof(Seat)}!", this);
            }
        }
    }
}
