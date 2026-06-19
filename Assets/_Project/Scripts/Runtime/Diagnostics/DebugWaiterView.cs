using TMPro;
using Game.Characters;
using Game.Items.Properties;
using UnityEngine;

namespace Game.Diagnostics
{
    public class DebugWaiterView : MonoBehaviour
    {
        [SerializeField] private Waiter _waiter;
        [SerializeField] private TextMeshPro _text;
        [SerializeField] private SpriteRenderer _mealSpriteRenderer;

        private void Awake()
        {
            if (_waiter != null)
            {
                _waiter.OnMealPointEntered += OnMealPointEntered;
                _waiter.OnMealPointExited += OnMealPointExited;
                _waiter.OnCustomerWasAsked += OnCustomerWasAsked;
                _waiter.OnCustomerUnassigned += OnCustomerUnassigned;
            }

            Refresh();
            HideMealSprite();
        }

        private void OnCustomerUnassigned()
        {
            _text.text = string.Empty;
        }

        private void OnCustomerWasAsked(Customer customer)
        {
            _text.text = customer.Table.TableNumber.ToString();
        }

        private void OnDestroy()
        {
            if (_waiter != null)
            {
                _waiter.OnMealPointEntered -= OnMealPointEntered;
                _waiter.OnMealPointExited -= OnMealPointExited;
                _waiter.OnCustomerWasAsked -= OnCustomerWasAsked;
                _waiter.OnCustomerUnassigned -= OnCustomerUnassigned;
            }
        }


        private void Refresh()
        {
            if (_text == null)
                return;

            var table = _waiter != null && _waiter.IsAssigned ? _waiter.AssignedCustomer.Table : null;
            _text.text = table != null ? table.TableNumber.ToString() : string.Empty;
        }

        private void OnMealPointEntered()
        {
            if (_mealSpriteRenderer == null)
                return;

            if (_waiter == null || !_waiter.IsAssigned || _waiter.ServiceState != WaiterServiceState.AwaitingMeal)
            {
                HideMealSprite();
                return;
            }

            if (_waiter.AssignedCustomer.Order != null &&
                _waiter.AssignedCustomer.Order.TryGetProperty<SpriteProperty>(out var spriteProperty))
            {
                _mealSpriteRenderer.sprite = spriteProperty.Sprite;
                _mealSpriteRenderer.enabled = spriteProperty.Sprite != null;
                return;
            }

            HideMealSprite();
        }

        private void OnMealPointExited()
        {
            HideMealSprite();
        }

        private void HideMealSprite()
        {
            if (_mealSpriteRenderer == null)
                return;

            _mealSpriteRenderer.sprite = null;
            _mealSpriteRenderer.enabled = false;
        }
    }
}
