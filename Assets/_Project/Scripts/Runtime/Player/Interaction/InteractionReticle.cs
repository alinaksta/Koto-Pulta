using UnityEngine;
using UnityEngine.UI;

namespace Game.Interaction
{
    public class InteractionReticle : MonoBehaviour
    {
        [SerializeField] private DualHandInteractor _interactor;
        [SerializeField] private Image _reticle;
        [SerializeField] private float _hoverSize = 16f;
        [SerializeField] private float _lerp = 12f;

        private float _originalSize;

        private void Awake()
        {
            _originalSize = _reticle.rectTransform.sizeDelta.x;
        }

        private void Update()
        {
            float target = _interactor.HoveredObject == null ? _originalSize : _hoverSize;
            Vector2 size = _reticle.rectTransform.sizeDelta;
            _reticle.rectTransform.sizeDelta = Vector2.Lerp(size, Vector2.one * target, _lerp * Time.deltaTime);

            bool unfocused = _interactor.FocusStatus == Player.FocusStatus.Unfocused;
            _reticle.gameObject.SetActive(unfocused);
        }
    }
}