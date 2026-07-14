using UnityEngine;

namespace Game.Environment
{
    /// <summary>
    /// Defines selectable floor and door textures.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewFloorCustomization",
        menuName = "Customization/Floor")]
    public class FloorCustomization : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private Texture2D _floorTexture;
        [SerializeField] private Texture2D _doorTexture;

        public string Name => _name;
        public Texture2D FloorTexture => _floorTexture;
        public Texture2D DoorTexture => _doorTexture;
    }
}
