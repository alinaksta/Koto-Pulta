using Game.Items;
using Game.Items.Properties;
using Game.Services;
using System;
using UnityEngine;

namespace Game.Interaction
{
    public class Hand : IContainer
    {
        private Item? _item = null;
        private bool _visible = true;
        private float _holdTime = 0f;

        public Item? Item => _item;
        public bool IsEmpty => !_item.HasValue;
        public bool Visible => _visible;

        public event Action OnInteracted = delegate { };
        public event Action<bool> OnSetVisible = delegate { };
        public event Action<Item?> OnItemChanged = delegate { };

        private PhysicsItemService _physicsItemService;

        public Hand()
        {
            _physicsItemService = ServiceLocator.Get<PhysicsItemService>();
        }

        public void SetVisible(bool visible)
        {
            if (_visible != visible)
            {
                _visible = visible;
                OnSetVisible.Invoke(visible);
            }
        }

        public bool CanInteract(in InteractionContext context) => true;

        public bool TryStartInteractionWithItemInHand(in InteractionContext context)
        {
            if (IsEmpty)
                return false;

            // TODO: Add some interaction logic

            return true;
        }

        public bool TryHoldInteractionWithItemInHand(in InteractionContext context, float delta)
        {
            if (IsEmpty)
                return false;

            _holdTime += delta;

            return true;
        }

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

        public void OnInteractionStarted(in InteractionContext context)
        {
            Debug.Log($"HandInteraction with hit? {context.Hit.HasValue}");
            if (context.Hit.HasValue)
            {
                var hitObject = context.Hit.Value.transform.gameObject;
                IContainer container = null;
                bool isContainer = hitObject.TryGetComponent<IContainer>(out container);
                if (!isContainer)
                {
                    isContainer = hitObject.TryGetComponent<IContainerHolder>(out var holder);
                    container = isContainer ? holder.Container : null;
                }

                Debug.Log($"IsContainer: {isContainer}");
                
                if (isContainer)
                {
                    if (IsEmpty)
                        ItemTransferUtility.TryTransfer(container, this);
                    else
                        ItemTransferUtility.TryTransfer(this, container);
                }
            }
        }

        public void OnInteractionHeld(in InteractionContext context, float delta) { }
        public void OnInteractionStopped(in InteractionContext context) { }

        public bool CanInsert(in TransferRequest request) => IsEmpty;
        public bool CanRemove(in TransferRequest request) => !IsEmpty;

        public void Insert(Item item)
        {
            _item = item;
            OnItemChanged.Invoke(_item);
        }

        public Item? Remove()
        {
            var removed = _item;
            _item = null;
            OnItemChanged.Invoke(_item);
            return removed;
        }

        public bool TryDropItem(Vector3 point, Vector3 velocity)
        {
            if (IsEmpty)
                return false;

            return _physicsItemService.TrySpawnFromContainer(point, velocity, this);
        }
    }
}