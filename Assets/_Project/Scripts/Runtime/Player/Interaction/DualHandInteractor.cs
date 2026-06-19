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

        /// <summary>
        /// Gets the left hand controller.
        /// </summary>
        public Hand LeftHand => _leftHand;

        /// <summary>
        /// Gets the right hand controller.
        /// </summary>
        public Hand RightHand => _rightHand;


        /// <inheritdoc/>
        public FocusStatus FocusStatus => _cameraController.FocusStatus;

        /// <inheritdoc/>
        public IFocusable FocusedObject => _cameraController.FocusedObject;

        private void Awake()
        {
            _inputService = ServiceLocator.Get<IInputService>();
            var physicsItemService = ServiceLocator.Get<PhysicsItemService>();
            _leftHand = new(physicsItemService);
            _rightHand = new(physicsItemService);
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

            if (_inputService.DropLeft.Pressed)
                _leftHand.TryDropItem(_leftSpawnPosition.position, Vector3.zero);
            if (_inputService.DropRight.Pressed)
                _rightHand.TryDropItem(_rightSpawnPosition.position, Vector3.zero);

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
                _lookDirection.position, 
                _lookDirection.forward, 
                this, 
                hand,
                this);

            if (hand.TryStartInteractionWithItemInHand(in context))
                return;

            if (Physics.Raycast(_lookDirection.position, _lookDirection.forward, out var hit, _interactionDistance, _interactionLayer))
            {
                Debug.Log($"Has hit object named {hit.collider.gameObject.name}");
                var contextWithHit = context.WithHitInfo(in hit);
                hand.OnInteractionStarted(in contextWithHit);
                if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
                {
                    if (interactable.CanInteract(in contextWithHit))
                        interactable.OnInteractionStarted(in contextWithHit);
                }
            }
        }

        private void HoldInteraction(Hand hand, float delta)
        {
            if (FocusStatus != FocusStatus.Unfocused)
                return;

            var context = new InteractionContext(
                _lookDirection.position, 
                _lookDirection.forward, 
                this, 
                hand, 
                this);

            if (hand.TryHoldInteractionWithItemInHand(in context, delta))
                return;
        }

        private void EndInteraction(Hand hand)
        {
            if (FocusStatus != FocusStatus.Unfocused)
                return;

            var context = new InteractionContext(_lookDirection.position, 
                _lookDirection.forward, 
                this, 
                hand, 
                this);

            if (hand.TryEndInteractionWithItemInHand(in context))
                return;
        }

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
