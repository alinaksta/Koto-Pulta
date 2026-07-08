using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Applies looping vertical motion to a UI rect transform.
    /// </summary>
    public class UIVerticalLoopMotion : MonoBehaviour
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
            position.y += Mathf.Sin(Time.unscaledTime * _speed) * _distance;
            _target.anchoredPosition = position;
        }
    }
}
