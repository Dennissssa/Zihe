using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class TutorialRoomRequest
 {
  static TutorialRoomRequest(){EditorApplication.update+=Poll;}
  static void Poll()
  {
   if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/TutorialRooms.request"))return;
   File.Delete("Temp/TutorialRooms.request");
   try{TutorialRoomAuthoring.Apply();File.WriteAllText("Logs/tutorial-rooms-applied.txt",DateTime.Now.ToString("O"));}
   catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/tutorial-rooms-error.txt",e.ToString());}
  }
 }
 public static class TutorialRoomAuthoring
 {
  const string Folder="Assets/Mach/Tutorial/Rooms";
  static TutorialDirector director;static double deadline;static ReflectionProbe[] probes;static int[] captures;
  static Material Material(string name,Color color,float smoothness)
  {
   string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(m)return m;
   m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smoothness);AssetDatabase.CreateAsset(m,path);return m;
  }
  [MenuItem("Mach/Tutorial/Add glass room enclosures")]
  public static void Apply()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before authoring rooms.");
   Directory.CreateDirectory(Folder);Directory.CreateDirectory("Logs");
   string backup="Logs/Tutorial-before-glass-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity.backup";
   File.Copy(TutorialAuthoring.Path,backup);
   var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(TutorialAuthoring.Path);
   if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(TutorialAuthoring.Path);
   director=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TutorialDirector>(true)).First();
   var original=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToDictionary(t=>t,t=>EditorJsonUtility.ToJson(t));
   var audit=new List<string>();
   foreach(var page in director.pages)
   {
    audit.Add(page.title+" | area "+page.layout.areaSize+" | origin "+page.layout.transform.position);
    foreach(var item in page.layout.items.Where(i=>i.visible&&i.target&&i.pose))audit.Add("  "+item.target.name+" @ "+item.pose.position);
    if(page.diagram)foreach(var txt in page.diagram.GetComponentsInChildren<UnityEngine.UI.Text>(true))audit.Add("  diagram: "+txt.text+" @ "+txt.rectTransform.anchoredPosition);
   }
   File.WriteAllLines("Logs/tutorial-existing-layout-before-glass.txt",audit);
   var glass=Material("Glass - pale reflective",new Color(.48f,.63f,.7f,.12f),.94f);
   glass.SetFloat("_Surface",1);glass.SetFloat("_Blend",1);glass.SetFloat("_SrcBlend",1);glass.SetFloat("_DstBlend",10);glass.SetFloat("_SrcBlendAlpha",1);glass.SetFloat("_DstBlendAlpha",10);glass.SetFloat("_ZWrite",0);glass.SetFloat("_Cull",0);
   glass.SetFloat("_WorkflowMode",0);glass.SetColor("_SpecColor",new Color(.52f,.55f,.58f));glass.EnableKeyword("_SPECULAR_SETUP");glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");glass.renderQueue=3000;glass.SetOverrideTag("RenderType","Transparent");glass.SetShaderPassEnabled("ShadowCaster",false);EditorUtility.SetDirty(glass);
   var graphite=Material("Frame - graphite",new Color(.13f,.17f,.19f),.48f);
   var floor=Material("Plinth - charcoal",new Color(.2f,.24f,.26f),.32f);
   var glow=Material("Strip - warm white",new Color(.82f,.91f,1),.4f);glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(.8f,.9f,1)*2.2f);EditorUtility.SetDirty(glow);
   foreach(var page in director.pages)
   {
    var layout=page.layout;if(layout.transform.Find("Glass room enclosure"))continue;
    var room=new GameObject("Glass room enclosure").transform;room.SetParent(layout.transform,false);
    Undo.RegisterCreatedObjectUndo(room.gameObject,"Add tutorial glass enclosure");
    float minX=-layout.areaSize.x/2,maxX=layout.areaSize.x/2,minZ=-layout.areaSize.y/2,maxZ=layout.areaSize.y/2;
    if(layout.fences)
    {
     var rs=layout.fences.GetComponentsInChildren<Renderer>(true);if(rs.Length>0)
     {
      minX=minZ=float.PositiveInfinity;maxX=maxZ=float.NegativeInfinity;
      foreach(var r in rs){var b=r.bounds;for(int n=0;n<8;n++){var p=layout.transform.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((n&1)==0?-1:1,(n&2)==0?-1:1,(n&4)==0?-1:1)));minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}}
     }
    }
    minX-=.3f;maxX+=.3f;minZ-=.3f;maxZ+=.3f;float width=maxX-minX,depth=maxZ-minZ,height=5.2f,cx=(minX+maxX)/2,cz=(minZ+maxZ)/2;
    GameObject Box(string name,Vector3 at,Vector3 size,Material mat,bool collider=true)
    {
     var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(room,false);g.transform.localPosition=at;g.transform.localScale=size;var r=g.GetComponent<Renderer>();r.sharedMaterial=mat;
     if(!collider)Object.DestroyImmediate(g.GetComponent<Collider>());
     if(mat==glass){g.layer=1;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
     return g;
    }
    // Top remains just below the existing ground to avoid overlapping driving surfaces.
    Box("Foundation - below existing floor",new Vector3(cx,-.32f,cz),new Vector3(width+.5f,.6f,depth+.5f),floor);
    foreach(float x in new[]{minX,maxX})
    {
     Box("Glass side wall",new Vector3(x,height/2,cz),new Vector3(.07f,height,depth),glass);
     foreach(float y in new[]{.12f,height})Box("Side frame",new Vector3(x,y,cz),new Vector3(.14f,.13f,depth),graphite);
     Box("Side base light",new Vector3(x,.23f,cz),new Vector3(.15f,.035f,depth-.2f),glow,false);
     int count=Mathf.CeilToInt(depth/5);for(int n=0;n<=count;n++)Box("Glass mullion",new Vector3(x,height/2,Mathf.Lerp(minZ,maxZ,n/(float)count)),new Vector3(.1f,height,.1f),graphite,false);
    }
    foreach(float z in new[]{minZ,maxZ})
    {
     Box("Glass end wall",new Vector3(cx,height/2,z),new Vector3(width,height,.07f),glass);
     foreach(float y in new[]{.12f,height})Box("End frame",new Vector3(cx,y,z),new Vector3(width,.13f,.14f),graphite);
     Box("End base light",new Vector3(cx,.23f,z),new Vector3(width-.2f,.035f,.15f),glow,false);
     int count=Mathf.CeilToInt(width/5);for(int n=0;n<=count;n++)Box("Glass mullion",new Vector3(Mathf.Lerp(minX,maxX,n/(float)count),height/2,z),new Vector3(.1f,height,.1f),graphite,false);
    }
    for(int n=0;n<4;n++)
    {
     float x=n%2==0?minX+.65f:maxX-.65f,z=n<2?minZ+depth*.27f:minZ+depth*.73f;
     Box("Wall light housing",new Vector3(x,4.5f,z),new Vector3(.45f,.15f,1.8f),graphite,false);
     Box("Wall light diffuser",new Vector3(x,4.41f,z),new Vector3(.33f,.035f,1.6f),glow,false);
     var lamp=new GameObject("Soft pool light");lamp.transform.SetParent(room,false);lamp.transform.localPosition=new Vector3(x,4.3f,z);lamp.transform.localRotation=Quaternion.Euler(90,0,0);
     var light=lamp.AddComponent<Light>();light.type=LightType.Spot;light.color=new Color(.88f,.94f,1);light.intensity=3.5f;light.range=13;light.spotAngle=115;light.innerSpotAngle=75;light.shadows=LightShadows.None;
    }
    var probeObject=new GameObject("Room reflection - local objects");probeObject.transform.SetParent(room,false);probeObject.transform.localPosition=new Vector3(cx,2,cz);
    var probe=probeObject.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;probe.resolution=128;probe.boxProjection=true;probe.size=new Vector3(width+2,height+2,depth+2);probe.center=new Vector3(0,.6f,0);probe.blendDistance=.4f;probe.intensity=1;probe.nearClipPlane=.2f;probe.farClipPlane=65;probe.cullingMask=~(1<<1);probe.importance=3;
    var updater=probeObject.AddComponent<TutorialRoomReflection>();updater.layout=layout;updater.probe=probe;
   }
   foreach(var pair in original)if(pair.Key&&EditorJsonUtility.ToJson(pair.Key)!=pair.Value&&!(pair.Key.GetComponent<TutorialStageLayout>()))throw new Exception("Existing transform changed: "+pair.Key.name);
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   Debug.Log("GLASS_ROOMS_SAVED: eight enclosures; existing diagrams and placements preserved. Backup: "+backup);
  }
  public static void ApplyAndPreview()
  {
   Apply();probes=director.GetComponentsInParent<Transform>().Length>0?director.pages.Select(p=>p.layout.GetComponentInChildren<ReflectionProbe>()).ToArray():Array.Empty<ReflectionProbe>();
   captures=probes.Select(p=>p.RenderProbe()).ToArray();deadline=EditorApplication.timeSinceStartup+45;EditorApplication.update+=FinishPreview;
  }
  static void FinishPreview()
  {
   if(probes.Where((p,i)=>!p.IsFinishedRendering(captures[i])).Any()&&EditorApplication.timeSinceStartup<deadline)return;
   EditorApplication.update-=FinishPreview;
   try
   {
    int i=Mathf.Clamp(director.editorPreviewPage,0,director.pages.Length-1);var layout=director.pages[i].layout;
    var cameraObject=new GameObject("Preview camera");var camera=cameraObject.AddComponent<Camera>();camera.transform.position=layout.transform.TransformPoint(new Vector3(18,12,-27));camera.transform.LookAt(layout.transform.position+Vector3.up*1.7f);camera.fieldOfView=55;camera.farClipPlane=180;
    var canvas=director.guidePanel.GetComponentInParent<Canvas>();bool wasEnabled=canvas.enabled;canvas.enabled=false;
    var rt=new RenderTexture(1440,1000,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1440,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,1000),0,0);image.Apply();File.WriteAllBytes("Logs/tutorial-glass-preview.png",image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;canvas.enabled=wasEnabled;Object.DestroyImmediate(cameraObject);Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
    Debug.Log("GLASS_ROOMS_PREVIEW_COMPLETE");EditorApplication.Exit(0);
   }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
  }
 }
}
