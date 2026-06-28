using UnityEngine;

public class AngleAnimator : MonoBehaviour
{
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    [SerializeField] private float _angle;
    [SerializeField] private bool _sideViewIsLeft = true;
    [SerializeField] private float _minSideThreshold = 70f;
    [SerializeField] private float _maxSideThreshold = 110f;
    [SerializeField] private float _forwardValue = 0f;
    [SerializeField] private float _sideValue = 1f;
    [SerializeField] private float _backValue = 2f;
    void Start()
    {
        _animator = gameObject.GetComponent<Animator>();
        _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }
    void LateUpdate()
    {
        _angle = transform.localEulerAngles.y-180; // [-180; 180] because absolutes
        float animAngle = (Mathf.Abs(_angle) < _minSideThreshold) ? _forwardValue : (Mathf.Abs(_angle) < _maxSideThreshold) ? _sideValue : _backValue;
        
        // if its in the side view state specifically to not flip on front and back views
        // and if its supposed to be right-facing (andle < 0) while the normal side view is left-facing!!!
        _spriteRenderer.flipX = animAngle == _sideValue && (_angle < 0 == _sideViewIsLeft);

        // ints dont seem to work in blend trees? its float only, so 0 1 2 are kind of weird.
        _animator.SetFloat("angle", animAngle); 
    }
}
