using UnityEngine;
namespace Mach
{
 // A stage performance only: no movement controller, raycasts, damage or battle events.
 public sealed class MenuRobotPerformance:MonoBehaviour
 {
  public Transform turret,gun,muzzle;public RobotTrackModule leftTrack,rightTrack;
  public LineRenderer tracer;public Light flash;public TextMesh status;
  public float yawSpeed=150,elevationSpeed=110;
  public Vector2 yawRange=new Vector2(-65,65),elevationRange=new Vector2(-10,40);
  public float[] shotsPerSecond={2,4.55f,10};
  public Color[] ammoColors={new Color(1,.75f,.25f),Color.white,new Color(1,.3f,.15f),Color.green,Color.cyan};
  readonly string[] ammoNames={"JHP","AP","BURST","ACID","THUNDER"};readonly string[] rates={"LOW","MID","HIGH"};
  Quaternion turretRest,gunRest;Vector3 gunPosition;float yaw,pitch,targetYaw,targetPitch,decision,shotTimer,flashTimer,recoil,forward,steer;int ammo,rate;bool firing;
  public int Shots {get;private set;}
  void Awake(){turretRest=turret.localRotation;gunRest=gun.localRotation;gunPosition=gun.localPosition;}
  void Update()
  {
   float dt=Time.unscaledDeltaTime;decision-=dt;
   if(decision<=0){decision=Random.Range(.65f,2.2f);targetYaw=Random.Range(yawRange.x,yawRange.y);targetPitch=Random.Range(elevationRange.x,elevationRange.y);ammo=Random.Range(0,5);rate=Random.Range(0,3);firing=Random.value>.2f;forward=Random.Range(-1f,1f);steer=Random.Range(-1f,1f);if(status)status.text=ammoNames[ammo]+" / "+rates[rate];}
   yaw=Mathf.MoveTowardsAngle(yaw,targetYaw,yawSpeed*dt);pitch=Mathf.MoveTowards(pitch,targetPitch,elevationSpeed*dt);turret.localRotation=turretRest*Quaternion.Euler(0,yaw,0);gun.localRotation=gunRest*Quaternion.Euler(-pitch,0,0);
   if(leftTrack)leftTrack.Advance((forward+steer)*dt*1.4f);if(rightTrack)rightTrack.Advance((forward-steer)*dt*1.4f);
   recoil=Mathf.MoveTowards(recoil,0,dt*.6f);gun.localPosition=gunPosition-gun.localRotation*Vector3.forward*recoil;shotTimer-=dt;flashTimer-=dt;
   if(firing&&shotTimer<=0){shotTimer=1/Mathf.Max(1,shotsPerSecond[rate]);flashTimer=.045f;recoil=.025f;Shots++;tracer.startColor=tracer.endColor=ammoColors[ammo];flash.color=ammoColors[ammo];}
   tracer.enabled=flash.enabled=flashTimer>0;if(tracer.enabled){tracer.SetPosition(0,muzzle.position);tracer.SetPosition(1,muzzle.position+muzzle.forward*Random.Range(3,6));}
  }
  void OnDisable(){if(tracer)tracer.enabled=false;if(flash)flash.enabled=false;}
 }
}
