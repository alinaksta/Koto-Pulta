using Game.Player;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Applies camera-driven sway to the hand UI root.
    /// </summary>
    public class HandsSway : MonoBehaviour
    {
        [SerializeField] private CameraController _camera;
        [SerializeField] private RectTransform _target;
        [SerializeField] private float _verticalScale = 0.6f;
        [SerializeField] private float _horizontallScale = 0.6f;
        [SerializeField] private float _maxDistance = 60f;
        [SerializeField] private float _acceleration = 10f;

        private Vector2 _restingPosition;
        private Vector2 _currentSway;

        private void Start()
        {
            _restingPosition = _target.anchoredPosition;
        }

        private void Update()
        {
            if (Time.timeScale == 0f) return;
            float dt = Time.deltaTime;

            float yawVelocity = _camera.AngularVelocity.x;
            float pitchVelocity = _camera.AngularVelocity.y;

            Vector2 targetSway = new Vector2(
                -yawVelocity * _horizontallScale,
                -pitchVelocity * _verticalScale);

            targetSway = Vector2.ClampMagnitude(targetSway, _maxDistance);

            _currentSway = Vector2.Lerp(_currentSway, targetSway, dt * _acceleration);
            if(float.IsNaN(_currentSway.x) || float.IsNaN(_currentSway.y)) _currentSway = Vector2.zero;
            _target.anchoredPosition = _restingPosition + _currentSway;
        }
    }
}
