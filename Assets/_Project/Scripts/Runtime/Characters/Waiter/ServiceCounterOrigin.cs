using Game.Services;
using UnityEngine;

namespace Game.Characters
{
    public class ServiceCounterOrigin : MonoBehaviour
    {
        [SerializeField] private Transform _origin;

        private void Awake()
        {
            if (_origin == null)
                _origin = transform;
        }

        private void Start()
        {
            var queueService = ServiceLocator.Get<WaiterQueueService>();

            queueService.SetServiceCounterOrigin(_origin);
        }
    }
}
