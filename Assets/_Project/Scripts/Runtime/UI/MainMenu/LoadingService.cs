using Game.Lifecycle;
using Game.Services;
using System;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Controls loading screen state and exposes loading events.
    /// </summary>
    public class LoadingService : MonoBehaviour, IBootstrapable
    {
        [SerializeField] private LoadingScreenUI _loadingScreenUI;

        private bool _isLoading;

        /// <summary>
        /// Gets whether the loading service is currently active.
        /// </summary>
        public bool IsLoading => _isLoading;

        public event Action OnLoadingStarted = delegate { };
        public event Action OnLoadingStopped = delegate { };

        private void Awake()
        {
            if (_loadingScreenUI == null)
                _loadingScreenUI = GetComponentInChildren<LoadingScreenUI>(true);
        }

        /// <inheritdoc/>
        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }

        /// <summary>
        /// Starts loading without waiting for the transition to finish.
        /// </summary>
        public void StartLoading()
        {
            _ = StartLoadingAsync();
        }

        /// <summary>
        /// Stops loading without waiting for the transition to finish.
        /// </summary>
        public void StopLoading()
        {
            _ = StopLoadingAsync();
        }

        /// <summary>
        /// Starts loading and waits until the loading screen is ready.
        /// </summary>
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

        /// <summary>
        /// Stops loading and waits until the loading screen is fully hidden.
        /// </summary>
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
