using Game.Services;
using Game.Progression;
using Game.Interaction;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
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

    private void Awake()
    {
        _shiftService = ServiceLocator.Get<ShiftService>();
        _runSessionService = ServiceLocator.Get<RunSessionService>();
    }
    private void Update()
    {
        if(_computerInteractable.HasInteractor && _runSessionService.State != RunSessionState.Running && _evaluatable) StartEvaluation();
        _backgroundImage.sprite = (_runSessionService.State != RunSessionState.Succeeded) ? _failSprite : _successSprite;
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
        StartCoroutine(EvaluateGrade());
        _evaluateButton.SetActive(false);
        _nextShiftButton.SetActive(true);
        _evaluatable = false;
    }
    public void ResetEvaluation()
    {
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
        for (int i = 0; i <= _shiftService.ShiftIndex; i++)
        {
            SetSegments(i);
            yield return new WaitForSeconds(0.4f);
        }
    }
    private void ResetGrade()
    {
        _customersServedText.text = "";
        _customersUnsatisfiedText.text = "";
        _moneyEarnedText.text = "";
        _deliveryTimeText.text = "";
        ResetSegments();
    }
}