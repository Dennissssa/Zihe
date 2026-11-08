using UnityEngine;
namespace Mach
{
    // Authored parts rig; previews the mechanisms without creating objects or controlling the player.
    public sealed class RobotAssemblyRig : MonoBehaviour
    {
        public Transform turretYaw,gunElevation,tabletSocket;
        public RobotTrackModule leftTrack,rightTrack;
        [Range(-180,180)]public float targetYaw;
        [Range(-12,55)]public float targetElevation;
        public float yawSpeed=65,elevationSpeed=40;
        [Tooltip("Animate the tracks. RobotGroundMotor supplies actual movement speeds during physical driving.")]
        public bool previewTransmission;
        [Range(-1,1)]public float forwardCommand,steeringCommand;
        public float beltSpeed=1.4f,steeringMix=1;
        public float LeftBeltSpeed {get;private set;}
        public float RightBeltSpeed {get;private set;}
        float yaw,elevation;
        public void Advance(float dt)
        {
            yaw=Mathf.MoveTowardsAngle(yaw,targetYaw,yawSpeed*dt);elevation=Mathf.MoveTowards(elevation,Mathf.Clamp(targetElevation,-12,55),elevationSpeed*dt);
            turretYaw.localRotation=Quaternion.Euler(0,yaw,0);gunElevation.localRotation=Quaternion.Euler(-elevation,0,0);
            // Common drive plus/minus steering flow: equal belts straight, opposing belts neutral turn.
            LeftBeltSpeed=previewTransmission?(forwardCommand+steeringCommand*steeringMix)*beltSpeed:0;
            RightBeltSpeed=previewTransmission?(forwardCommand-steeringCommand*steeringMix)*beltSpeed:0;
            leftTrack.Advance(LeftBeltSpeed*dt);rightTrack.Advance(RightBeltSpeed*dt);
        }
        void Update()=>Advance(Time.deltaTime);
    }
}
