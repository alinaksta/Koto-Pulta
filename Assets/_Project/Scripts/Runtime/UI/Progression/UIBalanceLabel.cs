using Game.Progression;
using Game.Services;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Displays the current balance using a customizable format string.
    /// </summary>
    public class UIBalanceLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private string _format = "Balance {0}";

        private BalanceService _balanceService;

        private void Awake()
        {
            TryResolveService();
        }

        private void OnEnable()
        {
            if (!TryResolveService())
                return;

            _balanceService.OnBalanceChanged += HandleBalanceChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (_balanceService == null)
                return;

            _balanceService.OnBalanceChanged -= HandleBalanceChanged;
        }

        private void Update()
        {
            if (_balanceService != null)
                return;

            if (!TryResolveService())
                return;

            _balanceService.OnBalanceChanged += HandleBalanceChanged;
            Refresh();
        }

        private void HandleBalanceChanged(int _)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_label == null || _balanceService == null)
                return;

            _label.text = string.Format(_format, _balanceService.Balance);
        }

        private bool TryResolveService()
        {
            if (_balanceService != null)
                return true;

            return ServiceLocator.TryGet(out _balanceService);
        }
    }
}
