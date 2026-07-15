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

        public MeshRenderer Renderer => _renderer;
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

        public RendererMaterialSlot Floor => _floor;
        public RendererMaterialSlot Door => _door;
        public RendererMaterialSlot Walls => _walls;
        public RendererMaterialSlot ServiceCounter => _serviceCounter;
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
