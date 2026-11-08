using UnityEngine;
namespace Mach
{
    public sealed class NavigationConsole : MonoBehaviour
    {
        public BehaviorProgram program;
        public int selectedPoint;
        public CircuitKind mode=CircuitKind.Facing;
        public bool dedicated;
        public float seconds=1;
        public TextMesh targetText,modeText,statusText;
        public string Prompt(int action)=>action==4?"[LMB] Add Cease Fire to tablet":mode==CircuitKind.Wait?"[LMB] "+(action==0?"Increase seconds":action==-1?"Decrease seconds":"Add Wait and Duration"):"[LMB] "+(action==-1?"Previous beacon":action==0?"Next beacon":dedicated?"Add "+(mode==CircuitKind.DriveTo?"Driving":mode.ToString())+" to tablet":action==1?"Select Facing":action==2?"Select Driving":"Confirm to tablet");
        public void Activate(int action)
        {
            if(action==4){if(program)program.Add(CircuitKind.CeaseFire);return;}
            if(mode==CircuitKind.Wait){if(action==0)seconds=Mathf.Min(600,seconds+1);else if(action==-1)seconds=Mathf.Max(0,seconds-1);else if(action==3)Confirm();Refresh();return;}
            int count=program && program.pointMarkers!=null?Mathf.Max(1,program.pointMarkers.Length):16;
            if(action==-1)selectedPoint=(selectedPoint+count-1)%count;
            else if(action==0)selectedPoint=(selectedPoint+1)%count;
            else if(action==1)mode=CircuitKind.Facing;
            else if(action==2)mode=CircuitKind.DriveTo;
            else if(action==3){Confirm();return;}
            Refresh();
        }
        public bool Confirm()
        {
            if(!program||program.Docked){if(statusText)statusText.text="REMOVE TABLET FIRST";return false;}
            if(mode==CircuitKind.Fire)return program.Add(CircuitKind.Fire)>=0;
            if(mode==CircuitKind.Wait){int timerFree=0;foreach(var n in program.nodes)if(!n.active)timerFree++;if(timerFree<2){program.Say("Need two free slots for Wait and Duration.");return false;}program.Add(CircuitKind.Duration,seconds.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture));program.Add(CircuitKind.Wait);return true;}
            string id=((char)('A'+selectedPoint)).ToString();
            CircuitPointMarker marker=null;
            foreach(var p in program.pointMarkers)if(p&&p.pointId==id)marker=p;
            if(!marker){statusText.text="BEACON UNAVAILABLE";return false;}
            int existing=-1,free=0;
            for(int i=0;i<program.nodes.Length;i++){var n=program.nodes[i];if(!n.active)free++;else if(n.kind==CircuitKind.Point&&n.pointId==id)existing=i;}
            if(free<(existing<0?2:1)){statusText.text="TABLET FULL";return false;}
            if(existing<0){int p=program.Add(CircuitKind.Point,id,marker.Position);program.nodes[p].pointId=id;}
            program.Add(mode);program.Save();
            Refresh();statusText.text="ADDED / CONNECT ON TABLET";return true;
        }
        public void Refresh(){if(mode==CircuitKind.Wait){if(targetText)targetText.text=seconds.ToString("0.##")+" SEC";if(modeText)modeText.text="WAIT";if(statusText)statusText.text="DURATION / PARAM";return;}if(targetText)targetText.text="POINT "+(char)('A'+selectedPoint);if(modeText)modeText.text=mode==CircuitKind.Facing?"FACING":"DRIVING";if(statusText)statusText.text=dedicated?"PRESS GREEN KEY TO ADD":"SELECT / CONFIRM";}
        void Awake(){if(!program)program=FindAnyObjectByType<BehaviorProgram>();}
        void Start()=>Refresh();
    }
}


