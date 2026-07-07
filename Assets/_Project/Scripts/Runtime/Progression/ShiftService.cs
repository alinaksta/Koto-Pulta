using Game.Characters;
using Game.Items;
using Game.Items.Properties;
using Game.Lifecycle;
using Game.Services;
using Itemworks.Core;
using Itemworks.UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Progression
{
    [Serializable]
    public struct Shift
    {
        public int GoalRevenue;
        public float CustomerAppearanceDelay;
        public int AvailableRecepies;
    }

    /// <summary>
    /// Stores shift definitions and runs the shift game mode.
    /// </summary>
    public class ShiftService : MonoBehaviour, IBootstrapable, IGameMode
    {
        [SerializeField] private List<Shift> _shifts;
        [SerializeField] private List<ItemDefinitionAsset> _allowedItems;
        [SerializeField] private float _shiftDuration = 180f;

        private GameModeContext _context;
        private int _shiftIndex = -1;
        private int _currentRevenue;
        private int _lastKnownBalance;
        private float _shiftTimer;
        private float _spawnTimer;
        private bool _modeActive;

        /// <inheritdoc/>
        public string Id => "shift";

        /// <summary>
        /// Gets the currently active shift index.
        /// </summary>
        public int ShiftIndex => _shiftIndex;

        /// <summary>
        /// Gets the configured amount of shifts.
        /// </summary>
        public int ShiftAmount => _shifts?.Count ?? 0;

        /// <summary>
        /// Gets the current shift definition.
        /// </summary>
        public Shift CurrentShift => HasCurrentShift ? _shifts[_shiftIndex] : default;

        /// <summary>
        /// Gets whether the current shift index points to a valid configured shift.
        /// </summary>
        public bool HasCurrentShift => _shiftIndex >= 0 && _shiftIndex < ShiftAmount;

        /// <summary>
        /// Gets the remaining time for the active shift.
        /// </summary>
        public float ShiftTimer => _shiftTimer;

        /// <summary>
        /// Gets whether a shift is currently in progress.
        /// </summary>
        public bool ShiftInProgress => _modeActive && HasCurrentShift && _shiftTimer > 0f;

        /// <summary>
        /// Gets the normalized elapsed progress of the active shift.
        /// </summary>
        public float NormalizedShiftProgress => CalculateNormalizedShiftProgress();

        /// <summary>
        /// Gets the revenue earned during the active shift.
        /// </summary>
        public int CurrentRevenue => _currentRevenue;

        /// <summary>
        /// Gets the revenue goal for the active shift.
        /// </summary>
        public int CurrentGoalRevenue => HasCurrentShift ? CurrentShift.GoalRevenue : 0;

        /// <summary>
        /// Gets the normalized revenue progress for the active shift.
        /// </summary>
        public float NormalizedRevenueProgress => CalculateNormalizedRevenueProgress();

        /// <summary>
        /// Raised after a shift starts.
        /// </summary>
        public event Action OnShiftStarted = delegate { };

        /// <summary>
        /// Raised after a shift ends.
        /// </summary>
        public event Action OnShiftEnded = delegate { };

        /// <inheritdoc/>
        public bool CanStart(GameModeContext context)
        {
            return context != null &&
                   context.Session != null &&
                   context.Balance != null &&
                   context.Customers != null &&
                   ShiftAmount > 0;
        }

        /// <inheritdoc/>
        public void Enter(GameModeContext context)
        {
            Debug.Log(context == null);
            _context = context;
            _modeActive = true;
            _shiftIndex = -1;
            _shiftTimer = 0f;
            _spawnTimer = 0f;
            _currentRevenue = 0;
            _lastKnownBalance = _context.Balance.Balance;

            _context.Balance.OnBalanceChanged += HandleBalanceChanged;

            _context.Customers.OnCustomerServed += HandleCustomerServed;

            _context.Customers.SetRandomItemGiver(GetShiftRandomItemGiver());

            _context.Session.ResetState();
            _context.Session.StartRun();

            StartNextShift();
        }

        private IRandomItemDefinitionGiver GetShiftRandomItemGiver()
        {
            List<ItemDefinition> itemList = new List<ItemDefinition>();
            foreach (var itemAsset in _allowedItems)
            {
                if (!ItemRegistry.Instance.TryGet(itemAsset.Id, out var definition))
                    continue;

                itemList.Add(definition);
            }

            return new ShiftRandomItemDefinitionGiver(this, itemList);
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            if (!ShiftInProgress)
                return;

            _shiftTimer = Mathf.Max(0f, _shiftTimer - deltaTime);
            _spawnTimer -= deltaTime;

            if (_spawnTimer <= 0f)
                SpawnCustomerAndResetTimer();

            if (CurrentRevenue >= CurrentGoalRevenue)
            {
                CompleteCurrentShift();
                return;
            }

            if (_shiftTimer <= 0f)
                FailCurrentShift();
        }

        /// <inheritdoc/>
        public void Exit()
        {
            if (_context?.Balance != null)
                _context.Balance.OnBalanceChanged -= HandleBalanceChanged;

            if (_context.Customers != null)
                _context.Customers.OnCustomerServed -= HandleCustomerServed;

            if (ShiftInProgress)
                EndCurrentShift();

            _modeActive = false;
            _shiftTimer = 0f;
            _spawnTimer = 0f;
            _currentRevenue = 0;
            _context = null;
        }

        /// <summary>
        /// Starts the next configured shift.
        /// </summary>
        public void StartNextShift()
        {
            if (ShiftInProgress)
                EndCurrentShift();

            int nextShiftIndex = _shiftIndex + 1;

            if (nextShiftIndex >= ShiftAmount)
            {
                _modeActive = false;
                _context.Session.MarkSucceeded();
                return;
            }

            _shiftIndex = nextShiftIndex;
            _currentRevenue = 0;
            _lastKnownBalance = _context.Balance.Balance;
            _shiftTimer = Mathf.Max(0f, _shiftDuration);
            _spawnTimer = GetSpawnDelay();
            OnShiftStarted.Invoke();
        }

        /// <inheritdoc/>
        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }

        private float CalculateNormalizedShiftProgress()
        {
            if (!HasCurrentShift || _shiftDuration <= 0f)
                return 0f;

            float elapsedTime = _shiftDuration - _shiftTimer;
            return Mathf.Clamp01(elapsedTime / _shiftDuration);
        }

        private float CalculateNormalizedRevenueProgress()
        {
            if (CurrentGoalRevenue <= 0)
                return 0f;

            return Mathf.Clamp01((float)CurrentRevenue / CurrentGoalRevenue);
        }

        private void SpawnCustomerAndResetTimer()
        {
            _context.Customers.TrySpawnCustomerAtRandomFreeTable();
            _spawnTimer = GetSpawnDelay();
        }

        private float GetSpawnDelay()
        {
            if (!HasCurrentShift)
                return 0.1f;

            return Mathf.Max(0.1f, CurrentShift.CustomerAppearanceDelay);
        }

        private void CompleteCurrentShift()
        {
            EndCurrentShift();

            if (_shiftIndex + 1 >= ShiftAmount)
            {
                _modeActive = false;
                _context.Session.MarkSucceeded();
                return;
            }

            StartNextShift();
        }

        private void FailCurrentShift()
        {
            EndCurrentShift();
            _modeActive = false;
            _context.Session.MarkFailed();
        }

        private void EndCurrentShift()
        {
            if (!HasCurrentShift)
                return;

            _shiftTimer = 0f;
            _spawnTimer = 0f;
            OnShiftEnded.Invoke();
        }

        private void HandleBalanceChanged(int newBalance)
        {
            if (newBalance > _lastKnownBalance)
                _currentRevenue += newBalance - _lastKnownBalance;

            _lastKnownBalance = newBalance;
        }


        private void HandleCustomerServed(Customer customer)
        {
            if (customer.Order.TryGetProperty(out FoodProperty food))
            {
                _context.Balance.Add(food.UnitPrice);
                Debug.Log("Here");
            }
        }
    }
}
