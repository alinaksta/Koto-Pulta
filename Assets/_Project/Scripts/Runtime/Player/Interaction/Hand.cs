using System;

namespace Game.Interaction
{
    public class Hand : IInteractable
    {
        private bool _visible = true;

        public bool Visible => _visible;

        public event Action OnInteracted = delegate { };
        public event Action<bool> OnSetVisible = delegate { };

        public void SetVisible(bool visible)
        {
            if (_visible != visible)
            {
                _visible = visible;
                OnSetVisible.Invoke(visible);
            }
        }

        public bool CanInteract(in InteractionContext context) => true;

        public void OnInteractionStarted(in InteractionContext context)
        {
            OnInteracted.Invoke();
        }

        public void OnInteractionHeld(in InteractionContext context, float delta) { }

        public void OnInteractionStopped(in InteractionContext context) { }
    }
}