using TMPro;
using Game.Items.Properties;
using UnityEngine;
using System;

namespace Game.Characters
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
                _waiter.OnCustomerAssigned += OnCustomerAssigned;
                _waiter.OnMealPointEntered += OnMealPointEntered;
                _waiter.OnMealPointExited += OnMealPointExited;
                _waiter.OnServiceStateChanged += OnServiceStateChanged;
            }

            Refresh();
            HideMealSprite();
        }

        private void OnDestroy()
        {
            if (_waiter != null)
            {
                _waiter.OnCustomerAssigned -= OnCustomerAssigned;
                _waiter.OnMealPointEntered -= OnMealPointEntered;
                _waiter.OnMealPointExited -= OnMealPointExited;
                _waiter.OnServiceStateChanged -= OnServiceStateChanged;
            }
        }

        private void OnServiceStateChanged(WaiterServiceState from, WaiterServiceState to)
        {
            if (from == WaiterServiceState.AskingCustomer && to == WaiterServiceState.AwaitingMeal)
            {
                _text.text = _waiter.IsAssigned ? _waiter.AssignedCustomer.Table.TableNumber.ToString() : string.Empty;
            }
            if (from == WaiterServiceState.Delivering && to == WaiterServiceState.Unassigned)
            {
                _text.text = string.Empty;
            }
        }

        private void OnCustomerAssigned(Customer customer)
        {
            // nothing
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
