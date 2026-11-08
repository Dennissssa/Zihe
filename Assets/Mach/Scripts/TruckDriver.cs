using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Mach
{
    [DefaultExecutionOrder(30)]
    public sealed class TruckDriver : MonoBehaviour
    {
        public PlayerPickup pickup;public FirstPersonWalker walker;public HeldTablet tablet;public MachinePlacement placement;
        public Text controls;
        public UtilityTruck CurrentTruck {get;private set;}
        public int CurrentSeat {get;private set;}
        public bool HasDrivingControl=>CurrentTruck&&CurrentSeat==0;
        public Transform OutlineTarget {get;private set;}
        [Min(0)] public float maximumExitSpeedMph=20;
        CharacterController character;
        Transform oldParent;bool tabletVisible;string oldControls;float yaw,pitch;int enteredFrame=-1;
        public bool Enter(UtilityTruck truck,int seat=0)
        {
            if(!truck||seat<0||seat>1||truck.Speed>.8f)return false;
            var occupant=seat==0?truck.Driver:truck.Passenger;
            if(occupant&&occupant!=this)return false;
            if(seat==1&&!truck.passengerAnchor)return false;
            if(CurrentTruck)
            {
                if(CurrentTruck!=truck||CurrentSeat==seat)return false;
                if(CurrentSeat==0)truck.SetDriver(null);else truck.SetPassenger(null);
                Sit(truck,seat);return true;
            }
            var entryDoor=seat==0?truck.driverDoor:truck.passengerDoor;
            if(entryDoor&&!entryDoor.IsOpen)return false;
            if(pickup.HeldItem || placement.IsPlacing || tablet.BlocksWorldInteraction)return false;
            character=GetComponent<CharacterController>();oldParent=transform.parent;oldControls=controls?controls.text:"";
            tabletVisible=tablet.poseRoot.gameObject.activeSelf;tablet.SnapToRest();tablet.enabled=false;tablet.poseRoot.gameObject.SetActive(false);
            if(tablet.crosshair)tablet.crosshair.enabled=true;
            pickup.enabled=false;placement.enabled=false;walker.enabled=false;character.enabled=false;
            Sit(truck,seat);
            if(controls)controls.text="";
            enteredFrame=Time.frameCount;PlayerLookInput.Discard();Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;return true;
        }
        void Sit(UtilityTruck truck,int seat)
        {
            CurrentTruck=truck;CurrentSeat=seat;
            transform.SetParent(seat==0?truck.driverAnchor:truck.passengerAnchor,false);
            transform.localPosition=Vector3.zero;transform.localRotation=Quaternion.identity;
            pickup.viewCamera.transform.localRotation=Quaternion.identity;yaw=pitch=0;OutlineTarget=null;
            if(seat==0)truck.SetDriver(this);else truck.SetPassenger(this);
            enteredFrame=Time.frameCount;PlayerLookInput.Discard();
        }
        public bool Exit()
        {
            if(!CurrentTruck || CurrentTruck.Speed*2.236936f>=maximumExitSpeedMph)return false;
            var truck=CurrentTruck;
            var exitDoor=CurrentSeat==0?truck.driverDoor:truck.passengerDoor;
            if(!exitDoor||!exitDoor.IsOpen)return false;
            FindLeftExit(truck,out var point);
            if(CurrentSeat==0)truck.SetDriver(null);else truck.SetPassenger(null);
            CurrentTruck=null;OutlineTarget=null;transform.SetParent(oldParent,true);transform.position=point;transform.rotation=Quaternion.Euler(0,truck.transform.eulerAngles.y,0);
            Physics.SyncTransforms();pickup.viewCamera.transform.localRotation=Quaternion.identity;walker.ResetMotionAndLook();character.enabled=true;walker.enabled=true;pickup.enabled=true;placement.enabled=true;
            tablet.enabled=true;tablet.poseRoot.gameObject.SetActive(tabletVisible);if(tablet.crosshair)tablet.crosshair.enabled=true;
            if(controls)controls.text=oldControls;return true;
        }
        public bool FindLeftExit(UtilityTruck truck,out Vector3 point)
        {
            if(!character)character=GetComponent<CharacterController>();
            Vector3 left=Vector3.ProjectOnPlane(truck.transform.right,Vector3.up).normalized*(CurrentSeat==1?1:-1);
            if(left.sqrMagnitude<.01f)left=Vector3.Cross(Vector3.up,Vector3.ProjectOnPlane(truck.transform.forward,Vector3.up)).normalized*(CurrentSeat==1?1:-1);
            if(left.sqrMagnitude<.01f)left=CurrentSeat==1?Vector3.right:Vector3.left;
            // Use world metres from the seat, independent of edited exit anchors or model scale.
            Vector3 origin=CurrentSeat==1&&truck.passengerAnchor?truck.passengerAnchor.position:truck.driverAnchor.position;
            for(int i=0;i<5;i++)
            {
                point=origin+left*(2f+i*.5f);
                if(!Physics.Raycast(point+Vector3.up*4,Vector3.down,out var ground,10,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)||ground.collider.transform.IsChildOf(truck.transform))continue;
                point=ground.point+Vector3.up*.08f;
                float radius=character.radius*Mathf.Max(transform.lossyScale.x,transform.lossyScale.z);
                float height=Mathf.Max(2*radius,character.height*transform.lossyScale.y);
                Vector3 center=point+Vector3.up*(character.center.y*transform.lossyScale.y);
                bool blocked=false;
                foreach(var hit in Physics.OverlapCapsule(center+Vector3.up*(height*.5f-radius),center-Vector3.up*(height*.5f-radius),radius,~(1<<30),QueryTriggerInteraction.Ignore))
                    if(!hit.transform.IsChildOf(transform)){blocked=true;break;}
                if(!blocked)return true;
            }
            // Clearance checks choose a convenient exit, never veto it. When
            // wedged or overturned, emerge upright above the vehicle instead.
            var bounds=new Bounds(truck.transform.position,Vector3.one);
            foreach(var collider in truck.GetComponentsInChildren<Collider>())if(collider.enabled&&!collider.isTrigger)bounds.Encapsulate(collider.bounds);
            point=new Vector3(origin.x,bounds.max.y+.6f,origin.z)+left*2;
            return true;
        }
        void Update()
        {
            if(MissionPauseMenu.BlocksGameplay)return;
            OutlineTarget=null;if(!CurrentTruck||Time.timeScale<=0)return;
            var keyboard=Keyboard.current;var mouse=Mouse.current;if(keyboard==null || mouse==null)return;
            if(keyboard.escapeKey.wasPressedThisFrame){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            if(Cursor.lockState!=CursorLockMode.Locked)
            {
                PlayerLookInput.Read();PlayerLookInput.Discard();
                if(CurrentSeat==0)CurrentTruck.SetInputs(0,0,true);if(pickup.prompt)pickup.prompt.text="";
                if(mouse.leftButton.wasPressedThisFrame){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}return;
            }
            Vector2 look=GamePreferences.Look(PlayerLookInput.Read(),walker.mouseSensitivity);yaw=Mathf.Clamp(yaw+look.x,-150,150);pitch=Mathf.Clamp(pitch-look.y,-55,80);
            pickup.viewCamera.transform.localRotation=Quaternion.Euler(pitch,yaw,0);
            if(CurrentSeat==0)
            {
                if(HasDrivingControl)CurrentTruck.SetInputs((keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed?1:0)-(keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed?1:0),(keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed?1:0)-(keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed?1:0),keyboard.spaceKey.isPressed);
                else CurrentTruck.SetInputs(0,0,true);
            }
            if(pickup.prompt)pickup.prompt.text="";
            bool consumed=false;
            var hits=Physics.RaycastAll(pickup.viewCamera.ViewportPointToRay(new Vector3(.5f,.5f)),pickup.reach,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                if(hit.transform.IsChildOf(transform))continue;
                var seat=hit.collider.GetComponentInParent<TruckInteractable>();
                var control=hit.collider.GetComponentInParent<TruckCabControl>();
                if(seat&&seat.truck==CurrentTruck&&seat.IsSeat&&seat.SeatIndex!=CurrentSeat)
                {
                    OutlineTarget=seat.transform;if(pickup.prompt)pickup.prompt.text=CurrentTruck.Speed>.8f?"Wait for vehicle to stop":seat.SeatIndex==0?"[E] Driver seat":"[E] Passenger seat";
                    if(keyboard.eKey.wasPressedThisFrame&&Time.frameCount!=enteredFrame){Enter(CurrentTruck,seat.SeatIndex);consumed=true;}
                    break;
                }
                if(control&&control.truck==CurrentTruck&&control.CanUse(this))
                {
                    OutlineTarget=control.transform;if(pickup.prompt)pickup.prompt.text=control.Prompt(this);
                    if(control.action!=TruckCabAction.Steering&&mouse.leftButton.wasPressedThisFrame)control.Activate(this);
                    // Dashboard clicks remain available; E can exit at any angle.
                    break;
                }
                if(seat&&seat.truck==CurrentTruck&&!seat.IsSeat)
                {
                    OutlineTarget=seat.transform;if(pickup.prompt)pickup.prompt.text=seat.Prompt;
                    if(mouse.leftButton.wasPressedThisFrame)seat.Activate();break;
                }
                if(!hit.collider.isTrigger)break;
            }
            if(!consumed&&Time.frameCount!=enteredFrame&&keyboard.eKey.wasPressedThisFrame&&!Exit()&&pickup.prompt){
                if(CurrentTruck.Speed*2.236936f>=maximumExitSpeedMph)
                    pickup.prompt.text=GamePreferences.Chinese?"车速低于 20 mph 时可下车":"Slow below 20 mph to exit";
                else
                    pickup.prompt.text=GamePreferences.Chinese?"先打开这一侧的车门，再按 E 下车":"Open your door first, then press E to exit";
            }
            if(CurrentTruck&&CurrentSeat==0&&CurrentTruck.parkingBrake&&pickup.prompt&&
               (keyboard.wKey.isPressed||keyboard.sKey.isPressed||keyboard.upArrowKey.isPressed||keyboard.downArrowKey.isPressed))
                pickup.prompt.text="Handbrake engaged / click the handbrake to release";
        }
        void OnApplicationFocus(bool focused){if(!focused && CurrentTruck){if(CurrentSeat==0)CurrentTruck.SetInputs(0,0,true);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}}
    }
}

