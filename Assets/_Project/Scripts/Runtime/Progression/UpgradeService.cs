using Game.Lifecycle;
using Game.Services;
using System;
using UnityEngine;

namespace Game.Progression
{
    public enum UpgradeType
    {
        WaiterCount,
        WaiterSpeed,
        TableCount,
        ExpensiveOrderChance
    }

    /// <summary>
    /// Owns persistent upgrade levels and performs upgrade purchases.
    /// </summary>
    public sealed class UpgradeService : MonoBehaviour, IBootstrapable
    {
        private const string KeyPrefix = "Progression.Upgrade.";

        private BalanceService _balance;
        private readonly int[] _levels = new int[4];

        public event Action<UpgradeType, int> OnUpgradeChanged = delegate { };

        public void Bootstrap()
        {
            _balance = ServiceLocator.Get<BalanceService>();

            foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType)))
            {
                int level = PlayerPrefs.GetInt(GetKey(type), GetInitialLevel(type));
                _levels[(int)type] = Mathf.Clamp(level, GetInitialLevel(type), GetMaxLevel(type));
            }

            ServiceLocator.Register(this);
        }

        public int GetLevel(UpgradeType type) => _levels[(int)type];

        public int GetMaxLevel(UpgradeType type)
        {
            return 10;
        }

        public bool IsMaxLevel(UpgradeType type) => GetLevel(type) >= GetMaxLevel(type);

        public int GetNextCost(UpgradeType type)
        {
            if (IsMaxLevel(type))
                return 0;

            int level = GetLevel(type);
            double baseCost;
            double growth;

            switch (type)
            {
                case UpgradeType.WaiterCount:
                    baseCost = 10d;
                    growth = 1.36d;
                    break;
                case UpgradeType.WaiterSpeed:
                    baseCost = 15d;
                    growth = 1.25d;
                    break;
                case UpgradeType.TableCount:
                    baseCost = 50d;
                    growth = 1.28d;
                    break;
                case UpgradeType.ExpensiveOrderChance:
                    baseCost = 40d;
                    growth = 1.25d;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            double cost = Math.Ceiling(baseCost * Math.Pow(growth, level - 1));
            return cost >= int.MaxValue ? int.MaxValue : (int)cost;
        }

        public bool CanUpgrade(UpgradeType type)
        {
            return !IsMaxLevel(type) && _balance != null && _balance.Balance >= GetNextCost(type);
        }

        public bool TryUpgrade(UpgradeType type)
        {
            if (!CanUpgrade(type) || !_balance.TrySpend(GetNextCost(type)))
                return false;

            int newLevel = GetLevel(type) + 1;
            _levels[(int)type] = newLevel;
            PlayerPrefs.SetInt(GetKey(type), newLevel);
            PlayerPrefs.Save();
            OnUpgradeChanged.Invoke(type, newLevel);
            return true;
        }

        /// <summary>
        /// Resets all upgrades to their initial levels.
        /// </summary>
        public void ResetAll()
        {
            foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType)))
            {
                int initialLevel = GetInitialLevel(type);
                _levels[(int)type] = initialLevel;
                PlayerPrefs.DeleteKey(GetKey(type));
                OnUpgradeChanged.Invoke(type, initialLevel);
            }

            PlayerPrefs.Save();
        }

        public float GetWaiterSpeedMultiplier()
        {
            return Mathf.Pow(1.15f, GetLevel(UpgradeType.WaiterSpeed) - 1);
        }

        public int GetOrderWeight(int tier)
        {
            int levelOffset = GetLevel(UpgradeType.ExpensiveOrderChance) - 1;
            switch (tier)
            {
                case 1: return 100;
                case 2: return 70 + 4 * levelOffset;
                case 3: return 40 + 5 * levelOffset;
                case 4: return 10 + 6 * levelOffset;
                default: return 0;
            }
        }

        private static int GetInitialLevel(UpgradeType type)
        {
            return type == UpgradeType.TableCount ? 5 : 1;
        }

        private static string GetKey(UpgradeType type) => KeyPrefix + type;
    }
}
