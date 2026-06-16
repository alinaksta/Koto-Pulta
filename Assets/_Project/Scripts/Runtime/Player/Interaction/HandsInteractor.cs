using Game.Input;
using Game.Items;
using Game.Player;
using Game.Services;
using System;
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
        [SerializeField] private Transform _leftSpawnPosition;
        [SerializeField] private Transform _rightSpawnPosition;

        private IInputService _inputService;
        private PhysicsItemService _physicsItemService;

        private Hand _leftHand;
        private Hand _rightHand;

        public Hand LeftHand => _leftHand;
        public Hand RightHand => _rightHand;


        public FocusStatus FocusStatus => _cameraController.FocusStatus;
        public IFocusable FocusedObject => _cameraController.FocusedObject;

        private void Awake()
        {
            _inputService = ServiceLocator.Get<IInputService>();
            _physicsItemService = ServiceLocator.Get<PhysicsItemService>();
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

            var context = new InteractionContext(_lookDirection.position, _lookDirection.forward, this, hand);

            if (hand.TryStartInteractionWithItemInHand(in context))
                return;

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

        private void HoldInteraction(Hand hand, float delta)
        {
            if (FocusStatus != FocusStatus.Unfocused)
                return;

            var context = new InteractionContext(_lookDirection.position, _lookDirection.forward, this, hand);

            if (hand.TryHoldInteractionWithItemInHand(in context, delta))
                return;
        }

        private void EndInteraction(Hand hand)
        {
            if (FocusStatus != FocusStatus.Unfocused)
                return;

            var context = new InteractionContext(_lookDirection.position, _lookDirection.forward, this, hand);

            if (hand.TryEndInteractionWithItemInHand(in context))
                return;
        }

        public Hand GetHand(HandType handType)
            => handType == HandType.Left ? _leftHand : _rightHand;

        public bool TryBeginFocus(IFocusable focusable) => _cameraController.TryBeginFocus(focusable);
        public void EndFocus() => _cameraController.EndFocus();

        public void SetMouseLocked(bool locked) => _cameraController.SetMouseLocked(locked);
        public void ClearMouseLocked() => _cameraController.ClearMouseLocked();
    }
}