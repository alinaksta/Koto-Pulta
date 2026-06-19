using UnityEngine;

namespace Game.Characters
{
    public class TableZone : MonoBehaviour
    {
        [SerializeField] private Table _table;

        public Table Table => _table;

        private void Awake()
        {
            _table ??= GetComponent<Table>();
        }
    }
}