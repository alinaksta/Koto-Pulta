using System;

namespace Game.Interaction
{
    public class Hand : IInteractable
    {
        public event Action OnInteracted = delegate { };
        public event Action<bool> OnSetVisible = delegate { };

        private bool _visible = true;

        public bool Visible => _visible;

        public void Interact()
        {
            OnInteracted.Invoke();
        }

        public void SetVisible(bool visible)
        {
            if (_visible != visible)
            {
                _visible = visible;
                OnSetVisible.Invoke(visible);
            }
        }
    }
}