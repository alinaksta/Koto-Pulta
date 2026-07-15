using Game.Input;
using Game.Services;
using Game.Audio;
using UnityEngine;

namespace Game.Interaction
{
    public class Pause : MonoBehaviour
    {
        private IInputService _inputService;
        private SoundService _soundService;
        [SerializeField] private SoundType _pauseSound = SoundType.Error;
        [Range(0f,1f)]
        [SerializeField] private float _volume = 1f;


        private void Awake()
        {
            _inputService = ServiceLocator.Get<IInputService>();
            _soundService = ServiceLocator.Get<SoundService>();
        }

        private void Update()
        {
            if (_inputService.Pause.Pressed)
                InitiatePause();
        }
        private void InitiatePause()
        {
            _soundService.PlaySound(_pauseSound, _volume);
            Debug.Log("This is a pause");
        }

    }
}
