using Game.Services;
using Game.Progression;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class ShiftProceed : MonoBehaviour
{
    private ShiftService _shiftService;

    private void Awake()
    {
        _shiftService = ServiceLocator.Get<ShiftService>();
    }
    public void Proceed()
    {
        Debug.Log(_shiftService.TryStartNextShift());
    }
}