using Game.Progression;
using Game.Services;
using UnityEditor;
using UnityEngine;

namespace Game.Diagnostics.Editor
{
    public static class KotoPultaMenu
    {
        private const string TutorialCompletedKey = "Tutorial.Completed";
        private const int MoneyGrantAmount = 500;

        [MenuItem("KotoPulta/Reset All PlayerPrefs")]
        private static void ResetAllPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            if (Application.isPlaying)
            {
                ResetRuntimeMoney();
                ResetRuntimeUpgrades();
            }

            Debug.Log("All PlayerPrefs have been reset.");
        }

        [MenuItem("KotoPulta/Reset Tutorial")]
        private static void ResetTutorial()
        {
            PlayerPrefs.DeleteKey(TutorialCompletedKey);
            PlayerPrefs.Save();
            Debug.Log("Tutorial progress has been reset.");
        }

        [MenuItem("KotoPulta/Reset Money")]
        private static void ResetMoney()
        {
            if (Application.isPlaying && ServiceLocator.TryGet<BalanceService>(out var balance))
                balance.SetBalance(0);
            else
            {
                PlayerPrefs.DeleteKey("Progression.Balance");
                PlayerPrefs.Save();
            }

            Debug.Log("Money has been reset.");
        }

        [MenuItem("KotoPulta/Reset All Upgrades")]
        private static void ResetAllUpgrades()
        {
            if (Application.isPlaying && ServiceLocator.TryGet<UpgradeService>(out var upgrades))
                upgrades.ResetAll();
            else
            {
                DeleteUpgradeKeys();
                PlayerPrefs.Save();
            }

            Debug.Log("All upgrades have been reset.");
        }

        [MenuItem("KotoPulta/Give 500 Money")]
        private static void Give500Money()
        {
            if (!Application.isPlaying || !ServiceLocator.TryGet<BalanceService>(out var balance))
            {
                Debug.LogWarning("Enter Play Mode to give money through BalanceService.");
                return;
            }

            balance.Add(MoneyGrantAmount);
            Debug.Log($"Added {MoneyGrantAmount} money.");
        }

        private static void ResetRuntimeMoney()
        {
            if (ServiceLocator.TryGet<BalanceService>(out var balance))
                balance.SetBalance(0);
        }

        private static void ResetRuntimeUpgrades()
        {
            if (ServiceLocator.TryGet<UpgradeService>(out var upgrades))
                upgrades.ResetAll();
        }

        private static void DeleteUpgradeKeys()
        {
            PlayerPrefs.DeleteKey("Progression.Upgrade.WaiterCount");
            PlayerPrefs.DeleteKey("Progression.Upgrade.WaiterSpeed");
            PlayerPrefs.DeleteKey("Progression.Upgrade.TableCount");
            PlayerPrefs.DeleteKey("Progression.Upgrade.ExpensiveOrderChance");
        }
    }
}
