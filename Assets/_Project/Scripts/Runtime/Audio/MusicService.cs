using Game.Lifecycle;
using Game.Services;
using Game.Progression;
using UnityEngine;
using System;

namespace Game.Audio
{
    [RequireComponent(typeof(AudioSource)), ExecuteInEditMode]
    public class MusicService : MonoBehaviour, IBootstrapable
    {
        private RunSessionService _runSessionService;
        [SerializeField] private AudioClip[] _musList;
        private AudioSource _audioSource;

        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            _runSessionService = ServiceLocator.Get<RunSessionService>();
            _audioSource = gameObject.GetComponent<AudioSource>();
        }
        private void Update()
        {
            if (!_audioSource.isPlaying)
            { 
                _audioSource.clip = _runSessionService.State != RunSessionState.Running ? _musList[0] : _musList[1];
                _audioSource.Play();
            }
        }
    }
} 
