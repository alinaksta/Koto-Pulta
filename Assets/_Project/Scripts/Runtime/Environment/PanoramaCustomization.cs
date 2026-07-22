using UnityEngine;

namespace Game.Environment
{
    /// <summary>
    /// Defines a selectable panorama/window texture.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewPanoramaCustomization",
        menuName = "Customization/Panorama")]
    public class PanoramaCustomization : CustomizationDefinition
    {
        [SerializeField] private Texture2D _texture;

        /// <inheritdoc/>
        public override CustomizationCategory Category => CustomizationCategory.Panorama;

        /// <summary>
        /// Gets the texture applied to panorama/window materials.
        /// </summary>
        public Texture2D Texture => _texture;
    }
}
