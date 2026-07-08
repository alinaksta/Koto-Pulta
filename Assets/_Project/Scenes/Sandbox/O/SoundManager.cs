using UnityEngine;
using System;

public enum SoundType
{
    WALK,
    PICKUP,
    THROW,
    MONEY,
    UPGRADE,
    BUTTON,
    WAITERFLY,
    WAITERSPLAT
} 
[Serializable]
public struct SoundList
{
    public AudioClip[] Sounds { get => _sounds; }
    [HideInInspector] public string _name;
    [SerializeField] private AudioClip[] _sounds;
}

[RequireComponent(typeof(AudioSource)), ExecuteInEditMode]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private SoundList[] _soundList;
    private static SoundManager _instance;
    private AudioSource _audioSource;

    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
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

    public static void PlaySound(SoundType sound, float volume = 1)
    {
        AudioClip[] clips = _instance._soundList[(int)sound].Sounds;
        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
        Debug.Log(randomClip);
        _instance._audioSource.PlayOneShot(randomClip, volume);
    }
}
