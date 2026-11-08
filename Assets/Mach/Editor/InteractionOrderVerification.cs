using System;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
 public static class InteractionOrderVerification
 {
  static void Check(bool value,string message){if(!value)throw new Exception(message);Debug.Log("INTERACTION_PASS: "+message);}
  public static void Verify()
  {
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
   var p=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();var board=p.board;var launcher=p.GetComponent<PointLauncher>();var remote=p.remote;
   // Batch-mode has no focused game window, so cursor locking is verified in Play Mode only.
   for(int i=0;i<3;i++)
   {
    launcher.SetRaised(true);Check(launcher.IsRaised&&!remote.IsRaised,"Launcher replaces remote");
    remote.SetRaised(true);Check(remote.IsRaised&&!launcher.IsRaised,"Remote replaces launcher");
   }
   launcher.SetRaised(true);launcher.SetRaised(false);
   void Setup(){board.ClearBoard();p.nodes[1]=new CircuitNode{active=true,kind=CircuitKind.Facing};p.nodes[2]=new CircuitNode{active=true,kind=CircuitKind.Fire};p.nodes[3]=new CircuitNode{active=true,kind=CircuitKind.Point};p.nodes[4]=new CircuitNode{active=true,kind=CircuitKind.Ammo,value="AP"};p.nodes[5]=new CircuitNode{active=true,kind=CircuitKind.Rate,value="1"};p.nodes[6]=new CircuitNode{active=true,kind=CircuitKind.DriveTo};}
   foreach(bool reverse in new[]{false,true})
   {
    Setup();if(reverse){board.ClickPort(1,true,false);board.ClickPort(0,false,false);}else{board.ClickPort(0,false,false);board.ClickPort(1,true,false);}Check(p.nodes[0].next==1,"Start execution wire reverse="+reverse);
    if(reverse){board.ClickPort(2,true,false);board.ClickPort(1,false,false);}else{board.ClickPort(1,false,false);board.ClickPort(2,true,false);}Check(p.nodes[1].next==2,"Action execution wire reverse="+reverse);
    foreach(int parameter in new[]{3,4,5})
    {
     int action=parameter==3?1:2;
     if(reverse){board.ClickPort(action,true,true);board.ClickPort(parameter,false,true);}else{board.ClickPort(parameter,false,true);board.ClickPort(action,true,true);}
     Check((parameter==3?p.nodes[action].pointInput:parameter==4?p.nodes[action].ammoInput:p.nodes[action].rateInput)==parameter,"Parameter "+parameter+" reverse="+reverse);
    }
    Check(p.Validate(out var error),"Completed circuit validates: "+error);
   }
   Setup();board.ClickPort(0,false,false);Check(!board.ClickPort(1,false,false)&&p.nodes[0].next==-1,"Two outputs rejected");Check(!board.ClickPort(1,true,true)&&p.nodes[1].pointInput==-1,"Execution/data mismatch rejected");Check(board.ClickPort(1,true,false)&&p.nodes[0].next==1,"Valid endpoint still completes pending wire");
   board.ClickPort(1,true,true);Check(!board.ClickPort(4,false,true)&&p.nodes[1].pointInput==-1,"Wrong parameter type rejected");Check(board.ClickPort(3,false,true)&&p.nodes[1].pointInput==3,"Correct parameter finishes reverse wire");
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Debug.Log("INTERACTION_ORDER_VERIFIED");
  }
 }
}

