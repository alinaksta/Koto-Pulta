using Game.Progression;
using Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.Prototype
{
    /// <summary>
    /// Sandbox main menu (Play / Settings / Exit) and level select, built from the art in Art/Prototype/UI.
    /// The old menu content of the canvas is hidden.
    /// </summary>
    public sealed class ProtoMainMenuController : MonoBehaviour
    {
        private static readonly Color InkColor = new Color32(100, 113, 126, 255);

        [Header("Art (Art/Prototype/UI)")]
        [SerializeField] private Sprite _menuBackground;      // Frame 126
        [SerializeField] private Sprite _title;               // Frame 127
        [SerializeField] private Sprite _levelSelectBackground; // Frame 128
        [SerializeField] private Sprite _button;              // Frame 130 (unused for now)
        [SerializeField] private Sprite _levelTile;           // Frame 131
        [SerializeField] private Sprite _lockedTile;          // Frame 132
        [SerializeField] private Sprite _starFull;            // Frame 133
        [SerializeField] private Sprite _starEmpty;           // Frame 134
        [SerializeField] private Sprite _plate;               // Frame 135
        [SerializeField] private Sprite _playButton;          // Frame 140
        [SerializeField] private TMP_FontAsset _font;

        [Header("Levels")]
        [SerializeField] private int _fallbackLevelCount = 5;
        [SerializeField] private int _columns = 5;
        [SerializeField] private int _rows = 2;
        [SerializeField] private Vector2 _tileSpacing = new Vector2(352f, 430f);
        [SerializeField] private float _firstRowY = 145f;
        [Tooltip("When on, a level opens after the previous one got at least one star.")]
        [SerializeField] private bool _lockLevels = true;

        private RectTransform _root;
        private RectTransform _mainMenu;
        private RectTransform _levelSelect;
        private RectTransform _grid;

        private void Start()
        {
#if UNITY_EDITOR
            ResolveMissingReferences();
#endif
            if (_font == null)
                _font = ProtoUi.FindSceneFont();

            Canvas canvas = FindCanvas();
            if (canvas == null)
            {
                Debug.LogError($"[{nameof(ProtoMainMenuController)}] No canvas in the scene.", this);
                return;
            }

            // Hide the old menu content; the new menu is built on the same canvas.
            foreach (Transform child in canvas.transform)
                child.gameObject.SetActive(false);

            _root = ProtoUi.CreateRect("ProtoMenu", canvas.transform);
            ProtoUi.Stretch(_root);

            BuildMainMenu();
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

        private Canvas FindCanvas()
        {
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                // Only this scene's canvas: persistent ones (e.g. the loading curtain) must not be touched.
                if (canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.gameObject.scene == gameObject.scene)
                    return canvas;
            }

            return ProtoUi.CreateOverlayCanvas("MenuCanvas", 0);
        }

        #region Screens
        public void ShowMainMenu() => Show(_mainMenu);

        public void ShowLevelSelect()
        {
            RefreshLevels();
            Show(_levelSelect);
        }

        private void Show(RectTransform screen)
        {
            _mainMenu.gameObject.SetActive(screen == _mainMenu);
            _levelSelect.gameObject.SetActive(screen == _levelSelect);
        }
        #endregion

        #region Main menu
        private void BuildMainMenu()
        {
            _mainMenu = CreateScreen("MainMenu", _menuBackground);

            // Only the title part of the full-screen title image, shown bigger.
            RectTransform title = CreateImage("Title", _mainMenu, CropTitle(_title), new Vector2(0f, 113f), new Vector2(1552f, 130f));
            title.GetComponent<Image>().preserveAspect = true;

            CreateImageButton("PlayButton", _mainMenu, _playButton, null, new Vector2(0f, -135f), new Vector2(757f, 357f), ShowLevelSelect);
        }

        private static Sprite CropTitle(Sprite full)
        {
            if (full == null)
                return null;

            // Title lettering inside the 1600x900 overlay (pixels, bottom-left origin).
            Rect source = full.rect;
            float scale = source.width / 1600f;
            var crop = new Rect(source.x + 314f * scale, source.y + (900f - 333f) * scale, 966f * scale, 81f * scale);
            return Sprite.Create(full.texture, crop, new Vector2(0.5f, 0.5f), full.pixelsPerUnit);
        }
        #endregion

        #region Level select
        private void BuildLevelSelect()
        {
            _levelSelect = CreateScreen("LevelSelect", _levelSelectBackground);

            _grid = ProtoUi.CreateRect("Levels", _levelSelect);
            ProtoUi.Stretch(_grid);

            // "<- back" on a plate hanging from the top-left corner.
            Button back = CreateImageButton("BackButton", _levelSelect, _plate, null, Vector2.zero, new Vector2(610f, 203f), ShowMainMenu);
            var backRect = (RectTransform)back.transform;
            backRect.anchorMin = backRect.anchorMax = backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(60f, 0f);
            TMP_Text backLabel = CreateLabel("Label", backRect, "<- back", 96f);
            ProtoUi.Stretch(backLabel.rectTransform);
            backLabel.rectTransform.offsetMin = new Vector2(0f, 12f);
            backLabel.font = TMP_Settings.defaultFontAsset != null ? TMP_Settings.defaultFontAsset : _font;
            backLabel.color = Color.black;
        }

        private int LevelCount
            => ServiceLocator.TryGet(out ShiftService shifts) && shifts.ShiftAmount > 0 ? shifts.ShiftAmount : _fallbackLevelCount;

        private bool IsUnlocked(int levelIndex)
            => levelIndex < LevelCount && (!_lockLevels || levelIndex == 0 || ProtoProgress.GetBestStars(levelIndex - 1) > 0);

        private void RefreshLevels()
        {
            for (int i = _grid.childCount - 1; i >= 0; i--)
                Destroy(_grid.GetChild(i).gameObject);

            // Fixed board of slots; slots past the last level stay locked.
            int slots = Mathf.Max(LevelCount, _columns * _rows);
            for (int i = 0; i < slots; i++)
            {
                int column = i % _columns;
                int row = i / _columns;
                var position = new Vector2((column - (_columns - 1) * 0.5f) * _tileSpacing.x, _firstRowY - row * _tileSpacing.y);
                CreateLevelCell(i, position);
            }
        }

        private void CreateLevelCell(int levelIndex, Vector2 position)
        {
            RectTransform cell = ProtoUi.CreateRect($"Level {levelIndex + 1}", _grid);
            cell.anchorMin = cell.anchorMax = cell.pivot = new Vector2(0.5f, 0.5f);
            cell.anchoredPosition = position;
            cell.sizeDelta = new Vector2(280f, 282f);

            if (!IsUnlocked(levelIndex))
            {
                CreateImage("Locked", cell, _lockedTile, Vector2.zero, new Vector2(280f, 282f));
                return;
            }

            Button tile = CreateImageButton("Tile", cell, _levelTile, null, Vector2.zero, new Vector2(280f, 282f), () => SelectLevel(levelIndex));
            TMP_Text number = CreateLabel("Number", tile.transform, (levelIndex + 1).ToString(), 110f);
            ProtoUi.Stretch(number.rectTransform);
            number.rectTransform.offsetMin = new Vector2(0f, 20f);
            number.color = Color.black;

            // Three stars overlapping the bottom edge of the tile.
            int stars = ProtoProgress.GetBestStars(levelIndex);
            for (int s = 0; s < 3; s++)
                CreateImage($"Star {s + 1}", cell, s < stars ? _starFull : _starEmpty, new Vector2((s - 1) * 88f, -123f), new Vector2(126f, 126f));
        }

        private void SelectLevel(int levelIndex)
        {
            ProtoProgress.SelectedLevelIndex = levelIndex;
            ProtoProgress.LoadScene(ProtoProgress.LevelSceneName);
        }
        #endregion

        #region UI helpers
        private RectTransform CreateScreen(string name, Sprite background)
        {
            RectTransform screen = ProtoUi.CreateRect(name, _root);
            ProtoUi.Stretch(screen);
            CreateFullScreenImage("Background", screen, background);
            return screen;
        }

        private static void CreateFullScreenImage(string name, RectTransform parent, Sprite sprite)
        {
            RectTransform rect = ProtoUi.CreateRect(name, parent);
            ProtoUi.Stretch(rect);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            if (sprite == null)
                image.color = new Color(0.7f, 0.85f, 0.9f);
        }

        private static RectTransform CreateImage(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            RectTransform rect = ProtoUi.CreateRect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return rect;
        }

        private Button CreateImageButton(string name, Transform parent, Sprite sprite, string label, Vector2 position, Vector2 size,
            UnityAction onClick, float fontSize = 60f)
        {
            RectTransform rect = CreateImage(name, parent, sprite, position, size);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.92f, 0.95f, 1f);
            colors.pressedColor = new Color(0.8f, 0.85f, 0.9f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            button.onClick.AddListener(onClick);

            if (!string.IsNullOrEmpty(label))
            {
                TMP_Text text = CreateLabel("Label", rect, label, fontSize);
                ProtoUi.Stretch(text.rectTransform);
                text.rectTransform.offsetMin = new Vector2(0f, 6f); // the art has a shadow at the bottom
            }

            return button;
        }

        private TMP_Text CreateLabel(string name, Transform parent, string text, float size)
        {
            TMP_Text label = ProtoUi.CreateText(name, parent, text, _font, size);
            label.color = InkColor;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }
        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// Editor safety net: fills art references from Art/Prototype/UI when the scene doesn't have them yet.
        /// </summary>
        private void ResolveMissingReferences()
        {
            Resolve(ref _menuBackground, "6e1c42c4349532b4a8cd93a417f0933b");
            Resolve(ref _title, "2c63119568839f140ab4add61409488c");
            Resolve(ref _levelSelectBackground, "4199e014578b06f47891d162b8c3cf48");
            Resolve(ref _button, "876a0a2d7efd31d4d8980b9b489766c3");
            Resolve(ref _levelTile, "51e5e7c118e3ae14cb0db70c91f082c4");
            Resolve(ref _lockedTile, "5804e4f880f78a74bb31589e8382a912");
            Resolve(ref _starFull, "e4bf7515b5c9ab94f9bb71f842ccea58");
            Resolve(ref _starEmpty, "284e66a02cd2aef47b632ee078793915");
            Resolve(ref _plate, "7c83782e226c41e42aafbe18dbd104a0");
            Resolve(ref _playButton, "b82ec02b710451e4682ab29a77347939");
            Resolve(ref _font, "236fde7f73574004aa2ce1c1579e2e44");
        }

        private static void Resolve<T>(ref T field, string guid) where T : Object
        {
            if (field != null)
                return;

            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
                field = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        }
#endif
    }
}
