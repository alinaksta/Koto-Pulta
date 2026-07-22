using Game.Characters;
using Game.Services;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// Applies persistent waiter and table upgrades to the active restaurant scene.
    /// </summary>
    public sealed class RestaurantUpgradeController : MonoBehaviour
    {
        [Header("Waiters")]
        [SerializeField] private Waiter _waiterPrefab;
        [SerializeField] private Waiter[] _initialWaiters;
        [SerializeField] private Transform[] _waiterSpawnPoints;

        [Header("Tables")]
        [SerializeField] private Table _tablePrefab;
        [SerializeField] private Table[] _initialTables;
        [SerializeField] private Transform _tableLayoutCenter;
        [SerializeField, Min(0f)] private float _tableRadius = 7f;
        [SerializeField] private float _firstTableAngle;
        [SerializeField] private float _tableRotationOffset;

        private readonly List<Waiter> _waiters = new();
        private readonly List<Table> _tables = new();
        private UpgradeService _upgrades;
        private CustomerService _customers;

        private void Awake()
        {
            _upgrades = ServiceLocator.Get<UpgradeService>();
            _customers = ServiceLocator.Get<CustomerService>();

            AddExisting(_initialWaiters, _waiters);
            AddExisting(_initialTables, _tables);

            EnsureWaiterCount(_upgrades.GetLevel(UpgradeType.WaiterCount));
            InitializeAndLayoutTables();
            EnsureTableCount(_upgrades.GetLevel(UpgradeType.TableCount));
            InitializeAndLayoutTables();

            _upgrades.OnUpgradeChanged += HandleUpgradeChanged;
        }

        private void OnDestroy()
        {
            if (_upgrades != null)
                _upgrades.OnUpgradeChanged -= HandleUpgradeChanged;
        }

        private void HandleUpgradeChanged(UpgradeType type, int level)
        {
            if (type == UpgradeType.WaiterCount)
                EnsureWaiterCount(level);
            else if (type == UpgradeType.TableCount)
            {
                EnsureTableCount(level);
                InitializeAndLayoutTables();
            }
        }

        private void EnsureWaiterCount(int targetCount)
        {
            while (_waiters.Count < targetCount)
            {
                int index = _waiters.Count;
                if (_waiterPrefab == null || _waiterSpawnPoints == null || index >= _waiterSpawnPoints.Length || _waiterSpawnPoints[index] == null)
                {
                    Debug.LogError($"Cannot spawn waiter {index + 1}. Assign a prefab and at least {targetCount} waiter spawn points.", this);
                    return;
                }

                Transform spawnPoint = _waiterSpawnPoints[index];
                _waiters.Add(Instantiate(_waiterPrefab, spawnPoint.position, spawnPoint.rotation));
            }
        }

        private void EnsureTableCount(int targetCount)
        {
            while (_tables.Count < targetCount)
            {
                if (_tablePrefab == null)
                {
                    Debug.LogError("Cannot spawn an upgraded table because no table prefab is assigned.", this);
                    return;
                }

                _tables.Add(Instantiate(_tablePrefab));
            }
        }

        private void InitializeAndLayoutTables()
        {
            if (_tables.Count == 0)
                return;

            for (int i = 0; i < _tables.Count; i++)
            {
                Table table = _tables[i];
                if (table != null)
                    table.Initialize(i + 1, _customers);
            }

            if (_tableLayoutCenter == null)
                return;

            float angleStep = 360f / _tables.Count;
            for (int i = 0; i < _tables.Count; i++)
            {
                Table table = _tables[i];
                if (table == null)
                    continue;

                float angle = _firstTableAngle + angleStep * i;
                Vector3 radialOffset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * _tableRadius;
                table.transform.position = _tableLayoutCenter.position + radialOffset;

                Vector3 towardCenter = _tableLayoutCenter.position - table.transform.position;
                towardCenter.y = 0f;
                if (towardCenter.sqrMagnitude > 0.001f)
                {
                    Quaternion facing = Quaternion.LookRotation(towardCenter.normalized, Vector3.up);
                    table.transform.rotation = facing * Quaternion.Euler(0f, _tableRotationOffset, 0f);
                }
            }
        }

        private static void AddExisting<T>(T[] source, List<T> destination) where T : Object
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != null && !destination.Contains(source[i]))
                    destination.Add(source[i]);
            }
        }
    }
}
