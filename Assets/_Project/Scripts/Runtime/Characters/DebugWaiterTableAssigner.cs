using TMPro;
using UnityEngine;

namespace Game.Characters
{
    public class DebugWaiterTableAssigner : MonoBehaviour
    {
        [SerializeField] private CatWaiter _waiter;
        [SerializeField] private TMP_InputField _inputField;

        public void Submit()
        {
            if (_waiter == null || _inputField == null)
                return;

            if (!int.TryParse(_inputField.text, out var table))
                return;

            _waiter.AssignTable(table);
        }
    }
}
