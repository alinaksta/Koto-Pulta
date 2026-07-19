using Game.Characters;
using Game.Input;
using Game.Interaction;
using Game.Items;
using Game.Items.Components;
using Game.Lifecycle;
using Game.Services;
using Game.UI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Game.Progression
{
    /// <summary>
    /// Runs the first-time player tutorial as a game mode.
    /// </summary>
    public sealed class TutorialService : MonoBehaviour, IBootstrapable, IGameMode
    {
        [Header("Persistence")]
        [SerializeField] private string _completionKey = "Tutorial.Completed";

        [Header("Practice Shift")]
        [SerializeField] private Shift _practiceShift = new Shift
        {
            GoalRevenue = 0,
            CustomerAppearanceDelay = 0.25f,
            AvailableRecepies = 1
        };
        [SerializeField, Min(1f)] private float _practiceCustomerWaitDuration = 900f;

        [Header("Basics")]
        [SerializeField] private TutorialStep _movementStep = new(
            "Use W, A, S, and D to move around.");
        [SerializeField] private TutorialStep _pickupStep = new(
            "Use LMB for your left hand or RMB for your right hand to pick up an object.");
        [SerializeField] private TutorialStep _dropStep = new(
            "Press Q to drop the left-hand item, or E to drop the right-hand item.");
        [SerializeField] private TutorialStep _throwWaiterStep = new(
            "Pick up a waiter, hold the matching mouse button to aim, then release it to throw.");

        [Header("Computer")]
        [SerializeField] private TutorialStep _computerStep = new(
            "Open the computer. I will explain the tabs from there.");
        [SerializeField] private TutorialStep _shiftTabStep = new(
            "The Shift tab starts shifts and shows statistics from the previous shift.");
        [SerializeField] private TutorialStep _shopTabStep = new(
            "The Shop tab lets you unlock and manage upgrades.");
        [SerializeField] private TutorialStep _mealsTabStep = new(
            "The Meals tab is where you select meals requested by customers.");
        [SerializeField] private TutorialStep _exitComputerStep = new(
            "You can close the computer by pressing Z.");

        [Header("Practice Order")]
        [SerializeField] private TutorialStep _startShiftStep = new(
            "Open the Shift tab and start your first shift.");
        [SerializeField] private TutorialStep _waiterOrderStep = new(
            "A waiter will ask a customer for their order, then return and wait for you to provide that meal.");
        [SerializeField] private TutorialStep _giveMealStep = new(
            "Open the Meals tab, get the requested meal, and give it to the waiting waiter.");
        [SerializeField] private TutorialStep _deliverStep = new(
            "The note above the waiter shows the table number. Throw the waiter to that table.");
        [SerializeField] private TutorialStep _successStep = new(
            "Good job! The order was delivered successfully.");
        [SerializeField] private TutorialStep _failureStep = new(
            "That delivery missed. Be more careful with the table number and your aim.");
        [SerializeField] private TutorialStep _retryStep = new(
            "If the meal is wrong or the waiter misses, the order remains active and the waiter will ask for the item again.");
        [SerializeField] private TutorialStep _statisticsComputerStep = new(
            "Shift statistics are shown on the Shift tab of the computer. Open the computer to view them.");
        [SerializeField] private TutorialStep _finishStep = new(
            "You can start the next shift from that tab. You are ready to play!");

        [Header("Events")]
        [SerializeField] private UnityEvent _onTutorialCompleted = new();

        [Header("Marker Offsets")]
        [SerializeField] private Vector3 _defaultMarkerOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private Vector3 _pickupMarkerOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField, FormerlySerializedAs("_waiterMarkerOffset")] private Vector3 _throwWaiterMarkerOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private Vector3 _giveMealWaiterMarkerOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private Vector3 _computerMarkerOffset = new Vector3(0f, 2.8f, 0f);
        [SerializeField] private Vector3 _tableMarkerOffset = new Vector3(0f, 2f, 0f);

        [Header("UI Marker Offsets")]
        [SerializeField] private Vector2 _tabMarkerOffset = Vector2.zero;

        private readonly HashSet<TutorialSignal> _latchedSignals = new();

        private GameModeContext _context;
        private DialogueService _dialogue;
        private ShiftService _shifts;
        private IInputService _input;
        private TutorialSceneBindings _scene;
        private TaskCompletionSource<TutorialSceneBindings> _sceneSource;
        private TaskCompletionSource<bool> _signalSource;
        private CancellationTokenSource _tutorialCancellation;
        private TutorialSignal _expectedSignal;
        private Waiter _tutorialWaiter;
        private Customer _tutorialCustomer;
        private WaiterLandingResult _landingResult;
        private bool _customerServed;
        private bool _active;

        /// <inheritdoc/>
        public string Id => "tutorial";

        /// <summary>
        /// Gets the current world-space marker target.
        /// </summary>
        public Transform Target { get; private set; }

        /// <summary>
        /// Gets the offset applied to the current marker target.
        /// </summary>
        public Vector3 TargetOffset { get; private set; }

        /// <summary>
        /// Gets the current canvas-space marker target.
        /// </summary>
        public RectTransform UiTarget { get; private set; }

        /// <summary>
        /// Gets the offset applied to the current canvas-space marker target.
        /// </summary>
        public Vector2 UiTargetOffset { get; private set; }

        public event Action<Transform, Vector3> OnTargetChanged = delegate { };
        public event Action<RectTransform, Vector2> OnUiTargetChanged = delegate { };

        /// <inheritdoc/>
        public bool CanStart(GameModeContext context)
        {
            return !IsCompleted &&
                   context != null &&
                   context.Customers != null &&
                   context.Waiters != null &&
                   ServiceLocator.TryGet<DialogueService>(out _) &&
                   ServiceLocator.TryGet<ShiftService>(out ShiftService shifts) &&
                   shifts.CanStart(context) &&
                   ServiceLocator.TryGet<IInputService>(out _);
        }

        /// <inheritdoc/>
        public void Enter(GameModeContext context)
        {
            _context = context;
            _dialogue = ServiceLocator.Get<DialogueService>();
            _shifts = ServiceLocator.Get<ShiftService>();
            _input = ServiceLocator.Get<IInputService>();
            _tutorialCancellation = new CancellationTokenSource();
            _active = true;
            _customerServed = false;
            _tutorialCustomer = null;
            _landingResult = default;
            _latchedSignals.Clear();

            _shifts.Enter(context);
            _shifts.SetNormalShiftStartLocked(true);
            SubscribeServices();
            _ = RunTutorialAsync(_tutorialCancellation.Token);
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            if (!_active)
                return;

            _shifts.Tick(deltaTime);

            if (_expectedSignal == TutorialSignal.Moved && _input.Move.sqrMagnitude > 0.01f)
                ReportSignal(TutorialSignal.Moved);
        }

        /// <inheritdoc/>
        public void Exit()
        {
            _active = false;
            _tutorialCancellation?.Cancel();
            _tutorialCancellation?.Dispose();
            _tutorialCancellation = null;
            _signalSource?.TrySetCanceled();
            _sceneSource?.TrySetCanceled();

            UnsubscribeScene();
            UnsubscribeServices();
            UnsubscribeWaiter();

            if (_tutorialCustomer != null && _tutorialCustomer.IsWaiting)
            {
                _tutorialCustomer.SetTimeoutEnabled(true);
                _tutorialCustomer.SetDirectDeliveryEnabled(true);
                _tutorialCustomer.ForceTimeout();
            }

            _dialogue?.Hide();
            ClearTarget();
            ClearUiTarget();
            _shifts?.Exit();

            _expectedSignal = TutorialSignal.None;
            _context = null;
        }

        /// <inheritdoc/>
        public void Bootstrap()
        {
            ServiceLocator.Register(this);
        }

        /// <summary>
        /// Reports a typed completion signal from code or an Inspector UnityEvent.
        /// </summary>
        public void ReportSignal(TutorialSignal signal)
        {
            if (!_active || signal == TutorialSignal.None)
                return;

            if (signal == _expectedSignal)
                _signalSource?.TrySetResult(true);
            else if (signal == TutorialSignal.ExitedComputer || signal == TutorialSignal.StartedPracticeShift)
                return;
            else
                _latchedSignals.Add(signal);
        }

        /// <summary>
        /// Completes the current signal-driven step. Intended for simple Inspector wiring.
        /// </summary>
        public void CompleteCurrentStep()
        {
            ReportSignal(_expectedSignal);
        }

        /// <summary>
        /// Sets the object followed by the tutorial marker view.
        /// </summary>
        public void SetTarget(Transform target)
        {
            SetTarget(target, _defaultMarkerOffset);
        }

        /// <summary>
        /// Sets the object followed by the tutorial marker view with a custom offset.
        /// </summary>
        public void SetTarget(Transform target, Vector3 offset)
        {
            if (Target == target && TargetOffset == offset)
                return;

            Target = target;
            TargetOffset = offset;
            OnTargetChanged.Invoke(Target, TargetOffset);
        }

        /// <summary>
        /// Sets the UI object followed by the canvas tutorial marker view.
        /// </summary>
        public void SetUiTarget(RectTransform target)
        {
            SetUiTarget(target, Vector2.zero);
        }

        /// <summary>
        /// Sets the UI object followed by the canvas tutorial marker view with a custom offset.
        /// </summary>
        public void SetUiTarget(RectTransform target, Vector2 offset)
        {
            if (UiTarget == target && UiTargetOffset == offset)
                return;

            UiTarget = target;
            UiTargetOffset = offset;
            OnUiTargetChanged.Invoke(UiTarget, UiTargetOffset);
        }

        /// <summary>
        /// Clears the current tutorial marker target.
        /// </summary>
        public void ClearTarget()
        {
            SetTarget(null, _defaultMarkerOffset);
        }

        /// <summary>
        /// Clears the current canvas-space tutorial marker target.
        /// </summary>
        public void ClearUiTarget()
        {
            SetUiTarget(null, Vector2.zero);
        }

        /// <summary>
        /// Clears persisted tutorial completion for testing or a new profile.
        /// </summary>
        [ContextMenu("Reset Tutorial Progress")]
        public void ResetTutorialProgress()
        {
            PlayerPrefs.DeleteKey(_completionKey);
            PlayerPrefs.Save();
        }

        internal void BindScene(TutorialSceneBindings scene)
        {
            if (_scene == scene)
                return;

            UnsubscribeScene();
            _scene = scene;

            if (_active)
                SubscribeScene();

            _sceneSource?.TrySetResult(_scene);
        }

        internal void UnbindScene(TutorialSceneBindings scene)
        {
            if (_scene != scene)
                return;

            UnsubscribeScene();
            _scene = null;
        }

        private bool IsCompleted => PlayerPrefs.GetInt(_completionKey, 0) != 0;

        private async Task RunTutorialAsync(CancellationToken cancellationToken)
        {
            try
            {
                await WaitForSceneAsync(cancellationToken);

                await RunSignalStepAsync(_movementStep, TutorialSignal.Moved, null, cancellationToken);
                await RunSignalStepAsync(_pickupStep, TutorialSignal.PickedUpObject, _scene.PickupTarget, _pickupMarkerOffset, cancellationToken);
                await RunSignalStepAsync(_dropStep, TutorialSignal.DroppedObject, null, cancellationToken);

                Waiter availableWaiter = await WaitForAnyWaiterAsync(cancellationToken);
                await RunSignalStepAsync(_throwWaiterStep, TutorialSignal.ThrewWaiter, availableWaiter.transform, _throwWaiterMarkerOffset, cancellationToken);

                await RunComputerIntroStepAsync(cancellationToken);
                await RunComputerTabsStepAsync(cancellationToken);
                await RunComputerCloseInstructionAsync(cancellationToken);

                if (_scene.ComputerTabs != null)
                    _scene.ComputerTabs.SetTabButtonsInteractable(true);

                _shifts.QueuePracticeShift(_practiceShift, _practiceCustomerWaitDuration);

                await RunStartShiftStepAsync(cancellationToken);

                await RunSignalStepAsync(
                    _waiterOrderStep,
                    TutorialSignal.WaiterAskedCustomer,
                    _tutorialWaiter != null ? _tutorialWaiter.transform : null,
                    _giveMealWaiterMarkerOffset,
                    cancellationToken);

                await RunGiveMealStepAsync(cancellationToken);

                Transform tableTarget = _tutorialWaiter != null && _tutorialWaiter.AssignedCustomer != null
                    ? _tutorialWaiter.AssignedCustomer.Table.transform
                    : null;
                await RunSignalStepAsync(_deliverStep, TutorialSignal.WaiterLanded, tableTarget, _tableMarkerOffset, cancellationToken);

                await RunDialogueStepAsync(
                    _landingResult == WaiterLandingResult.Delivered ? _successStep : _failureStep,
                    cancellationToken);
                await RunDialogueStepAsync(_retryStep, cancellationToken);

                if (!_customerServed)
                {
                    SetTarget(_tutorialWaiter != null ? _tutorialWaiter.transform : null, _giveMealWaiterMarkerOffset);
                    await WaitForSignalAsync(TutorialSignal.CustomerServed, cancellationToken);
                }

                if (_scene.ComputerTabs != null)
                    _scene.ComputerTabs.SetTab(ComputerSiteTab.ShiftStatistics);

                await RunStatisticsComputerStepAsync(cancellationToken);
                await RunDialogueStepAsync(_finishStep, _scene.ComputerTarget, _computerMarkerOffset, cancellationToken);
                CompleteTutorial();
            }
            catch (OperationCanceledException)
            {
                // Exiting the game mode cancels the active tutorial sequence.
            }
        }

        private async Task RunSignalStepAsync(
            TutorialStep step,
            TutorialSignal signal,
            Transform target,
            CancellationToken cancellationToken)
        {
            await RunSignalStepAsync(step, signal, target, _defaultMarkerOffset, cancellationToken);
        }

        private async Task RunSignalStepAsync(
            TutorialStep step,
            TutorialSignal signal,
            Transform target,
            Vector3 markerOffset,
            CancellationToken cancellationToken)
        {
            SetTarget(target, markerOffset);
            ClearUiTarget();
            step.InvokeStarted();
            await _dialogue.DisplayLinesAsync(step.Lines, cancellationToken, false);
            await WaitForSignalAsync(signal, cancellationToken);
            step.InvokeCompleted();
        }

        private async Task RunDialogueStepAsync(TutorialStep step, CancellationToken cancellationToken)
        {
            ClearTarget();
            ClearUiTarget();
            step.InvokeStarted();
            await _dialogue.DisplayLinesAsync(step.Lines, cancellationToken);
            step.InvokeCompleted();
        }

        private async Task RunDialogueStepAsync(
            TutorialStep step,
            Transform target,
            Vector3 markerOffset,
            CancellationToken cancellationToken)
        {
            SetTarget(target, markerOffset);
            ClearUiTarget();
            step.InvokeStarted();
            await _dialogue.DisplayLinesAsync(step.Lines, cancellationToken);
            step.InvokeCompleted();
        }

        private async Task RunStartShiftStepAsync(CancellationToken cancellationToken)
        {
            ClearTarget();
            SetUiTarget(_scene.ShiftTabTarget, _tabMarkerOffset);

            if (_scene.ShiftTabTarget == null)
                SetTarget(_scene.ComputerTarget, _computerMarkerOffset);

            _startShiftStep.InvokeStarted();
            await _dialogue.DisplayLinesAsync(_startShiftStep.Lines, cancellationToken, false);
            await WaitForSignalAsync(TutorialSignal.StartedPracticeShift, cancellationToken);
            _startShiftStep.InvokeCompleted();
            ClearUiTarget();
        }

        private async Task RunGiveMealStepAsync(CancellationToken cancellationToken)
        {
            SetTarget(_scene.ComputerTarget, _computerMarkerOffset);
            SetUiTarget(_scene.MealsTabTarget, _tabMarkerOffset);

            if (_scene.MealsTabTarget == null)
                ClearUiTarget();

            _giveMealStep.InvokeStarted();
            await _dialogue.DisplayLinesAsync(_giveMealStep.Lines, cancellationToken, false);
            await WaitForSignalAsync(TutorialSignal.GaveMealToWaiter, cancellationToken);
            _giveMealStep.InvokeCompleted();
            ClearUiTarget();
        }

        private async Task RunStatisticsComputerStepAsync(CancellationToken cancellationToken)
        {
            _latchedSignals.Remove(TutorialSignal.OpenedComputer);
            SetTarget(_scene.ComputerTarget, _computerMarkerOffset);
            ClearUiTarget();
            _statisticsComputerStep.InvokeStarted();
            await _dialogue.DisplayLinesAsync(_statisticsComputerStep.Lines, cancellationToken, false);

            if (_scene.ComputerTabs != null && !_scene.ComputerTabs.IsFocused)
                await WaitForSignalAsync(TutorialSignal.OpenedComputer, cancellationToken);

            _statisticsComputerStep.InvokeCompleted();
        }

        private async Task RunComputerIntroStepAsync(CancellationToken cancellationToken)
        {
            await RunSignalStepAsync(
                _computerStep,
                TutorialSignal.OpenedComputer,
                _scene.ComputerTarget,
                _computerMarkerOffset,
                cancellationToken);
        }

        private async Task RunComputerTabsStepAsync(CancellationToken cancellationToken)
        {
            if (_scene.ComputerTabs == null)
                return;

            _scene.ComputerTabs.SetTabButtonsInteractable(false);

            try
            {
                await ExplainTabAsync(ComputerSiteTab.ShiftStatistics, _scene.ShiftTabTarget, _shiftTabStep, cancellationToken);
                await ExplainTabAsync(ComputerSiteTab.Shop, _scene.ShopTabTarget, _shopTabStep, cancellationToken);
                await ExplainTabAsync(ComputerSiteTab.Meals, _scene.MealsTabTarget, _mealsTabStep, cancellationToken);
            }
            finally
            {
                ClearUiTarget();
            }
        }

        private async Task RunComputerCloseInstructionAsync(CancellationToken cancellationToken)
        {
            SetTarget(_scene.ComputerTarget, _computerMarkerOffset);
            ClearUiTarget();
            _exitComputerStep.InvokeStarted();
            await _dialogue.DisplayLinesAsync(_exitComputerStep.Lines, cancellationToken);
            _exitComputerStep.InvokeCompleted();
        }

        private async Task ExplainTabAsync(
            ComputerSiteTab tab,
            RectTransform markerTarget,
            TutorialStep step,
            CancellationToken cancellationToken)
        {
            ClearTarget();
            ClearUiTarget();
            _scene.ComputerTabs.SetTab(tab);

            step.InvokeStarted();
            await _dialogue.DisplayLinesAsync(step.Lines, cancellationToken);
            step.InvokeCompleted();
        }

        private async Task WaitForSignalAsync(TutorialSignal signal, CancellationToken cancellationToken)
        {
            if (_latchedSignals.Remove(signal))
                return;

            _expectedSignal = signal;
            var signalSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _signalSource = signalSource;

            using CancellationTokenRegistration registration = cancellationToken.Register(
                () => signalSource.TrySetCanceled());

            await signalSource.Task;

            if (ReferenceEquals(_signalSource, signalSource))
                _signalSource = null;

            _expectedSignal = TutorialSignal.None;
        }

        private async Task<TutorialSceneBindings> WaitForSceneAsync(CancellationToken cancellationToken)
        {
            if (_scene != null)
            {
                SubscribeScene();
                return _scene;
            }

            var sceneSource = new TaskCompletionSource<TutorialSceneBindings>(TaskCreationOptions.RunContinuationsAsynchronously);
            _sceneSource = sceneSource;
            using CancellationTokenRegistration registration = cancellationToken.Register(
                () => sceneSource.TrySetCanceled());

            TutorialSceneBindings scene = await sceneSource.Task;

            if (ReferenceEquals(_sceneSource, sceneSource))
                _sceneSource = null;

            SubscribeScene();
            return scene;
        }

        private async Task<Waiter> WaitForAnyWaiterAsync(CancellationToken cancellationToken)
        {
            Waiter waiter;
            while (!_context.Waiters.TryGetAnyWaiter(out waiter))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            return waiter;
        }

        private void SubscribeServices()
        {
            _shifts.OnShiftStarted += HandleShiftStarted;
            _context.Customers.OnCustomerSpawned += HandleCustomerSpawned;
            _context.Customers.OnCustomerServed += HandleCustomerServed;
            _context.Waiters.OnCustomerAssignedToWaiter += HandleCustomerAssigned;
        }

        private void UnsubscribeServices()
        {
            if (_shifts != null)
                _shifts.OnShiftStarted -= HandleShiftStarted;

            if (_context?.Customers != null)
            {
                _context.Customers.OnCustomerSpawned -= HandleCustomerSpawned;
                _context.Customers.OnCustomerServed -= HandleCustomerServed;
            }

            if (_context?.Waiters != null)
                _context.Waiters.OnCustomerAssignedToWaiter -= HandleCustomerAssigned;
        }

        private void SubscribeScene()
        {
            if (_scene?.PlayerInteractor == null)
                return;

            Hand leftHand = _scene.PlayerInteractor.LeftHand;
            Hand rightHand = _scene.PlayerInteractor.RightHand;

            leftHand.OnItemChanged -= HandleHandItemChanged;
            rightHand.OnItemChanged -= HandleHandItemChanged;
            leftHand.OnItemDropped -= HandleItemDropped;
            rightHand.OnItemDropped -= HandleItemDropped;
            leftHand.OnItemThrown -= HandleItemThrown;
            rightHand.OnItemThrown -= HandleItemThrown;

            leftHand.OnItemChanged += HandleHandItemChanged;
            rightHand.OnItemChanged += HandleHandItemChanged;
            leftHand.OnItemDropped += HandleItemDropped;
            rightHand.OnItemDropped += HandleItemDropped;
            leftHand.OnItemThrown += HandleItemThrown;
            rightHand.OnItemThrown += HandleItemThrown;

            if (_scene.ComputerTabs != null)
            {
                _scene.ComputerTabs.OnComputerEntered -= HandleComputerEntered;
                _scene.ComputerTabs.OnTabViewed -= HandleTabViewed;
                _scene.ComputerTabs.OnComputerExited -= HandleComputerExited;
                _scene.ComputerTabs.OnComputerEntered += HandleComputerEntered;
                _scene.ComputerTabs.OnTabViewed += HandleTabViewed;
                _scene.ComputerTabs.OnComputerExited += HandleComputerExited;
            }
        }

        private void UnsubscribeScene()
        {
            if (_scene?.PlayerInteractor != null)
            {
                Hand leftHand = _scene.PlayerInteractor.LeftHand;
                Hand rightHand = _scene.PlayerInteractor.RightHand;
                leftHand.OnItemChanged -= HandleHandItemChanged;
                rightHand.OnItemChanged -= HandleHandItemChanged;
                leftHand.OnItemDropped -= HandleItemDropped;
                rightHand.OnItemDropped -= HandleItemDropped;
                leftHand.OnItemThrown -= HandleItemThrown;
                rightHand.OnItemThrown -= HandleItemThrown;
            }

            if (_scene?.ComputerTabs != null)
            {
                _scene.ComputerTabs.OnComputerEntered -= HandleComputerEntered;
                _scene.ComputerTabs.OnTabViewed -= HandleTabViewed;
                _scene.ComputerTabs.OnComputerExited -= HandleComputerExited;
                _scene.ComputerTabs.SetTabButtonsInteractable(true);
            }
        }

        private void HandleHandItemChanged(Item? item)
        {
            if (!item.HasValue)
                return;

            if (_expectedSignal == TutorialSignal.GaveMealToWaiter)
            {
                ClearUiTarget();
                SetTarget(_tutorialWaiter != null ? _tutorialWaiter.transform : null, _giveMealWaiterMarkerOffset);
                return;
            }

            ReportSignal(TutorialSignal.PickedUpObject);
        }

        private void HandleItemDropped(Item item)
        {
            ReportSignal(TutorialSignal.DroppedObject);
        }

        private void HandleItemThrown(Item item)
        {
            if (item.TryGetComponent<WaiterComponent>(out _))
                ReportSignal(TutorialSignal.ThrewWaiter);
        }

        private void HandleComputerEntered()
        {
            ReportSignal(TutorialSignal.OpenedComputer);
        }

        private void HandleTabViewed(ComputerSiteTab tab)
        {
            if (tab == ComputerSiteTab.ShiftStatistics && UiTarget == _scene.ShiftTabTarget)
                ClearUiTarget();
            else if (tab == ComputerSiteTab.Meals && UiTarget == _scene.MealsTabTarget)
                ClearUiTarget();
        }

        private void HandleComputerExited()
        {
            ReportSignal(TutorialSignal.ExitedComputer);
        }

        private void HandleShiftStarted()
        {
            if (_shifts.IsPracticeShift)
                ReportSignal(TutorialSignal.StartedPracticeShift);
        }

        private void HandleCustomerSpawned(Customer customer)
        {
            if (!_shifts.IsPracticeShift)
                return;

            _tutorialCustomer = customer;
            _tutorialCustomer.SetTimeoutEnabled(false);
            _tutorialCustomer.SetDirectDeliveryEnabled(false);
        }

        private void HandleCustomerAssigned(Waiter waiter, Customer customer)
        {
            if (customer != _tutorialCustomer)
                return;

            UnsubscribeWaiter();
            _tutorialWaiter = waiter;
            _tutorialWaiter.OnCustomerWasAsked += HandleCustomerWasAsked;
            _tutorialWaiter.OnServiceStateChanged += HandleWaiterServiceStateChanged;
            _tutorialWaiter.OnLandingResolved += HandleWaiterLandingResolved;
        }

        private void UnsubscribeWaiter()
        {
            if (_tutorialWaiter == null)
                return;

            _tutorialWaiter.OnCustomerWasAsked -= HandleCustomerWasAsked;
            _tutorialWaiter.OnServiceStateChanged -= HandleWaiterServiceStateChanged;
            _tutorialWaiter.OnLandingResolved -= HandleWaiterLandingResolved;
            _tutorialWaiter = null;
        }

        private void HandleCustomerWasAsked(Customer customer)
        {
            ReportSignal(TutorialSignal.WaiterAskedCustomer);
        }

        private void HandleWaiterServiceStateChanged(WaiterServiceState previous, WaiterServiceState current)
        {
            if (current == WaiterServiceState.Delivering)
                ReportSignal(TutorialSignal.GaveMealToWaiter);
        }

        private void HandleWaiterLandingResolved(WaiterLandingResult result)
        {
            _landingResult = result;
            ReportSignal(TutorialSignal.WaiterLanded);
        }

        private void HandleCustomerServed(Customer customer)
        {
            if (customer != _tutorialCustomer)
                return;

            _customerServed = true;
            ReportSignal(TutorialSignal.CustomerServed);
        }

        private void CompleteTutorial()
        {
            PlayerPrefs.SetInt(_completionKey, 1);
            PlayerPrefs.Save();
            _shifts.SetNormalShiftStartLocked(false);
            _onTutorialCompleted.Invoke();
            _context.GameModes.TrySetGameMode("shift");
        }
    }
}
