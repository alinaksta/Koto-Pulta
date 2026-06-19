using TMPro;
using Game.Characters;
using UnityEngine;

namespace Game.Diagnostics
{
    /// <summary>
    /// Debug UI hook for entering a target table number for a waiter.
    /// </summary>
    public class DebugWaiterTableAssigner : MonoBehaviour
    {
        [SerializeField] private Waiter _waiter;
        [SerializeField] private TMP_InputField _inputField;

        /// <summary>
        /// Reads the input field and submits the requested table number.
        /// </summary>
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
