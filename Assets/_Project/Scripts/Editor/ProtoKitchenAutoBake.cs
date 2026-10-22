using Game.Prototype.Kitchen;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Prototype.Kitchen.EditorTools
{
    /// <summary>
    /// Puts the prototype stations and office niches into the open scene as regular objects:
    /// automatically when a scene with ProtoKitchen has none yet, or on demand from the Tools menu.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProtoKitchenAutoBake
    {
        static ProtoKitchenAutoBake()
        {
            EditorSceneManager.sceneOpened += (_, _) => EditorApplication.delayCall += BakeMissing;
            EditorApplication.delayCall += BakeMissing;
        }

        private const string NavMeshAssetName = "NavMesh-Office-v3";
        private const int NotWalkableArea = 1;
        // Colliders reaching higher than this (tables, desks, chairs, walls) are obstacles only: nothing walkable on top of them.
        private const float FloorMaxHeight = 0.1f;

        [MenuItem("Tools/Koto/Rebake Proto NavMesh")]
        private static void RebakeNavMesh()
        {
            foreach (NavMeshSurface surface in Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None))
                BakeNavMesh(surface);
        }

        /// <summary>
        /// Bakes the walkable area from the colliders of the level (office floor, tables, desks, chairs) and saves it next to the scene.
        /// </summary>
        private static void BakeNavMesh(NavMeshSurface surface)
        {
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = 1 << 0; // Default: level geometry (stations, player and customers live on other layers or have no colliders)

            // Only the floor is walkable: temporarily mark everything that sticks out of it as "Not Walkable".
            var temporary = new System.Collections.Generic.List<NavMeshModifier>();
            foreach (Collider collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (collider.isTrigger || collider.bounds.max.y <= FloorMaxHeight || collider.GetComponent<NavMeshModifier>() != null)
                    continue;

                var modifier = collider.gameObject.AddComponent<NavMeshModifier>();
                modifier.overrideArea = true;
                modifier.area = NotWalkableArea;
                modifier.applyToChildren = false;
                temporary.Add(modifier);
            }

            surface.RemoveData();
            surface.navMeshData = null;
            try
            {
                surface.BuildNavMesh();
            }
            finally
            {
                foreach (NavMeshModifier modifier in temporary)
                    Object.DestroyImmediate(modifier);
            }

            var scene = surface.gameObject.scene;
            string folder = Path.Combine(Path.GetDirectoryName(scene.path) ?? "Assets", scene.name).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\', '/'), Path.GetFileName(folder));

            string path = $"{folder}/{NavMeshAssetName}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            AssetDatabase.SaveAssets();

            EditorUtility.SetDirty(surface);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[ProtoKitchen] NavMesh baked for the office and saved to {path}. Save the scene to keep it.", surface);
        }

        [MenuItem("Tools/Koto/Rebuild Proto Stations And Niches")]
        private static void Rebuild()
        {
            foreach (ProtoKitchen kitchen in Object.FindObjectsByType<ProtoKitchen>(FindObjectsSortMode.None))
                kitchen.BakeIntoScene();
        }

        private static void BakeMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            ProtoKitchen[] kitchens = Object.FindObjectsByType<ProtoKitchen>(FindObjectsSortMode.None);

            // Scenes with the prototype kitchen get a NavMesh for the office once (rebake from the Tools menu after changes).
            if (kitchens.Length > 0)
            {
                foreach (NavMeshSurface surface in Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None))
                {
                    if (surface.navMeshData == null || surface.navMeshData.name != NavMeshAssetName)
                        BakeNavMesh(surface);
                }
            }

            foreach (ProtoKitchen kitchen in kitchens)
            {
                if (kitchen.transform.Find(ProtoKitchen.BakedStationsName) != null)
                {
                    int signs = kitchen.AddMissingNicheSigns();
                    if (signs > 0)
                        Debug.Log($"[ProtoKitchen] Added {signs} number signs above the niche computers. Save the scene to keep them.", kitchen);
                    continue;
                }

                kitchen.BakeIntoScene();
                Debug.Log("[ProtoKitchen] Stations and office niches were added to the scene. Save the scene to keep them; " +
                          "Tools > Koto > Rebuild Proto Stations And Niches rebuilds them from scratch.", kitchen);
            }
        }
    }
}
