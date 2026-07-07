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
        public void RegisterTable(Table table)
        {
            if (table == null || _tablesByNumber.ContainsKey(table.TableNumber))
                return;

            _tables.Add(table);
            _freeTables.Add(table);
            _tablesByNumber.Add(table.TableNumber, table);

            table.OnBecameFree += HandleTableFreed;
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
        public bool TrySpawnCustomerAtRandomFreeTable()
        {
            if (_freeTables.Count == 0)
                return false;

            int randomIndex = UnityEngine.Random.Range(0, _freeTables.Count);
            Table table = _freeTables[randomIndex];

            bool spawned = TrySpawnCustomerAtTable(table);

            if (spawned)
                _freeTables.RemoveAt(randomIndex);

            return spawned;
        }

        /// <summary>
        /// Tries to spawn a customer at the supplied table.
        /// </summary>
        /// <param name="table">Table to seat the customer at.</param>
        /// <returns><see langword="true"/> when the customer was spawned and seated.</returns>
        public bool TrySpawnCustomerAtTable(Table table)
        {
            if (table == null || !table.HasFreeSeat)
                return false;

            var customer = Instantiate(_customerPrefab);

            if (table.TryAddCustomerAtRandomSeat(customer, out var seat))
            {
                customer.transform.position = seat.CustomerSpawnOrigin;
                customer.transform.rotation = Quaternion.identity; // We rotate using SpriteRotator, so it doesn't matter
            }
            else
            {
                Destroy(customer.gameObject);
                return false;
            }

            ItemDefinition order = GetRandomOrder();
            customer.Initialize(table, seat, order);

            customer.OnServed += HandleCustomerServed;
            customer.OnTimedOut += HandleCustomerTimedOut;
            customer.OnWrongItemGiven += HandleCustomerWrongItem;

            _activeCustomers.Add(customer);

            if (ServiceLocator.TryGet<WaiterService>(out var waiterService))
                waiterService.TryAssignCustomerToUnassignedWaiter(customer);

            OnCustomerSpawned.Invoke(customer);

            return true;
        }

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
            var customers = _activeCustomers.ToArray();
            for (int i = 0; i < customers.Length; i++)
            {
                var customer = customers[i];
                if (customer == null || !customer.IsWaiting)
                    continue;

                customer.ForceTimeout();
            }
        }

        private void HandleCustomerWrongItem(Customer customer, Item item)
        {
            OnCustomerWrongItem.Invoke(customer, item);
            DespawnCustomer(customer);
        }

        private void HandleCustomerTimedOut(Customer customer)
        {
            OnCustomerTimedOut.Invoke(customer);
            DespawnCustomer(customer);
        }

        private void HandleCustomerServed(Customer customer)
        {
            OnCustomerServed.Invoke(customer);
            Debug.Log("Customer Served");
            DespawnCustomer(customer);
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
            
            _activeCustomers.Remove(customer);

            if (customer.Table != null)
                customer.Table.RemoveCustomer(customer);

            Destroy(customer.gameObject);
        }

        private void HandleTableFreed(Table table)
        {
            _freeTables.Add(table);
        }
    }
}
