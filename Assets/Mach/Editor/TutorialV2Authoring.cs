using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class TutorialV2Request
 {
  static TutorialV2Request(){EditorApplication.update+=Poll;}
  static void Poll(){if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/TutorialV2.request"))return;File.Delete("Temp/TutorialV2.request");try{TutorialAuthoring.ApplyV2();File.WriteAllText("Logs/tutorial-v2-applied.txt","Eight task-based lessons, HUD, route previews and UNLINK removal saved.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/tutorial-v2-error.txt",e.ToString());}}
 }
 public static partial class TutorialAuthoring
 {
  [MenuItem("Mach/Tutorial/Migrate to eight task lessons")]
  public static void ApplyV2()
  {
   var active=EditorSceneManager.GetActiveScene();
   foreach(var path in new[]{Path,"Assets/Mach/Scenes/WalkingPickup.unity"})
   {
    var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);
    if(path==Path)MigrateV2(scene);
    foreach(var root in scene.GetRootGameObjects()){RemoveUnlink(root);AuthorRoutes(root);}
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(active.IsValid()&&active.isLoaded)EditorSceneManager.SetActiveScene(active);if(opened)EditorSceneManager.CloseScene(scene,true);
   }
   foreach(var path in new[]{"Assets/Mach/Prefabs/FirstPersonPlayer.prefab","Assets/Mach/Tablet/RuggedTablet.prefab"}){var root=PrefabUtility.LoadPrefabContents(path);try{RemoveUnlink(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
   AssetDatabase.SaveAssets();
  }
  public static void PolishV2Diagram()
  {
   ApplyV2();
   var scene=EditorSceneManager.OpenScene(Path);
   var t=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TutorialDirector>(true)).First();
   var diagram=t.pages[5].diagram.transform;
   for(int i=diagram.childCount-1;i>=0;i--)Object.DestroyImmediate(diagram.GetChild(i).gameObject);
   Diagram(diagram,7);t.milestones=new bool[8];
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Debug.Log("TUTORIAL_V2_SAVED: eight lessons and revised diagram saved in main project.");
  }
  static void MigrateV2(UnityEngine.SceneManagement.Scene scene)
  {
   var t=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TutorialDirector>(true)).First();
   if(t.pages.Length==8)return;if(t.pages.Length!=11)throw new Exception("Expected eleven existing lessons; will not overwrite an unknown layout.");
   TutorialLayoutAuthoring.Capture(t);var old=t.pages;int[] keep={0,3,4,5,7,8,9,10};
   foreach(var p in old)if(p.diagram)Object.DestroyImmediate(p.diagram);
   t.pages=keep.Select(i=>old[i]).ToArray();t.editorPreviewPage=-1;
   string[] titles={"BASIC INTERACTION","LAUNCH BEACONS","ROBOT DRIVING","ROBOT FACING","ROBOT SHOOTING","ADVANCED ROBOT SHOOTING","PACK AND TRANSPORT","FINAL TEST"};
   string K(string s)=>"<color=#FFD479><b>"+s+"</b></color>";
   string[] keys={K("WASD")+" move   "+K("MOUSE")+" look\n"+K("E")+" carry / place\n"+K("SCROLL")+" rotate   "+K("RMB")+" cancel",K("RMB")+" launcher   "+K("SCROLL")+" select\n"+K("LMB")+" launch   "+K("E")+" recall",K("TAB")+" tablet   "+K("LMB")+" wire / CHECK\n"+K("E")+" insert / remove tablet",K("TAB")+" tablet   "+K("LMB")+" replace wire\n"+K("E")+" insert / remove tablet",K("LMB")+" machines / wire / CHECK\n"+K("TAB")+" tablet   "+K("E")+" dock",K("HOLD LMB")+" timer arrows\n"+K("TAB")+" tablet   "+K("LMB")+" wire / CHECK\n"+K("E")+" dock",K("E")+" carry / pack / enter\n"+K("SCROLL")+" rotate   "+K("LMB")+" door",K("E")+" unload / unpack / dock\n"+K("TAB")+" tablet   "+K("LMB")+" wire / CHECK\n"+K("R")+" remote   "+K("LMB")+" ENGAGE"};
   string[] guides={
    K("E")+" carry / place   "+K("SCROLL")+" rotate\n\nCarry {carry} by its round knob to the red/orange pad farther ahead. Press E to confirm placement.\n\nWASD moves; mouse looks around. No walking checkpoint is needed.",
    K("RMB")+" launcher   "+K("LMB")+" fire   "+K("E")+" recall\n\nSelect A with the wheel and hit WHITE BLOCK 1. Recall A.\n\nSelect B and hit WHITE BLOCK 2 on the higher platform.",
    K("TAB")+" tablet   "+K("LMB")+" wire / CHECK\n\nAdd DRIVING A. Connect START to DRIVING and A to PARAM. Press CHECK.\n\nThis lesson requires CHECK before docking. Re-check if you change the circuit.\n\nInsert the tablet with E; watch the robot reach A.",
    K("LMB")+" connect / replace wire   "+K("TAB")+" tablet\n\nA Driving circuit is prepared. Add FACING A between START and DRIVING.\n\nConnect START to FACING to replace the old wire, then FACING to DRIVING. A goes to both PARAM inputs.\n\nCHECK, dock, and watch it aim then drive to A.",
    K("LMB")+" add blocks / wire   "+K("TAB")+" tablet\n\nBuild START > FIRE. Set AMMO to BURST and RATE to MID.\n\nConnect both parameters to FIRE, then CHECK and dock.\n\nThe robot must fire using BURST at MID rate.",
    K("HOLD LMB")+" timer arrows   "+K("TAB")+" tablet\n\nUse the machines to add and connect: START > FACING > FIRE > WAIT > CEASE FIRE.\n\nUse POINT A, AP ammo, HIGH rate and 2 seconds.\n\nCHECK and dock. It must fire, wait 2 seconds, then stop.",
    K("E")+" pack / attach / enter   "+K("LMB")+" door\n\nPack every machine into the tray. Carry the tray to the truck bed and press E to attach.\n\nOpen the door and enter the driver's seat.",
    K("E")+" unload / unpack   "+K("R")+" remote\n\nUnload the tray and unpack all equipment. Build your circuit, CHECK, then dock.\n\nUse the transparent route to plan your defense. Press ENGAGE and defeat 5 ghosts.\n\nComplete this test to enter the main game."};
   string[][] req={new[]{"Place {carry} on the marked pad"},new[]{"Launch A onto white block 1","Recall A","Launch B onto white block 2"},new[]{"Build Driving A and CHECK","Insert tablet into robot","Robot reaches A"},new[]{"Insert FACING between START and DRIVING","CHECK the modified circuit","Insert tablet into robot","Robot aims, then reaches A"},new[]{"Connect BURST ammo","Connect MID rate","Complete circuit and CHECK","Insert tablet into robot","Robot fires BURST at MID rate"},new[]{"Connect AP ammo","Connect HIGH rate","Set WAIT to 2 seconds","Complete circuit and CHECK","Insert tablet into robot","Fire, wait, then cease fire"},new[]{"Pack all equipment into tray","Attach loaded tray to truck","Enter driver's seat"},new[]{"Unload tray and unpack all equipment","Build circuit and CHECK","ENGAGE the wave","Defeat all 5 enemies"}};
   CircuitKind[][] machineSets={t.pages[0].machines,new CircuitKind[0],new[]{CircuitKind.DriveTo},new[]{CircuitKind.Facing,CircuitKind.DriveTo},new[]{CircuitKind.Fire,CircuitKind.Ammo,CircuitKind.Rate},new[]{CircuitKind.Facing,CircuitKind.Fire,CircuitKind.Ammo,CircuitKind.Rate,CircuitKind.Wait},new CircuitKind[0],new CircuitKind[0]};
   for(int i=0;i<8;i++)
   {
    var p=t.pages[i];p.lesson=(TutorialLesson)i;p.title=titles[i];p.keys=keys[i];p.guide=guides[i];p.requirements=req[i];p.timeLimit=1200;p.machines=machineSets[i];p.layout.name=(i+1).ToString("00")+" - "+titles[i];p.layout.items=p.layout.items.Where(x=>x.target&&x.target!=t.walkGoal).ToArray();
    if(i>0)foreach(var item in p.layout.items)
    {
     var m=item.target.GetComponent<MovableMachine>();var nav=item.target.GetComponent<NavigationConsole>();
     if(m&&m!=t.rack.GetComponent<MovableMachine>())item.visible=i>=6||(m.GetComponent<RobotCircuitRunner>()?i>=2:m.GetComponent<AmmoSelectionConsole>()?p.machines.Contains(CircuitKind.Ammo):m.GetComponent<FireRateConsole>()?p.machines.Contains(CircuitKind.Rate):nav&&p.machines.Contains(nav.mode));
     if(item.target==t.rack.transform||item.target==t.truck.transform)item.visible=i>=6;
     if(item.target==t.defense.home.transform||item.target.GetComponent<GhostSpawner>())item.visible=i==7;
     if(item.target==t.placementGoal)item.visible=false;
    }
    if(i>=2&&i<=5)
    {
     var rect=Rect(t.guidePanel.transform,"Circuit diagram "+(i+1),new Vector2(345,20),new Vector2(560,490));p.diagram=rect.gameObject;
     if(i==4)Diagram(rect,9);else if(i==5)Diagram(rect,7);else if(i==2)Diagram(rect,4);else{
      string[] labels={"START","FACING","DRIVING"};for(int n=0;n<3;n++){SmallNode(rect,labels[n],-190+n*190,80,blue);if(n<2)Line(rect,new Vector2(-142+n*190,80),new Vector2(-48+n*190,80),Color.white);}
      SmallNode(rect,"POINT A",0,-100,gold);Line(rect,new Vector2(0,-71),new Vector2(0,51),gold);Line(rect,new Vector2(48,-100),new Vector2(190,-100),gold);Line(rect,new Vector2(190,-100),new Vector2(190,51),gold);Label(rect,"Replace wire","START -> FACING replaces START -> DRIVING",new Vector2(0,210),new Vector2(550,65),20);
     }
     p.diagram.SetActive(false);
    }
   }
   var goal=t.pages[0].layout.items.First(x=>x.target==t.placementGoal);goal.pose.localPosition=new Vector3(3,.015f,8);goal.visible=true;
   if(t.walkGoal){Object.DestroyImmediate(t.walkGoal.gameObject);t.walkGoal=null;}
   foreach(int i in new[]{1,2,6})if(old[i].layout)Object.DestroyImmediate(old[i].layout.gameObject);
   // The Facing lesson now needs both consoles, without moving the user's robot or player.
   var faceLayout=t.pages[3].layout;var consoleItems=faceLayout.items.Where(x=>x.visible&&x.target.GetComponent<NavigationConsole>()).ToArray();for(int i=0;i<consoleItems.Length;i++)consoleItems[i].pose.localPosition=new Vector3(-2+i*4,.05f,0);
   var beacon=t.pages[1].layout;var decor=new GameObject("Beacon terrain and numbered targets");decor.transform.SetParent(beacon.transform,false);beacon.decorations=decor;
   var stone=Mat("Beacon terrain grey",new Color(.38f,.43f,.46f));var white=Mat("Beacon target white",new Color(.92f,.91f,.85f));
   GameObject LocalBox(string name,Vector3 pos,Vector3 size,Material mat){var g=Box(name,Vector3.zero,size,mat);g.transform.SetParent(decor.transform,false);g.transform.localPosition=pos;return g;}
   LocalBox("Low terrain terrace",new Vector3(-4,.4f,4),new Vector3(5,.8f,5),stone);LocalBox("Raised terrain terrace",new Vector3(4,1.1f,9),new Vector3(5,2.2f,5),stone);
   beacon.beaconTargetOne=LocalBox("White block 1",new Vector3(-4,1.3f,4),new Vector3(2.4f,1,2.4f),white).transform;beacon.beaconTargetTwo=LocalBox("White block 2",new Vector3(4,2.7f,9),new Vector3(2.4f,1,2.4f),white).transform;
   foreach(var pair in new[]{(beacon.beaconTargetOne,"1"),(beacon.beaconTargetTwo,"2")}){var label=new GameObject("Target number").AddComponent<TextMesh>();label.transform.SetParent(pair.Item1,false);label.transform.localPosition=new Vector3(0,.1f,-.51f);label.transform.localScale=new Vector3(1/pair.Item1.localScale.x,1/pair.Item1.localScale.y,1/pair.Item1.localScale.z);label.font=ProjectTypography.Font;label.text=pair.Item2;label.characterSize=.5f;label.fontSize=64;label.anchor=TextAnchor.MiddleCenter;label.color=Color.black;label.GetComponent<Renderer>().sharedMaterial=ProjectTypography.Font.material;}
   t.heading.fontSize=46;t.heading.rectTransform.sizeDelta=new Vector2(1350,90);t.footer.text="F1  REOPEN GUIDE";t.footer.rectTransform.sizeDelta=new Vector2(1340,40);t.footer.rectTransform.anchoredPosition=new Vector2(0,-350);
   var canvas=t.guidePanel.transform.parent;t.keyHints=Label(canvas,"Lesson key hints","",Vector2.zero,new Vector2(470,180),21);t.keyHints.supportRichText=true;t.keyHints.alignment=TextAnchor.UpperLeft;t.keyHints.lineSpacing=1.2f;
   void Corner(Text text,bool right,Vector2 size){var r=text.rectTransform;r.anchorMin=r.anchorMax=new Vector2(right?1:0,1);r.pivot=new Vector2(right?1:0,1);r.anchoredPosition=new Vector2(right?-28:28,-28);r.sizeDelta=size;text.alignment=TextAnchor.UpperLeft;text.fontSize=21;text.supportRichText=true;text.lineSpacing=1.25f;if(!text.GetComponent<Shadow>()){var s=text.gameObject.AddComponent<Shadow>();s.effectColor=new Color(0,0,0,.8f);s.effectDistance=new Vector2(1,-1);}}
   Corner(t.keyHints,false,new Vector2(470,180));Corner(t.objective,true,new Vector2(500,270));t.defense.totalEnemies=5;
   TutorialLayoutAuthoring.Preview(t,0,false);t.footer.text="F1  REOPEN GUIDE";t.keyHints.text=t.pages[0].keys;t.objective.text="REQUIREMENTS\nTODO  Place "+(t.CarryTarget?t.CarryTarget.displayName:"machine")+" on the marked pad";
  }
  static void RemoveUnlink(GameObject root)
  {
   foreach(var board in root.GetComponentsInChildren<CircuitBoardView>(true)){board.unlinkButton=null;EditorUtility.SetDirty(board);}
   foreach(var tr in root.GetComponentsInChildren<Transform>(true))if(tr&&tr.name.StartsWith("UNLINK",StringComparison.OrdinalIgnoreCase))
   {
    if(PrefabUtility.IsPartOfPrefabInstance(tr)&&!PrefabUtility.IsAddedGameObjectOverride(tr.gameObject)){tr.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(tr.gameObject);}else Object.DestroyImmediate(tr.gameObject);
   }
  }
  static void AuthorRoutes(GameObject root)
  {
   const string path="Assets/Mach/Lighting/Enemy route.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){material=new Material(Shader.Find("Mach/EnemyRoute"));AssetDatabase.CreateAsset(material,path);}
   foreach(var spawner in root.GetComponentsInChildren<GhostSpawner>(true))
   {
    var preview=spawner.GetComponentInChildren<EnemyRoutePreview>(true);if(!preview){var go=new GameObject("Enemy navigation route");go.transform.SetParent(spawner.transform,false);preview=go.AddComponent<EnemyRoutePreview>();}
    preview.spawner=spawner;preview.line=preview.GetComponent<LineRenderer>();var line=preview.line;line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=0;line.widthMultiplier=.65f;line.numCornerVertices=4;line.numCapVertices=4;line.alignment=LineAlignment.TransformZ;line.transform.rotation=Quaternion.Euler(90,0,0);line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;EditorUtility.SetDirty(preview);
   }
  }
 }
}
