using Game.Progression;
using Game.Services;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private TMP_Text _shiftEndedLabel;
        [SerializeField] private Image _moneyIcon;

        [Header("Timer Warning")]
        [SerializeField, Min(0f)] private float _timerWarningSeconds = 30f;
        [SerializeField, Min(0f)] private float _timerBlinkFrequency = 8f;
        [SerializeField] private Color _timerWarningColor = Color.red;
        [SerializeField] private Color _revenueMetColor = Color.green;

        [Header("Shift End Message")]
        [SerializeField] private string _shiftEndedText = "Shift ended";
        [SerializeField, Min(0f)] private float _shiftEndedFadeInDuration = 0.35f;
        [SerializeField, Min(0f)] private float _shiftEndedVisibleDuration = 2f;
        [SerializeField, Min(0f)] private float _shiftEndedFadeOutDuration = 0.35f;

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
        private Coroutine _shiftEndedCoroutine;
        private Color _timerBaseColor = Color.white;
        private Color _revenueBaseColor = Color.white;
        private bool _hasTimerBaseColor;
        private bool _hasRevenueBaseColor;
        private int _displayedTimerSeconds = int.MinValue;
        private int _displayedRevenue = int.MinValue;
        private int _displayedGoalRevenue = int.MinValue;
        private int _displayedShiftNumber = int.MinValue;
        private int _displayedShiftAmount = int.MinValue;
        private bool _displayedPracticeShift;
        private bool _displayedEndlessRevenue;
        private bool _displayedEndlessShift;

        private void Awake()
        {
            _shownAnchoredPosition = _target.anchoredPosition;
            _hiddenAnchoredPosition = _shownAnchoredPosition + Vector2.up * _hiddenYOffset;

            if (_timerLabel != null)
            {
                _timerBaseColor = _timerLabel.color;
                _hasTimerBaseColor = true;
            }

            if (_revenueLabel != null)
            {
                _revenueBaseColor = _revenueLabel.color;
                _hasRevenueBaseColor = true;
            }

            EnsureShiftEndedLabel();
            SetShiftEndedAlpha(0f);
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

            if (_shiftEndedCoroutine != null)
            {
                StopCoroutine(_shiftEndedCoroutine);
                _shiftEndedCoroutine = null;
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
            if (_shiftEndedCoroutine != null)
            {
                StopCoroutine(_shiftEndedCoroutine);
                _shiftEndedCoroutine = null;
            }

            SetShiftEndedAlpha(0f);
            ResetTimerColor();
            RefreshAll();
            SlideTo(_shownAnchoredPosition);
        }

        private void HandleShiftEnded()
        {
            RefreshAll();

            if (_shiftEndedCoroutine != null)
                StopCoroutine(_shiftEndedCoroutine);

            _shiftEndedCoroutine = StartCoroutine(ShiftEndedRoutine());
        }

        private void RefreshAll()
        {
            InvalidateDisplayCache();
            UpdateTimer();
            UpdateRevenue();
            UpdateShiftNumber();
        }

        private void InvalidateDisplayCache()
        {
            _displayedTimerSeconds = int.MinValue;
            _displayedRevenue = int.MinValue;
            _displayedGoalRevenue = int.MinValue;
            _displayedShiftNumber = int.MinValue;
            _displayedShiftAmount = int.MinValue;
            _displayedPracticeShift = false;
            _displayedEndlessRevenue = false;
            _displayedEndlessShift = false;
        }

        private void UpdateTimer()
        {
            if (_timerLabel == null || _shiftService == null)
                return;

            if (_shiftService.IsPracticeShift)
            {
                if (!_displayedPracticeShift)
                    _timerLabel.text = _untimedTimerText;

                _displayedPracticeShift = true;
                _displayedTimerSeconds = int.MinValue;
                return;
            }

            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(_shiftService.ShiftTimer));
            if (_displayedPracticeShift || totalSeconds != _displayedTimerSeconds)
            {
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                _timerLabel.text = string.Format(_timerFormat, minutes, seconds);
                _displayedTimerSeconds = totalSeconds;
                _displayedPracticeShift = false;
            }

            UpdateTimerColor(totalSeconds);
        }

        private void UpdateRevenue()
        {
            if (_revenueLabel == null || _shiftService == null)
                return;

            if (_shiftService.IsEndlessShift)
            {
                if (!_displayedEndlessRevenue)
                {
                    _revenueLabel.text = _endlessRevenueText;
                    ResetRevenueColor();
                }

                _displayedEndlessRevenue = true;
                _displayedRevenue = int.MinValue;
                _displayedGoalRevenue = int.MinValue;
                return;
            }

            int revenue = _shiftService.CurrentRevenue;
            int goalRevenue = _shiftService.CurrentGoalRevenue;
            if (_displayedEndlessRevenue || revenue != _displayedRevenue || goalRevenue != _displayedGoalRevenue)
            {
                _revenueLabel.text = string.Format(_revenueFormat, revenue, goalRevenue);
                _displayedRevenue = revenue;
                _displayedGoalRevenue = goalRevenue;
                _displayedEndlessRevenue = false;
                UpdateRevenueColor();
            }

        }

        private void UpdateShiftNumber()
        {
            if (_shiftNumberLabel == null || _shiftService == null)
                return;

            int displayedShiftNumber = _shiftService.HasCurrentShift
                ? _shiftService.ShiftIndex + 1
                : 0;

            bool endlessShift = _shiftService.IsEndlessShift;
            int shiftAmount = _shiftService.ShiftAmount;
            if (displayedShiftNumber == _displayedShiftNumber &&
                shiftAmount == _displayedShiftAmount &&
                endlessShift == _displayedEndlessShift)
                return;

            string format = endlessShift
                ? _endlessShiftNumberFormat
                : _shiftNumberFormat;
            _shiftNumberLabel.text = string.Format(
                format,
                displayedShiftNumber,
                shiftAmount);
            _displayedShiftNumber = displayedShiftNumber;
            _displayedShiftAmount = shiftAmount;
            _displayedEndlessShift = endlessShift;
        }

        private bool TryResolveService()
        {
            if (_shiftService != null)
                return true;

            return ServiceLocator.TryGet(out _shiftService);
        }

        private void EnsureShiftEndedLabel()
        {
            if (_shiftEndedLabel != null)
                return;

            Transform parent = _target != null ? _target : transform;
            var labelObject = new GameObject("ShiftEndedLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.layer = parent.gameObject.layer;
            labelObject.transform.SetParent(parent, false);

            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(360f, 80f);

            _shiftEndedLabel = labelObject.GetComponent<TextMeshProUGUI>();
            _shiftEndedLabel.text = _shiftEndedText;
            _shiftEndedLabel.alignment = TextAlignmentOptions.Center;
            _shiftEndedLabel.raycastTarget = false;

            if (_timerLabel != null)
            {
                _shiftEndedLabel.font = _timerLabel.font;
                _shiftEndedLabel.fontSize = _timerLabel.fontSize * 1.25f;
                _shiftEndedLabel.fontStyle = FontStyles.Bold;
                _shiftEndedLabel.color = _timerLabel.color;
            }
        }

        private void SlideTo(Vector2 targetPosition)
        {
            if (_slideCoroutine != null)
                StopCoroutine(_slideCoroutine);

            _slideCoroutine = StartCoroutine(SlideToRoutine(targetPosition));
        }

        private IEnumerator ShiftEndedRoutine()
        {
            SetTimerColor(_timerWarningColor);

            if (_shiftEndedLabel != null)
                _shiftEndedLabel.text = _shiftEndedText;

            yield return FadeShiftEndedAsync(1f, _shiftEndedFadeInDuration);

            if (_shiftEndedVisibleDuration > 0f)
                yield return new WaitForSeconds(_shiftEndedVisibleDuration);

            yield return FadeShiftEndedAsync(0f, _shiftEndedFadeOutDuration);

            _shiftEndedCoroutine = null;
            SlideTo(_hiddenAnchoredPosition);
        }

        private IEnumerator FadeShiftEndedAsync(float targetAlpha, float duration)
        {
            if (_shiftEndedLabel == null)
            {
                if (duration > 0f)
                    yield return new WaitForSeconds(duration);

                yield break;
            }

            float startAlpha = _shiftEndedLabel.color.a;
            if (duration <= 0f)
            {
                SetShiftEndedAlpha(targetAlpha);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetShiftEndedAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
                yield return null;
            }

            SetShiftEndedAlpha(targetAlpha);
        }

        private void UpdateTimerColor(int totalSeconds)
        {
            if (_timerLabel == null || _shiftService == null)
                return;

            if (!_hasTimerBaseColor)
            {
                _timerBaseColor = _timerLabel.color;
                _hasTimerBaseColor = true;
            }

            if (!_shiftService.ShiftInProgress || totalSeconds <= 0)
            {
                SetTimerColor(_timerWarningColor);
                return;
            }

            if (_timerWarningSeconds <= 0f || totalSeconds > _timerWarningSeconds)
            {
                ResetTimerColor();
                return;
            }

            float blink = (Mathf.Sin(Time.time * _timerBlinkFrequency) + 1f) * 0.5f;
            SetTimerColor(Color.Lerp(_timerBaseColor, _timerWarningColor, blink));
        }

        private void ResetTimerColor()
        {
            if (_timerLabel != null && _hasTimerBaseColor)
                _timerLabel.color = _timerBaseColor;
        }

        private void UpdateRevenueColor()
        {
            if (_revenueLabel == null || _shiftService == null)
                return;

            if (!_hasRevenueBaseColor)
            {
                _revenueBaseColor = _revenueLabel.color;
                _hasRevenueBaseColor = true;
            }

            bool revenueMet = _shiftService.CurrentGoalRevenue > 0 &&
                              _shiftService.CurrentRevenue >= _shiftService.CurrentGoalRevenue;
            _revenueLabel.color = revenueMet ? _revenueMetColor : _revenueBaseColor;
            _moneyIcon.color = revenueMet ? _revenueMetColor : _revenueBaseColor;
        }

        private void ResetRevenueColor()
        {
            if (_revenueLabel != null && _hasRevenueBaseColor)
                _revenueLabel.color = _revenueBaseColor;
        }

        private void SetTimerColor(Color color)
        {
            if (_timerLabel != null)
                _timerLabel.color = color;
        }

        private void SetShiftEndedAlpha(float alpha)
        {
            if (_shiftEndedLabel == null)
                return;

            Color color = _shiftEndedLabel.color;
            color.a = alpha;
            _shiftEndedLabel.color = color;
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
