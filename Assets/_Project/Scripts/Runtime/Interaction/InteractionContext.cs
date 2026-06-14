using Game.Player;
using UnityEngine;

namespace Game.Interaction
{
    public readonly struct InteractionContext
    {
        public readonly Vector3 HeadPosition;
        public readonly Vector3 HeadForward;

        public readonly RaycastHit? Hit;

        public readonly IFocusHandler FocusHandler;

        public InteractionContext(Vector3 headPosition, Vector3 headForward, RaycastHit? hit, IFocusHandler focusHandler)
        {
            HeadPosition = headPosition;
            HeadForward = headForward;
            Hit = hit;
            FocusHandler = focusHandler;
        }

        public InteractionContext(Vector3 headPosition, Vector3 headForward, IFocusHandler focusHandler) : this()
        {
            HeadPosition = headPosition;
            HeadForward = headForward;
            Hit = null;
            FocusHandler = focusHandler;
        }

        public InteractionContext WithHitInfo(in RaycastHit hit)
        {
            return new InteractionContext(HeadPosition, HeadForward, hit, FocusHandler);
        }
    }
}