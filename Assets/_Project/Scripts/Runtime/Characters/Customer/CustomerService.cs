using Game.Items;
using Game.Lifecycle;
using Game.Services;
using Itemworks.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Characters
{
    public class CustomerService : MonoBehaviour, IBootstrapable
    {
        [SerializeField] private Customer _customerPrefab;

        private readonly List<Table> _tables = new();
        private readonly List<Table> _freeTables = new();
        private readonly Dictionary<int, Table> _tablesByNumber = new();
        private readonly List<Customer> _activeCustomers = new();

        public event Action<Customer> OnCustomerSpawned = delegate { };
        public event Action<Customer> OnCustomerServed = delegate { };
        public event Action<Customer> OnCustomerTimedOut = delegate { };
        public event Action<Customer, Item> OnCustomerWrongItem = delegate { };

        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }

        public void RegisterTable(Table table)
        {
            if (table == null || _tablesByNumber.ContainsKey(table.TableNumber))
                return;

            _tables.Add(table);
            _freeTables.Add(table);
            _tablesByNumber.Add(table.TableNumber, table);

            table.OnBecameFree += HandleTableFreed;
        }

        public bool TryGetTable(int tableNumber, out Table table)
            => _tablesByNumber.TryGetValue(tableNumber, out table);

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

        public bool TrySpawnCustomerAtTable(Table table)
        {
            if (table == null || !table.HasFreeSeat)
                return false;

            var customer = Instantiate(_customerPrefab);

            if (table.TryAddCustomer(customer, out var seat))
            {
                customer.transform.position = seat.position;
                customer.transform.rotation = seat.rotation;
            }
            else
            {
                Destroy(customer.gameObject);
                return false;
            }

            ItemDefinition order = GetRandomOrder();
            customer.Initialize(table, order);

            customer.OnServed += HandleCustomerServed;
            customer.OnTimedOut += HandleCustomerTimedOut;
            customer.OnWrongItemGiven += HandleCustomerWrongItem;

            _activeCustomers.Add(customer);

            if (ServiceLocator.TryGet<WaiterService>(out var waiterService))
                waiterService.TryAssignCustomerToUnassignedWaiter(customer);

            OnCustomerSpawned.Invoke(customer);

            return true;
        }

        private void HandleCustomerWrongItem(Customer customer, Item item)
        {
            OnCustomerWrongItem.Invoke(customer, item);
            CleanUpCustomer(customer);
        }

        private void HandleCustomerTimedOut(Customer customer)
        {
            OnCustomerTimedOut.Invoke(customer);
            CleanUpCustomer(customer);
        }

        private void HandleCustomerServed(Customer customer)
        {
            OnCustomerServed.Invoke(customer);
            CleanUpCustomer(customer);
        }

        private ItemDefinition GetRandomOrder()
        {
            return ItemRegistry.Instance.Get("dev_calculator"); // TODO: Add actual random
        }

        private void CleanUpCustomer(Customer customer)
        {
            if (customer == null) return;

            customer.OnServed -= HandleCustomerServed;
            customer.OnTimedOut -= HandleCustomerTimedOut;
            customer.OnWrongItemGiven -= HandleCustomerWrongItem;

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
