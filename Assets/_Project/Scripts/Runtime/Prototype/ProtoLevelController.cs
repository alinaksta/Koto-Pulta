using Game.Characters;
using Game.Progression;
using Game.Services;
using Game.UI;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

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
        [Tooltip("Hide the persistent money counter, shift number and timer; move or hide the quota label.")]
        [SerializeField] private bool _simplifyHud = true;
        [Tooltip("Timer and quota are stacked in the top-right corner with the money label's font size.")]
        [SerializeField] private Vector2 _cornerMargin = new Vector2(40f, 40f);
        [SerializeField] private Vector2 _cornerLabelSize = new Vector2(820f, 90f);
        [SerializeField] private string _moneyLabelName = "BalanceText";
        [SerializeField] private string _quotaLabelName = "RevenueLabel";
        [SerializeField] private string _shiftNumberLabelName = "ShiftNumberLabel";
        [SerializeField] private string _timerLabelName = "TimerLabel";

        [Header("Order board")]
        [Tooltip("Order notes strip (Prefabs/UI/Board). Spawned on the shift HUD canvas.")]
        [SerializeField] private OrderBoardView _orderBoardPrefab;
        [Tooltip("Vertical offset from the top of the screen.")]
        [SerializeField] private float _orderBoardTopOffset = 0f;
        [Tooltip("Uniform scale of the order board (notes and spacing).")]
        [SerializeField] private float _orderBoardScale = 0.9f;

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
            DisableClockTicks();
            SpawnOrderBoard();

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
            TMP_Text timer = FindLabel(_timerLabelName);

            if (shiftNumber != null)
                shiftNumber.gameObject.SetActive(false);

            // Timer on top, quota below it, both in the top-right corner (the order board owns the top-left).
            float fontSize = money != null ? money.fontSize : 72f;
            PlaceInTopRightCorner(timer, 0, fontSize);
            PlaceInTopRightCorner(quota, 1, fontSize);

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

        private void PlaceInTopRightCorner(TMP_Text label, int row, float fontSize)
        {
            if (label == null)
                return;

            RectTransform rect = label.rectTransform;
            rect.SetParent(label.canvas.rootCanvas.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = _cornerLabelSize;
            rect.anchoredPosition = new Vector2(-_cornerMargin.x, -_cornerMargin.y - row * _cornerLabelSize.y);
            rect.localScale = Vector3.one;

            label.enableAutoSizing = false;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.TopRight;
            label.gameObject.SetActive(true);
        }

        private static void DisableClockTicks()
        {
            foreach (var audio in FindObjectsByType<ProgressionAudio>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                audio.PlayTimerTicks = false;
        }

        private void SpawnOrderBoard()
        {
            if (_orderBoardPrefab == null || ServiceLocator.TryGet(out OrderBoardView _))
                return;

            var hud = FindAnyObjectByType<UIShiftHud>(FindObjectsInactive.Include);
            Canvas canvas = hud != null ? hud.GetComponentInParent<Canvas>(true) : null;
            if (canvas == null)
            {
                Debug.LogWarning($"[{nameof(ProtoLevelController)}] No HUD canvas found for the order board.", this);
                return;
            }

            OrderBoardView board = Instantiate(_orderBoardPrefab, canvas.rootCanvas.transform, false);
            var rect = (RectTransform)board.transform;

            // Notes line up from the top-left corner; scaling also shrinks toward that corner.
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, _orderBoardTopOffset);
            rect.localScale = Vector3.one * _orderBoardScale;
            if (board.TryGetComponent(out HorizontalLayoutGroup layout))
                layout.childAlignment = TextAnchor.UpperLeft;
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
