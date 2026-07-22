using Game.Lifecycle;
using Game.Items;
using Game.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Tracks waiter registration, customer assignments, and the meal point.
    /// </summary>
    public class WaiterService : MonoBehaviour, IBootstrapable
    {
        private readonly HashSet<Waiter> _waiters = new();
        private readonly Dictionary<Waiter, Customer> _customerByWaiter = new();
        private readonly Dictionary<Customer, Waiter> _waiterByCustomer = new();

        /// <summary>
        /// Raised after a customer is assigned to a waiter.
        /// </summary>
        public event Action<Waiter, Customer> OnCustomerAssignedToWaiter = delegate { };

        /// <summary>
        /// Raised after a waiter/customer assignment is cleared.
        /// </summary>
        public event Action<Waiter, Customer> OnCustomerUnassignedFromWaiter = delegate { };

        /// <summary>
        /// Raised when a waiter is directed to the active meal point.
        /// </summary>
        public event Action<Waiter> OnWaiterSentToMealPoint = delegate { };

        /// <inheritdoc/>
        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }

        /// <summary>
        /// Registers a waiter so it can receive assignments.
        /// </summary>
        /// <param name="waiter">Waiter to register.</param>
        public void RegisterWaiter(Waiter waiter)
        {
            if (waiter != null)
            {
                _waiters.Add(waiter);

                TryAssignNextWaitingCustomerToWaiter(waiter);
            }
        }

        /// <summary>
        /// Unregisters a waiter and clears any active assignment.
        /// </summary>
        /// <param name="waiter">Waiter to remove.</param>
        public void UnregisterWaiter(Waiter waiter)
        {
            if (waiter == null)
                return;

            _waiters.Remove(waiter);
            ClearAssignmentForWaiter(waiter, false, false);
        }

        /// <summary>
        /// Tries to find a waiter that can take a new customer.
        /// </summary>
        /// <param name="waiter">Receives the available waiter when found.</param>
        /// <returns><see langword="true"/> when an unassigned waiter is available.</returns>
        public bool TryGetUnassignedWaiter(out Waiter waiter)
        {
            foreach (var candidate in _waiters)
            {
                if (candidate == null)
                    continue;

                if (!candidate.CanAcceptAssignment)
                    continue;

                waiter = candidate;
                return true;
            }

            waiter = null;
            return false;
        }

        /// <summary>
        /// Tries to get any currently registered waiter.
        /// </summary>
        public bool TryGetAnyWaiter(out Waiter waiter)
        {
            foreach (Waiter candidate in _waiters)
            {
                if (candidate == null)
                    continue;

                waiter = candidate;
                return true;
            }

            waiter = null;
            return false;
        }

        /// <summary>
        /// Tries to assign a customer to an available waiter.
        /// </summary>
        /// <param name="customer">Customer that needs service.</param>
        /// <param name="waiter">Receives the assigned waiter when successful.</param>
        /// <returns><see langword="true"/> when the customer was assigned.</returns>
        public bool TryAssignCustomerToUnassignedWaiter(Customer customer, out Waiter waiter)
        {
            waiter = null;

            if (customer == null || !customer.NeedsWaiter)
                return false;

            if (_waiterByCustomer.ContainsKey(customer))
                return false;

            if (!TryGetUnassignedWaiter(out waiter))
                return false;

            AssignCustomerToWaiter(waiter, customer);
            return true;
        }

        /// <summary>
        /// Tries to assign a customer to an available waiter.
        /// </summary>
        /// <param name="customer">Customer that needs service.</param>
        /// <returns><see langword="true"/> when the customer was assigned.</returns>
        public bool TryAssignCustomerToUnassignedWaiter(Customer customer)
            => TryAssignCustomerToUnassignedWaiter(customer, out _);

        /// <summary>
        /// Checks whether any registered waiter can accept a new assignment.
        /// </summary>
        /// <returns><see langword="true"/> when a waiter is available.</returns>
        public bool HasUnassignedWaiter()
            => TryGetUnassignedWaiter(out _);

        /// <summary>
        /// Checks whether the supplied waiter is currently registered.
        /// </summary>
        /// <param name="waiter">Waiter to check.</param>
        /// <returns><see langword="true"/> when the waiter is registered.</returns>
        public bool IsWaiterRegistered(Waiter waiter)
            => waiter != null && _waiters.Contains(waiter);

        /// <summary>
        /// Gets the number of currently registered waiters.
        /// </summary>
        /// <returns>Registered waiter count.</returns>
        public int GetRegisteredWaiterCount()
            => _waiters.Count;

        /// <summary>
        /// Gets the number of registered waiters without a customer assignment.
        /// </summary>
        /// <returns>Unassigned waiter count.</returns>
        public int GetUnassignedWaiterCount()
        {
            int count = 0;

            foreach (var waiter in _waiters)
            {
                if (waiter != null && !waiter.IsAssigned)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Tries to assign the supplied waiter to the next active customer that still needs service.
        /// </summary>
        /// <param name="waiter">Waiter to assign.</param>
        /// <returns><see langword="true"/> when a new customer assignment was made.</returns>
        public bool TryAssignNextWaitingCustomerToWaiter(Waiter waiter)
        {
            if (waiter == null || !IsWaiterRegistered(waiter) || !waiter.CanAcceptAssignment)
                return false;

            if (!TryGetNextUnassignedCustomerNeedingWaiter(out var customer))
                return false;

            AssignCustomerToWaiter(waiter, customer);
            return true;
        }

        private void HandleCustomerServed(Customer customer)
        {
            ClearAssignmentForCustomer(customer, true, false);
        }

        private void HandleCustomerTimedOut(Customer customer)
        {
            ClearAssignmentForCustomer(customer, true, false);
        }

        private void HandleCustomerWrongItem(Customer customer, Item item)
        {
            if (customer == null || !_waiterByCustomer.TryGetValue(customer, out Waiter waiter))
                return;

            waiter.ClearCarriedItem();
            waiter.StartGoingToMealPoint();
        }

        private void ClearAssignmentForCustomer(Customer customer, bool clearCarriedItem, bool sendToMealPoint)
        {
            if (customer == null || !_waiterByCustomer.TryGetValue(customer, out var waiter))
                return;

            customer.OnServed -= HandleCustomerServed;
            customer.OnTimedOut -= HandleCustomerTimedOut;
            customer.OnWrongItemGiven -= HandleCustomerWrongItem;

            _waiterByCustomer.Remove(customer);
            _customerByWaiter.Remove(waiter);

            waiter.ClearCustomer();

            if (clearCarriedItem)
                waiter.ClearCarriedItem();

            OnCustomerUnassignedFromWaiter.Invoke(waiter, customer);

            if (TryAssignNextWaitingCustomerToWaiter(waiter))
                return;

            if (sendToMealPoint)
                waiter.StartGoingToMealPoint();
            else
                waiter.EnterIdleState();
        }

        private void ClearAssignmentForWaiter(Waiter waiter, bool clearCarriedItem, bool sendToMealPoint)
        {
            if (waiter == null || !_customerByWaiter.TryGetValue(waiter, out var customer))
                return;

            customer.OnServed -= HandleCustomerServed;
            customer.OnTimedOut -= HandleCustomerTimedOut;
            customer.OnWrongItemGiven -= HandleCustomerWrongItem;

            _customerByWaiter.Remove(waiter);
            _waiterByCustomer.Remove(customer);

            waiter.ClearCustomer();

            if (clearCarriedItem)
                waiter.ClearCarriedItem();

            OnCustomerUnassignedFromWaiter.Invoke(waiter, customer);

            if (TryAssignNextWaitingCustomerToWaiter(waiter))
                return;

            if (sendToMealPoint)
                waiter.StartGoingToMealPoint();
            else
                waiter.EnterIdleState();
        }

        private bool TryGetNextUnassignedCustomerNeedingWaiter(out Customer customer)
        {
            customer = null;

            if (!ServiceLocator.TryGet<CustomerService>(out var customerService))
                return false;

            return customerService.TryGetNextCustomerNeedingWaiter(out customer, candidate => !_waiterByCustomer.ContainsKey(candidate));
        }

        private void AssignCustomerToWaiter(Waiter waiter, Customer customer)
        {
            waiter.AssignCustomer(customer);

            _customerByWaiter[waiter] = customer;
            _waiterByCustomer[customer] = waiter;

            customer.OnServed += HandleCustomerServed;
            customer.OnTimedOut += HandleCustomerTimedOut;
            customer.OnWrongItemGiven += HandleCustomerWrongItem;

            OnCustomerAssignedToWaiter.Invoke(waiter, customer);
        }
    }
}
