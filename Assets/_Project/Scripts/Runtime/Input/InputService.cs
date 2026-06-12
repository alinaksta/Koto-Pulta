using Game.Input.Generated;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Input
{
    //TODO: Remove DefaultExecutionOrder if there is a bootstrap sequence
    [RequireComponent(typeof(PlayerInput)), DefaultExecutionOrder(-204)]
    public class InputService : MonoBehaviour, IInputService
    {
        private InputActions _actions;

        private void Awake()
        {
            _actions = new InputActions();
            _actions.Gameplay.Enable();
        }

        public Vector2 Move => _actions.Gameplay.Move.ReadValue<Vector2>();

        public Vector2 MouseDelta => _actions.Gameplay.MouseDelta.ReadValue<Vector2>();
        public Vector2 MouseScroll => _actions.Gameplay.MouseScroll.ReadValue<Vector2>();

        public ButtonState Jump => GetButtonState(_actions.Gameplay.Jump);

        public ButtonState InteractLeft => GetButtonState(_actions.Gameplay.InteractLeft);
        public ButtonState InteractRight => GetButtonState(_actions.Gameplay.InteractRight);
        public ButtonState DropLeft => GetButtonState(_actions.Gameplay.DropLeft);
        public ButtonState DropRight => GetButtonState(_actions.Gameplay.DropRight);

        public ButtonState GetButtonState(InputAction action)
        {
            return new ButtonState(
                action.WasPressedThisFrame(),
                action.IsPressed(),
                action.WasReleasedThisFrame());
        }
    }
}