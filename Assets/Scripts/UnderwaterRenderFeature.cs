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
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing; // Safer event
    }

    public Settings settings = new Settings();
    private UnderwaterRenderPass _pass;

    public override void Create()
    {
        // Force the event to be safe if it was set to something dangerous in inspector
        if (settings.renderPassEvent == RenderPassEvent.AfterRendering)
            settings.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            
        _pass = new UnderwaterRenderPass(settings.material);
        _pass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material == null)
        {
            Debug.LogWarning("UnderwaterRenderFeature: Material is missing. Effect will not render.");
            return;
        }
        renderer.EnqueuePass(_pass);
    }

    class UnderwaterRenderPass : ScriptableRenderPass
    {
        private Material _material;
        private RTHandle _tempTexture;

        public UnderwaterRenderPass(Material material)
        {
            _material = material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null)
            {
                 //Debug.LogWarning("Missing material in RecordRenderGraph");
                 return;
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // 1. Get Source Texture (Safely)
            TextureHandle source = resourceData.activeColorTexture;
            
            // Fallback if activeColor is not valid (e.g. in AfterRendering event)
            if (!source.IsValid()) source = resourceData.cameraColor;
            
            if (!source.IsValid())
            {
                //Debug.LogError("UnderwaterRenderPass: No valid source texture found!");
                return;
            }

            // 2. Create Temp Texture
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            TextureDesc texDesc = new TextureDesc(desc.width, desc.height);
            texDesc.format = desc.graphicsFormat;
            texDesc.name = "_UnderwaterTemp";

            TextureHandle tempTex = renderGraph.CreateTexture(texDesc);

            // 3. Pass 1: Source -> Temp (Apply Effect)
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Underwater Apply", out var passData))
            {
                passData.source = source;
                passData.material = _material;
                
                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(tempTex, 0, AccessFlags.Write);
                
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    // Blitter handles the full-screen triangle
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // 4. Pass 2: Temp -> Source (Copy Back)
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Underwater CopyBack", out var passData))
            {
                passData.source = tempTex;
                passData.material = null; // No material needed for copy
                
                builder.UseTexture(tempTex, AccessFlags.Read);
                builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }

        private class PassData
        {
            public TextureHandle source;
            public Material material;
        }


        // --- LEGACY API ---
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("Underwater Effect");
            
            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            
            // Alloc Temp
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _tempTexture, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_UnderwaterTempLegacy");

            // Apply
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
