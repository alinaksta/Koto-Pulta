using Game.Services;
using Game.Audio;
using Game.Input;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;


public class ComputerAudio : MonoBehaviour
{
    private SoundService _soundService;
    private IInputService _inputService;
    [SerializeField] private Button[] _buttonArray;
    [SerializeField] private Button[] _tabArray;

    [SerializeField] private SoundType _buttonSound = SoundType.Button;
    [SerializeField] private SoundType _tabSound = SoundType.TabSwitch;
    [SerializeField] private SoundType _clickSound = SoundType.ComputerClick;
    [Range(0f,1f)]
    [SerializeField] private float _volume = 1f;
    [Min(0f)]
    [SerializeField] private float _interval = 0.3f;

    private void Awake()
    {
        _soundService = ServiceLocator.Get<SoundService>(); 
        _inputService = ServiceLocator.Get<IInputService>();
    }
    private void OnEnable()
    {
        for(int i = 0; i < _buttonArray.Length; i++)
            _buttonArray[i].onClick.AddListener(PlayButtonSound);
        for(int i = 0; i < _tabArray.Length; i++)
            _tabArray[i].onClick.AddListener(PlayTabSound);
    }
    private void OnDisable()
    {
        for(int i = 0; i < _buttonArray.Length; i++)
            _buttonArray[i].onClick.RemoveListener(PlayButtonSound);
        for(int i = 0; i < _tabArray.Length; i++)
            _tabArray[i].onClick.RemoveListener(PlayTabSound);
    }
    private void Update()
    {
        if (_inputService.InteractLeft.Pressed && gameObject.activeInHierarchy)
            Play(_clickSound);
    }
    private void PlayButtonSound()
    {
        Play(_buttonSound);
    }
    private void PlayTabSound()
    {
        Play(_tabSound);
    }
    private void Play(SoundType sound)
    {
        _soundService.PlaySound(sound, _volume);
    }


}