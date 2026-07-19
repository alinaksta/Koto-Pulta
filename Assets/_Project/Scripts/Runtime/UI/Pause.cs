using Game.Input;
using Game.Services;
using Game.Player;
using Game.Audio;
using Game.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        [SerializeField] private int _mainMenuIndex = 0;

        private LoadingService _loadingService;

        private void Start()
        {
            _loadingService = ServiceLocator.Get<LoadingService>();
            _inputService = ServiceLocator.Get<IInputService>();
            _soundService = ServiceLocator.Get<SoundService>();
        }

        /// <summary>
        /// Loads the configured gameplay scene from the main menu.
        /// </summary>
        public async void QuitToMainMenu()
        {
            SceneManager.LoadScene(_mainMenuIndex, LoadSceneMode.Single);
        }

        private void Update()
        {
            if (_inputService.Pause.Pressed && !_pauseCanvas.activeSelf)
                InitiatePause();
            else if (_inputService.Pause.Pressed && _pauseCanvas.activeSelf)
                Continue();
        }
        private void InitiatePause()
        {
            _pauseCanvas.SetActive(true);
            CameraController.SetMouseLockedStatic(false);
            CameraController.SetActiveRotationStatic(true);
            _soundService.PlaySound(_pauseSound, _volume);
            Debug.Log("This is a pause");
            Time.timeScale = 0f;
        }
        public void Continue()
        {
            CameraController.SetMouseLockedStatic(true);
            CameraController.SetActiveRotationStatic(false);
            Time.timeScale = 1f;
            _pauseCanvas.SetActive(false);
        }

    }
}
