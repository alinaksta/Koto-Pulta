using Game.Animation;
using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Minimal world-space bubble above a customer: the ordered meal icon and a patience bar.
    /// Built entirely at runtime so it needs no prefab.
    /// </summary>
    public sealed class CustomerOrderBubble : MonoBehaviour
    {
        private const float BarWidth = 0.8f;
        private const float BarHeight = 0.09f;
        private const float IconSize = 0.55f;

        private static Sprite s_whiteSprite;

        private SpriteRenderer _icon;
        private SpriteRenderer _barBackground;
        private SpriteRenderer _barFill;
        private Transform _content;

        private static Sprite WhiteSprite
        {
            get
            {
                if (s_whiteSprite == null)
                {
                    var texture = Texture2D.whiteTexture;
                    s_whiteSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0f, 0.5f), texture.width);
                }

                return s_whiteSprite;
            }
        }

        /// <summary>
        /// Creates a bubble as a child of <paramref name="parent"/>.
        /// </summary>
        public static CustomerOrderBubble Create(Transform parent, float height, Material material)
        {
            var root = new GameObject("OrderBubble");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, height, 0f);
            root.AddComponent<SpriteRotator>();

            var bubble = root.AddComponent<CustomerOrderBubble>();
            bubble.Build(material);
            bubble.Hide();
            return bubble;
        }

        private void Build(Material material)
        {
            // SpriteRotator points +Z at the camera; flip the content so sprites read left-to-right.
            _content = new GameObject("Content").transform;
            _content.SetParent(transform, false);
            _content.localRotation = Quaternion.Euler(0f, 180f, 0f);

            _barBackground = CreateRenderer("BarBackground", material, 100);
            _barBackground.sprite = WhiteSprite;
            _barBackground.color = new Color(0f, 0f, 0f, 0.6f);
            _barBackground.transform.localPosition = new Vector3(-BarWidth * 0.5f, 0f, 0f);
            _barBackground.transform.localScale = new Vector3(BarWidth, BarHeight, 1f);

            _barFill = CreateRenderer("BarFill", material, 101);
            _barFill.sprite = WhiteSprite;
            _barFill.transform.localPosition = new Vector3(-BarWidth * 0.5f, 0f, -0.001f);

            _icon = CreateRenderer("Icon", material, 102);
            _icon.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        }

        private SpriteRenderer CreateRenderer(string objectName, Material material, int sortingOrder)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(_content, false);
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            if (material != null)
                spriteRenderer.sharedMaterial = material;
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }

        /// <summary>
        /// Shows the bubble. Pass a null icon to hide the icon, or a null progress to hide the bar.
        /// </summary>
        public void Show(Sprite icon, float? normalizedPatience)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            _icon.enabled = icon != null;
            if (icon != null && _icon.sprite != icon)
            {
                _icon.sprite = icon;
                Vector2 size = icon.bounds.size;
                float scale = IconSize / Mathf.Max(0.0001f, Mathf.Max(size.x, size.y));
                _icon.transform.localScale = new Vector3(scale, scale, 1f);
            }

            bool showBar = normalizedPatience.HasValue;
            _barBackground.enabled = showBar;
            _barFill.enabled = showBar;

            if (showBar)
            {
                float value = Mathf.Clamp01(normalizedPatience.Value);
                _barFill.transform.localScale = new Vector3(BarWidth * value, BarHeight * 0.7f, 1f);
                _barFill.color = value > 0.5f
                    ? Color.Lerp(Color.yellow, Color.green, (value - 0.5f) * 2f)
                    : Color.Lerp(Color.red, Color.yellow, value * 2f);
            }
        }

        /// <summary>
        /// Hides the bubble.
        /// </summary>
        public void Hide()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }
    }
}
