using Game.Interaction;
using Game.Items;
using Itemworks.Core;
using System;
using UnityEngine;

namespace Game.Characters
{
    public enum CustomerState
    {
        None,
        AwaitingWaiter,
        AwaitingDelivery
    }
    
    public class Customer : MonoBehaviour, IInteractable
    {
        [SerializeField] private float _defaultWaitTime = 60f;

        private Table _table;
        private ItemDefinition _order;
        private float _waitTimer;
        private CustomerState _state;

        public Table Table => _table;
        public ItemDefinition Order => _order;
        public float WaitTimer => _waitTimer;
        public bool IsWaiting => Order != null && WaitTimer > 0f;

        public event Action<Customer> OnOrderStarted = delegate { };
        public event Action<Customer> OnServed = delegate { };
        public event Action<Customer> OnTimedOut = delegate { };
        public event Action<Customer, Item> OnWrongItemGiven = delegate { };

        public void Initialize(Table table, ItemDefinition order, float? waitTimerOverride = null)
        {
            _table = table;
            _order = order;
            _waitTimer = waitTimerOverride ?? _defaultWaitTime;
            _state = CustomerState.AwaitingDelivery; // TODO: Implement AwaitingOrder phase/state
            OnOrderStarted.Invoke(this);
        }

        private void Update()
        {
            if (!IsWaiting)
                return;

            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _waitTimer = 0f;
                _state = CustomerState.None;
                OnTimedOut.Invoke(this);
            }
        }

        #region IInteractable
        public bool CanInteract(in InteractionContext context) => !context.ActiveHand.IsEmpty;

        public void OnInteractionStarted(in InteractionContext context)
        {
            if (!CanInteract(in context)) return;

            var heldItem = context.ActiveHand.Item.Value;

            if (heldItem.Definition.Id != _order.Id)
            {
                _state = CustomerState.None;
                OnWrongItemGiven.Invoke(this, heldItem);
                return;
            }

            context.ActiveHand.Remove();
            _state = CustomerState.None;
            OnServed.Invoke(this);
        }

        public void OnInteractionHeld(in InteractionContext context, float delta) { }
        public void OnInteractionStopped(in InteractionContext context) { }
        #endregion

        public bool CanRecieve(Item item)
        {
            return IsWaiting;
        }

        public bool TryRecieveFromWaiter(Item item)
        {
            if (!IsWaiting)
                return false;

            _state = CustomerState.None;

            if (_order == null || item.Definition.Id != _order.Id)
            {
                OnWrongItemGiven.Invoke(this, item);
                return false;
            }

            OnServed.Invoke(this);
            return true;
        }
    }
}
