using UnityEngine;

namespace Game.Environment
{
    /// <summary>
    /// Defines selectable wall colors and service counter texture.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewWallCustomization",
        menuName = "Customization/Walls")]
    public class WallCustomization : CustomizationDefinition
    {
        [SerializeField] private Color _topColor = Color.white;
        [SerializeField] private Color _bottomColor = Color.white;
        [SerializeField] private Texture2D _serviceCounterTexture;

        /// <inheritdoc/>
        public override CustomizationCategory Category => CustomizationCategory.Walls;

        /// <summary>
        /// Gets the color applied to the top wall shader property.
        /// </summary>
        public Color TopColor => _topColor;

        /// <summary>
        /// Gets the color applied to the bottom wall shader property.
        /// </summary>
        public Color BottomColor => _bottomColor;

        /// <summary>
        /// Gets the texture applied to the service counter/table material.
        /// </summary>
        public Texture2D ServiceCounterTexture => _serviceCounterTexture;
    }
}
