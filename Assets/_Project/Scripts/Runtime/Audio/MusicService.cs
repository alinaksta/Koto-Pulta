using Game.Lifecycle;
using Game.Services;
using Game.Progression;
using UnityEngine;
using System;

namespace Game.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicService : MonoBehaviour, IBootstrapable
    {
        private ShiftService _shiftService;
        [SerializeField] private AudioClip[] _musList;
        private AudioSource _audioSource;

        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
        }
        private void Update()
        {
            if (!_shiftService) _shiftService = ServiceLocator.Get<ShiftService>();
            Debug.Log(_shiftService.ShiftInProgress);
            Debug.Log(_audioSource.clip);
            Debug.Log(_shiftService.ShiftInProgress != _audioSource.clip == _musList[1]);
            
            if(_shiftService.ShiftInProgress != (_audioSource.clip == _musList[1]))
            {
                _audioSource.clip = _musList[_shiftService.ShiftInProgress ? 1 : 0];
                _audioSource.Play();
            }
            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }
    }
} 
