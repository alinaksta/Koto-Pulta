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
            _target = _target != null ? _target : transform;
            _cameraTransform = Camera.main != null ? Camera.main.transform : null;
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null)
                return;

            Vector3 direction = _cameraTransform.position - _target.position;

            if (!FullRotation)
                direction.y = 0f;

            if (direction.sqrMagnitude > Mathf.Epsilon)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                Vector3 eulerAngles = targetRotation.eulerAngles;
                eulerAngles.z = _target.rotation.eulerAngles.z;
                _target.rotation = Quaternion.Euler(eulerAngles);
            }
        }
    }
}
