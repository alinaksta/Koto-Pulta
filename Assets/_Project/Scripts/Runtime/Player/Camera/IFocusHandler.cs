using Game.Interaction;

namespace Game.Player
{
    /// <summary>
    /// Controls focus transitions for focusable objects.
    /// </summary>
    public interface IFocusHandler
    {
        /// <summary>
        /// Gets the current focus state.
        /// </summary>
        FocusStatus FocusStatus { get; }

        /// <summary>
        /// Gets the object currently focused, if any.
        /// </summary>
        IFocusable FocusedObject { get; }

        /// <summary>
        /// Attempts to start focusing the supplied object.
        /// </summary>
        /// <param name="focusable">Object to focus.</param>
        /// <returns><see langword="true"/> when focus started.</returns>
        bool TryBeginFocus(IFocusable focusable);

        /// <summary>
        /// Sets the current mouse lock state.
        /// </summary>
        /// <param name="locked">Whether the cursor should be locked.</param>
        void SetMouseLocked(bool locked);

        /// <summary>
        /// Restores the default mouse lock state for focus handling.
        /// </summary>
        void ClearMouseLocked();

        /// <summary>
        /// Ends the current focus, if one is active.
        /// </summary>
        void EndFocus();
    }
}
