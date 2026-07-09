using Game.Services;
using Game.Audio;
using Game.Characters;
using UnityEngine;
using System;
using System.Collections;


public class WaiterAudio : MonoBehaviour
{
    private SoundService _soundService;
    [SerializeField] private Waiter _waiterComponent;

    [SerializeField] private SoundType _soundWalk = SoundType.WaiterWalk;
    [SerializeField] private SoundType _soundSplat = SoundType.WaiterSplat;
    [SerializeField] private SoundType _soundThrow = SoundType.Throw;
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
        if(_waiterComponent.LocomotionState == WaiterLocomotionState.Ragdoll && _waiterComponent.IsGrounded) StartCoroutine(Play(_soundSplat, true));
        if(_waiterComponent.LocomotionState == WaiterLocomotionState.Ragdoll && !_waiterComponent.IsGrounded) StartCoroutine(Play(_soundThrow, true));
        if(_waiterComponent.LocomotionState == WaiterLocomotionState.Walking) StartCoroutine(Play(_soundWalk));
    }

    private bool _flag = true;
    private SoundType _previous = SoundType.WaiterWalk;
    private IEnumerator Play(SoundType snd, bool noRepeats = false)
    {
        if(!_flag) yield break;
        if(snd == _previous && noRepeats) yield break;
        _flag = false;
        _soundService.PlaySound(snd, _volume);
        Debug.Log(snd);
        yield return new WaitForSeconds(_interval);
        _previous = snd;
        _flag = true;
    } 


}