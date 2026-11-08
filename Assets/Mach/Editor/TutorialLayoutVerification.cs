using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Mach.Editor
{
 public static class TutorialLayoutVerification
 {
  public static void Verify()
  {
   EditorSceneManager.OpenScene(TutorialAuthoring.Path);var t=TutorialLayoutAuthoring.Director();if(!t.pages[0].layout)TutorialLayoutAuthoring.Build();
   foreach(var page in t.pages)
   {
    var layout=page.layout;if(!layout||!layout.pointA||!layout.pointB||layout.items.Any(i=>!i.target||!i.pose))throw new Exception("Missing layout binding");
    if(layout.fences.GetComponentsInChildren<Collider>().Length<20)throw new Exception("Incomplete fence");
    foreach(var item in layout.items.Where(i=>i.visible)){var p=layout.transform.InverseTransformPoint(item.pose.position);if(Mathf.Abs(p.x)>=layout.areaSize.x*.5f||Mathf.Abs(p.z)>=layout.areaSize.y*.5f)throw new Exception("Spawn outside yard: "+item.pose.name);}
   }
   TutorialLayoutAuthoring.Preview(t,0,false);var original=t.ammo.transform.position;var other=t.pages[1].layout.items.First(i=>i.target==t.ammo.transform).pose.position;
   t.ammo.transform.position+=Vector3.right*.25f;TutorialLayoutAuthoring.Capture(t);TutorialLayoutAuthoring.Preview(t,1,false);TutorialLayoutAuthoring.Preview(t,0,false);
   if(Vector3.Distance(t.ammo.transform.position,original+Vector3.right*.25f)>.001f||t.pages[1].layout.items.First(i=>i.target==t.ammo.transform).pose.position!=other)throw new Exception("Per-lesson placement leaked");
   t.ammo.transform.position=original+Vector3.right*.4f;EditorSceneManager.SaveScene(t.gameObject.scene);EditorSceneManager.OpenScene(TutorialAuthoring.Path);t=TutorialLayoutAuthoring.Director();
   if(Vector3.Distance(t.pages[0].layout.items.First(i=>i.target==t.ammo.transform).pose.position,original+Vector3.right*.4f)>.001f)throw new Exception("Normal scene save failed to capture layout");
   t.ammo.transform.position=original;TutorialLayoutAuthoring.Capture(t);
   TutorialLayoutAuthoring.Preview(t,10,false);t.defense.surface.BuildNavMesh();var spawner=UnityEngine.Object.FindAnyObjectByType<GhostSpawner>();if(!spawner.TryFindSpawn(out _))throw new Exception("No enemy route in compact yard");
   TutorialLayoutAuthoring.Preview(t,0,false);EditorSceneManager.SaveScene(t.gameObject.scene);Debug.Log("TUTORIAL_LAYOUT_VERIFIED: eleven isolated poses, roundtrip editing, fences, spawn bounds, final navmesh");
  }
 }
}
