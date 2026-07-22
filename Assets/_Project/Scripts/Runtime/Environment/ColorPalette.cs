using System.Collections.Generic;
using UnityEngine;

namespace Game.Environment
{
    /// <summary>
    /// Stores selectable colors for environment recoloring.
    /// </summary>
    [CreateAssetMenu(fileName = "NewColorPalette", menuName = "Color Palette")]
    public class ColorPalette : ScriptableObject
    {
        public List<Color> Colors;
    }
}