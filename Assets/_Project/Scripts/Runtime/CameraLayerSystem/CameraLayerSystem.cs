using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.CameraLayerSystem
{
    /// <summary>
    /// Stores positional and rotational offsets for a camera layer.
    /// </summary>
    public class CameraLayer
    {
        /// <summary>
        /// Gets or sets whether this layer adds onto previous layers instead of replacing them.
        /// </summary>
        public bool additive;

        /// <summary>
        /// Gets or sets whether position is interpreted in world space.
        /// </summary>
        public bool global;

        /// <summary>
        /// Gets or sets the positional offset applied by this layer.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// Gets or sets the rotational offset applied by this layer.
        /// </summary>
        public Quaternion rotation;

        /// <summary>
        /// Creates a camera layer with the supplied blend behavior.
        /// </summary>
        /// <param name="additive">Whether the layer adds to earlier layers.</param>
        /// <param name="global">Whether position should be interpreted in world space.</param>
        public CameraLayer(bool additive, bool global)
        {
            this.additive = additive;
            this.global = global;
            rotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Composes ordered camera layers on top of a source transform.
    /// </summary>
    /// <typeparam name="ELayer">Enum used to identify layers.</typeparam>
    public abstract class CameraLayerSystem<ELayer> : MonoBehaviour
        where ELayer : Enum
    {
        [Header("Dependencies")]
        [SerializeField] private Transform _source;
        [SerializeField] private Transform _target;

        /// <summary>
        /// Gets the base source transform used before layers are applied.
        /// </summary>
        public Transform Source => _source;

        /// <summary>
        /// Gets the target transform that receives the composed result.
        /// </summary>
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
        /// Appends a layer if it is missing, or updates and returns the existing layer.
        /// </summary>
        /// <param name="id">Identifier of the requested layer.</param>
        /// <param name="additive">Whether the layer adds onto previous layers instead of replacing them.</param>
        /// <param name="global">Whether the layer position should be interpreted in world space.</param>
        /// <returns>The created or existing layer.</returns>
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

        /// <summary>
        /// Tries to get a previously registered layer.
        /// </summary>
        /// <param name="id">Identifier of the requested layer.</param>
        /// <param name="layer">Receives the layer when found.</param>
        /// <returns><see langword="true"/> when the layer exists.</returns>
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
