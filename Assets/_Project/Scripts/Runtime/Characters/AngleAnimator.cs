using UnityEngine;

namespace Game.Animation
{
    public sealed class AngleAnimator : MonoBehaviour
    {
        private static readonly int AngleHash = Animator.StringToHash("angle");

        [Header("Dependencies")]
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _source;

        [Header("Side Sprite")]
        [SerializeField] private bool _sideViewIsLeft = true;

        [Header("Angle Thresholds")]
        [Range(0f, 180f)]
        [SerializeField] private float _minSideThreshold = 45f;

        [Range(0f, 180f)]
        [SerializeField] private float _maxSideThreshold = 135f;

        [Header("Animator Values")]
        [SerializeField] private float _forwardValue = 0f;
        [SerializeField] private float _sideValue = 1f;
        [SerializeField] private float _backValue = 2f;

        private Transform _cameraTransform;

        private void Awake()
        {
            _cameraTransform = Camera.main.transform;
            _source ??= transform;
        }

        private void LateUpdate()
        {
            float angle = GetViewAngle();
            float absoluteAngle = Mathf.Abs(angle);

            if (absoluteAngle < _minSideThreshold)
                SetView(_forwardValue, false);
            else if (absoluteAngle < _maxSideThreshold)
                SetView(_sideValue, (angle < 0f) != _sideViewIsLeft);
            else
                SetView(_backValue, false);
        }

        private float GetViewAngle()
        {
            Vector3 forward = _source.forward;
            Vector3 directionToCamera = _cameraTransform.position - _source.position;

            forward.y = 0f;
            directionToCamera.y = 0f;

            return Vector3.SignedAngle(forward, directionToCamera, Vector3.up);
        }

        private void SetView(float value, bool flipX)
        {
            _animator.SetFloat(AngleHash, value);
            _spriteRenderer.flipX = flipX;
        }

        private void OnValidate()
        {
            _maxSideThreshold = Mathf.Max(
                _minSideThreshold,
                _maxSideThreshold);
        }
    }
}