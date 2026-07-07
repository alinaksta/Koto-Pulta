using Game.Services;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Progression
{
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

        [Header("Ended Panel Dependencies")]
        [SerializeField] private RectTransform _endedPanel;
        [SerializeField] private Button _startNextShiftButton;
        [SerializeField] private TextMeshProUGUI _happyCustomersText;
        [SerializeField] private TextMeshProUGUI _angryCustomersText;
        [SerializeField] private TextMeshProUGUI _averageTimeText;
        [SerializeField] private TextMeshProUGUI _moneyEarnedText;

        [Header("In Progress Panel Dependencies")]
        [SerializeField] private RectTransform _inProgressPanel;

        private ShiftService _shiftService;
        private ComputerShiftState _state;

        private void Awake()
        {
            _shiftService = ServiceLocator.Get<ShiftService>();
        }

        private void Start()
        {
            _shiftService.OnShiftStarted += HandleShiftStarted;
            _shiftService.OnShiftEnded += HandleShiftEnded;

            _beginFirstShiftButton.onClick.AddListener(HandleStartNextShiftButtonClick);
            _startNextShiftButton.onClick.AddListener(HandleStartNextShiftButtonClick);

            _state = ComputerShiftState.Initial;
            UpdateUI();
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

        private void UpdateUI()
        {
            if (_state == ComputerShiftState.Initial)
                UpdateInitialUI();
            else if (_state == ComputerShiftState.Ended)
                UpdateEndedUI();
            else
                UpdateInProgressUI();
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
            var stats = _shiftService.LastStatistics;
            _angryCustomersText.text = stats.CustomersUnsatisfied.ToString();
            _happyCustomersText.text = stats.CustomersServed.ToString();
            _averageTimeText.text = stats.AverageDeliveryTime.ToString("F1");
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
