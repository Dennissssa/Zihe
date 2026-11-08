using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class TimedFireLive
 {
  static TimedFireLive(){EditorApplication.update+=Poll;}
  static void Poll(){if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/TimedFire.request"))return;File.Delete("Temp/TimedFire.request");try{AmmoMachineAuthoring.AuthorTimedFire();File.WriteAllText("Logs/timed-fire-applied.txt","Saved dual Fire/Cease Fire and Timer consoles.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/timed-fire-error.txt",e.ToString());}}
 }
 public static partial class AmmoMachineAuthoring
 {
  const string TimedFolder="Assets/Mach/TimedFire";
  static void SaveTimedPrefab(NavigationConsole c,string name){var go=UnityEngine.Object.Instantiate(c.gameObject);go.name=name;go.GetComponent<NavigationConsole>().program=null;go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);go.transform.localScale=Vector3.one;PrefabUtility.SaveAsPrefabAsset(go,TimedFolder+"/"+name+".prefab");UnityEngine.Object.DestroyImmediate(go);}
  public static void TimedFireBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);AuthorTimedFire();VerifyTimedFire();}
  public static void AuthorTimedFire()
  {
   NavigationConsole fire=null,source=null;foreach(var c in UnityEngine.Object.FindObjectsByType<NavigationConsole>()){if(c.mode==CircuitKind.Fire)fire=c;if(c.mode==CircuitKind.Facing)source=c;if(c.mode==CircuitKind.Wait)return;}
   if(!fire||!source||fire.gameObject.scene.path!=WalkingSceneAuthoring.ScenePath)throw new Exception("Open existing scene");
   Directory.CreateDirectory(TimedFolder+"/Meshes");AssetDatabase.Refresh();var savedRoot=root;var savedFolder=meshFolder;var savedPrefix=meshPrefix;int savedIndex=meshIndex;meshFolder=TimedFolder+"/Meshes";meshPrefix="Timed_";meshIndex=0;
   metal=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Graphite steel.mat");rubber=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Rubber seals.mat");var print=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Button ivory print.mat");var red=AssetDatabase.LoadAssetAtPath<Material>(ErgFolder+"/Fire red.mat");
   try
   {
    foreach(var name in new[]{"Compact ivory cabinet","Graphite foot","Top protective rail"}){var t=fire.transform.Find(name);Undo.RecordObject(t,"Widen fire cabinet");var scale=t.localScale;scale.x*=1.85f;t.localScale=scale;}
    var panel=fire.transform.Find("Sloped single control");foreach(var b in fire.GetComponentsInChildren<CircuitWorldButton>())Undo.DestroyObjectImmediate(b.gameObject);
    DedicatedKey(panel,fire,"FIRE",new Vector2(-.29f,.35f),new Vector2(.44f,.36f),3,red,print);DedicatedKey(panel,fire,"CEASE FIRE",new Vector2(.29f,.35f),new Vector2(.44f,.36f),4,red,print);
    var mesh=UnityEngine.Object.Instantiate(fire.GetComponent<MeshCollider>().sharedMesh);var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i].x*=1.85f;mesh.vertices=vertices;mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,TimedFolder+"/Wide fire collider.asset");fire.GetComponent<MeshCollider>().sharedMesh=mesh;
    fire.GetComponent<MovableMachine>().boundsSize=new Vector3(1.34f,.94f,.71f);SaveTimedPrefab(fire,"FireConsole");
    var timerGo=UnityEngine.Object.Instantiate(source.gameObject);timerGo.name="TimerConsole";Undo.RegisterCreatedObjectUndo(timerGo,"Create timer console");var timer=timerGo.GetComponent<NavigationConsole>();timer.mode=CircuitKind.Wait;timer.seconds=1;timer.dedicated=true;
    var face=timerGo.transform.Find("Inclined control panel");face.Find("Heading").GetComponent<TextMesh>().text="TIMER / CONTROL";
    foreach(var b in timerGo.GetComponentsInChildren<CircuitWorldButton>())Undo.DestroyObjectImmediate(b.gameObject);
    DedicatedKey(face,timer,"UP",new Vector2(-.255f,.59f),new Vector2(.33f,.188f),0,metal,print);DedicatedKey(face,timer,"DOWN",new Vector2(-.255f,.245f),new Vector2(.33f,.188f),-1,metal,print);DedicatedKey(face,timer,"WAIT",new Vector2(.255f,.418f),new Vector2(.33f,.50f),3,metal,print);
    timer.Refresh();timerGo.GetComponent<MovableMachine>().displayName="Timer console";timerGo.transform.position=fire.transform.position+fire.transform.right*1.55f;SaveTimedPrefab(timer,"TimerConsole");
    foreach(var b in UnityEngine.Object.FindObjectsByType<CircuitWorldButton>())if(!b.navigation&&b.kind==CircuitKind.Wait)b.gameObject.SetActive(false);
    AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(fire.gameObject.scene);EditorSceneManager.SaveScene(fire.gameObject.scene);
   }
   finally{root=savedRoot;meshFolder=savedFolder;meshPrefix=savedPrefix;meshIndex=savedIndex;}
  }
  public static void VerifyTimedFire()
  {
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var p=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();var runner=UnityEngine.Object.FindAnyObjectByType<RobotCircuitRunner>();NavigationConsole timer=null,fire=null;foreach(var c in UnityEngine.Object.FindObjectsByType<NavigationConsole>()){if(c.mode==CircuitKind.Wait)timer=c;if(c.mode==CircuitKind.Fire)fire=c;}
   if(!timer||!fire||fire.GetComponentsInChildren<CircuitWorldButton>().Length!=2)throw new Exception("Machines missing");p.ClearCircuit();timer.seconds=2;timer.Activate(3);if(p.nodes[1].kind!=CircuitKind.Duration||p.nodes[2].kind!=CircuitKind.Wait)throw new Exception("Timer output");p.board.ClickPort(2,true,true);if(!p.board.ClickPort(1,false,true)||p.nodes[2].durationInput!=1)throw new Exception("Duration reverse connection");p.Remove(1);if(p.nodes[2].durationInput!=-1)throw new Exception("Duration removal");
   p.ClearCircuit();p.nodes[0].next=1;p.nodes[1]=new CircuitNode{active=true,kind=CircuitKind.Fire,ammoInput=5,rateInput=6,next=2};p.nodes[2]=new CircuitNode{active=true,kind=CircuitKind.Wait,durationInput=4,next=3};p.nodes[3]=new CircuitNode{active=true,kind=CircuitKind.CeaseFire,next=1};p.nodes[4]=new CircuitNode{active=true,kind=CircuitKind.Duration,value="2"};p.nodes[5]=new CircuitNode{active=true,kind=CircuitKind.Ammo,value="AP"};p.nodes[6]=new CircuitNode{active=true,kind=CircuitKind.Rate,value="0"};
   runner.Run(p.Snapshot(),p);runner.Tick(.01f);runner.Tick(.01f);if(!runner.Firing)throw new Exception("Fire failed");runner.Tick(1);if(runner.CurrentNode!=2||!runner.Firing)throw new Exception("Wait did not retain Fire");runner.Tick(1);runner.Tick(.01f);if(runner.Firing)throw new Exception("Cease failed");runner.Tick(.01f);if(!runner.Firing)throw new Exception("Loop cannot restart Fire");runner.Tick(1);if(runner.CurrentNode!=2)throw new Exception("Loop wait not reset");runner.Tick(1);if(runner.CurrentNode!=3)throw new Exception("Loop delay incorrect");runner.StopProgram("");
   Physics.SyncTransforms();foreach(var c in new[]{fire,timer})foreach(var b in c.GetComponentsInChildren<CircuitWorldButton>()){if(!Physics.Raycast(b.transform.TransformPoint(new Vector3(0,0,-.6f)),b.transform.forward,out var hit,1)||hit.collider.GetComponent<CircuitWorldButton>()!=b)throw new Exception("Blocked button");}
   RenderPreview(fire.gameObject,"Logs/dual-fire-preview.png");RenderPreview(timer.gameObject,"Logs/timer-preview.png");EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Debug.Log("TIMED_FIRE_VERIFIED");
  }
 }
}
