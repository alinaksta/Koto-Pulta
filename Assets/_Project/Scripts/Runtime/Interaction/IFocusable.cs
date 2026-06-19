using Game.Player;

namespace Game.Interaction
{
    /// <summary>
    /// Represents an object the camera can focus onto.
    /// </summary>
    public interface IFocusable
    {
        /// <summary>
        /// Gets the target camera pose used while focused.
        /// </summary>
        FocusTarget Target { get; }

        /// <summary>
        /// Gets the transition duration used when entering focus.
        /// </summary>
        float StartFocusTransitionDuration { get; }

        /// <summary>
        /// Gets the transition duration used when leaving focus.
        /// </summary>
        float EndFocusTransitionDuration { get; }

        /// <summary>
        /// Called once when focus begins.
        /// </summary>
        void OnFocusStarted();

        /// <summary>
        /// Called each frame while the object remains focused.
        /// </summary>
        /// <param name="delta">Frame delta time spent focused.</param>
        void OnFocusHeld(float delta);

        /// <summary>
        /// Called once when focus ends.
        /// </summary>
        void OnFocusEnded();
    }
}
