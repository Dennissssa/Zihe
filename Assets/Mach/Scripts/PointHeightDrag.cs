using UnityEngine;
using UnityEngine.InputSystem;
namespace Mach
{
    [DefaultExecutionOrder(-20)]
    public sealed class PointHeightDrag : MonoBehaviour
    {
        public PlayerPickup pickup;
        public bool ConsumesInput {get;private set;}
        CircuitPointMarker dragging;
        float metresPerPixel;
        public CircuitPointMarker FindTarget()
        {
            var ray=pickup.viewCamera.ViewportPointToRay(new Vector3(.5f,.5f));
            float limit=pickup.reach;CircuitPointMarker result=null;
            if(Physics.Raycast(ray,out var solid,limit,pickup.rayMask,QueryTriggerInteraction.Ignore))limit=solid.distance;
            foreach(var hit in Physics.RaycastAll(ray,limit,pickup.rayMask,QueryTriggerInteraction.Collide))
            {
                var marker=hit.collider.GetComponentInParent<CircuitPointMarker>();
                if(marker&&marker.heightHandle&&hit.collider.transform.IsChildOf(marker.heightHandle)&&hit.distance<=limit)
                {result=marker;limit=hit.distance;}
            }
            return result;
        }
        void Update()
        {
            if(MissionPauseMenu.BlocksGameplay)return;
            ConsumesInput=false;var launcher=GetComponent<PointLauncher>();if(launcher&&launcher.BlocksInteraction){dragging=null;return;}
            var mouse=Mouse.current;var keyboard=Keyboard.current;
            var program=GetComponent<BehaviorProgram>();
            if(!pickup||!pickup.enabled||pickup.HeldItem||mouse==null||Cursor.lockState!=CursorLockMode.Locked||
               (pickup.placement&&pickup.placement.IsPlacing)||(pickup.heldTablet&&pickup.heldTablet.BlocksWorldInteraction)||
               (program&&program.remote&&program.remote.IsRaised)||(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame))
            {dragging=null;return;}
            if(dragging)
            {
                ConsumesInput=true;
                if(!mouse.leftButton.isPressed){dragging=null;return;}
                dragging.SetHeight(dragging.targetHeight+PlayerLookInput.Read().y*metresPerPixel);
                if(pickup.prompt)pickup.prompt.text=$"POINT {dragging.pointId}  Y {dragging.Position.y:0.00} m";
                return;
            }
            var target=FindTarget();if(!target)return;
            ConsumesInput=true;
            if(pickup.prompt)pickup.prompt.text=$"POINT {target.pointId}  Y {target.Position.y:0.00} m  |  Hold LMB + move up/down";
            if(mouse.leftButton.wasPressedThisFrame&&FirstPersonWalker.CursorCapturedFrame!=Time.frameCount)
            {
                dragging=target;
                float depth=Vector3.Distance(pickup.viewCamera.transform.position,target.heightHandle.position);
                metresPerPixel=2*depth*Mathf.Tan(pickup.viewCamera.fieldOfView*Mathf.Deg2Rad*.5f)/Mathf.Max(1,Screen.height);
            }
        }
        public bool IsDragging=>dragging;
        void OnDisable(){dragging=null;ConsumesInput=false;}
        void OnApplicationFocus(bool focused){if(!focused){dragging=null;ConsumesInput=false;}}
    }
}



