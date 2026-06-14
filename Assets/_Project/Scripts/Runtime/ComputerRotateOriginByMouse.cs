using Game.Input;
using Game.Services;
using UnityEngine;

namespace Game.Computer
{
    public class ComputerRotateOriginByMouse : MonoBehaviour
    {
        [SerializeField] private Transform _origin;
        [SerializeField] private float _rotationOfssetVertical = 10f;
        [SerializeField] private float _rotationOfssetHorizontal = 10f;
        [SerializeField] private float _lerpCoefficient = 12f;

        private IInputService _inputService;

        private void Start()
        {
            _inputService = ServiceLocator.Get<IInputService>();
        }

        private void Update()
        {
            Vector2 normalized = new Vector2(
                _inputService.MousePosition.x / Screen.width,
                _inputService.MousePosition.y / Screen.height
            );

            float x = Mathf.Lerp(-_rotationOfssetHorizontal, _rotationOfssetHorizontal, normalized.x);
            float y = Mathf.Lerp(-_rotationOfssetVertical, _rotationOfssetVertical, normalized.y);
            Quaternion targetLerp = Quaternion.Euler(-y, x, 0f);

            _origin.rotation = Quaternion.Slerp(_origin.rotation, targetLerp, _lerpCoefficient * Time.deltaTime);
        }
    }
}