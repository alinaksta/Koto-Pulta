using UnityEngine;

namespace Game.Movement
{
    /// <summary>
    /// Tracks yaw and pitch from a source transform.
    /// </summary>
    public class CharacterOrientation : MonoBehaviour, IOrientation
    {
        [SerializeField] private Transform _source;
        
        private float _yaw;
        private float _pitch;

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

        private void Update()
        {
            ApplyCameraRotation(_source.rotation);
        }

        /// <inheritdoc/>
        public void ResetRotation()
        {
            _yaw = 0f;
            _pitch = 0f;
        }

        /// <inheritdoc/>
        public Vector3 GetRelativeVelocity(Vector3 worldVelocity)
        {
            return Quaternion.Inverse(RotationFlat) * worldVelocity;
        }

        private void ApplyCameraRotation(Quaternion cameraRotation)
        {
            Vector3 cameraEuler = cameraRotation.eulerAngles;

            _yaw = NormalizeSignedAngle(cameraEuler.y);
            _pitch = NormalizeSignedAngle(cameraEuler.x);
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
