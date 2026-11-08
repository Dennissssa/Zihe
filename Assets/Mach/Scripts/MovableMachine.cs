using UnityEngine;
using System.Collections.Generic;
namespace Mach
{
    public sealed class MovableMachine : MonoBehaviour
    {
        public string displayName="Machine";
        public Vector3 boundsCenter, boundsSize;
        public Material validGhost, invalidGhost;
        public bool IsPreview { get; private set; }
        Renderer[] renderers; Material[][] savedMaterials; bool[] savedVisible;
        Collider[] colliders; bool[] savedColliders;
        public IEnumerable<Collider> PlacementColliders()
        {
            if(!IsPreview)yield break;
            for(int i=0;i<colliders.Length;i++)
                if(colliders[i]&&savedColliders[i]&&!colliders[i].isTrigger&&colliders[i].gameObject.activeInHierarchy)
                    yield return colliders[i];
        }
        Vector3 originalPosition; Quaternion originalRotation; bool? lastValidity;
        Transform originalParent;Vector3 originalLocalPosition;Quaternion originalLocalRotation;
        Rigidbody movableBody;bool groundKinematic;
        readonly Dictionary<MeshCollider,bool> originalConvex=new Dictionary<MeshCollider,bool>();
        readonly List<(Collider cargo,Collider vehicle)> transportPairs=new List<(Collider,Collider)>();
        void ClearTransportPairs()
        {
            foreach(var pair in transportPairs)if(pair.cargo&&pair.vehicle)Physics.IgnoreCollision(pair.cargo,pair.vehicle,false);
            transportPairs.Clear();
        }
        void IgnoreCarrierCollision()
        {
            ClearTransportPairs();
            var truck=GetComponentInParent<UtilityTruck>();if(!truck)return;
            foreach(var cargo in GetComponentsInChildren<Collider>())
            foreach(var vehicle in truck.GetComponentsInChildren<Collider>())
            {
                if(!cargo.enabled||cargo.isTrigger||!vehicle.enabled||vehicle.isTrigger||vehicle.GetComponentInParent<MovableMachine>())continue;
                Physics.IgnoreCollision(cargo,vehicle,true);transportPairs.Add((cargo,vehicle));
            }
        }
        void SetTransportCollision(Transform parent)
        {
            var body=parent?parent.GetComponentInParent<Rigidbody>():null;
            bool dynamicSupport=body&&!body.isKinematic;
            foreach(var mesh in GetComponentsInChildren<MeshCollider>(true))
            {
                if(dynamicSupport)
                {
                    if(!originalConvex.ContainsKey(mesh))originalConvex.Add(mesh,mesh.convex);
                    if(!mesh.convex)mesh.convex=true;
                }
                else if(originalConvex.TryGetValue(mesh,out bool convex))mesh.convex=convex;
            }
            if(!dynamicSupport)originalConvex.Clear();
        }
        public void BeginPreview()
        {
            if(IsPreview)return;
            movableBody=GetComponent<Rigidbody>();
            if(movableBody){if(!GetComponentInParent<UtilityTruck>()&&!GetComponentInParent<EquipmentRack>())groundKinematic=movableBody.isKinematic;if(!movableBody.isKinematic){movableBody.linearVelocity=Vector3.zero;movableBody.angularVelocity=Vector3.zero;}movableBody.isKinematic=true;}
            originalPosition=transform.position;originalRotation=transform.rotation;
            originalParent=transform.parent;originalLocalPosition=transform.localPosition;originalLocalRotation=transform.localRotation;
            renderers=GetComponentsInChildren<Renderer>(true);savedMaterials=new Material[renderers.Length][];savedVisible=new bool[renderers.Length];
            for(int i=0;i<renderers.Length;i++){savedMaterials[i]=renderers[i].sharedMaterials;savedVisible[i]=renderers[i].enabled;if(renderers[i].GetComponent<TextMesh>())renderers[i].enabled=false;}
            colliders=GetComponentsInChildren<Collider>(true);savedColliders=new bool[colliders.Length];
            for(int i=0;i<colliders.Length;i++){savedColliders[i]=colliders[i].enabled;colliders[i].enabled=false;}
            ClearTransportPairs();
            // Disable first, detach, then restore the authored ground collision.
            transform.SetParent(null,true);SetTransportCollision(null);
            IsPreview=true;lastValidity=null;ShowValidity(false);
        }
        public void ShowValidity(bool valid)
        {
            if(!IsPreview || lastValidity==valid)return;lastValidity=valid;
            for(int i=0;i<renderers.Length;i++)
            {
                if(!renderers[i] || renderers[i].GetComponent<TextMesh>())continue;
                var materials=new Material[savedMaterials[i].Length];for(int j=0;j<materials.Length;j++)materials[j]=valid?validGhost:invalidGhost;
                renderers[i].sharedMaterials=materials;
            }
        }
        public void RestorePreviewAppearance()
        {
            if(!IsPreview)return;
            for(int i=0;i<renderers.Length;i++)if(renderers[i]){renderers[i].sharedMaterials=savedMaterials[i];renderers[i].enabled=savedVisible[i];}
        }
        public void SetPreviewVisible(bool visible)
        {
            if(!IsPreview)return;
            for(int i=0;i<renderers.Length;i++)if(renderers[i])renderers[i].enabled=visible&&savedVisible[i]&&!renderers[i].GetComponent<TextMesh>();
        }
        public void EndPreview(bool confirm,Transform support=null)
        {
            if(!IsPreview)return;
            // Cook convex hulls while disabled and BEFORE joining the dynamic
            // truck body, including when cancelling an unload operation.
            SetTransportCollision(confirm?support:originalParent);
            if(confirm)transform.SetParent(support,true);
            else
            {
                transform.SetParent(originalParent,true);
                if(originalParent){transform.localPosition=originalLocalPosition;transform.localRotation=originalLocalRotation;}
                else transform.SetPositionAndRotation(originalPosition,originalRotation);
            }
            for(int i=0;i<renderers.Length;i++)if(renderers[i]){renderers[i].sharedMaterials=savedMaterials[i];renderers[i].enabled=savedVisible[i];}
            for(int i=0;i<colliders.Length;i++)if(colliders[i])colliders[i].enabled=savedColliders[i];
            IgnoreCarrierCollision();
            IsPreview=false;lastValidity=null;Physics.SyncTransforms();
            if(movableBody)movableBody.isKinematic=GetComponentInParent<UtilityTruck>()||groundKinematic;
        }
        void OnDisable(){if(IsPreview)EndPreview(false);}
    }
}
