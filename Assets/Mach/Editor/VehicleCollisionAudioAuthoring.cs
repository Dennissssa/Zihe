using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Mach.Editor {
 [InitializeOnLoad] public static class VehicleCollisionAudioAuthoring {
  static VehicleCollisionAudioAuthoring(){EditorApplication.update+=Poll;}
  static void Poll(){const string request="Logs/InstallCollisionAudio.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;File.Delete(request);try{Apply();File.WriteAllText("Logs/collision-audio-saved.txt","Saved collision audio bindings.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/collision-audio-error.txt",e.ToString());}}
  [MenuItem("Mach/Audio/Install vehicle collision sounds")]
  public static void Apply(){
   var light=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Mach/Audio/VehicleCollision/Vehicle light knock.wav");
   var heavy=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Mach/Audio/VehicleCollision/Vehicle crash.wav");
   if(!light||!heavy)throw new Exception("Collision audio assets have not imported.");
   var active=SceneManager.GetActiveScene();
   foreach(var path in new[]{"Assets/Mach/Scenes/WalkingPickup.unity","Assets/Mach/Scenes/Tutorial.unity","Assets/Mach/Scenes/StartMenu.unity"}){
    var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
    bool changed=false;
    try{foreach(var root in scene.GetRootGameObjects())foreach(var bank in root.GetComponentsInChildren<GameAudio>(true)){
     Undo.RecordObject(bank,"Assign vehicle collision sounds");bank.vehicleImpactLight=light;bank.vehicleImpactHeavy=heavy;EditorUtility.SetDirty(bank);PrefabUtility.RecordPrefabInstancePropertyModifications(bank);changed=true;
    }if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}}
    finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
   }
   if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();
  }
 }
}
