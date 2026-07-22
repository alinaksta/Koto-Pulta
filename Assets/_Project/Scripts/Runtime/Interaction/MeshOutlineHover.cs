using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Marks assigned mesh renderers for the screen-space outline while hovered.
    /// </summary>
    public class MeshOutlineHover : MonoBehaviour, IHoverable
    {
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private Color _outlineColor = Color.white;
        [SerializeField, Min(1f)] private float _outlineWidth = 2f;
        [SerializeField] private bool _outlinedWhenNotHovered;

        private bool _hovered;

        private void OnEnable() => ApplyOutlineState();

        private void OnDisable() => SetOutlined(false);

        /// <inheritdoc/>
        public void OnHoverEnter()
        {
            _hovered = true;
            ApplyOutlineState();
        }

        /// <inheritdoc/>
        public void OnHoverStay(float delta) { }

        /// <inheritdoc/>
        public void OnHoverExit()
        {
            _hovered = false;
            ApplyOutlineState();
        }

        private void ApplyOutlineState() => SetOutlined(_hovered || _outlinedWhenNotHovered);

        private void SetOutlined(bool outlined)
        {
            if (_renderers == null)
                return;

            if (outlined)
                ScreenSpaceOutlineRendererFeature.SetVisuals(_outlineColor, _outlineWidth);

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                    continue;

                if (outlined)
                    _renderers[i].renderingLayerMask |= ScreenSpaceOutlineRendererFeature.RenderingLayerMask;
                else
                    _renderers[i].renderingLayerMask &= ~ScreenSpaceOutlineRendererFeature.RenderingLayerMask;
            }
        }
    }
}
