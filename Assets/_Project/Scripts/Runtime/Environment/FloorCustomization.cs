using UnityEngine;

namespace Game.Environment
{
    /// <summary>
    /// Defines selectable floor and door textures.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewFloorCustomization",
        menuName = "Customization/Floor")]
    public class FloorCustomization : CustomizationDefinition
    {
        [SerializeField] private Texture2D _floorTexture;
        [SerializeField] private Texture2D _doorTexture;

        /// <inheritdoc/>
        public override CustomizationCategory Category => CustomizationCategory.Floor;

        /// <summary>
        /// Gets the texture applied to the floor material.
        /// </summary>
        public Texture2D FloorTexture => _floorTexture;

        /// <summary>
        /// Gets the texture applied to the door material.
        /// </summary>
        public Texture2D DoorTexture => _doorTexture;
    }
}
