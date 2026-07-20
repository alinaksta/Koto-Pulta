using Game.Input;
using Game.Interaction;
using Game.Services;
using Game.Utils;
using UnityEngine;
using System;

namespace Game.Movement
{
    /// <summary>
    /// Moves the player using CharacterController-based ground and air physics.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private InterfaceReference<IOrientation> _orientationReference;

        [Header("Ground")]
        [SerializeField] private float _groundSpeed = 7f;
        [SerializeField] private float _groundAcceleration = 60f;
        [SerializeField] private float _groundFriction = 12f;

        [Header("Air")]
        [SerializeField] private float _airSpeed = 7f;
        [SerializeField] private float _airAcceleration = 12f;
        [SerializeField] private float _airFriction = 0f;

        [Header("Jump / Gravity")]
        [SerializeField] private float _jumpHeight = 1.2f;
        [SerializeField] private float _gravity = -30f;
        [SerializeField] private float _groundStickVelocity = -0.2f;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask _groundLayers = -1;
        [SerializeField] private float _groundCheckDistance = 0.12f;
        [SerializeField] private float _groundSnapDistance = 0.6f;

        private IInputService _input;
        private IOrientation _orientation;
        private CharacterController _controller;

        private Vector3 _velocity;
        private Vector2 _move;
        private bool _jumpQueued;

        private bool _grounded;
        private Vector3 _groundNormal;

        /// <summary>
        /// Gets the current simulated velocity.
        /// </summary>
        public Vector3 Velocity => _velocity;

        /// <summary>
        /// Gets whether the controller is currently grounded.
        /// </summary>
        public bool IsGrounded => _grounded;

        /// <summary>
        /// Gets the current ground normal when grounded checks hit.
        /// </summary>
        public Vector3 GroundNormal => _groundNormal;

        /// <summary>
        /// Gets the orientation source used to convert movement input into world space.
        /// </summary>
        public IOrientation Orientation => _orientation;


        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = ServiceLocator.Get<IInputService>();

            _orientation = _orientationReference.Value;
            _orientation ??= GetComponent<IOrientation>();
            _grounded = CastGround(_groundCheckDistance, out _groundNormal);
        }

        private void Update()
        {
            if (_input == null)
                return;

            if (ComputerInteractable.AnyComputerInUse)
            {
                _move = Vector2.zero;
                _jumpQueued = false;
                return;
            }

            _move = Vector2.ClampMagnitude(_input.Move, 1f);

            if (_input.Jump.Pressed)
                _jumpQueued = true;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            _grounded = CastGround(_groundCheckDistance, out _groundNormal);

            bool wasGrounded = _grounded;
            bool jump = _jumpQueued;
            _jumpQueued = false;

            Vector3 wishDir = GetWishDir();

            if (_grounded && jump)
            {
                _velocity.y = MovementMath.ToJumpForce(_jumpHeight, _gravity);
                _grounded = false;
                wasGrounded = false;
                MoveAir(wishDir, dt);
            }
            else if (_grounded)
            {
                _velocity.y = _groundStickVelocity;
                MoveGround(wishDir, dt);
            }
            else
            {
                _velocity.y += _gravity * dt;
                MoveAir(wishDir, dt);
            }

            MoveAndSnap(dt, wasGrounded);
        }

        private void MoveGround(Vector3 wishDir, float dt)
        {
            Vector3 flat = _velocity.Flat();
            flat = MovementMath.ApplyFriction(flat, _groundFriction, 0f, dt);
            flat = MovementMath.Accelerate(flat, wishDir, _groundSpeed, _groundAcceleration, dt);

            _velocity.x = flat.x;
            _velocity.z = flat.z;
        }

        private void MoveAir(Vector3 wishDir, float dt)
        {
            Vector3 flat = _velocity.Flat();
            flat = MovementMath.ApplyFriction(flat, _airFriction, 0f, dt);
            flat = MovementMath.Accelerate(flat, wishDir, _airSpeed, _airAcceleration, dt);

            _velocity.x = flat.x;
            _velocity.z = flat.z;
        }

        private void MoveAndSnap(float dt, bool wasGrounded)
        {
            Vector3 before = transform.position;

            _controller.Move(_velocity * dt);

            if (wasGrounded && !CastGround(_groundCheckDistance, out _) && CastGround(_groundSnapDistance, out _))
                _controller.Move(Vector3.down * _groundSnapDistance);

            _velocity = (transform.position - before) / dt;
            _grounded = CastGround(_groundCheckDistance, out _groundNormal);
        }

        private Vector3 GetWishDir()
        {
            Vector3 forward = _orientation != null ? _orientation.ForwardFlat : transform.forward.Flat().normalized;
            Vector3 right = _orientation != null ? _orientation.RightFlat : transform.right.Flat().normalized;

            return Vector3.ClampMagnitude(forward * _move.y + right * _move.x, 1f);
        }

        private bool CastGround(float distance, out Vector3 normal)
        {
            normal = Vector3.zero;

            Vector3 center = transform.TransformPoint(_controller.center);
            Vector3 bottom = center - transform.up * ((_controller.height * 0.5f) - _controller.radius);

            const float padding = 0.02f;

            bool hitGround = Physics.SphereCast(
                bottom + transform.up * padding,
                Mathf.Max(0.01f, _controller.radius * 0.95f),
                -transform.up,
                out RaycastHit hit,
                distance + padding,
                _groundLayers,
                QueryTriggerInteraction.Ignore);

            if (hitGround)
                normal = hit.normal;

            return hitGround;
        }
    }
}
