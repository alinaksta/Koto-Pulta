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

        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 2.5f;

        [Header("Visuals")]
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

        public float WalkSpeed => _walkSpeed;
        public float QueuePatience => _queuePatience;
        public float FrontPatience => _frontPatience;
        public float OrderDuration => _orderDuration;
        public float LeaveTableDelay => _leaveTableDelay;
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

        private void Update()
        {
            PurgeDestroyed();
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

            if (!_customerService.TryReserveRandomFreeTable(queued.Customer, out Table table, out Seat seat))
            {
                Debug.Log($"[{nameof(CustomerQueue)}] No free table - the customer keeps waiting.", this);
                return false;
            }

            _line.Remove(queued);
            queued.StartOrder(order, table, seat, _tableWaitTime);
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
            queued.LeaveTable(_leaveTableDelay);
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

            if (front == null || !front.ArrivedAtSpot || front.Phase != QueuedCustomerPhase.InQueue)
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
