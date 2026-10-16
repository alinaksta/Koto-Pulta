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
    /// Optional handler that can take over interaction with a customer (e.g. order taking at the cashier).
    /// </summary>
    public interface ICustomerInteractionOverride
    {
        /// <summary>
        /// Returns <see langword="true"/> when the override wants to handle this interaction.
        /// </summary>
        bool CanHandleInteraction(Customer customer, in InteractionContext context);

        /// <summary>
        /// Handles an interaction previously accepted by <see cref="CanHandleInteraction"/>.
        /// </summary>
        void HandleInteraction(Customer customer, in InteractionContext context);
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
        private bool _waiterHasArrived;
        private float _waitTimer;
        private CustomerState _state;
        private bool _timeoutEnabled = true;
        private bool _directDeliveryEnabled = true;
        private bool _selfTickPatience;
        private bool _persistent;
        private ICustomerInteractionOverride _interactionOverride;

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

        /// <summary>
        /// Gets whether this customer can currently run out of patience.
        /// </summary>
        public bool TimeoutEnabled => _timeoutEnabled;

        /// <summary>
        /// Gets whether the player may deliver directly instead of using a waiter.
        /// </summary>
        public bool DirectDeliveryEnabled => _directDeliveryEnabled;

        /// <summary>
        /// Gets the duration of the customer spawn animation.
        /// </summary>
        public float SpawnDuration => _spawnDuration;

        /// <summary>
        /// Gets the duration of the customer despawn animation.
        /// </summary>
        public float DespawnDuration => _despawnDuration;

        /// <summary>
        /// Gets the initial amount of time the customer will wait.
        /// </summary>
        public float InitialWaitTime => _initialWaitTime;

        /// <summary>
        /// Gets whether this customer stays alive after a visit ends (queue/recycle flow) instead of despawning.
        /// </summary>
        public bool IsPersistent => _persistent;

        /// <summary>
        /// Gets whether the customer is currently seated at a table.
        /// </summary>
        public bool IsSeated => _seat != null;

        public float GetCurrentWaitTimer() => _waitTimer;

        public void SetWaitTimer(float value)
        {
            _waitTimer = Mathf.Clamp(value, 0f, _initialWaitTime);
        }

        /// <summary>
        /// Enables or disables patience timeout for this customer.
        /// </summary>
        public void SetTimeoutEnabled(bool enabled)
        {
            _timeoutEnabled = enabled;
        }

        /// <summary>
        /// Enables or disables direct player delivery for this customer.
        /// </summary>
        public void SetDirectDeliveryEnabled(bool enabled)
        {
            _directDeliveryEnabled = enabled;
        }

        /// <summary>
        /// Gets the current wait timer normalized to the initial wait time.
        /// </summary>
        public float NormalizedWaitTimer => _initialWaitTime > 0f ? Mathf.Clamp01(_waitTimer / _initialWaitTime) : 0f;

        /// <summary>
        /// Makes the customer tick its own table patience timer (used when no waiter drives it).
        /// </summary>
        public void SetSelfTickPatience(bool enabled)
        {
            _selfTickPatience = enabled;
        }

        /// <summary>
        /// Marks the customer as persistent, so services recycle it instead of despawning it.
        /// </summary>
        public void SetPersistent(bool persistent)
        {
            _persistent = persistent;
        }

        /// <summary>
        /// Installs a handler that may take over interaction before the default delivery logic.
        /// </summary>
        public void SetInteractionOverride(ICustomerInteractionOverride interactionOverride)
        {
            _interactionOverride = interactionOverride;
        }

        /// <summary>
        /// Sets the patience value shown by visuals while the customer has no active order (e.g. in the queue).
        /// </summary>
        public void SetIdlePatience(float seconds)
        {
            if (_state == CustomerState.None)
                _waitTimer = Mathf.Max(0f, seconds);
        }

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
        /// Raised when a visit (table + order) of a persistent customer ends.
        /// </summary>
        public event Action<Customer> OnVisitEnded = delegate { };

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
            _waiterHasArrived = false;
            OnOrderStarted.Invoke(this);
        }

        /// <summary>
        /// Starts a table visit directly in the awaiting-delivery state (order was already taken at the cashier).
        /// </summary>
        public void BeginVisit(Table table, Seat seat, ItemDefinition order, float waitTime)
        {
            _table = table;
            _seat = seat;
            _order = order;
            _initialWaitTime = Mathf.Max(0.01f, waitTime);
            _waitTimer = _initialWaitTime;
            _waiterHasArrived = true;
            _state = CustomerState.AwaitingDelivery;
            OnOrderStarted.Invoke(this);
        }

        /// <summary>
        /// Clears table, seat and order after a visit so the customer can be reused.
        /// </summary>
        public void EndVisit()
        {
            _table = null;
            _seat = null;
            _order = null;
            _state = CustomerState.None;
            _waiterHasArrived = false;
            OnVisitEnded.Invoke(this);
        }

        /// <summary>
        /// Plays the ordering animation without changing the order state.
        /// </summary>
        public void PlayOrderingAnimation(float duration)
        {
            OnWaiterStartedAsking.Invoke(this, duration);
        }

        /// <summary>
        /// Plays the customer order-taking interaction.
        /// </summary>
        public async void TakeOrder(float duration)
        {
            OnWaiterStartedAsking.Invoke(this, duration);

            await Awaitable.WaitForSecondsAsync(duration);

            _state = CustomerState.AwaitingDelivery;
        }

        private void Update()
        {
            if (_selfTickPatience && _timeoutEnabled && _state == CustomerState.AwaitingDelivery && _waitTimer > 0f)
                _waitTimer = Mathf.Max(0f, _waitTimer - Time.deltaTime);

            if (_timeoutEnabled && _waitTimer <= 0f && _state != CustomerState.None)
                ForceTimeout();
        }

        public void UpdatePatienceTimer(float timeRemaining)
        {
            _waitTimer = timeRemaining;
        }

        public void StartPatienceTimer()
        {
            _waiterHasArrived = true;
        }

        /// <summary>
        /// Forces the customer into the timed out state if it is still waiting.
        /// </summary>
        /// <returns><see langword="true"/> when a timeout was triggered.</returns>
        public bool ForceTimeout()
        {
            if (_state == CustomerState.None || _order == null)
                return false;

            _waitTimer = 0f;
            _state = CustomerState.None;
            OnTimedOut.Invoke(this);
            return true;
        }

        #region IInteractable
        /// <inheritdoc/>
        /// <remarks>
        /// Customers only accept direct interaction when the active hand is holding an item.
        /// </remarks>
        public bool CanInteract(in InteractionContext context)
            => (_interactionOverride != null && _interactionOverride.CanHandleInteraction(this, in context)) ||
               CanReceiveDirectDelivery(in context);

        private bool CanReceiveDirectDelivery(in InteractionContext context)
            => _directDeliveryEnabled && !context.ActiveHand.IsEmpty && _state != CustomerState.None && _order != null;

        /// <inheritdoc/>
        public void OnInteractionStarted(in InteractionContext context)
        {
            if (_interactionOverride != null && _interactionOverride.CanHandleInteraction(this, in context))
            {
                _interactionOverride.HandleInteraction(this, in context);
                return;
            }

            if (!CanReceiveDirectDelivery(in context)) return;

            var heldItem = context.ActiveHand.Item.Value;

            if (heldItem.Definition.Id != _order.Id)
            {
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

            if (_order == null || item.Definition.Id != _order.Id)
            {
                OnWrongItemGiven.Invoke(this, item);
                return false;
            }

            _state = CustomerState.None;
            OnServed.Invoke(this);
            return true;
        }
    }
}
