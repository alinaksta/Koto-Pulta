using Game.Characters;
using Game.Services;
using UnityEngine;

namespace Game.Diagnostics
{
    /// <summary>
    /// Debug helper that spawns a customer at a random free table.
    /// </summary>
    public class DebugCustomerSpawner : MonoBehaviour
    {
        private CustomerService _customerService;
        private WaiterService _waiterService;

        private void Awake()
        {
            _customerService = ServiceLocator.Get<CustomerService>();
        }

        /// <summary>
        /// Attempts to spawn a customer using the registered customer service.
        /// </summary>
        public void Spawn()
        {
            _customerService.TrySpawnCustomerAtRandomFreeTable();
        }
    }
}
