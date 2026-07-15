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
    [SerializeField] private TMP_Text grade;


    private void Awake()
    {
        _shiftService = ServiceLocator.Get<ShiftService>();
    }
    public void SetSegments()
    {
        for(int i = 0; i < _segments.Length; i++)
            _segments[i].color = i < _shiftService.ShiftIndex + 1 ? Color.green : Color.white;
    }
    public void ResetSegments()
    {
        for(int i = 0; i < _segments.Length; i++)
            _segments[i].color = Color.white;
    }
    public void StartEvaluation()
    {
        StartCoroutine(EvaluateGrade());
    }
    private IEnumerator EvaluateGrade()
    {
        int satisfied = 0, angry = 0, revenue = 0;
        for (int i = 0; i < Mathf.Max(_shiftService.LastStatistics.CustomersServed, _shiftService.LastStatistics.CustomersUnsatisfied, _shiftService.LastStatistics.MoneyEarned); i++) {
            satisfied = Mathf.Min(satisfied + 1, _shiftService.LastStatistics.CustomersServed);
            angry = Mathf.Min(angry + 1, _shiftService.LastStatistics.CustomersUnsatisfied);
            revenue = Mathf.Min(revenue + 1, _shiftService.LastStatistics.MoneyEarned);
            grade.text = $"{satisfied}\n{angry}\n{revenue}";
            yield return new WaitForSeconds(0.2f);
        }
    }
}