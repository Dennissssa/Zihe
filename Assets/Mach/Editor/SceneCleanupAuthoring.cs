using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class SceneCleanupAuthoring
 {
  static SceneCleanupAuthoring(){EditorApplication.update+=Poll;}
  static void Poll()
  {
   if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/SceneCleanup.request"))return;
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=WalkingSceneAuthoring.ScenePath)return;
   File.Delete("Temp/SceneCleanup.request");
   try{Apply();File.WriteAllText("Logs/scene-cleanup-applied.txt","Temporary station and pickup props removed; eight launcher markers stored; carry cues updated.");}
   catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/scene-cleanup-error.txt",e.ToString());}
  }
  public static void Apply()
  {
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   var program=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();
   if(!program||program.pointMarkers==null||program.pointMarkers.Length!=8||program.pointMarkers.Any(p=>!p))throw new Exception("Expected all eight authored launcher markers");
   var removed=new System.Collections.Generic.List<string>();
   foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
   {
    if(!t)continue;
    if(t.name=="Temporary behavior buttons"||t.name=="Workbench")
    {removed.Add(t.name);Undo.DestroyObjectImmediate(t.gameObject);}
   }
   foreach(var item in UnityEngine.Object.FindObjectsByType<PickupItem>(FindObjectsInactive.Include))
   {
    if(item.gameObject.scene!=scene)continue;
    removed.Add(item.name);Undo.DestroyObjectImmediate(item.gameObject);
   }
   foreach(var marker in program.pointMarkers)
   {
    Undo.RecordObject(marker,"Store beacon in launcher");Undo.RecordObject(marker.gameObject,"Hide stored beacon");
    marker.stored=true;marker.gameObject.SetActive(false);EditorUtility.SetDirty(marker);
    PrefabUtility.RecordPrefabInstancePropertyModifications(marker);PrefabUtility.RecordPrefabInstancePropertyModifications(marker.gameObject);
   }
   GameAudioAuthoring.RefreshCarrySounds();
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   File.WriteAllLines("Logs/scene-cleanup-removed.txt",removed);
  }
  public static void Verify()
  {
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
   if(UnityEngine.Object.FindObjectsByType<PickupItem>(FindObjectsInactive.Include).Length!=0)throw new Exception("Pickup props remain");
   foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))if(t.name=="Temporary behavior buttons"||t.name=="Workbench")throw new Exception("Temporary furniture remains");
   var program=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();var launcher=program.GetComponent<PointLauncher>();
   if(!launcher||program.pointMarkers.Length!=8)throw new Exception("Missing launcher inventory");
   for(int i=0;i<8;i++)
   {
    var marker=program.pointMarkers[i];if(!marker||marker.pointId!=((char)('A'+i)).ToString()||!marker.stored||marker.gameObject.activeSelf)throw new Exception("Invalid stored beacon "+i);
    if(!marker.Launch(new Vector3(0,100,0),Vector3.forward,program.transform))throw new Exception("Cannot launch "+marker.pointId);
    marker.Attach(new Vector3(i*3,100,3),Vector3.up,null);
    if(marker.stored||!marker.gameObject.activeSelf||marker.InFlight)throw new Exception("Cannot deploy beacon");
    if(!marker.Recall(launcher.muzzle))throw new Exception("Cannot recall beacon");
    for(int n=0;n<3000&&!marker.stored;n++)marker.Advance(.1f);
    if(!marker.stored||marker.gameObject.activeSelf)throw new Exception("Recall failed");
   }
   Debug.Log("SCENE_CLEANUP_VERIFIED: temporary props absent; all A-H stored, launchable, attachable and recallable");
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
  }
 }
}
