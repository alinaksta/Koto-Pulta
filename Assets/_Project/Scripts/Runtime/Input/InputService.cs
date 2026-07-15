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
        private bool _gameplaySuppressed;

        private void Awake()
        {
            _actions = new InputActions();
            _actions.Gameplay.Enable();
        }

        /// <inheritdoc/>
        public Vector2 Move => _gameplaySuppressed ? Vector2.zero : _actions.Gameplay.Move.ReadValue<Vector2>();

        /// <inheritdoc/>
        public bool GameplaySuppressed => _gameplaySuppressed;

        /// <inheritdoc/>
        public Vector2 MouseDelta => _gameplaySuppressed ? Vector2.zero : _actions.Gameplay.MouseDelta.ReadValue<Vector2>();

        /// <inheritdoc/>
        public Vector2 MouseScroll => _gameplaySuppressed ? Vector2.zero : _actions.Gameplay.MouseScroll.ReadValue<Vector2>();

        /// <inheritdoc/>
        public Vector2 MousePosition => _actions.Gameplay.MousePosition.ReadValue<Vector2>();

        /// <inheritdoc/>
        public ButtonState Jump => GetGameplayButtonState(_actions.Gameplay.Jump);

        /// <inheritdoc/>
        public ButtonState InteractLeft => GetGameplayButtonState(_actions.Gameplay.InteractLeft);

        /// <inheritdoc/>
        public ButtonState InteractRight => GetGameplayButtonState(_actions.Gameplay.InteractRight);

        /// <inheritdoc/>
        public ButtonState DropLeft => GetGameplayButtonState(_actions.Gameplay.DropLeft);

        /// <inheritdoc/>
        public ButtonState DropRight => GetGameplayButtonState(_actions.Gameplay.DropRight);

        /// <inheritdoc/>
        public ButtonState Cancel => GetGameplayButtonState(_actions.Gameplay.Cancel);

        /// <inheritdoc/>
        public ButtonState DialogueAdvance
        {
            get
            {
                ButtonState left = GetButtonState(_actions.Gameplay.InteractLeft);
                ButtonState right = GetButtonState(_actions.Gameplay.InteractRight);
                return new ButtonState(
                    left.Pressed || right.Pressed,
                    left.Held || right.Held,
                    left.Released || right.Released);
            }
        }

        /// <inheritdoc/>
        public void SetGameplaySuppressed(bool suppressed)
        {
            _gameplaySuppressed = suppressed;
        }

        /// <inheritdoc/>
        public ButtonState Pause => GetButtonState(_actions.Gameplay.Pause);

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

        private ButtonState GetGameplayButtonState(InputAction action)
        {
            return _gameplaySuppressed ? default : GetButtonState(action);
        }

        /// <inheritdoc/>
        public void Bootstrap()
        {
            ServiceLocator.Register<IInputService>(this);
        }
    }
}
