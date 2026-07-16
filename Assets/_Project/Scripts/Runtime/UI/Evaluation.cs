using Game.Services;
using Game.Progression;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class Evaluation : MonoBehaviour
{
    private ShiftService _shiftService;
    [SerializeField] private Image[] _segments;
    [SerializeField] private TMP_Text _customersServedText;
    [SerializeField] private TMP_Text _customersUnsatisfiedText;
    [SerializeField] private TMP_Text _deliveryTimeText;
    [SerializeField] private TMP_Text _moneyEarnedText;
    [SerializeField] private GameObject _evaluateButton;
    [SerializeField] private GameObject _nextShiftButton;



    private void Awake()
    {
        _shiftService = ServiceLocator.Get<ShiftService>();
    }
    public void SetSegments(int segmentAmount)
    {
        for(int i = 0; i < _segments.Length; i++)
            _segments[i].color = i < segmentAmount + 1 ? Color.green : Color.white;
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
    }
    public void ResetEvaluation()
    {
        ResetGrade();
        _evaluateButton.SetActive(true);
        _nextShiftButton.SetActive(false);
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