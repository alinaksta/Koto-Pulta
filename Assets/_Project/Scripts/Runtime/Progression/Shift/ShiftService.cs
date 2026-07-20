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
    /// <summary>
    /// Represents a configured gameplay shift.
    /// </summary>
    [Serializable]
    public struct Shift
    {
        /// <summary>
        /// Revenue required to complete the shift.
        /// </summary>
        public int GoalRevenue;

        /// <summary>
        /// Delay in seconds between customer spawn attempts.
        /// </summary>
        public float CustomerAppearanceDelay;

        /// <summary>
        /// Number of recipes available during the shift.
        /// </summary>
        public int AvailableRecepies;
    }

    /// <summary>
    /// Stores shift definitions and runs the shift game mode.
    /// </summary>
    public class ShiftService : MonoBehaviour, IBootstrapable, IGameMode
    {
        private const float InitialCustomerSpawnDelay = 4f;

        [SerializeField] private List<Shift> _shifts;
        [SerializeField] private List<ItemDefinitionAsset> _allowedItems;
        [SerializeField] private float _shiftDuration = 180f;

        private GameModeContext _context;
        private int _shiftIndex = -1;
        private int _currentRevenue;
        private int _lastKnownBalance;
        private float _shiftTimer;
        private float _spawnTimer;
        private float _activeShiftDuration;
        private bool _modeActive;

        private Shift _practiceShift;
        private float _practiceCustomerWaitDuration;
        private bool _practiceShiftQueued;
        private bool _practiceShiftActive;
        private bool _practiceCustomerSpawned;
        private bool _normalShiftStartLocked;
        private bool _currentShiftFailed;

        private ShiftStatisticsCollector _statisticsCollector;
        private ShiftStatistics _lastShiftStatistics;

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
        public Shift CurrentShift => _practiceShiftActive
            ? _practiceShift
            : HasNormalShift ? _shifts[Mathf.Min(_shiftIndex, ShiftAmount - 1)] : default;

        /// <summary>
        /// Gets whether a configured, endless, or practice shift is selected.
        /// </summary>
        public bool HasCurrentShift => _practiceShiftActive || HasNormalShift;

        /// <summary>
        /// Gets whether the active shift is the one-customer tutorial practice shift.
        /// </summary>
        public bool IsPracticeShift => _practiceShiftActive;

        /// <summary>
        /// Gets whether the current shift is an endless shift after the configured sequence.
        /// </summary>
        public bool IsEndlessShift => !_practiceShiftActive
            && ShiftAmount > 0
            && _shiftIndex >= ShiftAmount;

        /// <summary>
        /// Gets the shift index used when resolving unlocked items.
        /// </summary>
        public int ItemUnlockShiftIndex => _practiceShiftActive ? 0 : _shiftIndex;

        private bool HasNormalShift => _shiftIndex >= 0 && ShiftAmount > 0;

        /// <summary>
        /// Gets the remaining time for the active shift.
        /// </summary>
        public float ShiftTimer => _shiftTimer;

        /// <summary>
        /// Gets whether a shift is currently in progress.
        /// </summary>
        public bool ShiftInProgress => _modeActive && HasCurrentShift && (_practiceShiftActive || _shiftTimer > 0f);

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
        public int CurrentGoalRevenue => HasCurrentShift && !IsEndlessShift
            ? CurrentShift.GoalRevenue
            : 0;

        /// <summary>
        /// Gets the normalized revenue progress for the active shift.
        /// </summary>
        public float NormalizedRevenueProgress => CalculateNormalizedRevenueProgress();

        /// <summary>
        /// Gets the statistics from the most recently finished shift.
        /// </summary>
        public ShiftStatistics LastStatistics => _lastShiftStatistics;

        /// <summary>
        /// Gets whether the current normal shift ended in failure and can be retried.
        /// </summary>
        public bool CurrentShiftFailed => _currentShiftFailed;

        /// <summary>
        /// Gets whether the computer shift-start button may currently start a shift.
        /// </summary>
        public bool CanStartNextShift => !_modeActive && _currentShiftFailed && !_normalShiftStartLocked ||
                                         _modeActive && !ShiftInProgress && (!_normalShiftStartLocked || _practiceShiftQueued);

        /// <summary>
        /// Raised after a shift starts.
        /// </summary>
        public event Action OnShiftStarted = delegate { };

        /// <summary>
        /// Raised after a shift ends.
        /// </summary>
        public event Action OnShiftEnded = delegate { };

        /// <summary>
        /// Raised when the computer shift-start button availability may have changed.
        /// </summary>
        public event Action OnShiftStartAvailabilityChanged = delegate { };

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
            _activeShiftDuration = _shiftDuration;
            _currentRevenue = 0;
            _lastKnownBalance = _context.Balance.Balance;

            _context.Balance.OnBalanceChanged += HandleBalanceChanged;

            _statisticsCollector?.Dispose();
            _statisticsCollector = new ShiftStatisticsCollector(context.Customers);

            _context.Customers.OnCustomerServed += HandleCustomerServed;

            _context.Session.ResetState();
            _context.Session.StartRun();

            _practiceShiftQueued = false;
            _practiceShiftActive = false;
            _practiceCustomerSpawned = false;
            _currentShiftFailed = false;
            OnShiftStartAvailabilityChanged.Invoke();
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

            int unlockedTier = Mathf.Clamp(ItemUnlockShiftIndex + 1, 1, 4);
            return new ShiftRandomItemDefinitionGiver(itemList, unlockedTier, ServiceLocator.Get<UpgradeService>());
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            if (!ShiftInProgress)
                return;

            if (!_practiceShiftActive)
            {
                _shiftTimer = Mathf.Max(0f, _shiftTimer - deltaTime);

                if (_shiftTimer <= 0f)
                {
                    FinishCurrentShiftFromTimeout();
                    return;
                }
            }

            _spawnTimer -= deltaTime;

            if (_spawnTimer <= 0f)
                SpawnCustomerAndResetTimer();
        }

        private void FinishCurrentShift()
        {
            if (CurrentRevenue < CurrentGoalRevenue)
                FailCurrentShift();
            else
                CompleteCurrentShift();
        }

        private void FinishCurrentShiftFromTimeout()
        {
            _context?.Customers?.TimeoutAllActiveCustomers();
            FinishCurrentShift();
        }

        /// <inheritdoc/>
        public void Exit()
        {
            if (_context?.Balance != null)
                _context.Balance.OnBalanceChanged -= HandleBalanceChanged;

            if (_context?.Customers != null)
                _context.Customers.OnCustomerServed -= HandleCustomerServed;

            if (ShiftInProgress)
                EndCurrentShift();

            _statisticsCollector?.Dispose();
            _statisticsCollector = null;

            _modeActive = false;
            _shiftTimer = 0f;
            _spawnTimer = 0f;
            _currentRevenue = 0;
            _practiceShiftQueued = false;
            _practiceShiftActive = false;
            _practiceCustomerSpawned = false;
            _normalShiftStartLocked = false;
            _currentShiftFailed = false;
            OnShiftStartAvailabilityChanged.Invoke();
            _context = null;
        }

        /// <summary>
        /// Makes the next computer shift-start request launch a one-customer practice shift.
        /// </summary>
        public void QueuePracticeShift(Shift shift, float customerWaitDuration)
        {
            if (ShiftInProgress)
                throw new InvalidOperationException("Cannot queue a practice shift while another shift is running.");

            _practiceShift = shift;
            _practiceCustomerWaitDuration = Mathf.Max(1f, customerWaitDuration);
            _practiceShiftQueued = true;
            _practiceCustomerSpawned = false;
            _normalShiftStartLocked = true;
            OnShiftStartAvailabilityChanged.Invoke();
        }

        /// <summary>
        /// Controls whether normal shifts may be started after the practice shift.
        /// </summary>
        public void SetNormalShiftStartLocked(bool locked)
        {
            if (_normalShiftStartLocked == locked)
                return;

            _normalShiftStartLocked = locked;
            OnShiftStartAvailabilityChanged.Invoke();
        }

        /// <summary>
        /// Tries to start the next shift.
        /// </summary>
        public bool TryStartNextShift()
        {
            if (_currentShiftFailed)
                return RetryCurrentShift();

            if (!_modeActive || ShiftInProgress)
                return false;

            if (_normalShiftStartLocked && !_practiceShiftQueued)
                return false;

            StartNextShift();
            return true;
        }

        /// <summary>
        /// Starts the next shift.
        /// </summary>
        public void StartNextShift()
        {
            if (_currentShiftFailed)
            {
                RetryCurrentShift();
                return;
            }

            if (!_modeActive || ShiftInProgress)
                return;

            if (_normalShiftStartLocked && !_practiceShiftQueued)
                return;

            if (_practiceShiftQueued)
            {
                StartPracticeShift();
                return;
            }

            int nextShiftIndex = _shiftIndex + 1;
            _shiftIndex = nextShiftIndex;
            StartSelectedNormalShift();
        }

        /// <summary>
        /// Restarts the currently selected normal shift after it has failed.
        /// </summary>
        public bool RetryCurrentShift()
        {
            if (!_currentShiftFailed || ShiftInProgress || !HasNormalShift)
                return false;

            if (_normalShiftStartLocked)
                return false;

            StartSelectedNormalShift();
            return true;
        }

        private void StartSelectedNormalShift()
        {
            _currentShiftFailed = false;
            _currentRevenue = 0;
            _lastKnownBalance = _context.Balance.Balance;
            _activeShiftDuration = _shiftDuration;
            _shiftTimer = Mathf.Max(0f, _activeShiftDuration);
            _modeActive = true;
            _spawnTimer = InitialCustomerSpawnDelay;
            _context.Session.ResetState();
            _context.Session.StartRun();
            _context.Customers.SetRandomItemGiver(GetShiftRandomItemGiver());
            OnShiftStarted.Invoke();
            OnShiftStartAvailabilityChanged.Invoke();
        }

        private void StartPracticeShift()
        {
            _practiceShiftQueued = false;
            _practiceShiftActive = true;
            _practiceCustomerSpawned = false;
            _currentShiftFailed = false;
            _currentRevenue = 0;
            _lastKnownBalance = _context.Balance.Balance;
            _activeShiftDuration = 0f;
            _shiftTimer = 0f;
            _modeActive = true;
            _spawnTimer = InitialCustomerSpawnDelay;
            _context.Customers.SetRandomItemGiver(GetShiftRandomItemGiver());
            OnShiftStarted.Invoke();
            OnShiftStartAvailabilityChanged.Invoke();
        }

        /// <inheritdoc/>
        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }

        private float CalculateNormalizedShiftProgress()
        {
            if (!HasCurrentShift || _activeShiftDuration <= 0f)
                return 0f;

            float elapsedTime = _activeShiftDuration - _shiftTimer;
            return Mathf.Clamp01(elapsedTime / _activeShiftDuration);
        }

        private float CalculateNormalizedRevenueProgress()
        {
            if (CurrentGoalRevenue <= 0)
                return 0f;

            return Mathf.Clamp01((float)CurrentRevenue / CurrentGoalRevenue);
        }

        private void SpawnCustomerAndResetTimer()
        {
            if (_practiceShiftActive && _practiceCustomerSpawned)
            {
                _spawnTimer = float.MaxValue;
                return;
            }

            float? waitDuration = _practiceShiftActive ? _practiceCustomerWaitDuration : null;
            bool spawned = _context.Customers.TrySpawnCustomerAtRandomFreeTable(waitDuration);

            if (_practiceShiftActive && spawned)
                _practiceCustomerSpawned = true;

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
            bool wasPracticeShift = _practiceShiftActive;

            if (!wasPracticeShift)
            {
                _currentShiftFailed = false;
                _context.Session.MarkSucceeded();
            }

            EndCurrentShift();

            if (wasPracticeShift)
            {
                _practiceShiftActive = false;
                _currentShiftFailed = false;
                return;
            }
        }

        private void FailCurrentShift()
        {
            bool wasPracticeShift = _practiceShiftActive;

            if (!wasPracticeShift)
            {
                _currentShiftFailed = true;
                _modeActive = false;
                _context.Session.MarkFailed();
            }

            EndCurrentShift();

            if (wasPracticeShift)
            {
                _practiceShiftActive = false;
                _currentShiftFailed = false;
                return;
            }

            OnShiftStartAvailabilityChanged.Invoke();
        }

        private void EndCurrentShift()
        {
            if (!HasCurrentShift)
                return;

            _shiftTimer = 0f;
            _spawnTimer = 0f;
            _statisticsCollector.SetMoneyEarned(CurrentRevenue);
            _lastShiftStatistics = _statisticsCollector.GetStatistics();
            _statisticsCollector.Reset();
            OnShiftEnded.Invoke();
            OnShiftStartAvailabilityChanged.Invoke();
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

            if (_practiceShiftActive)
                CompleteCurrentShift();
        }
    }
}
