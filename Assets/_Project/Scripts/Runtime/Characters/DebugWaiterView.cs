using TMPro;
using UnityEngine;

namespace Game.Characters
{
    public class DebugWaiterView : MonoBehaviour
    {
        [SerializeField] private CatWaiter _waiter;
        [SerializeField] private TextMeshPro _text;

        private void Awake()
        {
            if (_waiter != null)
                _waiter.OnTableAssignmentChanged += OnTableAssignmentChanged;

            Refresh();
        }

        private void OnDestroy()
        {
            if (_waiter != null)
                _waiter.OnTableAssignmentChanged -= OnTableAssignmentChanged;
        }

        private void OnTableAssignmentChanged(int? table)
        {
            if (_text == null)
                return;

            Debug.Log("On Table changed");

            _text.text = table.HasValue ? table.Value.ToString() : string.Empty;
        }

        private void Refresh()
        {
            if (_text == null)
                return;

            var table = _waiter != null ? _waiter.AssignedTable : null;
            _text.text = table.HasValue ? table.Value.ToString() : string.Empty;
        }
    }
}
