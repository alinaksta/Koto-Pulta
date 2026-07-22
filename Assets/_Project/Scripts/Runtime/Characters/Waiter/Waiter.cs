using Game.Interaction;
using Game.Items;
using Game.Items.Components;
using Game.Items.Properties;
using Game.Progression;
using Game.Services;
using Game.Utils;
using Itemworks.Core;
using Itemworks.UnityEngine;
using System;
using UnityEngine;
using UnityEngine.AI;


namespace Game.Characters
{
    /// <summary>
    /// Describes the waiter's current service task.
    /// </summary>
    public enum WaiterServiceState
    {
        Unassigned,
        AskingCustomer,
        TellingMeal,
        AwaitingMeal,
        Delivering
    }

    /// <summary>
    /// Describes the waiter's current locomotion mode.
    /// </summary>
    public enum WaiterLocomotionState
    {
        Idle,
        InHand,
        Ragdoll,
        Recovering,
        Walking
    }

    /// <summary>
    /// Describes the result of recovering after being thrown or dropped.
    /// </summary>
    public enum WaiterLandingResult
    {
        NoItem,
        Delivered,
        Failed
    }

    /// <summary>
    /// Represents a waiter that can be picked up, assigned, and deliver meals.
    /// </summary>
    public class Waiter : MonoBehaviour, IInteractable
    {
        private static bool _sameLayerCollisionIgnored;

        [Header("Item Identification")]
        [SerializeField] private ItemDefinitionAsset _waiterDefinition;

        [Header("Dependences")]
        [SerializeField] private WaiterContainer _carryContainer;
        [SerializeField] private Transform _worldVisualRoot;

        [Header("Recovery")]
        [SerializeField] private float _recoverDelay = 2f;
        [SerializeField] private float _minimumRagdollTime = 0.2f;
        [SerializeField] private float _settledVelocity = 0.2f;
        [SerializeField] private float _navMeshSampleRadius = 2f;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask _groundLayer = 1;
        [SerializeField] private float _groundCheckDistance = 0.2f;
        [SerializeField] private float _groundCheckRadius = 0.2f;
        [SerializeField] private float _groundCheckOffset = 0.35f;

        [Header("Roaming")]
        [SerializeField] private Transform _wanderOrigin;
        [SerializeField] private float _wanderRadius = 6f;
        [SerializeField] private float _wanderPauseMin = 2f;
        [SerializeField] private float _wanderPauseMax = 4f;
        [SerializeField] private float _wanderArrivalDistance = 0.35f;

        [Header("Service")]
        [SerializeField] private float _askCustomerDuration = 2f;
        [SerializeField] private float _maxPatienceTime = 60f;
        private float _patienceTimer;
        

        private readonly ItemContainer _selfContainer = new();

        private Rigidbody _rigidbody;
        private NavMeshAgent _agent;
        private ItemInstance _itemInstance;
        private WaiterService _waiterService;
        private WaiterQueueService _waiterQueueService;
        private UpgradeService _upgradeService;
        private float _baseAgentSpeed;

        private Customer _assignedCustomer;
        private Table _currentTable;
        private WaiterMealPoint _mealPoint;
        private WaiterServiceState _serviceState = WaiterServiceState.Unassigned;
        private WaiterLocomotionState _locomotionState = WaiterLocomotionState.Idle;
        private float _ragdollTimer;
        private float _recoveryTimer;
        private float _idleTimer;
        private float _askCustomerTimer;
        private Vector3 _defaultWanderOrigin;
        private bool _mealPointEntered;
        private bool _holdAtMealPoint;

        public float PatienceTimer => _patienceTimer;
        public float MaxPatienceTime => _maxPatienceTime;
        public float NormalizedPatience => Mathf.Clamp01(_patienceTimer / _maxPatienceTime);
        public float WaitTimer => _patienceTimer;

        /// <summary>
        /// Gets the container holding the waiter's carried item.
        /// </summary>
        public IContainer CarryContainer => _carryContainer.Container;

