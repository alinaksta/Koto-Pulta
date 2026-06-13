using Game.Movement;
using UnityEngine;

namespace Game.Player.CameraLayers
{
    [AddComponentMenu("Camera Layers/Viewroll")]
    public class ViewrollLayer : PlayerCameraLayerBase
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerController _controller;

        [Header("Headroll settings")]
        [SerializeField] private float _lerp = 12f;
        [SerializeField] private float _minVelocity = 2f;
        [SerializeField] private float _maxVelocity = 10f;
        [SerializeField] private float _rollDegrees = 15f;

        private float _currentRoll = 0f;

        private void Update()
        {
            float targetRoll = 0f;

            if (_controller.IsGrounded)
            {
                var velocity = _controller.Velocity;
                float dotRight = Vector3.Dot(velocity.normalized, _controller.Orientation.RightFlat);
                float velocityRight = Mathf.Abs(velocity.magnitude * dotRight);

                float speed01 = Mathf.InverseLerp(_minVelocity, _maxVelocity, velocityRight);
                if (velocityRight < _minVelocity)
                    speed01 = 0f;
                if (velocityRight > _maxVelocity)
                    speed01 = 1f;

                targetRoll = speed01 * -_rollDegrees * Mathf.Sign(dotRight);
            }

            float t = 1f - Mathf.Exp(-_lerp * Time.deltaTime);
            _currentRoll = Mathf.Lerp(_currentRoll, targetRoll, t);

            layer.rotation = Quaternion.Euler(0f, 0f, _currentRoll);
        }
    }
}