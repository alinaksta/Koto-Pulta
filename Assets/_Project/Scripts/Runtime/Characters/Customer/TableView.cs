using TMPro;
using UnityEngine;

namespace Game.Characters
{
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
