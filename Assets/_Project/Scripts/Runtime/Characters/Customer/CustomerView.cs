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
        [Tooltip("Visuals shown only while the customer is seated (e.g. the chair). Hidden while walking/queuing.")]
        [SerializeField] private GameObject[] _seatedOnlyVisuals;
        [SerializeField] private float _resetAnimatorDelayAfterVisit = 1f;

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
            RefreshSeatedVisuals();
        }

        private void RefreshSeatedVisuals()
        {
            if (_seatedOnlyVisuals == null)
                return;

            bool seated = _customer.IsSeated;
            for (int i = 0; i < _seatedOnlyVisuals.Length; i++)
            {
                var visual = _seatedOnlyVisuals[i];
                if (visual != null && visual.activeSelf != seated)
                    visual.SetActive(seated);
            }
        }

        private void Start()
        {
            if (_customer.Seat != null)
                _bodyRenderer.flipX = _customer.Seat.FlipSprite;

            _customer.OnTimedOut += HandleTimedOut;
            _customer.OnWaiterStartedAsking += HandleWaiterStartedAsking;
            _customer.OnServed += HandleServed;
            _customer.OnOrderStarted += HandleOrderStarted;
            _customer.OnVisitEnded += HandleVisitEnded;

            RefreshSeatedVisuals();
            AnimateSpawn(_customer.SpawnDuration);
        }

        private void OnDestroy()
        {
            _destroyed = true;

            _customer.OnTimedOut -= HandleTimedOut;
            _customer.OnWaiterStartedAsking -= HandleWaiterStartedAsking;
            _customer.OnServed -= HandleServed;
            _customer.OnOrderStarted -= HandleOrderStarted;
            _customer.OnVisitEnded -= HandleVisitEnded;

            SetSpriteAlpha(NoAlpha);
        }

        #region Event handlers
        private void HandleServed(Customer customer)
        {
            if (CanUseAnimator())
                _animator.SetTrigger(ServedHash);

            if (!customer.IsPersistent)
                AnimateDespawn(customer.DespawnDuration);
        }

        private void HandleTimedOut(Customer customer)
        {
            if (!customer.IsPersistent)
                AnimateDespawn(customer.DespawnDuration);
        }

        private void HandleOrderStarted(Customer customer)
        {
            _bodyRenderer.flipX = customer.Seat != null && customer.Seat.FlipSprite;
            RefreshSeatedVisuals();
        }

        private async void HandleVisitEnded(Customer customer)
        {
            _bodyRenderer.flipX = false;
            RefreshSeatedVisuals();

            // Let the served/angry reaction play, then return the animator to its idle state for the next visit.
            await Awaitable.WaitForSecondsAsync(_resetAnimatorDelayAfterVisit);

            if (!CanUseAnimator() || _customer.State != CustomerState.None)
                return;

            _animator.Rebind();
            _animator.Update(0f);
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
