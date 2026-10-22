using Game.Characters;
using Game.Interaction;
using Game.Items;
using Game.Progression;
using Game.Services;
using Game.UI;
using Itemworks.Core;
using Itemworks.UnityEngine;
using UnityEngine;

namespace Game.Prototype.Kitchen
{
    /// <summary>
    /// Sets up the apple pie kitchen in the sandbox level:
    /// registers the prototype items, puts the folder press, shredder and stapler on the counter,
    /// replaces the meals website with an "Ingredients" site and makes customers order apple pie only.
    /// </summary>
    public sealed class ProtoKitchen : MonoBehaviour
    {
        private const int InteractableLayer = 8;

        [Header("Items")]
        [SerializeField] private ItemDefinitionAsset _dough;
        [SerializeField] private ItemDefinitionAsset _apple;
        [SerializeField] private ItemDefinitionAsset _preparedDough;
        [SerializeField] private ItemDefinitionAsset _preparedApple;
        [SerializeField] private ItemDefinitionAsset _applePie;
        [SerializeField] private ItemDefinitionAsset _stapler;

        [Header("Station models")]
        [SerializeField] private GameObject _folderModel;
        [SerializeField] private GameObject _shredderModel;
        [SerializeField] private GameObject _staplerModel;
        [SerializeField] private GameObject _boardModel;

        [Header("Station textures (the FBX materials come in without them)")]
        [SerializeField] private Texture2D _folderTexture;
        [SerializeField] private Texture2D _shredderTexture;
        [SerializeField] private Texture2D _staplerTexture;
        [SerializeField] private Texture2D _boardTexture;

        [Header("Minigame hint icons")]
        [SerializeField] private Sprite _mouseIcon;
        [SerializeField] private Sprite _circleArrowIcon;

        [Header("Placement on the round counter")]
        [Tooltip("Center of the round service counter.")]
        [SerializeField] private Vector3 _counterCenter = Vector3.zero;
        [Tooltip("Distance from the center to the middle of the counter top.")]
        [SerializeField] private float _counterRadius = 2.0f;
        [SerializeField] private float _fallbackCounterHeight = 0.97f;
        [Tooltip("On: the player stands outside the counter (a table in the middle of the room) and stations face outward. " +
                 "Off: the player stands inside a ring counter and stations face the center.")]
        [SerializeField] private bool _stationsFaceOutward = true;
        [Tooltip("Angle around the counter in degrees (0 = +X, 90 = +Z, where the computer is).")]
        [SerializeField] private float _folderAngle = 140f;
        [SerializeField] private float _shredderAngle = 185f;
        [SerializeField] private float _staplerAngle = 265f;
        [SerializeField] private float _boardAngle = 230f;
        [Tooltip("Largest horizontal size of each station in meters.")]
        [SerializeField] private float _folderSize = 1.5f;
        [SerializeField] private float _shredderSize = 1.2f;
        [SerializeField] private float _staplerSize = 0.45f;
        [SerializeField] private float _boardSize = 1.4f;
        [Tooltip("Size of the stapler model shown in the player's hand.")]
        [SerializeField] private float _heldStaplerSize = 0.2f;
        [Tooltip("Extra yaw applied to each model after it is turned to face the center.")]
        [SerializeField] private float _folderYaw = 180f;
        [SerializeField] private float _shredderYaw = 90f;
        [SerializeField] private float _staplerYaw = 0f;
        [SerializeField] private float _boardYaw = 0f;

        [Header("Computer")]
        [SerializeField] private string _siteTitle = "Ingredients";

        private ShiftService _shifts;
        private CustomerService _customers;

