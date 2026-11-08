using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;
namespace Mach {
 public sealed class RetroImageFeature:ScriptableRendererFeature {
  public RetroImageSettings settings;
  public Material filterMaterial,maskMaterial;
  RetroPass pass;
  public override void Create(){pass=new RetroPass{renderPassEvent=(RenderPassEvent)((int)RenderPassEvent.BeforeRenderingPostProcessing-1),requiresIntermediateTexture=true};pass.ConfigureInput(ScriptableRenderPassInput.Color|ScriptableRenderPassInput.Depth);}
  public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data){
   if(!settings||(!settings.enableFilter||settings.intensity<=0)&&data.cameraData.camera!=BeaconDroneStation.FeedCamera&&!MountainAtmosphere.Active||!filterMaterial||!maskMaterial||(data.cameraData.cameraType!=CameraType.Game&&!(data.cameraData.cameraType==CameraType.SceneView&&MountainAtmosphere.Active))||data.cameraData.renderType==CameraRenderType.Overlay)return;
   pass.settings=settings;pass.filter=filterMaterial;pass.mask=maskMaterial;renderer.EnqueuePass(pass);
  }
  sealed class RetroPass:ScriptableRenderPass {
   public RetroImageSettings settings;public Material filter,mask;
   class MaskData{public Renderer[] targets;public Material material;}
   class TacticalData{public DroneMissionOverlay overlay;public Renderer[][] groups;}
   class FilterData{public bool tactical;public TextureHandle source;public Material material;public Vector4 grid,effects,bloom,fog;public Color distantFog,valleyFog;}
   class CopyData{public TextureHandle source;}
   static readonly int SharpId=Shader.PropertyToID("_MachSharpMask");
   public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame){
    var res=frame.Get<UniversalResourceData>();if(res.isActiveTargetBackBuffer)return;
    var camera=frame.Get<UniversalCameraData>();var desc=camera.cameraTargetDescriptor;desc.depthBufferBits=0;desc.msaaSamples=1;
    var destination=UniversalRenderer.CreateRenderGraphTexture(graph,desc,"Retro scene color",false);
    desc.graphicsFormat=GraphicsFormat.R8_UNorm;
    var sharp=UniversalRenderer.CreateRenderGraphTexture(graph,desc,"Readable display mask",true);
    var targets=new List<Renderer>();foreach(var marker in RetroSharpSurface.Active)if(marker&&marker.surfaces!=null)foreach(var r in marker.surfaces)if(r&&r.enabled&&r.gameObject.activeInHierarchy&&!r.forceRenderingOff&&(camera.camera.cullingMask&(1<<r.gameObject.layer))!=0)targets.Add(r);
    using(var builder=graph.AddRasterRenderPass<MaskData>("Protect machine displays",out var d)){
     d.targets=targets.ToArray();d.material=mask;builder.SetRenderAttachment(sharp,0);builder.UseTexture(res.cameraDepthTexture,AccessFlags.Read);builder.SetGlobalTextureAfterPass(sharp,SharpId);
     builder.SetRenderFunc((MaskData data,RasterGraphContext ctx)=>{
      foreach(var r in data.targets){
       if(!r||!r.enabled||!r.gameObject.activeInHierarchy||r.forceRenderingOff)continue;
       int subMeshes=0;
       if(r is SkinnedMeshRenderer sk){
        var mesh=sk.sharedMesh;if(mesh)subMeshes=mesh.subMeshCount;
       }else if(r.TryGetComponent<MeshFilter>(out var meshFilter)&&meshFilter){
        var mesh=meshFilter.sharedMesh;if(mesh)subMeshes=mesh.subMeshCount;
       }else if(r is MeshRenderer&&r.TryGetComponent<TextMesh>(out var text)&&text){
        // TextMesh generates its own geometry; it does not have a MeshFilter.
        subMeshes=1;
       }
       // Unity missing-component objects must be checked with Unity's bool
       // operator, not ?. (which only checks managed null).
       for(int i=0;i<subMeshes;i++)ctx.cmd.DrawRenderer(r,data.material,i,0);
      }
     });
    }
    var overlay=DroneMissionOverlay.Current;
    bool tactical=camera.camera==BeaconDroneStation.FeedCamera && overlay && overlay.silhouetteMaterial;
    TextureHandle silhouettes=default;
    if(tactical){
     desc.graphicsFormat=GraphicsFormat.R8G8B8A8_UNorm;
     silhouettes=UniversalRenderer.CreateRenderGraphTexture(graph,desc,"Drone tactical silhouettes",true);
     using(var builder=graph.AddRasterRenderPass<TacticalData>("Drone objectives through walls",out var data)){
      data.overlay=overlay;
      data.groups=new[]{overlay.VisibleTargets(true),overlay.VisibleTargets(false),overlay.VisibleRoutes()};
      builder.SetRenderAttachment(silhouettes,0);
      builder.SetGlobalTextureAfterPass(silhouettes,Shader.PropertyToID("_MissionMask"));
      builder.SetRenderFunc((TacticalData draw,RasterGraphContext ctx)=>{
       for(int pass=0;pass<draw.groups.Length;pass++)foreach(var renderer in draw.groups[pass]){
        int count=renderer is LineRenderer?1:renderer.TryGetComponent<MeshFilter>(out var mf)&&mf.sharedMesh?mf.sharedMesh.subMeshCount:0;
        for(int sub=0;sub<count;sub++)ctx.cmd.DrawRenderer(renderer,draw.overlay.silhouetteMaterial,sub,pass);
       }
      });
     }
    }
    using(var builder=graph.AddRasterRenderPass<FilterData>("Retro image and soft glow",out var d)){
     d.source=res.activeColorTexture;d.material=filter;d.tactical=tactical;
     if(tactical)builder.UseTexture(silhouettes,AccessFlags.Read);
     float height=Mathf.Clamp(settings.verticalResolution,180,desc.height);
     d.grid=new Vector4(height*desc.width/desc.height,height,Mathf.Max(8,settings.colorSteps),settings.enableFilter&&camera.camera.cameraType==CameraType.Game?settings.intensity:0);
     d.effects=new Vector4(settings.dithering,settings.scanlines,1f/desc.width,1f/desc.height);
     d.bloom=new Vector4(settings.bloom,settings.bloomThreshold,settings.bloomRadius,camera.camera==BeaconDroneStation.FeedCamera?1:0);
     var atmosphere=MountainAtmosphere.Active;
     d.fog=atmosphere?new Vector4(atmosphere.distanceDensity,atmosphere.valleyDensity,atmosphere.valleyHeight,atmosphere.heightFalloff):Vector4.zero;
     d.distantFog=atmosphere?atmosphere.distantColor.linear:Color.black;
     d.valleyFog=atmosphere?atmosphere.valleyColor.linear:Color.black;
     builder.UseTexture(res.cameraDepthTexture,AccessFlags.Read);
     builder.UseTexture(d.source,AccessFlags.Read);builder.UseTexture(sharp,AccessFlags.Read);builder.UseAllGlobalTextures(true);builder.SetRenderAttachment(destination,0);
     builder.SetRenderFunc((FilterData data,RasterGraphContext ctx)=>{data.material.SetFloat("_HasMissionMask",data.tactical?1:0);data.material.SetVector("_RetroGrid",data.grid);data.material.SetVector("_RetroEffects",data.effects);data.material.SetVector("_RetroBloom",data.bloom);data.material.SetVector("_MountainFog",data.fog);data.material.SetColor("_DistantFogColor",data.distantFog);data.material.SetColor("_ValleyFogColor",data.valleyFog);Blitter.BlitTexture(ctx.cmd,data.source,new Vector4(1,1,0,0),data.material,0);});
    }
    if(!camera.resolveFinalTarget){
     // Overlay cameras import the stack's persistent color attachment again.
     // Keep that attachment and write the filtered scene back into it, so the
     // tablet camera receives the effect without filtering the tablet itself.
     using(var builder=graph.AddRasterRenderPass<CopyData>("Preserve retro scene for overlay cameras",out var d)){
      d.source=destination;builder.UseTexture(destination,AccessFlags.Read);
      builder.SetRenderAttachment(res.activeColorTexture,0,AccessFlags.Write);
      builder.AllowPassCulling(false);
      builder.SetRenderFunc((CopyData data,RasterGraphContext ctx)=>Blitter.BlitTexture(ctx.cmd,data.source,new Vector4(1,1,0,0),0,false));
     }
    }else res.cameraColor=destination;
   }
  }
 }
}
