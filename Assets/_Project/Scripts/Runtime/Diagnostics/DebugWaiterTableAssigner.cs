using TMPro;
using Game.Characters;
using UnityEngine;

namespace Game.Diagnostics
{
    public class DebugWaiterTableAssigner : MonoBehaviour
    {
        [SerializeField] private Waiter _waiter;
        [SerializeField] private TMP_InputField _inputField;

        public void Submit()
        {
            if (_waiter == null || _inputField == null)
                return;

            if (!int.TryParse(_inputField.text, out var table))
                return;

            // TODO: connect to spawn customer at table <number> and assign him to waiter
        }
    }
}
