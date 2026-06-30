using TMPro;
using Game.Characters;
using Game.Items.Properties;
using UnityEngine;

namespace Game.Diagnostics
{
    /// <summary>
    /// Shows the waiter's sticky note and requested meal debug visuals.
    /// </summary>
    public class DebugWaiterView : MonoBehaviour
    {
        [SerializeField] private Waiter _waiter;
        [SerializeField] private GameObject _orderNote;
        [SerializeField] private TextMeshPro _tableNumberLabel;
        [SerializeField] private SpriteRenderer _orderSpriteRenderer;

        private void OnEnable()
        {
            _waiter.OnCustomerAssigned += HandleCustomerAssigned;
            _waiter.OnCustomerUnassigned += HandleCustomerUnassigned;
            _waiter.OnCustomerWasAsked += HandleCustomerWasAsked;
            _waiter.OnMealPointEntered += HandleMealPointEntered;
            _waiter.OnMealPointExited += HandleMealPointExited;

            RefreshVisuals();
        }

        private void OnDisable()
        {
            _waiter.OnCustomerAssigned -= HandleCustomerAssigned;
            _waiter.OnCustomerUnassigned -= HandleCustomerUnassigned;
            _waiter.OnCustomerWasAsked -= HandleCustomerWasAsked;
            _waiter.OnMealPointEntered -= HandleMealPointEntered;
            _waiter.OnMealPointExited -= HandleMealPointExited;
        }

        private void HandleCustomerAssigned(Customer _)
        {
            HideOrderNote();
            HideOrderSprite();
        }

        private void HandleCustomerUnassigned()
        {
            HideOrderNote();
            HideOrderSprite();
        }

        private void HandleCustomerWasAsked(Customer customer)
        {
            ShowOrderNote(customer);
            HideOrderSprite();
        }

        private void HandleMealPointEntered()
        {
            if (!ShouldShowOrderSprite())
            {
                HideOrderSprite();
                return;
            }

            ShowOrderSprite(_waiter.AssignedCustomer);
        }

        private void HandleMealPointExited()
        {
            HideOrderSprite();
        }

        private void RefreshVisuals()
        {
            if (ShouldShowOrderNote())
                ShowOrderNote(_waiter.AssignedCustomer);
            else
                HideOrderNote();

            if (ShouldShowOrderSprite())
                ShowOrderSprite(_waiter.AssignedCustomer);
            else
                HideOrderSprite();
        }

        private bool ShouldShowOrderNote()
            => _waiter.IsAssigned &&
               _waiter.AssignedCustomer != null &&
               _waiter.ServiceState != WaiterServiceState.Unassigned &&
               _waiter.ServiceState != WaiterServiceState.AskingCustomer;

        private bool ShouldShowOrderSprite()
            => ShouldShowOrderNote() &&
               _waiter.ServiceState == WaiterServiceState.AwaitingMeal &&
               _waiter.AtMealPoint;

        private void ShowOrderNote(Customer customer)
        {
            _orderNote.SetActive(true);
            _tableNumberLabel.text = customer.Table.TableNumber.ToString();
        }

        private void HideOrderNote()
        {
            _orderNote.SetActive(false);
            _tableNumberLabel.text = string.Empty;
        }

        private void ShowOrderSprite(Customer customer)
        {
            if (customer.Order != null &&
                customer.Order.TryGetProperty<FoodProperty>(out var foodProperty) &&
                foodProperty.DialogueSprite != null)
            {
                _orderSpriteRenderer.sprite = foodProperty.DialogueSprite;
                _orderSpriteRenderer.enabled = true;
                return;
            }

            HideOrderSprite();
        }

        private void HideOrderSprite()
        {
            _orderSpriteRenderer.sprite = null;
            _orderSpriteRenderer.enabled = false;
        }
    }
}
