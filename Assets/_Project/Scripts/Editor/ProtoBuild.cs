using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Prototype.Kitchen.EditorTools
{
    /// <summary>
    /// Builds the prototype (main menu + level) as a Windows exe into Builds/KotoPulta-Proto.
    /// Run from Tools/Koto/Build Prototype (Windows), or by creating Builds/KotoBuildRequest (picked up on the next editor tick);
    /// the result is written to Builds/KotoBuildResult.txt.
    /// Before building, the art and sound references that the prototype only finds through the editor
    /// are written into the scenes, so the exe has them too.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProtoBuild
    {
        private const string RequestFile = "Builds/KotoBuildRequest";
        private const string ResultFile = "Builds/KotoBuildResult.txt";
        private const string OutputPath = "Builds/KotoPulta-Proto/KotoPulta.exe";

        private static readonly string[] Scenes =
        {
            "Assets/_Project/Scenes/Sandbox/Karim/Proto-MainMenu.unity",
            "Assets/_Project/Scenes/Sandbox/Karim/Proto-level.unity",
        };

        static ProtoBuild()
        {
            EditorApplication.update += PollRequest;
        }

        private static void PollRequest()
        {
            if (!File.Exists(RequestFile) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            File.Delete(RequestFile);
            EditorApplication.delayCall += Build;
        }

        [MenuItem("Tools/Koto/Build Prototype (Windows)")]
        private static void Build()
        {
            var log = new StringBuilder();
            log.AppendLine($"Started {DateTime.Now:HH:mm:ss}");
            try
            {
                // Keep the open scenes' unsaved changes (they are what is being built).
                EditorSceneManager.SaveOpenScenes();
                string[] opened = OpenScenePaths();

                foreach (string scene in Scenes)
                    BakeReferences(scene, log);

                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = Scenes,
                    locationPathName = OutputPath,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None,
                });

                BuildSummary summary = report.summary;
                log.AppendLine($"Result: {summary.result}, errors {summary.totalErrors}, warnings {summary.totalWarnings}, " +
                               $"size {summary.totalSize / (1024 * 1024)} MB, time {summary.totalTime:mm\\:ss}");
                log.AppendLine($"Output: {Path.GetFullPath(OutputPath)}");
                foreach (BuildStep step in report.steps)
                {
                    foreach (BuildStepMessage message in step.messages)
                    {
                        if (message.type == LogType.Error || message.type == LogType.Exception)
                            log.AppendLine($"ERROR [{step.name}]: {message.content}");
                    }
                }

                RestoreScenes(opened);
            }
            catch (Exception e)
            {
                log.AppendLine("EXCEPTION: " + e);
            }

            log.AppendLine($"Finished {DateTime.Now:HH:mm:ss}");
            File.WriteAllText(ResultFile, log.ToString());
            Debug.Log("[ProtoBuild] " + log);
        }

        private static string[] OpenScenePaths()
        {
            var paths = new string[EditorSceneManager.sceneCount];
            for (int i = 0; i < paths.Length; i++)
                paths[i] = EditorSceneManager.GetSceneAt(i).path;
            return paths;
        }

        private static void RestoreScenes(string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                if (!string.IsNullOrEmpty(paths[i]))
                    EditorSceneManager.OpenScene(paths[i], i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
            }
        }

        /// <summary>Fills the editor-only fallbacks (sounds, sprites, paws...) into the scene's components and saves it.</summary>
        private static void BakeReferences(string scenePath, StringBuilder log)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int count = 0;
            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null)
                    continue;
                MethodInfo resolve = behaviour.GetType().GetMethod("ResolveMissingReferences",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (resolve == null || resolve.GetParameters().Length != 0)
                    continue;

                resolve.Invoke(behaviour, null);
                EditorUtility.SetDirty(behaviour);
                count++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            log.AppendLine($"{Path.GetFileNameWithoutExtension(scenePath)}: references filled in {count} component(s)");
        }
    }
}
