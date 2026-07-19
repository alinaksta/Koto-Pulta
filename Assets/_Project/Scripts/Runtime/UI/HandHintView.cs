using Game.Interaction;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Displays an arm-specific hint for the item currently held in one hand.
    /// </summary>
    public sealed class HandHintView : MonoBehaviour
    {
        [SerializeField] private DualHandInteractor _interactor;
        [SerializeField] private HandType _handType;
        [SerializeField] private TMP_Text _label;

        [Header("Hints")]
        [SerializeField] private string _leftDropHint = "Q to drop";
        [SerializeField] private string _rightDropHint = "E to drop";
        [SerializeField] private string _leftThrowHint = "Hold LMB to throw";
        [SerializeField] private string _rightThrowHint = "Hold RMB to throw";

        private Hand _hand;

        private void Awake()
        {
            if (_label == null)
                _label = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            SubscribeHand();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeHand();
        }

        private void Update()
        {
            if (_hand == null)
                SubscribeHand();
        }

        private void SubscribeHand()
        {
            if (_hand != null || _interactor == null)
                return;

            _hand = _handType == HandType.Left
                ? _interactor.LeftHand
                : _interactor.RightHand;

            if (_hand == null)
                return;

            _hand.OnItemChanged += HandleItemChanged;
            _hand.OnSetVisible += HandleVisibilityChanged;
            Refresh();
        }

        private void UnsubscribeHand()
        {
            if (_hand == null)
                return;

            _hand.OnItemChanged -= HandleItemChanged;
            _hand.OnSetVisible -= HandleVisibilityChanged;
            _hand = null;
        }

        private void HandleItemChanged(Game.Items.Item? item)
        {
            Refresh();
        }

        private void HandleVisibilityChanged(bool visible)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_label == null)
                return;

            if (_hand == null || _hand.IsEmpty || !_hand.Visible)
            {
                _label.gameObject.SetActive(false);
                return;
            }

            _label.text = _hand.HasThrowableItem
                ? $"{GetThrowHint()}\n{GetDropHint()}"
                : GetDropHint();
            _label.gameObject.SetActive(true);
        }

        private string GetDropHint()
        {
            return _handType == HandType.Left ? _leftDropHint : _rightDropHint;
        }

        private string GetThrowHint()
        {
            return _handType == HandType.Left ? _leftThrowHint : _rightThrowHint;
        }
    }
}
