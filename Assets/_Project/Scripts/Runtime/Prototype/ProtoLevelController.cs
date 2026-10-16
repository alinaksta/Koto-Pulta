using Game.Characters;
using Game.Progression;
using Game.Services;
using Game.UI;
using System.Collections;
using UnityEngine;

namespace Game.Prototype
{
    /// <summary>
    /// Scene setup for the Proto-level sandbox: cashier queue flow, no waiters, direct hand delivery.
    /// </summary>
    /// <remarks>
    /// - Disables every waiter in the scene (the player carries food to customers by hand).
    /// - Switches the persistent game mode to the shift mode (skips the tutorial).
    /// - Optionally starts a shift right away and sends all customers home when the shift ends.
    /// </remarks>
    public sealed class ProtoLevelController : MonoBehaviour
    {
        [SerializeField] private CustomerQueue _queue;
        [SerializeField] private bool _disableWaiters = true;
        [SerializeField] private bool _autoStartShift = true;
        [SerializeField] private string _shiftModeId = "shift";
        [Tooltip("Only the meals tab stays on the computer; shifts are started from level select.")]
        [SerializeField] private bool _mealsTabOnly = true;
        [Tooltip("Show the stars/Retry/Level select screen when the shift ends.")]
        [SerializeField] private bool _showResultsScreen = true;

        private ShiftService _shiftService;

        private void Awake()
        {
            if (_queue == null)
                _queue = FindAnyObjectByType<CustomerQueue>();

            if (_disableWaiters)
                DisableWaiters();
        }

        private IEnumerator Start()
        {
            // Give the persistent services one frame to finish their own startup (GameModeService auto-start).
            yield return null;

            // (Re-)enter the shift mode so every visit to this scene starts from a clean state.
            if (ServiceLocator.TryGet<GameModeService>(out var gameModes) && !gameModes.TrySetGameMode(_shiftModeId))
                Debug.LogWarning($"[{nameof(ProtoLevelController)}] Could not switch to game mode '{_shiftModeId}'.", this);

            if (ServiceLocator.TryGet(out _shiftService))
            {
                _shiftService.OnShiftEnded += HandleShiftEnded;

                if (_autoStartShift && !_shiftService.ShiftInProgress)
                {
                    int levelIndex = Mathf.Clamp(ProtoProgress.SelectedLevelIndex, 0, Mathf.Max(0, _shiftService.ShiftAmount - 1));
                    ProtoProgress.SelectedLevelIndex = levelIndex;

                    // Level-select runs don't advance the main game's saved shift progression.
                    if (!_shiftService.TryStartShiftAt(levelIndex, saveProgress: false))
                        Debug.LogWarning($"[{nameof(ProtoLevelController)}] Could not start level {levelIndex + 1}.", this);
                }
            }

            if (_mealsTabOnly)
                KeepOnlyMealsTab();
        }

        private static void KeepOnlyMealsTab()
        {
            var computers = FindObjectsByType<SiteActivator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var computer in computers)
            {
                computer.HideTab(ComputerSiteTab.ShiftStatistics);
                computer.HideTab(ComputerSiteTab.Shop);
                computer.RefreshTabAvailability();

                if (computer.IsTabEnabled(ComputerSiteTab.Meals))
                    computer.SetTab(ComputerSiteTab.Meals);
            }
        }

        private void OnDestroy()
        {
            if (_shiftService != null)
                _shiftService.OnShiftEnded -= HandleShiftEnded;
        }

        private void HandleShiftEnded()
        {
            if (_queue != null)
                _queue.DismissAll();

            if (_showResultsScreen && _shiftService != null)
            {
                var results = gameObject.AddComponent<ProtoShiftResultsScreen>();
                results.Show(ProtoProgress.SelectedLevelIndex, _shiftService.CurrentRevenue, _shiftService.CurrentGoalRevenue);
            }
        }

        private static void DisableWaiters()
        {
            var waiters = FindObjectsByType<Waiter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var waiter in waiters)
                waiter.gameObject.SetActive(false);
        }
    }
}
