using Game.Services;
using UnityEngine;

namespace Game.Characters
{
    public class DebugCustomerSpawner : MonoBehaviour
    {
        private CustomerService _customerService;
        private WaiterService _waiterService;

        private void Awake()
        {
            _customerService = ServiceLocator.Get<CustomerService>();
        }

        public void Spawn()
        {
            _customerService.TrySpawnCustomerAtRandomFreeTable();
        }
    }
}