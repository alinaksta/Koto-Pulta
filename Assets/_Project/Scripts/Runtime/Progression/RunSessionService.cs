using Game.Lifecycle;
using Game.Services;
using System;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// Describes the current state of the active run.
    /// </summary>
    public enum RunSessionState
    {
        None,
        Running,
        Succeeded,
        Failed
    }

    /// <summary>
    /// Tracks generic run state independently from any specific game mode.
    /// </summary>
    public class RunSessionService : MonoBehaviour, IBootstrapable
    {
        private RunSessionState _state = RunSessionState.None;

        /// <summary>
        /// Gets the current run state.
        /// </summary>
        public RunSessionState State => _state;

        /// <summary>
        /// Gets whether the run is currently active.
        /// </summary>
        public bool IsRunning => _state == RunSessionState.Running;

        /// <summary>
        /// Raised after the run state changes.
        /// </summary>
        public event Action<RunSessionState> OnStateChanged = delegate { };

        /// <summary>
        /// Resets the run state to idle.
        /// </summary>
        public void ResetState()
            => SetState(RunSessionState.None);

        /// <summary>
        /// Marks the run as active.
        /// </summary>
        public void StartRun()
            => SetState(RunSessionState.Running);

        /// <summary>
        /// Marks the run as completed successfully.
        /// </summary>
        public void MarkSucceeded()
            => SetState(RunSessionState.Succeeded);

        /// <summary>
        /// Marks the run as failed.
        /// </summary>
        public void MarkFailed()
            => SetState(RunSessionState.Failed);

        /// <inheritdoc/>
        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }

        private void SetState(RunSessionState state)
        {
            if (_state == state)
                return;

            _state = state;
            OnStateChanged.Invoke(_state);
        }
    }
}
