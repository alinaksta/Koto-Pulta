using Game.Progression;
using Game.Services;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Displays the current shift timer, revenue, and shift number.
    /// </summary>
    public class UIShiftHud : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float _slideDuration = 0.35f;
        [SerializeField] private float _hiddenYOffset = 160f;
        [SerializeField] private RectTransform _target;
        [SerializeField] private AnimationCurve _slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Labels")]
        [SerializeField] private TMP_Text _timerLabel;
        [SerializeField] private TMP_Text _revenueLabel;
        [SerializeField] private TMP_Text _shiftNumberLabel;

        [Header("Format")]
        [SerializeField] private string _timerFormat = "{0:00}:{1:00}";
        [SerializeField] private string _untimedTimerText = "--:--";
        [SerializeField] private string _revenueFormat = "Revenue {0} / {1}";
        [SerializeField] private string _endlessRevenueText = "No revenue goal";
        [SerializeField] private string _shiftNumberFormat = "Shift {0} / {1}";
        [SerializeField] private string _endlessShiftNumberFormat = "Shift {0}";

        private ShiftService _shiftService;
        private Vector2 _shownAnchoredPosition;
        private Vector2 _hiddenAnchoredPosition;
        private Coroutine _slideCoroutine;

        private void Awake()
        {
            _shownAnchoredPosition = _target.anchoredPosition;
            _hiddenAnchoredPosition = _shownAnchoredPosition + Vector2.up * _hiddenYOffset;
            TryResolveService();
        }

        private void Start()
        {
            SetAnchoredPosition(_hiddenAnchoredPosition);
        }

        private void OnEnable()
        {
            if (!TryResolveService())
                return;

            _shiftService.OnShiftStarted += HandleShiftStarted;
            _shiftService.OnShiftEnded += HandleShiftEnded;
            RefreshAll();
        }

        private void OnDisable()
        {
            if (_slideCoroutine != null)
            {
                StopCoroutine(_slideCoroutine);
                _slideCoroutine = null;
            }

            if (_shiftService == null)
                return;

            _shiftService.OnShiftStarted -= HandleShiftStarted;
            _shiftService.OnShiftEnded -= HandleShiftEnded;
        }

        private void Update()
        {
            if (_shiftService == null)
            {
                if (!TryResolveService())
                    return;

                _shiftService.OnShiftStarted += HandleShiftStarted;
                _shiftService.OnShiftEnded += HandleShiftEnded;
                RefreshAll();
            }

            UpdateTimer();
            UpdateRevenue();
            UpdateShiftNumber();
        }

        private void HandleShiftStarted()
        {
            RefreshAll();
            SlideTo(_shownAnchoredPosition);
        }

        private void HandleShiftEnded()
        {
            RefreshAll();
            SlideTo(_hiddenAnchoredPosition);
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

            if (_shiftService.IsPracticeShift)
            {
                _timerLabel.text = _untimedTimerText;
                return;
            }

            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(_shiftService.ShiftTimer));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            _timerLabel.text = string.Format(_timerFormat, minutes, seconds);
        }

        private void UpdateRevenue()
        {
            if (_revenueLabel == null || _shiftService == null)
                return;

            if (_shiftService.IsEndlessShift)
            {
                _revenueLabel.text = _endlessRevenueText;
                return;
            }

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

            string format = _shiftService.IsEndlessShift
                ? _endlessShiftNumberFormat
                : _shiftNumberFormat;
            _shiftNumberLabel.text = string.Format(
                format,
                displayedShiftNumber,
                _shiftService.ShiftAmount);
        }

        private bool TryResolveService()
        {
            if (_shiftService != null)
                return true;

            return ServiceLocator.TryGet(out _shiftService);
        }

        private void SlideTo(Vector2 targetPosition)
        {
            if (_slideCoroutine != null)
                StopCoroutine(_slideCoroutine);

            _slideCoroutine = StartCoroutine(SlideToRoutine(targetPosition));
        }

        private IEnumerator SlideToRoutine(Vector2 targetPosition)
        {
            Vector2 startPosition = _target.anchoredPosition;

            if (_slideDuration <= 0f)
            {
                SetAnchoredPosition(targetPosition);
                _slideCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < _slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _slideDuration);
                float curveT = _slideCurve != null ? _slideCurve.Evaluate(t) : t;
                SetAnchoredPosition(Vector2.LerpUnclamped(startPosition, targetPosition, curveT));
                yield return null;
            }

            SetAnchoredPosition(targetPosition);
            _slideCoroutine = null;
        }

        private void SetAnchoredPosition(Vector2 position)
        {
            if (_target != null)
                _target.anchoredPosition = position;
        }
    }
}
