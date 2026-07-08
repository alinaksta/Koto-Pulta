using Game.Progression;
using Game.Services;
using UnityEngine;

namespace Game.Diagnostics
{
    /// <summary>
    /// Provides debug controls for starting shifts.
    /// </summary>
    public class DebugShiftComputer : MonoBehaviour
    {
        private ShiftService _shiftService;

        private void Awake()
        {
            _shiftService = ServiceLocator.Get<ShiftService>();
        }

        /// <summary>
        /// Attempts to start the next shift from the debug control.
        /// </summary>
        public void StartShift()
        {
            _shiftService.StartNextShift();
        }
    }
}
