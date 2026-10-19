using UnityEngine;

namespace Game.Prototype.Kitchen
{
    /// <summary>
    /// Shows an item sprite on a station (camera-facing). Supports squashing and offsets for station animations.
    /// The sprite's bottom stays on the surface it was placed on.
    /// </summary>
    public sealed class ItemSlotView : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private float _worldHeight;
        private Vector3 _restLocalPosition;
        private Vector2 _squash = Vector2.one;
        private Vector3 _worldOffset;

        public bool Visible => _renderer != null && _renderer.enabled;

        /// <param name="worldHeight">Height of the sprite in meters.</param>
        public void Init(float worldHeight)
        {
            _worldHeight = worldHeight;
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sortingOrder = 10;
            _restLocalPosition = transform.localPosition;
            Hide();
        }

        public void Show(Sprite sprite)
        {
            _renderer.sprite = sprite;
            _renderer.enabled = sprite != null;
            _squash = Vector2.one;
            _worldOffset = Vector3.zero;
            Apply();
        }

        public void Hide()
        {
            _renderer.enabled = false;
            _renderer.sprite = null;
        }

        /// <summary>
        /// Scales the sprite (x = width, y = height) relative to its normal size.
        /// </summary>
        public void SetSquash(Vector2 squash)
        {
            _squash = squash;
            Apply();
        }

        /// <summary>
        /// Moves the sprite relative to its rest position, in world space.
        /// </summary>
        public void SetWorldOffset(Vector3 offset)
        {
            _worldOffset = offset;
            Apply();
        }

        private void Apply()
        {
            if (_renderer.sprite == null)
                return;

            float spriteHeight = Mathf.Max(0.0001f, _renderer.sprite.bounds.size.y);
            float uniform = _worldHeight / spriteHeight;

            // Compensate the parent's (possibly odd FBX) scale so the sprite keeps its world size.
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            float px = Mathf.Max(0.0001f, Mathf.Abs(parentScale.x));
            float py = Mathf.Max(0.0001f, Mathf.Abs(parentScale.y));
            float pz = Mathf.Max(0.0001f, Mathf.Abs(parentScale.z));
            float horizontal = Mathf.Max(px, pz);

            transform.localScale = new Vector3(uniform * _squash.x / horizontal, uniform * _squash.y / py, uniform / horizontal);

            // Sprites pivot at their center: lift by half the visible height so the bottom rests on the surface.
            Vector3 lift = Vector3.up * (_worldHeight * _squash.y * 0.5f);
            Vector3 worldDelta = lift + _worldOffset;
            transform.localPosition = _restLocalPosition +
                (transform.parent != null ? transform.parent.InverseTransformVector(worldDelta) : worldDelta);
        }
    }
}
