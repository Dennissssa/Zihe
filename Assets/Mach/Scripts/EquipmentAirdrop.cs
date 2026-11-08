using UnityEngine;
namespace Mach
{
    [RequireComponent(typeof(Rigidbody),typeof(EquipmentRack))]
    public sealed class EquipmentAirdrop : MonoBehaviour
    {
        public Transform aircraft,parachute;
        public Transform[] propellers;
        public Renderer[] cargoRenderers;
        public Collider[] cargoColliders;
        public Vector3 planeStart,flightDirection=Vector3.back,releasePosition,landingPosition;
        public Vector3 landingNormal=Vector3.up;
        [Min(0)] public float flightDelay=5f,releaseAfter=7.4f;
        [Min(.1f)] public float flightSpeed=60f,descentSeconds=10f,parachuteFadeSeconds=2.5f;
        public float Elapsed=>elapsed;
        public bool Released {get;private set;}
        public bool Landed {get;private set;}
        Rigidbody body;float elapsed;Renderer[] fabric;MaterialPropertyBlock tint;
        void Awake()=>Initialize();
        void Initialize()
        {
            if(body)return;body=GetComponent<Rigidbody>();tint=new MaterialPropertyBlock();
            fabric=parachute?parachute.GetComponentsInChildren<Renderer>(true):new Renderer[0];
        }
        void Update()=>Advance(Time.deltaTime);
        public void Advance(float dt)
        {
            Initialize();elapsed+=Mathf.Max(0,dt);
            float flightTime=elapsed-flightDelay;
            if(aircraft)
            {
                bool visible=flightTime>=0&&flightTime<=releaseAfter+7;
                if(aircraft.gameObject.activeSelf!=visible)aircraft.gameObject.SetActive(visible);
                aircraft.position=planeStart+flightDirection.normalized*flightSpeed*Mathf.Max(0,flightTime);
                if(flightTime>=0&&propellers!=null)foreach(var p in propellers)if(p)p.Rotate(Vector3.forward,dt*1400,Space.Self);
                
            }
            if(flightTime<releaseAfter)return;
            if(!Released)
            {
                GameAudio.Play(SoundCue.Release,releasePosition);GameAudio.Play(SoundCue.Deploy,releasePosition);Released=true;transform.SetParent(null,true);body.isKinematic=true;
                body.interpolation=RigidbodyInterpolation.None;
                body.position=releasePosition;transform.position=releasePosition;
                if(cargoRenderers!=null)foreach(var r in cargoRenderers)if(r)r.enabled=true;
                if(cargoColliders!=null)foreach(var c in cargoColliders)if(c)c.enabled=true;
                landingPosition=GroundClearance(landingPosition);landingNormal=Vector3.up;
                if(parachute)parachute.gameObject.SetActive(true);
            }
            float descentTime=flightTime-releaseAfter,t=Mathf.Clamp01(descentTime/descentSeconds);
            if(!Landed)
            {
                var position=Vector3.Lerp(releasePosition,landingPosition,Mathf.SmoothStep(0,1,t));
                position.y=Mathf.Lerp(landingPosition.y,releasePosition.y,Mathf.Pow(1-t,1.35f));
                position=GroundClearance(position);
                var rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(Vector3.up,landingNormal),Mathf.InverseLerp(.7f,1,t));
                rotation*=Quaternion.Euler(Mathf.Sin(t*15)*1.2f*Mathf.Sin(t*Mathf.PI),0,Mathf.Sin(t*19)*1.5f*Mathf.Sin(t*Mathf.PI));
                transform.SetPositionAndRotation(position,rotation);
                body.position=position;body.rotation=rotation;
                if(parachute)parachute.localScale=Vector3.one*Mathf.Lerp(.08f,1,Mathf.SmoothStep(0,1,descentTime/.65f));
                if(t>=1){landingPosition=position;transform.SetPositionAndRotation(landingPosition,Quaternion.identity);Landed=true;GameAudio.Play(SoundCue.Land,landingPosition);}
            }
            if(Landed&&parachute&&parachute.gameObject.activeSelf)
            {
                float fade=Mathf.Clamp01((descentTime-descentSeconds)/parachuteFadeSeconds);
                parachute.localScale=new Vector3(1,Mathf.Lerp(1,.08f,Mathf.SmoothStep(0,1,fade)),1);
                foreach(var r in fabric)if(r){var color=r.sharedMaterial.color;color.a*=1-fade;r.GetPropertyBlock(tint);tint.SetColor("_BaseColor",color);tint.SetColor("_Color",color);r.SetPropertyBlock(tint);}
                if(fade>=1)parachute.gameObject.SetActive(false);
            }
        }
        // Check the highest surface across the pallet, including steep mountain faces.
        // Filtering by surface slope could incorrectly select the loading yard underneath it.
        public Vector3 GroundClearance(Vector3 position)
        {
            var floor=GetComponent<BoxCollider>();var scale=transform.lossyScale;
            float x=floor?floor.size.x*Mathf.Abs(scale.x)*.5f:1,z=floor?floor.size.z*Mathf.Abs(scale.z)*.5f:1.2f;
            float bottom=floor?(floor.center.y-floor.size.y*.5f)*scale.y:0;
            float ground=float.NegativeInfinity;
            Vector3[] probes={Vector3.zero,new Vector3(-x,0,-z),new Vector3(-x,0,z),new Vector3(x,0,-z),new Vector3(x,0,z)};
            foreach(var offset in probes)
            {
                var origin=position+offset;origin.y=Mathf.Max(releasePosition.y+30,position.y+30);
                foreach(var hit in Physics.RaycastAll(origin,Vector3.down,500,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                {
                    if(hit.transform.IsChildOf(transform)||(aircraft&&hit.transform.IsChildOf(aircraft))||hit.collider.GetComponentInParent<FirstPersonWalker>()||hit.collider.GetComponentInParent<UtilityTruck>()||hit.collider.GetComponentInParent<MovableMachine>())continue;
                    ground=Mathf.Max(ground,hit.point.y);
                }
            }
            if(!float.IsNegativeInfinity(ground))position.y=Mathf.Max(position.y,ground-bottom+.07f);
            return position;
        }
    }
}


