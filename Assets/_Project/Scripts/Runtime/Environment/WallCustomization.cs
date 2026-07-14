using UnityEngine;

namespace Game.Environment
{
    /// <summary>
    /// Defines selectable wall colors and service counter texture.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewWallCustomization",
        menuName = "Customization/Walls")]
    public class WallCustomization : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private Color _topColor = Color.white;
        [SerializeField] private Color _bottomColor = Color.white;
        [SerializeField] private Texture2D _serviceCounterTexture;

        public string Name => _name;
        public Color TopColor => _topColor;
        public Color BottomColor => _bottomColor;
        public Texture2D ServiceCounterTexture => _serviceCounterTexture;
    }
}
