using Game.Characters;
using Game.Lifecycle;
using Game.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// Owns the currently active runtime game mode.
    /// </summary>
    public class GameModeService : MonoBehaviour, IBootstrapable
    {
        [SerializeField] private string _startingModeId = "tutorial";
        [SerializeField] private string _fallbackModeId = "shift";
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private List<MonoBehaviour> _gameModeBehaviours = new();

        private readonly Dictionary<string, IGameMode> _modesById = new(StringComparer.OrdinalIgnoreCase);

        private IGameMode _activeMode;

        /// <summary>
        /// Gets the currently active game mode.
        /// </summary>
        public IGameMode ActiveMode => _activeMode;

        /// <summary>
        /// Gets the identifier of the currently active game mode.
        /// </summary>
        public string ActiveModeId => _activeMode?.Id ?? string.Empty;

        /// <summary>
        /// Gets whether a game mode is currently active.
        /// </summary>
        public bool HasActiveMode => _activeMode != null;

        /// <summary>
        /// Raised after the active game mode changes.
        /// </summary>
        public event Action<IGameMode> OnGameModeChanged = delegate { };

        private void Awake()
        {
            CacheModes();
        }

        private void Start()
        {
            if (!_autoStart || _activeMode != null)
                return;

            if (!string.IsNullOrWhiteSpace(_startingModeId) && TrySetGameMode(_startingModeId))
                return;

            if (!string.IsNullOrWhiteSpace(_fallbackModeId) && TrySetGameMode(_fallbackModeId))
                return;

            foreach (var mode in _modesById.Values)
            {
                if (TrySetGameMode(mode))
                    return;
            }
        }

        private void Update()
        {
            _activeMode?.Tick(Time.deltaTime);
        }

        /// <summary>
        /// Tries to activate the game mode with the supplied identifier.
        /// </summary>
        /// <param name="modeId">Identifier of the mode to activate.</param>
        /// <returns><see langword="true"/> when the mode was activated.</returns>
        public bool TrySetGameMode(string modeId)
        {
            if (string.IsNullOrWhiteSpace(modeId))
                return false;

            if (!_modesById.TryGetValue(modeId, out IGameMode mode))
                return false;

            return TrySetGameMode(mode);
        }

        /// <summary>
        /// Tries to activate the supplied game mode.
        /// </summary>
        /// <param name="mode">Mode to activate.</param>
        /// <returns><see langword="true"/> when the mode was activated.</returns>
        public bool TrySetGameMode(IGameMode mode)
        {
            if (mode == null)
                return false;

            GameModeContext context = BuildContext();

            if (!mode.CanStart(context))
                return false;

            _activeMode?.Exit();
            _activeMode = mode;
            _activeMode.Enter(context);
            OnGameModeChanged.Invoke(_activeMode);
            return true;
        }

        /// <summary>
        /// Stops the currently active game mode.
        /// </summary>
        public void ClearGameMode()
        {
            if (_activeMode == null)
                return;

            _activeMode.Exit();
            _activeMode = null;
            OnGameModeChanged.Invoke(null);
        }

        /// <inheritdoc/>
        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }

        private GameModeContext BuildContext()
        {
            ServiceLocator.TryGet<RunSessionService>(out var session);
            ServiceLocator.TryGet<BalanceService>(out var balance);
            ServiceLocator.TryGet<CustomerService>(out var customers);
            ServiceLocator.TryGet<WaiterService>(out var waiters);
            return new GameModeContext(this, session, balance, customers, waiters);
        }

        private void CacheModes()
        {
            _modesById.Clear();

            foreach (MonoBehaviour behaviour in _gameModeBehaviours)
            {
                if (behaviour == null)
                    continue;

                if (behaviour is not IGameMode mode)
                {
                    Debug.LogWarning($"{behaviour.name} does not implement IGameMode.", behaviour);
                    continue;
                }

                if (_modesById.ContainsKey(mode.Id))
                {
                    Debug.LogWarning($"Duplicate game mode id '{mode.Id}' found on {behaviour.name}.", behaviour);
                    continue;
                }

                _modesById.Add(mode.Id, mode);
            }
        }
    }
}
