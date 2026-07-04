using System.Collections.Generic;
using UnityEngine;

namespace Game.Environment
{
    [CreateAssetMenu(fileName = "NewColorPalette", menuName = "Color Palette")]
    public class ColorPalette : ScriptableObject
    {
        public List<Color> Colors;
    }
}