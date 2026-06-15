using Game.Input;
using Game.Player;
using Game.Services;
using UnityEngine;

namespace Game.Interaction
{
    public enum HandType
    {
        Left,
        Right
    }

    public class HandsInteractor : MonoBehaviour, IFocusHandler
    {
        [SerializeField] private CameraController _cameraController;
        [SerializeField] private Transform _lookDirection;
        [SerializeField] private float _interactionDistance = 1.4f;
        [SerializeField] private LayerMask _interactionLayer;
        private IInputService _inputService;

        private Hand _leftHand;
        private Hand _rightHand;

        public Hand LeftHand => _leftHand;
        public Hand RightHand => _rightHand;

        public FocusStatus FocusStatus => _cameraController.FocusStatus;
        public IFocusable FocusedObject => _cameraController.FocusedObject;

        private void Awake()
        {
            _inputService = ServiceLocator.Get<IInputService>();
            _leftHand = new();
            _rightHand = new();
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

            if (_inputService.InteractLeft.Pressed)
                CauseInteraction(_leftHand);
            if (_inputService.InteractRight.Pressed)
                CauseInteraction(_rightHand);
        }

        private void CauseInteraction(Hand hand)
        {
            if (FocusStatus != FocusStatus.Unfocused)
                return;

            var context = new InteractionContext(_lookDirection.position, _lookDirection.forward, this, hand);

            if (Physics.Raycast(_lookDirection.position, _lookDirection.forward, out var hit, _interactionDistance, _interactionLayer))
            {
                var contextWithHit = context.WithHitInfo(in hit);
                hand.OnInteractionStarted(in contextWithHit);
                if (hit.transform.TryGetComponent<IInteractable>(out var interactable))
                {
                    if (interactable.CanInteract(in contextWithHit))
                        interactable.OnInteractionStarted(in contextWithHit);
                }
            }
        }

        public Hand GetHand(HandType handType)
            => handType == HandType.Left ? _leftHand : _rightHand;

        public bool TryBeginFocus(IFocusable focusable) => _cameraController.TryBeginFocus(focusable);
        public void EndFocus() => _cameraController.EndFocus();

        public void SetMouseLocked(bool locked) => _cameraController.SetMouseLocked(locked);
        public void ClearMouseLocked() => _cameraController.ClearMouseLocked();
    }
}