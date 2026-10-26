using Game.Prototype.Kitchen;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
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

        private const string NavMeshAssetName = "NavMesh-Office-v4";
        private const string QueueBlockName = "NavMeshBlock_BetweenQueueAndCounter";
        private const int NotWalkableArea = 1;
        // Colliders reaching higher than this (tables, desks, chairs, walls) are obstacles only: nothing walkable on top of them.
        private const float FloorMaxHeight = 0.1f;

        /// <summary>
        /// The controls guide note (a notebook item spawned at the start) is not wanted in the prototype level.
        /// </summary>
        private static void RemoveControlsNote()
        {
            GameObject spawner = GameObject.Find("NoteSpawner");
            if (spawner == null)
                return;

            Undo.RecordObject(spawner, "Remove controls note");
            spawner.SetActive(false);
            EditorSceneManager.MarkSceneDirty(spawner.scene);
            Debug.Log("[ProtoKitchen] The controls guide note (NoteSpawner) is turned off. Save the scene to keep it.");
        }

        /// <summary>
        /// Customers walking past the line must go behind it, not between it and the cashier table:
        /// the strip between the table and the line is not walkable.
        /// </summary>
        private static void AddQueueBlock()
        {
            if (GameObject.Find(QueueBlockName) != null)
                return;

            var queue = Object.FindAnyObjectByType<Game.Characters.CustomerQueue>();
            if (queue == null)
                return;

            var block = new GameObject(QueueBlockName);
            Undo.RegisterCreatedObjectUndo(block, "Add queue NavMesh block");
            SceneManager.MoveGameObjectToScene(block, queue.gameObject.scene);

            // The line runs along +X from the cashier spot; the table is on its -Z side (0.7 m wide strip).
            Vector3 front = queue.transform.position;
            block.transform.position = new Vector3(front.x + 2.4f, 0f, front.z - 0.4f);
            var volume = block.AddComponent<NavMeshModifierVolume>();
            volume.size = new Vector3(10f, 3f, 0.5f);
            volume.center = new Vector3(0f, 1f, 0f);
            volume.area = 1; // Not Walkable
            EditorSceneManager.MarkSceneDirty(queue.gameObject.scene);
        }

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

        private const string PropsModelPath = "Assets/_Project/Art/Dev/Models/Prototype/office_props.fbx";
        private const string PropModelName = "OfficeModel";
        private static readonly string[] s_oldPropModelNames = { "OfficePropModel", "OfficeProp" };

        /// <summary>
        /// Gives the cashier computer and the bin the office models (office_props.fbx) instead of their own meshes.
        /// Their gameplay parts (screen UI, colliders, scripts) stay as they are. Returns how many were changed.
        /// </summary>
        private static int ApplyOfficeProps()
        {
            var props = AssetDatabase.LoadAssetAtPath<GameObject>(PropsModelPath);
            Transform computerSource = props != null ? props.transform.Find("Computer") : null;
            Transform binSource = props != null ? props.transform.Find("Cylinder") : null;
            if (computerSource == null || binSource == null)
                return 0;

            int changed = 0;

            GameObject computer = GameObject.Find("PR_Computer");
            if (computer != null && computer.transform.Find(PropModelName) == null)
            {
                // Older versions of this step: rebuild.
                RemoveOldPropModels(computer.transform);

                Canvas screen = computer.GetComponentInChildren<Canvas>(true);
                HideOwnRenderers(computer.transform, screen != null ? screen.transform : null);

                Transform model = CreatePropModel(computer.transform, computerSource, Quaternion.identity);
                Mesh mesh = model.GetComponent<MeshFilter>().sharedMesh;

                // In the props file the computer's front (screen, buttons) is +Y of the mesh: turn it to the player.
                Vector3 toPlayer = Flat(computer.transform.forward);
                Vector3 front = Flat(model.rotation * Vector3.up);
                // Yaw only, so the model stays upright (FromToRotation may flip it over for opposite vectors).
                model.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(front, toPlayer, Vector3.up), Vector3.up) * model.rotation;

                // Standing on the table where the old monitor stood.
                Bounds bounds = WorldBounds(model);
                Vector3 root = computer.transform.position;
                model.position += new Vector3(root.x - bounds.center.x, root.y - bounds.min.y, root.z - bounds.center.z);

                if (screen != null)
                    FitScreen(computer.transform, (RectTransform)screen.transform, model, mesh);
                changed++;
            }

            GameObject bin = GameObject.Find("Bin");
            if (bin != null && bin.transform.Find(PropModelName) == null)
            {
                RemoveOldPropModels(bin.transform);

                HideOwnRenderers(bin.transform, null);
                Transform model = CreatePropModel(bin.transform, binSource, Quaternion.identity);

                // Standing on the floor, centered on the bin.
                Bounds bounds = WorldBounds(model);
                Vector3 target = bin.transform.position;
                model.position += new Vector3(target.x - bounds.center.x, -bounds.min.y, target.z - bounds.center.z);
                changed++;
            }

            if (changed > 0)
                EditorSceneManager.MarkAllScenesDirty();
            return changed;
        }

        // The monitor screen in the props file (mesh units of the file: centimeters, Z down = up):
        // a 46.2 x 36.2 quad centered at (0, 16.3, -44.7), facing +Y and tilted slightly up.
        private const float ScreenWidthCm = 46.2f;
        private const float ScreenHeightCm = 36.2f;
        private static readonly Vector3 s_screenCenterCm = new Vector3(0f, 16.3f, -44.7f);
        private static readonly Vector3 s_screenNormal = new Vector3(0f, 0.99f, -0.14f);
        private const float RawComputerHalfWidthCm = 30f;

        /// <summary>
        /// Puts the website canvas onto the monitor screen of the props model and moves the computer's camera point in front of it.
        /// </summary>
        private static void FitScreen(Transform computer, RectTransform canvas, Transform model, Mesh mesh)
        {
            // Mesh units per centimeter of the file (the importer may already have converted units).
            float unit = mesh.bounds.extents.x / RawComputerHalfWidthCm;
            Vector3 center = model.TransformPoint(s_screenCenterCm * unit);
            Vector3 normal = model.TransformDirection(s_screenNormal).normalized;
            Vector3 up = model.TransformDirection(Vector3.back).normalized; // -Z of the file is up

            float width = model.TransformVector(Vector3.right * ScreenWidthCm * unit).magnitude;
            float height = model.TransformVector(Vector3.forward * ScreenHeightCm * unit).magnitude;

            Undo.RecordObject(canvas, "Fit computer screen");
            float oldScale = canvas.lossyScale.x;
            Vector2 size = canvas.rect.size;
            float scale = Mathf.Min(width / size.x, height / size.y) * 0.97f;

            // The UI is seen from its back side (-forward): face away from the viewer.
            canvas.SetPositionAndRotation(center + normal * 0.004f, Quaternion.LookRotation(-normal, up));
            Vector3 parentScale = canvas.parent.lossyScale;
            canvas.localScale = new Vector3(scale / parentScale.x, scale / parentScale.y, scale / parentScale.z);

            Transform cameraOrigin = computer.Find("CameraOrigin");
            if (cameraOrigin != null)
            {
                Undo.RecordObject(cameraOrigin, "Fit computer camera");
                float distance = Vector3.Distance(cameraOrigin.position, center) * (oldScale > 0f ? scale / oldScale : 1f);
                cameraOrigin.SetPositionAndRotation(center + normal * Mathf.Max(0.2f, distance), Quaternion.LookRotation(-normal, up));
            }
        }

        private const string PawLeftGuid = "980dd1055be00a743a4f81a71d0e3700";
        private const string PawRightGuid = "c2b2e71aeb348a24f8b3db65c71dac09";

        /// <summary>
        /// Puts the paws (ProtoPawView + both paw models) under the player in the scene, so they can be tuned there.
        /// </summary>
        private static bool AddPawsToPlayer()
        {
            if (Object.FindAnyObjectByType<ProtoPawView>(FindObjectsInactive.Include) != null)
                return false;

            GameObject player = GameObject.Find("PR_Player");
            var left = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(PawLeftGuid));
            var right = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(PawRightGuid));
            if (player == null || left == null || right == null)
                return false;

            var root = new GameObject("Paws");
            Undo.RegisterCreatedObjectUndo(root, "Add paws");
            root.transform.SetParent(player.transform, false);
            var view = root.AddComponent<ProtoPawView>();

            Transform leftPaw = CreatePaw(root.transform, "Paw_Left", left);
            Transform rightPaw = CreatePaw(root.transform, "Paw_Right", right);
            view.SetSceneObjects(left, right, leftPaw, rightPaw);

            EditorSceneManager.MarkSceneDirty(player.scene);
            return true;
        }

        private static Transform CreatePaw(Transform parent, string name, GameObject prefab)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            model.transform.localScale = Vector3.one * 0.08f; // editor preview size; set at runtime from the arm length
            return root.transform;
        }

        private static void RemoveOldPropModels(Transform parent)
        {
            foreach (string name in s_oldPropModelNames)
            {
                Transform old = parent.Find(name);
                if (old != null)
                    Undo.DestroyObjectImmediate(old.gameObject);
            }
        }

        private static void HideOwnRenderers(Transform root, Transform keep)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (keep != null && renderer.transform.IsChildOf(keep))
                    continue;

                Undo.RecordObject(renderer, "Hide old model");
                renderer.enabled = false;
            }
        }

        /// <summary>
        /// Copies a part of the props model (mesh, materials, its own rotation and scale from the file) under the parent.
        /// </summary>
        private static Transform CreatePropModel(Transform parent, Transform source, Quaternion facing)
        {
            var go = new GameObject(PropModelName);
            Undo.RegisterCreatedObjectUndo(go, "Add office prop model");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;

            Vector3 scale = source.lossyScale;
            Vector3 parentScale = parent.lossyScale;
            go.transform.rotation = facing * source.rotation;
            go.transform.localScale = new Vector3(scale.x / parentScale.x, scale.y / parentScale.y, scale.z / parentScale.z);
            return go.transform;
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        private static Bounds WorldBounds(Transform model)
        {
            Renderer renderer = model.GetComponent<Renderer>();
            Physics.SyncTransforms();
            return renderer.bounds;
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

            if (kitchens.Length > 0)
            {
                int staplers = ProtoKitchen.ReplaceStaplerStations();
                if (staplers > 0)
                    Debug.Log("[ProtoKitchen] The stapler station was replaced by a spawn point for the stapler item. Save the scene to keep it.");

                if (AddPawsToPlayer())
                    Debug.Log("[ProtoKitchen] The paws were added to PR_Player in the scene. Save the scene to keep them.");

                int props = ApplyOfficeProps();
                if (props > 0)
                    Debug.Log($"[ProtoKitchen] The cashier computer and the bin now use the office models ({props}). Save the scene to keep it.");
            }

            // Scenes with the prototype kitchen get a NavMesh for the office once (rebake from the Tools menu after changes).
            if (kitchens.Length > 0)
            {
                AddQueueBlock();
                RemoveControlsNote();

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
