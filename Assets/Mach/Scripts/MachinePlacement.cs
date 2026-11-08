using UnityEngine;
using UnityEngine.InputSystem;
namespace Mach
{
    [DefaultExecutionOrder(-5)]
    public sealed class MachinePlacement : MonoBehaviour
    {
        public Camera viewCamera; public PlayerPickup pickup; public HeldTablet tablet;
        public float reach=3f, placementReach=4f, fallbackDistance=2.5f, rotationStep=15f;
        public LayerMask surfaceMask=Physics.DefaultRaycastLayers;
        public LayerMask obstacleMask=~(1<<30);
        public MovableMachine Moving { get; private set; }
        public bool IsPlacing=>Moving!=null;
        public bool CanPlace { get; private set; }
        public event System.Action<MovableMachine> Placed;
        public string PlacementError {get;private set;}="";
        public bool ConsumedInputThisFrame=>handledFrame==Time.frameCount;
        int handledFrame=-1; float yaw;
        UtilityTruck previewTruck,confirmedSupport;
        EquipmentSlot packingSlot,returnSlot;EquipmentRack blockedRack;
        Vector3 markerNormal=Vector3.up;Transform markerSurface;
        Collider robotSupport; bool rackSnapped;
        public MachineMoveHandle FindHandle()
        {
            if(!viewCamera || IsPlacing || (pickup && pickup.HeldItem) || (tablet && tablet.BlocksWorldInteraction))return null;
            var ray=viewCamera.ViewportPointToRay(new Vector3(.5f,.5f));
            float limit=reach;MachineMoveHandle result=null;
            if(Physics.Raycast(ray,out var solid,reach,surfaceMask,QueryTriggerInteraction.Ignore)){limit=solid.distance;result=solid.collider.GetComponent<MachineMoveHandle>();}
            foreach(var hit in Physics.RaycastAll(ray,limit,surfaceMask,QueryTriggerInteraction.Collide))
            {
                var handle=hit.collider.GetComponent<MachineMoveHandle>();
                if(handle&&hit.distance<=limit){limit=hit.distance;result=handle;}
            }
            return result;
        }
        public bool Begin(MovableMachine machine)
        {
            if(!machine || IsPlacing || (pickup && pickup.HeldItem) || (tablet && tablet.BlocksWorldInteraction))return false;
            var drop=machine.GetComponentInParent<EquipmentAirdrop>();if(drop&&!drop.Landed)return false;
            returnSlot=null;foreach(var slot in FindObjectsByType<EquipmentSlot>())if(slot.machine==machine&&slot.Packed){returnSlot=slot;blockedRack=slot.rack;slot.Unpack();break;}
            GameAudio.Play(returnSlot?SoundCue.Unpack:SoundCue.Pickup,machine.transform.position);Moving=machine;yaw=machine.transform.eulerAngles.y;previewTruck=null;confirmedSupport=null;markerNormal=Vector3.up;markerSurface=null;Moving.BeginPreview();CanPlace=false;handledFrame=Time.frameCount;return true;
        }
        void Update()
        {
            if(MissionPauseMenu.BlocksGameplay)return;
            var launcher=GetComponent<PointLauncher>();if(launcher&&launcher.BlocksInteraction)return;
            var heightDrag=GetComponent<PointHeightDrag>();if(heightDrag&&heightDrag.ConsumesInput)return;
            var keyboard=Keyboard.current;var mouse=Mouse.current;
            if(IsPlacing)
            {
                if((keyboard!=null && keyboard.escapeKey.wasPressedThisFrame) || (mouse!=null && mouse.rightButton.wasPressedThisFrame) || Cursor.lockState!=CursorLockMode.Locked){Cancel();return;}
                if(mouse!=null && Mathf.Abs(mouse.scroll.ReadValue().y)>.01f)Rotate(Mathf.Sign(mouse.scroll.ReadValue().y));
                UpdatePreview();
                if(!IsPlacing)return;
                if(pickup && pickup.prompt)pickup.prompt.text=packingSlot?"[E] Pack into equipment rack":CanPlace?"":PlacementError;
                if(keyboard!=null && keyboard.eKey.wasPressedThisFrame){handledFrame=Time.frameCount;Confirm();}
                return;
            }
            if(Cursor.lockState!=CursorLockMode.Locked || keyboard==null)return;
            var handle=FindHandle();if(handle && keyboard.eKey.wasPressedThisFrame)Begin(handle.machine);
        }
        public void Rotate(float steps)
        {
            if(!IsPlacing)return;yaw=Mathf.Repeat(yaw+steps*rotationStep,360);Moving.transform.rotation=Quaternion.Euler(0,yaw,0);CanPlace=false;
        }
        public void UpdatePreview()
        {
            if(!IsPlacing || !viewCamera)return;
            var ray=viewCamera.ViewportPointToRay(new Vector3(.5f,.5f));
            if(Physics.Raycast(ray,out var hit,placementReach,surfaceMask,QueryTriggerInteraction.Ignore)){if(TryPacking(hit))return;PreviewHit(hit);}
            else
            {
                ClearPacking();blockedRack=null;
                var forward=Vector3.ProjectOnPlane(viewCamera.transform.forward,Vector3.up).normalized;if(forward.sqrMagnitude<.01f)forward=transform.forward;
                Vector3 above=viewCamera.transform.position+forward*fallbackDistance+Vector3.up;
                if(Physics.Raycast(above,Vector3.down,out hit,6,surfaceMask,QueryTriggerInteraction.Ignore))PreviewHit(hit);
                else PreviewAt(viewCamera.transform.position+forward*fallbackDistance-Vector3.up*1.6f,false);
            }
        }
        void PreviewHit(RaycastHit hit)
        {
            if(Moving.GetComponent<CircuitPointMarker>()){markerNormal=hit.normal;markerSurface=hit.collider.transform;PreviewAt(hit.point,true,hit.collider.GetComponentInParent<UtilityTruck>());return;}
            var truck=hit.collider.GetComponentInParent<UtilityTruck>();
            if(Moving.GetComponent<RobotGroundMotor>()){PreviewRobot(hit,truck);return;}
            if(Moving.GetComponent<EquipmentRack>()){markerNormal=hit.normal;PreviewAt(hit.point,Vector3.Dot(hit.normal,Vector3.up)>.94f,truck);return;}
            PreviewAt(hit.point,Vector3.Dot(hit.normal,truck?truck.transform.up:Vector3.up)>.97f,truck);
        }
        void ClearPacking(){if(packingSlot){packingSlot.Preview(false);Moving.SetPreviewVisible(true);}packingSlot=null;}
        bool TryPacking(RaycastHit hit)
        {
            var rack=hit.collider.GetComponentInParent<EquipmentRack>();if(rack!=blockedRack)blockedRack=null;
            var slot=rack&&rack!=blockedRack&&!Moving.GetComponent<EquipmentRack>()?rack.SlotFor(Moving):null;
            if(!slot||slot.Packed){ClearPacking();return false;}
            if(packingSlot!=slot){ClearPacking();packingSlot=slot;}
            Moving.SetPreviewVisible(false);slot.Preview(true);CanPlace=true;return true;
        }
        public bool FinishPacking(){if(!packingSlot||!Moving)return false;GameAudio.Play(SoundCue.Pack,packingSlot.transform.position);packingSlot.Pack();packingSlot=null;Moving=null;returnSlot=null;CanPlace=false;handledFrame=Time.frameCount;return true;}
        public bool PreviewAt(Vector3 surfacePoint,bool flatSurface,UtilityTruck truck=null)
        {
            if(!IsPlacing)return false;
            if(Moving.GetComponent<RobotGroundMotor>()){robotSupport=null;CanPlace=false;PlacementError="Aim at ground";Moving.ShowValidity(false);return false;}
                        rackSnapped=false;
            if(Moving.GetComponent<EquipmentRack>())
            {
                foreach(var dock in FindObjectsByType<TruckRackDock>())
                {
                    if((truck && dock.GetComponent<UtilityTruck>()!=truck)||!dock.Accepts(surfacePoint))continue;
                    truck=dock.GetComponent<UtilityTruck>();
                    surfacePoint=dock.transform.TransformPoint(dock.deckCenter);
                    markerNormal=truck.transform.up;
                    float relative=Mathf.DeltaAngle(truck.transform.eulerAngles.y,yaw);
                    yaw=truck.transform.eulerAngles.y;
                    flatSurface=true;rackSnapped=true;break;
                }
            }
            previewTruck=truck;var up=(Moving.GetComponent<CircuitPointMarker>()||Moving.GetComponent<EquipmentRack>())?markerNormal:truck?truck.transform.up:Vector3.up;
            float bottom=(Moving.boundsCenter.y-Moving.boundsSize.y*.5f)*Moving.transform.lossyScale.y;
            Moving.transform.SetPositionAndRotation(surfacePoint-up*bottom,rackSnapped?truck.transform.rotation:Quaternion.FromToRotation(Vector3.up,up)*Quaternion.Euler(0,yaw,0));
            Physics.SyncTransforms();PlacementError=flatSurface?"":"Surface is too steep";CanPlace=flatSurface && IsSupportedAndClear();Moving.ShowValidity(CanPlace);return CanPlace;
        }
        bool IsSupportedAndClear()
        {
            confirmedSupport=null;
            if(Moving.GetComponent<RobotGroundMotor>())return IsRobotClear(false);
            var t=Moving.transform;Vector3 size=Moving.boundsSize;Vector3 center=t.TransformPoint(Moving.boundsCenter);
            var half=Vector3.Max(Vector3.Scale(size,t.lossyScale)*.5f-new Vector3(.018f,.022f,.018f),Vector3.one*.01f);
            foreach(var obstruction in Physics.OverlapBox(center,half,t.rotation,obstacleMask,QueryTriggerInteraction.Ignore))if(!obstruction.transform.IsChildOf(t)){PlacementError="Blocked by "+obstruction.name;return false;}
            Vector3 baseCenter=t.TransformPoint(new Vector3(Moving.boundsCenter.x,Moving.boundsCenter.y-size.y*.5f,Moving.boundsCenter.z));
            Vector3[] offsets={Vector3.zero,new Vector3(-.46f,0,-.46f),new Vector3(-.46f,0,.46f),new Vector3(.46f,0,-.46f),new Vector3(.46f,0,.46f)};
            foreach(var offset in offsets)
            {
                Vector3 point=baseCenter+t.TransformVector(Vector3.Scale(offset,size));
                float tolerance=Moving.GetComponent<EquipmentRack>()?.10f:.035f;
                if(!Physics.Raycast(point+t.up*.14f,-t.up,out var support,.28f,surfaceMask,QueryTriggerInteraction.Ignore) || Vector3.Dot(support.normal,t.up)<.97f || Mathf.Abs(Vector3.Dot(support.point-baseCenter,t.up))>tolerance){PlacementError="No support under base";return false;}
                var truck=support.collider.GetComponentInParent<UtilityTruck>();
                if(truck!=previewTruck){PlacementError="Base spans different supports";return false;}
                confirmedSupport=truck;
            }
            if(viewCamera)
            {
                var target=baseCenter+t.up*Mathf.Min(.7f,size.y*.5f);
                if(Vector3.Distance(viewCamera.transform.position,target)>placementReach+1){PlacementError="Placement is out of reach";return false;}
                if(Physics.Linecast(viewCamera.transform.position,target,out var sight,surfaceMask,QueryTriggerInteraction.Ignore)&&!sight.transform.IsChildOf(t)&&!(rackSnapped&&sight.collider.GetComponentInParent<UtilityTruck>()==previewTruck)){PlacementError="View blocked by "+sight.collider.name;return false;}
            }
            return true;
        }
        void PreviewRobot(RaycastHit hit,UtilityTruck truck)
        {
            previewTruck=truck;robotSupport=hit.collider;markerNormal=hit.normal;
            var t=Moving.transform;
            t.SetPositionAndRotation(hit.point,Quaternion.FromToRotation(Vector3.up,hit.normal)*Quaternion.Euler(0,yaw,0));
            float bottom=float.PositiveInfinity;
            foreach(var c in Moving.PlacementColliders())bottom=Mathf.Min(bottom,ColliderMinimum(c,hit.normal));
            if(float.IsPositiveInfinity(bottom)){CanPlace=false;PlacementError="Robot has no solid collider";Moving.ShowValidity(false);return;}
            t.position+=hit.normal*(Vector3.Dot(hit.point,hit.normal)-bottom+.025f);
            Physics.SyncTransforms();PlacementError="";
            // The physics motor handles uneven ground; furniture's five-point support test does not apply.
            CanPlace=Vector3.Dot(hit.normal,Vector3.up)>.3f&&IsRobotClear(true);
            if(Vector3.Dot(hit.normal,Vector3.up)<=.3f)PlacementError="Aim at ground";
            Moving.ShowValidity(CanPlace);
        }
        static float ColliderMinimum(Collider collider,Vector3 axis)
        {
            var t=collider.transform;var scale=t.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
            if(collider is BoxCollider box)
            {
                var h=box.size*.5f;
                return Vector3.Dot(t.TransformPoint(box.center),axis)-Mathf.Abs(Vector3.Dot(t.TransformVector(Vector3.right*h.x),axis))-Mathf.Abs(Vector3.Dot(t.TransformVector(Vector3.up*h.y),axis))-Mathf.Abs(Vector3.Dot(t.TransformVector(Vector3.forward*h.z),axis));
            }
            if(collider is CapsuleCollider capsule)
            {
                int d=capsule.direction;float radius=capsule.radius*Mathf.Max(scale[(d+1)%3],scale[(d+2)%3]);
                var localAxis=d==0?Vector3.right:d==1?Vector3.up:Vector3.forward;
                float segment=Mathf.Max(0,capsule.height*scale[d]*.5f-radius);
                return Vector3.Dot(t.TransformPoint(capsule.center),axis)-radius-Mathf.Abs(Vector3.Dot(t.rotation*localAxis,axis))*segment;
            }
            if(collider is SphereCollider sphere)return Vector3.Dot(t.TransformPoint(sphere.center),axis)-sphere.radius*Mathf.Max(scale.x,Mathf.Max(scale.y,scale.z));
            if(collider is MeshCollider mesh&&mesh.sharedMesh)
            {
                // Mesh bounds are conservative; the narrow phase below still uses the actual convex shape.
                var b=mesh.sharedMesh.bounds;float min=float.PositiveInfinity;
                for(int i=0;i<8;i++){var p=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));min=Mathf.Min(min,Vector3.Dot(t.TransformPoint(p),axis));}return min;
            }
            return Vector3.Dot(t.position,axis);
        }
        bool IsRobotClear(bool settlePreview)
        {
            confirmedSupport=null;
            if(!robotSupport){PlacementError="Aim at ground";return false;}
            var t=Moving.transform;
            for(int pass=0;pass<(settlePreview?8:1);pass++)
            {
                float lift=0;
                foreach(var c in Moving.PlacementColliders())
                {
                    var min=new Vector3(ColliderMinimum(c,Vector3.right),ColliderMinimum(c,Vector3.up),ColliderMinimum(c,Vector3.forward));
                    var max=-new Vector3(ColliderMinimum(c,Vector3.left),ColliderMinimum(c,Vector3.down),ColliderMinimum(c,Vector3.back));
                    foreach(var other in Physics.OverlapBox((min+max)*.5f,(max-min)*.5f+Vector3.one*.01f,Quaternion.identity,obstacleMask,QueryTriggerInteraction.Ignore))
                    {
                        if(other.transform.IsChildOf(t))continue;
                        if(!PlacementPenetration(c,other,out var direction,out float distance)||distance<.002f)continue;
                        float up=Vector3.Dot(direction,markerNormal);
                        if(settlePreview&&other==robotSupport&&up>.35f)lift=Mathf.Max(lift,distance/up+.015f);
                        else{PlacementError="Blocked by "+other.name;return false;}
                    }
                }
                if(lift>0){t.position+=markerNormal*lift;Physics.SyncTransforms();continue;}
                if(viewCamera&&Vector3.Distance(viewCamera.transform.position,t.position)>placementReach+1){PlacementError="Placement is out of reach";return false;}
                confirmedSupport=previewTruck;PlacementError="";return true;
            }
            PlacementError="Not enough room above ground";return false;
        }
        static bool PlacementPenetration(Collider shape,Collider other,out Vector3 direction,out float distance)
        {
            // Unity may release the native shape when preview disables its collider.
            // Restore it only for this synchronous query; no simulation runs here.
            shape.enabled=true;
            try
            {
                Physics.SyncTransforms();
                return Physics.ComputePenetration(shape,shape.transform.position,shape.transform.rotation,other,other.transform.position,other.transform.rotation,out direction,out distance);
            }
            finally{shape.enabled=false;}
        }
        public bool Confirm()
        {
            if(packingSlot)return FinishPacking();
            if(!IsPlacing || !CanPlace || !IsSupportedAndClear())return false;
            var placed=Moving;GameAudio.Play(SoundCue.Place,Moving.transform.position);Moving.EndPreview(true,Moving.GetComponent<CircuitPointMarker>()&&markerSurface?markerSurface:confirmedSupport?confirmedSupport.transform:null);Moving=null;returnSlot=null;blockedRack=null;previewTruck=null;confirmedSupport=null;CanPlace=false;handledFrame=Time.frameCount;Placed?.Invoke(placed);return true;
        }
        public void Cancel()
        {
            if(!IsPlacing)return;GameAudio.Play(SoundCue.Cancel,transform.position,true);ClearPacking();Moving.EndPreview(false);if(returnSlot)returnSlot.Pack();Moving=null;returnSlot=null;blockedRack=null;previewTruck=null;confirmedSupport=null;CanPlace=false;handledFrame=Time.frameCount;
        }
        void OnApplicationFocus(bool focused){if(!focused)Cancel();}
        void OnDisable()=>Cancel();
    }
}








