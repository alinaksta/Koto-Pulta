using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Prototype.Kitchen
{
    /// <summary>
    /// Builds the order note from the prototype art (Art/Prototype/UI/OrderNote.png):
    /// top band = patience timer, big cell = the dish and "#table", two bottom cells = the ingredients.
    /// </summary>
    public static class ProtoOrderNote
    {
        // Layout in pixels of the 449 x 443 art, from its top-left corner.
        private const float ArtWidth = 449f;
        private const float ArtHeight = 443f;

        public static OrderNoteView BuildTemplate(Transform parent, Sprite frame, Vector2 size)
        {
            var root = new GameObject("ProtoOrderNote", typeof(RectTransform));
            root.SetActive(false); // template
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            var layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;

            Image background = CreateImage("Frame", rect, frame, 0f, 0f, ArtWidth, ArtHeight);
            background.preserveAspect = false;

            Image timer = CreateImage("Timer", rect, WhiteSprite, 6f, 4f, 443f, 76f);
            timer.type = Image.Type.Filled;
            timer.fillMethod = Image.FillMethod.Horizontal;
            timer.fillOrigin = (int)Image.OriginHorizontal.Left;
            // The bar spans the whole top band and shrinks toward the left edge.
            timer.preserveAspect = false;

            TMP_Text label = CreateLabel(rect);
            Image dish = CreateImage("Dish", rect, null, 111f, 31f, 363f, 321f);
            Image first = CreateImage("Ingredient1", rect, null, 75f, 350f, 205f, 432f);
            Image second = CreateImage("Ingredient2", rect, null, 248f, 350f, 378f, 432f);

            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    // Fill amount = patience left: yellow when full, slowly turning red as it runs out.
                    new GradientColorKey(new Color(0.88f, 0.22f, 0.2f), 0f),
                    new GradientColorKey(new Color(0.96f, 0.55f, 0.25f), 0.45f),
                    new GradientColorKey(new Color(0.96f, 0.86f, 0.37f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            var note = root.AddComponent<OrderNoteView>();
            note.Setup(dish, timer, gradient, label, new[] { first, second }, "#{0}");
            return note;
        }

        private static Sprite s_white;

        private static Sprite WhiteSprite => s_white != null
            ? s_white
            : s_white = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));

        private static TMP_Text CreateLabel(RectTransform parent)
        {
            var go = new GameObject("Number", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Place(rect, 38f, 225f, 230f, 315f);

            var label = go.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
                label.font = TMP_Settings.defaultFontAsset;
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 72f;
            label.alignment = TextAlignmentOptions.BottomLeft;
            label.color = new Color(0.3f, 0.3f, 0.3f);
            label.raycastTarget = false;
            label.enabled = false;
            return label;
        }

        private static Image CreateImage(string name, RectTransform parent, Sprite sprite, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Place(rect, x0, y0, x1, y1);

            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            return image;
        }

        /// <summary>Stretches the rect over an area of the art (pixels from the top-left corner).</summary>
        private static void Place(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0 / ArtWidth, 1f - y1 / ArtHeight);
            rect.anchorMax = new Vector2(x1 / ArtWidth, 1f - y0 / ArtHeight);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
