using Game.Lifecycle;
using Game.Services;
using UnityEngine;
using System;

namespace Game.Audio
{
    [RequireComponent(typeof(AudioSource)), ExecuteInEditMode]
    public class MusicService : MonoBehaviour, IBootstrapable
    {
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
            if (!_audioSource.isPlaying)
            { 
                _audioSource.clip = _musList[0];
                _audioSource.Play();
            }
        }
    }
} 
