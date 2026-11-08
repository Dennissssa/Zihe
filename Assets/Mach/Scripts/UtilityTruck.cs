using UnityEngine;
namespace Mach
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class UtilityTruck : MonoBehaviour
    {
        public WheelCollider[] wheels;
        public Transform[] wheelVisuals;
        public TruckHingedPanel driverDoor,passengerDoor,tailgate;
        public Transform driverAnchor,leftExit,rightExit,steeringWheel;
        public Transform passengerAnchor;
        public TruckDriver Passenger {get;private set;}
        public bool parkingBrake;
        [Min(1)] public float fuelCapacityGallons=100f;
        public int CurrentGear {get;private set;}=1;
        public float[] gearRatios={3.2f,2.1f,1.45f,1f};
        public float finalDrive=3.7f, idleRpm=800, redlineRpm=8000, upshiftRpm=5500, downshiftRpm=2200;
        public float shiftDuration=.3f;
        [Header("Four-speed automatic (mph)")]
        public Vector3 upshiftSpeedsMph=new Vector3(20,45,70);
        [Min(0)] public float downshiftHysteresisMph=3;
        public float rpmAfterUpshift=2200;
        float shiftRemaining;
        [Header("Automatic stop after driver exits")]
        [Min(0)] public float exitCoastSeconds=.65f;
        [Min(1)] public float unattendedDeceleration=14;
        float coastRemaining,groundContactUntil;
        bool stoppingAfterExit;
        [Header("Collision sound")]
        [Min(0)] public float minimumImpactSpeed=.8f;
        [Min(0)] public float minimumImpactDeltaVelocity=.3f;
        [Min(1)] public float fullImpactStrength=12f;
        [Min(.05f)] public float impactSoundInterval=.22f;
        [Range(0,2)] public float impactSoundVolume=1.15f;
        float nextImpactSound,pendingImpact;
        Vector3 pendingImpactPoint;
        public float EngineRPM {get;private set;}
        public bool EngineRunning=>Driver;
        public void SetPassenger(TruckDriver passenger)=>Passenger=passenger;
        public float motorTorque=1150, brakeTorque=4200, maxSpeed=18, maxSteer=31;
        [Min(.1f)] public float steeringLockSeconds=.5f;
        [Min(.1f)] public float steeringReturnSeconds=.75f;
        public float steeringWheelLockDegrees=270f;
        public float SteeringInput {get;private set;}
        public TruckDriver Driver {get;private set;}
        public Rigidbody Body=>GetComponent<Rigidbody>();
        public float Speed=>Body.linearVelocity.magnitude;
        public float SignedSpeed=>Vector3.Dot(Body.linearVelocity,transform.forward);
        public bool ServiceBrakeApplied=>parkingBrake||(Driver&&(brake||(Mathf.Abs(SignedSpeed)>.75f&&throttle*SignedSpeed<0)));
        float throttle,steer;bool brake;
        Quaternion steeringRest;
        float[] wheelRoll;
        float previousSteering;
        void Awake(){Body.centerOfMass=new Vector3(0,.65f,-.15f);steeringRest=steeringWheel?steeringWheel.localRotation:Quaternion.identity;wheelRoll=new float[wheels.Length];}
        public void SetDriver(TruckDriver driver)
        {
            if(Driver!=driver)GameAudio.Play(driver?SoundCue.EngineStart:SoundCue.EngineStop,transform.position);
            bool leaving=Driver&&!driver;
            Driver=driver;
            if(leaving){stoppingAfterExit=true;coastRemaining=parkingBrake?0:exitCoastSeconds;}
            if(driver){stoppingAfterExit=false;coastRemaining=0;}
            SetInputs(0,0,driver!=null);
        }
        public void SetInputs(float power,float turn,bool stop){throttle=Mathf.Clamp(power,-1,1);steer=Mathf.Clamp(turn,-1,1);brake=stop;}
        void FixedUpdate()=>DriveStep(Time.fixedDeltaTime);
        public void DriveStep(float dt)
        {
            coastRemaining=Mathf.Max(0,coastRemaining-dt);
            bool coasting=!Driver&&stoppingAfterExit&&coastRemaining>0&&!parkingBrake;
            bool stopping=brake || parkingBrake || (!Driver&&!coasting) || (Mathf.Abs(SignedSpeed)>.75f && throttle*SignedSpeed<0);
            shiftRemaining=Mathf.Max(0,shiftRemaining-dt);
            float mph=Mathf.Abs(SignedSpeed)*2.236936f;
            float first=Mathf.Max(1,upshiftSpeedsMph.x);
            float second=Mathf.Max(first+1,upshiftSpeedsMph.y);
            float third=Mathf.Max(second+1,upshiftSpeedsMph.z);
            CurrentGear=Mathf.Clamp(CurrentGear,1,4);
            if(EngineRunning&&shiftRemaining<=0)
            {
                float up=CurrentGear==1?first:CurrentGear==2?second:third;
                float down=CurrentGear==2?first:CurrentGear==3?second:third;
                if(SignedSpeed<0||mph<1)CurrentGear=1;
                else if(CurrentGear<4&&mph>=up){CurrentGear++;shiftRemaining=Mathf.Max(.1f,shiftDuration);}
                else if(CurrentGear>1&&mph<down-downshiftHysteresisMph){CurrentGear--;shiftRemaining=Mathf.Max(.1f,shiftDuration);}
            }
            float lower=CurrentGear==1?0:CurrentGear==2?first:CurrentGear==3?second:third;
            float upper=CurrentGear==1?first:CurrentGear==2?second:CurrentGear==3?third:Mathf.Max(third+1,maxSpeed*2.236936f);
            float progress=Mathf.InverseLerp(lower,upper,mph);
            float rpmTarget=EngineRunning?Mathf.Lerp(CurrentGear==1?idleRpm:rpmAfterUpshift,CurrentGear==4?redlineRpm*.95f:upshiftRpm,progress):0;
            float ratio=CurrentGear==1?1:CurrentGear==2?.66f:CurrentGear==3?.45f:.32f;
            float torque=!stopping&&EngineRunning&&Speed<maxSpeed?throttle*motorTorque*ratio*(shiftRemaining>0?.25f:1):0;
            // One RPM signal drives both the mechanical gauge and engine audio.
            float rpmRate=shiftRemaining>0?Mathf.Abs(upshiftRpm-rpmAfterUpshift)/Mathf.Max(.1f,shiftDuration):4500;
            EngineRPM=Mathf.MoveTowards(EngineRPM,rpmTarget,dt*rpmRate);
            previousSteering=SteeringInput;
            SteeringInput=Mathf.MoveTowards(SteeringInput,steer,dt/Mathf.Max(.1f,Mathf.Abs(steer)<.01f?steeringReturnSeconds:steeringLockSeconds));
            float angle=SteeringInput*Mathf.Lerp(maxSteer,maxSteer*.42f,Mathf.Clamp01(Speed/maxSpeed));
            for(int i=0;i<wheels.Length;i++)
            {
                wheels[i].motorTorque=torque;wheels[i].brakeTorque=stopping?brakeTorque:coasting?0:Mathf.Abs(throttle)<.01f?100:0;
                wheels[i].steerAngle=i<2?angle:0;
            }
            if(!Driver&&stoppingAfterExit&&!coasting){
                bool grounded=Time.fixedTime<groundContactUntil;
                foreach(var wheel in wheels)if(wheel&&wheel.isGrounded){grounded=true;break;}
                // Wheel brakes alone cannot stop an overturned chassis sliding.
                // Apply ground friction only while supported, never freeze it in air.
                if(grounded&&!Body.isKinematic){
                    Body.linearVelocity=Vector3.MoveTowards(Body.linearVelocity,Vector3.zero,unattendedDeceleration*dt);
                    Body.angularVelocity=Vector3.MoveTowards(Body.angularVelocity,Vector3.zero,5*dt);
                    if(Body.linearVelocity.sqrMagnitude<.01f&&Body.angularVelocity.sqrMagnitude<.01f)Body.Sleep();
                }
            }
        }
        void OnCollisionStay(Collision collision){foreach(var contact in collision.contacts)if(contact.normal.y>.15f){groundContactUntil=Time.fixedTime+.15f;break;}}
        void OnCollisionEnter(Collision collision)
        {
            if(Time.time<nextImpactSound||collision.contactCount==0||!collision.collider||collision.collider.transform.IsChildOf(transform))return;
            float normalSpeed=0;Vector3 point=collision.GetContact(0).point;
            for(int i=0;i<collision.contactCount;i++){
                var contact=collision.GetContact(i);
                float speed=Mathf.Abs(Vector3.Dot(collision.relativeVelocity,contact.normal));
                if(speed>normalSpeed){normalSpeed=speed;point=contact.point;}
            }
            // Impulse / mass is the truck's impact-induced change in velocity.
            // Both gates reject resting contacts and fast but shallow glancing hits.
            float deltaVelocity=collision.impulse.magnitude/Mathf.Max(1,Body.mass);
            if(normalSpeed<minimumImpactSpeed||deltaVelocity<minimumImpactDeltaVelocity)return;
            float strength=Mathf.Sqrt(normalSpeed*deltaVelocity);
            // Compound colliders can report multiple impacts in one frame: keep one.
            if(strength>pendingImpact){pendingImpact=strength;pendingImpactPoint=point;}
        }
        void LateUpdate()
        {
            if(pendingImpact>0){
                if(Time.time>=nextImpactSound&&GameAudio.Instance){
                    GameAudio.Instance.PlayVehicleImpact(pendingImpactPoint,Mathf.Clamp01(pendingImpact/Mathf.Max(1,fullImpactStrength)),impactSoundVolume);
                    nextImpactSound=Time.time+impactSoundInterval;
                }
                pendingImpact=0;
            }
            float blend=Mathf.Clamp01((Time.time-Time.fixedTime)/Time.fixedDeltaTime);
            float visualSteer=Mathf.Lerp(previousSteering,SteeringInput,blend);
            if(steeringWheel)steeringWheel.localRotation=steeringRest*Quaternion.AngleAxis(visualSteer*steeringWheelLockDegrees,Vector3.up);
            for(int i=0;i<wheels.Length;i++)if(wheelVisuals[i])
            {
                wheels[i].GetWorldPose(out var position,out _);
                wheelRoll[i]=Mathf.Repeat(wheelRoll[i]+wheels[i].rpm*6f*Time.deltaTime,360);
                float angle=i<2?visualSteer*Mathf.Lerp(maxSteer,maxSteer*.42f,Mathf.Clamp01(Speed/maxSpeed)):0;
                // Tire geometry is built around its local X axle. Y steers; X rolls.
                wheelVisuals[i].SetPositionAndRotation(position,wheels[i].transform.rotation*Quaternion.AngleAxis(angle,Vector3.up)*Quaternion.AngleAxis(wheelRoll[i],Vector3.right));
            }
        }
    }
}

