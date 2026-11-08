using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Object=UnityEngine.Object;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class StartMenuRequest
 {
  static StartMenuRequest(){EditorApplication.update+=Poll;}
  static void Poll(){if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/StartMenu.request"))return;File.Delete("Temp/StartMenu.request");try{StartMenuAuthoring.Apply();File.WriteAllText("Logs/start-menu-applied.txt","Saved");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/start-menu-error.txt",e.ToString());}}
 }
 public static class StartMenuAuthoring
 {
  public const string Path="Assets/Mach/Scenes/StartMenu.unity";
  static Font en,zh;static Color ink=new Color(.055f,.085f,.105f),paper=new Color(.92f,.94f,.91f),muted=new Color(.57f,.66f,.69f),cyan=new Color(.28f,.8f,.88f),orange=new Color(1,.4f,.22f);
  static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
  {var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
  static Image Panel(Transform parent,string name,float x,float y,float w,float h,Color color){var r=Rect(parent,name,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=color;return im;}
  static Text Text(Transform parent,string name,string english,string chinese,float x,float y,float w,float h,int size,Color color)
  {
   var r=Rect(parent,name,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=en;t.text=english;t.color=color;t.fontSize=size;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;
   t.verticalOverflow=VerticalWrapMode.Overflow;var l=r.gameObject.AddComponent<MenuBilingualText>();l.english=english;l.chinese=chinese;l.englishFont=en;l.chineseFont=zh;return t;
  }
  static Button Button(Transform parent,string name,string english,string chinese,float x,float y,float w,float h,Color color,int size=26)
  {
   var bg=Panel(parent,name,x,y,w,h,color);var b=bg.gameObject.AddComponent<Button>();b.targetGraphic=bg;var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.8f,1,1);colors.selectedColor=new Color(.8f,1,1);colors.pressedColor=new Color(.6f,.8f,.85f);colors.fadeDuration=.14f;b.colors=colors;
   Text(bg.transform,"Label",english,chinese,24,0,w-48,h,size,paper);return b;
  }
  static Slider Slider(Transform parent,string name,float x,float y,float min,float max)
  {
   var root=Rect(parent,name,x,y,570,44);var s=root.gameObject.AddComponent<Slider>();s.minValue=min;s.maxValue=max;
   Panel(root,"Track",0,18,570,6,new Color(.2f,.27f,.3f));var fillArea=Rect(root,"Fill area",0,18,570,6);var fill=Panel(fillArea,"Fill",0,0,0,0,cyan);s.fillRect=fill.rectTransform;
   var area=Rect(root,"Handle travel",0,7,570,28);var handle=Panel(area,"Handle",0,0,18,0,paper);handle.rectTransform.pivot=new Vector2(.5f,.5f);s.handleRect=handle.rectTransform;s.targetGraphic=handle;s.direction=UnityEngine.UI.Slider.Direction.LeftToRight;return s;
  }
  static Material Mat(string name,Color color,float metallic=0,float smooth=.4f)
  {
   string path="Assets/Mach/Menu/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);AssetDatabase.CreateAsset(m,path);return m;
  }
  [MenuItem("Mach/Menu/Create start menu")]
  public static void Apply()
  {
   if(File.Exists(Path)){var existing=SceneManager.GetSceneByPath(Path);if(existing.isLoaded&&SceneManager.GetActiveScene()!=existing)EditorSceneManager.CloseScene(existing,true);Debug.Log("Start menu already exists; preserving edits.");return;}
   en=ProjectTypography.Font;zh=AssetDatabase.LoadAssetAtPath<Font>("Assets/Mach/Resources/ChineseUI.ttf");if(!zh)throw new Exception("Chinese font is not imported yet");Directory.CreateDirectory("Assets/Mach/Menu");
   var active=SceneManager.GetActiveScene();var source=SceneManager.GetSceneByPath(TutorialAuthoring.Path);bool opened=!source.isLoaded;if(opened)source=EditorSceneManager.OpenScene(TutorialAuthoring.Path,OpenSceneMode.Additive);
   var robot=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RobotCircuitRunner>(true)).First();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
   var display=Object.Instantiate(robot.gameObject);display.name="Display robot - visual only";display.transform.SetParent(null,true);SceneManager.MoveGameObjectToScene(display,scene);
   foreach(var c in display.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(c);
   foreach(var c in display.GetComponentsInChildren<Joint>(true))Object.DestroyImmediate(c);
   foreach(var c in display.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(c);
   foreach(var c in display.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
   foreach(var c in display.GetComponentsInChildren<AudioSource>(true))Object.DestroyImmediate(c);
   foreach(var c in display.GetComponentsInChildren<LineRenderer>(true))Object.DestroyImmediate(c);
   foreach(var c in display.GetComponentsInChildren<Light>(true))Object.DestroyImmediate(c);
   foreach(var r in display.GetComponentsInChildren<Renderer>(true))if(r is not LineRenderer)r.enabled=true;
   display.SetActive(true);display.transform.position=new Vector3(2.3f,0,0);display.transform.rotation=Quaternion.Euler(0,-25,0);
   void Box(string name,Vector3 pos,Vector3 size,Material material){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(g.GetComponent<Collider>());}
   var dark=Mat("Stage graphite",new Color(.04f,.07f,.09f),.3f,.6f);Box("Display floor",new Vector3(0,-.22f,0),new Vector3(60,.25f,60),dark);Box("Low display plinth",new Vector3(2.3f,-.055f,0),new Vector3(4.5f,.15f,4),dark);
   var rim=Mat("Cyan rim",cyan);rim.EnableKeyword("_EMISSION");rim.SetColor("_EmissionColor",cyan*2);EditorUtility.SetDirty(rim);Box("Display edge light",new Vector3(2.3f,-.025f,2),new Vector3(4.5f,.035f,.035f),rim);
   var cameraObject=new GameObject("Menu Camera",typeof(Camera),typeof(AudioListener));var camera=cameraObject.GetComponent<Camera>();cameraObject.tag="MainCamera";camera.transform.position=new Vector3(7,3.5f,9);camera.transform.LookAt(new Vector3(.25f,1,0));camera.fieldOfView=42;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=ink;
   void Light(string name,Vector3 pos,Color color,float intensity){var g=new GameObject(name,typeof(Light));g.transform.position=pos;g.transform.LookAt(display.transform.position+Vector3.up);var l=g.GetComponent<Light>();l.type=LightType.Spot;l.range=25;l.spotAngle=75;l.intensity=intensity;l.color=color;l.shadows=LightShadows.Soft;}
   Light("Soft key",new Vector3(4,7,5),new Color(1,.87f,.7f),13);Light("Cool rim",new Vector3(5,4,-3),cyan,17);Light("Front fill",new Vector3(-1,4,4),new Color(.65f,.8f,1),6);
   camera.transform.position=new Vector3(6,3,5);camera.transform.LookAt(display.transform.position+Vector3.up);camera.transform.position-=camera.transform.right*2.8f;
   var sun=new GameObject("Studio soft light",typeof(Light));sun.transform.rotation=Quaternion.Euler(35,-35,0);sun.GetComponent<Light>().type=LightType.Directional;sun.GetComponent<Light>().intensity=1.7f;sun.GetComponent<Light>().color=new Color(1,.91f,.81f);
   RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.38f,.42f);RenderSettings.skybox=null;
   var ui=new GameObject("Start Menu Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(StartMenu));var canvas=ui.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;var scaler=ui.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;var menu=ui.GetComponent<StartMenu>();
   var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
   var shade=Panel(ui.transform,"Left dark overlay",0,0,910,900,new Color(.025f,.045f,.06f,.88f));shade.raycastTarget=false;
   Text(ui.transform,"Brand","MACH","MACH",76,42,420,105,96,paper);Panel(ui.transform,"Brand accent",80,161,55,5,orange);
   Text(ui.transform,"Edition","TACTICAL AUTOMATION / PROTOTYPE","战术自动化 / 原型版本",80,178,680,30,19,muted);
   Text(ui.transform,"Upper right index","SYSTEM  /  01","系统  /  01",1260,65,280,32,20,muted);
   var home=Rect(ui.transform,"Home",0,0,1600,900);menu.home=home.gameObject;
   Text(home,"Hero heading","BUILD A PLAN.\nLET IT RUN.","编写计划。\n让它执行。",80,262,770,180,72,paper);
   Text(home,"Hero description","Connect your logic. Deploy your machines.\nDefend on your own terms.","连接行为回路，部署你的机器。\n用自己的方式构建防线。",84,450,700,75,24,muted);
   menu.tutorialButton=Button(home,"01 - Tutorial","","",80,570,410,200,new Color(.13f,.35f,.42f));
   Panel(menu.tutorialButton.transform,"Active stripe",0,0,5,200,cyan);
   Text(menu.tutorialButton.transform,"Number","01 / TRAINING","01 / 训练",28,20,345,32,20,cyan);
   Text(menu.tutorialButton.transform,"Title","PLAY TUTORIAL","游玩教程",28,67,355,50,34,paper);
   Text(menu.tutorialButton.transform,"Description","LEARN. CONNECT. EXECUTE.  >","学习交互、连线与执行  >",28,140,355,36,19,muted);
   menu.playButton=Button(home,"02 - Play","","",510,570,350,200,new Color(.09f,.14f,.17f,.94f));
   Text(menu.playButton.transform,"Number","02 / DEPLOYMENT","02 / 部署",28,20,300,32,20,orange);
   Text(menu.playButton.transform,"Title","START GAME","直接开始游戏",28,67,300,50,32,paper);
   Text(menu.playButton.transform,"Description","ENTER THE FIELD  >","进入正式场景  >",28,140,300,36,19,muted);
   menu.settingsButton=Button(home,"Settings","SETTINGS  /  OPTIONS","设置  /  选项",80,804,410,48,new Color(.09f,.14f,.17f,.8f),22);
   Text(home,"Footer","MACH / ENGINEER YOUR OWN RESPONSE","MACH / 用逻辑构建你的应对方式",995,810,525,45,18,muted);
   var settings=Panel(ui.transform,"Settings",0,216,1600,684,new Color(.025f,.045f,.06f,.97f));menu.settings=settings.gameObject;
   Text(settings.transform,"Heading","SETTINGS","设置",80,34,750,74,54,paper);Text(settings.transform,"Settings subtitle","CHANGES ARE SAVED AUTOMATICALLY","更改会自动保存，并应用于教程和正式游戏",84,113,1100,36,21,muted);
   Text(settings.transform,"Sensitivity label","MOUSE SENSITIVITY","鼠标灵敏度",84,186,420,50,27,paper);menu.sensitivity=Slider(settings.transform,"Mouse sensitivity",610,190,.25f,3);menu.sensitivityValue=Text(settings.transform,"Sensitivity value","1.00 x","1.00 x",1240,184,200,50,29,cyan);
   Text(settings.transform,"Volume label","MASTER VOLUME","主音量",84,270,420,50,27,paper);menu.volume=Slider(settings.transform,"Master volume",610,274,0,1);menu.volumeValue=Text(settings.transform,"Volume value","100 %","100 %",1240,268,200,50,29,cyan);
   Text(settings.transform,"Language label","LANGUAGE","语言",84,352,420,50,27,paper);menu.englishButton=Button(settings.transform,"English","ENGLISH","ENGLISH",610,353,260,52,ink);menu.chineseButton=Button(settings.transform,"Chinese","简体中文","简体中文",890,353,290,52,ink);var chineseLabel=menu.chineseButton.GetComponentInChildren<MenuBilingualText>();chineseLabel.englishFont=zh;
   Text(settings.transform,"Invert label","INVERT VERTICAL LOOK","反转垂直视角",84,437,450,50,27,paper);menu.invertButton=Button(settings.transform,"Invert Y","","",610,438,260,52,ink);menu.invertValue=Text(menu.invertButton.transform,"Value","OFF","关闭",24,0,200,52,27,paper);
   menu.fullscreenButton=Button(settings.transform,"Fullscreen","TOGGLE FULLSCREEN","切换全屏",890,438,360,52,ink,23);
   menu.backButton=Button(settings.transform,"Back","<  BACK","<  返回",80,570,300,62,new Color(.13f,.35f,.42f));menu.defaultsButton=Button(settings.transform,"Defaults","RESET CONTROLS / AUDIO","重置控制与音量",910,570,530,62,ink,23);
   menu.loading=Panel(ui.transform,"Loading",0,0,1600,900,new Color(.025f,.045f,.06f,.98f)).gameObject;menu.loadingText=Text(menu.loading.transform,"Progress","LOADING...","正在载入…",120,360,1300,100,54,paper);
   settings.gameObject.SetActive(false);menu.loading.SetActive(false);
   EditorSceneManager.SaveScene(scene,Path);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Path,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=Path)).ToArray();AssetDatabase.SaveAssets();
   if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);if(opened)EditorSceneManager.CloseScene(source,true);EditorSceneManager.CloseScene(scene,true);Debug.Log("START_MENU_SAVED");
  }
  public static void RebuildPreview(){if(!Application.dataPath.Replace('\\','/').Contains("/.verification/"))throw new Exception("Verification only");EditorSceneManager.OpenScene(TutorialAuthoring.Path);AssetDatabase.DeleteAsset(Path);ApplyAndPreview();}
  public static void ApplyAndPreview(){EditorSceneManager.OpenScene(TutorialAuthoring.Path);Apply();var scene=EditorSceneManager.OpenScene(Path);var menu=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<StartMenu>(true)).First();Capture(menu,false,"Logs/start-menu-en.png");Capture(menu,true,"Logs/start-menu-settings-zh.png");Debug.Log("START_MENU_PREVIEW_VERIFIED");}
  static void Capture(StartMenu menu,bool settings,string path)
  {
   menu.home.SetActive(!settings);menu.settings.SetActive(settings);
   foreach(var label in menu.GetComponentsInChildren<MenuBilingualText>(true)){var t=label.GetComponent<Text>();t.text=settings?label.chinese:label.english;t.font=settings?label.chineseFont:label.englishFont;t.fontStyle=settings?FontStyle.Bold:FontStyle.Normal;}menu.sensitivity.value=1;menu.volume.value=1;
   var canvas=menu.GetComponent<Canvas>();var camera=Object.FindAnyObjectByType<Camera>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;
   var rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;var im=new Texture2D(1600,900,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,1600,900),0,0);im.Apply();Directory.CreateDirectory("Logs");File.WriteAllBytes(path,im.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(im);
   canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;
  }
 }
}


