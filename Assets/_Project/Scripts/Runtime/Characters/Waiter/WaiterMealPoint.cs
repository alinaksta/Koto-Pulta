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
        public Transform Point => _point != null ? _point : transform;

        private void OnEnable()
        {
            if (ServiceLocator.TryGet<WaiterService>(out var waiterService))
                waiterService.SetMealPoint(this);
        }

        private void OnDisable()
        {
            if (ServiceLocator.TryGet<WaiterService>(out var waiterService))
                waiterService.ClearMealPoint(this);
        }
    }
}
