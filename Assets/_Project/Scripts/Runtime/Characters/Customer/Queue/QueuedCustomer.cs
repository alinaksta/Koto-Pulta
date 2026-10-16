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

        private static readonly int AngleHash = Animator.StringToHash("angle");
        private static readonly int IsMovingHash = Animator.StringToHash("isMoving");

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

        // Body visuals: seated customers use their own animator, standing/walking ones use the waiter animator.
        private Animator _bodyAnimator;
        private SpriteRenderer _bodyRenderer;
        private RuntimeAnimatorController _seatedController;
        private Vector3 _seatedBodyLocalPosition;
        private bool _standingVisuals;
        private Vector3 _lastMoveDirection = Vector3.back;
        private Transform _cameraTransform;

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

            _bodyAnimator = customer.GetComponentInChildren<Animator>(true);
            if (_bodyAnimator != null)
            {
                _bodyRenderer = _bodyAnimator.GetComponent<SpriteRenderer>();
                _seatedController = _bodyAnimator.runtimeAnimatorController;
                _seatedBodyLocalPosition = _bodyAnimator.transform.localPosition;
            }
        }

        private void OnDestroy()
        {
            if (_customer != null)
                _customer.SetInteractionOverride(null);
        }

        private void Update()
        {
            _customer.SetTimeoutEnabled(!CustomerQueue.PatienceTimersDisabled);

            UpdateMovement();
            UpdatePhase();
            UpdateBubble();
        }

        private void LateUpdate()
        {
            UpdateBodyVisuals();
        }

        #region Body visuals
        private bool ShouldUseStandingVisuals
            => _phase != QueuedCustomerPhase.AtTable && _phase != QueuedCustomerPhase.LeavingTable;

        private void UpdateBodyVisuals()
        {
            if (_bodyAnimator == null || _queue.StandingAnimator == null)
                return;

            bool standing = ShouldUseStandingVisuals;
            if (standing != _standingVisuals)
                SetStandingVisuals(standing);

            if (!standing)
                return;

            _bodyAnimator.SetBool(IsMovingHash, _moving);
            UpdateFacing();
            AlignFeetToGround();
        }

        private void SetStandingVisuals(bool standing)
        {
            _standingVisuals = standing;
            _bodyAnimator.runtimeAnimatorController = standing ? _queue.StandingAnimator : _seatedController;

            if (!standing)
            {
                _bodyAnimator.transform.localPosition = _seatedBodyLocalPosition;
                _bodyAnimator.Rebind();
                _bodyAnimator.Update(0f);
            }
        }

        private void UpdateFacing()
        {
            if (_cameraTransform == null)
            {
                if (Camera.main == null)
                    return;
                _cameraTransform = Camera.main.transform;
            }

            Vector3 forward = _lastMoveDirection;
            Vector3 toCamera = _cameraTransform.position - transform.position;
            forward.y = 0f;
            toCamera.y = 0f;

            float angle = Vector3.SignedAngle(forward, toCamera, Vector3.up);
            float absolute = Mathf.Abs(angle);

            // Same thresholds/values as the waiter's AngleAnimator: 0 = front, 1 = side, 2 = back.
            if (absolute < 45f)
            {
                _bodyAnimator.SetFloat(AngleHash, 0f);
                if (_bodyRenderer != null) _bodyRenderer.flipX = false;
            }
            else if (absolute < 135f)
            {
                _bodyAnimator.SetFloat(AngleHash, 1f);
                if (_bodyRenderer != null) _bodyRenderer.flipX = angle >= 0f;
            }
            else
            {
                _bodyAnimator.SetFloat(AngleHash, 2f);
                if (_bodyRenderer != null) _bodyRenderer.flipX = false;
            }
        }

        private void AlignFeetToGround()
        {
            if (_bodyRenderer == null || _bodyRenderer.sprite == null)
                return;

            // Keep the bottom of the waiter sprite on the floor regardless of its pivot.
            Transform body = _bodyAnimator.transform;
            float bottom = _bodyRenderer.sprite.bounds.min.y * body.localScale.y;
            body.localPosition = new Vector3(_seatedBodyLocalPosition.x, -bottom, _seatedBodyLocalPosition.z);
        }
        #endregion

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

            _phase = QueuedCustomerPhase.AtTable;
            if (_standingVisuals && _bodyAnimator != null)
                SetStandingVisuals(false);

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
            Vector3 step = target - transform.position;
            step.y = 0f;
            if (step.sqrMagnitude > 0.0001f)
                _lastMoveDirection = step.normalized;

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
