using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Prototype
{
    /// <summary>
    /// Small screen effects for the prototype: click ripples and floating "+$10" labels.
    /// </summary>
    public sealed class ProtoUiFx : MonoBehaviour
    {
        private static ProtoUiFx s_instance;
        private static Sprite s_ring;
        private RectTransform _root;

        private static ProtoUiFx Instance
        {
            get
            {
                if (s_instance != null)
                    return s_instance;

                Canvas canvas = ProtoUi.CreateOverlayCanvas("UiFx", 900);
                // Effects never block clicks.
                Destroy(canvas.GetComponent<GraphicRaycaster>());
                s_instance = canvas.gameObject.AddComponent<ProtoUiFx>();
                s_instance._root = (RectTransform)canvas.transform;
                return s_instance;
            }
        }

        private void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }

        /// <summary>Where the player points: the mouse when the cursor is free, the screen center when it is locked.</summary>
        public static Vector2 PointerScreenPosition()
        {
            if (Cursor.lockState != CursorLockMode.Locked && Mouse.current != null)
                return Mouse.current.position.ReadValue();
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        #region Ripple
        /// <summary>Expanding, fading rings at a screen point.</summary>
        public static void Ripple(Vector2 screenPosition, Color color, int rings = 2, float size = 140f, float duration = 0.45f)
        {
            ProtoUiFx fx = Instance;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(fx._root, screenPosition, null, out Vector2 local);
            for (int i = 0; i < rings; i++)
                fx.StartCoroutine(fx.RippleRoutine(local, color, size * (1f - 0.25f * i), duration, i * 0.08f));
        }

        private IEnumerator RippleRoutine(Vector2 position, Color color, float size, float duration, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            RectTransform rect = ProtoUi.CreateRect("Ripple", _root);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = RingSprite;
            image.raycastTarget = false;

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                float eased = 1f - (1f - t) * (1f - t);
                rect.sizeDelta = Vector2.one * Mathf.Lerp(size * 0.15f, size, eased);
                image.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
                yield return null;
            }

            Destroy(rect.gameObject);
        }

        private static Sprite RingSprite
        {
            get
            {
                if (s_ring != null)
                    return s_ring;

                const int size = 128;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                float center = (size - 1) * 0.5f;
                float outer = size * 0.5f - 1f;
                float inner = outer - size * 0.09f;
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                        // Soft edges on both sides of the ring.
                        float a = Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                s_ring = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                return s_ring;
            }
        }
        #endregion

        #region World popup
        /// <summary>
        /// A label in the world (e.g. "+10$" above a cat) that turns to the camera, floats up and fades.
        /// </summary>
        public static void FloatingWorldText(Vector3 position, string text, Color color, float size = 4f,
            float rise = 0.6f, float duration = 1.2f)
        {
            ProtoUiFx fx = Instance;
            var go = new GameObject("FloatingWorldText");
            var label = go.AddComponent<TextMeshPro>();
            TMP_FontAsset font = ProtoUi.FindSceneFont();
            if (font != null)
                label.font = font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.outlineWidth = 0.2f;
            label.outlineColor = new Color32(0, 0, 0, 200);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            go.transform.position = position;
            fx.StartCoroutine(fx.WorldFloatRoutine(label, position, rise, duration, color));
        }

        private IEnumerator WorldFloatRoutine(TextMeshPro label, Vector3 start, float rise, float duration, Color color)
        {
            for (float t = 0f; t < 1f && label != null; t += Time.deltaTime / duration)
            {
                float eased = 1f - (1f - t) * (1f - t);
                label.transform.position = start + Vector3.up * (rise * eased);

                Camera camera = Camera.main;
                if (camera != null)
                    label.transform.rotation = camera.transform.rotation;

                float alpha = t < 0.35f ? 1f : 1f - (t - 0.35f) / 0.65f;
                label.color = new Color(color.r, color.g, color.b, color.a * alpha);
                yield return null;
            }

            if (label != null)
                Destroy(label.gameObject);
        }
        #endregion

        #region Floating text
        /// <summary>
        /// A label that appears next to <paramref name="anchor"/> (left of its text), floats up and fades.
        /// </summary>
        public static void FloatingText(TMP_Text anchor, string text, Color color, float rise = 80f, float duration = 1.1f)
        {
            if (anchor == null)
                return;

            ProtoUiFx fx = Instance;
            TMP_Text label = Instantiate(anchor, anchor.transform.parent, false);
            label.name = "FloatingText";
            // The copy must not be driven by the HUD scripts that update the original.
            foreach (MonoBehaviour behaviour in label.GetComponents<MonoBehaviour>())
            {
                if (behaviour != label)
                    Destroy(behaviour);
            }
            for (int i = label.transform.childCount - 1; i >= 0; i--)
                Destroy(label.transform.GetChild(i).gameObject);

            label.text = text;
            label.color = color;
            label.raycastTarget = false;

            RectTransform rect = label.rectTransform;
            float shift = anchor.preferredWidth + 24f;
            Vector2 start = anchor.rectTransform.anchoredPosition - new Vector2(shift, 0f);
            rect.anchoredPosition = start;

            fx.StartCoroutine(fx.FloatRoutine(label, start, rise, duration, color));
        }

        private IEnumerator FloatRoutine(TMP_Text label, Vector2 start, float rise, float duration, Color color)
        {
            for (float t = 0f; t < 1f && label != null; t += Time.unscaledDeltaTime / duration)
            {
                float eased = 1f - (1f - t) * (1f - t);
                label.rectTransform.anchoredPosition = start + Vector2.up * (rise * eased);
                // Fully visible for the first third, then fades out.
                float alpha = t < 0.35f ? 1f : 1f - (t - 0.35f) / 0.65f;
                label.color = new Color(color.r, color.g, color.b, color.a * alpha);
                yield return null;
            }

            if (label != null)
                Destroy(label.gameObject);
        }
        #endregion
    }
}
