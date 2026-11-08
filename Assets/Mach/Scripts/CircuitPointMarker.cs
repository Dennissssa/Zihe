using UnityEngine;
namespace Mach
{
    public sealed class CircuitPointMarker : MonoBehaviour
    {
        public string pointId;
        public Color beaconColor=Color.cyan;
        [Min(.1f)] public float recallSeconds=1.2f;
        public bool Recalling=>recalling;
        public Transform heightHandle;
        public Transform laserBeam,laserCore;
        public float deploySeconds=.35f,projectileSpeed=24f;
        public bool InFlight {get;private set;}
        public bool stored;
        bool recalling;Transform recallTarget;float recallSpeed;
        public float Deployment {get;private set;}=1;
        Vector3 beamScale,coreScale,handleScale,launchPosition,flightDirection;
        Transform launchOwner;float flightTime;bool launched,initialized;
        public event System.Action<CircuitPointMarker,Transform,Transform> Landed;
        public float targetHeight=1.02f,minHeight=.12f,maxHeight=1.92f;
        Vector3 committedPosition;
        void OnEnable(){if(!initialized)committedPosition=transform.position;}
        void LateUpdate()=>Advance(Time.deltaTime);
        void Initialize()
        {
            if(initialized)return;initialized=true;
            if(laserBeam)beamScale=laserBeam.localScale;if(laserCore)coreScale=laserCore.localScale;if(heightHandle)handleScale=heightHandle.localScale;
        }
        public bool Launch(Vector3 origin,Vector3 direction,Transform owner)
        {
            var mover=GetComponent<MovableMachine>();if(!stored||InFlight||!mover||mover.IsPreview)return false;
            Initialize();gameObject.SetActive(true);stored=false;Deployment=0;
            mover.BeginPreview();mover.RestorePreviewAppearance();
            launchPosition=origin;flightDirection=direction.normalized;launchOwner=owner;flightTime=0;launched=false;InFlight=true;return true;
        }
        public void Advance(float dt)
        {
            if(stored&&!InFlight&&!recalling)return;
            Initialize();var mover=GetComponent<MovableMachine>();
            bool folded=InFlight||recalling||(mover&&mover.IsPreview);
            Deployment=Mathf.MoveTowards(Deployment,folded?0:1,dt/Mathf.Max(.05f,deploySeconds));
            float ease=Deployment*Deployment*(3-2*Deployment);
            Vector3 emitter=transform.position+transform.up*.12f;
            foreach(var beam in new[]{laserBeam,laserCore})if(beam){var scale=beam==laserBeam?beamScale:coreScale;beam.rotation=Quaternion.identity;beam.localScale=new Vector3(scale.x,scale.y*ease,scale.z);beam.position=emitter+Vector3.up*(scale.y*ease);beam.gameObject.SetActive(ease>.001f);}
            if(heightHandle){heightHandle.position=Vector3.Lerp(emitter,transform.position+Vector3.up*targetHeight,ease);heightHandle.localScale=handleScale*ease;var hit=heightHandle.GetComponent<Collider>();if(hit)hit.enabled=!folded&&Deployment>.98f;}
            if(recalling&&Deployment<=0){transform.position=Vector3.MoveTowards(transform.position,recallTarget.position,Mathf.Max(recallSpeed,Vector3.Distance(transform.position,recallTarget.position)/Mathf.Max(.1f,recallSeconds))*dt);if(Vector3.Distance(transform.position,recallTarget.position)<.1f){recalling=false;mover.EndPreview(true);stored=true;gameObject.SetActive(false);}return;}
            if(InFlight&&Deployment<=0)
            {
                if(!launched){transform.position=launchPosition;launched=true;}
                float distance=projectileSpeed*dt;RaycastHit nearest=default;float limit=distance;
                foreach(var hit in Physics.SphereCastAll(transform.position,.08f,flightDirection,distance,~(1<<30),QueryTriggerInteraction.Ignore))
                    if(!hit.collider.transform.IsChildOf(transform)&&(!launchOwner||!hit.collider.transform.IsChildOf(launchOwner))&&hit.distance<=limit){nearest=hit;limit=hit.distance;}
                if(nearest.collider){Attach(nearest.point,nearest.normal,nearest.collider.transform);}
                else{transform.position+=flightDirection*distance;flightTime+=dt;if(flightTime>5){InFlight=false;mover.EndPreview(false);stored=true;gameObject.SetActive(false);}}
            }
            if(!folded)committedPosition=transform.position;
        }
        public bool Recall(Transform destination)
        {
            var mover=GetComponent<MovableMachine>();if(stored||InFlight||recalling||mover.IsPreview)return false;
            if(!destination)return false;
            committedPosition=transform.position;recallTarget=destination;recallSpeed=Mathf.Max(projectileSpeed,Vector3.Distance(transform.position,destination.position)/Mathf.Max(.1f,recallSeconds));recalling=true;mover.BeginPreview();mover.RestorePreviewAppearance();return true;
        }
        public void Attach(Vector3 point,Vector3 normal,Transform surface)
        {
            var mover=GetComponent<MovableMachine>();transform.SetPositionAndRotation(point+normal*.006f,Quaternion.FromToRotation(Vector3.up,normal));
            InFlight=false;mover.EndPreview(true,surface);committedPosition=transform.position;
            Landed?.Invoke(this,launchOwner,surface);
        }
        public Vector3 Position {get{var mover=GetComponent<MovableMachine>();return (stored||(mover&&mover.IsPreview)?committedPosition:transform.position)+Vector3.up*targetHeight;}}
        public void SetHeight(float height)
        {
            targetHeight=Mathf.Clamp(height,minHeight,maxHeight);
            if(heightHandle)heightHandle.position=new Vector3(transform.position.x,transform.position.y+targetHeight,transform.position.z);
        }
    }
}
