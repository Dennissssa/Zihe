using UnityEngine;
namespace Mach
{
    public sealed class RobotCircuitRunner : MonoBehaviour
    {
        public RobotAssemblyRig rig;public Transform muzzle;public LineRenderer tracer;public Light muzzleFlash;public TextMesh display;
        public float driveSpeed=1.25f,turnSpeed=75;
        public RobotGroundMotor groundMotor;
        public bool Running {get;private set;}public bool Firing {get;private set;}public bool FireLatched {get;private set;}
        public int CurrentNode {get;private set;}=-1;public int ShotCount {get;private set;}
        public string Ammo {get;private set;}="";public int Rate {get;private set;}=-1;
        public event System.Action<CircuitKind> ActionCompleted;
        CircuitNode[] nodes;BehaviorProgram owner;float elapsed,blocked,shotTimer,flashTime;bool hasFacing;Vector3 aim;int aimPoint=-1;
        public void Run(CircuitNode[] program,BehaviorProgram source)
        {StopProgram("");nodes=program;owner=source;CurrentNode=0;Ammo="";Rate=-1;hasFacing=false;aimPoint=-1;FireLatched=false;Running=true;elapsed=blocked=shotTimer=0;ShotCount=0;rig.previewTransmission=true;GameAudio.Play(SoundCue.RobotStart,transform.position);Report("RUNNING");}
        public void StopProgram(string reason)
        {if(Running)GameAudio.Play(SoundCue.RobotStop,transform.position);Running=false;Firing=false;CurrentNode=-1;if(groundMotor)groundMotor.Stop();rig.forwardCommand=rig.steeringCommand=0;rig.previewTransmission=false;if(tracer)tracer.enabled=false;if(muzzleFlash)muzzleFlash.enabled=false;Report(reason);}
        public void RemoteCeaseFire(){Firing=false;FireLatched=true;if(tracer)tracer.enabled=false;if(muzzleFlash)muzzleFlash.enabled=false;Report("REMOTE CEASE FIRE / LATCHED");}
        void Report(string text){if(display)display.text=text;if(owner)owner.Say(text);}
        void Next(){var completed=nodes[CurrentNode].kind;if(groundMotor)groundMotor.Stop();int next=nodes[CurrentNode].next;elapsed=blocked=0;CurrentNode=next;if(next<0){Running=false;rig.forwardCommand=rig.steeringCommand=0;Report(Firing?"PROGRAM END / FIRING":"PROGRAM END");}ActionCompleted?.Invoke(completed);}
        void Fail(string reason){StopProgram("ERROR: "+reason);}
        void Update(){if(!MissionPauseMenu.BlocksGameplay)Tick(Time.deltaTime);}
        public void Tick(float dt)
        {
            var carry=GetComponent<MovableMachine>();
            if(carry&&(carry.IsPreview||GetComponentInParent<UtilityTruck>())){if(groundMotor)groundMotor.Stop();rig.forwardCommand=rig.steeringCommand=0;if(tracer)tracer.enabled=false;if(muzzleFlash)muzzleFlash.enabled=false;return;}
            if(hasFacing&&aimPoint>=0&&(Running||Firing)){aim=owner.ResolvePoint(nodes[aimPoint]);AimAt(aim);}
            flashTime-=dt;if(tracer)tracer.enabled=flashTime>0&&Firing;if(muzzleFlash)muzzleFlash.enabled=flashTime>0&&Firing;
            if(Running)
            {
                if(CurrentNode<0||CurrentNode>=nodes.Length||!nodes[CurrentNode].active){Fail("Missing block");return;}
                var n=nodes[CurrentNode];elapsed+=dt;
                switch(n.kind)
                {
                    case CircuitKind.Start:Next();break;
                    case CircuitKind.Ammo:Ammo=n.value;Next();break;
                    case CircuitKind.Rate:Rate=int.TryParse(n.value,out int rate)?Mathf.Clamp(rate,0,2):-1;Next();break;
                    case CircuitKind.Facing:
                        if(n.pointInput<0){Fail("Facing needs a Point");break;}aimPoint=n.pointInput;aim=owner.ResolvePoint(nodes[n.pointInput]);
                        AimAt(aim);if(Quaternion.Angle(rig.gunElevation.rotation,Quaternion.LookRotation(aim-rig.gunElevation.position))<3 || (Mathf.Abs(Mathf.DeltaAngle(rig.turretYaw.localEulerAngles.y,rig.targetYaw))<2 && Mathf.Abs(Mathf.DeltaAngle(rig.gunElevation.localEulerAngles.x,-rig.targetElevation))<2)) {hasFacing=true;Next();}break;
                    case CircuitKind.Fire:
                        Ammo=n.ammoInput>=0&&n.ammoInput<nodes.Length&&nodes[n.ammoInput].active&&nodes[n.ammoInput].kind==CircuitKind.Ammo?nodes[n.ammoInput].value:"";Rate=n.rateInput>=0&&n.rateInput<nodes.Length&&nodes[n.rateInput].active&&nodes[n.rateInput].kind==CircuitKind.Rate&&int.TryParse(nodes[n.rateInput].value,out int r)&&r>=0&&r<=2?r:-1;
                        if(string.IsNullOrEmpty(Ammo)||Rate<0){Fail("AMMO and RATE required");break;}FireLatched=false;Firing=true;Next();break;
                    case CircuitKind.CeaseFire:Firing=false;FireLatched=false;if(tracer)tracer.enabled=false;if(muzzleFlash)muzzleFlash.enabled=false;Next();break;
                    case CircuitKind.DriveTo:
                        if(n.pointInput<0){Fail("Drive To needs a Point");break;}Drive(owner.ResolvePoint(nodes[n.pointInput]),dt);break;
                    case CircuitKind.Wait:if(n.durationInput<0||n.durationInput>=nodes.Length||nodes[n.durationInput].kind!=CircuitKind.Duration||!BehaviorProgram.TrySeconds(nodes[n.durationInput].value,out float seconds)){Fail("WAIT needs Duration PARAM");break;}if(elapsed>=seconds)Next();break;
                    default:Fail("Point must use a DATA wire");break;
                }
            }
            if(Firing&&!FireLatched){shotTimer-=dt;if(shotTimer<=0){Shoot();shotTimer=Rate==0?.5f:Rate==1?.22f:.10f;}}
        }
        [System.Serializable] public class Playback {public int node,shots,rate,point; public string ammo; public bool running,firing,latched,facing;public float elapsed,shotTimer;public Vector3 aim;}
        public Playback CapturePlayback()=>new Playback{node=CurrentNode,shots=ShotCount,rate=Rate,point=aimPoint,ammo=Ammo,running=Running,firing=Firing,latched=FireLatched,facing=hasFacing,elapsed=elapsed,shotTimer=shotTimer,aim=aim};
        public void RestorePlayback(Playback state){
            CurrentNode=state.node;ShotCount=state.shots;Rate=state.rate;aimPoint=state.point;Ammo=state.ammo;Running=state.running;Firing=state.firing;FireLatched=state.latched;hasFacing=state.facing;elapsed=state.elapsed;shotTimer=state.shotTimer;aim=state.aim;
        }
        void AimAt(Vector3 target)
        {
            var local=transform.InverseTransformDirection(target-rig.gunElevation.position);rig.targetYaw=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
            rig.targetElevation=Mathf.Clamp(Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg,-12,55);
        }
        void Drive(Vector3 goal,float dt)
        {
            if(!groundMotor){Fail("Robot ground motor missing");return;}
            Vector3 delta=Vector3.ProjectOnPlane(goal-transform.position,Vector3.up);
            if(delta.magnitude<.3f&&groundMotor.Grounded&&Mathf.Abs(goal.y-transform.position.y)<2.5f){Next();return;}
            float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(transform.forward,Vector3.up),delta,Vector3.up);
            float speed=Mathf.Abs(angle)<50?driveSpeed:0;
            groundMotor.SetDrive(speed,Mathf.Clamp(angle*3,-turnSpeed,turnSpeed));
            if(speed>.1f&&groundMotor.Grounded&&Vector3.ProjectOnPlane(groundMotor.body.linearVelocity,Vector3.up).magnitude<.08f)blocked+=dt;else blocked=0;
            if(blocked>4){Fail("Drive path blocked");return;}
            if(hasFacing)AimAt(aim);
        }
        void OnDisable(){if(groundMotor)groundMotor.Stop();}
        float tracerRestWidth=-1;
        void Shoot()
        {
            GameAudio.Play(SoundCue.Shot,muzzle.position);ShotCount++;flashTime=.055f;var start=muzzle.position;var direction=muzzle.forward;Vector3 end=start+direction*35;float nearest=35;CircuitPracticeTarget target=null;GhostEnemy enemy=null;GhostSpawner spawner=null;
            foreach(var hit in Physics.RaycastAll(start,direction,35,~(1<<30),QueryTriggerInteraction.Collide))if(!hit.collider.transform.IsChildOf(transform)&&hit.distance<nearest){nearest=hit.distance;end=hit.point;target=hit.collider.GetComponentInParent<CircuitPracticeTarget>();enemy=hit.collider.GetComponentInParent<GhostEnemy>();spawner=hit.collider.GetComponentInParent<GhostSpawner>();}
            if(target)target.Hit(Ammo);
            if(enemy)enemy.Damage(1);
            if(spawner)spawner.Damage(1);
            Color color=Ammo=="ACID"?Color.green:Ammo=="THUNDER"?Color.cyan:Ammo=="AP"?Color.white:Ammo=="BURST"?new Color(1,.3f,.15f):new Color(1,.75f,.25f);
            if(tracer){
                if(tracerRestWidth<0)tracerRestWidth=tracer.widthMultiplier;
                var overlay=DroneMissionOverlay.Current;
                bool engaged=overlay&&overlay.defense&&overlay.defense.Active;
                tracer.widthMultiplier=engaged&&BeaconDroneStation.FeedCamera?Mathf.Max(tracerRestWidth,.22f):tracerRestWidth;
                tracer.startColor=tracer.endColor=engaged?new Color(1,.015f,.025f):color;tracer.SetPosition(0,start);tracer.SetPosition(1,end);tracer.enabled=true;}
            if(muzzleFlash){muzzleFlash.color=color;muzzleFlash.enabled=true;}
        }
    }
}





