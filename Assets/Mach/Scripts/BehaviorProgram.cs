using System;
using UnityEngine;
namespace Mach
{
    public enum CircuitKind { Start, Facing, Fire, CeaseFire, DriveTo, Ammo, Rate, Point, Wait, Duration }
    [Serializable] public class CircuitNode
    {
        public bool active;public CircuitKind kind;public string value="",pointId="";public Vector3 point;public Vector2 position;
        public int next=-1,pointInput=-1,ammoInput=-1,rateInput=-1,durationInput=-1;
        public CircuitNode Copy()=>JsonUtility.FromJson<CircuitNode>(JsonUtility.ToJson(this));
    }
    public sealed class BehaviorProgram : MonoBehaviour
    {
        public HeldTablet tablet;public FirstPersonWalker walker;public PlayerPickup pickup;
        public Transform tabletVisual;public CircuitBoardView board;public RobotCircuitRunner dockedRobot;
        public CeaseFireRemote remote;
        public CircuitNode[] nodes=new CircuitNode[32];
        public CircuitPointMarker[] pointMarkers;
        public Vector3 ResolvePoint(CircuitNode node)
        {
            if(!string.IsNullOrEmpty(node.pointId)&&pointMarkers!=null)
                foreach(var marker in pointMarkers)if(marker&&marker.pointId==node.pointId)return marker.Position;
            return node.point;
        }
        public string Status {get;private set;}="CIRCUIT READY";
        public bool Editing=>tablet && tablet.IsRaised && !dockedRobot;
        public bool Docked=>dockedRobot;
        public event Action Changed;
        public Func<string> DockGuard;
        public event Action<RobotCircuitRunner> ProgramDocked;
        float messageUntil;
        void Update()
        {
            if(MissionPauseMenu.BlocksGameplay)return;
            var keyboard=UnityEngine.InputSystem.Keyboard.current;
            if(!Docked&&!Editing&&(!remote||!remote.IsRaised)&&Cursor.lockState==CursorLockMode.Locked&&keyboard!=null&&keyboard.pKey.wasPressedThisFrame&&Physics.Raycast(pickup.viewCamera.ViewportPointToRay(new Vector3(.5f,.5f)),out var hit,40,~(1<<30),QueryTriggerInteraction.Ignore))Add(CircuitKind.Point,"CUSTOM",hit.point);
        }
        void Awake()
        {
            // Reset data in the authored UI slots; never restore a previous session's circuit.
            for(int i=0;i<nodes.Length;i++)nodes[i]=new CircuitNode();
            nodes[0]=new CircuitNode{active=true,kind=CircuitKind.Start,position=new Vector2(-460,205)};
            if(board)board.Refresh();
        }
        public void Say(string message){Status=message;messageUntil=Time.unscaledTime+3;Changed?.Invoke();}
        public void ClearCircuit()
        {
            if(Docked){Say("Remove the tablet before clearing the circuit.");return;}
            for(int i=0;i<nodes.Length;i++)nodes[i]=new CircuitNode();
            nodes[0]=new CircuitNode{active=true,kind=CircuitKind.Start,position=new Vector2(-460,205)};
            GameAudio.Play(SoundCue.Disconnect,transform.position,true);Say("Circuit cleared. START retained.");
        }
        public int Add(CircuitKind kind,string value="",Vector3 point=default)
        {
            if(Docked){Say("Remove the tablet from the robot first.");return -1;}
            for(int i=1;i<nodes.Length;i++)if(!nodes[i].active)
            {nodes[i]=new CircuitNode{active=true,kind=kind,value=value,point=point,position=FindFreeNodePosition()};Say("Added "+kind+" "+value);Save();return i;}
            Say("Board full. Delete a block before collecting more.");return -1;
        }
        public Vector2 FindFreeNodePosition()
        {
            for(int cell=1;cell<1024;cell++)
            {
                var p=new Vector2(-460+(cell%4)*300,205-(cell/4)*152);bool free=true;
                foreach(var n in nodes)if(n!=null&&n.active&&Mathf.Abs(n.position.x-p.x)<240&&Mathf.Abs(n.position.y-p.y)<120){free=false;break;}
                if(free)return p;
            }
            return new Vector2(-460,-40000);
        }
        public static bool IsParameter(CircuitKind kind)=>kind==CircuitKind.Point||kind==CircuitKind.Ammo||kind==CircuitKind.Rate||kind==CircuitKind.Duration;
        public bool Connect(int source,int target,bool data)
        {
            if(!Valid(source)||!Valid(target)||nodes[target].kind==CircuitKind.Start)return false;
            var a=nodes[source];var b=nodes[target];
            if(data)
            {
                if(a.kind==CircuitKind.Point && (b.kind==CircuitKind.Facing||b.kind==CircuitKind.DriveTo))b.pointInput=source;
                else if(a.kind==CircuitKind.Rate && b.kind==CircuitKind.Fire)b.rateInput=source;
                else if(a.kind==CircuitKind.Ammo && b.kind==CircuitKind.Fire)b.ammoInput=source;
                else if(a.kind==CircuitKind.Duration && b.kind==CircuitKind.Wait)b.durationInput=source;
                else{Say("Duration -> Wait; Point -> Facing/Drive To; Rate or Ammo -> Fire.");return false;}
            }
            else
            {if(IsParameter(a.kind)||IsParameter(b.kind)){Say("Parameter blocks connect to an action's yellow PARAM input.");return false;}a.next=target;}
            GameAudio.Play(SoundCue.Connect,transform.position,true);Say(data?"Parameter connected.":"Execution wire connected.");Save();return true;
        }
        public bool Valid(int i)=>i>=0&&i<nodes.Length&&nodes[i]!=null&&nodes[i].active;
        public void Remove(int i)
        {
            if(i<=0||!Valid(i))return;nodes[i]=new CircuitNode();
            foreach(var n in nodes){if(n.next==i)n.next=-1;if(n.pointInput==i)n.pointInput=-1;if(n.rateInput==i)n.rateInput=-1;if(n.ammoInput==i)n.ammoInput=-1;if(n.durationInput==i)n.durationInput=-1;}
            GameAudio.Play(SoundCue.Disconnect,transform.position,true);Say("Block deleted.");Save();
        }
        public void DisconnectWire(int source,int target,bool data)
        {
            if(Docked||!Valid(source)||!Valid(target))return;
            bool removed=false;var n=nodes[target];
            if(!data){if(nodes[source].next==target){nodes[source].next=-1;removed=true;}}
            else
            {
                if(n.pointInput==source){n.pointInput=-1;removed=true;}
                if(n.ammoInput==source){n.ammoInput=-1;removed=true;}
                if(n.rateInput==source){n.rateInput=-1;removed=true;}
                if(n.durationInput==source){n.durationInput=-1;removed=true;}
            }
            if(removed){GameAudio.Play(SoundCue.Disconnect,transform.position,true);Say("Wire disconnected.");Save();}
        }
        public void Disconnect(int i)
        {if(!Valid(i))return;var n=nodes[i];bool wired=n.next>=0||n.pointInput>=0||n.rateInput>=0||n.ammoInput>=0||n.durationInput>=0;nodes[i].next=nodes[i].pointInput=nodes[i].rateInput=nodes[i].ammoInput=nodes[i].durationInput=-1;if(wired)GameAudio.Play(SoundCue.Disconnect,transform.position,true);Say("Selected block wires cleared.");Save();}
        public bool Validate(out string error)
        {
            error="Connect START to an action.";if(!Valid(0)||!Valid(nodes[0].next))return false;
            var seen=new bool[nodes.Length];int current=0;
            while(current>=0)
            {
                if(!Valid(current)){error="Execution wire points to a missing block.";return false;}if(seen[current])break;seen[current]=true;var n=nodes[current];
                if(IsParameter(n.kind)){error="Parameters cannot be in the execution path.";return false;}
                if(n.kind==CircuitKind.Facing||n.kind==CircuitKind.DriveTo){if(!Valid(n.pointInput)||nodes[n.pointInput].kind!=CircuitKind.Point){error=n.kind+" needs a Point DATA wire.";return false;}}
                if(n.kind==CircuitKind.Wait&&(!Valid(n.durationInput)||nodes[n.durationInput].kind!=CircuitKind.Duration||!TrySeconds(nodes[n.durationInput].value,out _))){error="WAIT needs a valid Duration PARAM.";return false;}
                if(n.rateInput>=0&&(!Valid(n.rateInput)||nodes[n.rateInput].kind!=CircuitKind.Rate)){error="Invalid rate wire.";return false;}
                if(n.ammoInput>=0&&(!Valid(n.ammoInput)||nodes[n.ammoInput].kind!=CircuitKind.Ammo)){error="Invalid ammo wire.";return false;}

                if(n.kind==CircuitKind.Fire){if(!Valid(n.ammoInput)||string.IsNullOrWhiteSpace(nodes[n.ammoInput].value)){error="FIRE needs its own AMMO parameter.";return false;}if(!Valid(n.rateInput)||!int.TryParse(nodes[n.rateInput].value,out int fireRate)||fireRate<0||fireRate>2){error="FIRE needs its own valid RATE parameter.";return false;}}
                current=n.next;
            }

            error="CIRCUIT READY";return true;
        }
        public static bool TrySeconds(string text,out float seconds)=>float.TryParse(text,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out seconds)&&!float.IsNaN(seconds)&&!float.IsInfinity(seconds)&&seconds>=0;
        public CircuitNode[] Snapshot(){var copy=new CircuitNode[nodes.Length];for(int i=0;i<nodes.Length;i++)copy[i]=nodes[i].Copy();return copy;}
        public void Save()=>Changed?.Invoke();
        public void ToggleDock(RobotCircuitRunner robot)
        {
            if(Docked)
            {
                if(dockedRobot!=robot)return;GameAudio.Play(SoundCue.Undock,transform.position,true);robot.StopProgram("Tablet removed");tabletVisual.SetParent(tablet.poseRoot,false);tabletVisual.localPosition=Vector3.zero;tabletVisual.localRotation=Quaternion.identity;tabletVisual.localScale=Vector3.one;SetLayer(tabletVisual,30);
                dockedRobot=null;tablet.poseRoot.gameObject.SetActive(true);tablet.enabled=true;board.canvas.worldCamera=tablet.tabletCamera;Say("Tablet removed. Program stopped.");return;
            }
            string blocked=DockGuard?.Invoke();if(!string.IsNullOrEmpty(blocked)){Say(blocked);return;}
            if(pickup.HeldItem){Say("Drop the carried item before inserting the tablet.");return;}
            if(!Validate(out string error)){GameAudio.Play(SoundCue.Error,transform.position,true);Say(error);return;}
            GameAudio.Play(SoundCue.Dock,transform.position,true);tablet.SnapToRest();tabletVisual.SetParent(robot.rig.tabletSocket,false);tabletVisual.localPosition=Vector3.zero;tabletVisual.localRotation=Quaternion.identity;tabletVisual.localScale=Vector3.one;SetLayer(tabletVisual,0);
            tablet.enabled=false;tablet.poseRoot.gameObject.SetActive(false);dockedRobot=robot;board.canvas.worldCamera=pickup.viewCamera;robot.Run(Snapshot(),this);Say("ROBOT RUNNING");ProgramDocked?.Invoke(robot);
        }
        static void SetLayer(Transform root,int layer){foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;}
    }
}




