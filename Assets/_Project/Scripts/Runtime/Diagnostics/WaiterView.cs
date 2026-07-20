using TMPro;
using Game.Characters;
using Game.Items.Properties;
using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

namespace Game.Diagnostics
{
    /// <summary>
    /// Shows the waiter's sticky note, requested meal, and patience timer visuals.
    /// </summary>
    public class WaiterView : MonoBehaviour
    {
        private static int PatienceHash = Animator.StringToHash("patience");
        
        private static int FillAmountID = Shader.PropertyToID("_FillAmount");

        private const float NoAlpha = 0f;
        private const float FullAlpha = 1f;

        [SerializeField] private Waiter _waiter;
        [SerializeField] private GameObject _orderNote;
        [SerializeField] private TextMeshPro _tableNumberLabel;
        [SerializeField] private SpriteRenderer _orderSpriteRenderer;
        [SerializeField] private SpriteMask _orderSpriteMask;
        
        [SerializeField] private SpriteRenderer _patienceRenderer;
        [SerializeField] private Gradient _patienceGradient;
        [SerializeField] private Animator _animator; 

        private Material _patienceMaterial;

        private void OnEnable()
        {
            _waiter.OnCustomerAssigned += HandleCustomerAssigned;
            _waiter.OnCustomerUnassigned += HandleCustomerUnassigned;
            _waiter.OnCustomerWasAsked += HandleCustomerWasAsked;
            _waiter.OnMealPointEntered += HandleMealPointEntered;
            _waiter.OnMealPointExited += HandleMealPointExited;

            if (_patienceRenderer != null)
            {
                _patienceMaterial = _patienceRenderer.material;
                SetPatienceAlpha(NoAlpha);
            }

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

        private void Update()
        {
            UpdatePatienceIndicator();
        }

        private void UpdatePatienceIndicator()
        {
            
            if (_patienceRenderer == null || _patienceMaterial == null)
                return;

            
            _patienceMaterial.SetFloat(FillAmountID, _waiter.NormalizedPatience);
            var patienceColor = _patienceRenderer.color;
            var oldAlpha = patienceColor.a;
            patienceColor = _patienceGradient.Evaluate(_waiter.NormalizedPatience);
            patienceColor.a = oldAlpha;
            _patienceRenderer.color = patienceColor;
               
        }

        private void SetPatienceAlpha(float alpha)
        {
            if (_patienceRenderer == null)
                return;
                
            var color = _patienceRenderer.color;
            color.a = alpha;
            _patienceRenderer.color = color;
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
                _orderSpriteMask.sprite = foodProperty.WorldSprite;
                _orderSpriteRenderer.enabled = true;
                SetPatienceAlpha(FullAlpha);
                return;
            }

            HideOrderSprite();
        }

        private void HideOrderSprite()
        {
            _orderSpriteRenderer.sprite = null;
            _orderSpriteRenderer.enabled = false;
            SetPatienceAlpha(NoAlpha);
        }
    }
}