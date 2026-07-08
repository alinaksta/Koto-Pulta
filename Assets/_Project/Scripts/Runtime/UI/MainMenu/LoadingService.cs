using Game.Lifecycle;
using Game.Services;
using System;
using UnityEngine;

namespace Game.UI
{
    public class LoadingService : MonoBehaviour, IBootstrapable
    {
        [SerializeField] private LoadingScreenUI _loadingScreenUI;

        private bool _isLoading;

        public bool IsLoading => _isLoading;

        public event Action OnLoadingStarted = delegate { };
        public event Action OnLoadingStopped = delegate { };

        private void Awake()
        {
            if (_loadingScreenUI == null)
                _loadingScreenUI = GetComponentInChildren<LoadingScreenUI>(true);
        }

        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }

        public void StartLoading()
        {
            _ = StartLoadingAsync();
        }

        public void StopLoading()
        {
            _ = StopLoadingAsync();
        }

        public async Awaitable StartLoadingAsync()
        {
            if (_isLoading)
            {
                if (_loadingScreenUI != null)
                {
                    await _loadingScreenUI.ShowAsync();
                }

                return;
            }

            _isLoading = true;

            if (_loadingScreenUI != null)
                await _loadingScreenUI.ShowAsync();

            OnLoadingStarted.Invoke();
        }

        public async Awaitable StopLoadingAsync()
        {
            if (!_isLoading)
            {
                if (_loadingScreenUI != null)
                    await _loadingScreenUI.HideAsync();

                return;
            }

            if (_loadingScreenUI != null)
                await _loadingScreenUI.HideAsync();

            _isLoading = false;
            OnLoadingStopped.Invoke();
        }
    }
}
