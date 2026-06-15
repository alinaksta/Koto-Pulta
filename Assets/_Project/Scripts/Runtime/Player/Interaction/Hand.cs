using Game.Items;
using System;
using UnityEngine;

namespace Game.Interaction
{
    public class Hand : IInteractable, IContainer
    {
        private Item? _item = null;
        private bool _visible = true;

        public Item? Item => _item;
        public bool IsEmpty => !_item.HasValue;
        public bool Visible => _visible;

        public event Action OnInteracted = delegate { };
        public event Action<bool> OnSetVisible = delegate { };
        public event Action<Item?> OnItemChanged = delegate { };

        public void SetVisible(bool visible)
        {
            if (_visible != visible)
            {
                _visible = visible;
                OnSetVisible.Invoke(visible);
            }
        }

        public bool CanInteract(in InteractionContext context) => true;

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

        public void Remove(Item item)
        {
            _item = null;
            OnItemChanged.Invoke(_item);
        }
    }
}