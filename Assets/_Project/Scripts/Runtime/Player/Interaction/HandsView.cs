using UnityEngine;
using LitMotion;
using Game.Movement;

namespace Game.Interaction
{
    public class HandView : MonoBehaviour
    {
        private static readonly int ClickTriggerHash =
            Animator.StringToHash("Click");

        [Header("Dependencies")]
        [SerializeField] private HandsInteractor _handsInteractor;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private Animator _handAnimator;
        [SerializeField] private RectTransform _handTransform;

        [Header("Hand Settings")]
        [SerializeField] private HandType _handType;
        [SerializeField, Min(0f)] private float _hiddenDistance = 480f;
        [SerializeField, Min(0f)] private float _visibilitySmoothness = 8f;

        [Header("Movement Bob")]
        [Tooltip("Minimum player speed required before bobbing begins.")]
        [SerializeField, Min(0f)] private float _bobSpeedThreshold = 0.25f;

        [Tooltip("Maximum vertical distance of the bob in UI units.")]
        [SerializeField, Min(0f)] private float _bobAmplitude = 14f;

        [Tooltip("Number of complete bob cycles per second.")]
        [SerializeField, Min(0f)] private float _bobFrequency = 2.5f;

        [Tooltip("How quickly bobbing fades in and out.")]
        [SerializeField, Min(0f)] private float _bobBlendSpeed = 8f;

        [Tooltip("Smooths the measured movement speed.")]
        [SerializeField, Min(0f)] private float _speedSmoothness = 12f;

        [Tooltip("Gradually increase bob amplitude based on player speed.")]
        [SerializeField] private bool _scaleBobWithSpeed = true;

        [Tooltip("Player speed at which the bob reaches full amplitude.")]
        [SerializeField, Min(0.01f)] private float _speedForFullBob = 5f;

        [Header("Interaction")]
        [SerializeField, Min(0f)] private float _clickDistance = 320f;
        [SerializeField, Min(0f)] private float _clickDuration = 0.2f;

        private float HandSign =>
            _handType == HandType.Left ? 1f : -1f;

        private float BobPhaseOffset =>
            _handType == HandType.Right ? Mathf.PI : 0f;

        private Hand _hand;

        private Vector2 _originalPosition;
        private Vector2 _basePosition;
        private Vector2 _clickOffset;

        private Vector3 _previousPlayerPosition;
        private float _smoothedPlayerSpeed;
        private float _bobWeight;

        private MotionHandle _clickMotion;

        private void Start()
        {
            _hand = _handsInteractor.GetHand(_handType);

            _hand.OnInteracted += OnHandInteracted;
            _hand.OnSetVisible += OnHandSetVisible;

            _originalPosition = _handTransform.anchoredPosition;
            _basePosition = _originalPosition;

            _previousPlayerPosition = _playerController.transform.position;
        }

        private void OnEnable()
        {
            // Prevent a speed spike after the object has been disabled.
            _previousPlayerPosition = _playerController.transform.position;
        }

        private void OnDisable()
        {
            _hand.OnInteracted -= OnHandInteracted;
            _hand.OnSetVisible -= OnHandSetVisible;

            if (_clickMotion.IsActive())
            {
                _clickMotion.Cancel();
            }

            _clickOffset = Vector2.zero;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            if (deltaTime <= 0f)
            {
                return;
            }

            UpdatePlayerSpeed(deltaTime);
            UpdateBasePosition(deltaTime);
            UpdateBob(deltaTime);

            float phase =
                Time.time * _bobFrequency * Mathf.PI * 2f +
                BobPhaseOffset;

            float bobY =
                Mathf.Sin(phase) *
                _bobAmplitude *
                _bobWeight;

            Vector2 bobOffset = Vector2.up * bobY;

            _handTransform.anchoredPosition =
                _basePosition +
                _clickOffset +
                bobOffset;
        }

        private void UpdatePlayerSpeed(float deltaTime)
        {
            Vector3 playerPosition = _playerController.transform.position;

            float measuredSpeed =
                Vector3.Distance(playerPosition, _previousPlayerPosition) /
                deltaTime;

            _previousPlayerPosition = playerPosition;

            float smoothingFactor =
                1f - Mathf.Exp(-_speedSmoothness * deltaTime);

            _smoothedPlayerSpeed = Mathf.Lerp(
                _smoothedPlayerSpeed,
                measuredSpeed,
                smoothingFactor);
        }

        private void UpdateBasePosition(float deltaTime)
        {
            Vector2 hiddenPosition =
                _originalPosition + Vector2.down * _hiddenDistance;

            Vector2 targetPosition =
                _hand.Visible ? _originalPosition : hiddenPosition;

            float smoothingFactor =
                1f - Mathf.Exp(-_visibilitySmoothness * deltaTime);

            _basePosition = Vector2.Lerp(
                _basePosition,
                targetPosition,
                smoothingFactor);
        }

        private void UpdateBob(float deltaTime)
        {
            float targetWeight = 0f;

            if (_smoothedPlayerSpeed > _bobSpeedThreshold && _playerController.IsGrounded)
            {
                targetWeight = _scaleBobWithSpeed
                    ? Mathf.InverseLerp(
                        _bobSpeedThreshold,
                        Mathf.Max(
                            _speedForFullBob,
                            _bobSpeedThreshold + 0.01f),
                        _smoothedPlayerSpeed)
                    : 1f;
            }

            _bobWeight = Mathf.MoveTowards(
                _bobWeight,
                targetWeight,
                _bobBlendSpeed * deltaTime);
        }

        private void OnHandSetVisible(bool visible)
        {
            // Visibility is read from _hand.Visible in UpdateBasePosition().
            // The event remains subscribed in case additional effects are added.
        }

        [ContextMenu("Play Interact animation")]
        private void OnHandInteracted()
        {
            if (_clickMotion.IsActive())
            {
                _clickMotion.Cancel();
            }

            _clickOffset = Vector2.zero;

            Vector2 clickTarget =
                Vector2.right * HandSign * _clickDistance;

            _clickMotion = LMotion
                .Create(Vector2.zero, clickTarget, _clickDuration)
                .WithLoops(2, LoopType.Yoyo)
                .WithOnComplete(() => _clickOffset = Vector2.zero)
                .Bind(value => _clickOffset = value);

            _handAnimator.SetTrigger(ClickTriggerHash);
        }
    }
}