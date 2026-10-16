using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.Prototype
{
    /// <summary>
    /// Small helpers for building placeholder uGUI screens from code.
    /// </summary>
    public static class ProtoUi
    {
        public static readonly Color PanelColor = new Color(0.1f, 0.08f, 0.12f, 0.88f);
        public static readonly Color RowColor = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color TextColor = Color.white;
        public static readonly Color StarColor = new Color(1f, 0.82f, 0.2f);

        public static Canvas CreateOverlayCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static RectTransform CreatePanel(string name, Transform parent, Vector2 size, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.gameObject.AddComponent<Image>().color = color;
            return rect;
        }

        public static TMP_Text CreateText(string name, Transform parent, string text, TMP_FontAsset font, float size,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
                label.font = font;
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = TextColor;
            label.raycastTarget = false;
            return label;
        }

        public static LayoutElement SetLayoutSize(Component component, float width, float height, float flexibleWidth = -1f)
        {
            var element = component.GetComponent<LayoutElement>();
            if (element == null)
                element = component.gameObject.AddComponent<LayoutElement>();

            element.preferredWidth = width;
            element.minWidth = width > 0f ? width : -1f;
            element.preferredHeight = height;
            element.minHeight = height > 0f ? height : -1f;
            element.flexibleWidth = flexibleWidth;
            return element;
        }

        /// <summary>
        /// Creates a button. When a template is supplied it is cloned so the button matches the existing style.
        /// </summary>
        public static Button CreateButton(string name, Transform parent, Button template, string label, TMP_FontAsset font,
            Vector2 size, UnityAction onClick)
        {
            Button button;

            if (template != null)
            {
                GameObject clone = Object.Instantiate(template.gameObject, parent, false);
                clone.name = name;
                clone.SetActive(true);

                // Drop scene-specific behaviours (e.g. PlayButton/ExitButton) and persistent click handlers.
                foreach (var behaviour in clone.GetComponents<MonoBehaviour>())
                {
                    if (behaviour is Button || behaviour is Graphic || behaviour is LayoutElement)
                        continue;
                    Object.Destroy(behaviour);
                }

                button = clone.GetComponent<Button>();
                button.onClick = new Button.ButtonClickedEvent();
                button.interactable = true;

                var cloneLabel = clone.GetComponentInChildren<TMP_Text>(true);
                if (cloneLabel != null)
                    cloneLabel.text = label;
            }
            else
            {
                RectTransform rect = CreateRect(name, parent);
                var image = rect.gameObject.AddComponent<Image>();
                image.color = new Color(0.95f, 0.55f, 0.35f);
                button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = image;

                TMP_Text text = CreateText("Label", rect, label, font, 36f);
                Stretch(text.rectTransform);
            }

            var buttonRect = (RectTransform)button.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.localScale = Vector3.one;
            buttonRect.sizeDelta = size;
            SetLayoutSize(button, size.x, size.y);

            if (onClick != null)
                button.onClick.AddListener(onClick);

            return button;
        }

        /// <summary>
        /// Formats a star rating, using star glyphs when the font has them.
        /// </summary>
        public static string FormatStars(int stars, int maxStars, TMP_FontAsset font)
        {
            bool hasGlyphs = font != null && font.HasCharacter('★', true) && font.HasCharacter('☆', true);
            if (!hasGlyphs)
                return $"{stars} / {maxStars}";

            var builder = new System.Text.StringBuilder();
            for (int i = 0; i < maxStars; i++)
                builder.Append(i < stars ? '★' : '☆');
            return builder.ToString();
        }

        /// <summary>
        /// Finds a TMP font already used in the loaded scenes.
        /// </summary>
        public static TMP_FontAsset FindSceneFont()
        {
            var texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var text in texts)
            {
                if (text.font != null)
                    return text.font;
            }

            return TMP_Settings.defaultFontAsset;
        }
    }
}
