using Game.Interaction;
using Game.Services;
using Game.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Progression
{
    /// <summary>
    /// Displays shift state and results on the computer UI.
    /// </summary>
    public class ComputerShiftView : MonoBehaviour
    {
        private enum ComputerShiftState
        {
            Initial,
            Ended,
            InProgress
        }

        [Header("Initial Panel Dependencies")]
        [SerializeField] private RectTransform _initialPanel;
        [SerializeField] private Button _beginFirstShiftButton;
        [SerializeField] private TextMeshProUGUI _beginFirstShiftButtonText;

        [Header("Ended Panel Dependencies")]
        [SerializeField] private RectTransform _endedPanel;
        [SerializeField] private Button _startNextShiftButton;
        [SerializeField] private TextMeshProUGUI _startNextShiftButtonText;
        [SerializeField] private TextMeshProUGUI _happyCustomersText;
        [SerializeField] private TextMeshProUGUI _angryCustomersText;
        [SerializeField] private TextMeshProUGUI _averageTimeText;
        [SerializeField] private TextMeshProUGUI _moneyEarnedText;
        [SerializeField] private SiteActivator _computer;

        [Header("Button Labels")]
        [SerializeField] private string _beginShiftButtonLabel = "Begin Shift";
        [SerializeField] private string _retryShiftButtonLabel = "Retry";
        [SerializeField] private string _beginNextShiftButtonLabel = "Begin Next Shift";

        [Header("In Progress Panel Dependencies")]
        [SerializeField] private RectTransform _inProgressPanel;

        private ShiftService _shiftService;
        private ComputerShiftState _state;

        private void Awake()
        {
            _shiftService = ServiceLocator.Get<ShiftService>();
            CacheButtonTextReferences();
        }

        private void OnEnable()
        {
            if (_shiftService == null)
                return;

            RefreshStateFromShiftService();
            UpdateUI();
        }

        private void CacheButtonTextReferences()
        {
            if (_beginFirstShiftButtonText == null && _beginFirstShiftButton != null)
                _beginFirstShiftButtonText = _beginFirstShiftButton.GetComponentInChildren<TextMeshProUGUI>(true);

            if (_startNextShiftButtonText == null && _startNextShiftButton != null)
                _startNextShiftButtonText = _startNextShiftButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void Start()
        {
            _shiftService.OnShiftStarted += HandleShiftStarted;
            _shiftService.OnShiftEnded += HandleShiftEnded;
            _shiftService.OnShiftStartAvailabilityChanged += HandleShiftStartAvailabilityChanged;

            _beginFirstShiftButton.onClick.AddListener(HandleStartNextShiftButtonClick);
            _startNextShiftButton.onClick.AddListener(HandleStartNextShiftButtonClick);

            RefreshStateFromShiftService();
            UpdateUI();
        }

        private void RefreshStateFromShiftService()
        {
            if (_shiftService.ShiftInProgress)
                _state = ComputerShiftState.InProgress;
            else if (_shiftService.HasCompletedShiftResults)
                _state = ComputerShiftState.Ended;
            else
                _state = ComputerShiftState.Initial;
        }

        private void HandleStartNextShiftButtonClick()
        {
            if (!_shiftService.TryStartNextShift())
            {
                Debug.LogError("Shift not started");
            }
        }

        private void OnDestroy()
        {
            _shiftService.OnShiftStarted -= HandleShiftStarted;
            _shiftService.OnShiftEnded -= HandleShiftEnded;
            _shiftService.OnShiftStartAvailabilityChanged -= HandleShiftStartAvailabilityChanged;

            _beginFirstShiftButton.onClick.RemoveListener(HandleStartNextShiftButtonClick);
            _startNextShiftButton.onClick.RemoveListener(HandleStartNextShiftButtonClick);
        }

        private void HandleShiftEnded()
        {
            _state = ComputerShiftState.Ended;
            UpdateUI();
        }

        private void HandleShiftStarted()
        {
            _state = ComputerShiftState.InProgress;
            UpdateUI();
        }

        private void HandleShiftStartAvailabilityChanged()
        {
            UpdateShiftButtonStates();
        }

        private void UpdateUI()
        {
            if (_state == ComputerShiftState.Initial)
                UpdateInitialUI();
            else if (_state == ComputerShiftState.Ended)
                UpdateEndedUI();
            else
                UpdateInProgressUI();

            UpdateShiftButtonStates();
        }

        private void UpdateShiftButtonStates()
        {
            bool canStart = _shiftService != null && _shiftService.CanStartNextShift;

            if (_beginFirstShiftButton != null)
                _beginFirstShiftButton.interactable = canStart;

            if (_startNextShiftButton != null)
                _startNextShiftButton.interactable = canStart;

            UpdateShiftButtonLabels();
        }

        private void UpdateShiftButtonLabels()
        {
            if (_beginFirstShiftButtonText != null)
                _beginFirstShiftButtonText.text = _beginShiftButtonLabel;

            if (_startNextShiftButtonText == null)
                return;

            _startNextShiftButtonText.text = _shiftService != null && _shiftService.CurrentShiftFailed
                ? _retryShiftButtonLabel
                : _beginNextShiftButtonLabel;
        }

        private void EnableSinglePanel(RectTransform panel)
        {
            _initialPanel.gameObject.SetActive(false);
            _endedPanel.gameObject.SetActive(false);
            _inProgressPanel.gameObject.SetActive(false);

            panel.gameObject.SetActive(true);
        }

        #region Updating individual panels
        private void UpdateEndedUI()
        {
            EnableSinglePanel(_endedPanel);

            ShiftStatistics stats = _shiftService.LastStatistics;

            if (_happyCustomersText != null)
                _happyCustomersText.text = stats.CustomersServed.ToString();

            if (_angryCustomersText != null)
                _angryCustomersText.text = stats.CustomersUnsatisfied.ToString();

            if (_averageTimeText != null)
                _averageTimeText.text = ((int)stats.AverageDeliveryTime).ToString();

            if (_moneyEarnedText != null)
                _moneyEarnedText.text = stats.MoneyEarned.ToString();
        }

        private void UpdateInitialUI()
        {
            EnableSinglePanel(_initialPanel);
        }

        private void UpdateInProgressUI()
        {
            EnableSinglePanel(_inProgressPanel);
        }
        #endregion
    }
}