        /// <summary>
        /// Gets the customer currently assigned to this waiter.
        /// </summary>
        public Customer AssignedCustomer => _assignedCustomer;

        /// <summary>
        /// Gets the table zone the waiter is currently inside, when any.
        /// </summary>
        public Table CurrentTable => _currentTable;

        /// <summary>
        /// Gets the waiter's current service state.
        /// </summary>
        public WaiterServiceState ServiceState => _serviceState;

        /// <summary>
        /// Gets the waiter's current locomotion state.
        /// </summary>
        public WaiterLocomotionState LocomotionState => _locomotionState;

        /// <summary>
        /// Gets the assigned table number, when the waiter has a customer.
        /// </summary>
        public int? TableNumber => IsAssigned ? AssignedCustomer.Table.TableNumber : null;

        /// <summary>
        /// Gets whether the waiter currently has an assigned customer.
        /// </summary>
        public bool IsAssigned => _assignedCustomer != null;

        /// <summary>
        /// Gets whether this waiter is currently eligible for a new assignment.
        /// </summary>
        public bool CanAcceptAssignment => gameObject.activeInHierarchy && _locomotionState != WaiterLocomotionState.InHand && _locomotionState != WaiterLocomotionState.Ragdoll && !IsAssigned;

        /// <summary>
        /// Gets whether the waiter is currently idle.
        /// </summary>
        public bool IsIdle => _locomotionState == WaiterLocomotionState.Idle;

        /// <summary>
        /// Gets whether the waiter is currently at the active meal point.
        /// </summary>
        public bool AtMealPoint => HasMealPoint && (_mealPointEntered || Vector3.Distance(transform.position, _mealPoint.Position) <= GetArrivalDistance());

        /// <summary>
        /// Gets whether the waiter currently has an assigned meal point.
        /// </summary>
        public bool HasMealPoint => _mealPoint != null;

        /// <summary>
        /// Gets whether the waiter is being held at its meal point instead of wandering.
        /// </summary>
        public bool HoldAtMealPoint => _holdAtMealPoint;

        /// <summary>
        /// Gets the remaining recovery time after ragdolling.
        /// </summary>
        public float RecoveryTimer => _recoveryTimer;

        /// <summary>
        /// Gets whether the waiter is in its recovery state.
        /// </summary>
        public bool IsRecovering => _locomotionState == WaiterLocomotionState.Recovering;

        /// <summary>
        /// Gets whether the waiter is currently ragdolled.
        /// </summary>
        public bool IsRagdolled => _locomotionState == WaiterLocomotionState.Ragdoll;

        /// <summary>
        /// Gets whether the waiter is currently asking a customer for an order.
        /// </summary>
        public bool IsAkingCustomer => _locomotionState == WaiterLocomotionState.Idle && _serviceState == WaiterServiceState.AskingCustomer && _askCustomerTimer > 0f;

        /// <summary>
        /// Gets the waiter's current velocity.
        /// </summary>
        /// <remarks>
        /// Returns <see cref="Rigidbody.linearVelocity"/> while the waiter is ragdolled or recovering.
        /// Returns <see cref="NavMeshAgent.velocity"/> while the waiter is using NavMesh locomotion.
        /// </remarks>
        public Vector3 Velocity
        {
            get
            {
                if (IsRecovering || IsRagdolled)
                    return _rigidbody.linearVelocity;
                else
                    return _agent.velocity;
            }
        }

        /// <summary>
        /// Gets whether the waiter is currently grounded.
        /// </summary>
        /// <remarks>
        /// Returns <see langword="true"/> while the waiter is idle or walking because those states use NavMesh locomotion.
        /// Returns <see langword="false"/> while the waiter is in hand.
        /// While ragdolled or recovering, this is determined by a downward sphere cast from <c>transform.position + Vector3.up * _groundCheckOffset</c>
        /// using <c>_groundCheckRadius</c>, <c>_groundCheckDistance</c>, and <c>_groundLayer</c>.
        /// </remarks>
        public bool IsGrounded
            => _locomotionState switch
            {
                WaiterLocomotionState.Idle => true,
                WaiterLocomotionState.Walking => true,
                WaiterLocomotionState.Ragdoll => CastGround(),
                WaiterLocomotionState.Recovering => CastGround(),
                _ => false
            };

