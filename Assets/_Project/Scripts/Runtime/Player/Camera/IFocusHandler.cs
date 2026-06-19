using Game.Interaction;

namespace Game.Player
{
    public interface IFocusHandler
    {
        FocusStatus FocusStatus { get; }
        IFocusable FocusedObject { get; }

        bool TryBeginFocus(IFocusable focusable);
        void SetMouseLocked(bool locked);
        void ClearMouseLocked();
        void EndFocus();
    }
}