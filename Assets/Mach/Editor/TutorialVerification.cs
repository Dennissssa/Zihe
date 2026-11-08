using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class TutorialVerification
 {
  static int phase;static TutorialDirector previous;static double deadline;
  static TutorialVerification(){EditorApplication.update+=Poll;}
  public static void Verify()
  {
   EditorSceneManager.OpenScene(TutorialAuthoring.Path);var t=UnityEngine.Object.FindAnyObjectByType<TutorialDirector>();
   if(!t||t.pages.Length!=11||t.inputScripts.Any(s=>!s)||t.rack.slots.Any(s=>s.Packed||!s.machine.gameObject.activeInHierarchy))throw new Exception("Tutorial wiring or editable setup missing");
   if(UnityEngine.Object.FindAnyObjectByType<EquipmentAirdrop>())throw new Exception("Air drop gate still blocks tutorial carrying");
   foreach(var page in t.pages)if(string.IsNullOrEmpty(page.guide)||page.timeLimit<60)throw new Exception("Lesson guide missing");
   t.defense.surface.BuildNavMesh();var spawner=UnityEngine.Object.FindAnyObjectByType<GhostSpawner>();if(!spawner.TryFindSpawn(out _))throw new Exception("No tutorial enemy path");
   foreach(int stage in new[]{0,7})
   {
    t.body.rectTransform.anchoredPosition=new Vector2(t.pages[stage].diagram?-340:0,10);t.body.rectTransform.sizeDelta=new Vector2(t.pages[stage].diagram?620:1150,530);
    t.heading.text=(stage+1)+" / "+t.pages.Length+"  "+t.pages[stage].title;t.body.text=t.pages[stage].guide;t.footer.text="ENTER / CLICK TO CONTINUE    |    F1 REOPEN GUIDE\nFailure: leaving the zone, robot error, 3 failed checks, or time limit. Current stage restarts.";
    foreach(var p in t.pages)if(p.diagram)p.diagram.SetActive(p==t.pages[stage]);Canvas.ForceUpdateCanvases();
    if(t.body.preferredHeight>t.body.rectTransform.rect.height)throw new Exception("Lesson text overflows: "+stage);
    var canvas=t.guidePanel.GetComponentInParent<Canvas>();var go=new GameObject("Preview camera");var camera=go.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.18f,.22f);camera.cullingMask=1<<29;foreach(var tr in canvas.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=29;
    canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;var rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes("Logs/tutorial-page-"+stage+".png",image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
   }
   Debug.Log("TUTORIAL_STATIC_VERIFIED: eleven authored lessons, diagrams, editable props, readable guide bounds, enemy navmesh route");EditorSceneManager.OpenScene(TutorialAuthoring.Path);
  }
  public static void PlaySmoke()
  {
   EditorSceneManager.OpenScene(TutorialAuthoring.Path);SessionState.SetBool("TutorialSmoke",true);SessionState.SetInt("TutorialSmokePhase",0);EditorApplication.EnterPlaymode();
  }
  static void Poll()
  {
   if(!SessionState.GetBool("TutorialSmoke",false)||!EditorApplication.isPlaying)return;
   if(deadline==0)deadline=EditorApplication.timeSinceStartup+90;
   try
   {
    if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Tutorial runtime timeout phase "+phase);
    var t=UnityEngine.Object.FindAnyObjectByType<TutorialDirector>();if(!t)return;phase=SessionState.GetInt("TutorialSmokePhase",0);
    if(phase==0&&t.GuideVisible&&Time.timeScale==0)
    {
     CarryTargetRegression(t);FirstLessonRegression(t);
     if(t.Stage!=0||t.inputScripts.Any(s=>s.enabled))throw new Exception("Guide does not block world input");
     t.BeginPractice();if(Time.timeScale!=1||!t.program.GetComponent<FirstPersonWalker>().enabled)throw new Exception("Continue did not restore input");
     var cc=t.program.GetComponent<CharacterController>();cc.enabled=false;t.program.transform.position=t.walkGoal.position+Vector3.up*.1f;cc.enabled=true;SessionState.SetInt("TutorialSmokePhase",10);
    }
    else if(phase==10&&t.Stage==0&&t.milestones[0])
    {t.placement.enabled=false;if(!t.placement.Begin(t.CarryTarget))throw new Exception("Carry did not start");SessionState.SetInt("TutorialSmokePhase",11);}
    else if(phase==11&&t.milestones[1])
    {t.placement.enabled=false;if(!t.placement.PreviewAt(t.placementGoal.position+Vector3.up*.013f,true)||!t.placement.Confirm())throw new Exception("Carry target rejected: "+t.placement.PlacementError);t.placement.enabled=true;SessionState.SetInt("TutorialSmokePhase",1);}
    else if(phase==1&&t.Stage==1&&t.GuideVisible&&Time.timeScale==0)
    {previous=t;t.BeginPractice();SessionState.SetInt("TutorialSmokePhase",2);t.Retry("Automated stage restart test");}
    else if(phase==2&&t.Stage==1&&!ReferenceEquals(t,previous)&&t.GuideVisible&&Time.timeScale==0)
    {
     if(!t.body.text.Contains("Automated stage restart test"))throw new Exception("Retry reason missing");
     Debug.Log("TUTORIAL_CHECKPOINTS_VERIFIED: guide pauses, input restored, movement advances, failure reloads current stage");
     typeof(TutorialDirector).GetField("resumeStage",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).SetValue(null,7);SessionState.SetInt("TutorialSmokePhase",12);Time.timeScale=1;UnityEngine.SceneManagement.SceneManager.LoadScene(TutorialAuthoring.Path);
    }
    else if(phase==12&&t.Stage==7&&t.GuideVisible)
    {
     if(!t.pages[7].diagram||t.pages[0].diagram||t.pages[1].diagram||t.pages[2].diagram||t.pages[3].diagram||t.pages[9].diagram)throw new Exception("Diagram visibility contract failed");
     if(Mathf.Abs(t.program.transform.position.x-t.zoneCenter.x)>1||Vector3.Distance(t.zoneCenter,t.pages[7].layout?t.pages[7].layout.transform.position:t.pages[7].areaCenter)>.01f)throw new Exception("Player not in firing bay");
     t.BeginPractice();var p=t.program;int fire=p.Add(CircuitKind.Fire),ammo=p.Add(CircuitKind.Ammo,"AP"),rate=p.Add(CircuitKind.Rate,"2");p.Connect(0,fire,false);p.Connect(ammo,fire,true);if(p.Validate(out _))throw new Exception("One parameter accepted");p.Connect(rate,fire,true);if(!p.Validate(out var message))throw new Exception(message);
     typeof(TutorialDirector).GetMethod("OnCheck",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(t,new object[]{true,message});p.ToggleDock(t.robot);SessionState.SetInt("TutorialSmokePhase",13);
    }
    else if(phase==13&&t.milestones[0]){t.program.ToggleDock(t.robot);SessionState.SetInt("TutorialSmokePhase",14);}
    else if(phase==14&&t.Stage==8&&t.GuideVisible)
    {
     Debug.Log("TUTORIAL_DUAL_PARAM_VERIFIED: one parameter rejected; Ammo+Rate on same Fire accepted; firing and undocking advances");typeof(TutorialDirector).GetField("resumeStage",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).SetValue(null,10);SessionState.SetInt("TutorialSmokePhase",3);Time.timeScale=1;UnityEngine.SceneManagement.SceneManager.LoadScene(TutorialAuthoring.Path);
    }
    else if(phase==3&&t.Stage==10&&t.GuideVisible&&t.defense.Ready)
    {
     t.BeginPractice();var p=t.program;p.ClearCircuit();int face=p.Add(CircuitKind.Facing),fire=p.Add(CircuitKind.Fire),point=p.Add(CircuitKind.Point,"A"),ammo=p.Add(CircuitKind.Ammo,"AP"),rate=p.Add(CircuitKind.Rate,"2");p.nodes[point].pointId="A";
     p.Connect(0,face,false);p.Connect(face,fire,false);p.Connect(point,face,true);p.Connect(ammo,fire,true);p.Connect(rate,fire,true);p.ToggleDock(t.robot);t.defense.BeginWave();SessionState.SetInt("TutorialSmokePhase",4);
    }
    else if(phase==4)
    {
     if(t.defense.home.Destroyed)throw new Exception("Tutorial battle lost with correct circuit");
     if(t.defense.Won){Debug.Log("TUTORIAL_PLAY_VERIFIED: checkpoint progression/retry and final three-enemy battle won");SessionState.SetBool("TutorialSmoke",false);EditorApplication.Exit(0);}
    }
   }
   catch(Exception e){Debug.LogException(e);SessionState.SetBool("TutorialSmoke",false);EditorApplication.Exit(1);}
  }
  static void FirstLessonRegression(TutorialDirector t)
  {
   var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
   var check=typeof(TutorialDirector).GetMethod("CheckFirstLesson",flags);var placed=typeof(TutorialDirector).GetMethod("OnPlaced",flags);
   var machine=t.CarryTarget;var start=machine.transform.position;var playerStart=t.program.transform.position;var scale=t.placementGoal.localScale;
   var cc=t.program.GetComponent<CharacterController>();cc.enabled=false;
   try
   {
    t.placementGoal.localScale=new Vector3(scale.x*2,scale.y,scale.z);Physics.SyncTransforms();var pad=t.placementGoal.GetComponent<Collider>().bounds;
    machine.transform.position=new Vector3(pad.max.x-.1f,pad.max.y,pad.center.z);
    machine.transform.position+=Vector3.up*(-machine.transform.TransformVector(machine.boundsCenter).y+machine.boundsSize.y*machine.transform.lossyScale.y*.5f);
    t.program.transform.position=t.walkGoal.position+Vector3.back*4;Physics.SyncTransforms();
    bool Check(){var args=new object[]{t.program.transform.position,""};return (bool)check.Invoke(t,args);}
    if(Check()||t.milestones[2])throw new Exception("Unconfirmed placement counted");
    placed.Invoke(t,new object[]{machine});if(!t.milestones[2]||Check())throw new Exception("Machine-first placement not remembered");
    var blue=t.walkGoal.GetComponent<Collider>().bounds;t.program.transform.position=new Vector3(blue.max.x-.05f,blue.max.y,blue.max.z-.05f);Physics.SyncTransforms();
    if(!Check())throw new Exception("Walking after placement did not complete");
    Debug.Log("FIRST_LESSON_REGRESSION_VERIFIED: enlarged pad edge, confirmation required, machine-first order");
   }
   finally{machine.transform.position=start;t.program.transform.position=playerStart;t.placementGoal.localScale=scale;t.milestones=new bool[5];cc.enabled=true;Physics.SyncTransforms();}
  }
  static void CarryTargetRegression(TutorialDirector t)
  {
   var layout=t.pages[0].layout;var original=layout.ResolveCarryTarget();
   var wait=t.rack.slots.Select(s=>s.machine).First(m=>m.GetComponent<NavigationConsole>()&&m.GetComponent<NavigationConsole>().mode==CircuitKind.Wait);
   layout.carryTarget=null;var entry=layout.items.First(i=>i.visible&&i.target==original.transform);entry.target=wait.transform;layout.Apply();
   if(t.CarryTarget!=wait||!wait.gameObject.activeInHierarchy)throw new Exception("Replacing visible item did not select WAIT");
   var posted=typeof(TutorialDirector).GetMethod("OnPlaced",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);posted.Invoke(t,new object[]{t.ammo.GetComponent<MovableMachine>()});
   if(t.milestones[2])throw new Exception("Wrong machine completed carry lesson");
   if(!t.GuideText(0).Contains(wait.displayName))throw new Exception("Guide still names old machine");
   layout.carryTarget=t.ammo.GetComponent<MovableMachine>();if(t.CarryTarget!=layout.carryTarget)throw new Exception("Explicit carry target ignored");
   layout.carryTarget=null;
   Debug.Log("CARRY_TARGET_VERIFIED: auto WAIT replacement, explicit override, wrong machine ignored, dynamic guide");
  }
 }
}










