using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class TutorialRetryVerification
 {
  static double deadline;static int phase;static int[] lessons={2,3,4,5,7};static int index;static TutorialDirector instance;static int frames;
  static TutorialRetryVerification(){EditorApplication.update+=Poll;}
  public static void Run(){SessionState.SetBool("Mach.RetryVerification",true);SessionState.SetInt("Mach.Tutorial.TestPage",2);EditorSceneManager.OpenScene(TutorialAuthoring.Path);EditorApplication.EnterPlaymode();}
  static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
  static void Poll()
  {
   if(!SessionState.GetBool("Mach.RetryVerification",false)||!EditorApplication.isPlaying)return;
   if(deadline==0)deadline=EditorApplication.timeSinceStartup+150;
   try
   {
    Require(EditorApplication.timeSinceStartup<deadline,"Timed out");var t=UnityEngine.Object.FindAnyObjectByType<TutorialDirector>();if(!t||!t.GuideVisible)return;
    if(phase==1&&ReferenceEquals(t,instance))return;
    if(++frames<4)return;frames=0;
    Require(t.guidePanel.activeInHierarchy&&t.heading.isActiveAndEnabled,"Guide missing at lesson "+t.Stage);
    if(t.Stage==5)Require(t.program.nodes.Count(n=>n!=null&&n.active)==1,"Advanced lesson not empty");
    if(t.Stage==7)Require(t.rack.slots.All(s=>s.Packed)&&t.rack.gameObject.activeInHierarchy,"Final cargo not initialized");
    else Require(t.robot.gameObject.activeInHierarchy,"Robot hidden at lesson "+t.Stage);
    t.BeginPractice();Require(t.objective.isActiveAndEnabled&&t.keyHints.isActiveAndEnabled,"HUD missing at lesson "+t.Stage);
    Require(!t.objective.text.Contains("TODO")&&!t.objective.text.Contains("DONE"),"Old task status labels");
    if(phase==0)
    {
     // Simulate a failed attempt with the tablet docked and robot packed into hidden cargo.
     t.program.ClearCircuit();int fire=t.program.Add(CircuitKind.Fire),ammo=t.program.Add(CircuitKind.Ammo,"AP"),rate=t.program.Add(CircuitKind.Rate,"1");t.program.Connect(0,fire,false);t.program.Connect(ammo,fire,true);t.program.Connect(rate,fire,true);
     t.program.DockGuard=null;t.program.ToggleDock(t.robot);t.rack.SlotFor(t.robot.GetComponent<MovableMachine>()).Pack();
     instance=t;phase=1;t.Retry("Verification retry");return;
    }
    Debug.Log("RETRY_PASS lesson "+(t.Stage+1));index++;
    if(index>=lessons.Length){Debug.Log("TUTORIAL_RETRY_VERIFIED");SessionState.SetBool("Mach.RetryVerification",false);EditorApplication.Exit(0);return;}
    phase=0;SessionState.SetInt("Mach.Tutorial.TestPage",lessons[index]);instance=t;t.Retry("Next verification lesson");
   }catch(Exception e){Debug.LogException(e);SessionState.SetBool("Mach.RetryVerification",false);EditorApplication.Exit(1);}
  }
 }
}
