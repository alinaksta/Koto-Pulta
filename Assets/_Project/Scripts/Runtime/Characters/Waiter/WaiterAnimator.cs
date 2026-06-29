using Game.Animation;
using UnityEngine;

namespace Game.Characters
{
    public class WaiterAnimator : MonoBehaviour
    {
        private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
        private static readonly int IsRagdolledHash = Animator.StringToHash("isRagdolled");

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

            _rotator.FullRotation = _waiter.IsRagdolled;
        }
    }
}