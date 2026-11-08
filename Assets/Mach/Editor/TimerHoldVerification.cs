using System;
using UnityEngine;
namespace Mach.Editor
{
 public static class TimerHoldVerification
 {
  public static void Verify()
  {
   var go=new GameObject("Timer hold verification");
   try
   {
    var timer=go.AddComponent<NavigationConsole>();timer.mode=CircuitKind.Wait;var b=go.AddComponent<CircuitWorldButton>();b.navigation=timer;b.navigationAction=0;
    timer.seconds=0;b.Activate();b.BeginHold();b.AdvanceHold(.49f);Check(timer.seconds==1,"Short press remains one step");b.AdvanceHold(.01f);Check(timer.seconds==2,"Slow first half second");b.AdvanceHold(1);Check(timer.seconds==14,"Linear acceleration");b.EndHold();b.AdvanceHold(10);Check(timer.seconds==14,"Release stops");
    float Run(int frames){timer.seconds=0;b.BeginHold();for(int i=0;i<frames;i++)b.AdvanceHold(2f/frames);b.EndHold();return timer.seconds;}
    Check(Run(60)==Run(240),"Frame rate independent");timer.seconds=599;b.BeginHold();b.AdvanceHold(10);b.EndHold();Check(timer.seconds==600,"Upper clamp");
    b.navigationAction=-1;timer.seconds=2;b.BeginHold();b.AdvanceHold(4);b.EndHold();Check(timer.seconds==0,"Lower clamp");
    timer.mode=CircuitKind.Facing;b.BeginHold();Check(!b.IsHeld,"Only timer arrows repeat");timer.mode=CircuitKind.Wait;b.navigationAction=3;b.BeginHold();Check(!b.IsHeld,"Confirm never repeats");Debug.Log("TIMER_HOLD_VERIFIED");
   }
   finally{UnityEngine.Object.DestroyImmediate(go);}
  }
  static void Check(bool value,string message){if(!value)throw new Exception(message);Debug.Log("TIMER_HOLD_PASS: "+message);}
 }
}
