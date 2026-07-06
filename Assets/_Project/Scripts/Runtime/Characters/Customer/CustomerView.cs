using UnityEngine;

namespace Game.Characters
{
    public class CustomerView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Customer _customer;

        private void Start()
        {
            if (_customer.Seat != null)
                _spriteRenderer.flipX = _customer.Seat.FlipSprite;
        }
    }
}