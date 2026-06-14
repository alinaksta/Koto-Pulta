using UnityEngine;

namespace Game.Movement
{
    public class CharacterOrientation : MonoBehaviour, IOrientation
    {
        [SerializeField] private Transform _source;
        
        private float _yaw;
        private float _pitch;

        public Quaternion RotationFlat => Quaternion.Euler(0f, _yaw, 0f);
        public Quaternion RotationFull => Quaternion.Euler(_pitch, _yaw, 0f);
        public Vector3 Euler => new Vector3(_pitch, _yaw, 0f);
        public float Yaw => _yaw;
        public float Pitch => _pitch;

        public Vector3 ForwardFlat => RotationFlat * Vector3.forward;
        public Vector3 RightFlat => RotationFlat * Vector3.right;

        public Vector3 Forward => RotationFull * Vector3.forward;
        public Vector3 Right => RotationFull * Vector3.right;

        private void Update()
        {
            ApplyCameraRotation(_source.rotation);
        }

        public void ResetRotation()
        {
            _yaw = 0f;
            _pitch = 0f;
        }

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