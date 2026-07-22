using UnityEngine;

namespace Game.Input
{
    /// <summary>
    /// Stores button state for the current frame.
    /// </summary>
    public readonly struct ButtonState
    {
        /// <summary>
        /// Gets whether the button was pressed this frame.
        /// </summary>
        public bool Pressed { get; }

        /// <summary>
        /// Gets whether the button is currently held.
        /// </summary>
        public bool Held { get; }

        /// <summary>
        /// Gets whether the button was released this frame.
        /// </summary>
        public bool Released { get; }

        /// <summary>
        /// Creates a button state snapshot for the current frame.
        /// </summary>
        /// <param name="pressed">Whether the button was pressed this frame.</param>
        /// <param name="held">Whether the button is currently held.</param>
        /// <param name="released">Whether the button was released this frame.</param>
        public ButtonState(bool pressed, bool held, bool released)
        {
            Pressed = pressed;
            Held = held;
            Released = released;
        }
    }

    /// <summary>
    /// Provides gameplay input values for the current frame.
    /// </summary>
    public interface IInputService
    {
        /// <summary>
        /// Gets the current movement input.
        /// </summary>
        public Vector2 Move { get; }

        /// <summary>
        /// Gets whether gameplay input is currently suppressed by a modal UI.
        /// </summary>
        public bool GameplaySuppressed { get; }

        /// <summary>
        /// Gets the mouse delta since the previous frame.
        /// </summary>
        public Vector2 MouseDelta { get; }

        /// <summary>
        /// Gets the current mouse scroll input.
        /// </summary>
        public Vector2 MouseScroll { get; }

        /// <summary>
        /// Gets the current mouse position.
        /// </summary>
        public Vector2 MousePosition { get; }

        /// <summary>
        /// Gets the current jump button state.
        /// </summary>
        public ButtonState Jump { get; }

        /// <summary>
        /// Gets the current left interaction button state.
        /// </summary>
        public ButtonState InteractLeft { get; }

        /// <summary>
        /// Gets the current right interaction button state.
        /// </summary>
        public ButtonState InteractRight { get; }

        /// <summary>
        /// Gets the current left drop button state.
        /// </summary>
        public ButtonState DropLeft { get; }

        /// <summary>
        /// Gets the current right drop button state.
        /// </summary>
        public ButtonState DropRight { get; }

        /// <summary>
        /// Gets the current cancel button state.
        /// </summary>
        public ButtonState Cancel { get; }

        /// <summary>
        /// Gets the current cancel button state.
        /// </summary>
        public ButtonState Pause { get; }

        /// Gets the unsuppressed input used to advance modal dialogue.
        /// </summary>
        public ButtonState DialogueAdvance { get; }

        /// <summary>
        /// Enables or disables input routed to gameplay systems.
        /// </summary>
        /// <param name="suppressed">Whether gameplay input should be suppressed.</param>
        public void SetGameplaySuppressed(bool suppressed);
    }
}
