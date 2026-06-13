using UnityEngine;

namespace Game.Input
{
    public readonly struct ButtonState
    {
        public bool Pressed { get; }
        public bool Held { get; }
        public bool Released { get; }

        public ButtonState(bool pressed, bool held, bool released)
        {
            Pressed = pressed;
            Held = held;
            Released = released;
        }
    }

    public interface IInputService
    {
        public Vector2 Move { get; }

        public Vector2 MouseDelta { get; }
        public Vector2 MouseScroll { get; }

        public ButtonState Jump { get; }

        public ButtonState InteractLeft { get; }
        public ButtonState InteractRight { get; }
        public ButtonState DropLeft { get; }
        public ButtonState DropRight { get; }

        public ButtonState Cancel { get; }
    }
}