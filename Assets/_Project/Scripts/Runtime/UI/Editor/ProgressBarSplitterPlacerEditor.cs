using Game.UI;
using UnityEditor;
using UnityEngine;

namespace Game.UI.Editor
{
    [CustomEditor(typeof(ProgressBarSplitterPlacer))]
    public sealed class ProgressBarSplitterPlacerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var placer = (ProgressBarSplitterPlacer)target;
            if (GUILayout.Button("Rebuild Splitters"))
            {
                Undo.RegisterFullObjectHierarchyUndo(placer.gameObject, "Rebuild Progress Bar Splitters");
                placer.Rebuild();
                EditorUtility.SetDirty(placer);
            }

            if (GUILayout.Button("Clear Splitters"))
            {
                Undo.RegisterFullObjectHierarchyUndo(placer.gameObject, "Clear Progress Bar Splitters");
                placer.ClearGenerated();
                EditorUtility.SetDirty(placer);
            }
        }
    }
}
