using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Mach.Editor
{
 public static partial class TutorialAuthoring
 {
  public const string Path="Assets/Mach/Scenes/Tutorial.unity";
  static Color blue=new Color(.2f,.65f,.9f),gold=new Color(1,.7f,.25f),ink=new Color(.07f,.1f,.14f);
  static RectTransform Rect(Transform parent,string name,Vector2 pos,Vector2 size){var go=new GameObject(name,typeof(RectTransform));var t=go.GetComponent<RectTransform>();t.SetParent(parent,false);t.anchorMin=t.anchorMax=new Vector2(.5f,.5f);t.sizeDelta=size;t.anchoredPosition=pos;return t;}
  static Image Panel(Transform parent,string name,Vector2 pos,Vector2 size,Color color){var t=Rect(parent,name,pos,size);var image=t.gameObject.AddComponent<Image>();image.color=color;return image;}
  static Text Label(Transform parent,string name,string text,Vector2 pos,Vector2 size,int font=24){var t=Rect(parent,name,pos,size);var label=t.gameObject.AddComponent<Text>();label.font=ProjectTypography.Font;label.fontSize=font;label.text=text;label.color=Color.white;label.alignment=TextAnchor.MiddleLeft;label.raycastTarget=false;return label;}
  static void Node(Transform parent,string text,float x,float y,Color color){var p=Panel(parent,text,new Vector2(x,y),new Vector2(150,62),color);Label(p.transform,"Caption",text,Vector2.zero,new Vector2(138,58),20).alignment=TextAnchor.MiddleCenter;if(text!="START"&&!text.StartsWith("POINT")&&text!="AMMO"&&text!="RATE")Panel(p.transform,"IN",new Vector2(-75,0),Vector2.one*12,Color.white);Panel(p.transform,"OUT",new Vector2(75,0),Vector2.one*12,Color.white);}
  static void Line(Transform parent,Vector2 a,Vector2 b,Color color){var p=Panel(parent,"Connection",(a+b)/2,new Vector2(Vector2.Distance(a,b),4),color);p.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);}
  static void SmallNode(Transform parent,string text,float x,float y,Color color)
  {var p=Panel(parent,text,new Vector2(x,y),new Vector2(96,58),color);Label(p.transform,"Caption",text,Vector2.zero,new Vector2(90,54),17).alignment=TextAnchor.MiddleCenter;}
  static void Diagram(Transform parent,int stage)
  {
   if(stage==6)
   {
    SmallNode(parent,"START",-210,90,blue);SmallNode(parent,"DRIVE A",0,90,blue);SmallNode(parent,"DRIVE B",210,90,blue);
    Line(parent,new Vector2(-162,90),new Vector2(-48,90),Color.white);Line(parent,new Vector2(48,90),new Vector2(162,90),Color.white);
    SmallNode(parent,"POINT A",0,-60,gold);SmallNode(parent,"POINT B",210,-60,gold);
    Line(parent,new Vector2(0,-31),new Vector2(0,61),gold);Line(parent,new Vector2(210,-31),new Vector2(210,61),gold);
    Label(parent,"Legend","WHITE: OUT -> IN\nGOLD: parameter -> PARAM",new Vector2(0,200),new Vector2(480,65),20);
    Label(parent,"Order","FIRST A, THEN B\nEach action has its own target.",new Vector2(0,-180),new Vector2(480,80),22);return;
   }
   if(stage==7)
   {
    string[] sequence={"START","FACING","FIRE","WAIT","CEASE\nFIRE"};for(int n=0;n<5;n++){SmallNode(parent,sequence[n],-220+n*110,120,blue);if(n<4)Line(parent,new Vector2(-172+n*110,120),new Vector2(-158+n*110,120),Color.white);}
    SmallNode(parent,"POINT A",-110,-30,gold);SmallNode(parent,"AMMO",0,-100,gold);SmallNode(parent,"RATE",0,-200,gold);SmallNode(parent,"2 SEC",110,-30,gold);
    Line(parent,new Vector2(-110,-1),new Vector2(-110,91),gold);Line(parent,new Vector2(110,-1),new Vector2(110,91),gold);Line(parent,new Vector2(0,-71),new Vector2(0,91),gold);Line(parent,new Vector2(48,-200),new Vector2(55,-200),gold);Line(parent,new Vector2(55,-200),new Vector2(55,40),gold);Line(parent,new Vector2(55,40),new Vector2(0,40),gold);
    Label(parent,"Timing","START FIRING -> WAIT 2 SEC -> STOP",new Vector2(0,215),new Vector2(530,50),20);return;
   }
   if(stage>=4&&stage<=7||stage==9)
   {
    string action=stage==7||stage==9?"FIRE":"DRIVING";Node(parent,"START",-155,90,blue);Node(parent,action,155,90,blue);Line(parent,new Vector2(-80,90),new Vector2(80,90),Color.white);Label(parent,"Execution","OUT  ->  IN",new Vector2(-80,140),new Vector2(220,35),20);
    Node(parent,action=="FIRE"?"AMMO":"POINT A",-155,-75,gold);Line(parent,new Vector2(-80,-75),new Vector2(155,-75),gold);Line(parent,new Vector2(155,-75),new Vector2(155,59),gold);Panel(parent,"PARAM",new Vector2(155,59),Vector2.one*14,gold);
    Label(parent,"Parameter","PARAMETER -> PARAM",new Vector2(-50,-135),new Vector2(330,35),19);
    if(action=="FIRE"){Node(parent,"RATE",-155,-190,gold);Line(parent,new Vector2(-80,-190),new Vector2(210,-190),gold);Line(parent,new Vector2(210,-190),new Vector2(210,-50),gold);Line(parent,new Vector2(210,-50),new Vector2(155,-50),gold);}
    Label(parent,"Legend","WHITE: execution order\nGOLD: information for an action",new Vector2(0,210),new Vector2(480,65),20);
   }
   else
   {
    string[] top={"W A S D","LMB","E","RMB + WHEEL","","","","","E + E",""};
    string[] bottom={"MOVE TO BLUE PAD","SELECT AMMO","PICK UP / PLACE","A  B  C  D  E  F  G  H","","","","","MACHINE -> CASE -> TRUCK",""};
    Node(parent,top[stage],0,95,blue);Line(parent,new Vector2(0,64),new Vector2(0,-35),gold);Panel(parent,"Direction",new Vector2(0,-35),new Vector2(28,8),gold);
    Label(parent,"Illustration",bottom[stage],new Vector2(0,-95),new Vector2(490,100),26).alignment=TextAnchor.MiddleCenter;
    if(stage==3)Label(parent,"Recall","LMB: deploy\nE: recall selected beacon",new Vector2(0,-190),new Vector2(450,65),22).alignment=TextAnchor.MiddleCenter;
   }
  }
  static Material Mat(string name,Color c){string folder="Assets/Mach/Tutorial";Directory.CreateDirectory(folder);AssetDatabase.Refresh();string p=folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}m.color=c;return m;}
  static GameObject Box(string name,Vector3 pos,Vector3 size,Material mat){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;return go;}
  static void Sign(string words,Vector3 pos){var go=new GameObject(words+" label");go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0,180,0);var text=go.AddComponent<TextMesh>();text.font=ProjectTypography.Font;text.text=words;text.characterSize=.12f;text.fontSize=48;text.anchor=TextAnchor.MiddleCenter;go.GetComponent<Renderer>().sharedMaterial=ProjectTypography.Font.material;}
  public static void RepairDiagrams()
  {
   var scene=EditorSceneManager.OpenScene(Path);var t=UnityEngine.Object.FindAnyObjectByType<TutorialDirector>();
   for(int i=0;i<t.pages.Length;i++){var parent=t.pages[i].diagram.transform;while(parent.childCount>0)UnityEngine.Object.DestroyImmediate(parent.GetChild(0).gameObject);Diagram(parent,i);}
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  public static void RepairSpawn()
  {
   var scene=EditorSceneManager.OpenScene(Path);foreach(var spawner in UnityEngine.Object.FindObjectsByType<GhostSpawner>())spawner.spawnRadius=2;
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  public static void Author()
  {
   if(File.Exists(Path))throw new Exception("Tutorial already exists; preserve edits instead of overwriting.");
   if(!AssetDatabase.CopyAsset(WalkingSceneAuthoring.ScenePath,Path))throw new Exception("Scene copy failed");
   var scene=EditorSceneManager.OpenScene(Path);var root=new GameObject("Tutorial - checkpoints and guides");var tutorial=root.AddComponent<TutorialDirector>();
   tutorial.program=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();tutorial.launcher=tutorial.program.GetComponent<PointLauncher>();tutorial.placement=tutorial.program.GetComponent<MachinePlacement>();tutorial.driver=tutorial.program.GetComponent<TruckDriver>();tutorial.truck=UnityEngine.Object.FindAnyObjectByType<UtilityTruck>();tutorial.rack=UnityEngine.Object.FindAnyObjectByType<EquipmentRack>();tutorial.defense=UnityEngine.Object.FindAnyObjectByType<GhostDefense>();
   var drop=tutorial.rack.GetComponent<EquipmentAirdrop>();drop.enabled=false;drop.aircraft.gameObject.SetActive(false);drop.parachute.gameObject.SetActive(false);foreach(var r in drop.cargoRenderers)if(r)r.enabled=true;foreach(var c in drop.cargoColliders)if(c)c.enabled=true;
   // Remove the air-drop controller only in this duplicate, so its landed gate cannot block carrying.
   UnityEngine.Object.DestroyImmediate(drop);
   var origin=tutorial.zoneCenter;Box("Tutorial training ground",origin+Vector3.down*.3f,new Vector3(76,.6f,76),Mat("Training floor",new Color(.22f,.25f,.28f)));
   tutorial.walkGoal=Box("Walk here - blue",origin+new Vector3(0,.015f,-1),new Vector3(2,.025f,2),Mat("Blue marker",blue)).transform;
   tutorial.placementGoal=Box("Place ammo here - orange",origin+new Vector3(-9,.015f,-2),new Vector3(2,.025f,2),Mat("Orange marker",gold)).transform;
   int i=0;foreach(var slot in tutorial.rack.slots){slot.Unpack();var machine=slot.machine;machine.transform.SetParent(null,true);machine.transform.SetPositionAndRotation(origin+new Vector3(-9+(i++)*3,.05f,3),Quaternion.identity);if(machine.GetComponent<RobotCircuitRunner>()){tutorial.robot=machine.GetComponent<RobotCircuitRunner>();machine.transform.position=origin+new Vector3(0,.1f,8);}if(machine.GetComponent<AmmoSelectionConsole>())tutorial.ammo=machine.GetComponent<AmmoSelectionConsole>();Sign(slot.name.Replace(" fixed slot",""),machine.transform.position+new Vector3(0,2,0));}
   tutorial.rack.transform.SetPositionAndRotation(origin+new Vector3(11,.06f,3),Quaternion.identity);tutorial.truck.transform.SetPositionAndRotation(origin+new Vector3(17,.1f,-6),Quaternion.identity);
   tutorial.program.transform.SetPositionAndRotation(origin+new Vector3(0,.1f,-5),Quaternion.identity);tutorial.program.GetComponent<FirstPersonWalker>().ResetMotionAndLook();
   tutorial.defense.totalEnemies=3;tutorial.defense.home.maxHealth=12;tutorial.defense.home.transform.position=origin+new Vector3(0,.1f,6);
   int spawnIndex=0;foreach(var spawner in UnityEngine.Object.FindObjectsByType<GhostSpawner>()){spawner.gameObject.SetActive(spawnIndex++==0);spawner.transform.position=origin+new Vector3(0,.1f,32);spawner.spawnRadius=2;spawner.initialDelay=2;spawner.spawnInterval=4;}
   if(tutorial.defense.surface){tutorial.defense.surface.collectObjects=Unity.AI.Navigation.CollectObjects.Volume;tutorial.defense.surface.center=tutorial.defense.surface.transform.InverseTransformPoint(origin);tutorial.defense.surface.size=new Vector3(76,12,76);}
   Sign("TRAINING / CIRCUIT LAB",origin+new Vector3(0,3,5));Sign("A / DRIVE TARGET",origin+new Vector3(0,.15f,16));Sign("B / DRIVE TARGET",origin+new Vector3(5,.15f,18));
   var canvasGo=new GameObject("Tutorial Guide Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;var scaler=canvasGo.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
   tutorial.objective=Label(canvasGo.transform,"Current objective","Training starts here",new Vector2(0,380),new Vector2(1480,85),22);
   var guide=Panel(canvasGo.transform,"Lesson guide",Vector2.zero,new Vector2(1480,830),ink);tutorial.guidePanel=guide.gameObject;
   tutorial.heading=Label(guide.transform,"Title","01 / 10  BASIC MOVEMENT",new Vector2(0,335),new Vector2(1350,70),36);
   tutorial.body=Label(guide.transform,"Instructions","Press Play to begin training.",new Vector2(-340,10),new Vector2(620,530),25);tutorial.body.verticalOverflow=VerticalWrapMode.Overflow;
   tutorial.footer=Label(guide.transform,"Failure conditions","ENTER TO BEGIN",new Vector2(0,-345),new Vector2(1340,95),19);
   tutorial.continueButton=Panel(guide.transform,"Continue",new Vector2(510,-265),new Vector2(260,55),blue).rectTransform;Label(tutorial.continueButton,"Caption","ENTER / CONTINUE",Vector2.zero,new Vector2(240,50),22).alignment=TextAnchor.MiddleCenter;
   string[] titles={"BASIC MOVEMENT","PHYSICAL BUTTONS","CARRY AND PLACE","BEACON LAUNCHER","YOUR FIRST CIRCUIT","EDIT AND REPAIR","ACTION ORDER","A CONTROLLED BURST","TRANSPORT EQUIPMENT","FIRST ENGAGEMENT"};
   string[] guides={
    "Move with WASD and look with the mouse.\n\nWalk onto the blue pad in front of you.\n\nEach lesson checks what you actually do. A new lesson gives you a fresh checkpoint. F1 reopens this guide without losing progress.",
    "Find the AMMO machine on the left.\n\nUse LMB on its arrow buttons, then SELECT. This creates an Ammo parameter on your tablet.\n\nPhysical function buttons use LMB. Carry knobs use E.",
    "Aim at the round carry knob on the AMMO machine and press E.\n\nScroll to rotate the preview. Move it over the orange pad and press E when placement is valid.\n\nRed means blocked or unsupported. RMB cancels carrying.",
    "RMB raises the launcher. Scroll to select A. LMB fires it onto a surface.\n\nSelect A and press E to recall it from any distance. Fire A again.\n\nEach letter has its own color. RMB hides the launcher. The height ball can be dragged vertically with LMB.",
    "A is already deployed for this lesson. Use the DRIVING machine: choose A, press its green key.\n\nTAB opens the tablet. Connect START OUT to DRIVING IN. Connect POINT A to DRIVING PARAM.\n\nPress CHECK. Close the tablet and dock it on the robot using its interaction prompt. Watch it drive to A. Either endpoint can be clicked first.",
    "A small circuit is prepared for you. TAB opens it. Select DRIVING and press UNLINK.\n\nReconnect POINT A to its PARAM and press CHECK.\n\nDrag blocks to arrange them; scroll to zoom. CLEAR removes a selected block. CLEAR ALL retains START. Three unsuccessful checks reset this lesson.",
    "Use the DRIVING machine twice: once for A, once for B.\n\nConnect START -> first DRIVING -> second DRIVING. Give the first POINT A and the second POINT B through PARAM.\n\nCHECK and dock the tablet. Wait for the robot to visit both targets. Touching blocks are not connected until you wire their ports.",
    "Build START -> FACING -> FIRE -> WAIT -> CEASE FIRE.\n\nFACING needs POINT A. FIRE needs both AMMO and RATE. WAIT needs DURATION: use 2 seconds.\n\nUse the matching machines to add these blocks, CHECK, then dock. Fire continues during Wait; Cease Fire stops it. Other circuits do not have to include Facing or Fire.",
    "Carry each machine into its tray preview and press E to pack it. Pack all seven.\n\nCarry the whole tray to the truck bed and press E to attach it. LMB opens the door; E at the seat enters.\n\nWASD drives, SPACE brakes. Drive at least 8 metres, stop and E to exit. Unload the tray, then unpack one machine.",
    "Build START -> FACING -> FIRE. Connect POINT A, AMMO and RATE. CHECK and dock the tablet.\n\nR opens the engagement remote. Click its button when ready. Docking alone does not start enemies.\n\nDefeat three ghosts before they destroy the base. If the base falls, this stage restarts automatically. Remove the tablet to stop the robot."};
   tutorial.pages=new TutorialPage[10];for(i=0;i<10;i++){var diagram=Rect(guide.transform,"Diagram "+(i+1),new Vector2(345,20),new Vector2(560,490));Diagram(diagram,i);diagram.gameObject.SetActive(i==0);tutorial.pages[i]=new TutorialPage{title=titles[i],guide=guides[i],diagram=diagram.gameObject,timeLimit=i<4?300:900};}
   tutorial.inputScripts=new MonoBehaviour[]{tutorial.program.GetComponent<FirstPersonWalker>(),tutorial.program.pickup,tutorial.program.tablet,tutorial.launcher,tutorial.placement,tutorial.driver,tutorial.program.remote,tutorial.program.board,tutorial.program.GetComponent<PointHeightDrag>()};
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   var builds=EditorBuildSettings.scenes.ToList();if(!builds.Any(s=>s.path==Path)){builds.Add(new EditorBuildSettingsScene(Path,true));EditorBuildSettings.scenes=builds.ToArray();}
   Debug.Log("TUTORIAL_AUTHORED: duplicate scene, editable guides, diagrams, ten checkpoints and practice area");
  }
 }
}




