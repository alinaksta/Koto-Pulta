using Game.Progression;
using Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Binds one upgrade row to progression and balance state.
    /// </summary>
    public sealed class UpgradeView : MonoBehaviour
    {
        [SerializeField] private UpgradeType _upgrade;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private TMP_Text _costLabel;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private GameObject _maxLevelObject;

        private UpgradeService _upgrades;
        private BalanceService _balance;

        private void OnEnable()
        {
            ServiceLocator.TryGet(out _upgrades);
            ServiceLocator.TryGet(out _balance);

            if (_upgrades != null)
                _upgrades.OnUpgradeChanged += HandleUpgradeChanged;

            if (_balance != null)
                _balance.OnBalanceChanged += HandleBalanceChanged;

            if (_upgradeButton != null)
                _upgradeButton.onClick.AddListener(HandleUpgradeClicked);

            Refresh();
        }

        private void OnDisable()
        {
            if (_upgrades != null)
                _upgrades.OnUpgradeChanged -= HandleUpgradeChanged;

            if (_balance != null)
                _balance.OnBalanceChanged -= HandleBalanceChanged;

            if (_upgradeButton != null)
                _upgradeButton.onClick.RemoveListener(HandleUpgradeClicked);
        }

        private void HandleUpgradeClicked()
        {
            _upgrades?.TryUpgrade(_upgrade);
        }

        private void HandleUpgradeChanged(UpgradeType type, int level)
        {
            if (type == _upgrade)
                Refresh();
        }

        private void HandleBalanceChanged(int balance)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_upgrades == null)
                return;

            int level = _upgrades.GetLevel(_upgrade);
            int maxLevel = _upgrades.GetMaxLevel(_upgrade);
            bool isMaxLevel = level >= maxLevel;

            if (_progressSlider != null)
            {
                _progressSlider.minValue = 1f;
                _progressSlider.maxValue = maxLevel;
                _progressSlider.wholeNumbers = true;
                _progressSlider.interactable = false;
                _progressSlider.SetValueWithoutNotify(level);
            }

            if (_levelLabel != null)
                _levelLabel.text = $"{level} / {maxLevel}";

            if (_costLabel != null)
                _costLabel.text = isMaxLevel ? string.Empty : _upgrades.GetNextCost(_upgrade).ToString();

            if (_upgradeButton != null)
            {
                _upgradeButton.gameObject.SetActive(!isMaxLevel);
                _upgradeButton.interactable = !isMaxLevel && _upgrades.CanUpgrade(_upgrade);
            }

            if (_maxLevelObject != null)
                _maxLevelObject.SetActive(isMaxLevel);
        }
    }
}
