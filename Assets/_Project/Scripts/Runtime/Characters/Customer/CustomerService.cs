using Game.Items;
using Game.Lifecycle;
using Game.Services;
using Itemworks.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Spawns customers, tracks active tables, and routes customer lifecycle events.
    /// </summary>
    public class CustomerService : MonoBehaviour, IBootstrapable
    {
        [SerializeField] private Customer _customerPrefab;

        private readonly List<Table> _tables = new();
        private readonly List<Table> _freeTables = new();
        private readonly Dictionary<int, Table> _tablesByNumber = new();
        private readonly List<Customer> _activeCustomers = new();

        private IRandomItemDefinitionGiver _randomItemGiver;
        private CustomerQueue _queue;

        /// <summary>
        /// Gets the active cashier queue, when the current scene uses the queue flow.
        /// </summary>
        public CustomerQueue Queue => _queue;

        /// <summary>
        /// Gets whether the current scene routes customers through a cashier queue.
        /// </summary>
        public bool UsesQueue => _queue != null && _queue.isActiveAndEnabled;

        /// <summary>
        /// Raised after a customer is spawned and initialized.
        /// </summary>
        public event Action<Customer> OnCustomerSpawned = delegate { };

        /// <summary>
        /// Raised when a spawned customer is served.
        /// </summary>
        public event Action<Customer> OnCustomerServed = delegate { };

        /// <summary>
        /// Raised when a spawned customer times out.
        /// </summary>
        public event Action<Customer> OnCustomerTimedOut = delegate { };

        /// <summary>
        /// Raised when a spawned customer receives the wrong item.
        /// </summary>
        public event Action<Customer, Item> OnCustomerWrongItem = delegate { };

        /// <inheritdoc/>
        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }

        /// <summary>
        /// Registers a table so it can receive spawned customers.
        /// </summary>
        /// <param name="table">Table to register.</param>
        public bool RegisterTable(Table table)
        {
            if (table == null || _tablesByNumber.ContainsKey(table.TableNumber))
                return false;

            _tables.Add(table);
            if (table.IsFree)
                _freeTables.Add(table);
            _tablesByNumber.Add(table.TableNumber, table);

            table.OnBecameFree += HandleTableFreed;
            return true;
        }

        /// <summary>
        /// Removes a table from customer spawning and lookup.
        /// </summary>
        public void UnregisterTable(Table table)
        {
            if (table == null || !_tables.Remove(table))
                return;

            _freeTables.Remove(table);
            if (_tablesByNumber.TryGetValue(table.TableNumber, out var registered) && registered == table)
                _tablesByNumber.Remove(table.TableNumber);

            table.OnBecameFree -= HandleTableFreed;
        }

        /// <summary>
        /// Tries to get a registered table by its number.
        /// </summary>
        /// <param name="tableNumber">Number assigned to the table.</param>
        /// <param name="table">Receives the table when found.</param>
        /// <returns><see langword="true"/> when the table is registered.</returns>
        public bool TryGetTable(int tableNumber, out Table table)
            => _tablesByNumber.TryGetValue(tableNumber, out table);

        /// <summary>
        /// Tries to spawn a customer at a random currently free table.
        /// </summary>
        /// <returns><see langword="true"/> when a customer was spawned.</returns>
        public bool TrySpawnCustomerAtRandomFreeTable(float? waitTimerOverride = null)
        {
            // Queue flow: customers enter the restaurant and line up at the cashier instead of appearing at tables.
            if (UsesQueue)
                return _queue.TrySpawnCustomer();

            if (_freeTables.Count == 0)
                return false;

            int randomIndex = UnityEngine.Random.Range(0, _freeTables.Count);
            Table table = _freeTables[randomIndex];

            bool spawned = TrySpawnCustomerAtTable(table, waitTimerOverride);

            if (spawned)
                _freeTables.RemoveAt(randomIndex);

            return spawned;
        }

        /// <summary>
        /// Tries to spawn a customer at the supplied table.
        /// </summary>
        /// <param name="table">Table to seat the customer at.</param>
        /// <returns><see langword="true"/> when the customer was spawned and seated.</returns>
        public bool TrySpawnCustomerAtTable(Table table, float? waitTimerOverride = null)
        {
            if (table == null || !table.HasFreeSeat)
                return false;

            var customer = Instantiate(_customerPrefab);

            if (table.TryAddCustomerAtRandomSeat(customer, out var seat))
            {
                customer.transform.position = seat.CustomerSpawnOrigin;
                customer.transform.rotation = Quaternion.identity; // We rotate using SpriteRotator, so it doesn't matter
                customer.transform.SetParent(seat.transform, true);
            }
            else
            {
                Destroy(customer.gameObject);
                return false;
            }

            ItemDefinition order = GetRandomOrder();
            customer.Initialize(table, seat, order, waitTimerOverride);

            customer.OnServed += HandleCustomerServed;
            customer.OnTimedOut += HandleCustomerTimedOut;
            customer.OnWrongItemGiven += HandleCustomerWrongItem;

            _activeCustomers.Add(customer);

            OnCustomerSpawned.Invoke(customer);

            if (ServiceLocator.TryGet<WaiterService>(out var waiterService))
                waiterService.TryAssignCustomerToUnassignedWaiter(customer);

            return true;
        }

        #region Queue flow
        /// <summary>
        /// Registers the scene's cashier queue. While registered, spawn requests are routed into the queue.
        /// </summary>
        public void RegisterQueue(CustomerQueue queue)
        {
            _queue = queue;
        }

        /// <summary>
        /// Unregisters the scene's cashier queue.
        /// </summary>
        public void UnregisterQueue(CustomerQueue queue)
        {
            if (_queue == queue)
                _queue = null;
        }

        /// <summary>
        /// Instantiates a persistent customer that is tracked by this service but not seated yet.
        /// </summary>
        public Customer CreatePersistentCustomer(Vector3 position)
        {
            var customer = Instantiate(_customerPrefab, position, Quaternion.identity);
            customer.SetPersistent(true);
            customer.SetSelfTickPatience(true);

            customer.OnServed += HandleCustomerServed;
            customer.OnTimedOut += HandleCustomerTimedOut;
            customer.OnWrongItemGiven += HandleCustomerWrongItem;

            _activeCustomers.Add(customer);
            return customer;
        }

        /// <summary>
        /// Stops tracking and destroys a persistent customer (e.g. when it leaves the restaurant).
        /// </summary>
        public void RemovePersistentCustomer(Customer customer)
        {
            if (customer == null)
                return;

            customer.OnServed -= HandleCustomerServed;
            customer.OnTimedOut -= HandleCustomerTimedOut;
            customer.OnWrongItemGiven -= HandleCustomerWrongItem;

            _activeCustomers.Remove(customer);

            if (customer.Table != null)
                customer.Table.RemoveCustomer(customer);

            Destroy(customer.gameObject);
        }

        /// <summary>
        /// Reserves a random free table for the customer and picks a seat on it.
        /// </summary>
        public bool TryReserveRandomFreeTable(Customer customer, out Table table, out Seat seat)
        {
            table = null;
            seat = null;

            if (customer == null || _freeTables.Count == 0)
                return false;

            int randomIndex = UnityEngine.Random.Range(0, _freeTables.Count);
            Table candidate = _freeTables[randomIndex];

            if (candidate == null || !candidate.TryAddCustomerAtRandomSeat(customer, out seat))
                return false;

            _freeTables.RemoveAt(randomIndex);
            table = candidate;
            return true;
        }

        /// <summary>
        /// Gets the number of currently free tables.
        /// </summary>
        public int FreeTableCount => _freeTables.Count;

        /// <summary>
        /// Tries to roll a random order using the installed item giver.
        /// </summary>
        public bool TryGetRandomOrder(out ItemDefinition order)
        {
            order = _randomItemGiver?.GetRandomItemDefinition();
            return order != null;
        }

        /// <summary>
        /// Reports a customer that ran out of patience while waiting in the cashier queue.
        /// </summary>
        public void ReportQueueTimeout(Customer customer)
        {
            if (customer != null)
                OnCustomerTimedOut.Invoke(customer);
        }
        #endregion

        /// <summary>
        /// Sets the random item source used when creating customer orders.
        /// </summary>
        public void SetRandomItemGiver(IRandomItemDefinitionGiver giver)
        {
            _randomItemGiver = giver;
        }

        /// <summary>
        /// Tries to get the next active customer who is still waiting for a waiter.
        /// </summary>
        /// <param name="customer">Receives the matching customer when found.</param>
        /// <param name="predicate">Optional extra filter for candidate customers.</param>
        /// <returns><see langword="true"/> when a matching customer is found.</returns>
        public bool TryGetNextCustomerNeedingWaiter(out Customer customer, Func<Customer, bool> predicate = null)
        {
            for (int i = 0; i < _activeCustomers.Count; i++)
            {
                var candidate = _activeCustomers[i];
                if (candidate == null || !candidate.NeedsWaiter)
                    continue;

                if (predicate != null && !predicate(candidate))
                    continue;

                customer = candidate;
                return true;
            }

            customer = null;
            return false;
        }

        /// <summary>
        /// Forces every currently waiting active customer to time out.
        /// </summary>
        public void TimeoutAllActiveCustomers()
        {
            for (int i = _activeCustomers.Count - 1; i >= 0; i--)
            {
                Customer customer = _activeCustomers[i];
                if (customer == null || !customer.IsWaiting)
                    continue;

                customer.ForceTimeout();
            }
        }

        /// <summary>
        /// Immediately removes all active customers when leaving gameplay.
        /// </summary>
        public void ClearActiveCustomers()
        {
            for (int i = _activeCustomers.Count - 1; i >= 0; i--)
            {
                Customer customer = _activeCustomers[i];
                if (customer == null)
                    continue;

                customer.OnServed -= HandleCustomerServed;
                customer.OnTimedOut -= HandleCustomerTimedOut;
                customer.OnWrongItemGiven -= HandleCustomerWrongItem;

                if (customer.Table != null)
                    customer.Table.RemoveCustomer(customer);

                Destroy(customer.gameObject);
            }

            _activeCustomers.Clear();

            if (_queue != null)
                _queue.HandleCustomersCleared();
        }

        private void HandleCustomerWrongItem(Customer customer, Item item)
        {
            OnCustomerWrongItem.Invoke(customer, item);
        }

        private void HandleCustomerTimedOut(Customer customer)
        {
            OnCustomerTimedOut.Invoke(customer);

            if (TryRecycleThroughQueue(customer))
                return;

            DespawnCustomer(customer);
        }

        private void HandleCustomerServed(Customer customer)
        {
            OnCustomerServed.Invoke(customer);

            if (TryRecycleThroughQueue(customer))
                return;

            DespawnCustomer(customer);
        }

        private bool TryRecycleThroughQueue(Customer customer)
        {
            if (customer == null || !customer.IsPersistent || _queue == null)
                return false;

            _queue.HandleVisitFinished(customer);
            return true;
        }

        private ItemDefinition GetRandomOrder()
        {
            return _randomItemGiver.GetRandomItemDefinition();
        }

        private async void DespawnCustomer(Customer customer)
        {
            if (customer == null) return;

            customer.OnServed -= HandleCustomerServed;
            customer.OnTimedOut -= HandleCustomerTimedOut;
            customer.OnWrongItemGiven -= HandleCustomerWrongItem;

            await Awaitable.WaitForSecondsAsync(customer.DespawnDuration);

            if (customer == null)
                return;

            _activeCustomers.Remove(customer);

            if (customer.Table != null)
                customer.Table.RemoveCustomer(customer);

            Destroy(customer.gameObject);
        }

        private void HandleTableFreed(Table table)
        {
            if (table != null && _tables.Contains(table) && !_freeTables.Contains(table))
                _freeTables.Add(table);
        }
    }
}
