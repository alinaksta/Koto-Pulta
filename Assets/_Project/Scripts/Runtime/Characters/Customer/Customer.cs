using Game.Interaction;
using Game.Items;
using Itemworks.Core;
using System;
using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Describes the customer's current order state.
    /// </summary>
    public enum CustomerState
    {
        None,
        AwaitingWaiter,
        AwaitingDelivery
    }
    
    /// <summary>
    /// Represents a seated customer waiting for an order.
    /// </summary>
    public class Customer : MonoBehaviour, IInteractable
    {
        [SerializeField] private float _defaultWaitTime = 60f;
        [SerializeField] private float _spawnDuration = 1f;
        [SerializeField] private float _despawnDuration = 1f;

        private Table _table;
        private Seat _seat;
        private ItemDefinition _order;
        private float _initialWaitTime;
        private float _waitTimer;
        private CustomerState _state;

        /// <summary>
        /// Gets the table this customer belongs to.
        /// </summary>
        public Table Table => _table;

        /// <summary>
        /// Gets the seat this customer belongs to.
        /// </summary>
        public Seat Seat => _seat;

        /// <summary>
        /// Gets the item definition currently ordered by the customer.
        /// </summary>
        public ItemDefinition Order => _order;

        /// <summary>
        /// Gets the remaining wait time for the current order.
        /// </summary>
        public float WaitTimer => _waitTimer;

        /// <summary>
        /// Gets the customer's current order/service state.
        /// </summary>
        public CustomerState State => _state;

        /// <summary>
        /// Gets whether the customer is still waiting for a valid delivery.
        /// </summary>
        public bool IsWaiting => _state != CustomerState.None && Order != null && WaitTimer > 0f;

        /// <summary>
        /// Gets whether the customer still needs a waiter to take the order.
        /// </summary>
        public bool NeedsWaiter => _state == CustomerState.AwaitingWaiter && Order != null && WaitTimer > 0f;

        public float SpawnDuration => _spawnDuration;

        public float DespawnDuration => _despawnDuration;

        public float InitialWaitTime => _initialWaitTime;

        public float NormalizedWaitTimer => Mathf.Clamp01(_waitTimer / _initialWaitTime);

        /// <summary>
        /// Raised when a new order starts.
        /// </summary>
        public event Action<Customer> OnOrderStarted = delegate { };

        /// <summary>
        /// Raised when the customer receives the correct item.
        /// </summary>
        public event Action<Customer> OnServed = delegate { };

        /// <summary>
        /// Raised when the customer runs out of waiting time.
        /// </summary>
        public event Action<Customer> OnTimedOut = delegate { };

        /// <summary>
        /// Raised when the customer receives an incorrect item.
        /// </summary>
        public event Action<Customer, Item> OnWrongItemGiven = delegate { };

        public event Action<Customer, float> OnWaiterStartedAsking = delegate { };

        /// <summary>
        /// Sets the table, order, and wait timer for this customer.
        /// </summary>
        /// <param name="table">Table the customer belongs to.</param>
        /// <param name="seat">Seat the customer belongs to.</param>
        /// <param name="order">Requested item definition.</param>
        /// <param name="waitTimerOverride">Optional override for the starting wait time.</param>
        public void Initialize(Table table, Seat seat, ItemDefinition order, float? waitTimerOverride = null)
        {
            _table = table;
            _seat = seat;
            _order = order;
            _initialWaitTime = waitTimerOverride == null ? _defaultWaitTime : waitTimerOverride.Value;
            _waitTimer = _initialWaitTime;
            _state = CustomerState.AwaitingWaiter;
            OnOrderStarted.Invoke(this);
        }

        public async void TakeOrder(float duration)
        {
            OnWaiterStartedAsking.Invoke(this, duration);

            await Awaitable.WaitForSecondsAsync(duration);

            _state = CustomerState.AwaitingDelivery;
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
        /// <inheritdoc/>
        /// <remarks>
        /// Customers only accept direct interaction when the active hand is holding an item.
        /// </remarks>
        public bool CanInteract(in InteractionContext context) => !context.ActiveHand.IsEmpty;

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void OnInteractionHeld(in InteractionContext context, float delta) { }

        /// <inheritdoc/>
        public void OnInteractionStopped(in InteractionContext context) { }
        #endregion

        /// <summary>
        /// Checks whether the customer is currently able to receive an item.
        /// </summary>
        /// <param name="item">Item being offered.</param>
        /// <returns><see langword="true"/> while the customer is still waiting.</returns>
        public bool CanRecieve(Item item)
        {
            return IsWaiting;
        }

        /// <summary>
        /// Tries to receive an item from a waiter and resolves the order.
        /// </summary>
        /// <param name="item">Item being delivered.</param>
        /// <returns><see langword="true"/> when the item satisfies the order.</returns>
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
