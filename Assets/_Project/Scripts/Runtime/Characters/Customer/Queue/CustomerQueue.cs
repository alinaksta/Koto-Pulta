using Game.Items.Properties;
using Game.Services;
using Itemworks.Core;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Characters
{
    /// <summary>
    /// Cashier queue for the "order at the counter" flow.
    /// </summary>
    /// <remarks>
    /// Customers enter through the entrance, line up, and the customer at the front waits for the player
    /// to take their order. Only the front customer loses patience. After the order is taken the customer
    /// walks to a free table, where the table timer starts. Served or not, the customer then walks back to
    /// the end of the line and repeats the loop.
    /// Place this component at the cashier spot (the front of the line). The line extends toward the entrance.
    /// </remarks>
    public class CustomerQueue : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("Where customers enter and leave the restaurant (e.g. the door).")]
        [SerializeField] private Transform _entrance;
        [Tooltip("Optional explicit queue spots. Index 0 is the cashier. When empty or too short, spots are generated toward the entrance.")]
        [SerializeField] private Transform[] _spots = new Transform[0];
        [SerializeField] private float _spotSpacing = 1.1f;
        [Tooltip("What the first customer in line looks at (the counter / cashier). When empty: against this object's forward.")]
        [SerializeField] private Transform _counter;
        [SerializeField] private float _navMeshSampleRadius = 2f;

        [Header("Population")]
        [Tooltip("Maximum number of customers that cycle through the restaurant at once.")]
        [SerializeField] private int _maxCustomers = 6;

        [Header("Timers")]
        [Tooltip("Patience of the customer at the front of the line, in seconds.")]
        [SerializeField] private float _queuePatience = 30f;
        [Tooltip("Patience at the table after the order was taken, in seconds.")]
        [SerializeField] private float _tableWaitTime = 60f;
        [Tooltip("How long taking an order takes, in seconds.")]
        [SerializeField] private float _orderDuration = 1.2f;
        [Tooltip("Delay before a customer leaves the table and walks back to the end of the line.")]
        [SerializeField] private float _leaveTableDelay = 1.2f;

        [Header("Home tables")]
        [Tooltip("Every customer owns one table: all start seated at their own table, get up one by one " +
                 "(random order, random interval) to order at the cashier and go back to their own table.")]
        [SerializeField] private bool _homeTables;
        [SerializeField, Min(0f)] private float _minDepartureInterval = 0.5f;
        [Tooltip("Longest pause between two customers getting up, in seconds.")]
        [SerializeField, Min(0f)] private float _maxDepartureInterval = 5f;
        [Tooltip("How long a customer stays at its table after a visit before it may get up again.")]
        [SerializeField, Min(0f)] private float _homeRestTime = 6f;
        [Tooltip("At the start of a shift every customer already stands in the line at the counter (random order).")]
        [SerializeField] private bool _startShiftInLine = true;

        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 2.5f;

        [Header("Visuals")]
        [Tooltip("Animator controller used for the customer body while it is not seated (waiter sprites).")]
        [SerializeField] private RuntimeAnimatorController _standingAnimator;
        [Tooltip("Bubble with the meal icon and patience bar above customers' heads (replaced by the order board).")]
        [SerializeField] private bool _showOrderBubbles = false;
        [SerializeField] private float _bubbleHeight = 2.1f;
        [Tooltip("Optional material for the order bubble sprites. Leave empty to use the default sprite material.")]
        [SerializeField] private Material _bubbleMaterial;

        private readonly List<QueuedCustomer> _line = new();
        private readonly List<QueuedCustomer> _all = new();
        private NavMeshPath _path;

        private CustomerService _customerService;
        private QueuedCustomer _frontCustomer;
        private float _frontPatience;
        private int _agentTypeId;
        private bool _homesPopulated;
        private bool _departuresActive;
        private bool _repopulateHomes;
        private Game.Progression.ShiftService _shifts;
        private float _departureTimer;
        private readonly List<QueuedCustomer> _departureCandidates = new();

        /// <summary>
        /// Debug switch: when true, no customer loses patience (queue or table).
        /// </summary>
        public static bool PatienceTimersDisabled { get; set; }

        public RuntimeAnimatorController StandingAnimator => _standingAnimator;
        public float WalkSpeed => _walkSpeed;
        public float QueuePatience => _queuePatience;
        public float FrontPatience => _frontPatience;
        public float OrderDuration => _orderDuration;
        public float LeaveTableDelay => _leaveTableDelay;
        public bool ShowOrderBubbles => _showOrderBubbles;
        public float BubbleHeight => _bubbleHeight;
        public Material BubbleMaterial => _bubbleMaterial;

        /// <summary>
        /// Gets the customers currently standing (or walking to stand) in line. Index 0 is at the cashier.
        /// </summary>
        public IReadOnlyList<QueuedCustomer> Line => _line;

        /// <summary>
        /// Gets the number of customers cycling through this queue.
        /// </summary>
        public int Population => _all.Count;

        /// <summary>
        /// Gets whether customers own a table each (see <see cref="TrySpawnCustomer"/>).
        /// </summary>
        public bool UsesHomeTables => _homeTables;

        /// <summary>
        /// Raised when the player has taken a customer's order at the cashier.
        /// </summary>
        public event System.Action<QueuedCustomer> OnOrderTaken;

        private void Awake()
        {
            _path = new NavMeshPath();
            var surface = FindAnyObjectByType<NavMeshSurface>();
            _agentTypeId = surface != null ? surface.agentTypeID : 0;
        }

        private void OnEnable()
        {
            if (ServiceLocator.TryGet(out _customerService))
                _customerService.RegisterQueue(this);
        }

        private void OnDisable()
        {
            if (_customerService != null)
                _customerService.UnregisterQueue(this);
        }

        private void Start()
        {
            if (_homeTables)
                PopulateHomes();
        }

        private void Update()
        {
            PurgeDestroyed();
            UpdateDepartures();
            AssignSpots();
            UpdateFrontPatience();
        }

        #region Spawning
        /// <summary>
        /// Spawns a new customer at the entrance and sends it to the end of the line.
        /// </summary>
        public bool TrySpawnCustomer()
        {
            PurgeDestroyed();

            if (_homeTables)
            {
                // Customers already sit at their tables; the shift asking for customers just lets them start ordering.
                PopulateHomes();
                if (!_departuresActive)
                    StartDepartures();
                return false;
            }

            if (_customerService == null || _all.Count >= _maxCustomers)
                return false;

            Vector3 position = SampleNavMesh(EntrancePosition);
            Customer customer = _customerService.CreatePersistentCustomer(position);

            var queued = customer.gameObject.AddComponent<QueuedCustomer>();
            queued.Initialize(customer, this);

            _all.Add(queued);
            JoinLine(queued);
            return true;
        }

        /// <summary>
        /// Sends every customer out through the entrance and removes them (e.g. at the end of a shift).
        /// </summary>
        public void DismissAll()
        {
            PurgeDestroyed();

            if (_homeTables)
            {
                // Office workers don't leave: everyone goes back to their own table and stays there.
                _departuresActive = false;
                foreach (QueuedCustomer queued in _all)
                {
                    ReleaseTable(queued);
                    queued.ReturnHome();
                }

                _line.Clear();
                _frontCustomer = null;
                return;
            }

            for (int i = _all.Count - 1; i >= 0; i--)
            {
                QueuedCustomer queued = _all[i];
                ReleaseTable(queued);
                queued.Exit(EntrancePosition, () =>
                {
                    if (_customerService != null && queued != null)
                        _customerService.RemovePersistentCustomer(queued.Customer);
                });
            }

            _all.Clear();
            _line.Clear();
            _frontCustomer = null;
        }

        /// <summary>
        /// Called by <see cref="CustomerService"/> after it destroyed every active customer.
        /// </summary>
        public void HandleCustomersCleared()
        {
            // Home-table customers are put back at their tables right away (next frame).
            _homesPopulated = false;
            _departuresActive = false;
            _repopulateHomes = _homeTables;
            _all.Clear();
            _line.Clear();
            _frontCustomer = null;
        }
        #endregion

        #region Order taking
        /// <summary>
        /// Checks whether the player can take this customer's order right now.
        /// </summary>
        public bool CanTakeOrder(QueuedCustomer queued)
            => queued != null &&
               _line.Count > 0 &&
               _line[0] == queued &&
               queued.Phase == QueuedCustomerPhase.InQueue &&
               queued.ArrivedAtSpot;

        /// <summary>
        /// Takes the front customer's order, reserves a table and sends the customer there.
        /// </summary>
        public bool TryTakeOrder(QueuedCustomer queued)
        {
            if (!CanTakeOrder(queued))
                return false;

            if (!_customerService.TryGetRandomOrder(out ItemDefinition order))
            {
                Debug.LogWarning($"[{nameof(CustomerQueue)}] Can't take an order: no order source installed. Start a shift first.", this);
                return false;
            }

            Table table;
            Seat seat;
            bool reserved = _homeTables && queued.HomeTable != null
                ? _customerService.TryReserveTable(table = queued.HomeTable, queued.Customer, out seat)
                : _customerService.TryReserveRandomFreeTable(queued.Customer, out table, out seat);

            if (!reserved)
            {
                Debug.Log($"[{nameof(CustomerQueue)}] No free table - the customer keeps waiting.", this);
                return false;
            }

            // The customer keeps its spot while ordering; the line moves up only when it walks away (LeaveLine).
            queued.StartOrder(order, table, seat, _tableWaitTime);
            OnOrderTaken?.Invoke(queued);
            return true;
        }
        #endregion

        #region Visit lifecycle
        /// <summary>
        /// Called when a seated customer was served or ran out of patience at the table.
        /// </summary>
        public void HandleVisitFinished(Customer customer)
        {
            if (customer == null || !customer.TryGetComponent(out QueuedCustomer queued) || !_all.Contains(queued))
                return;

            ReleaseTable(queued);

            if (_homeTables && queued.HomeTable != null)
                queued.StayAtHome(_homeRestTime);
            else
                queued.LeaveTable(_leaveTableDelay);
        }

        /// <summary>
        /// Removes the customer from the line (it walks away after ordering); the others move up.
        /// </summary>
        public void LeaveLine(QueuedCustomer queued)
        {
            _line.Remove(queued);
        }

        /// <summary>
        /// Adds the customer to the end of the line.
        /// </summary>
        public void JoinLine(QueuedCustomer queued)
        {
            if (queued == null || _line.Contains(queued))
                return;

            queued.EnterLine();
            _line.Add(queued);
        }

        private void ReleaseTable(QueuedCustomer queued)
        {
            if (queued == null)
                return;

            Table table = queued.ReservedTable;
            queued.ClearReservation();

            if (table != null)
                table.RemoveCustomer(queued.Customer);
        }
        #endregion

        #region Home tables
        /// <summary>
        /// Puts one customer at every registered table, seated.
        /// </summary>
        private void PopulateHomes()
        {
            if (_customerService == null && ServiceLocator.TryGet(out _customerService))
                _customerService.RegisterQueue(this);

            if (_homesPopulated || _customerService == null)
                return;

            // Tables register themselves on Awake; make sure none were missed (e.g. the service wasn't up yet).
            foreach (Table table in FindObjectsByType<Table>(FindObjectsSortMode.None))
            {
                bool registered = false;
                foreach (Table known in _customerService.Tables)
                    registered |= known == table;
                if (!registered)
                    table.Initialize(table.TableNumber, _customerService);
            }

            _homesPopulated = true;
            foreach (Table table in _customerService.Tables)
            {
                if (table == null || table.SeatCount == 0)
                    continue;

                Seat seat = table.Seats[0];
                Customer customer = _customerService.CreatePersistentCustomer(seat.CustomerSpawnOrigin);
                var queued = customer.gameObject.AddComponent<QueuedCustomer>();
                queued.Initialize(customer, this);
                queued.SetHome(table, seat);
                queued.SitAtHome(Random.Range(0f, _homeRestTime));
                _all.Add(queued);
            }

            Debug.Log($"[{nameof(CustomerQueue)}] Seated {_all.Count} customers at their own tables.", this);
        }

        private void StartDepartures()
        {
            _departuresActive = true;
            _departureTimer = 0f; // the first customer gets up immediately

            if (!_startShiftInLine)
                return;

            // Everyone starts in the line at the counter, in random order; they go to their own tables after ordering.
            _line.Clear();
            var order = new List<QueuedCustomer>(_all);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            foreach (QueuedCustomer queued in order)
            {
                if (queued == null)
                    continue;
                int index = _line.Count;
                queued.StandAtSpot(index, GetSpotPosition(index));
                _line.Add(queued);
            }
        }

        /// <summary>
        /// Every few seconds a random resting customer gets up and joins the line.
        /// </summary>
        private void UpdateDepartures()
        {
            if (!_homeTables)
                return;

            if (_repopulateHomes)
            {
                _repopulateHomes = false;
                PopulateHomes();
            }

            // Start as soon as the shift runs (the shift itself asks for the first customer only a few seconds later).
            if (!_departuresActive && (_shifts != null || ServiceLocator.TryGet(out _shifts)) && _shifts.ShiftInProgress)
            {
                PopulateHomes();
                StartDepartures();
            }

            if (!_departuresActive)
                return;

            _departureTimer -= Time.deltaTime;
            if (_departureTimer > 0f)
                return;

            _departureCandidates.Clear();
            foreach (QueuedCustomer queued in _all)
            {
                if (queued.CanLeaveHome)
                    _departureCandidates.Add(queued);
            }

            if (_departureCandidates.Count == 0)
            {
                _departureTimer = 0.5f;
                return;
            }

            QueuedCustomer leaving = _departureCandidates[Random.Range(0, _departureCandidates.Count)];
            leaving.GoToQueue();
            _departureTimer = Random.Range(_minDepartureInterval, Mathf.Max(_minDepartureInterval, _maxDepartureInterval));
        }
        #endregion

        #region Queue upkeep
        private void AssignSpots()
        {
            for (int i = 0; i < _line.Count; i++)
            {
                QueuedCustomer queued = _line[i];
                if (queued.QueueIndex == i)
                    continue;

                queued.GoToSpot(i, GetSpotPosition(i));
            }
        }

        private void UpdateFrontPatience()
        {
            QueuedCustomer front = _line.Count > 0 ? _line[0] : null;

            if (front != _frontCustomer)
            {
                _frontCustomer = front;
                _frontPatience = _queuePatience;
            }

            for (int i = 0; i < _line.Count; i++)
                _line[i].Customer.SetIdlePatience(i == 0 ? _frontPatience : _queuePatience);

            if (front == null || !front.ArrivedAtSpot || front.Phase != QueuedCustomerPhase.InQueue || PatienceTimersDisabled)
                return;

            _frontPatience = Mathf.Max(0f, _frontPatience - Time.deltaTime);
            if (_frontPatience > 0f)
                return;

            // Ran out of patience at the cashier: counts as unsatisfied, then goes to the end of the line.
            _customerService.ReportQueueTimeout(front.Customer);
            _line.RemoveAt(0);
            _frontCustomer = null;
            JoinLine(front);
        }

        private void PurgeDestroyed()
        {
            _all.RemoveAll(q => q == null || q.Customer == null);
            _line.RemoveAll(q => q == null || q.Customer == null);
        }
        #endregion

        #region Positions and paths
        private Vector3 EntrancePosition => _entrance != null ? _entrance.position : transform.position - transform.forward * 8f;

        /// <summary>
        /// Gets the world position of a queue spot. Index 0 is the cashier.
        /// </summary>
        public Vector3 GetSpotPosition(int index)
        {
            if (_spots != null && index < _spots.Length && _spots[index] != null)
                return _spots[index].position;

            Vector3 origin = transform.position;
            Vector3 direction = EntrancePosition - origin;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : -transform.forward;

            int lastExplicit = -1;
            if (_spots != null)
            {
                for (int i = Mathf.Min(index, _spots.Length) - 1; i >= 0; i--)
                {
                    if (_spots[i] != null) { lastExplicit = i; break; }
                }
            }

            if (lastExplicit >= 0)
                origin = _spots[lastExplicit].position;

            int steps = index - Mathf.Max(0, lastExplicit);
            return SampleNavMesh(origin + direction * (_spotSpacing * steps));
        }

        /// <summary>
        /// Direction a customer standing at this spot looks: at the customer in front, or at the counter for the first one.
        /// </summary>
        public Vector3 GetFacingDirection(int index)
        {
            Vector3 here = GetSpotPosition(index);
            Vector3 direction = index > 0
                ? GetSpotPosition(index - 1) - here
                : (_counter != null ? _counter.position - here : -transform.forward);

            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : -transform.forward;
        }

        /// <summary>
        /// Builds a walkable path between two points. Falls back to a straight line when no NavMesh path exists.
        /// </summary>
        public void BuildPath(Vector3 from, Vector3 to, List<Vector3> waypoints)
        {
            waypoints.Clear();
            _path ??= new NavMeshPath();

            var filter = new NavMeshQueryFilter { agentTypeID = _agentTypeId, areaMask = NavMesh.AllAreas };

            if (NavMesh.SamplePosition(from, out var start, _navMeshSampleRadius, filter) &&
                NavMesh.SamplePosition(to, out var end, _navMeshSampleRadius, filter) &&
                NavMesh.CalculatePath(start.position, end.position, filter, _path) &&
                _path.status != NavMeshPathStatus.PathInvalid)
            {
                var corners = _path.corners;
                for (int i = 1; i < corners.Length; i++)
                    waypoints.Add(corners[i]);
            }

            waypoints.Add(to);
        }

        private Vector3 SampleNavMesh(Vector3 position)
        {
            var filter = new NavMeshQueryFilter { agentTypeID = _agentTypeId, areaMask = NavMesh.AllAreas };
            return NavMesh.SamplePosition(position, out var hit, _navMeshSampleRadius, filter) ? hit.position : position;
        }
        #endregion

        /// <summary>
        /// Gets the sprite shown in the order bubble for an order.
        /// </summary>
        public static Sprite GetOrderSprite(ItemDefinition order)
        {
            if (order != null && order.TryGetProperty<FoodProperty>(out var food))
                return food.WorldSprite != null ? food.WorldSprite : food.DialogueSprite;

            return null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < Mathf.Max(_maxCustomers, 1); i++)
            {
                Vector3 p = GetSpotPosition(i);
                Gizmos.DrawWireSphere(p + Vector3.up * 0.5f, i == 0 ? 0.45f : 0.3f);
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, EntrancePosition);
        }
#endif
    }
}
