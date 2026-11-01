using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public sealed class FirstPersonHeldItemFeature : ScriptableRendererFeature
{
    HeldItemPass pass;
    public override void Create()=>pass=new HeldItemPass{renderPassEvent=RenderPassEvent.AfterRenderingTransparents};
    public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData renderingData)
    {
        if(FirstPersonHeldItemRenderer.Renders(renderingData.cameraData.camera))renderer.EnqueuePass(pass);
    }
    sealed class HeldItemPass : ScriptableRenderPass
    {
        readonly List<ShaderTagId> tags=new(){new("UniversalForwardOnly"),new("UniversalForward"),new("SRPDefaultUnlit")};
        sealed class PassData{public RendererListHandle opaque,transparent;}
        public override void RecordRenderGraph(RenderGraph graph,ContextContainer frameData)
        {
            var camera=frameData.Get<UniversalCameraData>();
            var rendering=frameData.Get<UniversalRenderingData>();
            var lighting=frameData.Get<UniversalLightData>();
            var resources=frameData.Get<UniversalResourceData>();
            // Separate depth preserves occlusion inside the item without inheriting walls' depth.
            // The scene's depth texture remains intact for subsequent post-processing.
            var descriptor=resources.activeDepthTexture.GetDescriptor(graph);
            descriptor.name="First person item depth";descriptor.clearBuffer=true;
            var depth=graph.CreateTexture(descriptor);
            var opaqueDrawing=RenderingUtils.CreateDrawingSettings(tags,rendering,camera,lighting,camera.defaultOpaqueSortFlags);
            var transparentDrawing=RenderingUtils.CreateDrawingSettings(tags,rendering,camera,lighting,SortingCriteria.CommonTransparent);
            var opaqueFilter=new FilteringSettings(RenderQueueRange.opaque,1<<FirstPersonHeldItemRenderer.ItemLayer);
            var transparentFilter=new FilteringSettings(RenderQueueRange.transparent,1<<FirstPersonHeldItemRenderer.ItemLayer);
            using(var builder=graph.AddRasterRenderPass<PassData>("First person held item",out var data))
            {
                data.opaque=graph.CreateRendererList(new RendererListParams(rendering.cullResults,opaqueDrawing,opaqueFilter));
                data.transparent=graph.CreateRendererList(new RendererListParams(rendering.cullResults,transparentDrawing,transparentFilter));
                builder.UseRendererList(data.opaque);builder.UseRendererList(data.transparent);
                builder.SetRenderAttachment(resources.activeColorTexture,0,AccessFlags.ReadWrite);
                builder.SetRenderAttachmentDepth(depth,AccessFlags.ReadWrite);
                if(resources.mainShadowsTexture.IsValid())builder.UseTexture(resources.mainShadowsTexture,AccessFlags.Read);
                if(resources.additionalShadowsTexture.IsValid())builder.UseTexture(resources.additionalShadowsTexture,AccessFlags.Read);
                if(resources.ssaoTexture.IsValid())builder.UseTexture(resources.ssaoTexture,AccessFlags.Read);
                if(resources.cameraOpaqueTexture.IsValid())builder.UseTexture(resources.cameraOpaqueTexture,AccessFlags.Read);
                foreach(var texture in resources.dBuffer)if(texture.IsValid())builder.UseTexture(texture,AccessFlags.Read);
                builder.SetRenderFunc(static (PassData data,RasterGraphContext context)=>
                {context.cmd.DrawRendererList(data.opaque);context.cmd.DrawRendererList(data.transparent);});
            }
        }
    }
}
