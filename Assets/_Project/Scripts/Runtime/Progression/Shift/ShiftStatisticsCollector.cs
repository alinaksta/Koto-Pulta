using Game.Characters;
using Game.Items;
using System;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// Represents runtime statistics collected for a shift.
    /// </summary>
    public readonly struct ShiftStatistics
    {
        /// <summary>
        /// Gets the number of customers served during the shift.
        /// </summary>
        public int CustomersServed { get; }
        /// <summary>
        /// Gets the number of unsatisfied customers during the shift.
        /// </summary>
        public int CustomersUnsatisfied { get; }
        /// <summary>
        /// Gets the money earned during the shift.
        /// </summary>
        public int MoneyEarned { get; }
        /// <summary>
        /// Gets the average delivery time recorded for the shift.
        /// </summary>
        public float AverageDeliveryTime { get; }

        /// <summary>
        /// Gets the total number of customers counted for the shift.
        /// </summary>
        public int CustomersOverall => CustomersServed + CustomersUnsatisfied;

        /// <summary>
        /// Gets the served customer percentage for the shift.
        /// </summary>
        public float CustomerServedPercentage => CustomersServed / (float)CustomersOverall;
        /// <summary>
        /// Gets the unsatisfied customer percentage for the shift.
        /// </summary>
        public float CustomerUnsatisfiedPercentage => 1f - CustomerServedPercentage;

        /// <summary>
        /// Creates a shift statistics snapshot from the supplied values.
        /// </summary>
        public ShiftStatistics(int customersServed, int customersUnsatisfied, int moneyEarned, float averageDeliveryTime)
        {
            CustomersServed = customersServed;
            CustomersUnsatisfied = customersUnsatisfied;
            MoneyEarned = moneyEarned;
            AverageDeliveryTime = averageDeliveryTime;
        }
    }
    
    /// <summary>
    /// Collects runtime statistics for the current shift.
    /// </summary>
    public sealed class ShiftStatisticsCollector
    {
        /// <summary>
        /// Creates a statistics collector bound to the supplied customer service.
        /// </summary>
        public ShiftStatisticsCollector(CustomerService customerService)
        {
            _customerService = customerService;

            _customerService.OnCustomerServed += HandleCustomerServed;
            _customerService.OnCustomerTimedOut += HandleCustomerTimedOut;
            _customerService.OnCustomerWrongItem += HandleCustomerWrong;
        }

        private int _customersServed;
        private int _customersUnsatisfied;

        private int _moneyEarned;

        private float _deliveryTimeSumm;

        private CustomerService _customerService;

        /// <summary>
        /// Resets all collected shift statistics.
        /// </summary>
        public void Reset()
        {
            _customersServed = 0;
            _customersUnsatisfied = 0;
            _moneyEarned = 0;
            _deliveryTimeSumm = 0;
        }

        /// <summary>
        /// Builds a snapshot of the currently collected shift statistics.
        /// </summary>
        public ShiftStatistics GetStatistics()
        {
            int customersOverall = _customersUnsatisfied + _customersServed;
            float averageDeliveryTime = _deliveryTimeSumm / (float)customersOverall;
            Debug.Log($"Average delivery time: {averageDeliveryTime}");

            return new ShiftStatistics(
                _customersServed, 
                _customersUnsatisfied, 
                _moneyEarned, 
                averageDeliveryTime);
        }

        /// <summary>
        /// Sets the money earned value tracked by the collector.
        /// </summary>
        public void SetMoneyEarned(int value) => _moneyEarned = value;

        #region Event handlers
        private void HandleCustomerWrong(Customer customer, Item item)
        {
            _customersUnsatisfied++;
        }

        private void HandleCustomerTimedOut(Customer customer)
        {
            _customersUnsatisfied++;
        }

        private void HandleCustomerServed(Customer customer)
        {
            _customersServed++;
            _deliveryTimeSumm += customer.InitialWaitTime - customer.WaitTimer;
        }
        #endregion
    }
}