        /// <summary>
        /// Raised after a customer is assigned to this waiter.
        /// </summary>
        public event Action<Customer> OnCustomerAssigned = delegate { };

        /// <summary>
        /// Raised after the current customer assignment is cleared.
        /// </summary>
        public event Action OnCustomerUnassigned = delegate { };

        /// <summary>
        /// Raised after the waiter finishes asking its assigned customer.
        /// </summary>
        public event Action<Customer> OnCustomerWasAsked = delegate { };

        /// <summary>
        /// Raised when the waiter reaches the meal point.
        /// </summary>
        public event Action OnMealPointEntered = delegate { };

        /// <summary>
        /// Raised when the waiter leaves the meal point.
        /// </summary>
        public event Action OnMealPointExited = delegate { };

        /// <summary>
        /// Raised after the service state changes.
        /// </summary>
        public event Action<WaiterServiceState, WaiterServiceState> OnServiceStateChanged = delegate { };

        /// <summary>
        /// Raised after the locomotion state changes.
        /// </summary>
        public event Action<WaiterLocomotionState, WaiterLocomotionState> OnLocomotionStateChanged = delegate { };

        /// <summary>
        /// Raised after the waiter recovers and resolves a landing attempt.
        /// </summary>
        public event Action<WaiterLandingResult> OnLandingResolved = delegate { };

        private void Awake()
        {
            if (_carryContainer == null)
                throw new MissingReferenceException($"{nameof(Waiter)} on {name} requires a {nameof(WaiterContainer)} reference.");

            if (_worldVisualRoot == null)
                throw new MissingReferenceException($"{nameof(Waiter)} on {name} requires a {nameof(_worldVisualRoot)} reference.");

            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
                throw new MissingComponentException($"{nameof(Waiter)} on {name} requires a {nameof(Rigidbody)}.");

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
                throw new MissingComponentException($"{nameof(Waiter)} on {name} requires a {nameof(NavMeshAgent)}.");

            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

            if (!_sameLayerCollisionIgnored)
            {
                _sameLayerCollisionIgnored = true;
                Physics.IgnoreLayerCollision(gameObject.layer, gameObject.layer, true);
            }

            _waiterService = ServiceLocator.Get<WaiterService>();
            _waiterQueueService = ServiceLocator.Get<WaiterQueueService>();
            _upgradeService = ServiceLocator.Get<UpgradeService>();
            _baseAgentSpeed = _agent.speed;
            ApplySpeedUpgrade();
            _upgradeService.OnUpgradeChanged += HandleUpgradeChanged;
            _defaultWanderOrigin = transform.position;

            CreateItemInstance();

            _rigidbody.isKinematic = true;
            CarryContainer.OnItemChanged += OnCarryItemChanged;
        }

        private void Start()
        {
            _waiterService.RegisterWaiter(this);
            RefreshServiceState();
        }

        private void OnDestroy()
        {
            CarryContainer.OnItemChanged -= OnCarryItemChanged;
            if (_upgradeService != null)
                _upgradeService.OnUpgradeChanged -= HandleUpgradeChanged;

            if (_waiterQueueService != null)
                ReleaseMealPoint();

            _waiterService.UnregisterWaiter(this);
        }

        private void HandleUpgradeChanged(UpgradeType type, int level)
        {
            if (type == UpgradeType.WaiterSpeed)
                ApplySpeedUpgrade();
        }

        private void ApplySpeedUpgrade()
        {
            if (_agent != null && _upgradeService != null)
                _agent.speed = _baseAgentSpeed * _upgradeService.GetWaiterSpeedMultiplier();
        }

