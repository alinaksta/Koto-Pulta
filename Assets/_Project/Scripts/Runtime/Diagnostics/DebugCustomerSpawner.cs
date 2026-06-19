using Game.Characters;
using Game.Services;
using UnityEngine;

namespace Game.Diagnostics
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
