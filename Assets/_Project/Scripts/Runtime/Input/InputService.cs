using Game.Input.Generated;
using Game.Lifecycle;
using Game.Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Input
{
    /// <summary>
    /// Reads gameplay input from the generated input actions asset.
    /// </summary>
    public class InputService : MonoBehaviour, IInputService, IBootstrapable
    {
        private InputActions _actions;

        private void Awake()
        {
            _actions = new InputActions();
            _actions.Gameplay.Enable();
        }

        /// <inheritdoc/>
        public Vector2 Move => _actions.Gameplay.Move.ReadValue<Vector2>();

        /// <inheritdoc/>
        public Vector2 MouseDelta => _actions.Gameplay.MouseDelta.ReadValue<Vector2>();

        /// <inheritdoc/>
        public Vector2 MouseScroll => _actions.Gameplay.MouseScroll.ReadValue<Vector2>();

        /// <inheritdoc/>
        public Vector2 MousePosition => _actions.Gameplay.MousePosition.ReadValue<Vector2>();

        /// <inheritdoc/>
        public ButtonState Jump => GetButtonState(_actions.Gameplay.Jump);

        /// <inheritdoc/>
        public ButtonState InteractLeft => GetButtonState(_actions.Gameplay.InteractLeft);

        /// <inheritdoc/>
        public ButtonState InteractRight => GetButtonState(_actions.Gameplay.InteractRight);

        /// <inheritdoc/>
        public ButtonState DropLeft => GetButtonState(_actions.Gameplay.DropLeft);

        /// <inheritdoc/>
        public ButtonState DropRight => GetButtonState(_actions.Gameplay.DropRight);

        /// <inheritdoc/>
        public ButtonState Cancel => GetButtonState(_actions.Gameplay.Cancel);

        /// <summary>
        /// Reads a frame snapshot for the supplied input action.
        /// </summary>
        /// <param name="action">Action to evaluate.</param>
        /// <returns>The action state for the current frame.</returns>
        public ButtonState GetButtonState(InputAction action)
        {
            return new ButtonState(
                action.WasPressedThisFrame(),
                action.IsPressed(),
                action.WasReleasedThisFrame());
        }

        /// <inheritdoc/>
        public void Bootstrap()
        {
            ServiceLocator.Register<IInputService>(this);
        }
    }
}
