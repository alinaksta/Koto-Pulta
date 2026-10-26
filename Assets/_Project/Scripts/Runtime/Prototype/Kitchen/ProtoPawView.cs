using Game.Interaction;
using Game.Items;
using Game.Items.Properties;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Game.Prototype.Kitchen
{
    /// <summary>
    /// 3D cat paws instead of the 2D hand UI. The arms grow from the shoulders:
    /// an empty paw hangs down (relaxed), a paw holding something closes into a fist around it
    /// and points where the camera looks.
    /// </summary>
    public sealed class ProtoPawView : MonoBehaviour
    {
        private struct HeldModel
        {
            public GameObject Model;
            public Texture2D Texture;
            public float Size;
        }

        [Header("Shoulders (right one, relative to the camera; mirrored for the left)")]
        [SerializeField] private Vector3 _shoulder = new Vector3(0.34f, -0.3f, 0.02f);
        [Tooltip("Shoulder to wrist, meters.")]
        [SerializeField] private float _armLength = 0.42f;
        [Tooltip("Turn of the paw around the arm, degrees.")]
        [SerializeField] private float _roll;

        [Header("Directions (right arm; mirrored for the left)")]
        [Tooltip("Empty paw: hangs down, relative to the body (camera yaw only).")]
        [SerializeField] private Vector3 _hangingDirection = new Vector3(0.05f, -1f, 0.35f);
        [Tooltip("Paw with an item: relative to the camera, i.e. it follows the view.")]
        [SerializeField] private Vector3 _holdingDirection = new Vector3(0.3f, -0.15f, 1f);
        [SerializeField] private float _turnSpeed = 12f;

        [Header("Held item, in the paw (same for both paws; X is mirrored for the left one)")]
        [Tooltip("Sideways from the arm: outward (+) / inward (-).")]
        [SerializeField, Range(-0.4f, 0.4f)] private float _itemX = 0f;
        [Tooltip("Above (+) / below (-) the arm.")]
        [SerializeField, Range(-0.4f, 0.4f)] private float _itemY = 0.035f;
        [Tooltip("Along the arm, from the shoulder.")]
        [SerializeField, Range(0f, 1.2f)] private float _itemZ = 0.55f;
        [SerializeField] private float _itemSize = 0.7f;

        [Header("3D held items (e.g. the stapler), left paw; the right paw is the mirror image")]
        [Tooltip("Where the item's front (its working end) points: turn left (-) / right (+), degrees from straight ahead.")]
        [SerializeField, Range(-180f, 180f)] private float _heldYaw = 35f;
        [Tooltip("Where the item's front points: up (-) / down (+), degrees.")]
        [SerializeField, Range(-90f, 90f)] private float _heldPitch = -40f;
        [Tooltip("Turn around the front direction, seen from the end, degrees (0 = the item's top faces up on screen).")]
        [SerializeField, Range(-180f, 180f)] private float _heldRoll;
        [Tooltip("The item's front was detected at the wrong end: use the other one.")]
        [SerializeField] private bool _flipHeldFront;
        [Tooltip("Size multiplier for 3D held items.")]
        [SerializeField] private float _heldModelScale = 2f;

        [Header("Animation")]
        [Tooltip("Speed of the closing / opening toes animation.")]
        [SerializeField] private float _gripAnimationSpeed = 3f;

        private readonly Dictionary<string, HeldModel> _heldModels = new();
        private readonly List<Paw> _paws = new();
        private readonly List<Graphic> _handGraphics = new();
        [Header("Models")]
        [SerializeField] private GameObject _leftPrefab;
        [SerializeField] private GameObject _rightPrefab;
        [Tooltip("Paw objects already placed in the scene (their first child is the model). Created at runtime when empty.")]
        [SerializeField] private Transform _leftPaw;
        [SerializeField] private Transform _rightPaw;
        private Transform _camera;

        public static ProtoPawView Create(Transform parent, GameObject leftPaw, GameObject rightPaw)
        {
            var go = new GameObject("Paws");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<ProtoPawView>();
            view._leftPrefab = leftPaw;
            view._rightPrefab = rightPaw;
            return view;
        }

        /// <summary>
        /// Shows a 3D model in the paw for this item instead of its sprite.
        /// </summary>
        public void AddHeldModel(string itemId, GameObject model, Texture2D texture, float size)
        {
            if (!string.IsNullOrEmpty(itemId) && model != null)
                _heldModels[itemId] = new HeldModel { Model = model, Texture = texture, Size = size };
        }

        private void Start()
        {
            var interactor = FindAnyObjectByType<DualHandInteractor>();
            Camera camera = Camera.main;
            if (interactor == null || camera == null)
            {
                Debug.LogWarning($"[{nameof(ProtoPawView)}] No player or camera; paws are off.", this);
                return;
            }

            _camera = camera.transform;

            // The 2D hands are replaced; their canvas keeps working (crosshair, hints, throw trajectory) but draws no hands.
            foreach (HandView view in FindObjectsByType<HandView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                _handGraphics.AddRange(view.GetComponentsInChildren<Graphic>(true));

            AddPaw(HandType.Left, _leftPrefab, _leftPaw, interactor.GetHand(HandType.Left));
            AddPaw(HandType.Right, _rightPrefab, _rightPaw, interactor.GetHand(HandType.Right));
        }

        private void OnDestroy()
        {
            foreach (Paw paw in _paws)
            {
                if (paw.Hand != null)
                    paw.Hand.OnItemChanged -= paw.HandleItemChanged;
                if (paw.Root != null)
                    Destroy(paw.Root.gameObject);
            }
        }

        /// <summary>
        /// Editor: links paw objects placed in the scene.
        /// </summary>
        public void SetSceneObjects(GameObject leftPrefab, GameObject rightPrefab, Transform leftPaw, Transform rightPaw)
        {
            _leftPrefab = leftPrefab;
            _rightPrefab = rightPrefab;
            _leftPaw = leftPaw;
            _rightPaw = rightPaw;
        }

        private void AddPaw(HandType type, GameObject prefab, Transform sceneRoot, Hand hand)
        {
            Transform sceneModel = sceneRoot != null && sceneRoot.childCount > 0 ? sceneRoot.GetChild(0) : null;
            if ((prefab == null && sceneModel == null) || hand == null)
                return;

            var paw = new Paw(this, type, hand);
            paw.Side = type == HandType.Right ? 1f : -1f;

            // Root: at the shoulder, +Z along the arm.
            Transform model;
            if (sceneModel != null)
            {
                paw.Root = sceneRoot;
                model = sceneModel;
            }
            else
            {
                paw.Root = new GameObject($"Paw_{type}").transform;
                paw.Root.SetParent(transform, false);
                model = Instantiate(prefab, paw.Root, false).transform;
                model.name = prefab.name;
            }

            // Measure the model at its neutral pose.
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
            model.localScale = Vector3.one;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            // Measure the arm in the model's own space: shoulder bone -> wrist bone.
            Transform arm = FindBone(model, "Arm");
            Transform wrist = FindBone(model, "Paw");
            Vector3 armLocal = arm != null ? model.InverseTransformPoint(arm.position) : Vector3.zero;
            Vector3 wristLocal = wrist != null ? model.InverseTransformPoint(wrist.position) : Vector3.up;
            Vector3 axis = wristLocal - armLocal;
            float length = axis.magnitude > 0.0001f ? axis.magnitude : 1f;
            axis = axis.magnitude > 0.0001f ? axis / length : Vector3.up;

            float scale = _armLength / length;
            Quaternion rotation = Quaternion.AngleAxis(_roll * paw.Side, Vector3.forward) * Quaternion.FromToRotation(axis, Vector3.forward);
            model.localScale = Vector3.one * scale;
            model.localRotation = rotation;
            model.localPosition = -(rotation * (armLocal * scale));

            paw.Animation = model.GetComponentInChildren<UnityEngine.Animation>(true);
            if (paw.Animation != null)
            {
                paw.Animation.playAutomatically = false;
                paw.Relaxed = FindClip(paw.Animation, "Relaxed");
                paw.Fist = FindClip(paw.Animation, "Fist");
                paw.Close = FindClip(paw.Animation, "Relaxed_To_Fist");
                paw.Open = FindClip(paw.Animation, "Fist_To_Relaxed");
                foreach (AnimationState state in paw.Animation)
                {
                    bool transition = state.name == paw.Close || state.name == paw.Open;
                    state.wrapMode = transition ? WrapMode.ClampForever : WrapMode.Loop;
                    if (transition)
                        state.speed = _gripAnimationSpeed;
                }
            }

            paw.Holder = new GameObject("HeldItem").transform;
            paw.Holder.SetParent(paw.Root, false);

            var spriteObject = new GameObject("ItemSprite");
            spriteObject.transform.SetParent(paw.Holder, false);
            paw.ItemSprite = spriteObject.AddComponent<SpriteRenderer>();
            paw.ItemSprite.shadowCastingMode = ShadowCastingMode.Off;
            paw.ItemSprite.enabled = false;

            paw.Direction = HangingDirection(paw.Side);
            hand.OnItemChanged += paw.HandleItemChanged;
            _paws.Add(paw);
            paw.HandleItemChanged(hand.Item);
            if (!hand.IsEmpty)
                paw.Direction = HoldingDirection(paw.Side);
            if (paw.Animation != null && paw.Relaxed != null && hand.IsEmpty)
                paw.Animation.Play(paw.Relaxed);
        }

        private Vector3 Mirror(Vector3 v, float side) => new Vector3(v.x * side, v.y, v.z);

        private Quaternion BodyRotation => Quaternion.Euler(0f, _camera.eulerAngles.y, 0f);

        private Vector3 HangingDirection(float side) => (BodyRotation * Mirror(_hangingDirection, side)).normalized;

        private Vector3 HoldingDirection(float side) => (_camera.rotation * Mirror(_holdingDirection, side)).normalized;

        private void LateUpdate()
        {
            if (_camera == null)
                return;

            foreach (Graphic graphic in _handGraphics)
            {
                if (graphic != null && graphic.enabled)
                    graphic.enabled = false;
            }

            foreach (Paw paw in _paws)
                paw.Tick(Time.deltaTime);
        }

        #region Paw
        private sealed class Paw
        {
            private readonly ProtoPawView _view;
            public readonly HandType Type;
            public readonly Hand Hand;

            public float Side;
            public Transform Root;
            public UnityEngine.Animation Animation;
            public string Relaxed, Fist, Close, Open;
            public Transform Holder;
            public SpriteRenderer ItemSprite;
            public Vector3 Direction;

            private GameObject _itemModel;
            private Quaternion _itemModelFrame = Quaternion.identity; // model axes: forward = its front, up = its top
            private string _itemModelId;
            private bool _holding;

            public Paw(ProtoPawView view, HandType type, Hand hand)
            {
                _view = view;
                Type = type;
                Hand = hand;
            }

            public void HandleItemChanged(Item? item)
            {
                if (item.HasValue)
                {
                    ShowItem(item.Value);
                    if (!_holding)
                        Play(Close, Fist);
                    _holding = true;
                }
                else if (_holding)
                {
                    _holding = false;
                    HideItem();
                    Play(Open, Relaxed);
                }
            }

            private void Play(string first, string then)
            {
                if (Animation == null)
                    return;

                if (first != null)
                {
                    Animation.Stop();
                    Animation.Play(first);
                    if (then != null)
                        Animation.CrossFadeQueued(then, 0.05f, QueueMode.CompleteOthers);
                }
                else if (then != null)
                {
                    Animation.Play(then);
                }
            }

            private void ShowItem(Item item)
            {
                string id = item.Definition.Id;
                if (_view._heldModels.TryGetValue(id, out HeldModel held))
                {
                    if (_itemModelId != id)
                    {
                        if (_itemModel != null)
                            Object.Destroy(_itemModel);
                        _itemModel = BuildModel(held);
                        _itemModelId = id;
                    }

                    _itemModel.SetActive(true);
                    ItemSprite.enabled = false;
                    return;
                }

                if (_itemModel != null)
                    _itemModel.SetActive(false);

                Sprite sprite = item.Definition.TryGetProperty<FoodProperty>(out var food)
                    ? (food.WorldSprite != null ? food.WorldSprite : food.DialogueSprite)
                    : null;
                ItemSprite.sprite = sprite;
                ItemSprite.enabled = sprite != null;
                if (sprite != null)
                {
                    float size = Mathf.Max(0.0001f, Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
                    ItemSprite.transform.localScale = Vector3.one * (_view._itemSize / size);
                }
            }

            private void HideItem()
            {
                ItemSprite.enabled = false;
                if (_itemModel != null)
                    _itemModel.SetActive(false);
            }

            private GameObject BuildModel(HeldModel held)
            {
                // Built at the world origin with no rotation, so measuring doesn't depend on where the camera looks.
                var root = new GameObject("HeldModel");
                GameObject visual = Object.Instantiate(held.Model, root.transform, false);
                ProtoKitchen.ApplyTexture(visual, held.Texture);
                ProtoKitchen.FitToSize(root.transform, visual.transform, held.Size * _view._heldModelScale);
                Bounds bounds = MeshBounds(root.transform, visual.transform);
                visual.transform.localPosition -= bounds.center;

                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }

                _itemModelFrame = DetectFrame(root.transform, visual.transform, _view._flipHeldFront);
                root.transform.SetParent(Holder, false);
                ApplyModelPose(root.transform);
                return root;
            }

            /// <summary>
            /// Turns the held model so its front points along the pose direction and its top along the pose up
            /// (the right paw gets the mirror image: direction reflected left/right, roll negated).
            /// </summary>
            private void ApplyModelPose(Transform model)
            {
                float yaw = _view._heldYaw * -Side;   // tuned on the left paw (Side = -1)
                float roll = _view._heldRoll * -Side;

                Vector3 front = Quaternion.Euler(_view._heldPitch, yaw, 0f) * Vector3.forward;
                Vector3 up = Vector3.ProjectOnPlane(Vector3.up, front);
                if (up.sqrMagnitude < 0.0001f)
                    up = Vector3.ProjectOnPlane(Vector3.back, front);
                up = Quaternion.AngleAxis(roll, front) * up.normalized;

                model.localRotation = Quaternion.LookRotation(front, up) * Quaternion.Inverse(_itemModelFrame);
            }

            public void Tick(float deltaTime)
            {
                Transform camera = _view._camera;

                bool visible = Hand.Visible;
                if (Root.gameObject.activeSelf != visible)
                    Root.gameObject.SetActive(visible);
                if (!visible)
                    return;

                // The arm grows from the shoulder: hanging down when empty, following the view when holding something.
                Vector3 target = _holding ? _view.HoldingDirection(Side) : _view.HangingDirection(Side);
                Direction = Vector3.Slerp(Direction, target, 1f - Mathf.Exp(-_view._turnSpeed * deltaTime)).normalized;

                Root.position = camera.TransformPoint(_view.Mirror(_view._shoulder, Side));
                Vector3 up = Vector3.ProjectOnPlane(camera.up, Direction);
                Root.rotation = Quaternion.LookRotation(Direction, up.sqrMagnitude > 0.0001f ? up : Vector3.up);

                // The item sits at the toe tips, facing the camera.
                // The item is held by the paw: it moves with the arm (sliders on this component), facing the camera.
                Holder.localPosition = new Vector3(_view._itemX * Side, _view._itemY, _view._itemZ);
                Holder.rotation = camera.rotation;

                if (_itemModel != null && _itemModel.activeSelf)
                    ApplyModelPose(_itemModel.transform);
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Bounds of the model's meshes in <paramref name="space"/>, from the meshes themselves
        /// (not the world-aligned renderer boxes, which change with the model's rotation).
        /// </summary>
        private static Bounds MeshBounds(Transform space, Transform model)
        {
            bool initialized = false;
            var bounds = new Bounds();
            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                    continue;

                Bounds mesh = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? mesh.min.x : mesh.max.x, (i & 2) == 0 ? mesh.min.y : mesh.max.y, (i & 4) == 0 ? mesh.min.z : mesh.max.z);
                    Vector3 local = space.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if (initialized)
                        bounds.Encapsulate(local);
                    else
                        bounds = new Bounds(local, Vector3.zero);
                    initialized = true;
                }
            }

            return bounds;
        }

        /// <summary>
        /// The model's own axes in <paramref name="space"/>: front = along its longest side, toward the working end;
        /// top = from its "down" part to its "up" part (the stapler's base and arm). As a rotation: forward = front, up = top.
        /// </summary>
        private static Quaternion DetectFrame(Transform space, Transform model, bool flipFront)
        {
            Bounds all = MeshBounds(space, model);
            Vector3 size = all.size;
            Vector3 front = size.x >= size.y && size.x >= size.z ? Vector3.right : size.y >= size.z ? Vector3.up : Vector3.forward;

            Transform upPart = FindPart(model, "up");
            Transform downPart = FindPart(model, "down");
            Vector3 top = upPart != null && downPart != null
                ? MeshBounds(space, upPart).center - MeshBounds(space, downPart).center
                : Vector3.zero;
            top = Vector3.ProjectOnPlane(top, front);
            if (top.sqrMagnitude < 0.000001f)
                top = size.x <= size.y && size.x <= size.z ? Vector3.right : size.y <= size.z ? Vector3.up : Vector3.forward;

            // The working end: the end of the long side where the "up" part reaches further out.
            if (upPart != null)
            {
                Bounds arm = MeshBounds(space, upPart);
                float towardPlus = Vector3.Dot(arm.max - all.center, front);
                float towardMinus = -Vector3.Dot(arm.min - all.center, front);
                if (towardMinus > towardPlus)
                    front = -front;
            }

            if (flipFront)
                front = -front;

            return Quaternion.LookRotation(front, top.normalized);
        }

        private static Transform FindPart(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }

            return null;
        }

        private static Transform FindBone(Transform root, string prefix)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.StartsWith(prefix + ".") || child.name == prefix)
                    return child;
            }

            return null;
        }

        /// <summary>Clip name by its base name ("Fist", "Relaxed_To_Fist", ...), ignoring prefixes and the .L/.R suffix.</summary>
        private static string FindClip(UnityEngine.Animation animation, string baseName)
        {
            foreach (AnimationState state in animation)
            {
                string name = state.name;
                int bar = name.LastIndexOf('|');
                if (bar >= 0)
                    name = name.Substring(bar + 1);
                if (name.EndsWith(".L") || name.EndsWith(".R"))
                    name = name.Substring(0, name.Length - 2);

                if (name == baseName)
                    return state.name;
            }

            return null;
        }
        #endregion
    }
}
