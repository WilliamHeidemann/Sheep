using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Post_Processing
{
    public class TiltShiftRenderPass : ScriptableRenderPass
    {
        private readonly Material _tiltShiftMaterial;
        private RenderTextureDescriptor _renderTextureDescriptor;

        private static readonly int TiltShiftId = Shader.PropertyToID("_TiltShift");
        private const string TiltShiftTextureName = "_TiltShiftTexture";
        private const string TiltShiftPassName = "TiltShiftRenderPass";

        public TiltShiftRenderPass(Material tiltShiftMaterial)
        {
            _tiltShiftMaterial = tiltShiftMaterial;
            _renderTextureDescriptor =
                new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
        }

        public TiltShiftRenderPass()
        {
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;

            // using var builder = renderGraph.AddRenderPass<TiltShiftRenderPass>(TiltShiftPassName, out var passData);

            if (resourceData.isActiveTargetBackBuffer) return;
            Debug.Log("TiltShiftRenderPass RecordRenderGraph");

            TextureHandle sourceCameraColor = resourceData.activeColorTexture;
            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, _renderTextureDescriptor, TiltShiftTextureName, false);

            _renderTextureDescriptor.width = cameraData.cameraTargetDescriptor.width;
            _renderTextureDescriptor.height = cameraData.cameraTargetDescriptor.height;
            _renderTextureDescriptor.depthBufferBits = 0;

            UpdateBlurSettings();
            if (!sourceCameraColor.IsValid() || !destination.IsValid()) return;

            var blitMaterialParameters = new RenderGraphUtils.BlitMaterialParameters
                (sourceCameraColor, destination, _tiltShiftMaterial, 0);
            renderGraph.AddBlitPass(blitMaterialParameters, TiltShiftPassName);
        }

        private void UpdateBlurSettings()
        {
            if (_tiltShiftMaterial == null) return;

            // var volumeComponent = VolumeManager.instance.stack.GetComponent<TiltShift>();
        }
    }
}