using Game.Items;
using Game.Items.Components;
using Game.Items.Properties;
using System;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Represents one player hand as a single-item container.
    /// </summary>
    public class Hand : IContainer
    {
        private Item? _item = null;
        private bool _visible = true;
        private float _holdTime = 0f;

        /// <inheritdoc/>
        public Item? Item => _item;

        /// <inheritdoc/>
        public bool IsEmpty => !_item.HasValue;

        /// <summary>
        /// Gets whether the hand is currently visible in the view model.
        /// </summary>
        public bool Visible => _visible;

        /// <summary>
        /// Event hook for custom hand interaction notifications.
        /// </summary>
        public event Action OnInteracted = delegate { };

        /// <summary>
        /// Raised when the hand visibility changes.
        /// </summary>
        public event Action<bool> OnSetVisible = delegate { };

        /// <inheritdoc/>
        public event Action<Item?> OnItemChanged = delegate { };

        private PhysicsItemService _physicsItemService;

        /// <summary>
        /// Creates a hand backed by the supplied physics item service.
        /// </summary>
        /// <param name="physicsItemService">Service used when dropping items into the world.</param>
        public Hand(PhysicsItemService physicsItemService)
        {
            _physicsItemService = physicsItemService;
        }

        /// <summary>
        /// Shows or hides the hand view and notifies listeners when it changes.
        /// </summary>
        /// <param name="visible">Whether the hand should be visible.</param>
        public void SetVisible(bool visible)
        {
            if (_visible != visible)
            {
                _visible = visible;
                OnSetVisible.Invoke(visible);
            }
        }

        /// <summary>
        /// Checks whether the hand can participate in interactions.
        /// </summary>
        /// <param name="context">Current interaction context.</param>
        /// <returns>Always <see langword="true"/>.</returns>
        public bool CanInteract(in InteractionContext context) => true;

        /// <summary>
        /// Tries to begin interacting with the item currently held in the hand.
        /// </summary>
        /// <param name="context">Current interaction context.</param>
        /// <returns><see langword="true"/> when the held item consumed the interaction.</returns>
        public bool TryStartInteractionWithItemInHand(in InteractionContext context)
        {
            if (IsEmpty)
                return false;

            // TODO: Add some interaction logic

            return false;
        }

        /// <summary>
        /// Updates hold state for the item currently held in the hand.
        /// </summary>
        /// <param name="context">Current interaction context.</param>
        /// <param name="delta">Frame delta time.</param>
        /// <returns><see langword="true"/> while the hand is tracking a held item.</returns>
        public bool TryHoldInteractionWithItemInHand(in InteractionContext context, float delta)
        {
            if (IsEmpty)
                return false;

            _holdTime += delta;

            return true;
        }

        /// <summary>
        /// Ends held-item interaction and throws the item when its hold rule is met.
        /// </summary>
        /// <param name="context">Current interaction context.</param>
        /// <returns><see langword="true"/> when ending the interaction caused a drop or throw.</returns>
        public bool TryEndInteractionWithItemInHand(in InteractionContext context)
        {
            if (IsEmpty)
                return false;

            var definition = Item.Value.Definition;
            if (definition.TryGetProperty<ThrowableProperty>(out var throwable) && _holdTime > throwable.HoldTime)
            {
                _holdTime = 0f;
                return TryDropItem(context.HeadPosition + context.HeadForward, context.HeadForward * throwable.ForwardForce);
            }

            _holdTime = 0f;

            return false;
        }

        /// <summary>
        /// Handles a world interaction by transferring items between containers.
        /// </summary>
        /// <param name="context">Current interaction context.</param>
        public void OnInteractionStarted(in InteractionContext context)
        {
            if (context.Hit.HasValue)
            {
                var hitObject = context.Hit.Value.collider.gameObject;
                IContainer container = null;
                bool isContainer = hitObject.TryGetComponent<IContainer>(out container);
                if (!isContainer)
                {
                    isContainer = hitObject.TryGetComponent<IContainerHolder>(out var holder);
                    container = isContainer ? holder.Container : null;
                }
                
                if (isContainer)
                {
                    if (IsEmpty)
                        ItemTransferUtility.TryTransfer(container, this);
                    else
                        ItemTransferUtility.TryTransfer(this, container);
                }
            }
        }

        /// <summary>
        /// Receives held-interaction updates after interaction has started.
        /// </summary>
        /// <param name="context">Current interaction context.</param>
        /// <param name="delta">Frame delta time.</param>
        public void OnInteractionHeld(in InteractionContext context, float delta) { }

        /// <summary>
        /// Called when the current hand interaction ends.
        /// </summary>
        /// <param name="context">Current interaction context.</param>
        public void OnInteractionStopped(in InteractionContext context) { }

        /// <inheritdoc/>
        public bool CanInsert(in TransferRequest request) => IsEmpty;

        /// <inheritdoc/>
        public bool CanRemove(in TransferRequest request) => !IsEmpty;

        /// <inheritdoc/>
        public void Insert(Item item)
        {
            _item = item;
            OnItemChanged.Invoke(_item);
        }

        /// <inheritdoc/>
        public Item? Remove()
        {
            var removed = _item;
            _item = null;
            OnItemChanged.Invoke(_item);
            return removed;
        }

        /// <summary>
        /// Tries to drop the held item into the world.
        /// </summary>
        /// <param name="point">Spawn point for the dropped item.</param>
        /// <param name="velocity">Initial velocity to apply.</param>
        /// <returns><see langword="true"/> when an item was released.</returns>
        public bool TryDropItem(Vector3 point, Vector3 velocity)
        {
            if (IsEmpty)
                return false;

            var item = _item.Value;
            if (item.TryGetComponent<WaiterComponent>(out var waiterComponent))
            {
                var removed = Remove();

                if (velocity.sqrMagnitude > 0f)
                    waiterComponent.Waiter.ThrowFromHand(removed.Value, point, velocity);
                else
                    waiterComponent.Waiter.DropFromHand(removed.Value, point);

                return true;
            }

            return _physicsItemService.TrySpawnFromContainer(point, velocity, this);
        }
    }
}
