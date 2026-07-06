using Game.Items;
using Game.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Represents a table with fixed seating for customers.
    /// </summary>
    public class Table : MonoBehaviour
    {
        [SerializeField] private int _tableNumber;
        [SerializeField] private bool _registerAutomatically = false;
        [SerializeField] private Seat[] _seats;

        private readonly List<Customer> _customers = new();

        /// <summary>
        /// Gets the configured table number.
        /// </summary>
        public int TableNumber => _tableNumber;

        /// <summary>
        /// Gets the customers currently seated at the table.
        /// </summary>
        public IReadOnlyList<Customer> Customers => _customers;

        /// <summary>
        /// Gets the number of seat points configured on the table.
        /// </summary>
        public int SeatCount => _seats.Length;

        /// <summary>
        /// Gets whether the table currently has no customers.
        /// </summary>
        public bool IsFree => _customers.Count == 0;

        /// <summary>
        /// Gets the number of occupied seats.
        /// </summary>
        public int OccupiedSeatCount => _customers.Count;

        /// <summary>
        /// Gets the number of currently free seats.
        /// </summary>
        public int FreeSeatCount => SeatCount - _customers.Count;

        /// <summary>
        /// Gets whether at least one seat is free.
        /// </summary>
        public bool HasFreeSeat => FreeSeatCount > 0;

        /// <summary>
        /// Raised after a customer is added to the table.
        /// </summary>
        public event Action<Table, Customer> OnCustomerAdded = delegate { };

        /// <summary>
        /// Raised after a customer is removed from the table.
        /// </summary>
        public event Action<Table, Customer> OnCustomerRemoved = delegate { };

        /// <summary>
        /// Raised when the table becomes empty.
        /// </summary>
        public event Action<Table> OnBecameFree = delegate { };

        private void Awake()
        {
            if (ServiceLocator.TryGet<CustomerService>(out var service) && _registerAutomatically)
            {
                service.RegisterTable(this);
            }
        }

        /// <summary>
        /// Checks whether the table can seat the requested number of customers.
        /// </summary>
        /// <param name="count">Number of seats needed.</param>
        /// <returns><see langword="true"/> when the table is empty and has enough seats.</returns>
        public bool CanSeat(int count) => IsFree && SeatCount >= count;

        /// <summary>
        /// Tries to add a customer and assign the next free seat transform.
        /// </summary>
        /// <param name="customer">Customer to seat.</param>
        /// <param name="seat">Receives the assigned seat.</param>
        /// <returns><see langword="true"/> when the customer was seated.</returns>
        public bool TryAddCustomer(Customer customer, out Seat seat)
        {
            seat = null;

            if (customer == null || !HasFreeSeat)
                return false;

            seat = _seats[_customers.Count];
            _customers.Add(customer);
            OnCustomerAdded.Invoke(this, customer);

            return true;
        }

        public bool TryAddCustomerAtRandomSeat(Customer customer, out Seat seat)
        {
            seat = null;

            if (customer == null || !IsFree)
                return false;

            int randomIndex = UnityEngine.Random.Range(0, _seats.Length);
            seat = _seats[randomIndex];
            _customers.Add(customer);
            OnCustomerAdded.Invoke(this, customer);

            return true;
        }

        /// <summary>
        /// Removes a customer from the table.
        /// </summary>
        /// <param name="customer">Customer to remove.</param>
        public void RemoveCustomer(Customer customer)
        {
            if (!_customers.Remove(customer))
                return;

            OnCustomerRemoved.Invoke(this, customer);

            if (IsFree)
                OnBecameFree.Invoke(this);
        }

        /// <summary>
        /// Tries to deliver an item to the best customer at the table.
        /// </summary>
        /// <param name="item">Item being delivered.</param>
        /// <param name="served">Receives the customer targeted for delivery.</param>
        /// <param name="exactMatch">Receives whether the chosen customer accepted the exact requested item.</param>
        /// <returns><see langword="true"/> when a customer accepted the item.</returns>
        public bool TryDeliverToCustomer(Item item, out Customer served, out bool exactMatch)
        {
            served = GetBestDeliveryTarget(item, out exactMatch);

            if (served == null)
                return false;

            return served.TryRecieveFromWaiter(item);
        }

        /// <summary>
        /// Gets the best delivery target among currently seated customers.
        /// </summary>
        /// <param name="item">Item being considered for delivery.</param>
        /// <param name="exactMatch">Receives whether a waiting customer was found.</param>
        /// <returns>The preferred customer to receive the item, or <see langword="null"/> when none are seated.</returns>
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
