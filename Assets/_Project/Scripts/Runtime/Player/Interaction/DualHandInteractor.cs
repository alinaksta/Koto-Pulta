using Game.Input;
using Game.Items;
using Game.Player;
using Game.Services;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Identifies one of the player's hands.
    /// </summary>
    public enum HandType
    {
        Left,
        Right
    }

    /// <summary>
    /// Routes player input into hand interactions and focus requests.
    /// </summary>
    public class DualHandInteractor : MonoBehaviour, IFocusHandler, IDualHandInteractor
    {
        [SerializeField] private CameraController _cameraController;
        [SerializeField] private Transform _lookDirection;
        [SerializeField] private float _interactionDistance = 1.4f;
        [SerializeField] private LayerMask _interactionLayer;
        [SerializeField] private Transform _leftSpawnPosition;
        [SerializeField] private Transform _rightSpawnPosition;

        private IInputService _inputService;

        private Hand _leftHand;
        private Hand _rightHand;

        private GameObject _hoveredObject;
        private IHoverable _hovered;
        private RaycastHit? _hoverHit;

        private IInteractable _leftInteraction;
        private IInteractable _rightInteraction;
        private RaycastHit? _leftInteractionHit;
        private RaycastHit? _rightInteractionHit;

        /// <summary>
        /// Gets the left hand controller.
        /// </summary>
        public Hand LeftHand => _leftHand;

        /// <summary>
        /// Gets the right hand controller.
        /// </summary>
        public Hand RightHand => _rightHand;

        /// <inheritdoc/>
        public GameObject HoveredObject => _hoveredObject;

        /// <summary>
        /// Gets the current world-space look direction.
        /// </summary>
        public Vector3 LookForward => _lookDirection != null ? _lookDirection.forward : transform.forward;

        /// <summary>
        /// Gets the current world-space look origin.
        /// </summary>
        public Vector3 LookPosition => _lookDirection != null ? _lookDirection.position : transform.position;


        /// <inheritdoc/>
        public FocusStatus FocusStatus => _cameraController.FocusStatus;

        /// <inheritdoc/>
        public IFocusable FocusedObject => _cameraController.FocusedObject;

        private void Awake()
        {
            _inputService = ServiceLocator.Get<IInputService>();
            var physicsItemService = ServiceLocator.Get<PhysicsItemService>();
            _leftHand = new(physicsItemService, _leftSpawnPosition);
            _rightHand = new(physicsItemService, _rightSpawnPosition);
        }

        private void Update()
        {
            if (_inputService.Cancel.Pressed && FocusStatus != FocusStatus.Unfocused)
            {
                EndFocus();
                _cameraController.SetMouseLocked(true);
            }

            bool visible = FocusStatus == FocusStatus.Unfocused;
            _rightHand.SetVisible(visible);
            _leftHand.SetVisible(visible);

            if (visible)
                UpdateHover(Time.deltaTime);
            else
                ClearHover();

            if (_inputService.DropLeft.Pressed)
                _leftHand.TryDropItem(Vector3.zero);
            if (_inputService.DropRight.Pressed)
                _rightHand.TryDropItem(Vector3.zero);

            if (_inputService.InteractLeft.Pressed)
                CauseInteraction(_leftHand);
            else if (_inputService.InteractLeft.Held)
                HoldInteraction(_leftHand, Time.deltaTime);
            else if (_inputService.InteractLeft.Released)
                EndInteraction(_leftHand);

            if (_inputService.InteractRight.Pressed)
                CauseInteraction(_rightHand);
            else if (_inputService.InteractRight.Held)
                HoldInteraction(_rightHand, Time.deltaTime);
            else if (_inputService.InteractRight.Released)
                EndInteraction(_rightHand);
        }

        private void CauseInteraction(Hand hand)
        {
            if (FocusStatus != FocusStatus.Unfocused)
                return;

            var context = new InteractionContext(
                LookPosition,
                LookForward,
                this,
                hand,
                this);

            if (hand.TryStartInteractionWithItemInHand(in context))
                return;

            if (!_hoverHit.HasValue)
                return;

            var hit = _hoverHit.Value;
            var contextWithHit = context.WithHitInfo(in hit);
            hand.OnInteractionStarted(in contextWithHit);

            if (!hit.collider.TryGetComponent<IInteractable>(out var interactable) ||
                !interactable.CanInteract(in contextWithHit))
                return;

            interactable.OnInteractionStarted(in contextWithHit);
            SetActiveInteraction(hand, interactable, in hit);
        }

        private void HoldInteraction(Hand hand, float delta)
        {
            if (FocusStatus != FocusStatus.Unfocused)
                return;

            var context = new InteractionContext(
                LookPosition,
                LookForward,
                this,
                hand,
                this);

            if (hand.TryHoldInteractionWithItemInHand(in context, delta))
                return;

            if (TryGetActiveInteraction(hand, out var interactable, out var hit))
            {
                var contextWithHit = context.WithHitInfo(in hit);
                interactable.OnInteractionHeld(in contextWithHit, delta);
            }
        }

        private void EndInteraction(Hand hand)
        {
            var context = new InteractionContext(LookPosition,
                LookForward,
                this,
                hand,
                this);

            hand.TryEndInteractionWithItemInHand(in context);

            if (TryGetActiveInteraction(hand, out var interactable, out var hit))
            {
                var contextWithHit = context.WithHitInfo(in hit);
                interactable.OnInteractionStopped(in contextWithHit);
                ClearActiveInteraction(hand);
            }
        }

        private void UpdateHover(float delta)
        {
            GameObject nextObject = null;
            IHoverable nextHoverable = null;
            RaycastHit? nextHit = null;

            if (Physics.Raycast(LookPosition, LookForward, out var hit, _interactionDistance, _interactionLayer))
            {
                nextObject = hit.collider.gameObject;
                nextObject.TryGetComponent(out nextHoverable);
                nextHit = hit;
            }

            if (nextObject != _hoveredObject)
            {
                _hovered?.OnHoverExit();
                _hoveredObject = nextObject;
                _hovered = nextHoverable;
                _hovered?.OnHoverEnter();
            }

            _hoverHit = nextHit;
            _hovered?.OnHoverStay(delta);
        }

        private void ClearHover()
        {
            _hovered?.OnHoverExit();
            _hoveredObject = null;
            _hovered = null;
            _hoverHit = null;
        }

        private void SetActiveInteraction(Hand hand, IInteractable interactable, in RaycastHit hit)
        {
            if (hand == _leftHand)
            {
                _leftInteraction = interactable;
                _leftInteractionHit = hit;
            }
            else
            {
                _rightInteraction = interactable;
                _rightInteractionHit = hit;
            }
        }

        private bool TryGetActiveInteraction(Hand hand, out IInteractable interactable, out RaycastHit hit)
        {
            interactable = hand == _leftHand ? _leftInteraction : _rightInteraction;
            var interactionHit = hand == _leftHand ? _leftInteractionHit : _rightInteractionHit;
            hit = interactionHit.GetValueOrDefault();
            return interactable != null && interactionHit.HasValue;
        }

        private void ClearActiveInteraction(Hand hand)
        {
            if (hand == _leftHand)
            {
                _leftInteraction = null;
                _leftInteractionHit = null;
            }
            else
            {
                _rightInteraction = null;
                _rightInteractionHit = null;
            }
        }

        private void OnDisable() => ClearHover();

        /// <inheritdoc/>
        public Hand GetHand(HandType handType)
            => handType == HandType.Left ? _leftHand : _rightHand;

        /// <inheritdoc/>
        public bool TryBeginFocus(IFocusable focusable) => _cameraController.TryBeginFocus(focusable);

        /// <inheritdoc/>
        public void EndFocus() => _cameraController.EndFocus();

        /// <inheritdoc/>
        public void SetMouseLocked(bool locked) => _cameraController.SetMouseLocked(locked);

        /// <inheritdoc/>
        public void ClearMouseLocked() => _cameraController.ClearMouseLocked();

        /// <inheritdoc/>
        /// <remarks>
        /// The left hand is preferred when both hands are empty.
        /// </remarks>
        public bool TryGetFreeHand(out IContainer freeHand)
        {
            freeHand = _leftHand.IsEmpty ? _leftHand : _rightHand; // Set to left hand if free, right hand otherwise
            freeHand = freeHand.IsEmpty ? freeHand : null; // If the selected hand is empty, set to null

            return freeHand != null;
        }
    }
}
