using Game.Progression;
using Game.Services;
using UnityEngine;

namespace Game.Diagnostics
{
    public class DebugShiftComputer : MonoBehaviour
    {
        private ShiftService _shiftService;

        private void Awake()
        {
            _shiftService = ServiceLocator.Get<ShiftService>();
        }

        public void StartShift()
        {
            _shiftService.StartNextShift();
        }
    }
}
