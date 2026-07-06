using Game.Environment;
using Game.Items;
using LitMotion;
using LitMotion.Extensions;
using System;
using UnityEngine;

namespace Game.Characters
{
    public class CustomerView : MonoBehaviour
    {
        private static int PatienceHash = Animator.StringToHash("patience");
        private static int StartedAskingHash = Animator.StringToHash("startedAsking");
        private static int StoppedAskingHash = Animator.StringToHash("stoppedAsking");
        private static int ServedHash = Animator.StringToHash("served");

        private const float NoAlpha = 0f;
        private const float FullAlpha = 1f;

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;
        [SerializeField] private Customer _customer;

        private void Awake()
        {
            SetSpriteAlpha(NoAlpha);
        }

        private void Start()
        {
            if (_customer.Seat != null)
                _spriteRenderer.flipX = _customer.Seat.FlipSprite;

            _customer.OnWrongItemGiven += HandleWrongItemGiven;
            _customer.OnTimedOut += HandleTimedOut;
            _customer.OnWaiterStartedAsking += HandleWaiterStartedAsking;
            _customer.OnServed += HandleServed;

            AnimateSpawn(_customer.SpawnDuration);
        }

        private void OnDestroy()
        {
            _customer.OnWrongItemGiven -= HandleWrongItemGiven;
            _customer.OnTimedOut -= HandleTimedOut;
            _customer.OnWaiterStartedAsking -= HandleWaiterStartedAsking;
            _customer.OnServed -= HandleServed;


            SetSpriteAlpha(NoAlpha);
        }

        private void Update()
        {
            _animator.SetFloat(PatienceHash, _customer.WaitTimer);
        }

        #region Event handlers
        private void HandleServed(Customer customer)
        {
            _animator.SetTrigger(ServedHash);
            AnimateDespawn(customer.DespawnDuration);
        }

        private void HandleTimedOut(Customer customer)
        {
            AnimateDespawn(customer.DespawnDuration);
        }

        private void HandleWrongItemGiven(Customer customer, Item item)
        {
            AnimateDespawn(customer.DespawnDuration);
        }

        private async void HandleWaiterStartedAsking(Customer customer, float duration)
        {
            _animator.SetTrigger(StartedAskingHash);

            await Awaitable.WaitForSecondsAsync(duration);

            _animator.SetTrigger(StoppedAskingHash);
        }
        #endregion

        #region Helpers
        private void SetSpriteAlpha(float alpha)
        {
            var color = _spriteRenderer.color;
            color.a = alpha;
            _spriteRenderer.color = color;
        }

        private void AnimateAlpha(float from, float to, float duration)
            => LMotion.Create(from, to, duration).BindToColorA(_spriteRenderer);

        private void AnimateSpawn(float duration)
            => AnimateAlpha(NoAlpha, FullAlpha, duration);

        private void AnimateDespawn(float duration)
            => AnimateAlpha(FullAlpha, NoAlpha, duration);
        #endregion
    }
}