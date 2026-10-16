using Game.Progression;
using Game.Services;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.Prototype
{
    /// <summary>
    /// Sandbox main menu: "Play" opens a level-select table (level, stars, select) over the same background.
    /// </summary>
    public sealed class ProtoMainMenuController : MonoBehaviour
    {
        [Tooltip("Objects hidden while level select is open (main menu buttons, logo). Found automatically when empty.")]
        [SerializeField] private GameObject[] _mainMenuObjects = new GameObject[0];
        [SerializeField] private string _logoObjectName = "KotoPultaLogo";
        [SerializeField] private int _fallbackLevelCount = 5;

        private Button _playButton;
        private Canvas _canvas;
        private TMP_FontAsset _font;
        private RectTransform _levelSelectRoot;
        private RectTransform _rowsRoot;

        private void Start()
        {
            var playButtonBehaviour = FindAnyObjectByType<PlayButton>(FindObjectsInactive.Include);
            if (playButtonBehaviour == null)
            {
                Debug.LogError($"[{nameof(ProtoMainMenuController)}] No PlayButton found in the scene.", this);
                return;
            }

            _playButton = playButtonBehaviour.GetComponent<Button>();
            _canvas = playButtonBehaviour.GetComponentInParent<Canvas>().rootCanvas;
            _font = playButtonBehaviour.GetComponentInChildren<TMP_Text>(true)?.font;

            // Redirect "Play" to level select instead of loading the gameplay scene directly.
            for (int i = 0; i < _playButton.onClick.GetPersistentEventCount(); i++)
                _playButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
            _playButton.onClick.AddListener(ShowLevelSelect);
            playButtonBehaviour.enabled = false;

            if (_mainMenuObjects == null || _mainMenuObjects.Length == 0)
            {
                var logo = GameObject.Find(_logoObjectName);
                _mainMenuObjects = logo != null
                    ? new[] { _playButton.transform.parent.gameObject, logo }
                    : new[] { _playButton.transform.parent.gameObject };
            }

            BuildLevelSelect();

            if (ProtoProgress.OpenLevelSelectOnMenuLoad)
            {
                ProtoProgress.OpenLevelSelectOnMenuLoad = false;
                ShowLevelSelect();
            }
            else
            {
                ShowMainMenu();
            }
        }

        public void ShowLevelSelect()
        {
            SetMainMenuVisible(false);
            RefreshRows();
            _levelSelectRoot.gameObject.SetActive(true);
        }

        public void ShowMainMenu()
        {
            if (_levelSelectRoot != null)
                _levelSelectRoot.gameObject.SetActive(false);
            SetMainMenuVisible(true);
        }

        private void SetMainMenuVisible(bool visible)
        {
            foreach (var go in _mainMenuObjects)
            {
                if (go != null)
                    go.SetActive(visible);
            }
        }

        private int LevelCount
            => ServiceLocator.TryGet(out ShiftService shifts) && shifts.ShiftAmount > 0 ? shifts.ShiftAmount : _fallbackLevelCount;

        #region UI building
        private void BuildLevelSelect()
        {
            _levelSelectRoot = ProtoUi.CreateRect("LevelSelect", _canvas.transform);
            ProtoUi.Stretch(_levelSelectRoot);
            _levelSelectRoot.SetAsLastSibling();

            // Back to main menu, top-left corner.
            Button back = ProtoUi.CreateButton("BackButton", _levelSelectRoot, _playButton, "Back", _font, new Vector2(260f, 90f), ShowMainMenu);
            var backRect = (RectTransform)back.transform;
            backRect.anchorMin = backRect.anchorMax = backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(40f, -40f);

            RectTransform panel = ProtoUi.CreatePanel("Table", _levelSelectRoot, new Vector2(1000f, 760f), ProtoUi.PanelColor);

            TMP_Text title = ProtoUi.CreateText("Title", panel, "Select level", _font, 64f);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.sizeDelta = new Vector2(0f, 110f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -20f);

            _rowsRoot = ProtoUi.CreateRect("Rows", panel);
            _rowsRoot.anchorMin = Vector2.zero;
            _rowsRoot.anchorMax = Vector2.one;
            _rowsRoot.offsetMin = new Vector2(40f, 40f);
            _rowsRoot.offsetMax = new Vector2(-40f, -140f);

            var layout = _rowsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateRow(_rowsRoot, "Header", "Level", "Stars", null, -1);

            _levelSelectRoot.gameObject.SetActive(false);
        }

        private void RefreshRows()
        {
            // Rebuild level rows (keep the header at index 0) so stars are always current.
            for (int i = _rowsRoot.childCount - 1; i >= 1; i--)
                Destroy(_rowsRoot.GetChild(i).gameObject);

            int count = LevelCount;
            for (int i = 0; i < count; i++)
            {
                int levelIndex = i;
                string stars = ProtoUi.FormatStars(ProtoProgress.GetBestStars(levelIndex), 3, _font);
                CreateRow(_rowsRoot, $"Level {levelIndex + 1}", (levelIndex + 1).ToString(), stars, () => SelectLevel(levelIndex), levelIndex);
            }
        }

        private void CreateRow(Transform parent, string name, string levelText, string starsText, UnityAction onSelect, int levelIndex)
        {
            RectTransform row = ProtoUi.CreateRect(name, parent);
            bool isHeader = onSelect == null;
            if (!isHeader)
                row.gameObject.AddComponent<Image>().color = ProtoUi.RowColor;
            ProtoUi.SetLayoutSize(row, -1f, isHeader ? 60f : 90f);

            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 0, 0);
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            float fontSize = isHeader ? 34f : 42f;

            TMP_Text level = ProtoUi.CreateText("Level", row, levelText, _font, fontSize);
            ProtoUi.SetLayoutSize(level, 200f, 80f);

            TMP_Text stars = ProtoUi.CreateText("Stars", row, starsText, _font, fontSize);
            if (!isHeader)
                stars.color = ProtoUi.StarColor;
            ProtoUi.SetLayoutSize(stars, -1f, 80f, 1f);

            if (isHeader)
            {
                TMP_Text spacer = ProtoUi.CreateText("Action", row, "", _font, fontSize);
                ProtoUi.SetLayoutSize(spacer, 240f, 60f);
                return;
            }

            ProtoUi.CreateButton("Select", row, _playButton, "Play", _font, new Vector2(240f, 76f), onSelect);
        }
        #endregion

        private void SelectLevel(int levelIndex)
        {
            ProtoProgress.SelectedLevelIndex = levelIndex;
            ProtoProgress.LoadScene(ProtoProgress.LevelSceneName);
        }
    }
}
