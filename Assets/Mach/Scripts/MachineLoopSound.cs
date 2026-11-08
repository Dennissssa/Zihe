using UnityEngine;
namespace Mach
{
    public enum LoopSoundKind {Aircraft,Truck,Robot,Airdrop}
    public sealed class MachineLoopSound : MonoBehaviour
    {
        public LoopSoundKind kind;public AudioSource motor,servo;
        public UtilityTruck truck;public RobotCircuitRunner robot;public EquipmentAirdrop drop;
        Quaternion lastTurret,lastElevation;
        void OnEnable(){if(robot&&robot.rig){lastTurret=robot.rig.turretYaw.localRotation;lastElevation=robot.rig.gunElevation.localRotation;}}
        void Update()
        {
            var bank=GameAudio.Instance;if(!bank)return;float amount=0,pitch=1,servoAmount=0;AudioClip clip=null;
            switch(kind)
            {
                case LoopSoundKind.Aircraft:clip=bank.aircraftLoop;amount=.95f;pitch=1f;break;
                case LoopSoundKind.Truck:
                    clip=bank.truckLoop;
                    float revs=truck?Mathf.InverseLerp(truck.idleRpm,truck.redlineRpm,truck.EngineRPM):0;
                    amount=truck&&truck.EngineRunning?.40f+revs*.56f:0;
                    // Follow the existing four-speed gearbox: an upshift lowers
                    // RPM (and pitch) instead of rising continuously with road speed.
                    pitch=Mathf.Lerp(.85f,1.55f,revs);break;
                case LoopSoundKind.Robot:
                    clip=bank.robotLoop;
                    if(robot&&robot.groundMotor){var carry=GetComponent<MovableMachine>();if(carry&&carry.IsPreview)break;float motion=robot.groundMotor.body.linearVelocity.magnitude;amount=robot.Running?.07f:0;amount+=Mathf.Clamp01(motion/2)*.4f;pitch=.8f+Mathf.Clamp01(motion/3)*.7f;
                    float turn=Quaternion.Angle(lastTurret,robot.rig.turretYaw.localRotation)+Quaternion.Angle(lastElevation,robot.rig.gunElevation.localRotation);servoAmount=Mathf.Clamp01(turn/Mathf.Max(.001f,Time.deltaTime)/65)*.25f;lastTurret=robot.rig.turretYaw.localRotation;lastElevation=robot.rig.gunElevation.localRotation;}break;
                case LoopSoundKind.Airdrop:clip=bank.windLoop;amount=drop&&drop.Released&&!drop.Landed?.36f:0;break;
            }
            Loop(motor,clip,amount*bank.masterVolume*bank.loopVolume,pitch);
            Loop(servo,bank.servoLoop,servoAmount*bank.masterVolume*bank.loopVolume,1);
        }
        static void Loop(AudioSource source,AudioClip clip,float volume,float pitch)
        {
            if(!source||!clip)return;if(source.clip!=clip)source.clip=clip;source.loop=true;source.pitch=Mathf.Lerp(source.pitch,pitch,1-Mathf.Exp(-5*Time.deltaTime));source.volume=Mathf.MoveTowards(source.volume,volume,Time.deltaTime*1.5f);
            if(source.volume>.001f){if(!source.isPlaying)source.Play();}else if(source.isPlaying)source.Stop();
        }
        void OnDisable(){if(motor)motor.Stop();if(servo)servo.Stop();}
    }
}


