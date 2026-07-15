using Game.Services;
using Game.Audio;
using Game.Items;
using UnityEngine;
using System;
using System.Collections;


public class ItemAudio : MonoBehaviour
{
    private SoundService _soundService;
    [SerializeField] private MonoBehaviour _containerComponent;

    [SerializeField] private SoundType _sound = SoundType.PickUp;
    [Range(0f,1f)]
    [SerializeField] private float _volume = 1f;
    [Min(0f)]
    [SerializeField] private float _interval = 0.3f;

    private IContainer _container;

    private void Awake()
    {
        if (_containerComponent is not IContainer)
            throw new InvalidOperationException($"{_containerComponent.name} is not an IContainer!");

        _container = _containerComponent as IContainer;
        _soundService = ServiceLocator.Get<SoundService>(); 
    }
    private void OnEnable()
    {
        _container.OnItemChanged += Play;
    }
    private void OnDisable()
    {
        _container.OnItemChanged -= Play;
    }
    private void Play(Item? item)
    {
        _soundService.PlaySound(_sound, _volume);
    }


}