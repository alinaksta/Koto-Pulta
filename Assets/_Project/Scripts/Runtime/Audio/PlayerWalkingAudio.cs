using Game.Services;
using Game.Audio;
using Game.Movement;
using UnityEngine;
using System;
using System.Collections;


public class PlayerWalkingAudio : MonoBehaviour
{
    private SoundService _soundService;
    [SerializeField] private PlayerController _playerController;

    [SerializeField] private SoundType _sound = SoundType.Walk;
    [Range(0f,1f)]
    [SerializeField] private float _volume = 1f;
    [Min(0f)]
    [SerializeField] private float _interval = 0.3f;

    private void Awake()
    {
        _soundService = ServiceLocator.Get<SoundService>(); 
    }

    private void LateUpdate()
    {
        if(_playerController.Velocity != Vector3.zero && _playerController.IsGrounded) StartCoroutine(Play());
    }

    private bool _flag = true;
    private IEnumerator Play()
    {
        if(!_flag) yield break;
        _flag = false;
        _soundService.PlaySound(_sound, _volume);
        yield return new WaitForSeconds(_interval);
        _flag = true;
    } 


}