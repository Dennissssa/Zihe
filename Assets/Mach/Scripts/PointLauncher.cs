using UnityEngine;
using UnityEngine.InputSystem;
namespace Mach
{
    [DefaultExecutionOrder(-25)]
    public sealed class PointLauncher:MonoBehaviour
    {
        public BehaviorProgram program;
        public Transform poseRoot,muzzle;
        public TextMesh screen;
        public Vector3 readyPosition=new Vector3(.25f,-.20f,.70f),readyAngles=new Vector3(0,-9,-3);
        public float transitionSeconds=.5f;
        public bool IsRaised {get;private set;}
        public bool BlocksInteraction=>IsRaised||blend>.03f;
        public int selected;
        float blend,velocity,kick,cooldown;
        public void SetRaised(bool value)
        {
            IsRaised=value;if(value){program.tablet.SetRaised(false);if(program.remote.IsRaised)program.remote.SetRaised(false);Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;poseRoot.gameObject.SetActive(true);}
        }
        public void Select(int step){int n=program.pointMarkers.Length;if(n>0)selected=(selected+step+n)%n;RefreshScreen();}
        void RefreshScreen(){if(screen&&program.pointMarkers.Length>0){var point=program.pointMarkers[selected];screen.text=point.pointId;screen.color=point.beaconColor;}}
        public Vector3 AimTarget()
        {
            var ray=program.pickup.viewCamera.ViewportPointToRay(new Vector3(.5f,.5f));float nearest=100;Vector3 target=ray.GetPoint(nearest);
            foreach(var hit in Physics.RaycastAll(ray,nearest,~(1<<30),QueryTriggerInteraction.Ignore))
                if(!hit.collider.transform.IsChildOf(transform)&&hit.distance<nearest){nearest=hit.distance;target=hit.point;}
            return target;
        }
        public bool Fire()
        {
            if(!IsRaised||blend<.95f||cooldown>0||program.pointMarkers.Length==0)return false;
            var cam=program.pickup.viewCamera;var target=AimTarget();
            var origin=muzzle.position;
            // The viewmodel can overlap nearby scenery: keep the projectile on the camera side of it.
            Vector3 toMuzzle=origin-cam.transform.position;
            foreach(var obstruction in Physics.RaycastAll(cam.transform.position,toMuzzle.normalized,toMuzzle.magnitude,~(1<<30),QueryTriggerInteraction.Ignore))
                if(!obstruction.collider.transform.IsChildOf(transform)){origin=cam.transform.position;break;}
            bool fired=program.pointMarkers[selected].Launch(origin,(target-origin).normalized,transform);
            if(fired){cooldown=.5f;kick=1;var marker=program.pointMarkers[selected];bool exists=false;foreach(var node in program.nodes)if(node.active&&node.kind==CircuitKind.Point&&node.pointId==marker.pointId)exists=true;if(!exists&&!program.Docked){int index=program.Add(CircuitKind.Point,marker.pointId,marker.Position);if(index>=0){program.nodes[index].pointId=marker.pointId;program.Save();}}}RefreshScreen();return fired;
        }
        void Update()
        {
            if(MissionPauseMenu.BlocksGameplay)return;
            cooldown=Mathf.Max(0,cooldown-Time.deltaTime);var mouse=Mouse.current;var key=Keyboard.current;if(mouse==null||key==null)return;
            if(GetComponent<TruckDriver>().CurrentTruck||program.pickup.HeldItem||program.pickup.placement.IsPlacing){if(IsRaised)SetRaised(false);return;}
            if(key.escapeKey.wasPressedThisFrame||key.tabKey.wasPressedThisFrame||key.rKey.wasPressedThisFrame){SetRaised(false);return;}
            // The remote owns an unlocked cursor, but RMB must still be able to replace it.
            if(mouse.rightButton.wasPressedThisFrame&&program.remote.IsRaised){SetRaised(true);return;}
            if(program.Editing||program.remote.IsRaised){if(IsRaised)SetRaised(false);return;}
            if(Cursor.lockState!=CursorLockMode.Locked)return;
            if(mouse.rightButton.wasPressedThisFrame)SetRaised(!IsRaised);
            if(!IsRaised)return;
            if(key.eKey.wasPressedThisFrame)RecallSelected();
            if(Mathf.Abs(mouse.scroll.ReadValue().y)>.01f)Select(mouse.scroll.ReadValue().y<0?1:-1);
            if(mouse.leftButton.wasPressedThisFrame&&FirstPersonWalker.CursorCapturedFrame!=Time.frameCount)Fire();
        }
        public CircuitPointMarker FindRecallTarget()
        {
            if(!program||program.pointMarkers==null||selected<0||selected>=program.pointMarkers.Length)return null;
            var point=program.pointMarkers[selected];
            return point&&!point.stored&&!point.InFlight&&!point.Recalling?point:null;
        }
        public bool RecallSelected()
        {
            if(!IsRaised)return false;
            var point=FindRecallTarget();if(!point||!point.Recall(muzzle))return false;
            GameAudio.Play(SoundCue.Pickup,transform.position,true);RefreshScreen();return true;
        }
        public void AdvancePose(float dt)
        {
            blend=Mathf.SmoothDamp(blend,IsRaised?1:0,ref velocity,transitionSeconds*.32f,Mathf.Infinity,Mathf.Max(.00001f,dt));kick=Mathf.MoveTowards(kick,0,dt*7);
            poseRoot.localPosition=readyPosition+new Vector3(.08f,-.55f,.14f)*(1-blend)+new Vector3(0,Mathf.Sin(blend*Mathf.PI)*.06f,-kick*.035f);
            var camera=program.pickup.viewCamera;
            var target=AimTarget();
            // Keep very close surfaces from turning the viewmodel backwards through the camera.
            if(Vector3.Dot(target-poseRoot.position,camera.transform.forward)<.65f)target=camera.transform.position+camera.transform.forward*1.5f;
            var aimed=Quaternion.Inverse(poseRoot.parent.rotation)*Quaternion.LookRotation(target-poseRoot.position,camera.transform.up)*Quaternion.Euler(0,0,readyAngles.z);
            var stowed=Quaternion.Euler(readyAngles+new Vector3(24,8,-12));
            poseRoot.localRotation=Quaternion.Slerp(stowed,aimed,blend)*Quaternion.Euler(-kick*4,0,0);
            if(!IsRaised&&blend<.001f)poseRoot.gameObject.SetActive(false);RefreshScreen();
        }
        void LateUpdate()=>AdvancePose(Mathf.Min(Time.unscaledDeltaTime,.05f));
        void OnApplicationFocus(bool focus){if(!focus)SetRaised(false);}
    }
}
