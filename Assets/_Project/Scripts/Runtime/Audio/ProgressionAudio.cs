using Game.Services;
using Game.Progression;
using Game.Audio;
using UnityEngine;
using System;
using System.Collections;


public class ProgressionAudio : MonoBehaviour
{
    private SoundService _soundService;
    private BalanceService _balanceService;
    private ShiftService _shiftService;
    private int _timerValue;

    [SerializeField] private SoundType _moneySound = SoundType.Money;
    [SerializeField] private SoundType _timerSound = SoundType.ClockTick;
    [SerializeField] private SoundType _alarmSound = SoundType.Alarm;
    [Range(0f,1f)]
    [SerializeField] private float _volume = 1f;
    [Min(0f)]
    [SerializeField] private float _interval = 0.3f;

    private void Awake()
    {
        _soundService = ServiceLocator.Get<SoundService>(); 
        _balanceService = ServiceLocator.Get<BalanceService>(); 
        _shiftService = ServiceLocator.Get<ShiftService>(); 
    }
    private void OnEnable()
    {
        _balanceService.OnBalanceChanged += PlayMoneySound;
        _shiftService.OnShiftEnded += PlayAlarmSound;
    }
    private void OnDisable()
    {
        _balanceService.OnBalanceChanged -= PlayMoneySound;
        _shiftService.OnShiftEnded -= PlayAlarmSound;
    }
    private void LateUpdate()
    {
        if((int)_shiftService.ShiftTimer != _timerValue)
        {
            Play(_timerSound, (int)_shiftService.ShiftTimer % 2);
            _timerValue = (int)_shiftService.ShiftTimer;
        }
    }
    private void PlayMoneySound (int _)
    {
        Play(_moneySound);
    }
    private void PlayAlarmSound ()
    {
        Play(_alarmSound);
    }
    private void Play(SoundType sound)
    {
        _soundService.PlaySound(sound, _volume);
    }
    private void Play(SoundType sound, int order)
    {
        _soundService.PlaySound(sound, _volume, order);
    }
}