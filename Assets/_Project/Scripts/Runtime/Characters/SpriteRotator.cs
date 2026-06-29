using UnityEngine;

namespace Game.Animation
{
    public sealed class SpriteRotator : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        public bool FullRotation = false;

        private Transform _cameraTransform;

        private void Start()
        {
            _cameraTransform = Camera.main.transform;
            _target = _target != null ? _target : transform;
        }

        private void LateUpdate()
        {
            Vector3 direction = _cameraTransform.position - _target.position;

            if (!FullRotation)
                direction.y = 0f;

            if (direction.sqrMagnitude > Mathf.Epsilon)
                _target.rotation = Quaternion.LookRotation(direction);
        }
    }
}