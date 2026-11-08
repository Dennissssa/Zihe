using UnityEngine;
namespace Mach
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RobotGroundMotor : MonoBehaviour
    {
        public Rigidbody body;
        public RobotAssemblyRig rig;
        public float acceleration=8,braking=12,turnAcceleration=8,groundAlignStrength=18,groundAlignDamping=7,maxSlope=48;
        public float probeHeight=.55f,probeDistance=.85f;
        public LayerMask groundMask=~(1<<30);
        public bool Grounded {get;private set;}
        public Vector3 GroundNormal {get;private set;}=Vector3.up;
        float speed,turn;
        public void SetDrive(float metresPerSecond,float degreesPerSecond){speed=metresPerSecond;turn=degreesPerSecond;}
        public void Stop()=>SetDrive(0,0);
        void Awake(){if(!body)body=GetComponent<Rigidbody>();}
        public void ResetForLesson(Vector3 position,Quaternion rotation)
        {
            if(!body)body=GetComponent<Rigidbody>();
            Stop();Grounded=false;GroundNormal=Vector3.up;
            // Teleport the physics body as well as its Transform. In particular, do
            // not carry an interpolated cargo/editor pose into the new lesson.
            var interpolation=body.interpolation;body.interpolation=RigidbodyInterpolation.None;
            body.isKinematic=false;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
            body.position=position;body.rotation=rotation;transform.SetPositionAndRotation(position,rotation);
            body.interpolation=interpolation;body.WakeUp();
        }
        void FixedUpdate()=>Step(Time.fixedDeltaTime);
        public void Step(float dt)
        {
            if(!body||body.isKinematic)return;
            Vector3 normal=Vector3.zero;int hits=0;
            foreach(float x in new[]{-.8f,.8f})foreach(float z in new[]{-.65f,.65f})
            {
                var origin=body.position+body.rotation*Vector3.Scale(new Vector3(x,probeHeight,z),transform.lossyScale);float nearest=probeDistance;
                Vector3 found=Vector3.zero;
                foreach(var hit in Physics.RaycastAll(origin,Vector3.down,probeDistance,groundMask,QueryTriggerInteraction.Ignore))
                    if(hit.rigidbody!=body&&!hit.collider.transform.IsChildOf(transform)&&hit.distance<nearest&&Vector3.Angle(hit.normal,Vector3.up)<=maxSlope){nearest=hit.distance;found=hit.normal;}
                if(found!=Vector3.zero){normal+=found;hits++;}
            }
            Grounded=hits>=2;if(Grounded)GroundNormal=normal.normalized;
            if(Grounded)
            {
                var forward=Vector3.ProjectOnPlane(body.rotation*Vector3.forward,GroundNormal).normalized;
                var tangentVelocity=Vector3.ProjectOnPlane(body.linearVelocity,GroundNormal);
                var force=Vector3.ClampMagnitude((forward*speed-tangentVelocity)/Mathf.Max(dt,.001f),Mathf.Abs(speed)>.01f?acceleration:braking);
                // Traction cancels downhill gravity; gravity normal to the slope
                // and collision contacts still support the body naturally.
                body.AddForce(force-Vector3.ProjectOnPlane(Physics.gravity,GroundNormal),ForceMode.Acceleration);
                float yaw=Vector3.Dot(body.angularVelocity,GroundNormal);
                body.AddTorque(GroundNormal*Mathf.Clamp((turn*Mathf.Deg2Rad-yaw)/Mathf.Max(dt,.001f),-turnAcceleration,turnAcceleration),ForceMode.Acceleration);
                var tilt=Vector3.Cross(body.rotation*Vector3.up,GroundNormal)*groundAlignStrength-Vector3.ProjectOnPlane(body.angularVelocity,GroundNormal)*groundAlignDamping;
                body.AddTorque(Vector3.ClampMagnitude(tilt,20),ForceMode.Acceleration);
            }
            if(rig)
            {
                rig.previewTransmission=true;
                rig.forwardCommand=Grounded?Vector3.Dot(body.linearVelocity,body.rotation*Vector3.forward)/Mathf.Max(.01f,rig.beltSpeed):0;
                rig.steeringCommand=Grounded?Vector3.Dot(body.angularVelocity,GroundNormal)*.8f/Mathf.Max(.01f,rig.beltSpeed):0;
            }
        }
        void OnDisable()=>Stop();
    }
}

