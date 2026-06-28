using UnityEngine;

public class WaiterAnimator : MonoBehaviour
{
    private Animator _animator;
    private Vector3 _lastPos;
    [SerializeField] private Vector3 _velocity;
    void Start()
    {
        _velocity = transform.parent.forward;
        _animator = gameObject.GetComponent<Animator>();
    }
    void LateUpdate()
    {
        _velocity = (transform.parent.position - _lastPos) / Time.deltaTime;
        _animator.SetBool("isMoving", _velocity.magnitude > 0.001f);
        _animator.SetFloat("speedY", Mathf.Abs(_velocity.y));

        _lastPos = transform.parent.position;
    }
}
