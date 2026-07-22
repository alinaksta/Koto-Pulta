using Game.Items;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Exposes access to the player's left and right hands.
    /// </summary>
    public interface IDualHandInteractor
    {
        /// <summary>
        /// Gets the object currently targeted by the interaction ray.
        /// </summary>
        GameObject HoveredObject { get; }

        /// <summary>
        /// Gets a hand by side.
        /// </summary>
        /// <param name="handType">Hand side to retrieve.</param>
        /// <returns>The requested hand.</returns>
        Hand GetHand(HandType handType);

        /// <summary>
        /// Tries to get a currently empty hand.
        /// </summary>
        /// <param name="freeHand">Receives an empty hand when one exists.</param>
        /// <returns><see langword="true"/> when at least one hand is empty.</returns>
        bool TryGetFreeHand(out IContainer freeHand);
    }
}
