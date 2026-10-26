using Game.Items;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Prototype.Kitchen
{
    /// <summary>
    /// Gives dropped / thrown items of one kind a 3D model instead of the flat sprite (used for the stapler).
    /// Held items are shown by <see cref="ProtoPawView"/>.
    /// </summary>
    public sealed class ProtoHeldModelView : MonoBehaviour
    {
        // Dropped / thrown items: the stapler model a bit bigger (as on the counter), and no sliding on the floor.
        private const float DroppedScale = 1.5f;
        private const float Friction = 1f;

        private static PhysicsMaterial s_gripMaterial;
        private readonly HashSet<PhysicsContainer> _gripped = new();
        private string _itemId;
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

        private GameObject BuildModel(string modelName)
        {
            var root = new GameObject(modelName);
            root.transform.SetParent(transform, false);

            GameObject visual = Instantiate(_modelPrefab, root.transform, false);
            ProtoKitchen.ApplyTexture(visual, _texture);
            ProtoKitchen.FitToSize(root.transform, visual.transform, _size * DroppedScale);

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
            UpdateDroppedModels();
        }

        /// <summary>
        /// A dropped or thrown item lives in a pooled PhysicsContainer; give the ones holding our item the 3D model.
        /// </summary>
        private void UpdateDroppedModels()
        {
            foreach (var container in FindObjectsByType<PhysicsContainer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                ApplyGrip(container);

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

        /// <summary>
        /// High friction for every dropped / thrown item, so it stops where it lands instead of sliding away.
        /// </summary>
        private void ApplyGrip(PhysicsContainer container)
        {
            if (!_gripped.Add(container))
                return;

            s_gripMaterial ??= new PhysicsMaterial("ProtoItemGrip")
            {
                dynamicFriction = Friction,
                staticFriction = Friction,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Maximum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };

            foreach (Collider collider in container.GetComponentsInChildren<Collider>(true))
            {
                if (!collider.isTrigger)
                    collider.sharedMaterial = s_gripMaterial;
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

    }
}
