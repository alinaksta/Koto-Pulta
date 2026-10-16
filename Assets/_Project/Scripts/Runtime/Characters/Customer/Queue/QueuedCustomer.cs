using Game.Interaction;
using Itemworks.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Where a queue-flow customer currently is in its loop.
    /// </summary>
    public enum QueuedCustomerPhase
    {
        InQueue,
        Ordering,
        WalkingToTable,
        AtTable,
        LeavingTable,
        Exiting
    }

    /// <summary>
    /// Drives one customer through the cashier-queue loop: walking, order taking and returning to the line.
    /// Added at runtime by <see cref="CustomerQueue"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QueuedCustomer : MonoBehaviour, ICustomerInteractionOverride
    {
        private const float ArrivalDistanceSqr = 0.0025f;

        private readonly List<Vector3> _waypoints = new();

        private Customer _customer;
        private CustomerQueue _queue;
        private CustomerOrderBubble _bubble;

        private QueuedCustomerPhase _phase;
        private int _queueIndex = -1;
        private bool _arrivedAtSpot;

        private int _waypointIndex;
        private bool _moving;
        private Action _onArrived;

        private float _phaseTimer;
        private ItemDefinition _pendingOrder;
        private Table _reservedTable;
        private Seat _reservedSeat;
        private float _tableWaitTime;

        public Customer Customer => _customer;
        public QueuedCustomerPhase Phase => _phase;
        public int QueueIndex => _queueIndex;
        public bool ArrivedAtSpot => _arrivedAtSpot;
        public Table ReservedTable => _reservedTable;
        public ItemDefinition PendingOrder => _pendingOrder;

        public void Initialize(Customer customer, CustomerQueue queue)
        {
            _customer = customer;
            _queue = queue;
            _customer.SetInteractionOverride(this);
            _bubble = CustomerOrderBubble.Create(transform, queue.BubbleHeight, queue.BubbleMaterial);
        }

        private void OnDestroy()
        {
            if (_customer != null)
                _customer.SetInteractionOverride(null);
        }

        private void Update()
        {
            UpdateMovement();
            UpdatePhase();
            UpdateBubble();
        }

        #region Loop steps (called by CustomerQueue)
        public void EnterLine()
        {
            _phase = QueuedCustomerPhase.InQueue;
            _queueIndex = -1;
            _arrivedAtSpot = false;
        }

        public void GoToSpot(int index, Vector3 position)
        {
            _queueIndex = index;
            _arrivedAtSpot = false;
            MoveTo(position, () => _arrivedAtSpot = true);
        }

        public void StartOrder(ItemDefinition order, Table table, Seat seat, float tableWaitTime)
        {
            _pendingOrder = order;
            _reservedTable = table;
            _reservedSeat = seat;
            _tableWaitTime = tableWaitTime;
            _queueIndex = -1;
            _arrivedAtSpot = false;

            StopMoving();
            _phase = QueuedCustomerPhase.Ordering;
            _phaseTimer = _queue.OrderDuration;
            _customer.PlayOrderingAnimation(_queue.OrderDuration);
        }

        public void LeaveTable(float delay)
        {
            transform.SetParent(null, true);
            _customer.EndVisit();
            _pendingOrder = null;

            _phase = QueuedCustomerPhase.LeavingTable;
            _phaseTimer = delay;
        }

        public void ClearReservation()
        {
            _reservedTable = null;
            _reservedSeat = null;
        }

        public void Exit(Vector3 exitPosition, Action onExited)
        {
            transform.SetParent(null, true);
            if (_customer.State != CustomerState.None || _customer.IsSeated)
                _customer.EndVisit();

            _pendingOrder = null;
            _phase = QueuedCustomerPhase.Exiting;
            MoveTo(exitPosition, onExited);
        }
        #endregion

        private void UpdatePhase()
        {
            switch (_phase)
            {
                case QueuedCustomerPhase.Ordering:
                    _phaseTimer -= Time.deltaTime;
                    if (_phaseTimer <= 0f)
                        WalkToTable();
                    break;

                case QueuedCustomerPhase.LeavingTable:
                    _phaseTimer -= Time.deltaTime;
                    if (_phaseTimer <= 0f)
                        _queue.JoinLine(this);
                    break;
            }
        }

        private void WalkToTable()
        {
            if (_reservedSeat == null)
            {
                // Reservation was released meanwhile (e.g. shift ended) - go back to the line.
                _queue.JoinLine(this);
                return;
            }

            _phase = QueuedCustomerPhase.WalkingToTable;
            MoveTo(_reservedSeat.CustomerAskOrigin, SitDown);
        }

        private void SitDown()
        {
            if (_reservedSeat == null || _reservedTable == null)
            {
                _queue.JoinLine(this);
                return;
            }

            transform.position = _reservedSeat.CustomerSpawnOrigin;
            transform.rotation = Quaternion.identity;
            transform.SetParent(_reservedSeat.transform, true);

            _phase = QueuedCustomerPhase.AtTable;
            _customer.BeginVisit(_reservedTable, _reservedSeat, _pendingOrder, _tableWaitTime);
        }

        #region Movement
        private void MoveTo(Vector3 destination, Action onArrived)
        {
            _queue.BuildPath(transform.position, destination, _waypoints);
            _waypointIndex = 0;
            _onArrived = onArrived;
            _moving = _waypoints.Count > 0;

            if (!_moving)
                CompleteMove();
        }

        private void StopMoving()
        {
            _moving = false;
            _onArrived = null;
            _waypoints.Clear();
        }

        private void UpdateMovement()
        {
            if (!_moving)
                return;

            Vector3 target = _waypoints[_waypointIndex];
            Vector3 next = Vector3.MoveTowards(transform.position, target, _queue.WalkSpeed * Time.deltaTime);
            transform.position = next;

            if ((next - target).sqrMagnitude > ArrivalDistanceSqr)
                return;

            _waypointIndex++;
            if (_waypointIndex >= _waypoints.Count)
                CompleteMove();
        }

        private void CompleteMove()
        {
            _moving = false;
            var callback = _onArrived;
            _onArrived = null;
            callback?.Invoke();
        }
        #endregion

        private void UpdateBubble()
        {
            if (_bubble == null)
                return;

            switch (_phase)
            {
                case QueuedCustomerPhase.InQueue when _queueIndex == 0 && _arrivedAtSpot:
                    _bubble.Show(null, _queue.QueuePatience > 0f ? _queue.FrontPatience / _queue.QueuePatience : 0f);
                    break;

                case QueuedCustomerPhase.Ordering:
                case QueuedCustomerPhase.WalkingToTable:
                    _bubble.Show(CustomerQueue.GetOrderSprite(_pendingOrder), null);
                    break;

                case QueuedCustomerPhase.AtTable when _customer.State != CustomerState.None:
                    _bubble.Show(CustomerQueue.GetOrderSprite(_customer.Order), _customer.NormalizedWaitTimer);
                    break;

                default:
                    _bubble.Hide();
                    break;
            }
        }

        #region ICustomerInteractionOverride
        /// <inheritdoc/>
        public bool CanHandleInteraction(Customer customer, in InteractionContext context)
            => _queue != null && _queue.CanTakeOrder(this);

        /// <inheritdoc/>
        public void HandleInteraction(Customer customer, in InteractionContext context)
            => _queue.TryTakeOrder(this);
        #endregion
    }
}