        private void Update()
        {
            switch (_locomotionState)
            {
                case WaiterLocomotionState.Ragdoll:
                    UpdateRagdoll();
                    break;
                case WaiterLocomotionState.Recovering:
                    UpdateRecovering();
                    break;
                case WaiterLocomotionState.Walking:
                    UpdateWalking();
                    break;
                case WaiterLocomotionState.Idle:
                    UpdateIdle();
                    break;
            }

            if (AtMealPoint)
            {
                LookAtServiceCounter();
            }
            if (IsAkingCustomer)
            {
                LookAtCustomer();
            }
        }

        private void CreateItemInstance()
        {
            if (!ItemRegistry.Instance.TryGet(_waiterDefinition.Id, out var definition))
            {
                Debug.LogError("Waiter ItemDefinition is not regiestered!");
                return;
            }

            var instance = new ItemInstance(definition);
            if (!instance.TryGetComponent<WaiterComponent>(out var waiterComponent))
            {
                Debug.LogError($"Waiter ItemDefinition must have {nameof(WaiterProperty)}");
                return;
            }

            _itemInstance = instance;
            waiterComponent.Waiter = this;
            _selfContainer.Insert(new Item(instance));
        }

        private void OnCarryItemChanged(Item? item)
        {
            RefreshServiceState();
        }

        private void UpdateRagdoll()
        {
            _ragdollTimer += Time.deltaTime;
            if (_ragdollTimer < _minimumRagdollTime)
                return;

            float settledVelocitySqr = _settledVelocity * _settledVelocity;
            if (_rigidbody.linearVelocity.sqrMagnitude > settledVelocitySqr)
                return;

            BeginRecovery();
        }

        private void UpdateRecovering()
        {
            _recoveryTimer -= Time.deltaTime;
            if (_recoveryTimer > 0f)
                return;

            FinishRecovery();
        }

        private void UpdateWalking()
        {
            if (_agent.pathPending)
                return;

            float arrivalDistance = GetArrivalDistance();
            if (_agent.remainingDistance > arrivalDistance)
                return;

            if (_serviceState == WaiterServiceState.Unassigned)
            {
                if (_holdAtMealPoint && HasMealPoint)
                {
                    EnterMealPoint();
                    EnterIdleState();
                    return;
                }

                StartWanderPause();
                return;
            }

            if (_serviceState == WaiterServiceState.AskingCustomer)
            {
                StartAskCustomerPause();
                return;
            }

            if (_serviceState == WaiterServiceState.AwaitingMeal)
            {
                EnterMealPoint();
                EnterIdleState();
                return;
            }

            EnterIdleState();
        }

        private void UpdateIdle()
        {
            if (_serviceState == WaiterServiceState.AskingCustomer)
            {
                _askCustomerTimer -= Time.deltaTime;
                SyncTimerWithCustomer();
                if (_askCustomerTimer <= 0f)
                {
                    EnterAwaitingMealState();
                    OnCustomerWasAsked.Invoke(_assignedCustomer);
                }
                

                return;
            }

            if (_serviceState == WaiterServiceState.AwaitingMeal)
            {
                if (_assignedCustomer == null || _assignedCustomer.TimeoutEnabled)
                    _patienceTimer -= Time.deltaTime;

                SyncTimerWithCustomer();
                if (_patienceTimer <= 0f)
                {
                    HandlePatienceTimeout();
                    return;
                }

                if (!HasMealPoint || !AtMealPoint)
                {
                    StartGoingToMealPoint();
                }

                return;
            }

            if (_serviceState == WaiterServiceState.Delivering)
            {
                if (_assignedCustomer == null || _assignedCustomer.TimeoutEnabled)
                    _patienceTimer -= Time.deltaTime;

                SyncTimerWithCustomer();
                if (_patienceTimer <= 0f)
                {
                    HandlePatienceTimeout();
                    return;
                }
            }

            if (_serviceState != WaiterServiceState.Unassigned)
                return;

            if (_holdAtMealPoint)
            {
                if (!HasMealPoint || !AtMealPoint)
                    StartGoingToMealPoint();

                return;
            }

            _idleTimer -= Time.deltaTime;
            if (_idleTimer > 0f)
                return;

            TryStartWander();
        }

