using Game.Lifecycle;
using Game.Items;
using Game.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Characters
{
    public class WaiterService : MonoBehaviour, IBootstrapable
    {
        private readonly HashSet<Waiter> _waiters = new();
        private readonly Dictionary<Waiter, Customer> _customerByWaiter = new();
        private readonly Dictionary<Customer, Waiter> _waiterByCustomer = new();

        private WaiterMealPoint _mealPoint;

        public event Action<Waiter, Customer> OnCustomerAssignedToWaiter = delegate { };
        public event Action<Waiter, Customer> OnCustomerUnassignedFromWaiter = delegate { };
        public event Action<Waiter> OnWaiterSentToMealPoint = delegate { };

        public bool HasMealPoint => _mealPoint != null;
        public Transform MealPointTransform => _mealPoint.Point;

        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }

        public void RegisterWaiter(Waiter waiter)
        {
            if (waiter != null)
                _waiters.Add(waiter);
        }

        public void UnregisterWaiter(Waiter waiter)
        {
            if (waiter == null)
                return;

            ClearAssignmentForWaiter(waiter, false, false);
            _waiters.Remove(waiter);
        }

        public void SetMealPoint(WaiterMealPoint mealPoint)
        {
            _mealPoint = mealPoint;
        }

        public void ClearMealPoint(WaiterMealPoint mealPoint)
        {
            if (_mealPoint == mealPoint)
                _mealPoint = null;
        }

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

        public bool TryAssignCustomerToUnassignedWaiter(Customer customer, out Waiter waiter)
        {
            waiter = null;

            if (customer == null)
                return false;

            if (_waiterByCustomer.ContainsKey(customer))
                return false;

            if (!TryGetUnassignedWaiter(out waiter))
                return false;

            waiter.AssignCustomer(customer);

            _customerByWaiter[waiter] = customer;
            _waiterByCustomer[customer] = waiter;

            customer.OnServed += HandleCustomerServed;
            customer.OnTimedOut += HandleCustomerTimedOut;
            customer.OnWrongItemGiven += HandleCustomerWrongItem;

            OnCustomerAssignedToWaiter.Invoke(waiter, customer);
            return true;
        }

        public bool TryAssignCustomerToUnassignedWaiter(Customer customer)
            => TryAssignCustomerToUnassignedWaiter(customer, out _);

        public bool HasUnassignedWaiter()
            => TryGetUnassignedWaiter(out _);

        public bool IsWaiterRegistered(Waiter waiter)
            => waiter != null && _waiters.Contains(waiter);

        public void SendWaiterToMealPoint(Waiter waiter)
        {
            if (waiter == null)
                return;

            if (_mealPoint == null)
            {
                waiter.EnterIdleState();
                return;
            }

            if (!waiter.gameObject.activeInHierarchy)
            {
                waiter.EnterIdleState();
                return;
            }

            waiter.NavigateTo(_mealPoint.Point.position);
            OnWaiterSentToMealPoint.Invoke(waiter);
        }

        public int GetRegisteredWaiterCount()
            => _waiters.Count;

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
            ClearAssignmentForCustomer(customer, true, false);
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

            if (sendToMealPoint)
                SendWaiterToMealPoint(waiter);
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

            if (sendToMealPoint)
                SendWaiterToMealPoint(waiter);
            else
                waiter.EnterIdleState();
        }
    }
}
