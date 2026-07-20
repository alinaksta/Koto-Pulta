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

    private Coroutine _evaluationCoroutine;
    private bool _evaluationPending;
    private int _evaluatedShiftIndex = -1;

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
        float interval = 0.2f;
        for (int i = 0; i < Mathf.Max(stats.CustomersServed, stats.CustomersUnsatisfied, stats.MoneyEarned, (int)stats.AverageDeliveryTime); i++) {
            _customersServedText.text = $"{Mathf.Min(i, stats.CustomersServed)}";
            _customersUnsatisfiedText.text = $"{Mathf.Min(i, stats.CustomersUnsatisfied)}";
            _moneyEarnedText.text = $"{Mathf.Min(i, stats.MoneyEarned)}";
            _deliveryTimeText.text = $"{Mathf.Min(i, (int)stats.AverageDeliveryTime)}";

            yield return new WaitForSeconds(interval);
            interval -= 0.005f;
        }

        _customersServedText.text = $"{stats.CustomersServed}";
        _customersUnsatisfiedText.text = $"{stats.CustomersUnsatisfied}";
        _moneyEarnedText.text = $"{stats.MoneyEarned}";
        _deliveryTimeText.text = $"{(int)stats.AverageDeliveryTime}";

        for (int i = 0; i <= _shiftService.ShiftIndex; i++)
        {
            SetSegments(i);
            yield return new WaitForSeconds(0.4f);
        }

        _evaluationCoroutine = null;
    }
    private void ResetGrade()
    {
        _customersServedText.text = "";
        _customersUnsatisfiedText.text = "";
        _moneyEarnedText.text = "";
        _deliveryTimeText.text = "";
    }
}
