using UnityEngine;

namespace Game.UI
{
    public class UIContinuousSpin : MonoBehaviour
    {
        [SerializeField] private RectTransform _target;
        [SerializeField] private float _speed = 45f;

        private void Awake()
        {
            if (_target == null)
                _target = transform as RectTransform;
        }

        private void Update()
        {
            if (_target == null)
                return;

            _target.Rotate(0f, 0f, _speed * Time.unscaledDeltaTime);
        }
    }
}
