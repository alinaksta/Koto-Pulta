using Game.Characters;
using Game.Items;
using System;
using UnityEngine;

namespace Game.Progression
{
    public readonly struct ShiftStatistics
    {
        public int CustomersServed { get; }
        public int CustomersUnsatisfied { get; }
        public int MoneyEarned { get; }
        public float AverageDeliveryTime { get; }

        public int CustomersOverall => CustomersServed + CustomersUnsatisfied;

        public float CustomerServedPercentage => CustomersServed / (float)CustomersOverall;
        public float CustomerUnsatisfiedPercentage => 1f - CustomerServedPercentage;

        public ShiftStatistics(int customersServed, int customersUnsatisfied, int moneyEarned, float averageDeliveryTime)
        {
            CustomersServed = customersServed;
            CustomersUnsatisfied = customersUnsatisfied;
            MoneyEarned = moneyEarned;
            AverageDeliveryTime = averageDeliveryTime;
        }
    }
    
    public sealed class ShiftStatisticsCollector
    {
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

        public void Reset()
        {
            _customersServed = 0;
            _customersUnsatisfied = 0;
            _moneyEarned = 0;
            _deliveryTimeSumm = 0;
        }

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
