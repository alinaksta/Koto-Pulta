using Game.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// Starts gameplay scene loading from the main menu.
    /// </summary>
    public class PlayButton : MonoBehaviour
    {
        [SerializeField] private int _nextSceneIndex = 1;

        private LoadingService _loadingService;
        private bool _isLoading;

        private void Start()
        {
            _loadingService = ServiceLocator.Get<LoadingService>();
        }

        /// <summary>
        /// Loads the configured gameplay scene from the main menu.
        /// </summary>
        public async void EnterNextScene()
        {
            if (_isLoading)
                return;

            _isLoading = true;
            await _loadingService.StartLoadingAsync();

            await Awaitable.WaitForSecondsAsync(0.8f);

            AsyncOperation sceneLoad = SceneManager.LoadSceneAsync(_nextSceneIndex, LoadSceneMode.Single);

            sceneLoad.allowSceneActivation = false;

            while (sceneLoad.progress < 0.9f)
                await Awaitable.NextFrameAsync();

            sceneLoad.allowSceneActivation = true;

            while (!sceneLoad.isDone)
                await Awaitable.NextFrameAsync();

            await Awaitable.WaitForSecondsAsync(0.8f);

            await _loadingService.StopLoadingAsync();
        }
    }
}
