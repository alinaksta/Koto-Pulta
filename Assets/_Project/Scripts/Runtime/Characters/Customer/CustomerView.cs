using Game.Environment;
using LitMotion;
using LitMotion.Extensions;
using System;
using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Updates customer visuals from gameplay state.
    /// </summary>
    public class CustomerView : MonoBehaviour
    {
        private static int PatienceHash = Animator.StringToHash("patience");
        private static int StartedAskingHash = Animator.StringToHash("startedAsking");
        private static int StoppedAskingHash = Animator.StringToHash("stoppedAsking");
        private static int ServedHash = Animator.StringToHash("served");

        private const float NoAlpha = 0f;
        private const float FullAlpha = 1f;

        [SerializeField] private SpriteRenderer _bodyRenderer;
        [SerializeField] private Animator _animator;
        [SerializeField] private Customer _customer;

        private bool _destroyed;

        
        
        private void Awake()
        {
            SetSpriteAlpha(NoAlpha);
        }

        private void Update()
        {
            if (!CanUseAnimator())
                return;

            _animator.SetFloat(PatienceHash, _customer.GetCurrentWaitTimer());
        }

        private void Start()
        {
            if (_customer.Seat != null)
                _bodyRenderer.flipX = _customer.Seat.FlipSprite;

            _customer.OnTimedOut += HandleTimedOut;
            _customer.OnWaiterStartedAsking += HandleWaiterStartedAsking;
            _customer.OnServed += HandleServed;

            AnimateSpawn(_customer.SpawnDuration);
        }

        private void OnDestroy()
        {
            _destroyed = true;

            _customer.OnTimedOut -= HandleTimedOut;
            _customer.OnWaiterStartedAsking -= HandleWaiterStartedAsking;
            _customer.OnServed -= HandleServed;


            SetSpriteAlpha(NoAlpha);
        }

        #region Event handlers
        private void HandleServed(Customer customer)
        {
            if (CanUseAnimator())
                _animator.SetTrigger(ServedHash);

            AnimateDespawn(customer.DespawnDuration);
        }

        private void HandleTimedOut(Customer customer)
        {
            AnimateDespawn(customer.DespawnDuration);
        }

        private async void HandleWaiterStartedAsking(Customer customer, float duration)
        {
            if (CanUseAnimator())
                _animator.SetTrigger(StartedAskingHash);

            await Awaitable.WaitForSecondsAsync(duration);

            if (CanUseAnimator())
                _animator.SetTrigger(StoppedAskingHash);
        }
        #endregion

        #region Helpers
        private bool CanUseAnimator()
            => !_destroyed && _animator != null;

        private void SetSpriteAlpha(float alpha)
        {
            var color = _bodyRenderer.color;
            color.a = alpha;
            _bodyRenderer.color = color;
        }

        private void AnimateAlpha(float from, float to, float duration)
        {
            LMotion.Create(from, to, duration).BindToColorA(_bodyRenderer);
        }

        private void AnimateSpawn(float duration)
            => AnimateAlpha(NoAlpha, FullAlpha, duration);

        private void AnimateDespawn(float duration)
            => AnimateAlpha(FullAlpha, NoAlpha, duration);
        #endregion
    }
}
