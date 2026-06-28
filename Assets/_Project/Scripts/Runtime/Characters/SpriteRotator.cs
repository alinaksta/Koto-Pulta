using UnityEngine;

public class SpriteRotator : MonoBehaviour
{
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private bool _fullRotation = false;
    void Start()
    {
        _cameraTransform = Camera.main.transform;
    }
    void LateUpdate()
    {
        transform.rotation = Quaternion.LookRotation(_cameraTransform.position - transform.position);   
        if(!_fullRotation) transform.localEulerAngles = new Vector3(0f, transform.localEulerAngles.y, 0f);
    }
}