        private void HandlePatienceTimeout()
        {
            if (_assignedCustomer != null)
            {
                _assignedCustomer.SetWaitTimer(0f);
                _assignedCustomer.ForceTimeout();
                ClearCustomer();
                ClearCarriedItem();
                if (gameObject.activeInHierarchy && _locomotionState != WaiterLocomotionState.InHand && 
                    _locomotionState != WaiterLocomotionState.Ragdoll && _locomotionState != WaiterLocomotionState.Recovering)
                {
                    StartWanderPause();
                }
            }
        }

        public void SyncTimerWithCustomer()
        {
            if (_assignedCustomer != null)
            {
                _assignedCustomer.SetWaitTimer(_patienceTimer);
            }
        }

        private void ResetPatience()
        {
            if (_assignedCustomer != null)
            {
                _maxPatienceTime = _assignedCustomer.InitialWaitTime;
                _patienceTimer = _maxPatienceTime;
            }
        }

        #region Interaction Logic
        /// <inheritdoc/>
        public bool CanInteract(in InteractionContext context) => true;

        /// <inheritdoc/>
        /// <remarks>
        /// An empty hand picks up the waiter itself; a filled hand tries to give its item to the waiter.
        /// </remarks>
        public void OnInteractionStarted(in InteractionContext context)
        {
            if (context.ActiveHand.IsEmpty)
            {
                var result = ItemTransferUtility.TryTransfer(_selfContainer, context.ActiveHand);
                if (result == ItemTransferUtility.TransferResult.Success)
                    EnterHandState();

                return;
            }

            ItemTransferUtility.TryTransfer(context.ActiveHand, _carryContainer.Container);
        }

        /// <inheritdoc/>
        public void OnInteractionHeld(in InteractionContext context, float delta) { }

        /// <inheritdoc/>
        public void OnInteractionStopped(in InteractionContext context) { }
        #endregion

        #region Serving Logic
        /// <summary>
        /// Assigns a customer and begins the service flow.
        /// </summary>
        /// <param name="customer">Customer to serve.</param>
        public void AssignCustomer(Customer customer)
        {
            if (customer == null || _assignedCustomer == customer)
                return;

            _assignedCustomer = customer;
            EnterAskingCustomerState();
            OnCustomerAssigned.Invoke(customer);
        }

        /// <summary>
        /// Clears the current customer assignment.
        /// </summary>
        public void ClearCustomer()
        {
            _assignedCustomer = null;
            EnterUnassignedState();
        }

        /// <summary>
        /// Removes the item currently carried by the waiter, if any.
        /// </summary>
        public void ClearCarriedItem()
        {
            if (CarryContainer.IsEmpty)
                return;

            CarryContainer.Remove();
        }

        private void RefreshServiceState()
        {
            if (!IsAssigned)
            {
                EnterUnassignedState();
                return;
            }

            if (!CarryContainer.IsEmpty)
            {
                EnterDeliveringState();
                return;
            }

            if (_serviceState == WaiterServiceState.Delivering)
            {
                EnterAwaitingMealState();
                return;
            }

            if (_serviceState == WaiterServiceState.Unassigned)
                EnterAskingCustomerState();
        }

        private bool TryDeliverToCurrentTable()
        {
            if (_currentTable == null || CarryContainer.IsEmpty)
                return false;

            var carriedItem = CarryContainer.Item.Value;

            Customer randomWaitingCustomer = null;
            int waitingCount = 0;

            Customer randomMatchingCustomer = null;
            int matchingCount = 0;

            var customers = _currentTable.Customers;
            for (int i = 0; i < customers.Count; i++)
            {
                var customer = customers[i];
                if (!customer.IsWaiting)
                    continue;

                waitingCount++;
                if (UnityEngine.Random.Range(0, waitingCount) == 0)
                    randomWaitingCustomer = customer;

                if (customer.Order != null && customer.Order.Id == carriedItem.Definition.Id)
                {
                    matchingCount++;
                    if (UnityEngine.Random.Range(0, matchingCount) == 0)
                        randomMatchingCustomer = customer;
                }
            }

            var targetCustomer = randomMatchingCustomer != null ? randomMatchingCustomer : randomWaitingCustomer;
            if (targetCustomer == null)
                return false;

            bool delivered = targetCustomer.TryRecieveFromWaiter(carriedItem);

            if (delivered)
            {
                CarryContainer.Remove();

                return true;
            }

            ClearCarriedItem();
            return false;
        }
        #endregion

