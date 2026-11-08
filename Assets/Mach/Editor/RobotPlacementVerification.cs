using System;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
    public static class RobotPlacementVerification
    {
        public static void Verify()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var motor=UnityEngine.Object.FindAnyObjectByType<RobotGroundMotor>();
            var machine=motor.GetComponent<MovableMachine>();
            var placement=UnityEngine.Object.FindAnyObjectByType<MachinePlacement>();
            EquipmentSlot slot=null;
            foreach(var s in UnityEngine.Object.FindObjectsByType<EquipmentSlot>())if(s.machine==machine)slot=s;
            Require(slot,"Robot rack slot exists");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Sloped placement ground";
            floor.transform.position=new Vector3(400,30,400);floor.transform.localScale=new Vector3(16,.3f,16);
            foreach(float slope in new[]{0f,1.49f,15f,30f})
            {
                machine.BeginPreview();slot.Pack();
                floor.transform.rotation=Quaternion.Euler(slope,0,0);
                var top=floor.transform.position+floor.transform.up*.15f;
                placement.viewCamera.transform.position=top+new Vector3(0,2.4f,-2.4f);
                placement.viewCamera.transform.LookAt(top);Physics.SyncTransforms();
                Require(placement.Begin(machine),"Unpack starts preview");placement.UpdatePreview();
                Require(placement.CanPlace,"Unpacked robot slope "+slope+": "+placement.PlacementError);
                Require(placement.Confirm(),"Confirm robot placement");
                Require(!motor.body.isKinematic&&motor.body.useGravity,"Restores dynamic gravity");
                var previous=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
                try{for(int i=0;i<40;i++)Physics.Simulate(.02f);}
                finally{Physics.simulationMode=previous;}
                Require(motor.transform.position.y>28&&motor.transform.position.y<33,"Settles without falling through ground");
                Debug.Log("ROBOT_PLACEMENT_PASS slope="+slope);
            }
            floor.transform.rotation=Quaternion.identity;
            var target=floor.transform.position+Vector3.up*.15f;
            placement.viewCamera.transform.position=target+new Vector3(0,2.4f,-2.4f);placement.viewCamera.transform.LookAt(target);
            Physics.SyncTransforms();Require(placement.Begin(machine),"Begin obstacle test");placement.UpdatePreview();Require(placement.CanPlace,"Clear test area");
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Blocking wall";wall.transform.position=machine.transform.position+Vector3.up*.5f;wall.transform.localScale=new Vector3(3,2,3);Physics.SyncTransforms();
            Require(!placement.Confirm(),"Actual collider intersection blocks placement");placement.Cancel();
            Require(!motor.body.isKinematic,"Cancel restores body");
            UnityEngine.Object.DestroyImmediate(wall);
            MeshCollider mountain=null;
            foreach(var c in UnityEngine.Object.FindObjectsByType<MeshCollider>())if(c.name.ToLowerInvariant().Contains("faceted")&&c.name.ToLowerInvariant().Contains("mountain")){mountain=c;break;}
            Require(mountain,"Scene mountain collider exists");
            var terrainCopy=new GameObject("Mountain placement test");terrainCopy.transform.SetPositionAndRotation(mountain.transform.position+new Vector3(800,0,800),mountain.transform.rotation);terrainCopy.transform.localScale=mountain.transform.lossyScale;
            var terrainCollider=terrainCopy.AddComponent<MeshCollider>();terrainCollider.sharedMesh=mountain.sharedMesh;Physics.SyncTransforms();
            Require(terrainCollider.Raycast(new Ray(terrainCollider.bounds.center+Vector3.up*(terrainCollider.bounds.extents.y+10),Vector3.down),out var ground,500),"Find actual faceted mountain surface");
            placement.viewCamera.transform.position=ground.point+new Vector3(0,2.4f,-2.4f);placement.viewCamera.transform.LookAt(ground.point);
            machine.BeginPreview();slot.Pack();Require(placement.Begin(machine),"Unpack on actual mountain");Physics.SyncTransforms();placement.UpdatePreview();
            Require(placement.CanPlace,"Actual mountain: "+placement.PlacementError);Require(placement.Confirm(),"Confirm on actual mountain");
            Debug.Log("ROBOT_MOUNTAIN_PLACEMENT_PASS");UnityEngine.Object.DestroyImmediate(terrainCopy);
            Debug.Log("ROBOT_PLACEMENT_VERIFIED");
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
        }
        static void Require(bool pass,string message){if(!pass)throw new Exception(message);}
    }
}
