using Game.Animation;
using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Updates waiter animation parameters from runtime state.
    /// </summary>
    public class WaiterAnimator : MonoBehaviour
    {
        private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
        private static readonly int IsRagdolledHash = Animator.StringToHash("isRagdolled");
        private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");

        [SerializeField] private Animator _animator;
        [SerializeField] private Waiter _waiter;
        [SerializeField] private SpriteRotator _rotator;

        private void LateUpdate()
        {
            WaiterLocomotionState state = _waiter.LocomotionState;

            _animator.SetBool(
                IsMovingHash,
                state is WaiterLocomotionState.Walking);

            _animator.SetBool(
                IsRagdolledHash,
                state == WaiterLocomotionState.Ragdoll);

            _animator.SetBool(
                IsGroundedHash,
                (state == WaiterLocomotionState.Ragdoll || state == WaiterLocomotionState.Recovering) && _waiter.IsGrounded);

            _rotator.FullRotation = _waiter.IsRagdolled;
        }
    }
}