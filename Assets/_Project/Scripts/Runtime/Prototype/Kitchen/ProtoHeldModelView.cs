using Game.Interaction;
using Game.Items;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Game.Prototype.Kitchen
{
    /// <summary>
    /// Shows a 3D model in the player's hand instead of the flat item sprite (used for the stapler).
    /// The model is placed in front of the camera at the on-screen position of the hand's held-item image.
    /// </summary>
    public sealed class ProtoHeldModelView : MonoBehaviour
    {
        [SerializeField] private float _distance = 0.45f;
        [Tooltip("Rotation of the model relative to the camera (right hand; mirrored for the left hand).")]
        [SerializeField] private Vector3 _euler = new Vector3(15f, -150f, -10f);
        [Tooltip("Spin of the held model around its own vertical axis, degrees per second.")]
        [SerializeField] private float _spinSpeed = 90f;

        private string _itemId;
        private HandView[] _views = new HandView[0];
        private GameObject[] _models = new GameObject[0];
        private GameObject _modelPrefab;
        private Texture2D _texture;
        private float _size;
        private readonly Dictionary<PhysicsContainer, GameObject> _droppedModels = new();
        private readonly List<PhysicsContainer> _stale = new();

        public static ProtoHeldModelView Create(Transform parent, string itemId, GameObject model, Texture2D texture, float size)
        {
            var go = new GameObject("HeldModels");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<ProtoHeldModelView>();
            view._itemId = itemId;
            view._modelPrefab = model;
            view._texture = texture;
            view._size = size;
            return view;
        }

        private void Start()
        {
            _views = FindObjectsByType<HandView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _models = new GameObject[_views.Length];
            for (int i = 0; i < _views.Length; i++)
                _models[i] = BuildModel($"Held_{_views[i].HandType}");
        }

        private GameObject BuildModel(string modelName)
        {
            var root = new GameObject(modelName);
            root.transform.SetParent(transform, false);

            GameObject visual = Instantiate(_modelPrefab, root.transform, false);
            ProtoKitchen.ApplyTexture(visual, _texture);
            ProtoKitchen.FitToSize(root.transform, visual.transform, _size);

            // Center the model on the root so it rotates around its middle.
            Bounds bounds = ProtoKitchen.GetLocalBounds(root.transform, visual.transform);
            visual.transform.localPosition -= bounds.center;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            root.SetActive(false);
            return root;
        }

        private void LateUpdate()
        {
            UpdateHeldModels();
            UpdateDroppedModels();
        }

        /// <summary>
        /// A dropped or thrown item lives in a pooled PhysicsContainer; give the ones holding our item the 3D model.
        /// </summary>
        private void UpdateDroppedModels()
        {
            foreach (var container in FindObjectsByType<PhysicsContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                bool ours = !container.IsEmpty && container.Item.Value.Definition.Id == _itemId;
                if (ours && !_droppedModels.ContainsKey(container))
                {
                    GameObject model = BuildModel("DroppedModel");
                    model.transform.SetParent(container.transform, false);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.identity;
                    model.SetActive(true);
                    _droppedModels.Add(container, model);
                }
            }

            // Remove models from containers that were picked up / recycled for other items.
            _stale.Clear();
            foreach (var pair in _droppedModels)
            {
                PhysicsContainer container = pair.Key;
                bool stillOurs = container != null && container.isActiveAndEnabled && !container.IsEmpty &&
                                 container.Item.Value.Definition.Id == _itemId;
                if (!stillOurs)
                    _stale.Add(container);
            }

            foreach (var container in _stale)
            {
                if (_droppedModels.TryGetValue(container, out GameObject model) && model != null)
                    Destroy(model);
                _droppedModels.Remove(container);
            }
        }

        private void OnDestroy()
        {
            foreach (var model in _droppedModels.Values)
            {
                if (model != null)
                    Destroy(model);
            }
            _droppedModels.Clear();
        }

        private void UpdateHeldModels()
        {
            Camera camera = Camera.main;

            for (int i = 0; i < _views.Length; i++)
            {
                HandView view = _views[i];
                GameObject model = _models[i];
                if (view == null || model == null)
                    continue;

                Hand hand = view.Hand;
                bool show = camera != null && view.isActiveAndEnabled && hand != null && hand.Visible && !hand.IsEmpty &&
                            hand.Item.Value.Definition.Id == _itemId;

                if (model.activeSelf != show)
                    model.SetActive(show);

                if (!show)
                    continue;

                // Hide the flat icon; the 3D model takes its place.
                Image image = view.HeldItemImage;
                if (image != null)
                    image.enabled = false;

                Vector3 screen = GetScreenPoint(image != null ? image.rectTransform : (RectTransform)view.transform);
                screen.z = _distance;
                model.transform.position = camera.ScreenToWorldPoint(screen);

                Vector3 euler = view.HandType == HandType.Left ? new Vector3(_euler.x, -_euler.y, -_euler.z) : _euler;
                float spin = Time.unscaledTime * _spinSpeed * (view.HandType == HandType.Left ? -1f : 1f);
                model.transform.rotation = camera.transform.rotation * Quaternion.Euler(euler) * Quaternion.AngleAxis(spin, Vector3.up);
            }
        }

        private static Vector3 GetScreenPoint(RectTransform rect)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
            Vector3 center = rect.TransformPoint(rect.rect.center);
            return RectTransformUtility.WorldToScreenPoint(uiCamera, center);
        }
    }
}
