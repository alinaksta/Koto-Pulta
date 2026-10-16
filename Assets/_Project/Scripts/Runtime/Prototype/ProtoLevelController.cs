using Game.Characters;
using Game.Progression;
using Game.Services;
using System.Collections;
using UnityEngine;

namespace Game.Prototype
{
    /// <summary>
    /// Scene setup for the Proto-level sandbox: cashier queue flow, no waiters, direct hand delivery.
    /// </summary>
    /// <remarks>
    /// - Disables every waiter in the scene (the player carries food to customers by hand).
    /// - Switches the persistent game mode to the shift mode (skips the tutorial).
    /// - Optionally starts a shift right away and sends all customers home when the shift ends.
    /// </remarks>
    public sealed class ProtoLevelController : MonoBehaviour
    {
        [SerializeField] private CustomerQueue _queue;
        [SerializeField] private bool _disableWaiters = true;
        [SerializeField] private bool _autoStartShift = true;
        [SerializeField] private string _shiftModeId = "shift";

        private ShiftService _shiftService;

        private void Awake()
        {
            if (_queue == null)
                _queue = FindAnyObjectByType<CustomerQueue>();

            if (_disableWaiters)
                DisableWaiters();
        }

        private IEnumerator Start()
        {
            // Give the persistent services one frame to finish their own startup (GameModeService auto-start).
            yield return null;

            if (ServiceLocator.TryGet<GameModeService>(out var gameModes) &&
                !string.Equals(gameModes.ActiveModeId, _shiftModeId, System.StringComparison.OrdinalIgnoreCase))
            {
                if (!gameModes.TrySetGameMode(_shiftModeId))
                    Debug.LogWarning($"[{nameof(ProtoLevelController)}] Could not switch to game mode '{_shiftModeId}'.", this);
            }

            if (ServiceLocator.TryGet(out _shiftService))
            {
                _shiftService.OnShiftEnded += HandleShiftEnded;

                if (_autoStartShift && !_shiftService.ShiftInProgress && !_shiftService.TryStartNextShift())
                    Debug.Log($"[{nameof(ProtoLevelController)}] Shift was not auto-started - start it from the computer.", this);
            }
        }

        private void OnDestroy()
        {
            if (_shiftService != null)
                _shiftService.OnShiftEnded -= HandleShiftEnded;
        }

        private void HandleShiftEnded()
        {
            if (_queue != null)
                _queue.DismissAll();
        }

        private static void DisableWaiters()
        {
            var waiters = FindObjectsByType<Waiter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var waiter in waiters)
                waiter.gameObject.SetActive(false);
        }
    }
}
