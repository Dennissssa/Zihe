using UnityEngine;
namespace Mach
{
    public sealed class TruckHingedPanel : MonoBehaviour
    {
        public Vector3 axis=Vector3.up;
        public float openAngle=78, smoothTime=.16f;
        public bool IsOpen {get;private set;}
        public float Angle {get;private set;}
        float velocity;
        Collider[] surfaces;
        Rigidbody truckBody;
        bool suspended;
        void Awake(){surfaces=GetComponentsInChildren<Collider>(true);truckBody=GetComponentInParent<Rigidbody>();}
        void SuspendCollision(){if(surfaces==null)Awake();foreach(var c in surfaces)if(c)c.isTrigger=true;suspended=true;}
        public void SetOpen(bool value){if(value!=IsOpen){SuspendCollision();GameAudio.Play(value?SoundCue.DoorOpen:SoundCue.DoorClose,transform.position);}IsOpen=value;}
        public void Toggle()=>SetOpen(!IsOpen);
        public void Animate(float dt)
        {
            Angle=Mathf.SmoothDampAngle(Angle,IsOpen?openAngle:0,ref velocity,smoothTime,Mathf.Infinity,dt);
            transform.localRotation=Quaternion.AngleAxis(Angle,axis);
            if(suspended&&Mathf.Abs(Mathf.DeltaAngle(Angle,IsOpen?openAngle:0))<.05f)
            {
                // Do not restore solid collision inside a player or cargo when the panel settles.
                foreach(var c in surfaces)
                {
                    if(!c||!c.enabled)continue;
                    var bounds=c.bounds;
                    foreach(var other in Physics.OverlapBox(bounds.center,bounds.extents+Vector3.one*.025f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                    {
                        if(other.transform.IsChildOf(transform)||other.attachedRigidbody==truckBody)continue;
                        if(Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out float depth)&&depth>.001f)return;
                    }
                }
                foreach(var c in surfaces)if(c)c.isTrigger=false;suspended=false;
            }
        }
        void Update()=>Animate(Time.deltaTime);
    }
}

