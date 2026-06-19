namespace Game.Interaction
{
    /// <summary>
    /// Handles interaction input routed from the player.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Called on the frame an interaction begins.
        /// </summary>
        /// <param name="context">Interaction state for the current actor and target.</param>
        void OnInteractionStarted(in InteractionContext context);

        /// <summary>
        /// Called each frame while an interaction is held.
        /// </summary>
        /// <param name="context">Interaction state for the current actor and target.</param>
        /// <param name="delta">Frame delta time for the held interaction.</param>
        void OnInteractionHeld(in InteractionContext context, float delta);

        /// <summary>
        /// Called when an active interaction is released.
        /// </summary>
        /// <param name="context">Interaction state for the current actor and target.</param>
        void OnInteractionStopped(in InteractionContext context);

        /// <summary>
        /// Checks whether interaction can begin with the current context.
        /// </summary>
        /// <param name="context">Interaction state for the current actor and target.</param>
        /// <returns><see langword="true"/> when interaction is allowed.</returns>
        bool CanInteract(in InteractionContext context);
    }

    /// <summary>
    /// Represents a simple interaction acceptance result.
    /// </summary>
    public readonly struct InteractionResult
    {
        /// <summary>
        /// Gets whether the interaction was accepted.
        /// </summary>
        public readonly bool WasAccepted;
    }
}
