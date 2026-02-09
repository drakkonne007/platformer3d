using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class UnderwaterRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material material;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public Settings settings = new Settings();
    private UnderwaterRenderPass _pass;

    public override void Create()
    {
        if (_pass != null) _pass.Dispose();
        _pass = new UnderwaterRenderPass(settings.material);
        _pass.renderPassEvent = settings.renderPassEvent;
    }

    protected override void Dispose(bool disposing)
    {
        if (_pass != null) _pass.Dispose();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material != null)
        {
            renderer.EnqueuePass(_pass);
        }
    }

    class UnderwaterRenderPass : ScriptableRenderPass
    {
        private Material _material;
        private RTHandle _tempTexture;

        public UnderwaterRenderPass(Material material)
        {
            _material = material;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        // --- NEW RENDER GRAPH API (Unity 6+) ---
        public override void RecordRenderGraph(UnityEngine.Rendering.RenderGraphModule.RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            TextureHandle activeColor = resourceData.activeColorTexture;
            if (!activeColor.IsValid()) return;

            // Use TextureDesc for Unity 6 compatibility
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            TextureDesc textureDesc = new TextureDesc(desc.width, desc.height);
            textureDesc.format = desc.graphicsFormat;
            textureDesc.name = "_TempUnderwaterTexture";

            TextureHandle tempTex = renderGraph.CreateTexture(textureDesc);

            // Pass 1: ActiveColor -> Temp (with material)
            using (var builder = renderGraph.AddRasterRenderPass<BlitPassData>("Underwater To Temp", out var passData))
            {
                passData.source = activeColor;
                passData.material = _material;

                builder.UseTexture(passData.source, AccessFlags.Read);
                // Important for Unity 6: explicitly use Depth if shader samples it
                builder.UseTexture(resourceData.activeDepthTexture, AccessFlags.Read);
                
                builder.SetRenderAttachment(tempTex, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((BlitPassData data, RasterGraphContext context) =>
                {
                    // Use Blitter for Unity 6 RenderGraph. It correctly handles TextureHandle -> Attachment logic internally.
                    // Important: Ensure the shader uses _BlitTexture instead of _MainTex if Blitter sets it.
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // Pass 2: Temp -> ActiveColor
            using (var builder = renderGraph.AddRasterRenderPass<BlitPassData>("Underwater Back To Source", out var passData))
            {
                passData.source = tempTex;
                passData.material = null;

                builder.UseTexture(passData.source, AccessFlags.Read);
                builder.SetRenderAttachment(activeColor, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((BlitPassData data, RasterGraphContext context) =>
                {
                    // Simple blit back to source
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0.0f, false);
                });
            }
        }

        private class BlitPassData
        {
            public TextureHandle source;
            public Material material;
        }

        // --- LEGACY API (Compatibility Mode) ---
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("UnderwaterDistortion");
            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // Re-allocate temp texture if needed (Legacy way)
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _tempTexture, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempUnderwaterTexture");

            Blit(cmd, source, _tempTexture, _material);
            Blit(cmd, _tempTexture, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            _tempTexture?.Release();
        }
    }
}
