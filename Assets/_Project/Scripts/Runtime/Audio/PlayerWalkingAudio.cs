using Game.Services;
using Game.Audio;
using Game.Movement;
using UnityEngine;


public class PlayerWalkingAudio : MonoBehaviour
{
    private SoundService _soundService;
    [SerializeField] private PlayerController _playerController;

    [SerializeField] private SoundType _sound = SoundType.Walk;
    [Range(0f,1f)]
    [SerializeField] private float _volume = 1f;
    [Min(0f)]
    [SerializeField] private float _interval = 0.3f;

    private float _cooldown;

    private void Awake()
    {
        _soundService = ServiceLocator.Get<SoundService>(); 
    }

    private void LateUpdate()
    {
        _cooldown = Mathf.Max(0f, _cooldown - Time.deltaTime);
        Vector3 velocity = _playerController.Velocity;
        bool moving = velocity.x * velocity.x + velocity.z * velocity.z > 0.01f;

        if (_cooldown > 0f || !moving || !_playerController.IsGrounded)
            return;

        _soundService.PlaySound(_sound, _volume);
        _cooldown = _interval;
    }


}
