using Game.Lifecycle;
using Game.Services;
using System;
using UnityEngine;

namespace Game.Progression
{
    public class BalanceService : MonoBehaviour, IBootstrapable
    {
        private int _balance;

        public int Balance => _balance;

        public event Action<int> OnBalanceChanged = delegate { };

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            _balance += amount;
            OnBalanceChanged.Invoke(_balance);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || amount > _balance)
                return false;

            _balance -= amount;
            OnBalanceChanged.Invoke(_balance);
            return true;
        }

        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }
    }
}
