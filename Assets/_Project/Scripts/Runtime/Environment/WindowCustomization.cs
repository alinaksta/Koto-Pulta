using UnityEngine;

namespace Game.Environment
{
    /// <summary>
    /// Defines a selectable window texture.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewWindowCustomization",
        menuName = "Customization/Windows")]
    public class WindowCustomization : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private Texture2D _texture;

        public string Name => _name;
        public Texture2D Texture => _texture;
    }
}
