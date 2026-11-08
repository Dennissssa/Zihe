using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class ShootingSpawnVerification
 {
  static TutorialDirector current;static double began,deadline;static int attempts;
  static ShootingSpawnVerification(){EditorApplication.update+=Poll;}
  public static void Run(){SessionState.SetBool("Mach.SpawnCheck",true);SessionState.SetInt("Mach.Tutorial.TestPage",4);EditorSceneManager.OpenScene(TutorialAuthoring.Path);EditorApplication.EnterPlaymode();}
  static void Poll()
  {
   if(!SessionState.GetBool("Mach.SpawnCheck",false)||!EditorApplication.isPlaying)return;
   try{
    if(deadline==0)deadline=EditorApplication.timeSinceStartup+90;
    if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Spawn test timed out");
    var t=UnityEngine.Object.FindAnyObjectByType<TutorialDirector>();if(!t)return;
    if(!ReferenceEquals(t,current))
    {
     if(!t.GuideVisible)return;current=t;began=EditorApplication.timeSinceStartup;
     var body=t.robot.GetComponent<Rigidbody>();body.isKinematic=false;
     body.position+=new Vector3(100,20,100);body.rotation=Quaternion.Euler(65,125,30);body.linearVelocity=new Vector3(70,-30,40);body.angularVelocity=Vector3.one*20;
     if(attempts%2==0)body.isKinematic=true;
     t.pages[4].layout.Apply();var expected=t.pages[4].layout.items.First(x=>x.target==t.robot.transform&&x.visible).pose;
     if(body.isKinematic||body.linearVelocity.sqrMagnitude>.001f||body.angularVelocity.sqrMagnitude>.001f||Vector3.Distance(body.position,expected.position)>.01f||Quaternion.Angle(body.rotation,expected.rotation)>.1f)throw new Exception("Physics spawn reset failed");
     t.BeginPractice();
    }
    if(EditorApplication.timeSinceStartup-began<2)return;
    var pose=t.pages[4].layout.items.First(x=>x.target==t.robot.transform&&x.visible).pose;var rb=t.robot.GetComponent<Rigidbody>();
    Debug.Log("SPAWN_CHECK "+attempts+" active="+t.robot.gameObject.activeInHierarchy+" transform="+t.robot.transform.position+" body="+rb.position+" expected="+pose.position);
    if(!t.robot.gameObject.activeInHierarchy||Vector3.Distance(t.robot.transform.position,pose.position)>2)throw new Exception("Shooting robot missing/displaced after physics");
    if(++attempts==6){Debug.Log("SHOOTING_SPAWN_VERIFIED");SessionState.SetBool("Mach.SpawnCheck",false);EditorApplication.Exit(0);return;}
    t.Retry("Spawn regression");began=EditorApplication.timeSinceStartup;
   }catch(Exception e){Debug.LogException(e);SessionState.SetBool("Mach.SpawnCheck",false);EditorApplication.Exit(1);}
  }
 }
}
