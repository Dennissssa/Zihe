using System;
using UnityEngine;
using UnityEditor.SceneManagement;
namespace Mach.Editor
{
 public static class LocalParameterVerification
 {
  public static void Verify()
  {
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
   var p=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();
   void Check(bool expected,string label){bool actual=p.Validate(out var error);if(actual!=expected)throw new Exception(label+": "+error);}
   foreach(var kind in new[]{CircuitKind.Facing,CircuitKind.DriveTo,CircuitKind.Wait})
   {
    p.ClearCircuit();int action=p.Add(kind);p.Connect(0,action,false);Check(false,"Missing local parameter");
    int parameter=p.Add(kind==CircuitKind.Wait?CircuitKind.Duration:CircuitKind.Point,kind==CircuitKind.Wait?"0.1":"A");p.Connect(parameter,action,true);Check(true,"Standalone "+kind);
    p.Connect(action,action,false);Check(true,"Loop "+kind);
   }
   p.ClearCircuit();int cease=p.Add(CircuitKind.CeaseFire);p.Connect(0,cease,false);Check(true,"Cease without parameters");
   p.ClearCircuit();int fire=p.Add(CircuitKind.Fire),ammo=p.Add(CircuitKind.Ammo,"JHP"),rate=p.Add(CircuitKind.Rate,"1");p.Connect(0,fire,false);p.Connect(ammo,fire,true);Check(false,"Fire missing rate");p.Connect(rate,fire,true);Check(true,"Fire without facing");
   int second=p.Add(CircuitKind.Fire);p.Connect(fire,second,false);Check(false,"Second fire cannot inherit parameters");p.Connect(ammo,second,true);p.Connect(rate,second,true);Check(true,"Explicit shared parameters");
   p.nodes[rate].value="bad";Check(false,"Invalid rate");p.nodes[rate].value="1";
   var robot=UnityEngine.Object.FindObjectsByType<RobotCircuitRunner>(FindObjectsInactive.Include)[0];robot.Run(p.Snapshot(),p);robot.Tick(.02f);robot.Tick(.02f);if(!robot.Firing||robot.ShotCount<1)throw new Exception("Fire without Facing did not execute");robot.StopProgram("");
   p.ClearCircuit();int wait=p.Add(CircuitKind.Wait),duration=p.Add(CircuitKind.Duration,"0.1");p.Connect(0,wait,false);p.Connect(duration,wait,true);robot.Run(p.Snapshot(),p);robot.Tick(.01f);robot.Tick(.15f);if(robot.Running)throw new Exception("Standalone wait did not finish");
   Debug.Log("LOCAL_PARAMETERS_VERIFIED: independent Facing/Driving/Wait/Fire; cycles; missing and invalid parameters rejected; each Fire requires explicit inputs; runtime Fire and Wait execute");
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
  }
 }
}
