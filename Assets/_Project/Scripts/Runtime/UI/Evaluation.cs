using Game.Services;
using Game.Progression;
using Game.Interaction;
using Game.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Game.Player;

public class Evaluation : MonoBehaviour
{
    private ShiftService _shiftService;
    private RunSessionService _runSessionService;
    [SerializeField] ComputerInteractable _computerInteractable;
    [SerializeField] private SiteActivator _computerTabs;
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
    private int _evaluatedResultVersion;
    private int _displayedSegmentIndex = -1;
    private RunSessionState _displayedSessionState = (RunSessionState)(-1);

    private void Awake()
    {
        _shiftService = ServiceLocator.Get<ShiftService>();
        _runSessionService = ServiceLocator.Get<RunSessionService>();
        _evaluateButton.SetActive(false);

        if (_computerTabs == null && _computerInteractable != null)
            _computerTabs = _computerInteractable.GetComponentInParent<SiteActivator>();

        if (_computerTabs == null && _computerInteractable != null)
            _computerTabs = _computerInteractable.GetComponentInChildren<SiteActivator>(true);
    }

    private void OnEnable()
    {
        _shiftService.OnShiftStarted += HandleShiftStarted;
        _shiftService.OnShiftEnded += HandleShiftEnded;

        if (_computerInteractable != null)
            _computerInteractable.FocusStarted += HandleComputerFocusStarted;

        if (_computerTabs != null)
            _computerTabs.OnTabViewed += HandleTabViewed;

        RefreshPendingEvaluation();
    }

    private void OnDisable()
    {
        _shiftService.OnShiftStarted -= HandleShiftStarted;
        _shiftService.OnShiftEnded -= HandleShiftEnded;

        if (_computerInteractable != null)
            _computerInteractable.FocusStarted -= HandleComputerFocusStarted;

        if (_computerTabs != null)
            _computerTabs.OnTabViewed -= HandleTabViewed;
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
        _computerTabs.DisableTab(ComputerSiteTab.Shop);
        _computerTabs.SetTab(ComputerSiteTab.ShiftStatistics);
        _computerTabs.EnableTab(ComputerSiteTab.Shop);
        RefreshPendingEvaluation();
    }

    private void HandleComputerFocusStarted()
    {
        StartEvaluation();
    }

    private void HandleTabViewed(ComputerSiteTab tab)
    {
        if (tab == ComputerSiteTab.ShiftStatistics)
            RefreshPendingEvaluation();
    }

    public void RefreshPendingEvaluation()
    {
        //if (!_shiftService.HasCompletedShiftResults || _shiftService.ShiftInProgress || _runSessionService.State == RunSessionState.Running)
        //    return;

        //if (_shiftService.CompletedShiftResultVersion != _evaluatedResultVersion)
        _evaluatable = true;

        _evaluationPending = true;
        TryStartPendingEvaluation();
    }

    private void TryStartPendingEvaluation()
    {
        //if (!_evaluationPending || !_evaluatable)
        //    return;
        if (!IsComputerFocused())
            return;

        //if (_runSessionService.State == RunSessionState.Running)
        //    return;

        StartEvaluation();
    }

    public void SetSegments(int segmentAmount)
    {
        for(int i = 0; i < _segments.Length; i++)
            _segments[i].color = i < segmentAmount + 1 ? Color.purple : Color.white;

        _displayedSegmentIndex = segmentAmount;
    }
    public void ResetSegments()
    {
        for(int i = 0; i < _segments.Length; i++)
            _segments[i].color = Color.white;

        _displayedSegmentIndex = -1;
    }
    public void StartEvaluation()
    {
        if (_evaluationCoroutine != null)
            StopCoroutine(_evaluationCoroutine);

        Debug.Log("Started evaluation");
        _evaluationCoroutine = StartCoroutine(EvaluateGrade());
        _nextShiftButton.SetActive(true);
        _evaluatable = false;
        _evaluationPending = false;
        _evaluatedResultVersion = _shiftService.CompletedShiftResultVersion;
    }

    public void ShowCurrentResultsImmediate()
    {
        if (_evaluationCoroutine != null)
        {
            StopCoroutine(_evaluationCoroutine);
            _evaluationCoroutine = null;
        }

        var stats = _shiftService.LastStatistics;
        SetStatText(
            stats.CustomersServed,
            stats.CustomersUnsatisfied,
            stats.MoneyEarned,
            (int)stats.AverageDeliveryTime);

        _nextShiftButton.SetActive(true);
        _evaluatable = false;
        _evaluationPending = false;
        _evaluatedResultVersion = _shiftService.CompletedShiftResultVersion;
    }

    public void ResetEvaluation()
    {
        if (_evaluationCoroutine != null)
        {
            StopCoroutine(_evaluationCoroutine);
            _evaluationCoroutine = null;
        }

        ResetGrade();
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

        int shiftIndex = _runSessionService.State == RunSessionState.Failed ? _shiftService.ShiftIndex - 1 : _shiftService.ShiftIndex;
        int targetSegmentIndex = Mathf.Min(shiftIndex, _segments.Length - 1);
        var segmentDelay = new WaitForSeconds(0.4f);
        for (int i = _displayedSegmentIndex + 1; i <= targetSegmentIndex; i++)
        {
            SetSegments(i);
            yield return segmentDelay;
        }

        _evaluationCoroutine = null;
    }

    private bool IsComputerFocused()
    {
        return CameraController.CurrentFocusStatus == FocusStatus.InTransition || CameraController.CurrentFocusStatus == FocusStatus.Focused;
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
