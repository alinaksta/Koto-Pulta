using Game.Items.Components;
using Itemworks.Core;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Items.Properties
{
    /// <summary>
    /// Stores the world sprite used to display an item.
    /// </summary>
    [Serializable]
    public class FoodProperty : ItemProperty
    {
        /// <summary>
        /// Sprite shown for the item in world-space views.
        /// </summary>
        public Sprite WorldSprite;

        /// <summary>
        /// Sprite shown for the item in dialogues (waiter for example).
        /// </summary>
        public Sprite DialogueSprite;

        /// <summary>
        /// How much money you get for successfully delivering this item.
        /// </summary>
        public int UnitPrice = 10;
    }

    /// <summary>
    /// Stores information on when can this item be spawned in the shift game mode.
    /// </summary>
    [Serializable]
    public class ShiftProperty : ItemProperty
    {
        /// <summary>
        /// At which shift this item can be get using computer
        /// </summary>
        public int RequiredShift = 0;
    }

    /// <summary>
    /// Stores the sprite used while the item is held.
    /// </summary>
    [Serializable]
    public class HandSpriteProperty : ItemProperty
    {
        /// <summary>
        /// WorldSprite shown while the item is in hand.
        /// </summary>
        public Sprite HandSprite;
    }

    /// <summary>
    /// Configures how long an item must be held before it is thrown.
    /// </summary>
    [Serializable]
    public class ThrowableProperty : ItemProperty
    {
        /// <summary>
        /// Minimum hold time before releasing becomes a throw.
        /// </summary>
        public float HoldTime = 0.8f;

        /// <summary>
        /// Forward velocity applied when the item is thrown.
        /// </summary>
        public float ForwardForce = 6f;
    }

    /// <summary>
    /// Marks an item definition as producing a waiter runtime component.
    /// </summary>
    [Serializable]
    public class WaiterProperty : ItemProperty, IItemInitializer
    {
        /// <inheritdoc/>
        public void OnInstanceCreated(ItemInstance instance)
        {
            instance.AddComponent(new WaiterComponent());
        }
    }
} 
