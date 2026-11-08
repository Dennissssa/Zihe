using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mach.Editor
{
    // Runs only in the isolated verification project, never saves test objects.
    public static class CargoTransportVerification
    {
        static void Check(bool condition,string message)
        {if(!condition)throw new Exception("CARGO FAILED: "+message);Debug.Log("CARGO PASS: "+message);}
        public static void VerifyAndBuild()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var truck=UnityEngine.Object.FindAnyObjectByType<UtilityTruck>();
            var placement=UnityEngine.Object.FindAnyObjectByType<MachinePlacement>();
            var machines=UnityEngine.Object.FindObjectsByType<MovableMachine>();
            var cargo=machines;
            placement.viewCamera=null;placement.pickup=null;placement.tablet=null;
            var floor=truck.GetComponentsInChildren<Collider>().First(c=>c.name=="Cargo bed floor");
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.position=new Vector3(30,100,0);ground.transform.localScale=new Vector3(10,.1f,10);
            foreach(var item in cargo)
            {
                var meshColliders=item.GetComponentsInChildren<MeshCollider>(true);
                var originalConvex=meshColliders.Select(c=>c.convex).ToArray();
                truck.transform.SetPositionAndRotation(new Vector3(0,100,0),Quaternion.identity);Physics.SyncTransforms();
                Vector3 deck=new Vector3(floor.bounds.center.x,floor.bounds.max.y,floor.bounds.center.z);
                Check(placement.Begin(item),item.name+" begins ghost preview");
                bool fits=placement.PreviewAt(deck,true,truck);
                if(!fits){placement.Rotate(90/placement.rotationStep);fits=placement.PreviewAt(deck,true,truck);}
                Check(fits,item.name+" fits actual truck bed");
                Check(placement.Confirm()&&item.transform.parent==truck.transform,item.name+" attaches to truck");
                Check(meshColliders.All(c=>c.convex),item.name+" has only convex meshes under dynamic truck");
                var local=item.transform.localPosition;var rotation=item.transform.localRotation;
                truck.transform.SetPositionAndRotation(new Vector3(5,101,8),Quaternion.Euler(8,65,4));Physics.SyncTransforms();
                Check(Vector3.Distance(item.transform.position,truck.transform.TransformPoint(local))<.001f,item.name+" follows translation, steering and suspension tilt");
                Check(Quaternion.Angle(item.transform.rotation,truck.transform.rotation*rotation)<.01f,"Cargo rotation follows truck");
                Check(placement.Begin(item)&&item.transform.parent==null,"Pickup detaches cargo");
                truck.transform.position+=Vector3.right*2;placement.Cancel();
                Check(meshColliders.All(c=>c.convex),"Cancel restores dynamic-compatible collision");
                Check(item.transform.parent==truck.transform&&Vector3.Distance(item.transform.localPosition,local)<.001f,"Cancel restores truck-local position after truck moves");
                Check(placement.Begin(item)&&placement.PreviewAt(new Vector3(30,100.05f,0),true)&&placement.Confirm(),"Unload onto ground");
                var world=item.transform.position;truck.transform.position+=Vector3.forward*10;
                Check(item.transform.parent==null&&Vector3.Distance(item.transform.position,world)<.001f,"Unloaded cargo stays on ground");
                Check(meshColliders.Select((c,i)=>c.convex==originalConvex[i]).All(v=>v),"Unload restores authored convex flags");
                var marker=item.GetComponent<CircuitPointMarker>();
                if(marker)Check(Vector3.Distance(marker.Position,world+Vector3.up*marker.targetHeight)<.001f,"Beacon target resolves at unloaded location");
                item.transform.position+=Vector3.right*20;Physics.SyncTransforms();
            }
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            Debug.Log("CARGO_TRANSPORT_VERIFIED");TabletAuthoring.VerifyAndBuild();
        }
    }
}
