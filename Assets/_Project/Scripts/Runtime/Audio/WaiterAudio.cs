using Game.Services;
using Game.Audio;
using Game.Characters;
using UnityEngine;


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

    private float _cooldown;
    private SoundType _previous = SoundType.WaiterWalk;

    private void Awake()
    {
        _soundService = ServiceLocator.Get<SoundService>(); 
    }

    private void LateUpdate()
    {
        _cooldown = Mathf.Max(0f, _cooldown - Time.deltaTime);

        if (_waiterComponent.LocomotionState == WaiterLocomotionState.Ragdoll)
            TryPlay(_waiterComponent.IsGrounded ? _soundSplat : _soundThrow, true);
        else if (_waiterComponent.LocomotionState == WaiterLocomotionState.Walking)
            TryPlay(_soundWalk, false);
    }

    private void OnEnable()
    {
        _cooldown = 0f;
        _previous = SoundType.WaiterWalk;
    }

    private void TryPlay(SoundType sound, bool noRepeats)
    {
        if (_cooldown > 0f || noRepeats && sound == _previous)
            return;

        _soundService.PlaySound(sound, _volume);
        _previous = sound;
        _cooldown = _interval;
    }


}
