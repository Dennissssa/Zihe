using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Mach
{
 // One sample per frame, shared by walking, driving and held-object sway.
 public static class PlayerLookInput
 {
  static int sampledFrame=-1,lastFrame=-10;static Vector2 sample;
#if UNITY_WEBGL && !UNITY_EDITOR
  static readonly float[] browserDelta=new float[2];
  [DllImport("__Internal")] static extern int Mach_ReadPointerDelta([Out] float[] output);
#endif
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  static void Reset(){sampledFrame=-1;lastFrame=-10;sample=Vector2.zero;}
  public static Vector2 Read()
  {
   if(sampledFrame==Time.frameCount)return sample;
   sampledFrame=Time.frameCount;sample=Vector2.zero;
   bool active=Application.isFocused&&Cursor.lockState==CursorLockMode.Locked;
   Vector2 delta=Mouse.current!=null?Mouse.current.delta.ReadValue():Vector2.zero;
#if UNITY_WEBGL && !UNITY_EDITOR
   active=Mach_ReadPointerDelta(browserDelta)!=0&&active;
   delta=new Vector2(browserDelta[0],browserDelta[1]);
#endif
   bool continuous=lastFrame==Time.frameCount-1;lastFrame=active?Time.frameCount:-10;
   if(active&&continuous)sample=Finite(delta);
   return sample;
  }
  public static Vector2 Finite(Vector2 delta)=>float.IsNaN(delta.x)||float.IsNaN(delta.y)||float.IsInfinity(delta.x)||float.IsInfinity(delta.y)?Vector2.zero:delta;
  public static void Discard(){lastFrame=-10;sample=Vector2.zero;sampledFrame=-1;}
 }
}
