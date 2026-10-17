using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// One sticky note: dish picture, recipe picture and an optional patience bar.
    /// Knows nothing about gameplay, it only displays what it is given.
    /// </summary>
    public sealed class OrderNoteView : MonoBehaviour
    {
        [SerializeField] private Image _dishImage;
        [SerializeField] private Image _recipeImage;
        [Tooltip("Optional. Image with Type = Filled.")]
        [SerializeField] private Image _timerFill;
        [SerializeField] private Gradient _timerGradient;
        [SerializeField] private float _popInDuration = 0.25f;

        public void SetDish(Sprite sprite) => SetImage(_dishImage, sprite);

        public void SetRecipe(Sprite sprite) => SetImage(_recipeImage, sprite);

        /// <summary>Sets the patience bar. 1 = full, 0 = empty.</summary>
        public void SetPatience(float normalized)
        {
            if (_timerFill == null)
                return;

            normalized = Mathf.Clamp01(normalized);
            _timerFill.fillAmount = normalized;
            _timerFill.color = _timerGradient.Evaluate(normalized);
        }

        public void PlayPopIn()
        {
            if (_popInDuration <= 0f)
                return;

            transform.localScale = Vector3.zero;
            LMotion.Create(Vector3.zero, Vector3.one, _popInDuration)
                .WithEase(Ease.OutBack)
                .BindToLocalScale(transform);
        }

        private static void SetImage(Image image, Sprite sprite)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }
}