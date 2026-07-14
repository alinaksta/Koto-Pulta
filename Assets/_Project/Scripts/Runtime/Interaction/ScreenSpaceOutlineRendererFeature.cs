using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Game.Interaction
{
    /// <summary>
    /// Draws renderers marked by hover components into a mask and composites an outline around them.
    /// </summary>
    public class ScreenSpaceOutlineRendererFeature : ScriptableRendererFeature
    {
        internal const uint RenderingLayerMask = 1u << 31;

        private const string MaskShaderName = "Hidden/Game/ScreenSpaceOutlineMask";
        private const string CompositeShaderName = "Hidden/Game/ScreenSpaceOutlineComposite";

        private static Color s_outlineColor = Color.white;
        private static float s_outlineWidth = 2f;

        [SerializeField] private Color _defaultColor = Color.white;
        [SerializeField, Range(1f, 12f)] private float _defaultWidth = 2f;
        [SerializeField, Range(0f, 1f)] private float _fringeAlphaMin = 0.05f;
        [SerializeField, Range(0f, 1f)] private float _fringeAlphaMax = 0.65f;
        [SerializeField, Range(0f, 1f)] private float _fringeBlendStrength = 1f;

        private Material _maskMaterial;
        private Material _compositeMaterial;
        private OutlinePass _pass;

        internal static void SetVisuals(Color color, float width)
        {
            s_outlineColor = color;
            s_outlineWidth = Mathf.Max(1f, width);
        }

        /// <inheritdoc/>
        public override void Create()
        {
            _maskMaterial = CreateMaterial(MaskShaderName, _maskMaterial);
            _compositeMaterial = CreateMaterial(CompositeShaderName, _compositeMaterial);
            _pass = new OutlinePass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_maskMaterial == null || _compositeMaterial == null)
                return;

            var color = s_outlineColor == default ? _defaultColor : s_outlineColor;
            var width = s_outlineWidth <= 0f ? _defaultWidth : s_outlineWidth;
            _pass.Setup(
                _maskMaterial,
                _compositeMaterial,
                color,
                width,
                _fringeAlphaMin,
                Mathf.Max(_fringeAlphaMin, _fringeAlphaMax),
                _fringeBlendStrength);
            renderer.EnqueuePass(_pass);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_maskMaterial);
            CoreUtils.Destroy(_compositeMaterial);
        }

        private static Material CreateMaterial(string shaderName, Material current)
        {
            if (current != null)
                return current;

            var shader = Shader.Find(shaderName);
            return shader != null ? CoreUtils.CreateEngineMaterial(shader) : null;
        }

        private class OutlinePass : ScriptableRenderPass
        {
            private static readonly int OutlineMaskId = Shader.PropertyToID("_ScreenSpaceOutlineMask");
            private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
            private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
            private static readonly int FringeAlphaMinId = Shader.PropertyToID("_FringeAlphaMin");
            private static readonly int FringeAlphaMaxId = Shader.PropertyToID("_FringeAlphaMax");
            private static readonly int FringeBlendStrengthId = Shader.PropertyToID("_FringeBlendStrength");

            private readonly List<ShaderTagId> _shaderTags = new()
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("LightweightForward")
            };

            private Material _maskMaterial;
            private Material _compositeMaterial;

            public void Setup(
                Material maskMaterial,
                Material compositeMaterial,
                Color color,
                float width,
                float fringeAlphaMin,
                float fringeAlphaMax,
                float fringeBlendStrength)
            {
                _maskMaterial = maskMaterial;
                _compositeMaterial = compositeMaterial;
                _compositeMaterial.SetColor(OutlineColorId, color);
                _compositeMaterial.SetFloat(OutlineWidthId, width);
                _compositeMaterial.SetFloat(FringeAlphaMinId, fringeAlphaMin);
                _compositeMaterial.SetFloat(FringeAlphaMaxId, fringeAlphaMax);
                _compositeMaterial.SetFloat(FringeBlendStrengthId, fringeBlendStrength);
                requiresIntermediateTexture = true;
            }

            /// <inheritdoc/>
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
                    return;

                var maskDescriptor = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
                maskDescriptor.name = "Screen Space Outline Mask";
                maskDescriptor.colorFormat = GraphicsFormat.R8_UNorm;
                maskDescriptor.depthBufferBits = DepthBits.None;
                maskDescriptor.msaaSamples = MSAASamples.None;
                maskDescriptor.clearBuffer = true;
                maskDescriptor.clearColor = Color.clear;
                var mask = renderGraph.CreateTexture(maskDescriptor);

                AddMaskPass(renderGraph, frameData, mask, resourceData.activeDepthTexture);

                var source = resourceData.activeColorTexture;
                var destinationDescriptor = renderGraph.GetTextureDesc(source);
                destinationDescriptor.name = "Camera Color With Screen Space Outline";
                destinationDescriptor.clearBuffer = false;
                var destination = renderGraph.CreateTexture(destinationDescriptor);

                using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>(
                           "Screen Space Outline Composite", out var passData))
                {
                    passData.source = source;
                    passData.mask = mask;
                    passData.material = _compositeMaterial;

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.UseTexture(mask, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1f, 1f, 0f, 0f), data.material, 0);
                    });
                }

                resourceData.cameraColor = destination;
            }

            private void AddMaskPass(
                RenderGraph renderGraph,
                ContextContainer frameData,
                TextureHandle mask,
                TextureHandle depth)
            {
                var renderingData = frameData.Get<UniversalRenderingData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                var lightData = frameData.Get<UniversalLightData>();
                var filteringSettings = new FilteringSettings(RenderQueueRange.all)
                {
                    renderingLayerMask = RenderingLayerMask
                };
                var drawingSettings = RenderingUtils.CreateDrawingSettings(
                    _shaderTags,
                    renderingData,
                    cameraData,
                    lightData,
                    SortingCriteria.CommonTransparent);
                drawingSettings.overrideMaterial = _maskMaterial;

                var rendererListParams = new RendererListParams(
                    renderingData.cullResults,
                    drawingSettings,
                    filteringSettings);
                var rendererList = renderGraph.CreateRendererList(rendererListParams);

                using var builder = renderGraph.AddRasterRenderPass<MaskPassData>(
                    "Screen Space Outline Mask", out var passData);
                passData.rendererList = rendererList;
                builder.UseRendererList(rendererList);
                builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(depth, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(mask, OutlineMaskId);
                builder.SetRenderFunc(static (MaskPassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }

            private class MaskPassData
            {
                public RendererListHandle rendererList;
            }

            private class CompositePassData
            {
                public TextureHandle source;
                public TextureHandle mask;
                public Material material;
            }
        }
    }
}
