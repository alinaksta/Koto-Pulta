using UnityEngine;

namespace Game.Characters
{
    /// <summary>
    /// Marks a trigger area as belonging to a table.
    /// </summary>
    public class TableZone : MonoBehaviour
    {
        [SerializeField] private Table _table;

        /// <summary>
        /// Gets the table associated with this zone.
        /// </summary>
        public Table Table => _table;

        private void Awake()
        {
            _table ??= GetComponent<Table>();
        }
    }
}
