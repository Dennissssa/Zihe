using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
namespace Mach.Editor {
 [InitializeOnLoad] public static class RetroMenuAuthoring {
  static RetroMenuAuthoring(){EditorApplication.update+=Poll;}
  static void Poll(){const string request="Temp/RetroMenu20261022.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;File.Delete(request);try{Apply();File.WriteAllText("Logs/retro-menu-saved.txt","Saved quit button, retro profile, renderer features and display protection.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/retro-menu-error.txt",e.ToString());}}
  static void Protect(GameObject go,Renderer[] surfaces){if(surfaces.Length==0)return;var marker=go.GetComponent<RetroSharpSurface>();if(!marker)marker=go.AddComponent<RetroSharpSurface>();marker.surfaces=surfaces;EditorUtility.SetDirty(marker);PrefabUtility.RecordPrefabInstancePropertyModifications(marker);}
  static void Wire(GameObject root){
   foreach(var r in root.GetComponentsInChildren<Renderer>(true))if(r.name.IndexOf("display glass",StringComparison.OrdinalIgnoreCase)>=0)Protect(r.gameObject,new[]{r});
   foreach(var c in root.GetComponentsInChildren<NavigationConsole>(true))Protect(c.gameObject,c.GetComponentsInChildren<TextMesh>(true).Select(t=>t.GetComponent<Renderer>()).Where(r=>r).ToArray());
   foreach(var c in root.GetComponentsInChildren<FireRateConsole>(true))Protect(c.gameObject,c.GetComponentsInChildren<TextMesh>(true).Select(t=>t.GetComponent<Renderer>()).Where(r=>r).ToArray());
   foreach(var c in root.GetComponentsInChildren<AmmoSelectionConsole>(true))Protect(c.gameObject,c.GetComponentsInChildren<TextMesh>(true).Select(t=>t.GetComponent<Renderer>()).Where(r=>r).ToArray());
   foreach(var tablet in root.GetComponentsInChildren<HeldTablet>(true))if(tablet.poseRoot)Protect(tablet.poseRoot.gameObject,tablet.poseRoot.GetComponentsInChildren<Renderer>(true));
  }
  [MenuItem("Mach/Polish/Install retro image and quit button")]
  public static void Apply(){
   const string profilePath="Assets/Mach/Lighting/Retro Image Settings.asset";
   var profile=AssetDatabase.LoadAssetAtPath<RetroImageSettings>(profilePath);if(!profile){profile=ScriptableObject.CreateInstance<RetroImageSettings>();AssetDatabase.CreateAsset(profile,profilePath);}
   const string materialPath="Assets/Mach/Lighting/Retro Image.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!material){material=new Material(Shader.Find("Mach/RetroImage"));AssetDatabase.CreateAsset(material,materialPath);}
   var mask=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mach/Lighting/Interaction mask.mat");
   foreach(var path in new[]{"Assets/Settings/PC_Renderer.asset","Assets/Settings/Mobile_Renderer.asset"}){
    var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);if(!renderer)continue;
    var feature=renderer.rendererFeatures.OfType<RetroImageFeature>().FirstOrDefault();if(!feature){feature=ScriptableObject.CreateInstance<RetroImageFeature>();feature.name="Retro scene - readable displays";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);}
    feature.settings=profile;feature.filterMaterial=material;feature.maskMaterial=mask;feature.Create();EditorUtility.SetDirty(feature);renderer.SetDirty();EditorUtility.SetDirty(renderer);
   }
   foreach(var path in new[]{"Assets/Mach/AmmoMachine/AmmoSwitchMachine.prefab","Assets/Mach/FireRateConsole/FireRateConsole.prefab","Assets/Mach/NavigationConsole/NavigationConsole.prefab","Assets/Mach/BehaviorConsoles/FacingConsole.prefab","Assets/Mach/BehaviorConsoles/DrivingConsole.prefab","Assets/Mach/TimedFire/TimerConsole.prefab","Assets/Mach/Tablet/RuggedTablet.prefab","Assets/Mach/Prefabs/FirstPersonPlayer.prefab"}){
    var prefab=PrefabUtility.LoadPrefabContents(path);try{Wire(prefab);if(path.EndsWith("RuggedTablet.prefab"))Protect(prefab,prefab.GetComponentsInChildren<Renderer>(true));PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
   }
   var active=SceneManager.GetActiveScene();
   foreach(var path in new[]{"Assets/Mach/Scenes/StartMenu.unity","Assets/Mach/Scenes/Tutorial.unity","Assets/Mach/Scenes/WalkingPickup.unity"}){
    var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
    foreach(var root in scene.GetRootGameObjects()){
     Wire(root);
     foreach(var menu in root.GetComponentsInChildren<StartMenu>(true)){
      if(!menu.quitButton){var go=UnityEngine.Object.Instantiate(menu.settingsButton.gameObject,menu.settingsButton.transform.parent);go.name="Quit Game";menu.quitButton=go.GetComponent<Button>();menu.quitButton.onClick=new Button.ButtonClickedEvent();}
      var source=(RectTransform)menu.settingsButton.transform;var target=(RectTransform)menu.quitButton.transform;target.anchorMin=source.anchorMin;target.anchorMax=source.anchorMax;target.pivot=source.pivot;target.sizeDelta=source.sizeDelta;target.anchoredPosition=source.anchoredPosition+new Vector2(source.rect.width+20,0);
      foreach(var label in menu.quitButton.GetComponentsInChildren<MenuBilingualText>(true)){label.english="QUIT GAME";label.chinese="退出游戏";label.Refresh();EditorUtility.SetDirty(label);}
      EditorUtility.SetDirty(menu);
     }
    }
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
   }
   if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();
  }
 }
}
