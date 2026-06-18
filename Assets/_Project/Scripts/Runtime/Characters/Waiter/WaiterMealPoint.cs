using Game.Services;
using UnityEngine;

namespace Game.Characters
{
    public class WaiterMealPoint : MonoBehaviour
    {
        [SerializeField] private Transform _point;

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
