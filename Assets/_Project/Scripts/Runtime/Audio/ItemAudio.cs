using Game.Services;
using Game.Audio;
using Game.Items;
using UnityEngine;
using System;
using System.Collections;


public class ItemAudio : MonoBehaviour
{
    private SoundService _soundService;
    [SerializeField] private ShelveContainer _containerComponent;

    [SerializeField] private SoundType _sound = SoundType.PickUp;
    [Range(0f,1f)]
    [SerializeField] private float _volume = 1f;
    [Min(0f)]
    [SerializeField] private float _interval = 0.3f;

    private void Awake()
    {
        _soundService = ServiceLocator.Get<SoundService>(); 
    }
    private void OnEnable()
    {
        _containerComponent.Container.OnItemChanged += Play;
    }
    private void OnDisable()
    {
        _containerComponent.Container.OnItemChanged -= Play;
    }
    private void Play(Item? item)
    {
        Debug.Log("AAAAAAAAAAAAAAAAAA");
        //_soundService.PlaySound(_sound, _volume);
    }


}