using Game.Progression;
using Game.Services;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Displays the current shift timer, revenue, and shift number.
    /// </summary>
    public class UIShiftHud : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TMP_Text _timerLabel;
        [SerializeField] private TMP_Text _revenueLabel;
        [SerializeField] private TMP_Text _shiftNumberLabel;

        [Header("Format")]
        [SerializeField] private string _timerFormat = "{0:00}:{1:00}";
        [SerializeField] private string _revenueFormat = "Revenue {0} / {1}";
        [SerializeField] private string _shiftNumberFormat = "Shift {0} / {1}";

        private ShiftService _shiftService;

        private void Awake()
        {
            TryResolveService();
        }

        private void OnEnable()
        {
            if (!TryResolveService())
                return;

            _shiftService.OnShiftStarted += RefreshAll;
            _shiftService.OnShiftEnded += RefreshAll;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (_shiftService == null)
                return;

            _shiftService.OnShiftStarted -= RefreshAll;
            _shiftService.OnShiftEnded -= RefreshAll;
        }

        private void Update()
        {
            if (_shiftService == null)
            {
                if (!TryResolveService())
                    return;

                _shiftService.OnShiftStarted += RefreshAll;
                _shiftService.OnShiftEnded += RefreshAll;
                RefreshAll();
            }

            UpdateTimer();
            UpdateRevenue();
            UpdateShiftNumber();
        }

        private void RefreshAll()
        {
            UpdateTimer();
            UpdateRevenue();
            UpdateShiftNumber();
        }

        private void UpdateTimer()
        {
            if (_timerLabel == null || _shiftService == null)
                return;

            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(_shiftService.ShiftTimer));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            _timerLabel.text = string.Format(_timerFormat, minutes, seconds);
        }

        private void UpdateRevenue()
        {
            if (_revenueLabel == null || _shiftService == null)
                return;

            _revenueLabel.text = string.Format(
                _revenueFormat,
                _shiftService.CurrentRevenue,
                _shiftService.CurrentGoalRevenue);
        }

        private void UpdateShiftNumber()
        {
            if (_shiftNumberLabel == null || _shiftService == null)
                return;

            int displayedShiftNumber = _shiftService.HasCurrentShift
                ? _shiftService.ShiftIndex + 1
                : 0;

            _shiftNumberLabel.text = string.Format(
                _shiftNumberFormat,
                displayedShiftNumber,
                _shiftService.ShiftAmount);
        }

        private bool TryResolveService()
        {
            if (_shiftService != null)
                return true;

            return ServiceLocator.TryGet(out _shiftService);
        }
    }
}
