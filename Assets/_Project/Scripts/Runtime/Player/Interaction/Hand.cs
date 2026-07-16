using Game.Items;
using Game.Items.Components;
using Game.Items.Properties;
using System;
using UnityEngine;

namespace Game.Interaction
{
    public readonly struct ThrowPreviewData
    {
        public ThrowPreviewData(Vector3 start, Vector3 direction, float force)
        {
            Start = start;
            Direction = direction;
            Force = force;
        }

        public Vector3 Start { get; }
        public Vector3 Direction { get; }
        public float Force { get; }
    }

    /// <summary>
    /// Represents one player hand as a single-item container.
    /// </summary>
    public class Hand : IContainer
    {
        private Item? _item = null;
        private bool _visible = true;
        private float _holdTime = 0f;

        private readonly Transform _spawnPoint;
        private readonly PhysicsItemService _physicsItemService;

        private Vector3 _aimDirection;
        private ThrowableProperty _throwable;

        /// <inheritdoc/>
        public Item? Item => _item;

        /// <inheritdoc/>
        public bool IsEmpty => !_item.HasValue;

        /// <summary>
        /// Gets whether the hand is currently visible in the view model.
        /// </summary>
        public bool Visible => _visible;

        /// <summary>
        /// Raised when the hand visibility changes.
        /// </summary>
        public event Action<bool> OnSetVisible = delegate { };

        /// <inheritdoc/>
        public event Action<Item?> OnItemChanged = delegate { };

        /// <summary>
        /// Creates a hand backed by the supplied physics item service.
        /// </summary>
        /// <param name="physicsItemService">Service used when dropping items into the world.</param>
        /// <param name="spawnPoint">Where the item spawns after getting dropped or thrown.</param>
        public Hand(PhysicsItemService physicsItemService, Transform spawnPoint)
        {
            _physicsItemService = physicsItemService;
            _spawnPoint = spawnPoint;
        }

        /// <summary>
        /// Tries to describe the current throw preview for the held item.
        /// </summary>
        /// <param name="aimDirection">Current world-space aim direction.</param>
        /// <param name="preview">Current throw preview data.</param>
        /// <returns><see langword="true"/> when the held item should show a throw preview.</returns>
        public bool TryGetThrowPreview(Vector3 aimDirection, out ThrowPreviewData preview)
        {
            preview = default;

            if (IsEmpty || _throwable == null)
                return false;

            if (_holdTime < _throwable.HoldTime)
                return false;

            if (aimDirection.sqrMagnitude <= Mathf.Epsilon)
                return false;

            preview = new ThrowPreviewData(_spawnPoint.position, aimDirection, _throwable.ForwardForce);
            return true;
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
            _aimDirection = context.HeadForward;

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

            _aimDirection = context.HeadForward;

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

            _aimDirection = context.HeadForward;

            if (_throwable != null && _holdTime >= _throwable.HoldTime)
            {
                _holdTime = 0f;
                return TryDropItem(_aimDirection * _throwable.ForwardForce);
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
            _holdTime = 0f;
            _aimDirection = Vector3.zero;
            CacheThrowable(item);
            OnItemChanged.Invoke(_item);
        }

        /// <inheritdoc/>
        public Item? Remove()
        {
            var removed = _item;
            _item = null;
            _holdTime = 0f;
            _aimDirection = Vector3.zero;
            _throwable = null;
            OnItemChanged.Invoke(_item);
            return removed;
        }

        private void CacheThrowable(Item item)
        {
            _throwable = null;
            item.Definition.TryGetProperty(out _throwable);
        }

        /// <summary>
        /// Tries to drop the held item into the world.
        /// </summary>
        /// <param name="velocity">Initial velocity to apply.</param>
        /// <returns><see langword="true"/> when an item was released.</returns>
        public bool TryDropItem(Vector3 velocity)
        {
            if (IsEmpty)
                return false;

            Vector3 point = _spawnPoint.position;

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
