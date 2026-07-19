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
        private const string BalanceKey = "Progression.Balance";

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
            Save();
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
            Save();
            OnBalanceChanged.Invoke(_balance);
            return true;
        }

        /// <summary>
        /// Sets the balance to an exact value.
        /// </summary>
        public void SetBalance(int amount)
        {
            int clampedAmount = Mathf.Max(0, amount);
            if (_balance == clampedAmount)
                return;

            _balance = clampedAmount;
            Save();
            OnBalanceChanged.Invoke(_balance);
        }

        /// <inheritdoc/>
        public void Bootstrap()
        {
            _balance = Mathf.Max(0, PlayerPrefs.GetInt(BalanceKey, 0));
            ServiceLocator.Register(this);
        }

        private void Save()
        {
            PlayerPrefs.SetInt(BalanceKey, _balance);
            PlayerPrefs.Save();
        }
    }
}
