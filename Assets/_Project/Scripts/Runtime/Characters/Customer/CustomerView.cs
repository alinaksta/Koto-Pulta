using Game.Environment;
using Game.Items;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace Game.Characters
{
    public class CustomerView : MonoBehaviour
    {
        private const float NoAlpha = 0f;
        private const float FullAlpha = 1f;

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private ColorPalette _colorPalette;
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

            AnimateSpawn(_customer.SpawnDuration);
        }

        private void OnDestroy()
        {
            _customer.OnWrongItemGiven -= HandleWrongItemGiven;
            _customer.OnTimedOut -= HandleTimedOut;

            SetSpriteAlpha(NoAlpha);
        }

        #region Event handlers
        private void HandleTimedOut(Customer customer)
        {
            AnimateDespawn(customer.DespawnDuration);
        }

        private void HandleWrongItemGiven(Customer customer, Item item)
        {
            AnimateDespawn(customer.DespawnDuration);
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