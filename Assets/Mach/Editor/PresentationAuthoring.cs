using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class PresentationRequest
 {
  static PresentationRequest(){EditorApplication.update+=Poll;}
  static void Poll(){if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/Presentation.request"))return;File.Delete("Temp/Presentation.request");try{PresentationAuthoring.Apply();File.WriteAllText("Logs/presentation-applied.txt","Saved");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/presentation-error.txt",e.ToString());}}
 }
 public static class PresentationAuthoring
 {
  [MenuItem("Mach/Menu/Apply presentation and localization")]
  public static void Apply()
  {
   var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Mach/Resources/ChineseUIHeavy.ttf");if(!font)throw new Exception("Heavy Chinese font not imported");var active=SceneManager.GetActiveScene();
   foreach(var path in new[]{StartMenuAuthoring.Path,TutorialAuthoring.Path,"Assets/Mach/Scenes/WalkingPickup.unity"})
   {
    var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
    var roots=scene.GetRootGameObjects();var localization=roots.SelectMany(g=>g.GetComponentsInChildren<LocalizedScene>(true)).FirstOrDefault();if(!localization)localization=new GameObject("Language - Chinese and English").AddComponent<LocalizedScene>();localization.chineseFont=font;EditorUtility.SetDirty(localization);
    if(path==StartMenuAuthoring.Path){foreach(var l in roots.SelectMany(g=>g.GetComponentsInChildren<MenuBilingualText>(true))){l.chineseFont=font;if(l.english=="简体中文")l.englishFont=font;EditorUtility.SetDirty(l);}AuthorPerformance(roots.First(g=>g.name=="Display robot - visual only"));}
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
   }
   if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();Debug.Log("PRESENTATION_AUTHORED");
  }
  static void AuthorPerformance(GameObject root)
  {
   var all=root.GetComponentsInChildren<Transform>(true);var p=root.GetComponent<MenuRobotPerformance>();if(!p)p=root.AddComponent<MenuRobotPerformance>();p.turret=all.First(t=>t.name=="Turret azimuth pivot");p.gun=all.First(t=>t.name=="Gun elevation pivot");p.muzzle=all.First(t=>t.name=="Muzzle visual");p.status=root.GetComponentsInChildren<TextMesh>(true).FirstOrDefault();
   RobotTrackModule Track(string name){var t=all.First(x=>x.name==name);var module=t.GetComponent<RobotTrackModule>();if(!module)module=t.gameObject.AddComponent<RobotTrackModule>();module.links=t.GetComponentsInChildren<Transform>(true).Where(x=>x.name.StartsWith("Tread link ")).OrderBy(x=>x.name).ToArray();module.wheels=t.GetComponentsInChildren<Transform>(true).Where(x=>x.name.StartsWith("Track wheel ")).OrderBy(x=>x.name).ToArray();EditorUtility.SetDirty(module);return module;}p.leftTrack=Track("Left track module");p.rightTrack=Track("Right track module");
   var tracer=all.First(t=>t.name=="Authored reusable shot tracer");p.tracer=tracer.GetComponent<LineRenderer>();if(!p.tracer)p.tracer=tracer.gameObject.AddComponent<LineRenderer>();var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mach/Menu/Performance tracer.mat");if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(mat,"Assets/Mach/Menu/Performance tracer.mat");}p.tracer.sharedMaterial=mat;p.tracer.positionCount=2;p.tracer.useWorldSpace=true;p.tracer.startWidth=.018f;p.tracer.endWidth=.004f;p.tracer.enabled=false;
   var flash=all.First(t=>t.name=="Authored muzzle flash");p.flash=flash.GetComponent<Light>();if(!p.flash)p.flash=flash.gameObject.AddComponent<Light>();p.flash.type=LightType.Point;p.flash.intensity=3;p.flash.range=2;p.flash.enabled=false;EditorUtility.SetDirty(p);
  }
  public static void VerifyAuthor(){EditorSceneManager.OpenScene(TutorialAuthoring.Path);Apply();foreach(var path in new[]{StartMenuAuthoring.Path,TutorialAuthoring.Path,"Assets/Mach/Scenes/WalkingPickup.unity"}){var s=EditorSceneManager.OpenScene(path);var all=s.GetRootGameObjects();var loc=all.SelectMany(g=>g.GetComponentsInChildren<LocalizedScene>(true)).Single();if(!loc.chineseFont)throw new Exception("Missing font");var texts=all.SelectMany(g=>g.GetComponentsInChildren<Text>(true)).Select(t=>t.text).Concat(all.SelectMany(g=>g.GetComponentsInChildren<TextMesh>(true)).Select(t=>t.text));File.WriteAllLines("Logs/localization-"+System.IO.Path.GetFileNameWithoutExtension(path)+".txt",texts.Distinct().Select(t=>t+" => "+GameLocalization.Chinese(t)));}Debug.Log("PRESENTATION_AUTHOR_VERIFIED");}
 }
}
