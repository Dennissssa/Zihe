using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class FireErgonomicsLive
 {
  static FireErgonomicsLive(){EditorApplication.update+=Poll;}
  static void Poll(){if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/FireErgonomics.request"))return;File.Delete("Temp/FireErgonomics.request");try{AmmoMachineAuthoring.RefineFireErgonomics();File.WriteAllText("Logs/fire-ergonomics-applied.txt","Fire slope and shared red buttons saved.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/fire-ergonomics-error.txt",e.ToString());}}
 }
 public static partial class AmmoMachineAuthoring
 {
  const string ErgFolder="Assets/Mach/FireErgonomics";
  public static void FireErgonomicsBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);RefineFireErgonomics();VerifyFireErgonomics();}
  public static void RefineFireErgonomics()
  {
   NavigationConsole fire=null;foreach(var c in UnityEngine.Object.FindObjectsByType<NavigationConsole>())if(c.mode==CircuitKind.Fire)fire=c;
   if(!fire||fire.gameObject.scene.path!=WalkingSceneAuthoring.ScenePath)throw new Exception("Open current scene");
   Directory.CreateDirectory(ErgFolder+"/Meshes");AssetDatabase.Refresh();
   var red=AssetDatabase.LoadAssetAtPath<Material>(ErgFolder+"/Fire red.mat");if(!red){red=new Material(Shader.Find("Universal Render Pipeline/Lit"));red.color=new Color(.72f,.065f,.035f);red.SetFloat("_Smoothness",.28f);AssetDatabase.CreateAsset(red,ErgFolder+"/Fire red.mat");}
   var saveRoot=root;var saveFolder=meshFolder;var savePrefix=meshPrefix;int saveIndex=meshIndex;root=fire.transform;meshFolder=ErgFolder+"/Meshes";meshPrefix="Fire_";meshIndex=0;while(File.Exists(meshFolder+"/Fire_"+meshIndex.ToString("D3")+".asset"))meshIndex++;
   ivory=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Warm ivory enamel.mat");metal=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Graphite steel.mat");rubber=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Rubber seals.mat");var print=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Button ivory print.mat");
   try
   {
    var panel=root.Find("Sloped single control");Undo.RecordObject(panel,"Tilt Fire panel upward");panel.localRotation=Quaternion.Euler(40,0,0);
    foreach(var b in fire.GetComponentsInChildren<CircuitWorldButton>())Undo.DestroyObjectImmediate(b.gameObject);
    DedicatedKey(panel,fire,"FIRE",new Vector2(0,.35f),new Vector2(.43f,.36f),3,red,print);
    Undo.DestroyObjectImmediate(root.Find("Compact ivory cabinet").gameObject);
    Hull("Compact ivory cabinet",new[]{new Vector2(.10f,-.28f),new Vector2(.34f,-.28f),new Vector2(.92f,.207f),new Vector2(.92f,.30f),new Vector2(.10f,.30f)},.32f,.024f,ivory);
    var body=root.GetComponent<MeshCollider>();body.sharedMesh=root.Find("Compact ivory cabinet").GetComponent<MeshFilter>().sharedMesh;
    var rail=root.Find("Top protective rail");rail.localPosition=new Vector3(0,.928f,.252f);rail.localScale=new Vector3(.53f,.025f,.085f);
    Undo.DestroyObjectImmediate(root.Find("Graphite foot").gameObject);
    Hull("Graphite foot",new[]{new Vector2(.025f,-.32f),new Vector2(.11f,-.28f),new Vector2(.11f,.30f),new Vector2(.025f,.35f)},.36f,.02f,metal);
    var mover=fire.GetComponent<MovableMachine>();mover.boundsCenter=new Vector3(0,.48f,-.005f);mover.boundsSize=new Vector3(.72f,.94f,.71f);
    var copy=UnityEngine.Object.Instantiate(fire.gameObject);copy.name="FireConsole";copy.GetComponent<NavigationConsole>().program=null;copy.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);copy.transform.localScale=Vector3.one;PrefabUtility.SaveAsPrefabAsset(copy,SplitFolder+"/FireConsole.prefab");UnityEngine.Object.DestroyImmediate(copy);
    foreach(var rate in UnityEngine.Object.FindObjectsByType<FireRateConsole>())SetRateRed(rate,red);
    var asset=PrefabUtility.LoadPrefabContents(RatePrefab);try{SetRateRed(asset.GetComponent<FireRateConsole>(),red);PrefabUtility.SaveAsPrefabAsset(asset,RatePrefab);}finally{PrefabUtility.UnloadPrefabContents(asset);}
    AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(fire.gameObject.scene);EditorSceneManager.SaveScene(fire.gameObject.scene);
   }
   finally{root=saveRoot;meshFolder=saveFolder;meshPrefix=savePrefix;meshIndex=saveIndex;}
  }
  static void SetRateRed(FireRateConsole rate,Material red)
  {
   foreach(var b in rate.GetComponentsInChildren<FireRateButton>(true))if(b.isConfirm)foreach(var r in b.keycap.GetComponentsInChildren<MeshRenderer>(true))if(r.name=="Raised keycap"){Undo.RecordObject(r,"Fire confirm red");r.sharedMaterial=red;EditorUtility.SetDirty(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
  }
  public static void VerifyFireErgonomics()
  {
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);NavigationConsole fire=null;foreach(var c in UnityEngine.Object.FindObjectsByType<NavigationConsole>())if(c.mode==CircuitKind.Fire)fire=c;
   var key=fire.GetComponentInChildren<CircuitWorldButton>();Physics.SyncTransforms();
   // Standing eye at 1.65m, looking down at the low console from typical reach.
   var eye=fire.transform.TransformPoint(new Vector3(0,1.65f,-1.05f));var aim=key.transform.TransformPoint(new Vector3(0,0,-.075f));
   if(!Physics.Raycast(eye,(aim-eye).normalized,out var hit,3)||hit.collider.GetComponent<CircuitWorldButton>()!=key)throw new Exception("Standing interaction blocked");
   var material=key.cap.Find("Keycap").GetComponent<Renderer>().sharedMaterial;int count=0;
   foreach(var b in UnityEngine.Object.FindObjectsByType<FireRateButton>())if(b.isConfirm){if(b.keycap.Find("Raised keycap").GetComponent<Renderer>().sharedMaterial!=material)throw new Exception("Confirm color mismatch");count++;}
   if(count==0)throw new Exception("Rate confirm missing");if(!fire.transform.Find("Round carry button"))throw new Exception("Carry button missing");
   RenderPreview(fire.gameObject,"Logs/fire-ergonomics-preview.png");EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Debug.Log("FIRE_ERGONOMICS_VERIFIED");
  }
 }
}
