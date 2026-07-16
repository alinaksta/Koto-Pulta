using Game.Input;
using Game.Services;
using Game.Player;
using Game.Audio;
using UnityEngine;

namespace Game.Interaction
{
    public class Pause : MonoBehaviour
    {
        private IInputService _inputService;
        private SoundService _soundService;
        public GameObject _pauseCanvas;
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
            if (_inputService.Pause.Pressed && !_pauseCanvas.activeSelf)
                InitiatePause();
            if (_inputService.Pause.Pressed && _pauseCanvas.activeSelf)
                Continue();
        }
        private void InitiatePause()
        {
            _pauseCanvas.SetActive(true);
            CameraController.SetMouseLockedStatic(false);
            _soundService.PlaySound(_pauseSound, _volume);
            Debug.Log("This is a pause");
            Time.timeScale = 0f;
        }
        public void Continue()
        {
            CameraController.SetMouseLockedStatic(true);
             Time.timeScale = 1f;
            _pauseCanvas.SetActive(false);
        }

    }
}
