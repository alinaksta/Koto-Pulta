using System.Collections.Generic;
using UnityEngine;

namespace Game.Environment
{
    public class BatchRecolor : MonoBehaviour
    {
        private static int _counter = 0;

        [SerializeField] private ColorPalette _palette;
        [SerializeField] private List<SpriteRenderer> _targets;

        private void Start()
        {
            if (_targets.Count == 0)
                return;

            _counter %= _palette.Colors.Count;
            var color = _palette.Colors[_counter];
            var material = _targets[0].material;
            material.color = color;

            foreach (var target in _targets)
            {
                target.material = material;
            }

            _counter++;
        }

        private Color GetRandomColor()
        {
            int randomIndex = UnityEngine.Random.Range(0, _palette.Colors.Count);
            return _palette.Colors[randomIndex];
        }
    }
}