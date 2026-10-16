using Game.Characters;
using Game.Progression;
using Game.Services;
using Game.UI;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

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

        [Header("HUD")]
        [Tooltip("Hide the persistent money counter, hide the shift number and move the quota label into the money's corner.")]
        [SerializeField] private bool _simplifyHud = true;
        [SerializeField] private string _moneyLabelName = "BalanceText";
        [SerializeField] private string _quotaLabelName = "RevenueLabel";
        [SerializeField] private string _shiftNumberLabelName = "ShiftNumberLabel";

        [Header("Pause")]
        [SerializeField] private AudioMixer _audioMixer;

        private ProtoPauseMenu _pauseMenu;

        private ShiftService _shiftService;

        private void Awake()
        {
            if (_queue == null)
                _queue = FindAnyObjectByType<CustomerQueue>();

            if (_disableWaiters)
                DisableWaiters();

            ProtoAudioSettings.ApplyMaster();

            if (_simplifyHud)
                SimplifyHud();

            _pauseMenu = gameObject.AddComponent<ProtoPauseMenu>();
            _pauseMenu.SetMixer(_audioMixer);
        }

        private void SimplifyHud()
        {
            TMP_Text money = FindLabel(_moneyLabelName);
            TMP_Text quota = FindLabel(_quotaLabelName);
            TMP_Text shiftNumber = FindLabel(_shiftNumberLabelName);

            if (shiftNumber != null)
                shiftNumber.gameObject.SetActive(false);

            if (quota != null && money != null)
            {
                // Take over the money label's spot (top-left corner) on the shift HUD's own canvas.
                RectTransform from = money.rectTransform;
                RectTransform to = quota.rectTransform;
                to.SetParent(quota.canvas.rootCanvas.transform, false);
                to.anchorMin = from.anchorMin;
                to.anchorMax = from.anchorMax;
                to.pivot = from.pivot;
                to.sizeDelta = new Vector2(Mathf.Max(from.sizeDelta.x, 420f), from.sizeDelta.y);
                to.anchoredPosition = from.anchoredPosition;
                quota.alignment = money.alignment;
            }

            if (money != null)
            {
                Canvas moneyCanvas = money.canvas != null ? money.canvas.rootCanvas : null;
                bool sharedCanvas = quota != null && moneyCanvas == quota.canvas.rootCanvas;
                if (moneyCanvas != null && !sharedCanvas)
                    moneyCanvas.gameObject.SetActive(false);
                else
                    money.gameObject.SetActive(false);
            }
        }

        private static TMP_Text FindLabel(string objectName)
        {
            foreach (var label in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (label.name == objectName && label.gameObject.scene.IsValid())
                    return label;
            }

            return null;
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
                if (_pauseMenu != null)
                {
                    _pauseMenu.Resume();
                    _pauseMenu.CanPause = false;
                }

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
