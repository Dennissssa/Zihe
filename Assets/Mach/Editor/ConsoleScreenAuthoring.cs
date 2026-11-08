using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class ConsoleScreenRequest
 {
  static ConsoleScreenRequest(){EditorApplication.update+=Poll;}
  static void Poll(){if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/ConsoleScreensDoubleText.request"))return;File.Delete("Temp/ConsoleScreensDoubleText.request");try{ConsoleScreenAuthoring.Apply();File.WriteAllText("Logs/console-screens-applied.txt","Saved");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/console-screens-error.txt",e.ToString());}}
 }
 public static class ConsoleScreenAuthoring
 {
  const string Folder="Assets/Mach/ScreenDesign";
  static readonly Color Ink=new Color(.91f,.97f,1f);
  static Material Backlight(string role,Color tint)
  {
   var path=Folder+"/"+role+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   m.SetColor("_BaseColor",tint*.12f);m.SetColor("_EmissionColor",tint*.18f);m.EnableKeyword("_EMISSION");m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;m.SetFloat("_Smoothness",.15f);m.SetFloat("_Metallic",0);m.SetFloat("_SpecularHighlights",0);m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");EditorUtility.SetDirty(m);return m;
  }
  static Transform Find(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
  static void Label(TextMesh text,Transform screen,float x,float y,float size,string content=null)
  {
   if(!text)return;Undo.RecordObject(text,"Redesign screen label");Undo.RecordObject(text.transform,"Arrange screen");
   // Work in the current glass plane, preserving the model's authored placement and tilt.
   var depth=screen.InverseTransformPoint(text.transform.position).z;
   text.transform.position=screen.TransformPoint(new Vector3(x,y,Mathf.Min(depth,-.007f)));
   text.characterSize=size*.125f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=Ink;
   if(content!=null)text.text=content;EditorUtility.SetDirty(text);EditorUtility.SetDirty(text.transform);
   PrefabUtility.RecordPrefabInstancePropertyModifications(text);PrefabUtility.RecordPrefabInstancePropertyModifications(text.transform);
  }
  static void HideCaption(TextMesh text)
  {
   // Keep references used by interaction logic; remove secondary captions from the visible layout.
   if(!text)return;var renderer=text.GetComponent<Renderer>();if(!renderer)return;
   renderer.enabled=false;EditorUtility.SetDirty(renderer);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
  }
  static void RemoveScreenLight(GameObject root)
  {
   foreach(var light in root.GetComponentsInChildren<Light>(true))
    if(light.name=="Authored instrument glow")Object.DestroyImmediate(light.gameObject);
  }
  static int Redesign(GameObject root)
  {
   int count=0;
   foreach(var c in root.GetComponentsInChildren<NavigationConsole>(true))
   {
    if(c.mode==CircuitKind.Fire)continue;var screen=Find(c.gameObject,"Display glass");if(!screen)continue;
    RemoveScreenLight(c.gameObject);
    bool driving=c.mode==CircuitKind.DriveTo,timer=c.mode==CircuitKind.Wait;
    var tint=timer?new Color(.66f,.46f,.19f):driving?new Color(.25f,.59f,.38f):new Color(.22f,.48f,.67f);
    var renderer=screen.GetComponent<Renderer>();renderer.sharedMaterial=Backlight(timer?"Timer amber":driving?"Driving green":"Facing blue",tint);EditorUtility.SetDirty(renderer);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
    var heading=Find(c.gameObject,"Heading")?.GetComponent<TextMesh>();
    Label(c.modeText,screen,0,.12f,.105f,timer?"WAIT":driving?"DRIVING":"FACING");
    HideCaption(heading);
    Label(c.targetText,screen,0,-.055f,.144f);
    HideCaption(c.statusText);
    count++;
   }
   foreach(var c in root.GetComponentsInChildren<FireRateConsole>(true))
   {
    var screen=Find(c.gameObject,"Portrait display glass");if(!screen)continue;var renderer=screen.GetComponent<Renderer>();renderer.sharedMaterial=Backlight("Fire rate coral",new Color(.68f,.32f,.25f));EditorUtility.SetDirty(renderer);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
    RemoveScreenLight(c.gameObject);
    Label(Find(c.gameObject,"Display title")?.GetComponent<TextMesh>(),screen,0,.18f,.06f,"FIRE RATE");
    Label(c.selectedText,screen,0,-.015f,.19f);
    HideCaption(Find(c.gameObject,"Preset index")?.GetComponent<TextMesh>());
    HideCaption(c.statusText);count++;
   }
   foreach(var c in root.GetComponentsInChildren<AmmoSelectionConsole>(true))
   {
    var screen=Find(c.gameObject,"Dark display glass");if(!screen)continue;var renderer=screen.GetComponent<Renderer>();renderer.sharedMaterial=Backlight("Ammo warm red",new Color(.63f,.36f,.28f));EditorUtility.SetDirty(renderer);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
    RemoveScreenLight(c.gameObject);
    Label(Find(c.gameObject,"Ammo selection display")?.Find("Title")?.GetComponent<TextMesh>(),screen,0,.18f,.075f,"AMMUNITION");
    Label(c.selectedText,screen,0,-.045f,.20f);
    HideCaption(c.indexText);
    if(c.optionLabels!=null)for(int i=0;i<c.optionLabels.Length;i++)HideCaption(c.optionLabels[i]);
    HideCaption(c.statusText);c.displayColor=Ink;c.inactiveDisplayColor=new Color(.57f,.68f,.72f);EditorUtility.SetDirty(c);PrefabUtility.RecordPrefabInstancePropertyModifications(c);count++;
   }
   return count;
  }
  [MenuItem("Mach/Polish/Redesign console screens")]
  public static void Apply()
  {
   Directory.CreateDirectory(Folder);var active=SceneManager.GetActiveScene();int total=0;
   // Target only machine prefabs; preserve all other authored model and scene objects.
   foreach(string path in new[]{"Assets/Mach/AmmoMachine/AmmoSwitchMachine.prefab","Assets/Mach/FireRateConsole/FireRateConsole.prefab","Assets/Mach/NavigationConsole/NavigationConsole.prefab","Assets/Mach/BehaviorConsoles/FacingConsole.prefab","Assets/Mach/BehaviorConsoles/DrivingConsole.prefab","Assets/Mach/TimedFire/TimerConsole.prefab"})
   {if(!File.Exists(path))continue;var prefab=PrefabUtility.LoadPrefabContents(path);try{total+=Redesign(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}}
   foreach(var path in new[]{"Assets/Mach/Scenes/Tutorial.unity","Assets/Mach/Scenes/WalkingPickup.unity"})
   {var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);foreach(var root in scene.GetRootGameObjects())total+=Redesign(root);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);}
   if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();Debug.Log("CONSOLE_SCREENS_SAVED "+total);
  }
  public static void Verify()
  {
   EditorSceneManager.OpenScene("Assets/Mach/Scenes/WalkingPickup.unity");Apply();
   foreach(string name in new[]{"FacingConsole","DrivingConsole","TimerConsole"})
   {
    string folder=name=="TimerConsole"?"TimedFire":"BehaviorConsoles";
    var p=PrefabUtility.LoadPrefabContents("Assets/Mach/"+folder+"/"+name+".prefab");try{var c=p.GetComponent<NavigationConsole>();var glass=Find(p,"Display glass");if(!glass.GetComponent<Renderer>().sharedMaterial.IsKeywordEnabled("_EMISSION"))throw new Exception("Screen not emissive");if(c.modeText.transform.position.y<=c.targetText.transform.position.y)throw new Exception("Title not above parameter");Preview(p,"Logs/"+name+"-screen.png");}finally{PrefabUtility.UnloadPrefabContents(p);}
   }
   foreach(var path in new[]{"Assets/Mach/AmmoMachine/AmmoSwitchMachine.prefab","Assets/Mach/FireRateConsole/FireRateConsole.prefab"})
   {var p=PrefabUtility.LoadPrefabContents(path);try{Preview(p,"Logs/"+p.name+"-screen.png");}finally{PrefabUtility.UnloadPrefabContents(p);}}
   foreach(var path in new[]{"Assets/Mach/Scenes/Tutorial.unity","Assets/Mach/Scenes/WalkingPickup.unity"})
    if(File.ReadAllText(path).Contains("Authored instrument glow"))throw new Exception("Screen point light remains: "+path);
   Debug.Log("CONSOLE_SCREENS_VERIFIED");
  }
  static void Preview(GameObject source,string path)
  {
   var scene=EditorSceneManager.NewPreviewScene();var model=Object.Instantiate(source);SceneManager.MoveGameObjectToScene(model,scene);model.SetActive(true);model.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);model.transform.localScale=Vector3.one;
   var cameraGo=new GameObject("Preview",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraGo,scene);var camera=cameraGo.GetComponent<Camera>();camera.scene=scene;camera.transform.position=new Vector3(-.15f,1.65f,-3);camera.transform.LookAt(new Vector3(0,1,0));camera.orthographic=true;camera.orthographicSize=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.065f,.075f);
   var light=new GameObject("Key",typeof(Light));SceneManager.MoveGameObjectToScene(light,scene);light.transform.rotation=Quaternion.Euler(30,-30,0);light.GetComponent<Light>().type=LightType.Directional;light.GetComponent<Light>().intensity=1.4f;
   bool old=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;var rt=new RenderTexture(900,1100,24);camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;var image=new Texture2D(900,1100,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,900,1100),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);ShaderUtil.allowAsyncCompilation=old;EditorSceneManager.ClosePreviewScene(scene);
  }
 }
}


