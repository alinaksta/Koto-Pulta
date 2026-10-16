using Game.Services;
using Game.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Prototype
{
    /// <summary>
    /// Shared state and helpers for the prototype level-select flow (menu -> level -> results -> menu).
    /// </summary>
    public static class ProtoProgress
    {
        public const string MenuSceneName = "Proto-MainMenu";
        public const string LevelSceneName = "Proto-level";

        private const string StarsKeyPrefix = "Proto.LevelStars.";

        private static bool s_isLoading;

        /// <summary>
        /// Zero-based index of the level (shift) to play when the level scene loads.
        /// </summary>
        public static int SelectedLevelIndex { get; set; }

        /// <summary>
        /// When set, the menu scene opens straight into level select instead of the main menu.
        /// </summary>
        public static bool OpenLevelSelectOnMenuLoad { get; set; }

        /// <summary>
        /// Gets the best star count saved for a level.
        /// </summary>
        public static int GetBestStars(int levelIndex)
            => PlayerPrefs.GetInt(StarsKeyPrefix + levelIndex, 0);

        /// <summary>
        /// Saves the star count for a level when it beats the previous best.
        /// </summary>
        public static void SubmitStars(int levelIndex, int stars)
        {
            if (stars <= GetBestStars(levelIndex))
                return;

            PlayerPrefs.SetInt(StarsKeyPrefix + levelIndex, stars);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 50% of the quota = 1 star, 75% = 2 stars, 100%+ = 3 stars.
        /// </summary>
        public static int CalculateStars(int earned, int goal)
        {
            if (goal <= 0)
                return 3;

            float ratio = (float)earned / goal;
            if (ratio >= 1f) return 3;
            if (ratio >= 0.75f) return 2;
            if (ratio >= 0.5f) return 1;
            return 0;
        }

        /// <summary>
        /// Loads a scene behind the shared loading curtain.
        /// </summary>
        public static async void LoadScene(string sceneName)
        {
            if (s_isLoading)
                return;

            s_isLoading = true;
            Time.timeScale = 1f;

            try
            {
                ServiceLocator.TryGet(out LoadingService loading);
                if (loading != null)
                    await loading.StartLoadingAsync();

                AsyncOperation load = StartSceneLoad(sceneName);
                if (load == null)
                {
                    Debug.LogError($"[{nameof(ProtoProgress)}] Scene '{sceneName}' is not in the Build Profile scene list.");
                    if (loading != null)
                        await loading.StopLoadingAsync();
                    return;
                }

                while (!load.isDone)
                    await Awaitable.NextFrameAsync();

                await WaitUnscaled(0.3f);

                if (loading != null)
                    await loading.StopLoadingAsync();
            }
            finally
            {
                s_isLoading = false;
            }
        }

        private static AsyncOperation StartSceneLoad(string sceneName)
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
                return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

#if UNITY_EDITOR
            // Editor fallback: the sandbox scenes don't have to be in the build list to be played.
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets($"{sceneName} t:Scene"))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                    return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            }
#endif
            return null;
        }

        private static async Awaitable WaitUnscaled(float seconds)
        {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end)
                await Awaitable.NextFrameAsync();
        }
    }
}
