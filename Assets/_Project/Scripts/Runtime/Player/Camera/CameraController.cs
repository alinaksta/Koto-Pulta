using Game.Input;
using Game.Interaction;
using Game.Movement;
using Game.Services;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Drives free look and camera focus transitions.
    /// </summary>
    public class CameraController : MonoBehaviour, IOrientation, IFocusHandler
    {
        [Header("References")]
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _target;
        [SerializeField] private Transform _head;

        [Header("Look")]
        [SerializeField] private Vector2 _lookSensitivity = new Vector2(1f, 1f);
        [SerializeField] private float _mouseSmoothing = 0.04f;
        [SerializeField] private Vector2 _angleLimits = new Vector2(-90f, 90f);
        [SerializeField] private bool _invertY;

        [Header("Position")]
        [SerializeField] private float _headPositionLerp = 20f;

        [Header("Cursor")]
        [SerializeField] private bool _lockMouseOnAwake = true;

        private IInputService _input;
        private static bool _stop = false;

        private IFocusable _focusedObject;
        private FocusTransition? _focusTransition;

        private float _yaw;
        private float _pitch;
        private Vector2 _angularVelocity;

        private Vector2 _smoothedMouseDelta;
        private Vector2 _mouseDeltaVelocity;

        private static bool _mouseLocked;

        /// <inheritdoc/>
        public Quaternion RotationFlat => Quaternion.Euler(0f, _yaw, 0f);

        /// <inheritdoc/>
        public Quaternion RotationFull => Quaternion.Euler(_pitch, _yaw, 0f);

        /// <inheritdoc/>
        public Vector3 Euler => new Vector3(_pitch, _yaw, 0f);

        /// <inheritdoc/>
        public float Yaw => _yaw;

        /// <inheritdoc/>
        public float Pitch => _pitch;

        /// <inheritdoc/>
        public Vector3 ForwardFlat => RotationFlat * Vector3.forward;

        /// <inheritdoc/>
        public Vector3 RightFlat => RotationFlat * Vector3.right;

        /// <inheritdoc/>
        public Vector3 Forward => RotationFull * Vector3.forward;

        /// <inheritdoc/>
        public Vector3 Right => RotationFull * Vector3.right;

        /// <summary>
        /// Gets the current angular velocity in degrees per second.
        /// </summary>
        /// <remarks>
        /// X stores yaw velocity and Y stores pitch velocity.
        /// </remarks>
        public Vector2 AngularVelocity => _angularVelocity;

        /// <inheritdoc/>
        public FocusStatus FocusStatus
        {
            get
            {
                if (_focusTransition.HasValue)
                    return FocusStatus.InTransition;
                else if (_focusedObject != null)
                    return FocusStatus.Focused;
                else
                    return FocusStatus.Unfocused;
            }
        }

        /// <inheritdoc/>
        public IFocusable FocusedObject => _focusedObject;

        private void Awake()
        {
            _input = ServiceLocator.Get<IInputService>();

            if (_target == null)
                _target = transform;

            InitializeRotation();
            SetMouseLocked(_lockMouseOnAwake);
        }

        private void LateUpdate()
        {
            if (_input == null || _target == null || _stop == true)
                return;

            Vector2 mouseDelta = _input.MouseDelta;
            Vector2 lookDelta = GetSmoothedMouseDelta(mouseDelta);

            float minPitch = Mathf.Min(_angleLimits.x, _angleLimits.y);
            float maxPitch = Mathf.Max(_angleLimits.x, _angleLimits.y);

            float previousYaw = _yaw;
            float previousPitch = _pitch;

            HandleFocusTransitionTime();

            if (FocusStatus == FocusStatus.Unfocused)
            {
                UpdateLookRotation(lookDelta, minPitch, maxPitch);
            }
            else if (FocusStatus == FocusStatus.Focused)
            {
                UpdateFocusedRotation();
            }
            else
            {
                UpdateTransitionRotation();
            }

            if (FocusStatus == FocusStatus.Focused)
            {
                _focusedObject.OnFocusHeld(Time.deltaTime);
            }

            UpdateAngularVelocity(previousYaw, previousPitch);
        }

        private void UpdateAngularVelocity(float previousYaw, float previousPitch)
        {
            float yawVelocity = Mathf.DeltaAngle(_yaw, previousYaw) / Time.deltaTime;
            float pitchVelocity = Mathf.DeltaAngle(_pitch, previousPitch) / Time.deltaTime;
            _angularVelocity = new Vector2(yawVelocity, pitchVelocity);
        }

        private void UpdateTransitionRotation()
        {
            var transition = _focusTransition.Value;
            float t = Mathf.InverseLerp(transition.StartTime, transition.StartTime + transition.Duration, Time.time);
            t = Mathf.Clamp01(t);

            _target.position = Vector3.Lerp(transition.From.Position, transition.To.Position, t);
            _target.rotation = Quaternion.Slerp(transition.From.Rotation, transition.To.Rotation, t);
            _camera.fieldOfView = Mathf.Lerp(transition.From.Fov, transition.To.Fov, t);
        }

        private void UpdateFocusedRotation()
        {
            FocusTarget target = _focusedObject.Target;

            _target.position = Vector3.Lerp(_target.position, target.Position, target.PositionLerp * Time.deltaTime);

            if (target.ApplyRotation)
                _target.rotation = Quaternion.Slerp(_target.rotation, target.Rotation, target.RotationLerp * Time.deltaTime);
            else
                _target.rotation = RotationFull;

            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, target.Fov, target.FovLerp * Time.deltaTime);
        }

        private void UpdateLookRotation(Vector2 lookDelta, float minPitch, float maxPitch)
        {
            _yaw += lookDelta.x * _lookSensitivity.x;
            _pitch += lookDelta.y * _lookSensitivity.y * (_invertY ? 1f : -1f);
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            _target.position = Vector3.Lerp(_target.position, _head.position, _headPositionLerp * Time.deltaTime);
            _target.rotation = RotationFull;
        }

        private void HandleFocusTransitionTime()
        {
            if (_focusTransition.HasValue && Time.time >= _focusTransition.Value.EndTime)
                _focusTransition = null;
        }

        /// <inheritdoc/>
        public void ResetRotation()
        {
            _yaw = 0f;
            _pitch = 0f;

            _smoothedMouseDelta = Vector2.zero;
            _mouseDeltaVelocity = Vector2.zero;

            if (_target != null)
                _target.rotation = RotationFull;
        }

        /// <inheritdoc/>
        public Vector3 GetRelativeVelocity(Vector3 worldVelocity)
        {
            return Quaternion.Inverse(RotationFlat) * worldVelocity;
        }

        /// <inheritdoc/>
        public static void SetMouseLockedStatic(bool locked = true)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
            _mouseLocked = locked;
        }
        public void SetMouseLocked(bool locked = true)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
            _mouseLocked = locked;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This implementation always restores the cursor to the locked state.
        /// </remarks>
        public void ClearMouseLocked()
        {
            SetMouseLocked(true);
        }

        /// <summary>
        /// Toggles between locked and unlocked cursor states.
        /// </summary>
        public void ToggleMouseLocked()
        {
            SetMouseLocked(!_mouseLocked);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Focus can only begin while the camera is fully unfocused.
        /// </remarks>
        public bool TryBeginFocus(IFocusable focusable)
        {
            if (FocusStatus == FocusStatus.Unfocused)
            {
                _focusedObject = focusable;
                focusable.OnFocusStarted();

                var transition = new FocusTransition(
                    GetCurrentCameraSnapshot(),
                    focusable.Target.ToSnapshot(),
                    focusable.StartFocusTransitionDuration,
                    Time.time);

                _focusTransition = transition;

                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public void EndFocus()
        {
            if (_focusedObject == null)
                return;

            IFocusable focusable = _focusedObject;
            focusable.OnFocusEnded();
            _focusedObject = null;

            var transition = new FocusTransition(
                    focusable.Target.ToSnapshot(),
                    GetCurrentCameraSnapshot(),
                    focusable.EndFocusTransitionDuration,
                    Time.time);

            _focusTransition = transition;
        }

        private CameraSnapshot GetCurrentCameraSnapshot()
        {
            return new CameraSnapshot(_head.position, RotationFull, 90f);
        }

        private void InitializeRotation()
        {
            if (_target == null)
                return;

            Vector3 euler = _target.rotation.eulerAngles;

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
        public static void SetActiveRotationStatic(bool stop)
        {
            _stop = stop;
        }
    }
}
