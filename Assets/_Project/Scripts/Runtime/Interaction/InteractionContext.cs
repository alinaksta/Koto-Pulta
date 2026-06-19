using Game.Items;
using Game.Player;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Carries player and hit data for a single interaction evaluation.
    /// </summary>
    public readonly struct InteractionContext
    {
        /// <summary>
        /// Gets the player's head position when the context was created.
        /// </summary>
        public readonly Vector3 HeadPosition;

        /// <summary>
        /// Gets the player's forward direction when the context was created.
        /// </summary>
        public readonly Vector3 HeadForward;

        /// <summary>
        /// Gets the current interaction raycast hit, when one exists.
        /// </summary>
        public readonly RaycastHit? Hit;

        /// <summary>
        /// Gets the focus handler that can start or stop focus mode.
        /// </summary>
        public readonly IFocusHandler FocusHandler;

        /// <summary>
        /// Gets the hand or container performing the interaction.
        /// </summary>
        public readonly IContainer ActiveHand;

        /// <summary>
        /// Gets the owning dual-hand interactor.
        /// </summary>
        public readonly IDualHandInteractor DualHandInteractor;

        /// <summary>
        /// Creates an interaction context with optional hit information.
        /// </summary>
        /// <param name="headPosition">Player head position.</param>
        /// <param name="headForward">Player head forward direction.</param>
        /// <param name="hit">Hit information for the current target.</param>
        /// <param name="focusHandler">Focus handler for the interacting player.</param>
        /// <param name="activeHand">Active hand or container.</param>
        /// <param name="dualHandInteractor">Owning dual-hand interactor.</param>
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

        /// <summary>
        /// Creates an interaction context without hit information.
        /// </summary>
        /// <param name="headPosition">Player head position.</param>
        /// <param name="headForward">Player head forward direction.</param>
        /// <param name="focusHandler">Focus handler for the interacting player.</param>
        /// <param name="activeHand">Active hand or container.</param>
        /// <param name="dualHandInteractor">Owning dual-hand interactor.</param>
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

        /// <summary>
        /// Creates a copy of this context with hit information attached.
        /// </summary>
        /// <param name="hit">Hit information to include.</param>
        /// <returns>A new context containing the provided hit.</returns>
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
