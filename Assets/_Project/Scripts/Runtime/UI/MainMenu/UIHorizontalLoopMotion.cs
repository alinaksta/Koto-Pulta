using UnityEngine;

namespace Game.UI
{
    public class UIHorizontalLoopMotion : MonoBehaviour
    {
        [SerializeField] private RectTransform _target;
        [SerializeField] private float _distance = 20f;
        [SerializeField] private float _speed = 1f;

        private Vector2 _startPosition;

        private void Awake()
        {
            if (_target == null)
                _target = transform as RectTransform;

            if (_target != null)
                _startPosition = _target.anchoredPosition;
        }

        private void Update()
        {
            if (_target == null)
                return;

            Vector2 position = _startPosition;
            position.x += Mathf.Sin(Time.unscaledTime * _speed) * _distance;
            _target.anchoredPosition = position;
        }
    }
}
