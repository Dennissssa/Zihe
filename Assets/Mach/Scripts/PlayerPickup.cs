using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Mach
{
    public sealed class PlayerPickup : MonoBehaviour
    {
        public Camera viewCamera;
        public Text prompt;
        public HeldTablet heldTablet;
        public MachinePlacement placement;
        public TruckDriver truckDriver;
        [Min(0.1f)] public float reach = 3f;
        [Min(0.5f)] public float holdDistance = 1.8f;
        [Min(1)] public float followStrength = 12f;
        public LayerMask rayMask = Physics.DefaultRaycastLayers;
        public PickupItem HeldItem { get; private set; }
        public Transform OutlineTarget {get;private set;}
        bool oldGravity;
        RigidbodyConstraints oldConstraints;
        CollisionDetectionMode oldDetection;

        public PickupItem FindTarget()
        {
            if(placement && placement.IsPlacing)return null;
            if (heldTablet && heldTablet.BlocksWorldInteraction) return null;
            if (!viewCamera) return null;
            Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (!Physics.Raycast(ray, out var hit, reach, rayMask, QueryTriggerInteraction.Ignore)) return null;
            return hit.rigidbody ? hit.rigidbody.GetComponent<PickupItem>() : null;
        }

        CircuitWorldButton heldRepeatButton;bool repeatTouched;
        void Update()
        {
            if(MissionPauseMenu.BlocksGameplay)return;
            OutlineTarget=null;repeatTouched=false;
            try{UpdateInteraction();}
            finally{if(!repeatTouched&&heldRepeatButton){heldRepeatButton.EndHold();heldRepeatButton=null;}}
        }
        void UpdateInteraction()
        {
            var launcher=GetComponent<PointLauncher>();if(launcher&&launcher.BlocksInteraction){var recallTarget=launcher.FindRecallTarget();if(prompt)prompt.text=recallTarget?"[E] Recall Point "+recallTarget.pointId:"";return;}
            var heightDrag=GetComponent<PointHeightDrag>();if(heightDrag&&heightDrag.ConsumesInput){var point=heightDrag.FindTarget();OutlineTarget=point?point.heightHandle:null;return;}
            if(placement && (placement.IsPlacing || placement.ConsumedInputThisFrame))return;
            if(prompt)prompt.text="";
            bool active = Cursor.lockState == CursorLockMode.Locked;
            var program=GetComponent<BehaviorProgram>();
            if(program && program.remote && program.remote.IsRaised)return;
            if (heldTablet && heldTablet.BlocksWorldInteraction)
            {
                if (prompt) prompt.text = "";
                return;
            }
            if(active && !HeldItem && Physics.Raycast(viewCamera.ViewportPointToRay(new Vector3(.5f,.5f)),out var circuitHit,reach,rayMask,QueryTriggerInteraction.Ignore))
            {
                var droneButton=circuitHit.collider.GetComponentInParent<DroneAccessButton>();
                if(droneButton&&droneButton.station){OutlineTarget=droneButton.cap?droneButton.cap:droneButton.transform;if(prompt)prompt.text=droneButton.station.Prompt;if(Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame)droneButton.Activate(this);return;}
                var dock=circuitHit.collider.GetComponentInParent<RobotTabletDock>();var button=circuitHit.collider.GetComponentInParent<CircuitWorldButton>();
                if(dock && program){OutlineTarget=dock.transform;if(prompt)prompt.text=program.Docked?"[E] Remove tablet / stop program":"[E] Insert tablet / run circuit";if(Keyboard.current!=null && Keyboard.current.eKey.wasPressedThisFrame)program.ToggleDock(dock.robot);return;}
                if(button)
                {
                    OutlineTarget=button.cap?button.cap:button.transform;if(prompt)prompt.text=button.Prompt;
                    var mouse=Mouse.current;
                    if(mouse!=null&&mouse.leftButton.wasPressedThisFrame&&FirstPersonWalker.CursorCapturedFrame!=Time.frameCount)
                    {
                        button.Activate();
                        if(button.CanRepeat){if(heldRepeatButton)heldRepeatButton.EndHold();heldRepeatButton=button;button.BeginHold();repeatTouched=true;}
                    }
                    else if(mouse!=null&&mouse.leftButton.isPressed&&heldRepeatButton==button){repeatTouched=true;button.AdvanceHold(Time.unscaledDeltaTime);}
                    return;
                }
            }
            AmmoSelectionButton ammoButton = !HeldItem ? FindAmmoButton() : null;
            var truckTarget=!HeldItem?FindTruckTarget():null;
            if(active && truckTarget)
            {
                OutlineTarget=truckTarget.transform;if(prompt)prompt.text=truckTarget.Prompt;
                if(Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame && FirstPersonWalker.CursorCapturedFrame!=Time.frameCount)truckTarget.Activate();
                if(Keyboard.current!=null && Keyboard.current.eKey.wasPressedThisFrame && truckTarget.IsSeat && truckDriver)truckDriver.Enter(truckTarget.truck,truckTarget.SeatIndex);
                return;
            }
            if(active && ammoButton)
            {
                OutlineTarget=ammoButton.movingCap?ammoButton.movingCap:ammoButton.transform;if(prompt)prompt.text=ammoButton.Prompt;
                if(Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame && FirstPersonWalker.CursorCapturedFrame!=Time.frameCount)ammoButton.Activate();
                return;
            }
            FireRateButton rateButton = !HeldItem ? FindRateButton() : null;
            if(active && rateButton)
            {
                OutlineTarget=rateButton.keycap?rateButton.keycap:rateButton.transform;if(prompt)prompt.text=rateButton.Prompt;
                if(Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame && FirstPersonWalker.CursorCapturedFrame!=Time.frameCount)rateButton.Activate();
                return;
            }
            var moveHandle=placement?placement.FindHandle():null;
            if(active && moveHandle){var slot=moveHandle.GetComponentInParent<EquipmentSlot>();OutlineTarget=slot&&slot.Packed?slot.box.transform:moveHandle.machine.transform;if(prompt)prompt.text="[E] Move "+moveHandle.machine.displayName;return;}
            PickupItem target = HeldItem ? null : FindTarget();if(active&&target)OutlineTarget=target.transform;
            if (prompt) prompt.text = !active ? "" : HeldItem
                ? "" : target ? "[E] Pick up " + target.displayName : "";
            if (!active || Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame) return;
            if (HeldItem) Drop();
            else if (target) TryPickUp(target);
        }

        public bool TryPickUp(PickupItem item)
        {
            if (HeldItem || !item || !viewCamera || item.Body.isKinematic || FindTarget() != item) return false;
            GameAudio.Play(SoundCue.Pickup,item.transform.position);HeldItem = item;
            var body = item.Body;
            oldGravity = body.useGravity;
            oldConstraints = body.constraints;
            oldDetection = body.collisionDetectionMode;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.angularVelocity = Vector3.zero;
            return true;
        }

        public FireRateButton FindRateButton()
        {
            if(placement && placement.IsPlacing)return null;
            if(!viewCamera || HeldItem || (heldTablet && heldTablet.BlocksWorldInteraction))return null;
            return Physics.Raycast(viewCamera.ViewportPointToRay(new Vector3(.5f,.5f)),out var hit,reach,rayMask,QueryTriggerInteraction.Ignore)
                ? hit.collider.GetComponent<FireRateButton>() : null;
        }
        public AmmoSelectionButton FindAmmoButton()
        {
            if(placement && placement.IsPlacing)return null;
            if(!viewCamera || HeldItem || (heldTablet && heldTablet.BlocksWorldInteraction))return null;
            return Physics.Raycast(viewCamera.ViewportPointToRay(new Vector3(.5f,.5f)),out var hit,reach,rayMask,QueryTriggerInteraction.Ignore)
                ? hit.collider.GetComponent<AmmoSelectionButton>() : null;
        }
        public TruckInteractable FindTruckTarget()
        {
            if(!viewCamera || HeldItem || (placement && placement.IsPlacing) || (heldTablet && heldTablet.BlocksWorldInteraction))return null;
            var hits=Physics.RaycastAll(viewCamera.ViewportPointToRay(new Vector3(.5f,.5f)),reach,rayMask,QueryTriggerInteraction.Collide);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits){var target=hit.collider.GetComponentInParent<TruckInteractable>();if(target)return target;if(!hit.collider.isTrigger)return null;}
            return null;
        }

        void FixedUpdate()
        {
            if (!HeldItem) return;
            var body = HeldItem.Body;
            Vector3 destination = viewCamera.transform.position + viewCamera.transform.forward * holdDistance;
            Vector3 offset = destination - body.worldCenterOfMass;
            if (offset.sqrMagnitude > 16f) { Drop(); return; }
            // Keep the rigidbody dynamic: walls and furniture still block carried objects.
            body.linearVelocity = Vector3.ClampMagnitude(offset * followStrength, 8f);
        }

        public void Drop()
        {
            if (!HeldItem) return;
            var body = HeldItem.Body;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.useGravity = oldGravity;
            body.constraints = oldConstraints;
            body.collisionDetectionMode = oldDetection;
            GameAudio.Play(SoundCue.Place,HeldItem.transform.position);HeldItem = null;
        }

        void StopRepeat(){if(heldRepeatButton)heldRepeatButton.EndHold();heldRepeatButton=null;}
        void OnApplicationFocus(bool focus){if(!focus)StopRepeat();}
        void OnDisable(){OutlineTarget=null;StopRepeat();Drop();}
    }
}