        #region Item to World Logic
        /// <summary>
        /// Drops the waiter into the world without added velocity.
        /// </summary>
        /// <param name="selfItem">Item representation of this waiter.</param>
        /// <param name="position">Drop position.</param>
        public void DropFromHand(Item selfItem, Vector3 position)
        {
            ReleaseToWorld(selfItem, position, Vector3.zero);
            EnterRagdollState();
        }

        /// <summary>
        /// Throws the waiter into the world with velocity.
        /// </summary>
        /// <param name="selfItem">Item representation of this waiter.</param>
        /// <param name="position">Spawn position.</param>
        /// <param name="velocity">Initial throw velocity.</param>
        public void ThrowFromHand(Item selfItem, Vector3 position, Vector3 velocity)
        {
            ReleaseToWorld(selfItem, position, velocity);
            SetHoldAtMealPoint(false);
            EnterRagdollState();
        }

        private void ReleaseToWorld(Item selfItem, Vector3 position, Vector3 velocity)
        {
            Debug.Log($"Tried releasing to world at {position}");

            _selfContainer.Insert(selfItem);

            gameObject.SetActive(true);
            _agent.enabled = false;
            transform.position = position;
            _rigidbody.position = position;

            _worldVisualRoot.localPosition = Vector3.zero;


            _rigidbody.isKinematic = false;
            transform.position = position;
            _rigidbody.position = position;
            _rigidbody.linearVelocity = velocity;
        }

        private void EnterHandState()
        {
            SetHoldAtMealPoint(false);
            ExitMealPoint();
            _currentTable = null;
            _idleTimer = 0f;
            _askCustomerTimer = 0f;
            _agent.enabled = false;

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;

            SetLocomotionState(WaiterLocomotionState.InHand);
            gameObject.SetActive(false);
        }
        #endregion

        #region Locomotion
        private void EnterRagdollState()
        {
            ExitMealPoint();
            _currentTable = null;
            _idleTimer = 0f;
            _askCustomerTimer = 0f;
            _ragdollTimer = 0f;
            _recoveryTimer = 0f;
            SetLocomotionState(WaiterLocomotionState.Ragdoll);
        }

        private void BeginRecovery()
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;

            _recoveryTimer = _recoverDelay;
            SetLocomotionState(WaiterLocomotionState.Recovering);
        }

        private void FinishRecovery()
        {
            _recoveryTimer = 0f;

            var eulerAngles = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, eulerAngles.y, 0f);

            if (NavMesh.SamplePosition(transform.position, out var hit, _navMeshSampleRadius, NavMesh.AllAreas))
            {
                _agent.enabled = true;
                _agent.Warp(hit.position);
            }
            else
            {
                _agent.enabled = true;
            }

            WaiterLandingResult landingResult = ResolveLanding();
            OnLandingResolved.Invoke(landingResult);

            if (_locomotionState == WaiterLocomotionState.Walking)
                return;

            if (landingResult == WaiterLandingResult.Failed || landingResult == WaiterLandingResult.NoItem)
            {
                switch (_serviceState)
                {
                    case WaiterServiceState.AskingCustomer:
                        NavigateToAssignedCustomer();
                        return;
                    case WaiterServiceState.AwaitingMeal:
                        StartGoingToMealPoint();
                        return;
                    case WaiterServiceState.Delivering:
                        EnterIdleState();
                        return;
                }
            }

            if (IsAssigned)
            {
                if (_serviceState == WaiterServiceState.Delivering)
                    EnterIdleState();
                else if (_serviceState == WaiterServiceState.AwaitingMeal)
                    StartGoingToMealPoint();
                else if (_serviceState == WaiterServiceState.AskingCustomer)
                    NavigateToAssignedCustomer();

                return;
            }