        private void Awake()
        {
#if UNITY_EDITOR
            ResolveMissingReferences();
#endif
            RegisterItems();
            ProtoMinigameHud.MouseSprite = _mouseIcon;
            ProtoMinigameHud.CircleArrowSprite = _circleArrowIcon;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor safety net: if the scene in memory predates newly added fields (e.g. it was not reloaded
        /// after an external change), fill missing references from the known assets.
        /// </summary>
        private void ResolveMissingReferences()
        {
            Resolve(ref _dough, "2eaf897075a1472b90814df248edc538");
            Resolve(ref _apple, "5be1b64e3efe4e26ba096f2f7a44c3d8");
            Resolve(ref _preparedDough, "2fab8f6bd91649bd8d05c8fefc78ff44");
            Resolve(ref _preparedApple, "4b58404338d9454685d3077f7932424a");
            Resolve(ref _applePie, "cd7a35fa39eb401380dcb086d92de9df");
            Resolve(ref _stapler, "c9da10980d9449d8a0c8b71ebf511d24");
            Resolve(ref _folderModel, "378b6970db3e55448bcd1776c3f27659");
            Resolve(ref _shredderModel, "b602ad247f80c0744876001eaef0d726");
            Resolve(ref _staplerModel, "462529d527a3c614f82c450557ab769f");
            Resolve(ref _boardModel, "b42f50a40288d83438ec14c67c1f0cca");
            Resolve(ref _folderTexture, "ebd0418187c5c4e42a252a1714ea37d1");
            Resolve(ref _shredderTexture, "fe7e5f648827b39488f50b381ff8edb4");
            Resolve(ref _staplerTexture, "fe7e5f648827b39488f50b381ff8edb4");
            Resolve(ref _boardTexture, "ebd0418187c5c4e42a252a1714ea37d1");
            Resolve(ref _mouseIcon, "f1953862e898410b9d92670da6313311");
            Resolve(ref _circleArrowIcon, "cc259b4f0f1145fb9a8740d2e4c606f2");
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

        private void Start()
        {
            // Stations are placed in the scene (see "Bake stations and office niches into the scene");
            // spawning around the counter is only a fallback for scenes without them.
            ProtoStation[] stations = FindObjectsByType<ProtoStation>(FindObjectsSortMode.None);
            if (stations.Length == 0)
                SpawnStations();
            else
                foreach (ProtoStation station in stations)
                    ConfigureStation(station);

            BuildIngredientsSite();

            if (_stapler != null && _staplerModel != null)
                ProtoHeldModelView.Create(transform, _stapler.Id, _staplerModel, _staplerTexture, _heldStaplerSize);

            ServiceLocator.TryGet(out _customers);
            if (ServiceLocator.TryGet(out _shifts))
            {
                _shifts.OnShiftStarted += ApplyApplePieOrders;
                if (_shifts.ShiftInProgress)
                    ApplyApplePieOrders();
            }
        }

        private void OnDestroy()
        {
            if (_shifts != null)
                _shifts.OnShiftStarted -= ApplyApplePieOrders;
        }

        #region Items and orders
        private void RegisterItems()
        {
            var serializer = new ItemDefinitionAssetSerializer();
            foreach (var asset in new[] { _dough, _apple, _preparedDough, _preparedApple, _applePie, _stapler })
            {
                if (asset == null || ItemRegistry.Instance.TryGet(asset.Id, out _))
                    continue;

                ItemRegistry.Instance.RegisterItem(serializer.Deserialize(asset));
            }
        }

        private void ApplyApplePieOrders()
        {
            if (_customers == null || _applePie == null || !ItemRegistry.Instance.TryGet(_applePie.Id, out ItemDefinition pie))
                return;

            // ShiftService installs its own order source when the shift starts; replace it: apple pie only.
            _customers.SetRandomItemGiver(new FixedItemGiver(pie));
        }

        private sealed class FixedItemGiver : IRandomItemDefinitionGiver
        {
            private readonly ItemDefinition _definition;
            public FixedItemGiver(ItemDefinition definition) => _definition = definition;
            public ItemDefinition GetRandomItemDefinition() => _definition;
        }
        #endregion

        #region Computer
        private void BuildIngredientsSite()
        {
            foreach (var computer in FindObjectsByType<SiteActivator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                ProtoIngredientsSite.Create(computer, new[] { _dough, _apple }, _siteTitle);
                computer.HideTab(ComputerSiteTab.Meals);
            }
        }
        #endregion

        #region Stations
        private void SpawnStations()
        {
            ConfigureStation(SpawnStation<FolderPressStation>("Station_FolderPress", _folderModel, _folderTexture, _folderAngle, _folderSize, _folderYaw));
            ConfigureStation(SpawnStation<ShredderStation>("Station_Shredder", _shredderModel, _shredderTexture, _shredderAngle, _shredderSize, _shredderYaw));
            ConfigureStation(SpawnStation<BoardStation>("Station_Board", _boardModel, _boardTexture, _boardAngle, _boardSize, _boardYaw));
            ConfigureStation(SpawnStation<StaplerStation>("Station_Stapler", _staplerModel, _staplerTexture, _staplerAngle, _staplerSize, _staplerYaw));
        }

        /// <summary>
        /// Gives a station its items and click mode.
        /// </summary>
        private void ConfigureStation(ProtoStation station)
        {
            switch (station)
            {
                case FolderPressStation folder:
                    folder.Input = _dough;
                    folder.Output = _preparedDough;
                    break;

                case ShredderStation shredder:
                    shredder.Input = _apple;
                    shredder.Output = _preparedApple;
                    break;

                case BoardStation board:
                    board.SetUseFocus(false); // plain clicks, no camera lock
                    board.Stapler = _stapler;
                    board.FirstInput = _preparedApple;
                    board.SecondInput = _preparedDough;
                    board.Output = _applePie;
                    break;

                case StaplerStation stapler:
                    stapler.Stapler = _stapler;
                    // Grabbing the stapler is a single click, no need to zoom in.
                    stapler.SetUseFocus(false);
                    break;
            }
        }

#if UNITY_EDITOR
        public const string BakedStationsName = "Stations";

        /// <summary>
        /// Places the stations around the work area and builds the office niches as regular scene objects.
        /// Run once (component menu), then move things by hand and save the scene.
        /// </summary>
        [ContextMenu("Bake stations and office niches into the scene")]
        public void BakeIntoScene()
        {
            ResolveMissingReferences();
            Physics.SyncTransforms();

            Transform old = transform.Find(BakedStationsName);
            if (old != null)
                UnityEditor.Undo.DestroyObjectImmediate(old.gameObject);

            var root = new GameObject(BakedStationsName);
            UnityEditor.Undo.RegisterCreatedObjectUndo(root, "Bake stations");
            root.transform.SetParent(transform, false);

            // The work area: the ring between the central table and the outer L-shaped tables.
            // Position on a table top -> direction to where the player stands.
            BakeStation<ShredderStation>(root.transform, "Station_Shredder", _shredderModel, new Vector3(-3.55f, 0f, 0.5f), Vector3.right, _shredderSize, _shredderYaw);
            BakeStation<FolderPressStation>(root.transform, "Station_FolderPress", _folderModel, new Vector3(0f, 0f, -0.55f), Vector3.back, _folderSize, _folderYaw);
            BakeStation<BoardStation>(root.transform, "Station_Board", _boardModel, new Vector3(0.55f, 0f, 0.1f), Vector3.right, _boardSize, _boardYaw);
            BakeStation<StaplerStation>(root.transform, "Station_Stapler", _staplerModel, new Vector3(3.55f, 0f, -0.5f), Vector3.left, _staplerSize, _staplerYaw);

            GameObject office = GameObject.Find("Office");
            if (office != null)
                ProtoOfficeLayout.Bake(transform, office);
            else
                Debug.LogWarning($"[{nameof(ProtoKitchen)}] No 'Office' object in the scene; niches were not built.", this);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        /// <summary>
        /// Editor: adds number signs to baked niches that don't have one yet.
        /// </summary>
        public int AddMissingNicheSigns()
        {
            int added = ProtoOfficeLayout.AddMissingSigns(transform);
            if (added > 0)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            return added;
        }

        private void BakeStation<T>(Transform parent, string stationName, GameObject model, Vector3 position, Vector3 toViewer, float size, float yaw) where T : ProtoStation
        {
            if (model == null)
            {
                Debug.LogWarning($"[{nameof(ProtoKitchen)}] No model assigned for {stationName}.", this);
                return;
            }

            position.y = FindCounterTop(position);

            var root = new GameObject(stationName) { layer = InteractableLayer };
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, Quaternion.LookRotation(toViewer, Vector3.up) * Quaternion.Euler(0f, yaw, 0f));

            var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(model, root.transform);
            FitToSize(root.transform, visual.transform, size);

            var renderers = root.GetComponentsInChildren<Renderer>();
            AddCollider(root, renderers);

            var hover = root.AddComponent<MeshOutlineHover>();
            var hoverData = new UnityEditor.SerializedObject(hover);
            SerializedPropertyArray(hoverData.FindProperty("_renderers"), renderers);
            hoverData.ApplyModifiedPropertiesWithoutUndo();

            var station = root.AddComponent<T>();
            station.BakePlacement(visual.transform, toViewer);
            ConfigureStation(station);
        }

        private static void SerializedPropertyArray(UnityEditor.SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
#endif

        private T SpawnStation<T>(string stationName, GameObject model, Texture2D texture, float angle, float size, float yaw) where T : ProtoStation
        {
            if (model == null)
            {
                Debug.LogWarning($"[{nameof(ProtoKitchen)}] No model assigned for {stationName}.", this);
                return null;
            }

            float radians = angle * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
            Vector3 position = _counterCenter + direction * _counterRadius;
            position.y = FindCounterTop(position);

            var root = new GameObject(stationName) { layer = InteractableLayer };
            root.transform.SetParent(transform, false);
            root.transform.position = position;
            Vector3 toCenter = _counterCenter - position;
            toCenter.y = 0f;
            // Direction toward where the player stands when using this station.
            Vector3 toViewer = _stationsFaceOutward ? -toCenter : toCenter;
            root.transform.rotation = Quaternion.LookRotation(toViewer.normalized, Vector3.up) * Quaternion.Euler(0f, yaw, 0f);

            GameObject visual = Instantiate(model, root.transform, false);
            visual.name = model.name;
            ApplyTexture(visual, texture);
            FitToSize(root.transform, visual.transform, size);

            var renderers = root.GetComponentsInChildren<Renderer>();
            AddCollider(root, renderers);
            root.AddComponent<MeshOutlineHover>().SetRenderers(renderers);

            var station = root.AddComponent<T>();
            station.Setup(visual.transform);
            station.ConfigureFocus(toViewer);
            return station;
        }

        /// <summary>
        /// Gives every material of the model the station's UV texture.
        /// </summary>
        internal static void ApplyTexture(GameObject visual, Texture2D texture)
        {
            if (texture == null)
                return;

            foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.materials; // instanced copies, the FBX assets stay untouched
                foreach (var material in materials)
                {
                    if (material.HasProperty("_BaseMap"))
                        material.SetTexture("_BaseMap", texture);
                    if (material.HasProperty("_MainTex"))
                        material.SetTexture("_MainTex", texture);
                    if (material.HasProperty("_BaseColor"))
                        material.SetColor("_BaseColor", Color.white);
                    else if (material.HasProperty("_Color"))
                        material.SetColor("_Color", Color.white);
                }
                renderer.materials = materials;
            }
        }

        private float FindCounterTop(Vector3 position)
        {
            // Start a bit above the expected counter top (but below anything hanging over it).
            var origin = new Vector3(position.x, _counterCenter.y + _fallbackCounterHeight + 0.5f, position.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);

            float best = float.NegativeInfinity;
            foreach (var hit in hits)
            {
                if (hit.point.y > best)
                    best = hit.point.y;
            }

            return float.IsNegativeInfinity(best) ? _fallbackCounterHeight : best;
        }

        /// <summary>
        /// Scales the model to the requested footprint and puts its bottom center on the station root.
        /// </summary>
        internal static void FitToSize(Transform root, Transform visual, float size)
        {
            Bounds bounds = GetLocalBounds(root, visual);
            float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprint > 0.0001f)
                visual.localScale *= size / footprint;

            bounds = GetLocalBounds(root, visual);
            Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            visual.localPosition -= bottomCenter;
        }

        private static void AddCollider(GameObject root, Renderer[] renderers)
        {
            Bounds bounds = GetLocalBounds(root.transform, root.transform);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = Vector3.Max(bounds.size, new Vector3(0.1f, 0.1f, 0.1f));
        }

        /// <summary>
        /// Bounds of all renderers under <paramref name="target"/>, in <paramref name="space"/>'s local space.
        /// </summary>
        internal static Bounds GetLocalBounds(Transform space, Transform target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            bool initialized = false;
            var bounds = new Bounds();

            foreach (var renderer in renderers)
            {
                Bounds world = renderer.bounds;
                Vector3 min = world.min, max = world.max;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                    Vector3 local = space.InverseTransformPoint(corner);
                    if (!initialized)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        initialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(local);
                    }
                }
            }

            return bounds;
        }
        #endregion
    }
}
