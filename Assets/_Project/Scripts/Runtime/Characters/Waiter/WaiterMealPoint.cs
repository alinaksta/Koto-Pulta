using Game.Services;
using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Registers the active waiter meal pickup point.
    /// </summary>
    public class WaiterMealPoint : MonoBehaviour
    {
        [SerializeField] private Transform _point;

        /// <summary>
        /// Gets the transform waiters should navigate to for meals.
        /// </summary>
        public Transform Point => _point;

        public Vector3 Position => _point.position;

        private void Awake()
        {
            _point = _point == null ? transform : _point;
        }

        private void OnEnable()
        {
            if (ServiceLocator.TryGet<WaiterQueueService>(out var waiterQueueService))
                waiterQueueService.AddPoint(this);
        }

        private void OnDisable()
        {
            if (ServiceLocator.TryGet<WaiterQueueService>(out var waiterQueueService))
                waiterQueueService.RemovePoint(this);
        }
    }
}
