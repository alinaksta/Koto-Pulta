using Game.Input;
using Game.Services;
using UnityEngine;

namespace Game.Movement
{

    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _groundSpeed = 6f;
        [SerializeField] private float _groundAcceleration = 12f;
        [SerializeField] private float _groundFriction = 8f;
        [SerializeField] private float _stopSpeed = 1f;

        private IInputService _inputService;

        private Vector3 _velocity;
        private bool _isGrounded;

        private void Start()
        {
            _inputService = ServiceLocator.Get<IInputService>();
        }

        private void FixedUpdate()
        {
            
        }
    }
}