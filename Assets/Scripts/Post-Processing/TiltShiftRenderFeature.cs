using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Post_Processing
{
    public class TiltShiftRenderFeature : ScriptableRendererFeature
    {
        [SerializeField] private TiltShiftSettings _tiltShiftSettings = new();
        [SerializeField] private Shader _shader;
        private Material _material;
        private TiltShiftRenderPass _tiltShiftRenderPass;

        public override void Create()
        {
            _tiltShiftRenderPass = new TiltShiftRenderPass(_tiltShiftSettings.TiltShiftMaterial)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_tiltShiftRenderPass);
        }
    }
}