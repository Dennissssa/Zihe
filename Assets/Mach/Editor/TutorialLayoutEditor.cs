using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class TutorialLayoutAuthoring
 {
  static TutorialLayoutAuthoring(){EditorApplication.update+=Poll;EditorSceneManager.sceneSaving+=OnSceneSaving;}
  static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene,string path)
  {
   if(Application.isPlaying||path!=TutorialAuthoring.Path)return;
   var t=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TutorialDirector>(true)).FirstOrDefault();
   if(t)Capture(t);
  }
  static void Poll()
  {
   if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/TutorialLayout.request"))return;
   File.Delete("Temp/TutorialLayout.request");var active=EditorSceneManager.GetActiveScene();var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(TutorialAuthoring.Path);bool opened=!scene.isLoaded;
   try{if(opened)scene=EditorSceneManager.OpenScene(TutorialAuthoring.Path,OpenSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);Build();File.WriteAllText("Logs/tutorial-layout-applied.txt","Eleven editable layouts and compact fenced yards saved.");}
   catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/tutorial-layout-error.txt",e.ToString());}
   finally{if(active.IsValid()&&active.isLoaded)EditorSceneManager.SetActiveScene(active);if(opened&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);}
  }
  public static TutorialDirector Director()=>Object.FindAnyObjectByType<TutorialDirector>();
  public static void Build()
  {
   var t=Director();if(!t||t.pages.Any(p=>p.layout))throw new InvalidOperationException("Layout migration is one-time only; existing layouts are preserved.");
   var root=new GameObject("Tutorial Lesson Layouts");var originalCenter=t.zoneCenter;
   var spawners=t.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GhostSpawner>(true)).ToArray();
   for(int i=0;i<t.pages.Length;i++)
   {
    var page=t.pages[i];var go=new GameObject((i+1).ToString("00")+" - "+page.title);go.transform.SetParent(root.transform);go.transform.position=new Vector3(500+(i%4)*26,0,(i/4)*42);
    var layout=go.AddComponent<TutorialStageLayout>();layout.director=t;page.layout=layout;page.areaCenter=go.transform.position;var list=new List<TutorialLayoutItem>();
    void Pose(Transform target,string name,Vector3 pos,bool visible){var anchor=new GameObject(name).transform;anchor.SetParent(go.transform,false);anchor.localPosition=pos;anchor.rotation=target.rotation;list.Add(new TutorialLayoutItem{target=target,pose=anchor,visible=visible});}
    Pose(t.program.transform,"Player spawn",new Vector3(i==0?-3:0,.1f,i<3?-3:-5),true);
    Pose(t.rack.transform,"Equipment tray",new Vector3(-5,.06f,-4),i==9);
    Pose(t.truck.transform,"Truck spawn",new Vector3(5,.1f,-4),i==9);
    var visibleMachines=t.rack.slots.Select(s=>s.machine).Where(m=>i==9||(m.GetComponent<RobotCircuitRunner>()?page.showRobot:m.GetComponent<AmmoSelectionConsole>()?page.machines.Contains(CircuitKind.Ammo):m.GetComponent<FireRateConsole>()?page.machines.Contains(CircuitKind.Rate):m.GetComponent<NavigationConsole>()&&page.machines.Contains(m.GetComponent<NavigationConsole>().mode))).ToArray();
    var consoles=visibleMachines.Where(m=>!m.GetComponent<RobotCircuitRunner>()).ToArray();
    foreach(var slot in t.rack.slots)
    {
     var m=slot.machine;int index=Array.IndexOf(consoles,m);bool robot=m.GetComponent<RobotCircuitRunner>();float x=index<0?0:(index-(consoles.Length-1)*.5f)*2.8f;Vector3 pos=robot?new Vector3(0,.1f,6):new Vector3(x,m.transform.position.y-originalCenter.y,i==9?-11:0);
     if(i==0)pos.x=-3;
     Pose(m.transform,"Machine - "+m.displayName,pos,visibleMachines.Contains(m));
    }
    Pose(t.walkGoal,"Walk goal",new Vector3(-3,.015f,-1),i==0);Pose(t.placementGoal,"Placement goal",new Vector3(1,.015f,-1),i==0);
    Pose(t.defense.home.transform,"Defense base",new Vector3(0,.1f,-12),i==10);
    foreach(var spawner in spawners)Pose(spawner.transform,"Enemy spawner",new Vector3(0,.1f,16),i==10);
    layout.items=list.ToArray();
    Transform Point(string name,Vector3 at){var p=new GameObject(name).transform;p.SetParent(go.transform,false);p.localPosition=at;return p;}
    layout.pointA=Point("Point A target",new Vector3(0,.01f,12));layout.pointB=Point("Point B target",new Vector3(5,.01f,14));
    RebuildFence(layout);
   }
   var old=GameObject.Find("Tutorial area dividers");if(old)Object.DestroyImmediate(old);
   foreach(var obj in t.gameObject.scene.GetRootGameObjects())if(obj.GetComponent<TextMesh>()&&obj.name.EndsWith(" label")&&char.IsDigit(obj.name[0]))Object.DestroyImmediate(obj);
   var floor=GameObject.Find("Tutorial training ground");floor.transform.position=new Vector3(539,-.3f,42);floor.transform.localScale=new Vector3(104,.6f,128);
   if(t.defense.surface){t.defense.surface.center=t.defense.surface.transform.InverseTransformPoint(new Vector3(539,0,42));t.defense.surface.size=new Vector3(104,12,128);}
   t.editorPreviewPage=-1;Preview(t,0,false);EditorSceneManager.SaveScene(t.gameObject.scene);AssetDatabase.SaveAssets();
  }
  public static void RebuildFence(TutorialStageLayout layout)
  {
   if(layout.fences)Undo.DestroyObjectImmediate(layout.fences.gameObject);
   var parent=new GameObject("Perimeter fence - editable").transform;parent.SetParent(layout.transform,false);layout.fences=parent;
   string path="Assets/Mach/Tutorial/Fence graphite.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){Directory.CreateDirectory("Assets/Mach/Tutorial");mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",new Color(.24f,.28f,.3f));AssetDatabase.CreateAsset(mat,path);}
   void Box(string name,Vector3 pos,Vector3 size){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;}
   float x=layout.areaSize.x*.5f,z=layout.areaSize.y*.5f;
   foreach(float side in new[]{-1f,1f})
   {
    Box("Side barrier",new Vector3(side*x,.4f,0),new Vector3(.16f,.8f,z*2));Box("Side handrail",new Vector3(side*x,1.35f,0),new Vector3(.18f,.12f,z*2));
    Box("End barrier",new Vector3(0,.4f,side*z),new Vector3(x*2,.8f,.16f));Box("End handrail",new Vector3(0,1.35f,side*z),new Vector3(x*2,.12f,.18f));
    for(float p=-z;p<=z+.01f;p+=z/5)Box("Side post",new Vector3(side*x,.7f,p),new Vector3(.22f,1.4f,.22f));
    for(float p=-x;p<=x+.01f;p+=x/3)Box("End post",new Vector3(p,.7f,side*z),new Vector3(.22f,1.4f,.22f));
   }
   EditorUtility.SetDirty(layout);EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);
  }
  public static void Capture(TutorialDirector t)
  {
   if(t.editorPreviewPage<0||t.editorPreviewPage>=t.pages.Length)return;var layout=t.pages[t.editorPreviewPage].layout;if(!layout)return;
   foreach(var item in layout.items)if(item.target&&item.pose){Undo.RecordObject(item.pose,"Save lesson placement");item.pose.SetPositionAndRotation(item.target.position,item.target.rotation);}
   EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
  }
  public static void SetCarryTarget(TutorialStageLayout layout,MovableMachine target,MovableMachine previous)
  {
   Undo.RecordObject(layout,"Change carry lesson target");
   if(target&&target.gameObject.scene!=layout.gameObject.scene){Debug.LogWarning("Drag a machine instance from the Tutorial scene.");layout.carryTarget=null;return;}
   layout.carryTarget=target;
   if(target)
   {
    var slot=layout.items.FirstOrDefault(i=>previous&&i.visible&&i.target==previous.transform)??layout.items.FirstOrDefault(i=>i.target==target.transform);
    if(slot!=null){slot.target=target.transform;slot.visible=true;}
    else
    {
     var pose=new GameObject("Carry target spawn").transform;Undo.RegisterCreatedObjectUndo(pose.gameObject,"Add carry pose");pose.SetParent(layout.transform);pose.SetPositionAndRotation(target.transform.position,target.transform.rotation);
     layout.items=layout.items.Concat(new[]{new TutorialLayoutItem{target=target.transform,pose=pose,visible=true}}).ToArray();
    }
    // Keep the former machine out of this lesson without changing any other lesson's layout.
    if(previous&&previous!=target){foreach(var item in layout.items)if(item.target==previous.transform)item.visible=false;}
    if(layout.director.editorPreviewPage>=0&&layout.director.pages[layout.director.editorPreviewPage].layout==layout)
    {
     if(previous&&previous!=target)previous.gameObject.SetActive(false);
     layout.Apply();
    }
   }
   EditorUtility.SetDirty(layout);EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);
  }
  public static void Preview(TutorialDirector t,int index,bool capture=true)
  {
   if(Application.isPlaying)return;if(capture&&t.editorPreviewPage!=index)Capture(t);var layout=t.pages[index].layout;if(!layout)return;
   var objects=layout.items.Where(i=>i.target).SelectMany(i=>new Object[]{i.target,i.target.gameObject}).Concat(new Object[]{t}).ToArray();Undo.RecordObjects(objects,"Preview tutorial layout");
   layout.Apply();t.editorPreviewPage=index;
   // Keep the guide authored, but hide it to make Scene/Game layout inspection clear.
   t.guidePanel.SetActive(false);t.heading.text=(index+1)+" / "+t.pages.Length+"  "+t.pages[index].title;t.body.text=t.GuideText(index);
   foreach(var p in t.pages)if(p.diagram)p.diagram.SetActive(false);
   EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
  }
  public static void EditGuide(TutorialDirector t,int index)
  {
   if(Application.isPlaying||index<0||index>=t.pages.Length)return;
   var page=t.pages[index];
   // Preview only UI: do not apply a lesson layout or move the user's scene objects.
   var objects=t.guidePanel.GetComponentsInChildren<Transform>(true).Select(x=>(Object)x.gameObject)
    .Concat(new Object[]{t.heading,t.body,t.footer,t.body.rectTransform}).ToArray();
   Undo.RecordObjects(objects,"Preview tutorial guide");
   for(var parent=t.guidePanel.transform;parent;parent=parent.parent){Undo.RecordObject(parent.gameObject,"Show guide parent");parent.gameObject.SetActive(true);}
   if(t.keyHints){Undo.RecordObject(t.keyHints.gameObject,"Hide lesson HUD");t.keyHints.gameObject.SetActive(false);}
   if(t.objective){Undo.RecordObject(t.objective.gameObject,"Hide lesson HUD");t.objective.gameObject.SetActive(false);}
   t.heading.text=(index+1).ToString("00")+" / "+t.pages.Length+"   "+page.title;
   t.body.text=t.GuideText(index);t.footer.text="F1  REOPEN GUIDE";
   t.body.rectTransform.anchoredPosition=page.diagram?t.diagramTextPosition:t.plainTextPosition;
   t.body.rectTransform.sizeDelta=page.diagram?t.diagramTextSize:t.plainTextSize;
   foreach(var p in t.pages)if(p.diagram)p.diagram.SetActive(p==page);
   var target=page.diagram?page.diagram:t.guidePanel;
   SceneVisibilityManager.instance.Show(t.guidePanel,true);
   SceneVisibilityManager.instance.EnablePicking(t.guidePanel,true);
   Canvas.ForceUpdateCanvases();Selection.activeGameObject=target;Tools.current=Tool.Rect;
   var view=SceneView.lastActiveSceneView??EditorWindow.GetWindow<SceneView>();view.in2DMode=true;view.FrameSelected();view.Focus();
   EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
  }
 }
 public sealed class TutorialLayoutWindow:EditorWindow
 {
  Vector2 scroll;
  [MenuItem("Mach/Tutorial/Edit lesson layouts")]
  public static void Open()=>GetWindow<TutorialLayoutWindow>("Lesson Layouts");
  void OnGUI()
  {
   var t=TutorialLayoutAuthoring.Director();if(!t){EditorGUILayout.HelpBox("Open Tutorial.unity first.",MessageType.Info);return;}
   EditorGUILayout.HelpBox("选择一课 Edit。直接在 Scene 中移动玩家、机器和车。Ctrl+S 或 Save Layout 保存该课；切换课程前也会自动记住位置。请退出 Play Mode 后编辑。",MessageType.Info);
   using(new EditorGUI.DisabledScope(Application.isPlaying))
   {
    if(GUILayout.Button("Save Layout / 保存当前课摆设")){TutorialLayoutAuthoring.Capture(t);EditorSceneManager.SaveScene(t.gameObject.scene);}
    scroll=EditorGUILayout.BeginScrollView(scroll);
    for(int i=0;i<t.pages.Length;i++)
    {
     var page=t.pages[i];EditorGUILayout.BeginVertical("box");EditorGUILayout.LabelField((i+1).ToString("00")+"  "+page.title+(t.editorPreviewPage==i?"  [EDITING]":""));
     if(i==0&&page.layout)
     {
      var previous=page.layout.ResolveCarryTarget();var chosen=(MovableMachine)EditorGUILayout.ObjectField("Carry Target (auto if empty)",page.layout.carryTarget,typeof(MovableMachine),true);
      if(chosen!=page.layout.carryTarget)TutorialLayoutAuthoring.SetCarryTarget(page.layout,chosen,previous);
      var resolved=page.layout.ResolveCarryTarget();EditorGUILayout.LabelField("Detection target",resolved?resolved.displayName:"Choose a machine; multiple or no visible targets.");
     }
     EditorGUILayout.BeginHorizontal();if(GUILayout.Button("Edit / 摆设")){TutorialLayoutAuthoring.Preview(t,i);Selection.activeGameObject=page.layout.gameObject;SceneView.lastActiveSceneView?.Frame(new Bounds(page.layout.transform.position,new Vector3(page.layout.areaSize.x,8,page.layout.areaSize.y)),false);}
     if(GUILayout.Button("Play this lesson")){TutorialLayoutAuthoring.Capture(t);TutorialLayoutAuthoring.Preview(t,i,false);EditorSceneManager.SaveScene(t.gameObject.scene);SessionState.SetInt("Mach.Tutorial.TestPage",i);EditorApplication.EnterPlaymode();}
     EditorGUILayout.EndHorizontal();
     if(GUILayout.Button(page.diagram?"Edit Guide / Diagram · 显示并编辑线路图":"Edit Guide · 显示并编辑教程页面"))TutorialLayoutAuthoring.EditGuide(t,i);
     EditorGUILayout.EndVertical();
    }
    EditorGUILayout.EndScrollView();
   }
  }
 }
 [CustomEditor(typeof(TutorialStageLayout))] public sealed class TutorialStageLayoutInspector:UnityEditor.Editor
 {
  public override void OnInspectorGUI()
  {
   var layout=(TutorialStageLayout)target;var previous=layout.ResolveCarryTarget();var oldOverride=layout.carryTarget;DrawDefaultInspector();
   if(!Application.isPlaying&&layout.carryTarget!=oldOverride)TutorialLayoutAuthoring.SetCarryTarget(layout,layout.carryTarget,previous);
   if(layout.director&&layout.director.pages.Length>0&&layout.director.pages[0].layout==layout){var resolved=layout.ResolveCarryTarget();EditorGUILayout.HelpBox(resolved?"搬运检测目标："+resolved.displayName+"。Guide 中的 {carry} 会显示该名称。":"请指定 Carry Target，或在 Items 中只保留一台 Visible 的可搬运机器。",resolved?MessageType.Info:MessageType.Warning);}
   using(new EditorGUI.DisabledScope(Application.isPlaying))
   {
    if(GUILayout.Button("Edit this layout / 显示该课设备")){var i=Array.FindIndex(layout.director.pages,p=>p.layout==layout);if(i>=0)TutorialLayoutAuthoring.Preview(layout.director,i);}
    if(GUILayout.Button("Save current object positions / 保存设备摆设")){if(layout.director.editorPreviewPage>=0&&layout.director.pages[layout.director.editorPreviewPage].layout==layout){TutorialLayoutAuthoring.Capture(layout.director);EditorSceneManager.SaveScene(layout.gameObject.scene);}}
    if(GUILayout.Button("Rebuild fence for Area Size / 按区域尺寸更新围栏"))TutorialLayoutAuthoring.RebuildFence(layout);
   }
  }
  void OnSceneGUI()
  {
   var layout=(TutorialStageLayout)target;if(layout.items==null)return;
   foreach(var item in layout.items)if(item.pose&&item.visible){Handles.Label(item.pose.position+Vector3.up*.5f,item.pose.name);}
   if(layout.pointA)Handles.Label(layout.pointA.position,"Point A");if(layout.pointB)Handles.Label(layout.pointB.position,"Point B");
  }
 }
}


