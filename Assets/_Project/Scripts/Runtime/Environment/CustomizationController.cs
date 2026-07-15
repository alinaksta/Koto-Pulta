using Game.Services;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Environment
{
    /// <summary>
    /// Identifies one material slot on a scene renderer.
    /// </summary>
    [Serializable]
    public class RendererMaterialSlot
    {
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField, Min(0)] private int _materialIndex;

        /// <summary>
        /// Gets the renderer that owns the target material slot.
        /// </summary>
        public MeshRenderer Renderer => _renderer;

        /// <summary>
        /// Gets the material index to customize on the renderer.
        /// </summary>
        public int MaterialIndex => _materialIndex;
    }

    /// <summary>
    /// Provides scene renderer references to the customization service.
    /// </summary>
    public class CustomizationController : MonoBehaviour
    {
        [Header("Floor")]
        [SerializeField] private RendererMaterialSlot _floor = new();
        [SerializeField] private RendererMaterialSlot _door = new();

        [Header("Walls")]
        [SerializeField] private RendererMaterialSlot _walls = new();
        [Tooltip("Simple URP Unlit material slot used by the service counter/table.")]
        [SerializeField] private RendererMaterialSlot _serviceCounter = new();

        [Header("Panorama")]
        [SerializeField, FormerlySerializedAs("_windows")]
        private List<RendererMaterialSlot> _panoramas = new();

        /// <summary>
        /// Gets the floor material slot.
        /// </summary>
        public RendererMaterialSlot Floor => _floor;

        /// <summary>
        /// Gets the door material slot.
        /// </summary>
        public RendererMaterialSlot Door => _door;

        /// <summary>
        /// Gets the wall material slot.
        /// </summary>
        public RendererMaterialSlot Walls => _walls;

        /// <summary>
        /// Gets the service counter/table material slot.
        /// </summary>
        public RendererMaterialSlot ServiceCounter => _serviceCounter;

        /// <summary>
        /// Gets all panorama/window material slots.
        /// </summary>
        public IReadOnlyList<RendererMaterialSlot> Panoramas => _panoramas;

        private void OnEnable()
        {
            if (ServiceLocator.TryGet(out CustomizationService service))
                service.Bind(this);
        }

        private void OnDisable()
        {
            if (ServiceLocator.TryGet(out CustomizationService service))
                service.Unbind(this);
        }
    }
}
