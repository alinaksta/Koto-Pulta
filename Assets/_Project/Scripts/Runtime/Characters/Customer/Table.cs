using Game.Items;
using Game.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Characters
{
    public class Table : MonoBehaviour
    {
        [SerializeField] private int _tableNumber;
        [SerializeField] private bool _registerAutomatically = false;
        [SerializeField] private Transform[] _seatPoints;

        private readonly List<Customer> _customers = new();

        public int TableNumber => _tableNumber;
        public IReadOnlyList<Customer> Customers => _customers;
        public int SeatCount => _seatPoints.Length;
        public bool IsFree => _customers.Count == 0;
        public int OccupiedSeatCount => _customers.Count;
        public int FreeSeatCount => SeatCount - _customers.Count;
        public bool HasFreeSeat => FreeSeatCount > 0;

        public event Action<Table, Customer> OnCustomerAdded = delegate { };
        public event Action<Table, Customer> OnCustomerRemoved = delegate { };
        public event Action<Table> OnBecameFree = delegate { };

        private void Awake()
        {
            if (ServiceLocator.TryGet<CustomerService>(out var service) && _registerAutomatically)
            {
                service.RegisterTable(this);
            }
        }

        public bool CanSeat(int count) => IsFree && SeatCount >= count;

        public bool TryAddCustomer(Customer customer, out Transform seat)
        {
            seat = null;

            if (customer == null || !HasFreeSeat)
                return false;

            seat = _seatPoints[_customers.Count];
            _customers.Add(customer);
            OnCustomerAdded.Invoke(this, customer);

            return true;
        }

        public void RemoveCustomer(Customer customer)
        {
            if (!_customers.Remove(customer))
                return;

            OnCustomerRemoved.Invoke(this, customer);

            if (IsFree)
                OnBecameFree.Invoke(this);
        }

        public bool TryDeliverToCustomer(Item item, out Customer served, out bool exactMatch)
        {
            served = GetBestDeliveryTarget(item, out exactMatch);

            if (served == null)
                return false;

            return served.TryRecieveFromWaiter(item);
        }

        public Customer GetBestDeliveryTarget(Item item, out bool exactMatch)
        {
            Customer accepted = null;
            exactMatch = false;
            for (int i = 0; i < _customers.Count; i++)
            {
                var customer = _customers[i];
                if (customer.CanRecieve(item))
                {
                    exactMatch = true;
                    return customer;
                }

                accepted = customer;
            }

            return accepted;
        }
    }
}