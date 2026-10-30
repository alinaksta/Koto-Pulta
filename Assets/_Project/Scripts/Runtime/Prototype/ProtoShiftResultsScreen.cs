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
        private Sprite _starFull;
        private Sprite _starEmpty;

        private static readonly Color ButtonColor = new Color(0.3f, 0.62f, 1f);
        private const float FirstStarDelay = 0.45f;
        private const float StarInterval = 0.4f;
        private const float StarPopDuration = 0.3f;

        /// <summary>Star pictures for the result; without them the stars are drawn as text.</summary>
        public void SetStarSprites(Sprite full, Sprite empty)
        {
            _starFull = full;
            _starEmpty = empty;
        }

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

            ProtoUi.SetLayoutSize(ProtoUi.CreateText("Title", panel, $"LEVEL {levelIndex + 1}", font, 90f), -1f, 110f);

            if (_starFull != null && _starEmpty != null)
            {
                BuildStarRow(panel, stars);
            }
            else
            {
                TMP_Text starsText = ProtoUi.CreateText("Stars", panel, ProtoUi.FormatStars(stars, 3, font), font, 110f);
                starsText.color = ProtoUi.StarColor;
                ProtoUi.SetLayoutSize(starsText, -1f, 140f);
            }

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

            Paint(ProtoUi.CreateButton("RetryButton", buttons, template, "Retry", font, new Vector2(320f, 90f), Retry));
            Paint(ProtoUi.CreateButton("LevelSelectButton", buttons, template, "Return to Level select", font, new Vector2(440f, 90f), ReturnToLevelSelect));
        }

        private static void Paint(Button button)
        {
            if (button == null)
                return;
            if (button.targetGraphic != null)
                button.targetGraphic.color = ButtonColor;
            foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
                label.color = Color.white;
        }

        /// <summary>Three empty stars; the earned ones pop in one after another.</summary>
        private void BuildStarRow(RectTransform panel, int stars)
        {
            RectTransform row = ProtoUi.CreateRect("Stars", panel);
            ProtoUi.SetLayoutSize(row, -1f, 170f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 30f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var earned = new RectTransform[Mathf.Clamp(stars, 0, 3)];
            for (int i = 0; i < 3; i++)
            {
                RectTransform cell = ProtoUi.CreateRect($"Star {i + 1}", row);
                ProtoUi.SetLayoutSize(cell, 150f, 150f);
                var empty = cell.gameObject.AddComponent<Image>();
                empty.sprite = _starEmpty;
                empty.preserveAspect = true;
                empty.raycastTarget = false;

                if (i >= earned.Length)
                    continue;

                RectTransform full = ProtoUi.CreateRect("Full", cell);
                ProtoUi.Stretch(full);
                var image = full.gameObject.AddComponent<Image>();
                image.sprite = _starFull;
                image.preserveAspect = true;
                image.raycastTarget = false;
                full.localScale = Vector3.zero;
                earned[i] = full;
            }

            StartCoroutine(RevealStars(earned));
        }

        private System.Collections.IEnumerator RevealStars(RectTransform[] stars)
        {
            // Time is frozen on this screen: real time only.
            yield return new WaitForSecondsRealtime(FirstStarDelay);
            foreach (RectTransform star in stars)
            {
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / StarPopDuration)
                {
                    // Overshoots a little, then settles.
                    float scale = t < 0.6f ? Mathf.Lerp(0f, 1.25f, t / 0.6f) : Mathf.Lerp(1.25f, 1f, (t - 0.6f) / 0.4f);
                    star.localScale = Vector3.one * scale;
                    yield return null;
                }

                star.localScale = Vector3.one;
                yield return new WaitForSecondsRealtime(StarInterval - StarPopDuration);
            }
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
