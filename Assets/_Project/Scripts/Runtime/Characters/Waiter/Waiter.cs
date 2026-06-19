using Game.Interaction;
using Game.Items;
using Game.Items.Components;
using Game.Items.Properties;
using Game.Services;
using Itemworks.Core;
using Itemworks.UnityEngine;
using System;
using UnityEngine;
using UnityEngine.AI;


namespace Game.Characters
{
    public enum WaiterServiceState
    {
        Unassigned,
        AskingCustomer,
        TellingMeal,
        AwaitingMeal,
        Delivering
    }

    public enum WaiterLocomotionState
    {
        Idle,
        InHand,
        Ragdoll,
        Recovering,
        Walking
    }

    public enum WaiterLandingResult
    {
        NoItem,
        Delivered,
        Failed
    }

    public class Waiter : MonoBehaviour, IInteractable
    {
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

        [Header("Roaming")]
        [SerializeField] private Transform _wanderOrigin;
        [SerializeField] private float _wanderRadius = 6f;
        [SerializeField] private float _wanderPauseMin = 2f;
        [SerializeField] private float _wanderPauseMax = 4f;
        [SerializeField] private float _wanderArrivalDistance = 0.35f;

        [Header("Service")]
        [SerializeField] private float _askCustomerDuration = 2f;

        private readonly ItemContainer _selfContainer = new();

        private Rigidbody _rigidbody;
        private NavMeshAgent _agent;
        private ItemInstance _itemInstance;
        private WaiterService _waiterService;

        private Customer _assignedCustomer;
        private Table _currentTable;
        private WaiterServiceState _serviceState = WaiterServiceState.Unassigned;
        private WaiterLocomotionState _locomotionState = WaiterLocomotionState.Idle;
        private float _ragdollTimer;
        private float _recoveryTimer;
        private float _idleTimer;
        private float _askCustomerTimer;
        private Vector3 _defaultWanderOrigin;
        private bool _mealPointEntered;

        public IContainer CarryContainer => _carryContainer.Container;
        public Customer AssignedCustomer => _assignedCustomer;
        public Table CurrentTable => _currentTable;
        public WaiterServiceState ServiceState => _serviceState;
        public WaiterLocomotionState LocomotionState => _locomotionState;
        public int? TableNumber => IsAssigned ? AssignedCustomer.Table.TableNumber : null;
        public bool IsAssigned => _assignedCustomer != null;
        public bool CanAcceptAssignment => gameObject.activeInHierarchy && _locomotionState != WaiterLocomotionState.InHand && _locomotionState != WaiterLocomotionState.Ragdoll && _locomotionState != WaiterLocomotionState.Recovering && !IsAssigned;
        public bool IsIdle => _locomotionState == WaiterLocomotionState.Idle;
        public bool AtMealPoint => _waiterService.HasMealPoint && Vector3.Distance(transform.position, _waiterService.MealPointTransform.position) <= GetArrivalDistance();
        public float RecoveryTimer => _recoveryTimer;
        public bool IsRecovering => _locomotionState == WaiterLocomotionState.Recovering;
        public bool IsRagdolled => _locomotionState == WaiterLocomotionState.Ragdoll;

        public event Action<Customer> OnCustomerAssigned = delegate { };
        public event Action OnCustomerUnassigned = delegate { };
        public event Action<Customer> OnCustomerWasAsked = delegate { };

        public event Action OnMealPointEntered = delegate { };
        public event Action OnMealPointExited = delegate { };

        public event Action<WaiterServiceState, WaiterServiceState> OnServiceStateChanged = delegate { };
        public event Action<WaiterLocomotionState, WaiterLocomotionState> OnLocomotionStateChanged = delegate { };

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

            _waiterService = ServiceLocator.Get<WaiterService>();
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
            _waiterService.UnregisterWaiter(this);
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
                if (_askCustomerTimer <= 0f)
                {
                    EnterAwaitingMealState();
                    OnCustomerWasAsked.Invoke(_assignedCustomer);
                }

                return;
            }

            if (_serviceState == WaiterServiceState.AwaitingMeal)
            {
                if (_waiterService.HasMealPoint)
                {
                    if (!AtMealPoint)
                        _waiterService.SendWaiterToMealPoint(this);
                }

                return;
            }

            if (_serviceState == WaiterServiceState.Delivering)
                return;

            if (_serviceState != WaiterServiceState.Unassigned)
                return;

            _idleTimer -= Time.deltaTime;
            if (_idleTimer > 0f)
                return;

            TryStartWander();
        }

        #region Interaction Logic
        public bool CanInteract(in InteractionContext context) => true;

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

        public void OnInteractionHeld(in InteractionContext context, float delta) { }
        public void OnInteractionStopped(in InteractionContext context) { }
        #endregion

        #region Serving Logic
        public void AssignCustomer(Customer customer)
        {
            if (customer == null || _assignedCustomer == customer)
                return;

            _assignedCustomer = customer;
            EnterAskingCustomerState();
            OnCustomerAssigned.Invoke(customer);
        }

        public void ClearCustomer()
        {
            _assignedCustomer = null;
            EnterUnassignedState();
        }

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
        public void DropFromHand(Item selfItem, Vector3 position)
        {
            ReleaseToWorld(selfItem, position, Vector3.zero);
            EnterRagdollState();
        }

        public void ThrowFromHand(Item selfItem, Vector3 position, Vector3 velocity)
        {
            ReleaseToWorld(selfItem, position, velocity);
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
                        _waiterService.SendWaiterToMealPoint(this);
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
                    _waiterService.SendWaiterToMealPoint(this);
                else if (_serviceState == WaiterServiceState.AskingCustomer)
                    NavigateToAssignedCustomer();

                return;
            }

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

        public void NavigateTo(Vector3 destination)
        {
            if (_waiterService.HasMealPoint && Vector3.Distance(destination, _waiterService.MealPointTransform.position) > GetArrivalDistance())
                ExitMealPoint();

            if (!_agent.enabled)
                _agent.enabled = true;

            _idleTimer = 0f;
            _agent.isStopped = false;
            _agent.SetDestination(destination);
            SetLocomotionState(WaiterLocomotionState.Walking);
        }

        public void NavigateToAssignedCustomer()
        {
            if (!IsAssigned)
                return;

            NavigateTo(_assignedCustomer.transform.position);
        }

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
                _waiterService.SendWaiterToMealPoint(this);
        }

        private void EnterDeliveringState()
        {
            if (!IsAssigned)
            {
                EnterUnassignedState();
                return;
            }

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
                return;

            _mealPointEntered = false;
            OnMealPointExited.Invoke();
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
