using UnityEngine;

namespace Game.Characters
{
    public class Seat : MonoBehaviour
    {
        [SerializeField] private Transform _customerSpawnOrigin;
        [SerializeField] private Transform _customerAskOrigin;
        [SerializeField] private bool _flipSprite;

        public Vector3 CustomerSpawnOrigin => _customerSpawnOrigin.position;
        public Vector3 CustomerAskOrigin => _customerAskOrigin.position;
        public bool FlipSprite => _flipSprite;

        public Vector3 CustomerAskDirection => (CustomerSpawnOrigin - CustomerAskOrigin).normalized;
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
