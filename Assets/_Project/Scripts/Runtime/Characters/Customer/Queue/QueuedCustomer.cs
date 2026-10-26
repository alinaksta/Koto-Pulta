using Game.Interaction;
using Game.Items;
using Game.Items.Properties;
using Game.Services;
using Game.UI;
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
        Exiting,
        /// <summary>Sitting at its own table without an order (home-table flow).</summary>
        AtHome,
        /// <summary>Walking back to its own table without an order (home-table flow).</summary>
        ReturningHome,
        /// <summary>Got up and walks to the end of the line; takes its place there on arrival (home-table flow).</summary>
        GoingToQueue
    }

    /// <summary>
    /// Drives one customer through the cashier-queue loop: walking, order taking and returning to the line.
    /// Added at runtime by <see cref="CustomerQueue"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QueuedCustomer : MonoBehaviour, ICustomerInteractionOverride
    {
        private const float ArrivalDistanceSqr = 0.0025f;
        private const float JoinDistance = 1.5f;
        // A thrown order is caught when it flies (or lands) this close to a waiting, seated customer.
        private const float CatchRadius = 0.75f;
        private const float CatchHeight = 0.6f;
        private static readonly Collider[] s_catchHits = new Collider[16];

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
        private int _orderNoteId = -1;
        private Table _homeTable;
        private Seat _homeSeat;

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
        public Table HomeTable => _homeTable;

        /// <summary>
        /// Gets whether the customer sits at its own table, rested, and may get up to order.
        /// </summary>
        public bool CanLeaveHome => _phase == QueuedCustomerPhase.AtHome && _phaseTimer <= 0f;

        public void Initialize(Customer customer, CustomerQueue queue)
        {
            _customer = customer;
            _queue = queue;
            _customer.SetInteractionOverride(this);
            if (queue.ShowOrderBubbles)
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
            RemoveOrderNote();

            if (_customer != null)
                _customer.SetInteractionOverride(null);
        }

        #region Order board
        private void AddOrderNote(ItemDefinition order)
        {
            RemoveOrderNote();

            if (!ServiceLocator.TryGet(out OrderBoardView board) || board == null)
                return;

            Sprite dish = null;
            if (order != null && order.TryGetProperty<FoodProperty>(out var food))
                dish = food.WorldSprite != null ? food.WorldSprite : food.DialogueSprite;

            // No recipe pictures yet.
            _orderNoteId = board.AddOrder(dish, null, 1f);
            if (_reservedTable != null)
                board.SetLabel(_orderNoteId, _reservedTable.TableNumber.ToString());
        }

        private void UpdateOrderNote()
        {
            if (_orderNoteId < 0 || !ServiceLocator.TryGet(out OrderBoardView board) || board == null)
                return;

            // Full until the customer is seated, then it follows the table timer.
            float patience = _phase == QueuedCustomerPhase.AtTable && _customer.State != CustomerState.None
                ? _customer.NormalizedWaitTimer
                : 1f;
            board.SetPatience(_orderNoteId, patience);
        }

        private void RemoveOrderNote()
        {
            if (_orderNoteId < 0)
                return;

            if (ServiceLocator.TryGet(out OrderBoardView board) && board != null)
                board.RemoveOrder(_orderNoteId);

            _orderNoteId = -1;
        }
        #endregion

        private void Update()
        {
            _customer.SetTimeoutEnabled(!CustomerQueue.PatienceTimersDisabled);

            UpdateMovement();
            UpdatePhase();
            CatchThrownOrder();
            UpdateBubble();
            UpdateOrderNote();
        }

        private void LateUpdate()
        {
            UpdateBodyVisuals();
        }

        #region Body visuals
        private bool ShouldUseStandingVisuals
            => _phase != QueuedCustomerPhase.AtTable && _phase != QueuedCustomerPhase.LeavingTable &&
               _phase != QueuedCustomerPhase.AtHome;

        private void UpdateBodyVisuals()
        {
            if (_bodyAnimator == null || _queue.StandingAnimator == null)
                return;

            bool standing = ShouldUseStandingVisuals;
            if (standing != _standingVisuals)
                SetStandingVisuals(standing);

            if (!standing)
            {
                UpdateSeatedFacing();
                return;
            }

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

            // Standing in line: look at the customer in front, or at the counter when first in line.
            Vector3 forward = _phase == QueuedCustomerPhase.InQueue && _arrivedAtSpot && !_moving && _queueIndex >= 0
                ? _queue.GetFacingDirection(_queueIndex)
                : _lastMoveDirection;
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
                // The side sprite faces left as drawn: flip it when the customer faces right on screen.
                if (_bodyRenderer != null) _bodyRenderer.flipX = angle < 0f;
            }
            else
            {
                _bodyAnimator.SetFloat(AngleHash, 2f);
                if (_bodyRenderer != null) _bodyRenderer.flipX = false;
            }
        }

        /// <summary>
        /// The seated sprite is drawn from the side with the legs to the right. Mirror it so the legs always point
        /// toward the table in front of the seat, from wherever the camera looks.
        /// </summary>
        private void UpdateSeatedFacing()
        {
            Seat seat = _phase == QueuedCustomerPhase.AtHome ? _homeSeat : _reservedSeat ?? _homeSeat;
            if (_bodyRenderer == null || seat == null)
                return;

            if (_cameraTransform == null)
            {
                if (Camera.main == null)
                    return;
                _cameraTransform = Camera.main.transform;
            }

            Vector3 toTable = seat.transform.forward;
            toTable.y = 0f;
            if (toTable.sqrMagnitude < 0.0001f)
                return;

            _bodyRenderer.flipX = Vector3.Dot(toTable, _cameraTransform.right) >= 0f;
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

            // Moving along the line: straight to the spot. A NavMesh path would first push the customer out of the
            // line onto the walkable area (the line itself stands on the edge of it).
            Vector3 offset = position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < DirectMoveDistance * DirectMoveDistance)
                MoveDirect(position, () => _arrivedAtSpot = true);
            else
                MoveTo(position, () => _arrivedAtSpot = true);
        }

        private const float DirectMoveDistance = 3.5f;

        private void MoveDirect(Vector3 destination, Action onArrived)
        {
            _waypoints.Clear();
            _waypoints.Add(destination);
            _waypointIndex = 0;
            _onArrived = onArrived;
            _moving = true;
        }

        public void StartOrder(ItemDefinition order, Table table, Seat seat, float tableWaitTime)
        {
            _pendingOrder = order;
            _reservedTable = table;
            _reservedSeat = seat;
            _tableWaitTime = tableWaitTime;
            // Keeps its queue index: it still holds the cashier spot until it walks away.

            StopMoving();
            _phase = QueuedCustomerPhase.Ordering;
            _phaseTimer = _queue.OrderDuration;
            AddOrderNote(order);
            _customer.PlayOrderingAnimation(_queue.OrderDuration);
        }

        public void LeaveTable(float delay)
        {
            transform.SetParent(null, true);
            _customer.EndVisit();
            _pendingOrder = null;
            RemoveOrderNote();

            _phase = QueuedCustomerPhase.LeavingTable;
            _phaseTimer = delay;
        }

        #region Home table
        /// <summary>
        /// Gives the customer its own table (home-table flow).
        /// </summary>
        public void SetHome(Table table, Seat seat)
        {
            _homeTable = table;
            _homeSeat = seat;
        }

        /// <summary>
        /// Puts the customer on its own seat, without an order.
        /// </summary>
        public void SitAtHome(float rest)
        {
            if (_homeSeat == null)
                return;

            StopMoving();
            transform.position = _homeSeat.CustomerSpawnOrigin;
            transform.rotation = Quaternion.identity;
            transform.SetParent(_homeSeat.transform, true);

            _phase = QueuedCustomerPhase.AtHome;
            _phaseTimer = rest;
            if (_standingVisuals && _bodyAnimator != null)
                SetStandingVisuals(false);
        }

        /// <summary>
        /// Gets up from its own table to go and order.
        /// </summary>
        public void LeaveHome()
        {
            transform.SetParent(null, true);
        }

        /// <summary>
        /// Walks to the end of the line and joins it only on arrival, so the line order is the arrival order
        /// (a customer from a far table that got up earlier doesn't keep a place for itself).
        /// </summary>
        public void GoToQueue()
        {
            transform.SetParent(null, true);
            _queueIndex = -1;
            _arrivedAtSpot = false;
            _phase = QueuedCustomerPhase.GoingToQueue;
            HeadForLineEnd();
        }

        private Vector3 _lineEndTarget;

        private void HeadForLineEnd()
        {
            _lineEndTarget = _queue.GetSpotPosition(_queue.Line.Count);
            MoveTo(_lineEndTarget, () => _queue.JoinLine(this));
        }

        /// <summary>
        /// The visit at its own table is over (served or not): stays seated and rests a while.
        /// </summary>
        public void StayAtHome(float rest)
        {
            if (_customer.State != CustomerState.None || _customer.IsSeated)
                _customer.EndVisit();

            _pendingOrder = null;
            RemoveOrderNote();
            _phase = QueuedCustomerPhase.AtHome;
            _phaseTimer = rest;
        }

        /// <summary>
        /// Goes back to its own table and sits there (e.g. when the shift ends).
        /// </summary>
        public void ReturnHome()
        {
            if (_phase == QueuedCustomerPhase.AtHome)
            {
                _phaseTimer = float.MaxValue;
                return;
            }

            if (_phase == QueuedCustomerPhase.AtTable)
            {
                StayAtHome(float.MaxValue);
                return;
            }

            transform.SetParent(null, true);
            _pendingOrder = null;
            RemoveOrderNote();
            _queueIndex = -1;
            _phase = QueuedCustomerPhase.ReturningHome;
            if (_homeSeat != null)
                MoveTo(_homeSeat.CustomerAskOrigin, () => SitAtHome(float.MaxValue));
        }
        #endregion

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
            RemoveOrderNote();
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

                case QueuedCustomerPhase.AtHome:
                    if (_phaseTimer > 0f)
                        _phaseTimer -= Time.deltaTime;
                    break;

                case QueuedCustomerPhase.GoingToQueue:
                    // Close to the current end of the line: take the place (the line may have moved meanwhile).
                    Vector3 lineEnd = _queue.GetSpotPosition(_queue.Line.Count);
                    Vector3 tail = lineEnd - transform.position;
                    tail.y = 0f;
                    if (tail.sqrMagnitude < JoinDistance * JoinDistance)
                    {
                        StopMoving();
                        _queue.JoinLine(this);
                    }
                    else if ((lineEnd - _lineEndTarget).sqrMagnitude > 0.09f)
                    {
                        // The line moved up (or grew) while walking: head for its current end right away.
                        HeadForLineEnd();
                    }
                    break;
            }
        }

        /// <summary>
        /// A seated customer waiting for its order takes it when it is thrown to it (or lands next to it).
        /// Other items are ignored.
        /// </summary>
        private void CatchThrownOrder()
        {
            if (_phase != QueuedCustomerPhase.AtTable || !_customer.IsWaiting || _customer.Order == null)
                return;

            int count = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * CatchHeight, CatchRadius,
                s_catchHits, ~0, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                PhysicsContainer container = s_catchHits[i].GetComponentInParent<PhysicsContainer>();
                if (container == null || container.IsEmpty || container.Item.Value.Definition.Id != _customer.Order.Id)
                    continue;

                Item? item = container.Remove();
                if (item.HasValue)
                    _customer.TryRecieveFromWaiter(item.Value);
                return;
            }
        }

        private void WalkToTable()
        {
            // Free the cashier spot now; the next customer moves up.
            _queue.LeaveLine(this);
            _queueIndex = -1;
            _arrivedAtSpot = false;

            if (_reservedSeat == null)
            {
                // Reservation was released meanwhile (e.g. shift ended) - go back to the line.
                RemoveOrderNote();
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
                RemoveOrderNote();
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
            // Near the destination the customer goes straight to it, so it always arrives.
            // Customers moving up in the line don't dodge (they would step out of it); only the ones walking around do.
            bool lastLeg = _waypointIndex >= _waypoints.Count - 1;
            if (_phase != QueuedCustomerPhase.InQueue && (!lastLeg || (next - target).sqrMagnitude > 0.25f))
                next += GetSeparation() * Time.deltaTime;
            transform.position = next;

            if ((next - target).sqrMagnitude > ArrivalDistanceSqr)
                return;

            _waypointIndex++;
            if (_waypointIndex >= _waypoints.Count)
                CompleteMove();
        }

        private const float PersonalSpace = 1f;
        private const float SeparationSpeed = 1.5f;
        private static readonly List<QueuedCustomer> s_walkers = new();

        private void OnEnable() => s_walkers.Add(this);

        private void OnDisable() => s_walkers.Remove(this);

        /// <summary>
        /// Walking customers step around the others instead of walking through them.
        /// </summary>
        private Vector3 GetSeparation()
        {
            Vector3 push = Vector3.zero;
            Vector3 position = transform.position;

            foreach (QueuedCustomer other in s_walkers)
            {
                if (other == this || other == null)
                    continue;

                Vector3 away = position - other.transform.position;
                away.y = 0f;
                float distance = away.magnitude;
                if (distance >= PersonalSpace)
                    continue;

                // Exactly on top of each other: step to the side of the walking direction.
                Vector3 direction = distance > 0.001f ? away / distance : Vector3.Cross(Vector3.up, _lastMoveDirection).normalized;
                push += direction * (PersonalSpace - distance) / PersonalSpace;
            }

            return push * SeparationSpeed;
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
