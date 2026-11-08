using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class InteractionOutlineAuthoring
 {
  static InteractionOutlineAuthoring(){EditorApplication.update+=Poll;}
  static void Poll(){if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/InteractionOutline.request"))return;File.Delete("Temp/InteractionOutline.request");try{Apply();File.WriteAllText("Logs/interaction-outline-applied.txt","Outline saved to both scenes and player prefab.");}catch(System.Exception e){Debug.LogException(e);File.WriteAllText("Logs/interaction-outline-error.txt",e.ToString());}}
  static Material Material(string name,string shader){var path="Assets/Mach/Lighting/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}return m;}
  [MenuItem("Mach/Interaction/Install white outlines")]
  public static void Apply()
  {
   var mask=Material("Interaction mask","Mach/InteractionMask");var edge=Material("Interaction edge","Mach/InteractionEdge");
   foreach(var path in new[]{"Assets/Settings/PC_Renderer.asset","Assets/Settings/Mobile_Renderer.asset"})
   {
    var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);if(!renderer)continue;
    var feature=renderer.rendererFeatures.OfType<InteractionOutlineFeature>().FirstOrDefault();if(!feature){feature=ScriptableObject.CreateInstance<InteractionOutlineFeature>();feature.name="White interaction outlines";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);}
    feature.maskMaterial=mask;feature.edgeMaterial=edge;feature.Create();EditorUtility.SetDirty(feature);renderer.SetDirty();EditorUtility.SetDirty(renderer);
   }
   var active=EditorSceneManager.GetActiveScene();
   foreach(var path in new[]{"Assets/Mach/Scenes/WalkingPickup.unity","Assets/Mach/Scenes/Tutorial.unity"})
   {
    var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
    foreach(var root in scene.GetRootGameObjects())Wire(root);
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
   }
   if(active.IsValid()&&active.isLoaded)EditorSceneManager.SetActiveScene(active);
   const string prefab="Assets/Mach/Prefabs/FirstPersonPlayer.prefab";
   var contents=PrefabUtility.LoadPrefabContents(prefab);try{Wire(contents);PrefabUtility.SaveAsPrefabAsset(contents,prefab);}finally{PrefabUtility.UnloadPrefabContents(contents);}
   AssetDatabase.SaveAssets();
  }
  static void Wire(GameObject root)
  {
   foreach(var pickup in root.GetComponentsInChildren<PlayerPickup>(true)){if(!pickup.viewCamera)continue;var outline=pickup.viewCamera.GetComponent<InteractionOutline>();if(!outline)outline=pickup.viewCamera.gameObject.AddComponent<InteractionOutline>();outline.pickup=pickup;EditorUtility.SetDirty(outline);}
   foreach(var entry in root.GetComponentsInChildren<TruckInteractable>(true))
   {
    if(!entry.driverEntry||!entry.truck||entry.name!="Driver seat")continue;
    var group=entry.GetComponent<InteractionOutlineGroup>();if(!group)group=entry.gameObject.AddComponent<InteractionOutlineGroup>();
    float x=entry.truck.transform.InverseTransformPoint(entry.transform.position).x;
    var parts=entry.truck.GetComponentsInChildren<Transform>(true).Where(t=>(t.name=="Seat back"||t.name=="Headrest")&&Mathf.Abs(entry.truck.transform.InverseTransformPoint(t.position).x-x)<.25f);
    group.parts=new[]{entry.transform}.Concat(parts).ToArray();EditorUtility.SetDirty(group);
    foreach(var part in group.parts.Skip(1)){if(!part.GetComponent<Collider>())continue;var backEntry=part.GetComponent<TruckInteractable>();if(!backEntry)backEntry=part.gameObject.AddComponent<TruckInteractable>();backEntry.truck=entry.truck;backEntry.panel=entry.panel;backEntry.driverEntry=true;backEntry.panelName="Driver seat";var backGroup=part.GetComponent<InteractionOutlineGroup>();if(!backGroup)backGroup=part.gameObject.AddComponent<InteractionOutlineGroup>();backGroup.parts=group.parts;EditorUtility.SetDirty(backEntry);EditorUtility.SetDirty(backGroup);}
   }
  }
  public static void Verify()
  {
   Apply();
   foreach(var path in new[]{"Assets/Mach/Scenes/WalkingPickup.unity","Assets/Mach/Scenes/Tutorial.unity"})
   {
    EditorSceneManager.OpenScene(path);var pickup=Object.FindAnyObjectByType<PlayerPickup>();var outline=pickup.viewCamera.GetComponent<InteractionOutline>();if(!outline||outline.pickup!=pickup)throw new System.Exception("Missing camera binding");
    var seat=Object.FindObjectsByType<TruckInteractable>().First(t=>t.driverEntry&&t.name=="Driver seat");if(seat.GetComponent<InteractionOutlineGroup>().parts.Length<2)throw new System.Exception("Seat back missing");
    typeof(PlayerPickup).GetProperty("OutlineTarget").SetValue(pickup,seat.transform);if(outline.Targets().Length<2)throw new System.Exception("Seat group selection failed");
    typeof(PlayerPickup).GetProperty("OutlineTarget").SetValue(pickup,null);if(outline.Targets().Length!=0)throw new System.Exception("Outline did not clear");
    if(path.Contains("Tutorial"))
    {
     Capture(pickup,seat.transform,"seat");
     var ammo=Object.FindAnyObjectByType<AmmoSelectionConsole>();Capture(pickup,ammo.transform,"machine");
     var button=ammo.GetComponentInChildren<AmmoSelectionButton>();Capture(pickup,button.movingCap?button.movingCap:button.transform,"button");
    }
   }
   foreach(var shaderName in new[]{"Mach/InteractionMask","Mach/InteractionEdge"}){var shader=Shader.Find(shaderName);if(!shader||ShaderUtil.ShaderHasError(shader))throw new System.Exception("Outline shader error: "+shaderName);}
   Debug.Log("INTERACTION_OUTLINE_VERIFIED: both scenes, seat groups, target clearing, shaders");
  }
  static void Capture(PlayerPickup pickup,Transform target,string label)
  {
   foreach(var canvas in Object.FindObjectsByType<Canvas>())canvas.gameObject.SetActive(false);
   if(pickup.heldTablet)pickup.heldTablet.poseRoot.gameObject.SetActive(false);
   var camera=pickup.viewCamera;var rt=new RenderTexture(960,640,24);camera.targetTexture=rt;
   typeof(PlayerPickup).GetProperty("OutlineTarget").SetValue(pickup,target);
   var parts=camera.GetComponent<InteractionOutline>().Targets();var bounds=parts[0].bounds;foreach(var r in parts)bounds.Encapsulate(r.bounds);
   camera.transform.position=bounds.center+target.root.TransformDirection(new Vector3(-1,1,1)).normalized*Mathf.Max(.35f,bounds.size.magnitude*1.6f);camera.transform.LookAt(bounds.center);
   var pixels=new Texture2D(960,640,TextureFormat.RGB24,false);camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,960,640),0,0);pixels.Apply();File.WriteAllBytes("Logs/outline-"+label+".png",pixels.EncodeToPNG());
   var outlined=pixels.GetPixels32();typeof(PlayerPickup).GetProperty("OutlineTarget").SetValue(pickup,null);camera.Render();pixels.ReadPixels(new Rect(0,0,960,640),0,0);pixels.Apply();var plain=pixels.GetPixels32();int changed=0;for(int i=0;i<plain.Length;i++)if(outlined[i].r>plain[i].r+15&&outlined[i].g>plain[i].g+15)changed++;
   RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(pixels);Object.DestroyImmediate(rt);if(changed<20)throw new System.Exception("Outline produced no visible pixels: "+changed);Debug.Log("OUTLINE_RENDER_VERIFIED pixels="+changed);
  }
 }
}

