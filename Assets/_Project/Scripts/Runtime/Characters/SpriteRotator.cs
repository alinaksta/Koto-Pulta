using UnityEngine;

public class SpriteRotator : MonoBehaviour
{
    public Vector3 cam_position;
    Animator animator;
    public float angle;

    Vector3 last_pos;
    public Vector3 velocity;

    void Start()
    {
        velocity = transform.parent.forward;
        animator = gameObject.GetComponent<Animator>();
    }

    void Update()
    {
        cam_position = GameObject.FindWithTag("MainCamera").transform.parent.parent.position;
        transform.rotation = Quaternion.LookRotation(cam_position - transform.position);
        angle = transform.localEulerAngles.y;
        transform.localEulerAngles = new Vector3(0f, angle, 0f);
        animator.SetFloat("angle", angle - 180);

        velocity = (transform.parent.position - last_pos) / Time.deltaTime;
        animator.SetFloat("speed", velocity.magnitude);

        last_pos = transform.parent.position;
    }
}
