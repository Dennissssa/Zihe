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
 [InitializeOnLoad] public static class TutorialV2Verification
 {
  static readonly bool[] visited=new bool[8];static double deadline;static int beaconPhase;static bool finalStarted;
  static TutorialV2Verification(){EditorApplication.update+=Poll;}
  public static void Run(){EditorSceneManager.OpenScene(TutorialAuthoring.Path);SessionState.SetBool("TutorialV2Test",true);EditorApplication.EnterPlaymode();}
  public static void VerifyGuideEditing()
  {
   EditorSceneManager.OpenScene(TutorialAuthoring.Path);var t=TutorialLayoutAuthoring.Director();
   var positions=t.rack.slots.Select(s=>s.machine.transform.position).ToArray();
   for(int i=0;i<t.pages.Length;i++)
   {
    TutorialLayoutAuthoring.EditGuide(t,i);
    Need(t.guidePanel.activeInHierarchy,"Guide parent still hidden");
    foreach(var p in t.pages)if(p.diagram)Need(p.diagram.activeInHierarchy==(p==t.pages[i]),"Wrong diagram visibility");
    Need(Selection.activeGameObject==(t.pages[i].diagram?t.pages[i].diagram:t.guidePanel),"Diagram not selected");
    Need(positions.SequenceEqual(t.rack.slots.Select(s=>s.machine.transform.position)),"Guide preview moved machines");
   }
   Debug.Log("GUIDE_EDIT_VERIFIED: all pages visible, exclusive diagram selection, machine positions preserved.");
  }
  static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
  static void Dock(TutorialDirector t){Need(t.program.board.CheckCircuit(),"Circuit validation failed");t.program.ToggleDock(t.robot);Need(t.program.Docked,"Dock rejected: "+t.program.Status);}
  static void FireParameters(BehaviorProgram p,int fire,string ammo,string rate){int a=p.Add(CircuitKind.Ammo,ammo),r=p.Add(CircuitKind.Rate,rate);p.Connect(a,fire,true);p.Connect(r,fire,true);}
  static int Point(BehaviorProgram p){int n=p.Add(CircuitKind.Point,"A");p.nodes[n].pointId="A";return n;}
  static void SnapPlayer(TutorialDirector t,Vector3 at){var c=t.program.GetComponent<CharacterController>();c.enabled=false;t.program.transform.position=at;c.enabled=true;Physics.SyncTransforms();}
  static void PutBeacon(TutorialDirector t,int index,Transform target){var p=t.program.pointMarkers[index];Need(p.Launch(target.position+Vector3.up*3,Vector3.down,t.program.transform),"Beacon could not launch");var box=target.GetComponent<Collider>().bounds;p.Attach(new Vector3(box.center.x,box.max.y,box.center.z),Vector3.up,target);p.Advance(1);}
  static void Poll()
  {
   if(!SessionState.GetBool("TutorialV2Test",false)||!EditorApplication.isPlaying)return;
   if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;
   try
   {
    if(SceneManager.GetActiveScene().path=="Assets/Mach/Scenes/WalkingPickup.unity"){Need(visited.All(v=>v),"Main scene loaded early");Debug.Log("TUTORIAL_V2_PLAY_VERIFIED: all eight lessons, dock gate, parameters, timer, transport, five enemies, main scene transition");SessionState.SetBool("TutorialV2Test",false);EditorApplication.Exit(0);return;}
    var t=UnityEngine.Object.FindAnyObjectByType<TutorialDirector>();if(!t)return;Need(EditorApplication.timeSinceStartup<deadline,"Timeout at lesson "+t.Stage+" / "+string.Join(",",t.milestones)+" / "+t.program.Status);
    if(t.GuideVisible)
    {
     Need(!visited[t.Stage],"Lesson unexpectedly restarted: "+t.Stage+" / "+t.body.text);visited[t.Stage]=true;
     int[] counts={1,3,3,4,5,6,3,4};Need(t.pages.Length==8&&t.pages[t.Stage].requirements.Length==counts[t.Stage],"Wrong requirements count");Need(t.footer.text=="F1  REOPEN GUIDE"&&t.heading.fontSize>=46,"Guide styling not applied");Need(!t.program.board.unlinkButton,"UNLINK still assigned");
     Canvas.ForceUpdateCanvases();Need(t.body.preferredHeight<=t.body.rectTransform.rect.height,"Guide text overflow at "+t.Stage);
     if(t.Stage==4||t.Stage==5)Capture(t);
     t.BeginPractice();var p=t.program;
     switch(t.Lesson)
     {
      case TutorialLesson.BasicInteraction:
       Need(!t.walkGoal,"Blue goal still exists");SnapPlayer(t,t.placementGoal.position+Vector3.back*2+Vector3.up*.1f);t.placement.enabled=false;Need(t.placement.Begin(t.CarryTarget),"Carry failed");Need(t.placement.PreviewAt(t.placementGoal.position+Vector3.up*.013f,true)&&t.placement.Confirm(),"Placement failed: "+t.placement.PlacementError);t.placement.enabled=true;break;
      case TutorialLesson.LaunchBeacons:beaconPhase=0;PutBeacon(t,0,t.pages[t.Stage].layout.beaconTargetOne);break;
      case TutorialLesson.Driving:
       int drive=p.Add(CircuitKind.DriveTo),a=Point(p);p.Connect(0,drive,false);p.Connect(a,drive,true);p.ToggleDock(t.robot);Need(!p.Docked,"Driving allowed docking before CHECK");p.board.CheckCircuit();int unused=p.Add(CircuitKind.Rate,"1");p.ToggleDock(t.robot);Need(!p.Docked,"Changed circuit did not require new CHECK");p.Remove(unused);Dock(t);break;
      case TutorialLesson.Facing:
       int d=p.nodes[0].next,face=p.Add(CircuitKind.Facing),point=Point(p);p.Connect(0,face,false);p.Connect(face,d,false);p.Connect(point,face,true);Need(p.nodes[0].next==face,"Old START wire not replaced");Dock(t);break;
      case TutorialLesson.Shooting:
       int fire=p.Add(CircuitKind.Fire);p.Connect(0,fire,false);FireParameters(p,fire,"AP","0");p.board.CheckCircuit();p.ToggleDock(t.robot);Need(p.Docked,"Non-driving lesson unexpectedly hard-gated");p.ToggleDock(t.robot);FireParameters(p,fire,"BURST","1");Dock(t);break;
      case TutorialLesson.AdvancedShooting:
       Need(p.nodes.Count(n=>n!=null&&n.active)==1,"Advanced lesson must start with START only"); int facing=p.Add(CircuitKind.Facing),f=p.Add(CircuitKind.Fire),w=p.Add(CircuitKind.Wait),stop=p.Add(CircuitKind.CeaseFire),pt=Point(p);
       p.Connect(0,facing,false);p.Connect(facing,f,false);p.Connect(f,w,false);p.Connect(w,stop,false);p.Connect(pt,facing,true);FireParameters(p,f,"AP","2");int duration=p.Add(CircuitKind.Duration,"2");p.Connect(duration,w,true);Dock(t);break;
      case TutorialLesson.Transport:
       foreach(var slot in t.rack.slots)slot.Pack();var mover=t.rack.GetComponent<MovableMachine>();mover.BeginPreview();t.rack.transform.position=t.truck.transform.position+Vector3.up*2;mover.EndPreview(true,t.truck.transform);t.truck.driverDoor.SetOpen(true);Need(t.driver.Enter(t.truck),"Driver could not enter");break;
      case TutorialLesson.FinalTest:
       Need(t.rack.GetComponentInParent<UtilityTruck>()==t.truck&&t.rack.slots.All(s=>s.Packed),"Final test did not start with loaded truck");
       var rackMove=t.rack.GetComponent<MovableMachine>();rackMove.BeginPreview();var rackPose=t.pages[t.Stage].layout.items.First(i=>i.target==t.rack.transform).pose;t.rack.transform.SetPositionAndRotation(rackPose.position,rackPose.rotation);rackMove.EndPreview(true);
       foreach(var slot in t.rack.slots){slot.Unpack();var m=slot.machine;m.BeginPreview();var pose=t.pages[t.Stage].layout.items.First(i=>i.target==m.transform).pose;m.transform.SetPositionAndRotation(pose.position,pose.rotation);m.EndPreview(true);}
       Physics.SyncTransforms();int fa=p.Add(CircuitKind.Facing),fi=p.Add(CircuitKind.Fire),pa=Point(p);p.Connect(0,fa,false);p.Connect(fa,fi,false);p.Connect(pa,fa,true);FireParameters(p,fi,"AP","2");Dock(t);finalStarted=false;break;
     }
     Debug.Log("TUTORIAL_V2_STARTED "+t.Stage+" "+t.Lesson);
    }
    if(t.Lesson==TutorialLesson.LaunchBeacons)
    {
     if(beaconPhase==0&&t.milestones[0]){Need(t.program.pointMarkers[0].Recall(t.launcher.muzzle),"Recall failed");beaconPhase=1;}
     else if(beaconPhase==1&&t.milestones[1]){PutBeacon(t,1,t.pages[t.Stage].layout.beaconTargetTwo);beaconPhase=2;}
    }
    if(t.Lesson==TutorialLesson.FinalTest&&t.defense.Ready&&!finalStarted)
    {
     var preview=UnityEngine.Object.FindAnyObjectByType<EnemyRoutePreview>();preview.Refresh();Need(preview.line.enabled&&preview.line.positionCount>1,"Navigation route not visible");Need(preview.width>.3f&&preview.color.a<.6f,"Route is not wide/translucent");t.program.remote.PressEngage();Need(t.defense.totalEnemies==5&&t.defense.Active,"Final wave not started with 5 enemies");finalStarted=true;
    }
   }
   catch(Exception e){Debug.LogException(e);SessionState.SetBool("TutorialV2Test",false);EditorApplication.Exit(1);}
  }
  static void Capture(TutorialDirector t)
  {
   var canvas=t.guidePanel.GetComponentInParent<Canvas>();var transforms=canvas.GetComponentsInChildren<Transform>(true);var layers=transforms.Select(x=>x.gameObject.layer).ToArray();foreach(var tr in transforms)tr.gameObject.layer=29;
   var obj=new GameObject("Verification UI camera");var camera=obj.AddComponent<Camera>();camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.2f,.24f,.27f);var mode=canvas.renderMode;var old=canvas.worldCamera;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   var rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes("Logs/tutorial-v2-guide-"+t.Stage+".png",image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;
   canvas.renderMode=mode;canvas.worldCamera=old;for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=layers[i];UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(obj);
  }
 }
}
