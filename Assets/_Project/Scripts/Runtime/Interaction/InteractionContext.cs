using Game.Items;
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
        public readonly IContainer ActiveHand;
        public readonly IDualHandInteractor DualHandInteractor;

        public InteractionContext(
            Vector3 headPosition, 
            Vector3 headForward, 
            RaycastHit? hit, 
            IFocusHandler focusHandler, 
            IContainer activeHand,
            IDualHandInteractor dualHandInteractor)
        {
            HeadPosition = headPosition;
            HeadForward = headForward;
            Hit = hit;
            FocusHandler = focusHandler;
            ActiveHand = activeHand;
            DualHandInteractor = dualHandInteractor;
        }

        public InteractionContext(
            Vector3 headPosition, 
            Vector3 headForward, 
            IFocusHandler focusHandler, 
            IContainer activeHand,
            IDualHandInteractor dualHandInteractor)
        {
            HeadPosition = headPosition;
            HeadForward = headForward;
            Hit = null;
            FocusHandler = focusHandler;
            ActiveHand = activeHand;
            DualHandInteractor = dualHandInteractor;
        }

        public InteractionContext WithHitInfo(in RaycastHit hit)
        {
            return new InteractionContext(
                HeadPosition, 
                HeadForward, 
                hit, 
                FocusHandler, 
                ActiveHand, 
                DualHandInteractor);
        }
    }
}
