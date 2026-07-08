using Game.Lifecycle;
using Game.Services;
using System;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// Tracks and mutates the player balance.
    /// </summary>
    public class BalanceService : MonoBehaviour, IBootstrapable
    {
        private int _balance;

        /// <summary>
        /// Gets the current balance value.
        /// </summary>
        public int Balance => _balance;

        public event Action<int> OnBalanceChanged = delegate { };

        /// <summary>
        /// Adds money to the current balance.
        /// </summary>
        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            _balance += amount;
            OnBalanceChanged.Invoke(_balance);
        }

        /// <summary>
        /// Tries to spend money from the current balance.
        /// </summary>
        public bool TrySpend(int amount)
        {
            if (amount <= 0 || amount > _balance)
                return false;

            _balance -= amount;
            OnBalanceChanged.Invoke(_balance);
            return true;
        }

        /// <inheritdoc/>
        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }
    }
}
