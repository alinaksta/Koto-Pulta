using Game.Player;

namespace Game.Interaction
{
    public interface IFocusable
    {
        FocusTarget Target { get; }
        float StartFocusTransitionDuration { get; }
        float EndFocusTransitionDuration { get; }

        void OnFocusStarted();
        void OnFocusHeld(float delta);
        void OnFocusEnded();
    }
}