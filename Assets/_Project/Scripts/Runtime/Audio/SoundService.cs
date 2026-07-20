using Game.Lifecycle;
using Game.Services;
using UnityEngine;
using System;

namespace Game.Audio
{
    public enum SoundType
    {
        Walk,
        PickUp,
        Throw,
        Money,
        Upgrade,
        Button,
        WaiterWalk,
        WaiterSplat,
        ClockTick,
        Alarm,
        Error,
        PopUp,
        ComputerClick,
        TabSwitch
    } 
    [Serializable]
    public struct SoundList
    {
        public AudioClip[] Sounds { get => _sounds; }
        [HideInInspector] public string _name;
        [SerializeField] private AudioClip[] _sounds;
    }

    [RequireComponent(typeof(AudioSource)), ExecuteInEditMode]
    public class SoundService : MonoBehaviour, IBootstrapable
    {
        [SerializeField] private SoundList[] _soundList;
        private AudioSource _audioSource;

        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
        }


    #if UNITY_EDITOR
        private void OnEnable()
        {
            string[] names = Enum.GetNames(typeof(SoundType));
            Array.Resize(ref _soundList, names.Length);  
            for(int i = 0; i < _soundList.Length; i++)
                _soundList[i]._name = names[i];
        }
    #endif

        public void PlaySound(SoundType sound, float volume = 1, int order = -1)
        {
            AudioClip[] clips = _soundList[(int)sound].Sounds;
            if(clips.Length == 0 || _audioSource == null) return;
            AudioClip selectedClip = (order > -1 && order < clips.Length) ? clips[order] :
                                     (clips.Length > 1) ? clips[UnityEngine.Random.Range(0, clips.Length)] : clips[0];
            _audioSource.PlayOneShot(selectedClip, volume);
        }
    }
} 
