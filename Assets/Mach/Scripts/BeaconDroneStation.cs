using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Mach
{
    [DefaultExecutionOrder(100)]
    public sealed class BeaconDroneStation : MonoBehaviour
    {
        public Transform drone, berth, cameraMount;
        public Transform[] rotors;
        public GameObject hud;
        public Text beaconLabel;
        public Text telemetryLabel, controlLabel;
        public static Camera FeedCamera {get;private set;}
        Vector3 lastSamplePosition, lastSampleVelocity;
        float sampleTime, groundSpeed, accelerationReading, altitude;
        float signalStart;
        Vector3 measuredVelocity;
        static readonly int SignalAge = Shader.PropertyToID("_MachDroneSignalAge");
        public float horizontalSpeed=22,verticalSpeed=10,acceleration=28,collisionRadius=.38f,maxHeight=90;
        public GameObject flightHud, dropHud;
        public AudioSource rotorAudio;
        [Range(0,1)] public float hoverVolume=.30f;
        float previousAirSpeed, motorLoad, rotorAngularSpeed;
        DroneViewfinder pod;
        void Awake(){if(dropHud)pod=dropHud.GetComponent<DroneViewfinder>();}
        void LateUpdate()
        {
            if(!rotorAudio)return;
            float dt=Time.unscaledDeltaTime;
            bool running=airborne && Time.timeScale>0;
            float speed=measuredVelocity.magnitude;
            float speedChange=(speed-previousAirSpeed)/Mathf.Max(Time.deltaTime,.001f);
            previousAirSpeed=speed;
            float demand=.24f*Mathf.Clamp01(speed/Mathf.Max(1,horizontalSpeed))
                +.23f*Mathf.Clamp(measuredVelocity.y/Mathf.Max(1,verticalSpeed),-1,1)
                +.18f*Mathf.Clamp(speedChange/Mathf.Max(1,acceleration),-1,1);
            motorLoad=Mathf.Lerp(motorLoad,demand,1-Mathf.Exp(-7*dt));
            rotorAudio.pitch=Mathf.MoveTowards(rotorAudio.pitch,running?1+motorLoad:.7f,1.7f*dt);
            rotorAudio.volume=Mathf.MoveTowards(rotorAudio.volume,running?Mathf.Clamp01(hoverVolume*(1+motorLoad)):0,dt*1.4f);
            if(running&&!rotorAudio.isPlaying){rotorAudio.spatialBlend=1;rotorAudio.Play();}
            if(!running&&rotorAudio.volume<=0&&rotorAudio.isPlaying)rotorAudio.Stop();
        }
        public float returnClearance=8;
        readonly List<Vector3> trail = new List<Vector3>();
        readonly Queue<Vector3> route = new Queue<Vector3>();
        bool returning, blocked;
        float stalled;
        public Transform dropIndicator;
        public Renderer[] indicatorSurfaces;
        public Renderer topSignal;
        [Min(.1f)] public float signalInterval=.6f;
        MaterialPropertyBlock signalProperties,indicatorProperties;
        public bool Controlling {get;private set;}
        public bool InDropView => Controlling && bombView;
        public event System.Action ReturnedToPad;
        PlayerPickup pilot;BehaviorProgram program;Camera camera;
        Transform oldCameraParent;Vector3 oldCameraPosition;Quaternion oldCameraRotation;
        readonly List<Behaviour> suspended=new List<Behaviour>();
        readonly List<GameObject> hidden=new List<GameObject>();
        Vector3 velocity;float yaw,pitch,dropCooldown;int selected,enteredFrame;bool airborne,bombView;
        public string Prompt=>GamePreferences.Chinese?"[E] 操控信标无人机":"[E] Control beacon drone";
        [System.Serializable] public class FlightSnapshot {
            public bool airborne,controlling,returning,dropView; public int selected;
            public Vector3 position,velocity; public Quaternion rotation; public float yaw,pitch;
            public Vector3[] trail;
        }
        public FlightSnapshot CaptureFlight()=>new FlightSnapshot {
            airborne=airborne,controlling=Controlling,returning=returning,dropView=bombView,selected=selected,
            position=drone.position,rotation=drone.rotation,velocity=velocity,yaw=yaw,pitch=pitch,trail=trail.ToArray()
        };
        public void RestoreFlight(FlightSnapshot saved,PlayerPickup player){
            if(saved==null)return;
            selected=saved.selected;if(!saved.airborne)return;
            drone.SetParent(null,true);drone.SetPositionAndRotation(saved.position,saved.rotation);airborne=true;
            trail.Clear();if(saved.trail!=null)trail.AddRange(saved.trail);if(trail.Count==0)trail.Add(berth.position);
            if(saved.controlling)Begin(player);
            yaw=saved.yaw;pitch=saved.pitch;velocity=saved.velocity;bombView=saved.dropView;returning=saved.returning;
            if(returning)PlanReturn();if(Controlling){cameraMount.localRotation=Quaternion.Euler(bombView?90:pitch,0,0);RefreshHUD();}
        }
        public void Begin(PlayerPickup player)
        {
            if(Controlling||!player||player.HeldItem||player.truckDriver.CurrentTruck||player.placement.IsPlacing)return;
            pilot=player;program=player.GetComponent<BehaviorProgram>();camera=player.viewCamera;
            if(!program||!camera||program.Editing)return;
            var truck=GetComponentInParent<UtilityTruck>();if(truck&&truck.Speed>.5f)return;
            var launcher=player.GetComponent<PointLauncher>();if(launcher)launcher.SetRaised(false);
            program.tablet.SetRaised(false);program.remote.SetRaised(false);
            oldCameraParent=camera.transform.parent;oldCameraPosition=camera.transform.localPosition;oldCameraRotation=camera.transform.localRotation;
            Hide(program.tablet.poseRoot.gameObject);Hide(program.remote.panel.gameObject);if(launcher)Hide(launcher.poseRoot.gameObject);
            Suspend(player);Suspend(program.walker);Suspend(player.placement);Suspend(program.tablet);Suspend(program.remote);Suspend(launcher);
            Suspend(player.GetComponent<PointHeightDrag>());Suspend(program);
            if(!airborne){trail.Clear();trail.Add(berth.position);blocked=false;drone.SetParent(null,true);airborne=true;}
            yaw=drone.eulerAngles.y;pitch=12;bombView=false;if(!returning)velocity=Vector3.zero;
            camera.transform.SetParent(cameraMount,false);camera.transform.localPosition=Vector3.zero;camera.transform.localRotation=Quaternion.identity;
            signalStart=Time.unscaledTime;Shader.SetGlobalFloat(SignalAge,0);measuredVelocity=Vector3.zero;
            Controlling=true;enteredFrame=Time.frameCount;hud.SetActive(true);if(pilot.prompt)pilot.prompt.text="";
            FeedCamera=camera;lastSamplePosition=drone.position;lastSampleVelocity=velocity;sampleTime=0;
            groundSpeed=accelerationReading=0;UpdateAltitude();
            PlayerLookInput.Discard();Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
            GameAudio.Play(SoundCue.Button,transform.position,true);RefreshHUD();
        }
        void Suspend(Behaviour b){if(b&&b.enabled){suspended.Add(b);b.enabled=false;}}
        void Hide(GameObject g){if(g&&g.activeSelf){hidden.Add(g);g.SetActive(false);}}
        public void End()
        {
            if(!Controlling)return;Controlling=false;if(!returning)velocity=Vector3.zero;
            if(FeedCamera==camera)FeedCamera=null;
            if(dropIndicator)dropIndicator.gameObject.SetActive(false);
            if(camera){camera.transform.SetParent(oldCameraParent,false);camera.transform.localPosition=oldCameraPosition;camera.transform.localRotation=oldCameraRotation;}
            foreach(var b in suspended)if(b)b.enabled=true;suspended.Clear();
            foreach(var g in hidden)if(g)g.SetActive(true);hidden.Clear();
            if(hud)hud.SetActive(false);PlayerLookInput.Discard();Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
        }
        void Update()
        {
            if(MissionPauseMenu.BlocksGameplay)return;
            UpdateSignal();
            if(Controlling)Shader.SetGlobalFloat(SignalAge,Time.unscaledTime-signalStart);
            measuredVelocity=Vector3.zero;
            if(returning && Time.timeScale>0) FlyHome();
            rotorAngularSpeed=Mathf.MoveTowards(rotorAngularSpeed,airborne?900+motorLoad*650:0,1800*Time.deltaTime);
            if(rotors!=null)
                for(int i=0;i<rotors.Length;i++)
                    if(rotors[i])rotors[i].Rotate(Vector3.up,rotorAngularSpeed*Time.deltaTime*(i==0||i==3?1:-1),Space.Self);
            if(!Controlling||Time.timeScale<=0)return;
            var k=Keyboard.current;var m=Mouse.current;if(k==null||m==null)return;
            if(Time.frameCount!=enteredFrame&&(k.eKey.wasPressedThisFrame||k.escapeKey.wasPressedThisFrame)){End();return;}
            if(Cursor.lockState!=CursorLockMode.Locked){PlayerLookInput.Discard();return;}
            var look=GamePreferences.Look(PlayerLookInput.Read(),pilot.GetComponent<FirstPersonWalker>().mouseSensitivity);
            yaw+=look.x;pitch=Mathf.Clamp(pitch-look.y,-75,75);
            if(k.cKey.wasPressedThisFrame)bombView=!bombView;
            drone.rotation=Quaternion.Euler(0,yaw,0);cameraMount.localRotation=Quaternion.Euler(bombView?90:pitch,0,0);
            if(k.rKey.wasPressedThisFrame)
            {
                returning=!returning; blocked=false; stalled=0;
                if(returning) PlanReturn();
            }
            if(returning){dropIndicator.gameObject.SetActive(false);if(pod)pod.SetImpact(false,Vector3.zero);RefreshHUD();return;}
            Vector2 axes=Vector2.ClampMagnitude(new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0)),1);
            float vertical=(k.leftShiftKey.isPressed||k.rightShiftKey.isPressed?1:0)-(k.leftCtrlKey.isPressed||k.rightCtrlKey.isPressed?1:0);
            var desired=(drone.right*axes.x+drone.forward*axes.y)*horizontalSpeed+Vector3.up*vertical*verticalSpeed;
            float dt=Mathf.Min(Time.deltaTime,.05f);velocity=Vector3.MoveTowards(velocity,desired,acceleration*dt);
            Vector3 destination=drone.position+velocity*dt;
            destination.y=Mathf.Min(destination.y,IslandAirspace.Active?IslandAirspace.Active.ceiling:berth.position.y+maxHeight);
            Move(destination-drone.position);
            if(Vector3.Distance(trail[trail.Count-1],drone.position)>.6f)trail.Add(drone.position);
            float scroll=m.scroll.ReadValue().y;if(scroll!=0&&program.pointMarkers.Length>0)selected=(selected+(scroll<0?1:-1)+program.pointMarkers.Length)%program.pointMarkers.Length;
            dropCooldown=Mathf.Max(0,dropCooldown-dt);
            if(m.rightButton.wasPressedThisFrame && program.pointMarkers.Length>0){
                var beacon=program.pointMarkers[selected];
                if(beacon.Recall(drone))GameAudio.Play(SoundCue.Pickup,drone.position,true);
            }
            if(m.leftButton.wasPressedThisFrame&&Time.frameCount!=enteredFrame)DropBeacon();
            UpdateDropIndicator();RefreshHUD();
        }
        bool ClearLeg(Vector3 from, Vector3 to)
        {
            Vector3 step=to-from;
            return step.sqrMagnitude<.001f || !Physics.SphereCast(from,collisionRadius+.02f,step.normalized,out _,step.magnitude,~(1<<30),QueryTriggerInteraction.Ignore);
        }
        void PlanReturn()
        {
            route.Clear();
            var here=drone.position;
            var home=berth.position;
            for(int level=1;level<=8;level++)
            {
                float height=Mathf.Max(here.y,home.y)+Mathf.Max(1,returnClearance)*level;
                if(IslandAirspace.Active)height=Mathf.Min(height,IslandAirspace.Active.ceiling);
                var climb=new Vector3(here.x,height,here.z);
                var approach=new Vector3(home.x,height,home.z);
                if(!ClearLeg(here,climb))break;
                if(!ClearLeg(climb,approach)||!ClearLeg(approach,home))continue;
                route.Enqueue(climb);route.Enqueue(approach);route.Enqueue(home+Vector3.up*2);return;
            }
            // Under a roof, retrace the route flown through the building.
            for(int i=trail.Count-1;i>=0;i--) route.Enqueue(trail[i]);
        }
        void FlyHome()
        {
            // Round clear corners before reaching the waypoint, including on a recorded trail.
            float lead=Mathf.Clamp(velocity.magnitude*.35f,.45f,4f);
            while(route.Count>0 && Vector3.Distance(drone.position,route.Peek())<lead)
            {
                var ahead=route.GetEnumerator();
                ahead.MoveNext();
                Vector3 next=ahead.MoveNext()?ahead.Current:berth.position;
                if(Vector3.Distance(drone.position,route.Peek())>.1f && !ClearLeg(drone.position,next))break;
                route.Dequeue();
            }
            bool landing=route.Count==0;
            Vector3 target=landing?berth.position:route.Peek();
            float distance=Vector3.Distance(drone.position,target);
            if(distance<.1f)
            {
                if(!landing){route.Dequeue();return;}
                returning=false;End();airborne=false;
                drone.SetParent(berth,false);drone.localPosition=Vector3.zero;drone.localRotation=Quaternion.identity;
                ReturnedToPad?.Invoke();
                return;
            }
            float dt=Mathf.Min(Time.deltaTime,.05f);
            Vector3 offset=target-drone.position;
            Vector3 horizontal=Vector3.ProjectOnPlane(offset,Vector3.up);
            float accel=Mathf.Max(.01f,acceleration);
            // Reserve braking distance on each axis while sharing the manual acceleration budget.
            float cruise=Mathf.Min(horizontalSpeed,Mathf.Sqrt(accel*horizontal.magnitude));
            float climb=Mathf.Min(verticalSpeed,Mathf.Sqrt(accel*Mathf.Abs(offset.y)));
            Vector3 desired=Vector3.ClampMagnitude(horizontal*3f,cruise)
                +Vector3.up*Mathf.Clamp(offset.y*3f,-climb,climb);
            velocity=Vector3.MoveTowards(velocity,desired,accel*dt);
            Move(velocity*dt);
            stalled=measuredVelocity.sqrMagnitude<.0025f?stalled+dt:0;
            if(stalled>1){returning=false;blocked=true;velocity=Vector3.zero;}
        }
        void UpdateSignal()
        {
            if(!topSignal)return;
            float phase=Mathf.Repeat(Time.time,Mathf.Max(.1f,signalInterval)*2);
            float interval=Mathf.Max(.1f,signalInterval);
            bool lit=Mathf.Repeat(phase,interval)<interval*.55f;
            Color c=phase<interval?new Color(1,.035f,.015f):new Color(.04f,1,.12f);
            if(signalProperties==null)signalProperties=new MaterialPropertyBlock();
            topSignal.GetPropertyBlock(signalProperties);
            signalProperties.SetColor("_BaseColor",lit?c:c*.12f);
            signalProperties.SetColor("_EmissionColor",lit?c*2.5f:Color.black);
            topSignal.SetPropertyBlock(signalProperties);
        }
        void UpdateDropIndicator()
        {
            if(!dropIndicator||program.pointMarkers.Length==0)return;
            var point=program.pointMarkers[selected];
            var origin=drone.position-Vector3.up*.45f;
            RaycastHit nearest=default;float distance=point.projectileSpeed*5;
            foreach(var hit in Physics.SphereCastAll(origin,.08f,Vector3.down,distance,~(1<<30),QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(drone)&&hit.distance<distance){nearest=hit;distance=hit.distance;}
            bool blocked=Physics.Linecast(drone.position,origin,out var obstruction,~(1<<30),QueryTriggerInteraction.Ignore)&&!obstruction.transform.IsChildOf(drone);
            bool visible=point.stored&&nearest.collider&&!blocked;
            if(pod)pod.SetImpact(visible && bombView,nearest.point);
            dropIndicator.gameObject.SetActive(visible && !bombView);if(!visible||bombView)return;
            dropIndicator.SetPositionAndRotation(nearest.point+nearest.normal*.025f,Quaternion.FromToRotation(Vector3.up,nearest.normal));
            if(indicatorProperties==null)indicatorProperties=new MaterialPropertyBlock();
            foreach(var surface in indicatorSurfaces)if(surface)
            {
                var c=point.beaconColor;c.a=surface.name.Contains("Fill")?.16f:.65f;
                surface.GetPropertyBlock(indicatorProperties);indicatorProperties.SetColor("_BaseColor",c);indicatorProperties.SetColor("_Color",c);surface.SetPropertyBlock(indicatorProperties);
            }
        }
        void Move(Vector3 step)
        {
            var start=drone.position;
            if(IslandAirspace.Active){
                var requested=start+step;var limited=IslandAirspace.Active.Constrain(requested);
                var blockedDirection=requested-limited;
                if(blockedDirection.sqrMagnitude>.00001f)velocity=Vector3.ProjectOnPlane(velocity,blockedDirection.normalized);
                step=limited-start;
            }
            // Sweep and slide against the authored world, including the truck roof.
            for(int pass=0;pass<3&&step.sqrMagnitude>.000001f;pass++)
            {
                float distance=step.magnitude;var direction=step/distance;RaycastHit nearest=default;float allowed=distance;
                foreach(var hit in Physics.SphereCastAll(drone.position,collisionRadius,direction,distance+.025f,~(1<<30),QueryTriggerInteraction.Ignore))
                    if(!hit.transform.IsChildOf(drone)&&hit.distance<allowed){nearest=hit;allowed=Mathf.Max(0,hit.distance-.025f);}
                drone.position+=direction*allowed;
                if(!nearest.collider)break;
                step=Vector3.ProjectOnPlane(step-direction*allowed,nearest.normal);velocity=Vector3.ProjectOnPlane(velocity,nearest.normal);
            }
            if(Time.deltaTime>0)measuredVelocity=(drone.position-start)/Time.deltaTime;
        }
        void DropBeacon()
        {
            if(dropCooldown>0||program.pointMarkers.Length==0)return;
            var marker=program.pointMarkers[selected];
            if(!marker.stored){GameAudio.Play(SoundCue.Error,drone.position,true);return;}
            var origin=drone.position-Vector3.up*.45f;
            if(Physics.Linecast(drone.position,origin,out var hit,~(1<<30),QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(drone)){GameAudio.Play(SoundCue.Error,drone.position,true);return;}
            if(!marker.Launch(origin,Vector3.down,drone))return;
            dropCooldown=.5f;GameAudio.Play(SoundCue.Release,drone.position,true);
            bool exists=false;foreach(var node in program.nodes)if(node.active&&node.kind==CircuitKind.Point&&node.pointId==marker.pointId)exists=true;
            if(!exists&&!program.Docked){int index=program.Add(CircuitKind.Point,marker.pointId,marker.Position);if(index>=0){program.nodes[index].pointId=marker.pointId;program.Save();}}
        }
        void RefreshHUD()
        {
            if(!beaconLabel||program.pointMarkers.Length==0)return;
            var point=program.pointMarkers[selected];bool cn=GamePreferences.Chinese;
            if(flightHud)flightHud.SetActive(!bombView);
            if(dropHud)dropHud.SetActive(bombView);
            string state=point.Recalling?(cn?"回收中":"RECALLING"):point.InFlight?(cn?"投放中":"DROPPING"):
                point.stored?(cn?"待投放":"READY"):(cn?"已部署":"DEPLOYED");
            beaconLabel.text=(cn?"信标 ":"POINT ")+point.pointId+"   /   "+state;
            controlLabel.text=(cn?"[右键]  回收信标":"[RMB]  RECALL BEACON")+"\n"+(cn?"[C]  切换视角":"[C]  SWITCH OPTIC")+"\n"+
                (returning?(cn?"[R]  取消返航":"[R]  CANCEL RETURN"):blocked?(cn?"[R]  重试返航":"[R]  RETRY RETURN"):(cn?"[R]  返航":"[R]  RETURN"));
            sampleTime+=Time.deltaTime;
            if(sampleTime>=.12f)
            {
                Vector3 sample=(drone.position-lastSamplePosition)/sampleTime;
                groundSpeed=sample.magnitude*3.6f;
                accelerationReading=(sample-lastSampleVelocity).magnitude/sampleTime;
                lastSampleVelocity=sample;lastSamplePosition=drone.position;sampleTime=0;
                UpdateAltitude();
            }
            string height=float.IsNaN(altitude)?"--":altitude.ToString("0.0");
            telemetryLabel.text=$"ALT {height} m    {(cn?"速度":"SPD")} {groundSpeed:0} km/h    {(cn?"加速度":"ACC")} {accelerationReading:0.0} m/s²";
        }
        void UpdateAltitude()
        {
            altitude=Physics.Raycast(drone.position,Vector3.down,out var ground,Mathf.Infinity,~(1<<30),QueryTriggerInteraction.Ignore)?ground.distance:float.NaN;
        }
        void OnApplicationFocus(bool focus){if(!focus)End();}
        void OnDisable(){returning=false;End();if(drone&&berth){drone.SetParent(berth,false);drone.localPosition=Vector3.zero;drone.localRotation=Quaternion.identity;airborne=false;}}
    }
}
