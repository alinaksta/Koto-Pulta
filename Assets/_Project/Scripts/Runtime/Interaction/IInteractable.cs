using Game.Player;

namespace Game.Interaction
{
    public interface IInteractable
    {
        void Interact();
    }

    public interface IFocusInteractable
    {
        CameraTarget CameraTarget { get; }
        float ResetTransitionDuration { get; }

        void BeginInteraction();
        void EndInteraction();
    }
}