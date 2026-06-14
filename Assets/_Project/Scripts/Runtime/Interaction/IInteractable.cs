namespace Game.Interaction
{
    public interface IInteractable
    {
        void OnInteractionStarted(in InteractionContext context);
        void OnInteractionHeld(in InteractionContext context, float delta);
        void OnInteractionStopped(in InteractionContext context);

        bool CanInteract(in InteractionContext context);
    }

    public readonly struct InteractionResult
    {
        public readonly bool WasAccepted;
    }
}