            if (_waiterService.TryAssignNextWaitingCustomerToWaiter(this))
                return;

            StartWanderPause();
        }

        private WaiterLandingResult ResolveLanding()
        {
            if (CarryContainer.IsEmpty)
                return WaiterLandingResult.NoItem;

            if (_assignedCustomer == null || _currentTable != _assignedCustomer.Table)
            {
                ClearCarriedItem();
                return WaiterLandingResult.Failed;
            }

            if (TryDeliverToCurrentTable())
                return WaiterLandingResult.Delivered;

            ClearCarriedItem();
            return WaiterLandingResult.Failed;
        }

        /// <summary>
        /// Starts navigating toward a world position.
        /// </summary>
        /// <param name="destination">Destination to move toward.</param>
        public void NavigateTo(Vector3 destination)
        {
            if (HasMealPoint && Vector3.Distance(destination, _mealPoint.Position) > GetArrivalDistance())
                ExitMealPoint();

            if (!_agent.enabled)
                _agent.enabled = true;

            _idleTimer = 0f;
            _agent.isStopped = false;
            _agent.SetDestination(destination);
            SetLocomotionState(WaiterLocomotionState.Walking);
        }

        /// <summary>
        /// Starts navigating toward the currently assigned customer.
        /// </summary>
        public void NavigateToAssignedCustomer()
        {
            if (!IsAssigned)
                return;

            NavigateTo(_assignedCustomer.Seat.CustomerAskOrigin);
        }

        /// <summary>
        /// Keeps the waiter at its meal point instead of starting its unassigned wander loop.
        /// </summary>
        public void SetHoldAtMealPoint(bool hold)
        {
            _holdAtMealPoint = hold;

            if (!hold || !CanReactToServiceState())
                return;

            if (!AtMealPoint)
                StartGoingToMealPoint();
        }

        /// <summary>
        /// Stops pathing and switches the waiter to its idle locomotion state.
        /// </summary>
        public void EnterIdleState()
        {
            if (_agent.enabled && gameObject.activeInHierarchy)
                _agent.isStopped = true;

            SetLocomotionState(WaiterLocomotionState.Idle);
        }

        private void StartWanderPause()
        {
            _idleTimer = UnityEngine.Random.Range(_wanderPauseMin, _wanderPauseMax);
            EnterIdleState();
        }

        private void StartAskCustomerPause()
        {
            _askCustomerTimer = _askCustomerDuration;

            if (_assignedCustomer != null)
            {
                _assignedCustomer.StartPatienceTimer();
                ResetPatience();
                SyncTimerWithCustomer();
            }
            _assignedCustomer.TakeOrder(_askCustomerDuration);
            EnterIdleState();
        }

        private void EnterUnassignedState()
        {
            ExitMealPoint();
            SetServiceState(WaiterServiceState.Unassigned);
            _askCustomerTimer = 0f;
            OnCustomerUnassigned.Invoke();

            if (gameObject.activeInHierarchy && _locomotionState != WaiterLocomotionState.InHand && _locomotionState != WaiterLocomotionState.Ragdoll && _locomotionState != WaiterLocomotionState.Recovering)
                StartWanderPause();
        }

        private void EnterAskingCustomerState()
        {
            if (!IsAssigned)
            {
                EnterUnassignedState();
                return;
            }

            ExitMealPoint();
            SetServiceState(WaiterServiceState.AskingCustomer);
            _askCustomerTimer = 0f;
            ResetPatience();

            if (CanReactToServiceState())
                NavigateToAssignedCustomer();
        }

        private void EnterAwaitingMealState()
        {
            if (!IsAssigned)
            {
                EnterUnassignedState();
                return;
            }

            SetServiceState(WaiterServiceState.AwaitingMeal);
            _askCustomerTimer = 0f;

            if (CanReactToServiceState())
                StartGoingToMealPoint();
        }

        /// <summary>
        /// Sends the waiter toward its assigned meal point.
        /// </summary>
        public void StartGoingToMealPoint()
        {
            if (!gameObject.activeInHierarchy)
            {
                EnterIdleState();
                return;
            }

            if (!TryReserveMealPoint())
            {
                EnterIdleState();
                return;
            }

            NavigateTo(_mealPoint.Position);
        }

        private bool TryReserveMealPoint()
        {
            if (_waiterQueueService == null)
                return false;

            if (!_waiterQueueService.TryReserveMealPoint(this, out var mealPoint))
            {
                _mealPoint = null;
                return false;
            }

            _mealPoint = mealPoint;
            return true;
        }

        private void ReleaseMealPoint()
        {
            if (_waiterQueueService != null)
                _waiterQueueService.ReleaseMealPoint(this);

            _mealPoint = null;
        }

        private void EnterDeliveringState()
        {
            if (!IsAssigned)
            {
                EnterUnassignedState();
                return;
            }

            if (!AtMealPoint)
                ExitMealPoint();

            SetServiceState(WaiterServiceState.Delivering);
            _askCustomerTimer = 0f;

            if (CanReactToServiceState())
                EnterIdleState();
        }

        private bool CanReactToServiceState()
            => gameObject.activeInHierarchy && _locomotionState != WaiterLocomotionState.InHand && _locomotionState != WaiterLocomotionState.Ragdoll && _locomotionState != WaiterLocomotionState.Recovering;

        private float GetArrivalDistance()
            => Mathf.Max(_agent.stoppingDistance, _wanderArrivalDistance);

        private bool CastGround()
        {
            const float padding = 0.02f;

            Vector3 origin = transform.position + Vector3.up * Mathf.Max(_groundCheckOffset, _groundCheckRadius + padding);

            return Physics.SphereCast(
                origin,
                Mathf.Max(0.01f, _groundCheckRadius),
                Vector3.down,
                out _,
                Mathf.Max(0f, _groundCheckDistance) + padding,
                _groundLayer,
                QueryTriggerInteraction.Ignore);
        }

        private void SetServiceState(WaiterServiceState state)
        {
            if (_serviceState == state)
                return;

            var previousState = _serviceState;
            _serviceState = state;
            OnServiceStateChanged.Invoke(previousState, state);
        }

        private void SetLocomotionState(WaiterLocomotionState state)
        {
            if (_locomotionState == state)
                return;

            var previousState = _locomotionState;
            _locomotionState = state;
            OnLocomotionStateChanged.Invoke(previousState, state);
        }

        private void EnterMealPoint()
        {
            if (_mealPointEntered)
                return;

            _mealPointEntered = true;

            OnMealPointEntered.Invoke();
        }

        private void ExitMealPoint()
        {
            if (!_mealPointEntered)
            {
                ReleaseMealPoint();
                return;
            }

            _mealPointEntered = false;
            OnMealPointExited.Invoke();
            ReleaseMealPoint();
        }

        private void LookAtServiceCounter()
        {
            Vector3 lookDirection = _waiterQueueService.ServiceCounterOrigin - transform.position;
            lookDirection.Normalize();
            lookDirection = lookDirection.Flat();

            transform.rotation = Quaternion.FromToRotation(Vector3.forward, lookDirection);
        }

        private void LookAtCustomer()
        {
            transform.rotation = _assignedCustomer.Seat.CustomerAskRotation;
        }

        private void TryStartWander()
        {
            Vector3 origin = _wanderOrigin != null ? _wanderOrigin.position : _defaultWanderOrigin;

            for (int i = 0; i < 8; i++)
            {
                Vector3 candidate = origin + UnityEngine.Random.insideUnitSphere * _wanderRadius;
                candidate.y = origin.y;

                if (NavMesh.SamplePosition(candidate, out var hit, _wanderRadius, NavMesh.AllAreas))
                {
                    NavigateTo(hit.position);
                    return;
                }
            }

            StartWanderPause();
        }
        #endregion

        #region Table Tracking
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<TableZone>(out var zone))
                _currentTable = zone.Table;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<TableZone>(out var zone) && _currentTable == zone.Table)
                _currentTable = null;
        }
        #endregion
    }
}
