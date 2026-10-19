using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Prototype.Kitchen
{
    /// <summary>
    /// Screen HUD for station minigames: title, progress bar at the top of the screen and a hint line.
    /// </summary>
    public sealed class ProtoMinigameHud : MonoBehaviour
    {
        public enum Icon { None, MouseCircle }

        /// <summary>Sprites for the "move the mouse in circles" hint (set by <see cref="ProtoKitchen"/>).</summary>
        public static Sprite MouseSprite;
        public static Sprite CircleArrowSprite;

        private const float IconOrbitRadius = 28f;
        private const float IconOrbitSpeed = 300f; // degrees per second

        private static ProtoMinigameHud s_instance;

        private RectTransform _icon;
        private RectTransform _iconMouse;
        private RectTransform _iconArrow;
        private float _iconAngle;

        private RectTransform _fill;
        private TMP_Text _title;
        private TMP_Text _hint;
        private float _shownProgress;
        private float _targetProgress;

        public static void Show(string title, string hint, Icon icon = Icon.None)
        {
            ProtoMinigameHud hud = GetOrCreate();
            hud.ShowIcon(icon);
            hud._title.text = title;
            hud._hint.text = hint;
            hud._shownProgress = hud._targetProgress = 0f;
            hud.ApplyFill(0f);
            hud.gameObject.SetActive(true);
        }

        public static void SetProgress(float progress)
        {
            if (s_instance != null)
                s_instance._targetProgress = Mathf.Clamp01(progress);
        }

        public static void Hide()
        {
            if (s_instance != null)
                s_instance.gameObject.SetActive(false);
        }

        private static ProtoMinigameHud GetOrCreate()
        {
            if (s_instance != null)
                return s_instance;

            Canvas canvas = ProtoUi.CreateOverlayCanvas("MinigameHud", 800);
            s_instance = canvas.gameObject.AddComponent<ProtoMinigameHud>();
            s_instance.Build(canvas.transform);
            return s_instance;
        }

        private void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }

        private void Build(Transform root)
        {
            TMP_FontAsset font = ProtoUi.FindSceneFont();

            RectTransform panel = ProtoUi.CreateRect("Panel", root);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 1f);
            panel.sizeDelta = new Vector2(900f, 230f);
            panel.anchoredPosition = new Vector2(0f, -30f);
            panel.gameObject.AddComponent<Image>().color = ProtoUi.PanelColor;

            _title = ProtoUi.CreateText("Title", panel, "", font, 52f);
            Place(_title.rectTransform, new Vector2(0f, -15f), new Vector2(-40f, 70f));

            RectTransform bar = ProtoUi.CreateRect("Bar", panel);
            Place(bar, new Vector2(0f, -100f), new Vector2(-80f, 46f));
            bar.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            _fill = ProtoUi.CreateRect("Fill", bar);
            _fill.anchorMin = new Vector2(0f, 0f);
            _fill.anchorMax = new Vector2(0f, 1f);
            _fill.pivot = new Vector2(0f, 0.5f);
            _fill.offsetMin = new Vector2(4f, 4f);
            _fill.offsetMax = new Vector2(4f, -4f);
            _fill.gameObject.AddComponent<Image>().color = ProtoUi.StarColor;

            _hint = ProtoUi.CreateText("Hint", panel, "", font, 32f);
            Place(_hint.rectTransform, new Vector2(0f, -165f), new Vector2(-40f, 50f));

            // Animated hint icon at the right edge of the screen: a mouse running around a circular arrow.
            _icon = ProtoUi.CreateRect("Icon", root);
            _icon.anchorMin = _icon.anchorMax = _icon.pivot = new Vector2(1f, 0.5f);
            _icon.sizeDelta = new Vector2(220f, 220f);
            _icon.anchoredPosition = new Vector2(-230f, 0f);

            _iconArrow = CreateIconImage("Arrow", _icon, CircleArrowSprite, 170f);
            _iconMouse = CreateIconImage("Mouse", _icon, MouseSprite, 110f);
            _icon.gameObject.SetActive(false);
        }

        private static RectTransform CreateIconImage(string name, RectTransform parent, Sprite sprite, float size)
        {
            RectTransform rect = ProtoUi.CreateRect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            if (sprite == null)
                image.color = new Color(1f, 1f, 1f, 0f);
            return rect;
        }

        private void ShowIcon(Icon icon)
        {
            bool show = icon == Icon.MouseCircle;
            _icon.gameObject.SetActive(show);
            if (!show)
                return;

            // Sprites may be assigned after the HUD was built.
            ApplySprite(_iconArrow, CircleArrowSprite);
            ApplySprite(_iconMouse, MouseSprite);
            _iconAngle = 0f;
        }

        private static void ApplySprite(RectTransform rect, Sprite sprite)
        {
            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 sizeDelta)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = position;
        }

        private void Update()
        {
            _shownProgress = Mathf.MoveTowards(_shownProgress, _targetProgress, Time.unscaledDeltaTime * 4f);
            ApplyFill(_shownProgress);

            if (_icon != null && _icon.gameObject.activeSelf)
            {
                // The mouse runs clockwise around the circle; the arrow turns with it.
                _iconAngle = (_iconAngle + IconOrbitSpeed * Time.unscaledDeltaTime) % 360f;
                float radians = -_iconAngle * Mathf.Deg2Rad;
                _iconMouse.anchoredPosition = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * IconOrbitRadius;
                _iconArrow.localRotation = Quaternion.Euler(0f, 0f, -_iconAngle);
            }
        }

        private void ApplyFill(float progress)
        {
            var bar = (RectTransform)_fill.parent;
            float width = Mathf.Max(0f, bar.rect.width - 8f);
            _fill.sizeDelta = new Vector2(width * progress, _fill.sizeDelta.y);
        }
    }
}
