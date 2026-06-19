using Game.Characters;
using Game.Items;
using Game.Services;
using UnityEngine;

namespace Game.Diagnostics
{
    /// <summary>
    /// Logs customer and waiter service events to the Unity console.
    /// </summary>
    public class CustomerWaiterDebug : MonoBehaviour
    {
        private CustomerService _customerService;
        private WaiterService _waiterService;

        private void Start()
        {
            _customerService = ServiceLocator.Get<CustomerService>();
            _waiterService = ServiceLocator.Get<WaiterService>();

            _customerService.OnCustomerSpawned += OnCustomerSpawned;
            _customerService.OnCustomerServed += OnCustomerServed;
            _customerService.OnCustomerTimedOut += OnCustomerTimedOut;
            _customerService.OnCustomerWrongItem += OnCustomerWrongItem;

            _waiterService.OnCustomerAssignedToWaiter += OnCustomerAssignedToWaiter;
            _waiterService.OnCustomerUnassignedFromWaiter += OnCustomerUnassignedFromWaiter;
            _waiterService.OnWaiterSentToMealPoint += OnWaiterSentToMealPoint;
        }

        private void OnDestroy()
        {
            if (_customerService != null)
            {
                _customerService.OnCustomerSpawned -= OnCustomerSpawned;
                _customerService.OnCustomerServed -= OnCustomerServed;
                _customerService.OnCustomerTimedOut -= OnCustomerTimedOut;
                _customerService.OnCustomerWrongItem -= OnCustomerWrongItem;
            }

            if (_waiterService != null)
            {
                _waiterService.OnCustomerAssignedToWaiter -= OnCustomerAssignedToWaiter;
                _waiterService.OnCustomerUnassignedFromWaiter -= OnCustomerUnassignedFromWaiter;
                _waiterService.OnWaiterSentToMealPoint -= OnWaiterSentToMealPoint;
            }
        }

        private void OnCustomerSpawned(Customer customer)
        {
            UnityEngine.Debug.Log($"Customer spawned: {customer.name} at table {customer.Table.TableNumber}, order {customer.Order.Id}");
        }

        private void OnCustomerServed(Customer customer)
        {
            UnityEngine.Debug.Log($"Customer served: {customer.name} at table {customer.Table.TableNumber}");
        }

        private void OnCustomerTimedOut(Customer customer)
        {
            UnityEngine.Debug.Log($"Customer timed out: {customer.name} at table {customer.Table.TableNumber}");
        }

        private void OnCustomerWrongItem(Customer customer, Item item)
        {
            UnityEngine.Debug.Log($"Customer got wrong item: {customer.name} at table {customer.Table.TableNumber}, item {item.Definition.Id}");
        }

        private void OnCustomerAssignedToWaiter(Waiter waiter, Customer customer)
        {
            UnityEngine.Debug.Log($"Waiter assigned: {waiter.name} -> {customer.name} at table {customer.Table.TableNumber}");
        }

        private void OnCustomerUnassignedFromWaiter(Waiter waiter, Customer customer)
        {
            UnityEngine.Debug.Log($"Waiter unassigned: {waiter.name} from {customer.name}");
        }

        private void OnWaiterSentToMealPoint(Waiter waiter)
        {
            UnityEngine.Debug.Log($"Waiter sent to meal point: {waiter.name}");
        }
    }
}
