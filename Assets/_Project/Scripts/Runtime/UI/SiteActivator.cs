using Game.Interaction;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Defines the available computer site tabs.
    /// </summary>
    public enum ComputerSiteTab
    {
        ShiftStatistics,
        Shop,
        Meals
    }

    /// <summary>
    /// Controls which computer site tab is currently displayed.
    /// </summary>
    public class SiteActivator : MonoBehaviour
    {
        [Serializable]
        private struct TabBinding
        {
            public ComputerSiteTab Tab;
            public RectTransform Root;
        }

        [Serializable]
        private struct TabButtonBinding
        {
            public ComputerSiteTab Tab;
            public Button Button;
            public Graphic TargetGraphic;
        }

        [SerializeField] private ComputerInteractable _computer;
        [SerializeField] private ComputerSiteTab _defaultTab = ComputerSiteTab.Meals;
        [SerializeField] private List<TabBinding> _tabs = new List<TabBinding>();
        [SerializeField] private List<TabButtonBinding> _tabButtons = new List<TabButtonBinding>();
        [SerializeField] private CanvasGroup _interactionGroup;

        [SerializeField, HideInInspector] private List<GameObject> sites = new List<GameObject>();

        private readonly Dictionary<ComputerSiteTab, RectTransform> _tabRoots = new Dictionary<ComputerSiteTab, RectTransform>();
        private readonly Dictionary<Button, ColorBlock> _tabButtonColors = new Dictionary<Button, ColorBlock>();
        private readonly HashSet<ComputerSiteTab> _disabledTabs = new HashSet<ComputerSiteTab>();

        private ComputerSiteTab _currentTab;
        private bool _hasCurrentTab;
        private bool _isFocused;

        public event Action<ComputerSiteTab> OnTabChanged = delegate { };
        public event Action<ComputerSiteTab> OnTabViewed = delegate { };
        public event Action OnComputerExited = delegate { };

        public ComputerSiteTab CurrentTab => _currentTab;
        public bool HasCurrentTab => _hasCurrentTab;
        public bool IsFocused => _isFocused;

        /// <summary>
        /// Gets whether the tab can currently be selected.
        /// </summary>
        public bool IsTabEnabled(ComputerSiteTab tab)
        {
            return !_disabledTabs.Contains(tab);
        }

        /// <summary>
        /// Enables or disables a computer tab.
        /// </summary>
        public void SetTabEnabled(ComputerSiteTab tab, bool enabled)
        {
            bool changed = enabled
                ? _disabledTabs.Remove(tab)
                : _disabledTabs.Add(tab);

            if (!changed)
                return;

            RefreshTabButtonStates();

            if (_hasCurrentTab && _currentTab == tab && !enabled)
                SelectFirstEnabledTab();
        }

        /// <summary>
        /// Enables or disables a computer tab by enum index.
        /// </summary>
        public void SetTabEnabled(int tab, bool enabled)
        {
            if (!Enum.IsDefined(typeof(ComputerSiteTab), tab))
            {
                Debug.LogWarning($"{nameof(SiteActivator)} on {name} received invalid tab index {tab}.");
                return;
            }

            SetTabEnabled((ComputerSiteTab)tab, enabled);
        }

        /// <summary>
        /// Enables a computer tab.
        /// </summary>
        public void EnableTab(ComputerSiteTab tab)
        {
            SetTabEnabled(tab, true);
        }

        /// <summary>
        /// Enables a computer tab by enum index.
        /// </summary>
        public void EnableTab(int tab)
        {
            SetTabEnabled(tab, true);
        }

        /// <summary>
        /// Disables a computer tab.
        /// </summary>
        public void DisableTab(ComputerSiteTab tab)
        {
            SetTabEnabled(tab, false);
        }

        /// <summary>
        /// Disables a computer tab by enum index.
        /// </summary>
        public void DisableTab(int tab)
        {
            SetTabEnabled(tab, false);
        }

        private void Awake()
        {
            if (_computer == null)
                _computer = GetComponent<ComputerInteractable>();

            if (_interactionGroup == null)
                _interactionGroup = GetComponent<CanvasGroup>();

            if (_interactionGroup == null)
                _interactionGroup = gameObject.AddComponent<CanvasGroup>();

            RebuildTabLookup();
            InitializeCurrentTab();
            RefreshTabButtonStates();
            SetScreenInteractable(false);
            ShowCurrentTab();
        }

        private void OnEnable()
        {
            if (_computer == null)
                _computer = GetComponent<ComputerInteractable>();

            if (_computer == null)
            {
                Debug.LogError($"{nameof(SiteActivator)} on {name} requires a {nameof(ComputerInteractable)} reference.");
                return;
            }

            _computer.FocusStarted += HandleFocusStarted;
            _computer.FocusEnded += HandleFocusEnded;
        }

        private void OnDisable()
        {
            if (_computer != null)
            {
                _computer.FocusStarted -= HandleFocusStarted;
                _computer.FocusEnded -= HandleFocusEnded;
            }

            _isFocused = false;
            SetScreenInteractable(false);
            HideAllTabs();
        }

        /// <summary>
        /// Sets the active computer site tab.
        /// </summary>
        public void SetTab(ComputerSiteTab tab)
        {
            if (!CanSelectTab(tab))
            {
                Debug.LogWarning($"{nameof(SiteActivator)} on {name} cannot select tab {tab} because it is disabled or has no site bound.");
                return;
            }

            bool changed = !_hasCurrentTab || _currentTab != tab;
            _currentTab = tab;
            _hasCurrentTab = true;

            ShowCurrentTab();

            if (changed)
            {
                RefreshTabButtonStates();
                OnTabChanged.Invoke(_currentTab);
            }

            if (_isFocused)
                OnTabViewed.Invoke(_currentTab);
        }

        /// <summary>
        /// Sets the active computer site tab by enum index.
        /// </summary>
        public void SetTab(int tab)
        {
            if (!Enum.IsDefined(typeof(ComputerSiteTab), tab))
            {
                Debug.LogWarning($"{nameof(SiteActivator)} on {name} received invalid tab index {tab}.");
                return;
            }

            SetTab((ComputerSiteTab)tab);
        }

        /// <summary>
        /// Hides every known computer site tab.
        /// </summary>
        public void HideAllTabs()
        {
            foreach (var root in GetAllKnownRoots())
            {
                if (root != null)
                    root.gameObject.SetActive(false);
            }
        }

        private void HandleFocusStarted()
        {
            _isFocused = true;

            if (!_hasCurrentTab)
                InitializeCurrentTab();

            RefreshTabButtonStates();
            SetScreenInteractable(true);
            ShowCurrentTab();

            if (_hasCurrentTab)
                OnTabViewed.Invoke(_currentTab);
        }

        private void HandleFocusEnded()
        {
            _isFocused = false;
            SetScreenInteractable(false);
            ShowCurrentTab();
            OnComputerExited.Invoke();
        }

        private void InitializeCurrentTab()
        {
            if (CanSelectTab(_defaultTab))
            {
                _currentTab = _defaultTab;
                _hasCurrentTab = true;
                RefreshTabButtonStates();
                return;
            }

            if (TryGetFirstAvailableTab(out var firstAvailableTab))
            {
                _currentTab = firstAvailableTab;
                _hasCurrentTab = true;
                RefreshTabButtonStates();
            }
        }

        private void RefreshTabButtonStates()
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                TabButtonBinding binding = _tabButtons[i];
                if (binding.Button == null)
                    continue;

                binding.Button.interactable = IsTabEnabled(binding.Tab);

                Graphic targetGraphic = binding.TargetGraphic != null
                    ? binding.TargetGraphic
                    : binding.Button.targetGraphic;

                if (targetGraphic == null)
                    continue;

                if (!_tabButtonColors.TryGetValue(binding.Button, out ColorBlock originalColors))
                {
                    originalColors = binding.Button.colors;
                    _tabButtonColors.Add(binding.Button, originalColors);
                }

                bool isSelected = IsTabEnabled(binding.Tab) && _hasCurrentTab && binding.Tab == _currentTab;
                if (isSelected)
                {
                    Color selectedColor = originalColors.selectedColor;
                    ColorBlock selectedColors = originalColors;
                    selectedColors.normalColor = selectedColor;
                    selectedColors.highlightedColor = selectedColor;
                    selectedColors.pressedColor = selectedColor;
                    binding.Button.colors = selectedColors;
                    targetGraphic.color = selectedColor;
                    continue;
                }

                binding.Button.colors = originalColors;
                targetGraphic.color = IsTabEnabled(binding.Tab)
                    ? originalColors.normalColor
                    : originalColors.disabledColor;
            }
        }

        private void SelectFirstEnabledTab()
        {
            if (TryGetFirstAvailableTab(out var firstAvailableTab))
            {
                SetTab(firstAvailableTab);
                return;
            }

            _hasCurrentTab = false;
            RefreshTabButtonStates();
            HideAllTabs();
        }

        private void SetScreenInteractable(bool interactable)
        {
            if (_interactionGroup == null)
                return;

            _interactionGroup.interactable = interactable;
            _interactionGroup.blocksRaycasts = interactable;
        }

        private void ShowCurrentTab()
        {
            HideAllTabs();

            if (!_hasCurrentTab)
                return;

            if (!TryGetRootForTab(_currentTab, out var root) || root == null)
            {
                Debug.LogWarning($"{nameof(SiteActivator)} on {name} cannot show tab {_currentTab} because no root is assigned.");
                return;
            }

            root.gameObject.SetActive(true);
            ResetScrollPosition(root);
        }

        private void RebuildTabLookup()
        {
            _tabRoots.Clear();

            for (int i = 0; i < _tabs.Count; i++)
            {
                TabBinding binding = _tabs[i];
                if (binding.Root == null)
                    continue;

                if (_tabRoots.ContainsKey(binding.Tab))
                {
                    Debug.LogWarning($"{nameof(SiteActivator)} on {name} has duplicate binding for tab {binding.Tab}. Keeping the first one.");
                    continue;
                }

                _tabRoots.Add(binding.Tab, binding.Root);
            }
        }

        private bool CanDisplayTab(ComputerSiteTab tab)
        {
            return TryGetRootForTab(tab, out var root) && root != null;
        }

        private bool CanSelectTab(ComputerSiteTab tab)
        {
            return IsTabEnabled(tab) && CanDisplayTab(tab);
        }

        private bool TryGetRootForTab(ComputerSiteTab tab, out RectTransform root)
        {
            if (_tabRoots.TryGetValue(tab, out root) && root != null)
                return true;

            if (sites.Count == 1)
            {
                root = sites[0] != null ? sites[0].transform as RectTransform : null;
                return root != null;
            }

            int tabIndex = (int)tab;
            if (tabIndex >= 0 && tabIndex < sites.Count && sites[tabIndex] != null)
            {
                root = sites[tabIndex].transform as RectTransform;
                return root != null;
            }

            root = null;
            return false;
        }

        private bool TryGetFirstAvailableTab(out ComputerSiteTab tab)
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i].Root == null || !IsTabEnabled(_tabs[i].Tab))
                    continue;

                tab = _tabs[i].Tab;
                return true;
            }

            if (sites.Count > 0)
            {
                tab = default;
                if (!IsTabEnabled(_defaultTab))
                    return false;

                tab = _defaultTab;
                return true;
            }

            tab = default;
            return false;
        }

        private IEnumerable<RectTransform> GetAllKnownRoots()
        {
            foreach (var entry in _tabRoots)
            {
                if (entry.Value != null)
                    yield return entry.Value;
            }

            for (int i = 0; i < sites.Count; i++)
            {
                if (sites[i] != null)
                    yield return sites[i].transform as RectTransform;
            }
        }

        private static void ResetScrollPosition(RectTransform root)
        {
            ScrollRect scrollRect = root.GetComponent<ScrollRect>();
            if (scrollRect == null)
                return;

            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
            scrollRect.horizontalNormalizedPosition = 0f;
        }
    }
}
