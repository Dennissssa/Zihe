using UnityEngine;
namespace Mach
{
    public sealed class CircuitWorldButton : MonoBehaviour
    {
        public BehaviorProgram program;public CircuitKind kind;public string value="";public Transform point;
        public NavigationConsole navigation;public int navigationAction;
        public Vector3 pressDirection=Vector3.down;
        public string Prompt=>navigation?navigation.Prompt(navigationAction):"[LMB] Add "+kind+" "+value+" to tablet";
        public Transform cap;float press;Vector3 rest;
        public bool CanRepeat=>navigation&&navigation.mode==CircuitKind.Wait&&(navigationAction==0||navigationAction==-1);
        public bool IsHeld {get;private set;}
        float heldTime,repeatBudget;
        public void BeginHold(){if(!CanRepeat)return;IsHeld=true;heldTime=repeatBudget=0;}
        public void EndHold(){IsHeld=false;heldTime=repeatBudget=0;}
        // Integral of a 2 steps/s lead-in, then a linear ramp to 40 steps/s.
        static float RepeatCount(float t)
        {
            float fast=Mathf.Max(0,t-.5f),ramp=Mathf.Min(fast,1.9f);
            return 2*Mathf.Min(t,.5f)+2*ramp+10*ramp*ramp+40*Mathf.Max(0,fast-1.9f);
        }
        public void AdvanceHold(float dt)
        {
            if(!IsHeld||!CanRepeat)return;
            float next=heldTime+Mathf.Max(0,dt);repeatBudget+=RepeatCount(next)-RepeatCount(heldTime);heldTime=next;
            int steps=Mathf.FloorToInt(repeatBudget+.00001f);repeatBudget-=steps;
            if(steps>0){navigation.seconds=Mathf.Clamp(navigation.seconds+(navigationAction==0?steps:-steps),0,600);navigation.Refresh();}
        }
        void Awake(){if(cap)rest=cap.localPosition;}
        public void Activate(){GameAudio.Play(SoundCue.Button,transform.position);if(navigation){navigation.Activate(navigationAction);press=1;return;}int index=program.Add(kind,value,point?point.position:Vector3.zero);if(index>=0&&point&&point.TryGetComponent<CircuitPointMarker>(out var marker))program.nodes[index].pointId=marker.pointId;press=1;}
        void Update(){press=Mathf.MoveTowards(press,0,Time.deltaTime*5);if(cap)cap.localPosition=rest+pressDirection*.018f*(IsHeld?1:Mathf.Sin(press*Mathf.PI));}
        void OnDisable()=>EndHold();
    }

}


