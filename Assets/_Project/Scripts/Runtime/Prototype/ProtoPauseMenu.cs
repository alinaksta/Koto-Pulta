using Game.Characters;
using Game.Input;
using Game.Player;
using Game.Progression;
using Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Game.Prototype
{
    /// <summary>
    /// Sandbox pause menu: Continue (or Esc), Settings (master / SFX / music), Return to Level select,
    /// and a debug checkbox that freezes all customer patience timers.
    /// </summary>
    public sealed class ProtoPauseMenu : MonoBehaviour
    {
        [SerializeField] private AudioMixer _mixer;

        private IInputService _input;
        private Canvas _canvas;
        private RectTransform _mainPanel;
        private RectTransform _settingsPanel;
        private TMP_FontAsset _font;
        private bool _isPaused;
        private float _timeScaleBeforePause = 1f;

        /// <summary>
        /// When false the menu ignores Esc (e.g. while the shift results screen is open).
        /// </summary>
        public bool CanPause { get; set; } = true;

        public bool IsPaused => _isPaused;

        public void SetMixer(AudioMixer mixer)
        {
            if (mixer != null)
                _mixer = mixer;
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _input);
            if (_mixer == null)
                _mixer = ProtoAudioSettings.FindMixer();

            Build();
            _canvas.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_input == null || !_input.Pause.Pressed)
                return;

            if (_isPaused)
            {
                if (_settingsPanel.gameObject.activeSelf)
                    ShowMain();
                else
                    Resume();
            }
            else if (CanPause && CameraController.IsUnfocused)
            {
                Pause();
            }
        }

        private void OnDestroy()
        {
            if (_isPaused)
                RestoreGameplay();
        }

        public void Pause()
        {
            if (_isPaused)
                return;

            _isPaused = true;
            _timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            _input?.SetGameplaySuppressed(true);
            CameraController.SetMouseLockedStatic(false);
            CameraController.SetActiveRotationStatic(true);

            ShowMain();
            _canvas.gameObject.SetActive(true);
        }

        public void Resume()
        {
            if (!_isPaused)
                return;

            RestoreGameplay();
            CameraController.SetMouseLockedStatic(true);
            _canvas.gameObject.SetActive(false);
        }

        private void RestoreGameplay()
        {
            _isPaused = false;
            Time.timeScale = _timeScaleBeforePause > 0f ? _timeScaleBeforePause : 1f;
            _input?.SetGameplaySuppressed(false);
            CameraController.SetActiveRotationStatic(false);
        }

        private void ReturnToLevelSelect()
        {
            RestoreGameplay();
            Time.timeScale = 1f;

            if (ServiceLocator.TryGet<GameModeService>(out var gameModes))
                gameModes.ClearGameMode();

            ProtoProgress.OpenLevelSelectOnMenuLoad = true;
            ProtoProgress.LoadScene(ProtoProgress.MenuSceneName);
        }

        private void ShowMain()
        {
            _settingsPanel.gameObject.SetActive(false);
            _mainPanel.gameObject.SetActive(true);
        }

        private void ShowSettings()
        {
            _mainPanel.gameObject.SetActive(false);
            _settingsPanel.gameObject.SetActive(true);
        }

        #region UI
        private void Build()
        {
            _font = ProtoUi.FindSceneFont();

            _canvas = ProtoUi.CreateOverlayCanvas("PauseMenuCanvas", 900);
            _canvas.transform.SetParent(transform, false);

            RectTransform dim = ProtoUi.CreateRect("Dim", _canvas.transform);
            ProtoUi.Stretch(dim);
            dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            // Main pause panel.
            _mainPanel = ProtoUi.CreatePanel("PausePanel", _canvas.transform, new Vector2(760f, 640f), ProtoUi.PanelColor);
            ProtoUi.AddVerticalLayout(_mainPanel, 40, 22f);
            ProtoUi.SetLayoutSize(ProtoUi.CreateText("Title", _mainPanel, "Pause", _font, 64f), -1f, 90f);
            ProtoUi.CreateButton("ContinueButton", _mainPanel, null, "Continue", _font, new Vector2(520f, 90f), Resume);
            ProtoUi.CreateButton("SettingsButton", _mainPanel, null, "Settings", _font, new Vector2(520f, 90f), ShowSettings);
            ProtoUi.CreateButton("LevelSelectButton", _mainPanel, null, "Return to Level select", _font, new Vector2(520f, 90f), ReturnToLevelSelect);
            ProtoUi.CreateToggle("DebugPatienceToggle", _mainPanel, "Debug: disable patience timers", _font,
                CustomerQueue.PatienceTimersDisabled, value => CustomerQueue.PatienceTimersDisabled = value);

            // Settings panel.
            _settingsPanel = ProtoUi.CreatePanel("SettingsPanel", _canvas.transform, new Vector2(900f, 560f), ProtoUi.PanelColor);
            ProtoUi.AddVerticalLayout(_settingsPanel, 40, 22f);
            ProtoUi.SetLayoutSize(ProtoUi.CreateText("Title", _settingsPanel, "Settings", _font, 64f), -1f, 90f);
            ProtoUi.CreateSlider("MasterSlider", _settingsPanel, "Master", _font, 0f, 1f, ProtoAudioSettings.Master,
                value => ProtoAudioSettings.Master = value);
            ProtoUi.CreateSlider("SfxSlider", _settingsPanel, "SFX", _font, ProtoAudioSettings.MinDb, ProtoAudioSettings.MaxDb,
                ProtoAudioSettings.GetSfx(_mixer), value => ProtoAudioSettings.SetSfx(_mixer, value));
            ProtoUi.CreateSlider("MusicSlider", _settingsPanel, "Music", _font, ProtoAudioSettings.MinDb, ProtoAudioSettings.MaxDb,
                ProtoAudioSettings.GetMusic(_mixer), value => ProtoAudioSettings.SetMusic(_mixer, value));
            ProtoUi.CreateButton("BackButton", _settingsPanel, null, "Back", _font, new Vector2(320f, 90f), ShowMain);

            _settingsPanel.gameObject.SetActive(false);
        }
        #endregion
    }
}
