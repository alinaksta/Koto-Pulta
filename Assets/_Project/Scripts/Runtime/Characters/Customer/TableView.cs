using TMPro;
using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Updates table visuals based on customer occupancy.
    /// </summary>
    public class TableView : MonoBehaviour
    {
        [SerializeField] private Table _table;
        [SerializeField] private TextMeshPro _tableNumberText;

        private void Awake()
        {
            if (_tableNumberText == null)
                _tableNumberText = GetComponent<TextMeshPro>();
        }

        private void Start()
        {
            _tableNumberText.text = _table.TableNumber.ToString();
        }
    }
}
