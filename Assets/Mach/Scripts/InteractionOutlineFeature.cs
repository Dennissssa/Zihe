using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;
namespace Mach
{
 public sealed class InteractionOutlineFeature:ScriptableRendererFeature
 {
  public Material maskMaterial,edgeMaterial;OutlinePass pass;
  public override void Create(){pass=new OutlinePass{renderPassEvent=RenderPassEvent.BeforeRenderingPostProcessing};pass.ConfigureInput(ScriptableRenderPassInput.Depth);}
  public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data)
  {
   if(data.cameraData.cameraType!=CameraType.Game||!maskMaterial||!edgeMaterial)return;
   var outline=data.cameraData.camera.GetComponent<InteractionOutline>();if(!outline||!outline.enabled)return;
   var targets=outline.Targets();if(targets.Length==0)return;
   pass.targets=targets;pass.mask=maskMaterial;pass.edge=edgeMaterial;pass.width=outline.width;pass.color=outline.color;renderer.EnqueuePass(pass);
  }
  sealed class OutlinePass:ScriptableRenderPass
  {
   public Renderer[] targets;public Material mask,edge;public float width;public Color color;
   class MaskData{public Renderer[] targets;public Material material;}
   class EdgeData{public TextureHandle texture;public Material material;public Vector4 settings;public Color color;}
   public override void RecordRenderGraph(RenderGraph graph,ContextContainer frame)
   {
    var res=frame.Get<UniversalResourceData>();var camera=frame.Get<UniversalCameraData>();
    var desc=camera.cameraTargetDescriptor;desc.depthBufferBits=0;desc.msaaSamples=1;desc.graphicsFormat=GraphicsFormat.R8_UNorm;
    var texture=UniversalRenderer.CreateRenderGraphTexture(graph,desc,"Interaction silhouette",true);
    using(var builder=graph.AddRasterRenderPass<MaskData>("Interaction silhouette",out var data))
    {
     data.targets=targets;data.material=mask;builder.SetRenderAttachment(texture,0);builder.UseTexture(res.cameraDepthTexture,AccessFlags.Read);
     builder.SetRenderFunc((MaskData d,RasterGraphContext ctx)=>{foreach(var r in d.targets){if(!r||!r.enabled||!r.gameObject.activeInHierarchy||r.forceRenderingOff)continue;var mesh=r is SkinnedMeshRenderer s?s.sharedMesh:r.GetComponent<MeshFilter>()?.sharedMesh;if(!mesh)continue;for(int i=0;i<mesh.subMeshCount;i++)ctx.cmd.DrawRenderer(r,d.material,i,0);}});
    }
    using(var builder=graph.AddRasterRenderPass<EdgeData>("White interaction contour",out var data))
    {
     data.texture=texture;data.material=edge;data.settings=new Vector4(1f/desc.width,1f/desc.height,width,0);data.color=color;builder.UseTexture(texture,AccessFlags.Read);builder.SetRenderAttachment(res.activeColorTexture,0,AccessFlags.ReadWrite);
     builder.SetRenderFunc((EdgeData d,RasterGraphContext ctx)=>{d.material.SetVector("_OutlineSettings",d.settings);d.material.SetColor("_OutlineColor",d.color);Blitter.BlitTexture(ctx.cmd,d.texture,new Vector4(1,1,0,0),d.material,0);});
    }
   }
  }
 }
}
