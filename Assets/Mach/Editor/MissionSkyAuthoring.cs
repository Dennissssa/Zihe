using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Mach.Editor {
 [InitializeOnLoad] public static class MissionSkyAuthoring {
  static MissionSkyAuthoring(){EditorApplication.update+=Poll;}
  static void Poll(){const string request="Temp/MissionAbstractSky.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;File.Delete(request);try{Apply();File.WriteAllText("Logs/mission-sky-saved.txt","Saved abstract sky to WalkingPickup only.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/mission-sky-error.txt",e.ToString());}}
  [MenuItem("Mach/Polish/Apply mission abstract sky")]
  public static void Apply(){
   const string path="Assets/Mach/Lighting/Mission Abstract Sky.mat";
   var material=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!material){material=new Material(Shader.Find("Mach/Abstract Dusk Sky"));AssetDatabase.CreateAsset(material,path);}
   var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath("Assets/Mach/Scenes/WalkingPickup.unity");bool opened=!scene.isLoaded;
   if(opened)scene=EditorSceneManager.OpenScene("Assets/Mach/Scenes/WalkingPickup.unity",OpenSceneMode.Additive);
   try{SceneManager.SetActiveScene(scene);RenderSettings.skybox=material;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
   finally{if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
  }
 }
}
