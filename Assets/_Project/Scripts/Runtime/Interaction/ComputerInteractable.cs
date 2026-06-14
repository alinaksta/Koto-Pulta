using Game.Player;
using UnityEngine;

namespace Game.Interaction
{
    public class ComputerInteractable : MonoBehaviour, IFocusable, IInteractable
    {
        [SerializeField] private FocusTarget _focusTarget;
        [SerializeField] private float _startFocusDuration = 2f;
        [SerializeField] private float _endFocusDuration = 2f;

        public FocusTarget Target => _focusTarget;

        public float StartFocusTransitionDuration => _startFocusDuration;
        public float EndFocusTransitionDuration => _endFocusDuration;

        private IFocusHandler _focusHandler;

        public bool CanInteract(in InteractionContext context)
        {
            return Vector3.Dot(_focusTarget.Transform.forward, context.HeadForward) >= 0.4;
        }

        public void OnFocusEnded()
        {
            _focusHandler.ClearMouseLocked();
            _focusHandler = null;
            Debug.Log("Exited computer");
        }

        public void OnFocusHeld(float delta)
        {
            // noop for now
        }

        public void OnFocusStarted()
        {
            _focusHandler.SetMouseLocked(false);
            Debug.Log("Entered computer");
        }

        public void ExitComputer()
        {
            _focusHandler.EndFocus();
        }

        public void OnInteractionStarted(in InteractionContext context)
        {
            _focusHandler = context.FocusHandler;
            bool success = context.FocusHandler.TryBeginFocus(this);
            if (!success)
                _focusHandler = null;
        }

        public void OnInteractionHeld(in InteractionContext context, float delta) { }
        public void OnInteractionStopped(in InteractionContext context) { }
    }
}