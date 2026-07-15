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

        public override CustomizationCategory Category => CustomizationCategory.Floor;
        public Texture2D FloorTexture => _floorTexture;
        public Texture2D DoorTexture => _doorTexture;
    }
}
