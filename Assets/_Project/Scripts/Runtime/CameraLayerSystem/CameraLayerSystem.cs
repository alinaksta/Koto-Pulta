using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.CameraLayerSystem
{
    public class CameraLayer
    {
        public bool additive;
        public bool global;
        public Vector3 position;
        public Quaternion rotation;

        public CameraLayer(bool additive, bool global)
        {
            this.additive = additive;
            this.global = global;
            rotation = Quaternion.identity;
        }
    }

    public abstract class CameraLayerSystem<ELayer> : MonoBehaviour
        where ELayer : Enum
    {
        [Header("Dependencies")]
        [SerializeField] private Transform _source;
        [SerializeField] private Transform _target;

        public Transform Source => _source;
        public Transform Target => _target;

        private List<CameraLayer> _layers;
        private Dictionary<ELayer, CameraLayer> _layerReferences;

        private void Awake()
        {
            _layers = new List<CameraLayer>();
            _layerReferences = new Dictionary<ELayer, CameraLayer>();
            SetupLayers();
            LateAwake();
        }

        protected virtual void LateAwake() { }

        protected abstract void SetupLayers();

        /// <summary>
        /// Appends layer if it is missing. If it is present, modifies the layer to match the arguments and returns it.
        /// </summary>
        /// <param name="id">id of the requested layer</param>
        /// <param name="additive">should layer add its values to previous layer, or overwrite it</param>
        /// <param name="global">should position be calculated based on rotation</param>
        /// <returns>Created or existing layer</returns>
        public CameraLayer AppendLayer(ELayer id, bool additive = true, bool global = false)
        {
            if (_layerReferences.TryGetValue(id, out var value))
            {
                value.additive = additive;
                value.global = global;
                return value;
            }

            var created = new CameraLayer(additive, global);
            _layers.Add(created);
            _layerReferences.Add(id, created);
            return created;
        }

        public bool TryGetLayer(ELayer id, out CameraLayer layer)
        {
            return _layerReferences.TryGetValue(id, out layer);
        }

        private void LateUpdate()
        {
            Vector3 position = _source.position;
            Quaternion rotation = _source.rotation;

            for (int i = 0; i < _layers.Count; i++)
            {
                var layer = _layers[i];

                if (layer.additive)
                {
                    if (layer.global)
                    {
                        position += layer.position;
                        rotation = layer.rotation * rotation;
                    }
                    else
                    {
                        position += rotation * layer.position;
                        rotation = rotation * layer.rotation;
                    }
                }
                else
                {
                    position = layer.position;
                    rotation = layer.rotation;
                }
            }
            
            _target.SetPositionAndRotation(position, rotation);
        }
    }
}
