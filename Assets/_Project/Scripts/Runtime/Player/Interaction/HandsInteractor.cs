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

    public class HandsInteractor : MonoBehaviour
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

        private IFocusInteractable _currentFocus;

        private void Awake()
        {
            _inputService = ServiceLocator.Get<IInputService>();
            _leftHand = new();
            _rightHand = new();
        }

        private void Update()
        {
            if (_inputService.Cancel.Pressed && _currentFocus != null)
            {
                _currentFocus.EndInteraction();
                _cameraController.TransitionToDefaultTarget(
                    new CameraTransiton(
                        _currentFocus.CameraTarget.Position,
                        _currentFocus.CameraTarget.Rotation,
                        _currentFocus.CameraTarget.Fov,
                        _currentFocus.ResetTransitionDuration, 
                        Time.time));
                _currentFocus = null;
                _rightHand.SetVisible(true);
                _leftHand.SetVisible(true);
                _cameraController.SetMouseLocked(true);
            }

            if (_inputService.InteractLeft.Pressed)
                CauseInteraction(_leftHand);
            if (_inputService.InteractRight.Pressed)
                CauseInteraction(_rightHand);
        }

        private void CauseInteraction(Hand hand)
        {
            if (_currentFocus != null)
                return;

            hand.Interact();
            if (Physics.Raycast(_lookDirection.position, _lookDirection.forward, out var hit, _interactionDistance, _interactionLayer))
            {
                if (hit.transform.TryGetComponent<IInteractable>(out var interactable))
                {
                    interactable.Interact();
                }

                if (hit.transform.TryGetComponent<IFocusInteractable>(out var focusable))
                {
                    focusable.BeginInteraction();
                    _cameraController.SetCameraTarget(focusable.CameraTarget);
                    _currentFocus = focusable;
                    _rightHand.SetVisible(false);
                    _leftHand.SetVisible(false);
                    _cameraController.SetMouseLocked(false);
                }
            }
        }

        public Hand GetHand(HandType handType)
            => handType == HandType.Left ? _leftHand : _rightHand;
    }
}