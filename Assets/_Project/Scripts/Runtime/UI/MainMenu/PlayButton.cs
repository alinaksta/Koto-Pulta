using Game.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    public class PlayButton : MonoBehaviour
    {
        [SerializeField] private int _nextSceneIndex = 1;

        private LoadingService _loadingService;

        private void Start()
        {
            _loadingService = ServiceLocator.Get<LoadingService>();
        }

        public async void EnterNextScene()
        {
            await _loadingService.StartLoadingAsync();

            await SceneManager.LoadSceneAsync(_nextSceneIndex, LoadSceneMode.Single);

            await Awaitable.WaitForSecondsAsync(1.2f);

            await _loadingService.StopLoadingAsync();
        }
    }
}