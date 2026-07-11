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
            _waiter.OnServiceStateChanged += HandleServiceStateChanged;

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
            _waiter.OnServiceStateChanged -= HandleServiceStateChanged;
        }

        private void Update()
        {
            UpdatePatienceIndicator();
            
            if (_animator != null)
            {
                _animator.SetFloat(PatienceHash, _waiter.WaitTimer);
            }
        }

        private void UpdatePatienceIndicator()
        {
            if (_patienceRenderer == null || _patienceMaterial == null)
                return;

            bool shouldShowPatience = ShouldShowPatience();
            
            if (shouldShowPatience)
            {
                _patienceMaterial.SetFloat(FillAmountID, _waiter.NormalizedPatience);
                var patienceColor = _patienceRenderer.color;
                var oldAlpha = patienceColor.a;
                patienceColor = _patienceGradient.Evaluate(_waiter.NormalizedPatience);
                patienceColor.a = oldAlpha;
                _patienceRenderer.color = patienceColor;
                
                SetPatienceAlpha(FullAlpha);
            }
            else
            {
                SetPatienceAlpha(NoAlpha);
            }
        }

        private bool ShouldShowPatience()
        {
            return _waiter.IsAssigned && 
                   _waiter.ServiceState != WaiterServiceState.Unassigned &&
                   _waiter.LocomotionState != WaiterLocomotionState.InHand &&
                   _waiter.LocomotionState != WaiterLocomotionState.Ragdoll &&
                   _waiter.LocomotionState != WaiterLocomotionState.Recovering;
        }

        private void SetPatienceAlpha(float alpha)
        {
            if (_patienceRenderer == null)
                return;
                
            var color = _patienceRenderer.color;
            color.a = alpha;
            _patienceRenderer.color = color;
        }

        private void AnimatePatience(float from, float to, float duration)
        {
            if (_patienceRenderer == null)
                return;
                
            LMotion.Create(from, to, duration).BindToColorA(_patienceRenderer);
        }

        private void HandleServiceStateChanged(WaiterServiceState from, WaiterServiceState to)
        {
            if (ShouldShowPatience())
            {
                AnimatePatience(NoAlpha, FullAlpha, 0.3f);
            }
            else
            {
                AnimatePatience(FullAlpha, NoAlpha, 0.3f);
            }
        }

        private void HandleCustomerAssigned(Customer _)
        {
            HideOrderNote();
            HideOrderSprite();
            SetPatienceAlpha(NoAlpha);
        }

        private void HandleCustomerUnassigned()
        {
            HideOrderNote();
            HideOrderSprite();
            SetPatienceAlpha(NoAlpha);
        }

        private void HandleCustomerWasAsked(Customer customer)
        {
            ShowOrderNote(customer);
            HideOrderSprite();
            AnimatePatience(NoAlpha, FullAlpha, 0.3f);
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