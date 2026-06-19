using Game.Characters;
using Itemworks.Core;
using System;

namespace Game.Items.Components
{
    /// <summary>
    /// Stores a runtime waiter reference on an item instance.
    /// </summary>
    [Serializable]
    public class WaiterComponent : ItemComponent
    {
        /// <summary>
        /// Waiter represented by the item instance.
        /// </summary>
        public Waiter Waiter;
    }
}
