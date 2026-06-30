using Game.Player;
using Game.UI;
using System;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Allows the player to focus onto a computer-style interaction target.
    /// </summary>
    public class ComputerInteractable : MonoBehaviour, IFocusable, IInteractable
    {
        [SerializeField] private FocusTarget _focusTarget;
        [SerializeField] private float _startFocusDuration = 2f;
        [SerializeField] private float _endFocusDuration = 2f;


        /// <summary>
        /// Raised when the player enters focus on this computer.
        /// </summary>
        public event Action FocusStarted = delegate { };

        /// <summary>
        /// Raised when the player exits focus on this computer.
        /// </summary>
        public event Action FocusEnded = delegate { };

        /// <inheritdoc/>
        public FocusTarget Target => _focusTarget;

        /// <summary>
        /// Gets the interactor currently using this computer.
        /// </summary>
        public IDualHandInteractor CurrentInteractor { get; private set; }

        /// <summary>
        /// Gets whether a player is currently using this computer.
        /// </summary>
        public bool HasInteractor => CurrentInteractor != null;

        /// <inheritdoc/>
        public float StartFocusTransitionDuration => _startFocusDuration;

        /// <inheritdoc/>
        public float EndFocusTransitionDuration => _endFocusDuration;

        private IFocusHandler _focusHandler;


        /// <inheritdoc/>
        /// <remarks>
        /// Interaction only succeeds while the player is facing the front of the focus target.
        /// </remarks>
        public bool CanInteract(in InteractionContext context)
        {
            return Vector3.Dot(_focusTarget.Transform.forward, context.HeadForward) >= 0.4;
        }

        /// <inheritdoc/>
        public void OnFocusEnded()
        {
            _focusHandler.ClearMouseLocked();
            FocusEnded.Invoke();
            CurrentInteractor = null;
            _focusHandler = null;
            Debug.Log("Exited computer");
        }

        /// <inheritdoc/>
        public void OnFocusHeld(float delta)
        {
            // noop for now
        }

        /// <inheritdoc/>
        public void OnFocusStarted()
        {
            _focusHandler.SetMouseLocked(false);
            FocusStarted.Invoke();
            Debug.Log("Entered computer");
        }

        /// <summary>
        /// Ends focus mode for the current user.
        /// </summary>
        public void ExitComputer()
        {
            _focusHandler.EndFocus();
        }

        /// <inheritdoc/>
        public void OnInteractionStarted(in InteractionContext context)
        {
            _focusHandler = context.FocusHandler;
            CurrentInteractor = context.DualHandInteractor;
            bool success = context.FocusHandler.TryBeginFocus(this);
            if (!success)
            {
                CurrentInteractor = null;
                _focusHandler = null;
            }
        }

        /// <inheritdoc/>
        public void OnInteractionHeld(in InteractionContext context, float delta) { }

        /// <inheritdoc/>
        public void OnInteractionStopped(in InteractionContext context) { }
    }
}
