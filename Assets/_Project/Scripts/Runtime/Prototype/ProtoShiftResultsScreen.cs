using Game.Input;
using Game.Interaction;
using Game.Player;
using Game.Progression;
using Game.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Prototype
{
    /// <summary>
    /// End-of-shift screen for the sandbox: stars earned plus "Retry" and "Return to Level select".
    /// </summary>
    public sealed class ProtoShiftResultsScreen : MonoBehaviour
    {
        private Canvas _canvas;
        private IInputService _input;
        private bool _shown;

        /// <summary>
        /// Builds and shows the results screen.
        /// </summary>
        public void Show(int levelIndex, int earned, int goal)
        {
            if (_shown)
                return;

            _shown = true;
            int stars = ProtoProgress.CalculateStars(earned, goal);
            ProtoProgress.SubmitStars(levelIndex, stars);

            FreezeGameplay();
            Build(levelIndex, stars, earned, goal);
        }

        private void FreezeGameplay()
        {
            if (ServiceLocator.TryGet(out _input))
                _input.SetGameplaySuppressed(true);

            var pause = FindAnyObjectByType<Pause>(FindObjectsInactive.Include);
            if (pause != null)
                pause.enabled = false;

            CameraController.SetMouseLockedStatic(false);
            CameraController.SetActiveRotationStatic(true);
            Time.timeScale = 0f;
        }

        private void OnDestroy()
        {
            if (!_shown)
                return;

            _input?.SetGameplaySuppressed(false);
            CameraController.SetActiveRotationStatic(false);
            Time.timeScale = 1f;
        }

        private void Build(int levelIndex, int stars, int earned, int goal)
        {
            TMP_FontAsset font = ProtoUi.FindSceneFont();
            Button template = FindTemplateButton();

            _canvas = ProtoUi.CreateOverlayCanvas("ShiftResultsCanvas", 1000);
            _canvas.transform.SetParent(transform, false);

            RectTransform dim = ProtoUi.CreateRect("Dim", _canvas.transform);
            ProtoUi.Stretch(dim);
            dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            RectTransform panel = ProtoUi.CreatePanel("Panel", _canvas.transform, new Vector2(900f, 620f), ProtoUi.PanelColor);

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 40);
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ProtoUi.SetLayoutSize(ProtoUi.CreateText("Title", panel, $"Level {levelIndex + 1} - shift over", font, 60f), -1f, 90f);

            TMP_Text starsText = ProtoUi.CreateText("Stars", panel, ProtoUi.FormatStars(stars, 3, font), font, 110f);
            starsText.color = ProtoUi.StarColor;
            ProtoUi.SetLayoutSize(starsText, -1f, 140f);

            string quota = goal > 0 ? $"Earned {earned} / quota {goal}" : $"Earned {earned}";
            ProtoUi.SetLayoutSize(ProtoUi.CreateText("Earned", panel, quota, font, 40f), -1f, 60f);

            RectTransform buttons = ProtoUi.CreateRect("Buttons", panel);
            ProtoUi.SetLayoutSize(buttons, -1f, 100f);
            var row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 40f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            ProtoUi.CreateButton("RetryButton", buttons, template, "Retry", font, new Vector2(320f, 90f), Retry);
            ProtoUi.CreateButton("LevelSelectButton", buttons, template, "Return to Level select", font, new Vector2(440f, 90f), ReturnToLevelSelect);
        }

        private static Button FindTemplateButton()
        {
            var pause = FindAnyObjectByType<Pause>(FindObjectsInactive.Include);
            if (pause != null && pause._pauseCanvas != null)
                return pause._pauseCanvas.GetComponentInChildren<Button>(true);

            return null;
        }

        private void Retry()
        {
            LeaveGameplay();
            ProtoProgress.LoadScene(ProtoProgress.LevelSceneName);
        }

        private void ReturnToLevelSelect()
        {
            LeaveGameplay();
            ProtoProgress.OpenLevelSelectOnMenuLoad = true;
            ProtoProgress.LoadScene(ProtoProgress.MenuSceneName);
        }

        private void LeaveGameplay()
        {
            Time.timeScale = 1f;
            if (ServiceLocator.TryGet<GameModeService>(out var gameModes))
                gameModes.ClearGameMode();
        }
    }
}
