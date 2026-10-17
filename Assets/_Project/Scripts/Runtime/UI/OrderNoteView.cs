using LitMotion;
using LitMotion.Extensions;
using TMPro;
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
        [Tooltip("Optional. Short label identifying the order (e.g. table number). Created at runtime when empty.")]
        [SerializeField] private TMP_Text _label;

        public void SetDish(Sprite sprite) => SetImage(_dishImage, sprite);

        public void SetRecipe(Sprite sprite) => SetImage(_recipeImage, sprite);

        /// <summary>Shows a short label on the note (e.g. the table number). Empty hides it.</summary>
        public void SetLabel(string text)
        {
            if (_label == null && !string.IsNullOrEmpty(text))
                _label = CreateLabel();

            if (_label == null)
                return;

            _label.text = text;
            _label.enabled = !string.IsNullOrEmpty(text);
        }

        private TMP_Text CreateLabel()
        {
            var go = new GameObject("Label", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0f, 0.7f);
            rect.anchorMax = new Vector2(0.45f, 1f);
            rect.offsetMin = new Vector2(8f, 0f);
            rect.offsetMax = new Vector2(0f, -6f);

            var label = go.AddComponent<TextMeshProUGUI>();
            var sceneText = FindAnyObjectByType<TextMeshProUGUI>();
            if (sceneText != null && sceneText != label && sceneText.font != null)
                label.font = sceneText.font;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 48f;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.color = new Color(0.15f, 0.12f, 0.1f);
            label.raycastTarget = false;
            return label;
        }

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