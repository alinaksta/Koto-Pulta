using Game.Characters;
using Game.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Interaction
{
    /// <summary>
    /// Displays a held waiter with layered sprites and camera-driven secondary motion.
    /// </summary>
    public class WaiterHeldView : MonoBehaviour
    {
        [System.Serializable]
        private struct MotionSettings
        {
            public bool Invert;
            public float RotationFromYaw;
            public float RotationFromPitch;
            public Vector2 OffsetFromYaw;
            public Vector2 OffsetFromPitch;
            [Min(0f)] public float MaxRotation;
            [Min(0f)] public float MaxOffset;
            [Min(0f)] public float Smoothness;
            public bool Overshoot;
        }

        [System.Serializable]
        private struct PartBinding
        {
            public RectTransform Target;
            public MotionSettings Motion;
        }

        private sealed class RuntimePart
        {
            public RuntimePart(RectTransform rectTransform)
            {
                RectTransform = rectTransform;
                RestPosition = rectTransform.anchoredPosition;
                RestRotation = rectTransform.localRotation;
            }

            /// <summary>
            /// Gets the rect transform that drives the waiter-held visual.
            /// </summary>
            public RectTransform RectTransform { get; }
            /// <summary>
            /// Gets the default anchored position for the waiter-held visual.
            /// </summary>
            public Vector2 RestPosition { get; }
            /// <summary>
            /// Gets the default rotation for the waiter-held visual.
            /// </summary>
            public Quaternion RestRotation { get; }
            public float Rotation;
            public Vector2 Offset;
            public float RotationVelocity;
            public Vector2 OffsetVelocity;
        }

        [Header("References")]
        [SerializeField] private CameraController _cameraController;
        [SerializeField] private Image _noteImage;
        [SerializeField] private TextMeshProUGUI _tableNumberLabel;

        [Header("Parts")]
        [SerializeField] private PartBinding _head = new PartBinding
        {
            Motion = new MotionSettings
            {
                RotationFromYaw = 0.03f,
                RotationFromPitch = -0.01f,
                OffsetFromYaw = new Vector2(-0.03f, 0f),
                OffsetFromPitch = new Vector2(0f, 0.03f),
                MaxRotation = 12f,
                MaxOffset = 12f,
                Smoothness = 16f,
                Overshoot = false
            }
        };
        [SerializeField] private PartBinding _chest = new PartBinding
        {
            Motion = new MotionSettings
            {
                RotationFromYaw = -0.04f,
                RotationFromPitch = 0.015f,
                OffsetFromYaw = new Vector2(0.045f, 0f),
                OffsetFromPitch = new Vector2(0f, -0.02f),
                MaxRotation = 16f,
                MaxOffset = 18f,
                Smoothness = 12f,
                Overshoot = false
            }
        };
        [SerializeField] private PartBinding _legs = new PartBinding
        {
            Motion = new MotionSettings
            {
                RotationFromYaw = -0.05f,
                RotationFromPitch = 0.02f,
                OffsetFromYaw = new Vector2(0.05f, 0f),
                OffsetFromPitch = new Vector2(0f, -0.03f),
                MaxRotation = 18f,
                MaxOffset = 22f,
                Smoothness = 10f,
                Overshoot = false
            }
        };
        [SerializeField] private PartBinding _tail = new PartBinding
        {
            Motion = new MotionSettings
            {
                RotationFromYaw = -0.07f,
                RotationFromPitch = 0.03f,
                OffsetFromYaw = new Vector2(0.08f, 0f),
                OffsetFromPitch = new Vector2(0f, -0.04f),
                MaxRotation = 24f,
                MaxOffset = 28f,
                Smoothness = 8f,
                Overshoot = true
            }
        };

        private readonly PartBinding[] _partBindings = new PartBinding[4];
        private RuntimePart[] _parts;
        private Waiter _waiter;
        private int _displayedTableNumber = int.MinValue;
        private bool _noteVisible;
        private Vector2 _baseOffset;
        private Vector2 _noteRestPosition;

        private void Awake()
        {
            CacheParts();
            if (_noteImage != null)
                _noteRestPosition = _noteImage.rectTransform.anchoredPosition;

            _noteVisible = (_noteImage != null && _noteImage.gameObject.activeSelf) ||
                           (_tableNumberLabel != null && _tableNumberLabel.gameObject.activeSelf);
            HideNote();
            SetPartsVisible(false);
        }

        /// <summary>
        /// Sets the shared waiter-held offset applied to all waiter visuals.
        /// </summary>
        public void SetBaseOffset(Vector2 baseOffset)
        {
            _baseOffset = baseOffset;
            ResetMotion();
        }

        /// <summary>
        /// Sets the waiter shown in hand.
        /// </summary>
        public void SetWaiter(Waiter waiter)
        {
            _waiter = waiter;

            bool isVisible = _waiter != null;
            SetPartsVisible(isVisible);

            if (!isVisible)
            {
                ResetMotion();
                HideNote();
                return;
            }

            UpdateNote();
        }

        private void LateUpdate()
        {
            if (_waiter == null)
                return;

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f || deltaTime > 2f)
                return;

            UpdateNote();
            UpdateMotion(deltaTime);
        }

        private void CacheParts()
        {
            _partBindings[0] = _tail;
            _partBindings[1] = _legs;
            _partBindings[2] = _chest;
            _partBindings[3] = _head;

            _parts = new RuntimePart[_partBindings.Length];

            for (int i = 0; i < _partBindings.Length; i++)
            {
                RectTransform target = _partBindings[i].Target;
                if (target == null)
                    continue;

                _parts[i] = new RuntimePart(target);
            }
        }

        private void UpdateMotion(float deltaTime)
        {
            Vector2 angularVelocity = _cameraController != null
                ? _cameraController.AngularVelocity
                : Vector2.zero;

            for (int i = 0; i < _parts.Length; i++)
            {
                RuntimePart part = _parts[i];
                if (part == null)
                    continue;

                UpdatePart(part, _partBindings[i].Motion, deltaTime, angularVelocity);
            }
        }

        private void UpdatePart(RuntimePart part, MotionSettings motion, float deltaTime, Vector2 angularVelocity)
        {
            float direction = motion.Invert ? -1f : 1f;
            float targetRotation = angularVelocity.x * motion.RotationFromYaw + angularVelocity.y * motion.RotationFromPitch;
            targetRotation *= direction;
            targetRotation = Mathf.Clamp(targetRotation, -motion.MaxRotation, motion.MaxRotation);

            Vector2 targetOffset = angularVelocity.x * motion.OffsetFromYaw + angularVelocity.y * motion.OffsetFromPitch;
            targetOffset *= direction;
            targetOffset = Vector2.ClampMagnitude(targetOffset, motion.MaxOffset);

            if (motion.Overshoot)
            {
                float smoothTime = 1f / Mathf.Max(0.01f, motion.Smoothness);
                part.Rotation = Mathf.SmoothDampAngle(part.Rotation, targetRotation, ref part.RotationVelocity, smoothTime, Mathf.Infinity, deltaTime);
                part.Offset = Vector2.SmoothDamp(part.Offset, targetOffset, ref part.OffsetVelocity, smoothTime, Mathf.Infinity, deltaTime);
            }
            else
            {
                float blend = 1f - Mathf.Exp(-motion.Smoothness * deltaTime);
                part.Rotation = Mathf.Lerp(part.Rotation, targetRotation, blend);
                part.Offset = Vector2.Lerp(part.Offset, targetOffset, blend);
                part.RotationVelocity = 0f;
                part.OffsetVelocity = Vector2.zero;
            }
            
            if(float.IsNaN(part.Offset.x) || float.IsNaN(part.Offset.y)) part.Offset = Vector2.zero;
            if(float.IsNaN(part.Rotation)) part.Rotation = 0f;

            part.RectTransform.anchoredPosition = part.RestPosition + _baseOffset + part.Offset;
            part.RectTransform.localRotation = part.RestRotation * Quaternion.Euler(0f, 0f, part.Rotation);
        }

        private void ResetMotion()
        {
            if (_parts == null)
                return;

            for (int i = 0; i < _parts.Length; i++)
            {
                RuntimePart part = _parts[i];
                if (part == null)
                    continue;

                part.Rotation = 0f;
                part.RotationVelocity = 0f;
                part.Offset = Vector2.zero;
                part.OffsetVelocity = Vector2.zero;
                part.RectTransform.anchoredPosition = part.RestPosition + _baseOffset;
                part.RectTransform.localRotation = part.RestRotation;
            }

            if (_noteImage != null)
                _noteImage.rectTransform.anchoredPosition = _noteRestPosition + _baseOffset;
        }

        private void SetPartsVisible(bool isVisible)
        {
            if (_parts == null)
                return;

            for (int i = 0; i < _parts.Length; i++)
            {
                RuntimePart part = _parts[i];
                if (part == null)
                    continue;

                part.RectTransform.gameObject.SetActive(isVisible);
            }
        }

        private void UpdateNote()
        {
            if (!ShouldShowNote())
            {
                HideNote();
                return;
            }

            bool wasVisible = _noteVisible;
            if (!wasVisible && _noteImage != null)
            {
                _noteImage.rectTransform.anchoredPosition = _noteRestPosition + _baseOffset;
                _noteImage.gameObject.SetActive(true);
            }

            _noteVisible = true;

            if (_tableNumberLabel == null)
                return;

            if (!_tableNumberLabel.gameObject.activeSelf)
                _tableNumberLabel.gameObject.SetActive(true);

            int tableNumber = _waiter.AssignedCustomer.Table.TableNumber;
            if (tableNumber == _displayedTableNumber)
                return;

            _tableNumberLabel.SetText("{0}", tableNumber);
            _displayedTableNumber = tableNumber;
        }

        private bool ShouldShowNote()
            => _waiter != null &&
               _waiter.IsAssigned &&
               _waiter.AssignedCustomer != null &&
               _waiter.ServiceState != WaiterServiceState.Unassigned &&
               _waiter.ServiceState != WaiterServiceState.AskingCustomer;

        private void HideNote()
        {
            if (!_noteVisible)
                return;

            if (_noteImage != null)
                _noteImage.gameObject.SetActive(false);

            if (_tableNumberLabel != null)
            {
                _tableNumberLabel.gameObject.SetActive(false);
                _tableNumberLabel.text = string.Empty;
            }

            _displayedTableNumber = int.MinValue;
            _noteVisible = false;
        }
    }
}
