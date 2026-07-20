using Game.Services;
using Game.Progression;
using Game.Interaction;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Evaluation : MonoBehaviour
{
    private ShiftService _shiftService;
    private RunSessionService _runSessionService;
    [SerializeField] ComputerInteractable _computerInteractable;
    private bool _evaluatable = true;
    [SerializeField] private Image[] _segments;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Sprite _successSprite;
    [SerializeField] private Sprite _failSprite;


    [SerializeField] private TMP_Text _customersServedText;
    [SerializeField] private TMP_Text _customersUnsatisfiedText;
    [SerializeField] private TMP_Text _deliveryTimeText;
    [SerializeField] private TMP_Text _moneyEarnedText;
    [SerializeField] private GameObject _evaluateButton;
    [SerializeField] private GameObject _nextShiftButton;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float _countAnimationDuration = 1.5f;
    [SerializeField, Min(1)] private int _countAnimationSteps = 30;

    private Coroutine _evaluationCoroutine;
    private bool _evaluationPending;
    private int _evaluatedShiftIndex = -1;
    private RunSessionState _displayedSessionState = (RunSessionState)(-1);

    private void Awake()
    {
        _shiftService = ServiceLocator.Get<ShiftService>();
        _runSessionService = ServiceLocator.Get<RunSessionService>();
    }

    private void OnEnable()
    {
        _shiftService.OnShiftStarted += HandleShiftStarted;
        _shiftService.OnShiftEnded += HandleShiftEnded;

        if (_computerInteractable != null)
            _computerInteractable.FocusStarted += HandleComputerFocusStarted;

        if (_shiftService.HasCurrentShift && !_shiftService.ShiftInProgress && _runSessionService.State != RunSessionState.Running)
        {
            if (_shiftService.ShiftIndex != _evaluatedShiftIndex)
                _evaluatable = true;

            _evaluationPending = true;
            TryStartPendingEvaluation();
        }
    }

    private void OnDisable()
    {
        _shiftService.OnShiftStarted -= HandleShiftStarted;
        _shiftService.OnShiftEnded -= HandleShiftEnded;

        if (_computerInteractable != null)
            _computerInteractable.FocusStarted -= HandleComputerFocusStarted;
    }

    private void Update()
    {
        if (_displayedSessionState == _runSessionService.State)
            return;

        _displayedSessionState = _runSessionService.State;
        _backgroundImage.sprite = (_runSessionService.State != RunSessionState.Succeeded) ? _failSprite : _successSprite;
    }

    private void HandleShiftStarted()
    {
        _evaluationPending = false;
        ResetEvaluation();
    }

    private void HandleShiftEnded()
    {
        _evaluationPending = true;
        TryStartPendingEvaluation();
    }

    private void HandleComputerFocusStarted()
    {
        TryStartPendingEvaluation();
    }

    private void TryStartPendingEvaluation()
    {
        if (!_evaluationPending || !_evaluatable)
            return;

        if (_computerInteractable == null || !_computerInteractable.HasInteractor)
            return;

        if (_runSessionService.State == RunSessionState.Running)
            return;

        StartEvaluation();
    }

    public void SetSegments(int segmentAmount)
    {
        for(int i = 0; i < _segments.Length; i++)
            _segments[i].color = i < segmentAmount + 1 ? Color.purple : Color.white;
    }
    public void ResetSegments()
    {
        for(int i = 0; i < _segments.Length; i++)
            _segments[i].color = Color.white;
    }
    public void StartEvaluation()
    {
        if (_runSessionService.State == RunSessionState.Running)
            return;

        if (_evaluationCoroutine != null)
            StopCoroutine(_evaluationCoroutine);

        _evaluationCoroutine = StartCoroutine(EvaluateGrade());
        _evaluateButton.SetActive(false);
        _nextShiftButton.SetActive(true);
        _evaluatable = false;
        _evaluationPending = false;
        _evaluatedShiftIndex = _shiftService.ShiftIndex;
    }
    public void ResetEvaluation()
    {
        if (_evaluationCoroutine != null)
        {
            StopCoroutine(_evaluationCoroutine);
            _evaluationCoroutine = null;
        }

        ResetGrade();
        _evaluateButton.SetActive(true);
        _nextShiftButton.SetActive(false);
        _evaluatable = true;
    }
    private IEnumerator EvaluateGrade()
    {
        var stats = _shiftService.LastStatistics;
        int largestValue = Mathf.Max(
            stats.CustomersServed,
            stats.CustomersUnsatisfied,
            stats.MoneyEarned,
            (int)stats.AverageDeliveryTime);

        int stepCount = Mathf.Min(Mathf.Max(1, _countAnimationSteps), Mathf.Max(1, largestValue));
        if (largestValue > 0 && _countAnimationDuration > 0f)
        {
            var stepDelay = new WaitForSeconds(_countAnimationDuration / stepCount);
            for (int step = 1; step <= stepCount; step++)
            {
                float progress = (float)step / stepCount;
                SetStatText(
                    Mathf.RoundToInt(stats.CustomersServed * progress),
                    Mathf.RoundToInt(stats.CustomersUnsatisfied * progress),
                    Mathf.RoundToInt(stats.MoneyEarned * progress),
                    Mathf.RoundToInt(stats.AverageDeliveryTime * progress));
                yield return stepDelay;
            }
        }

        SetStatText(
            stats.CustomersServed,
            stats.CustomersUnsatisfied,
            stats.MoneyEarned,
            (int)stats.AverageDeliveryTime);

        var segmentDelay = new WaitForSeconds(0.4f);
        for (int i = 0; i <= _shiftService.ShiftIndex; i++)
        {
            SetSegments(i);
            yield return segmentDelay;
        }

        _evaluationCoroutine = null;
    }

    private void SetStatText(int customersServed, int customersUnsatisfied, int moneyEarned, int deliveryTime)
    {
        _customersServedText.SetText("{0}", customersServed);
        _customersUnsatisfiedText.SetText("{0}", customersUnsatisfied);
        _moneyEarnedText.SetText("{0}", moneyEarned);
        _deliveryTimeText.SetText("{0}", deliveryTime);
    }
    private void ResetGrade()
    {
        _customersServedText.text = "";
        _customersUnsatisfiedText.text = "";
        _moneyEarnedText.text = "";
        _deliveryTimeText.text = "";
    }
}
