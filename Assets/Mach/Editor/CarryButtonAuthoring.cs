using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class CarryButtonLive
 {
  static CarryButtonLive(){EditorApplication.update+=Poll;}
  static void Poll(){if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/CarryButtons.request"))return;File.Delete("Temp/CarryButtons.request");try{AmmoMachineAuthoring.RefineCarryButtons();File.WriteAllText("Logs/carry-buttons-applied.txt","Fire and robot rear carry buttons saved.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/carry-buttons-error.txt",e.ToString());}}
 }
 public static partial class AmmoMachineAuthoring
 {
  const string CarryFolder="Assets/Mach/CarryButtons";
  static Transform CarryKnob(Transform parent,Vector3 pos,MovableMachine mover,Material print)
  {
   var t=new GameObject("Round carry button").transform;t.SetParent(parent,false);t.localPosition=pos;
   Panel("Mount",t,Vector3.zero,.27f,.24f,.032f,.026f,metal);
   Disc("Socket",t,new Vector3(0,0,-.026f),.098f,.022f,20,rubber);
   Disc("Circular cap",t,new Vector3(0,0,-.047f),.079f,.03f,24,metal);
   Text("MOVE label",t,"MOVE",new Vector3(0,0,-.065f),.031f,print);
   var hit=t.gameObject.AddComponent<SphereCollider>();hit.center=new Vector3(0,0,-.046f);hit.radius=.088f;t.gameObject.AddComponent<MachineMoveHandle>().machine=mover;return t;
  }
  public static void CarryBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);RefineCarryButtons();VerifyCarryButtons();}
  public static void RefineCarryButtons()
  {
   var robot=UnityEngine.Object.FindAnyObjectByType<RobotAssemblyRig>();if(!robot||robot.gameObject.scene.path!=WalkingSceneAuthoring.ScenePath)throw new Exception("Open current robot scene first");
   NavigationConsole fire=null;foreach(var c in UnityEngine.Object.FindObjectsByType<NavigationConsole>())if(c.mode==CircuitKind.Fire)fire=c;
   if(!fire)throw new Exception("Fire console missing");if(fire.transform.Find("Round carry button"))return;
   Directory.CreateDirectory(CarryFolder+"/Meshes");AssetDatabase.Refresh();var oldFolder=meshFolder;var oldPrefix=meshPrefix;int oldIndex=meshIndex;
   meshFolder=CarryFolder+"/Meshes";meshPrefix="Carry_";meshIndex=0;
   metal=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Graphite steel.mat");rubber=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Rubber seals.mat");var print=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Button ivory print.mat");
   var orange=AssetDatabase.LoadAssetAtPath<Material>(CarryFolder+"/Fire amber.mat");if(!orange){orange=new Material(Shader.Find("Universal Render Pipeline/Lit"));orange.color=new Color(.86f,.27f,.055f);orange.SetFloat("_Smoothness",.28f);AssetDatabase.CreateAsset(orange,CarryFolder+"/Fire amber.mat");}
   try
   {
    var old=fire.GetComponentInChildren<CircuitWorldButton>();var parent=old.transform.parent;UnityEngine.Object.DestroyImmediate(old.gameObject);
    var key=new GameObject("FIRE armored paddle").transform;key.SetParent(parent,false);key.localPosition=new Vector3(0,.30f,-.035f);
    Panel("Octagonal socket",key,Vector3.zero,.53f,.35f,.072f,.028f,rubber);
    Ring("Heavy guard",key,new Vector3(0,0,-.02f),.51f,.33f,.43f,.25f,.052f,.039f,metal);
    var cap=new GameObject("Moving fire paddle").transform;cap.SetParent(key,false);cap.localPosition=new Vector3(0,0,-.070f);
    Panel("Amber chamfered paddle",cap,Vector3.zero,.414f,.235f,.049f,.04f,orange);
    Text("FIRE",cap,"FIRE",new Vector3(0,0,-.023f),.079f,print);
    foreach(float side in new[]{-1f,1f})for(int i=0;i<3;i++)Box("Grip rib",cap,new Vector3(side*.163f,(i-1)*.038f,-.024f),new Vector3(.023f,.009f,.005f),metal);
    var hit=key.gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(0,0,-.06f);hit.size=new Vector3(.5f,.31f,.09f);
    var button=key.gameObject.AddComponent<CircuitWorldButton>();button.navigation=fire;button.navigationAction=3;button.cap=cap;button.pressDirection=Vector3.forward;
    var rootHandle=fire.GetComponent<MachineMoveHandle>();if(rootHandle)UnityEngine.Object.DestroyImmediate(rootHandle);
    CarryKnob(fire.transform,new Vector3(0,.225f,-.30f),fire.GetComponent<MovableMachine>(),print);
    var assetCopy=UnityEngine.Object.Instantiate(fire.gameObject);assetCopy.name="FireConsole";assetCopy.GetComponent<NavigationConsole>().program=null;assetCopy.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);assetCopy.transform.localScale=Vector3.one;PrefabUtility.SaveAsPrefabAsset(assetCopy,SplitFolder+"/FireConsole.prefab");UnityEngine.Object.DestroyImmediate(assetCopy);
    var mover=robot.GetComponent<MovableMachine>();if(!mover)mover=Undo.AddComponent<MovableMachine>(robot.gameObject);
    mover.displayName="Tracked robot";mover.validGhost=fire.GetComponent<MovableMachine>().validGhost;mover.invalidGhost=fire.GetComponent<MovableMachine>().invalidGhost;
    // Mount below the tablet on the fixed rear chassis, outside the service grille.
    var knob=CarryKnob(robot.transform,new Vector3(.47f,.61f,-1.105f),mover,print);Undo.RegisterCreatedObjectUndo(knob.gameObject,"Add rear robot carry button");
    var bounds=new Bounds();bool first=true;
    foreach(var filter in robot.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh)foreach(var v in filter.sharedMesh.vertices){var local=robot.transform.InverseTransformPoint(filter.transform.TransformPoint(v));if(first){bounds=new Bounds(local,Vector3.zero);first=false;}else bounds.Encapsulate(local);}
    // Footprint follows the tracks; barrel overhang should not require floor support.
    mover.boundsCenter=new Vector3(0,(bounds.min.y+bounds.max.y)*.5f,0);mover.boundsSize=new Vector3(bounds.size.x,bounds.size.y,2.25f);
    EditorUtility.SetDirty(mover);PrefabUtility.RecordPrefabInstancePropertyModifications(mover);
    AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(robot.gameObject.scene);EditorSceneManager.SaveScene(robot.gameObject.scene);
   }
   finally{meshFolder=oldFolder;meshPrefix=oldPrefix;meshIndex=oldIndex;}
  }
  public static void VerifyCarryButtons()
  {
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var robot=UnityEngine.Object.FindAnyObjectByType<RobotAssemblyRig>();NavigationConsole fire=null;foreach(var c in UnityEngine.Object.FindObjectsByType<NavigationConsole>())if(c.mode==CircuitKind.Fire)fire=c;
   Physics.SyncTransforms();foreach(var t in new[]{robot.transform,fire.transform}){var knob=t.Find("Round carry button");if(!knob)throw new Exception("Missing carry button");var origin=knob.TransformPoint(new Vector3(0,0,-.6f));if(!Physics.Raycast(origin,knob.forward,out var hit,1)||!hit.collider.GetComponent<MachineMoveHandle>())throw new Exception("Obstructed carry button "+t.name);}
   var mover=robot.GetComponent<MovableMachine>();var body=robot.GetComponent<Rigidbody>();bool initial=body.isKinematic;var pos=robot.transform.position;mover.BeginPreview();if(!body.isKinematic||!mover.IsPreview)throw new Exception("Carry physics not suspended");robot.transform.position+=Vector3.up;mover.EndPreview(false);if(body.isKinematic!=initial||Vector3.Distance(pos,robot.transform.position)>.001f)throw new Exception("Cancel did not restore robot");
   mover.BeginPreview();mover.EndPreview(true);if(body.isKinematic!=initial)throw new Exception("Ground physics not restored");
   RenderPreview(fire.gameObject,"Logs/fire-carry-preview.png");EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Debug.Log("CARRY_BUTTONS_VERIFIED");
  }
 }
}

