using UnityEngine;
namespace Mach
{
 // Geometry and probes are authored in the scene; only their captures update at runtime.
 public sealed class TutorialRoomReflection:MonoBehaviour
 {
  public TutorialStageLayout layout;
  public ReflectionProbe probe;
  [Min(1)] public float refreshSeconds=3;
  float nextCapture;int renderId=-1;
  void Update()
  {
   if(!layout||!probe||!layout.director)return;
   var director=layout.director;
   if(director.pages[director.Stage].layout!=layout)return;
   if(Time.unscaledTime<nextCapture||renderId>=0&&!probe.IsFinishedRendering(renderId))return;
   renderId=probe.RenderProbe();nextCapture=Time.unscaledTime+refreshSeconds;
  }
 }
}
