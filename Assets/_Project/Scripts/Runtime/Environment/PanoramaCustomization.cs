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

        public override CustomizationCategory Category => CustomizationCategory.Panorama;
        public Texture2D Texture => _texture;
    }
}
