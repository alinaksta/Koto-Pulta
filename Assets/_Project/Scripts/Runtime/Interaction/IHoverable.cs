namespace Game.Interaction
{
    /// <summary>
    /// Receives notifications while an object is targeted by the player.
    /// </summary>
    public interface IHoverable
    {
        /// <summary>
        /// Called when the object becomes the current hover target.
        /// </summary>
        void OnHoverEnter();

        /// <summary>
        /// Called each frame while the object remains the current hover target.
        /// </summary>
        /// <param name="delta">Frame delta time.</param>
        void OnHoverStay(float delta);

        /// <summary>
        /// Called when the object is no longer the current hover target.
        /// </summary>
        void OnHoverExit();
    }
}
