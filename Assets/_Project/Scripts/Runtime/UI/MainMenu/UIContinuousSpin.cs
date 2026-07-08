using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Applies continuous rotation to a UI rect transform.
    /// </summary>
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
