using Game.Interaction;
using Game.Items;
using Itemworks.UnityEngine;
using UnityEngine;

namespace Game.Prototype.Kitchen
{
    /// <summary>
    /// Spot on the counter where the stapler lives.
    /// Click with an empty hand to pick the stapler up (it becomes an item), click with the stapler to put it back.
    /// </summary>
    public sealed class StaplerStation : ProtoStation
    {
        public ItemDefinitionAsset Stapler;

        private bool _hasStapler = true;

        protected override bool Interact(in InteractionContext context)
        {
            IContainer hand = context.ActiveHand;
            if (hand == null)
                return false;

            if (_hasStapler)
            {
                if (!GiveToHand(hand, Definition(Stapler)))
                    return false;

                SetStaplerVisible(false);
                return true;
            }

            if (!Holds(hand, Stapler))
                return false;

            TakeFromHand(hand);
            SetStaplerVisible(true);
            return true;
        }

        private void SetStaplerVisible(bool visible)
        {
            _hasStapler = visible;
            foreach (var renderer in Visual.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = visible;
        }
    }
}
