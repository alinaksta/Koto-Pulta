using Game.Lifecycle;
using Game.Services;
using UnityEngine;

namespace Game.Environment
{
    /// <summary>
    /// Applies the player's cosmetic environment choices to the active scene.
    /// </summary>
    public class CustomizationService : MonoBehaviour, IBootstrapable
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int TopColorId = Shader.PropertyToID("_Top");
        private static readonly int BottomColorId = Shader.PropertyToID("_Bottom");

        private MaterialPropertyBlock _propertyBlock;

        private CustomizationController _controller;
        private FloorCustomization _floor;
        private WallCustomization _walls;
        private PanoramaCustomization _panorama;

        public FloorCustomization Floor => _floor;
        public WallCustomization Walls => _walls;
        public PanoramaCustomization Panorama => _panorama;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        /// <inheritdoc/>
        public void Bootstrap()
        {
            EnsurePropertyBlock();
            ServiceLocator.Register(this);
        }

        /// <summary>
        /// Applies a floor texture to the floor and door.
        /// </summary>
        public void ApplyFloor(FloorCustomization customization)
        {
            if (customization == null)
                return;

            _floor = customization;

            if (_controller == null)
                return;

            SetTexture(_controller.Floor, customization.FloorTexture);
            SetTexture(_controller.Door, customization.DoorTexture);
        }

        /// <summary>
        /// Applies wall colors to the walls and service counter.
        /// </summary>
        public void ApplyWalls(WallCustomization customization)
        {
            if (customization == null)
                return;

            _walls = customization;

            if (_controller == null)
                return;

            SetWallColors(_controller.Walls, customization);
            SetTexture(_controller.ServiceCounter, customization.ServiceCounterTexture);
        }

        /// <summary>
        /// Applies a panorama texture to all registered panorama renderers.
        /// </summary>
        public void ApplyPanorama(PanoramaCustomization customization)
        {
            if (customization == null)
                return;

            _panorama = customization;

            if (_controller == null)
                return;

            foreach (RendererMaterialSlot panorama in _controller.Panoramas)
            {
                SetTexture(panorama, customization.Texture);
            }
        }

        public void Bind(CustomizationController controller)
        {
            _controller = controller;

            if (_floor != null)
                ApplyFloor(_floor);

            if (_walls != null)
                ApplyWalls(_walls);

            if (_panorama != null)
                ApplyPanorama(_panorama);
        }

        public void Unbind(CustomizationController controller)
        {
            if (_controller == controller)
                _controller = null;
        }

        private void SetTexture(RendererMaterialSlot slot, Texture texture)
        {
            if (!TryGetMaterial(slot, BaseMapId, out MeshRenderer renderer))
                return;

            EnsurePropertyBlock();
            _propertyBlock.Clear();
            renderer.GetPropertyBlock(_propertyBlock, slot.MaterialIndex);
            _propertyBlock.SetTexture(BaseMapId, texture);
            renderer.SetPropertyBlock(_propertyBlock, slot.MaterialIndex);
        }

        private void SetWallColors(RendererMaterialSlot slot, WallCustomization customization)
        {
            if (!TryGetMaterial(slot, TopColorId, out MeshRenderer renderer))
                return;

            Material material = renderer.sharedMaterials[slot.MaterialIndex];

            if (!material.HasProperty(BottomColorId))
            {
                Debug.LogWarning(
                    $"Material '{material.name}' does not have property '_Bottom'.",
                    renderer);
                return;
            }

            EnsurePropertyBlock();
            _propertyBlock.Clear();
            renderer.GetPropertyBlock(_propertyBlock, slot.MaterialIndex);
            _propertyBlock.SetColor(TopColorId, customization.TopColor);
            _propertyBlock.SetColor(BottomColorId, customization.BottomColor);
            renderer.SetPropertyBlock(_propertyBlock, slot.MaterialIndex);
        }

        private void EnsurePropertyBlock()
        {
            _propertyBlock ??= new MaterialPropertyBlock();
        }

        private static bool TryGetMaterial(
            RendererMaterialSlot slot,
            int propertyId,
            out MeshRenderer renderer)
        {
            renderer = slot?.Renderer;

            if (renderer == null)
                return false;

            Material[] materials = renderer.sharedMaterials;

            if (slot.MaterialIndex < 0 || slot.MaterialIndex >= materials.Length)
            {
                Debug.LogWarning(
                    $"Material index {slot.MaterialIndex} is invalid for '{renderer.name}'.",
                    renderer);
                return false;
            }

            Material material = materials[slot.MaterialIndex];

            if (material == null || !material.HasProperty(propertyId))
            {
                Debug.LogWarning(
                    $"Material slot {slot.MaterialIndex} on '{renderer.name}' does not have "
                    + $"property ID {propertyId}.",
                    renderer);
                return false;
            }

            return true;
        }
    }
}
