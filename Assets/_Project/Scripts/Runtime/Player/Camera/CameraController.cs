using Game.Input;
using Game.Movement;
using Game.Services;
using UnityEngine;

namespace Game.Player
{
    public class CameraController : MonoBehaviour, IOrientation
    {
        [Header("References")]
        [SerializeField] private Transform _followTarget;
        [SerializeField] private Transform _targetTransform;

        [Header("Look")]
        [SerializeField] private Vector2 _lookSensitivity = new Vector2(1f, 1f);
        [SerializeField] private float _mouseSmoothing = 0.04f;
        [SerializeField] private Vector2 _angleLimits = new Vector2(-90f, 90f);
        [SerializeField] private bool _invertY;

        [Header("Cursor")]
        [SerializeField] private bool _lockMouseOnAwake = true;

        private IInputService _input;

        private float _yaw;
        private float _pitch;

        private Vector2 _smoothedMouseDelta;
        private Vector2 _mouseDeltaVelocity;

        private bool _mouseLocked;

        public Quaternion Rotation => RotationFull;
        public Quaternion RotationFlat => Quaternion.Euler(0f, _yaw, 0f);
        public Quaternion RotationFull => Quaternion.Euler(_pitch, _yaw, 0f);
        public Vector3 Euler => new Vector3(_pitch, _yaw, 0f);
        public float Yaw => _yaw;
        public float Pitch => _pitch;

        public Vector3 ForwardFlat => RotationFlat * Vector3.forward;
        public Vector3 RightFlat => RotationFlat * Vector3.right;

        public Vector3 Forward => RotationFull * Vector3.forward;
        public Vector3 Right => RotationFull * Vector3.right;

        public Quaternion ViewRotationFlat => RotationFlat;
        public Quaternion ViewRotationFull => RotationFull;
        public Vector3 ViewEuler => Euler;
        public float ViewYaw => Yaw;
        public float ViewPitch => Pitch;

        public Vector3 ViewForwardFlat => ForwardFlat;
        public Vector3 ViewRightFlat => RightFlat;

        public Vector3 ViewForward => Forward;
        public Vector3 ViewRight => Right;

        private void Awake()
        {
            _input = ServiceLocator.Get<IInputService>();

            if (_targetTransform == null)
                _targetTransform = transform;

            InitializeRotation();
            SetMouseLocked(_lockMouseOnAwake);
        }

        private void LateUpdate()
        {
            if (_input == null || _targetTransform == null)
                return;

            Vector2 mouseDelta = _input.MouseDelta;
            Vector2 lookDelta = GetSmoothedMouseDelta(mouseDelta);

            float minPitch = Mathf.Min(_angleLimits.x, _angleLimits.y);
            float maxPitch = Mathf.Max(_angleLimits.x, _angleLimits.y);

            _yaw += lookDelta.x * _lookSensitivity.x;
            _pitch += lookDelta.y * _lookSensitivity.y * (_invertY ? 1f : -1f);
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            if (_followTarget != null)
                _targetTransform.position = _followTarget.position;

            _targetTransform.rotation = RotationFull;
        }

        public void ResetRotation()
        {
            _yaw = 0f;
            _pitch = 0f;

            _smoothedMouseDelta = Vector2.zero;
            _mouseDeltaVelocity = Vector2.zero;

            if (_targetTransform != null)
                _targetTransform.rotation = RotationFull;
        }

        public Vector3 GetRelativeVelocity(Vector3 worldVelocity)
        {
            return Quaternion.Inverse(RotationFlat) * worldVelocity;
        }

        public void SetMouseLocked(bool locked = true)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
            _mouseLocked = locked;
        }

        public void ToggleMouseLocked()
        {
            SetMouseLocked(!_mouseLocked);
        }

        private void InitializeRotation()
        {
            if (_targetTransform == null)
                return;

            Vector3 euler = _targetTransform.rotation.eulerAngles;

            _yaw = NormalizeSignedAngle(euler.y);
            _pitch = NormalizeSignedAngle(euler.x);
        }

        private Vector2 GetSmoothedMouseDelta(Vector2 mouseDelta)
        {
            if (_mouseSmoothing <= 0f)
            {
                _smoothedMouseDelta = mouseDelta;
                _mouseDeltaVelocity = Vector2.zero;
                return _smoothedMouseDelta;
            }

            _smoothedMouseDelta = Vector2.SmoothDamp(
                _smoothedMouseDelta,
                mouseDelta,
                ref _mouseDeltaVelocity,
                _mouseSmoothing,
                Mathf.Infinity,
                Time.unscaledDeltaTime);

            return _smoothedMouseDelta;
        }

        private static float NormalizeSignedAngle(float angle)
        {
            angle %= 360f;

            if (angle > 180f)
                angle -= 360f;

            if (angle < -180f)
                angle += 360f;

            return angle;
        }
    }
}