using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
    [InitializeOnLoad] public static class SummitAirdropAuthoring
    {
        static SummitAirdropAuthoring(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/SummitAirdrop.request"))return;
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;
            File.Delete("Temp/SummitAirdrop.request");try{Apply();File.WriteAllText("Logs/summit-airdrop-applied.txt","Seven packed machines authored in airborne rack. Player and truck placed on summit.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/summit-airdrop-error.txt",e.ToString());}
        }
        public static void Apply()
        {
            var rack=UnityEngine.Object.FindAnyObjectByType<EquipmentRack>();var truck=UnityEngine.Object.FindAnyObjectByType<UtilityTruck>();var player=UnityEngine.Object.FindAnyObjectByType<FirstPersonWalker>();
            var mountain=GameObject.Find("Faceted mountain and summit").GetComponent<MeshCollider>();
            if(!rack||!truck||!player||!mountain)throw new Exception("Missing summit setup objects");
            var scene=rack.gameObject.scene;EditorSceneManager.SaveScene(scene,"Logs/Before-airdrop-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            Undo.RegisterFullObjectHierarchyUndo(rack.gameObject,"Pack initial equipment");
            foreach(var slot in rack.slots)
            {
                Undo.RegisterFullObjectHierarchyUndo(slot.machine.gameObject,"Pack initial machine");slot.Pack();EditorUtility.SetDirty(slot);EditorUtility.SetDirty(slot.machine.gameObject);PrefabUtility.RecordPrefabInstancePropertyModifications(slot.machine.transform);
            }
            Vector3 Ground(float x,float z)
            {
                var p=mountain.transform.TransformPoint(new Vector3(x,36,z));
                if(!mountain.Raycast(new Ray(p+Vector3.up*100,Vector3.down),out var hit,200))throw new Exception("No summit ground at "+p);
                return hit.point;
            }
            Undo.RecordObject(rack.transform,"Airborne equipment pallet");rack.transform.SetParent(null,true);rack.transform.rotation=Quaternion.identity;rack.transform.position=Ground(-3,106)+Vector3.up*14;
            var body=rack.GetComponent<Rigidbody>();if(!body)body=Undo.AddComponent<Rigidbody>(rack.gameObject);
            Undo.RecordObject(body,"Airdrop gravity");body.isKinematic=false;body.useGravity=true;body.mass=180;body.linearDamping=.15f;body.angularDamping=4;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.constraints=RigidbodyConstraints.FreezeRotation;body.solverIterations=12;
            if(!rack.GetComponent<EquipmentAirdrop>())Undo.AddComponent<EquipmentAirdrop>(rack.gameObject);
            Undo.RecordObject(truck.transform,"Truck starts on summit");truck.transform.rotation=Quaternion.identity;
            float wheelBottom=float.PositiveInfinity;foreach(var w in truck.wheels)wheelBottom=Mathf.Min(wheelBottom,w.transform.TransformPoint(w.center).y-truck.transform.position.y-w.radius-w.suspensionDistance*.5f);
            truck.transform.position=Ground(6,106)+Vector3.up*(.08f-wheelBottom);
            Undo.RecordObject(player.transform,"Player starts on summit");var controller=player.GetComponent<CharacterController>();
            player.transform.position=Ground(-3,99)+Vector3.up*(.12f-controller.center.y+controller.height*.5f);player.transform.rotation=Quaternion.identity;
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(truck.transform);
            Physics.SyncTransforms();EditorUtility.SetDirty(body);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/summit-airdrop-layout.txt",$"Player {player.transform.position}\nTruck {truck.transform.position}\nDrop {rack.transform.position}\nLanding {Ground(-3,106)}\nPacked {rack.slots.Length}");
        }
        public static void Verify()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var rack=UnityEngine.Object.FindAnyObjectByType<EquipmentRack>();
            foreach(var s in rack.slots)if(!s.Packed||s.machine.gameObject.activeSelf||!s.box.activeSelf||s.machine.transform.parent!=rack.transform)throw new Exception("Initial packing not saved: "+s.name);
            var body=rack.GetComponent<Rigidbody>();float start=body.position.y;var mode=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
            try{for(int i=0;i<240;i++)Physics.Simulate(.02f);}finally{Physics.simulationMode=mode;}
            if(body.position.y>start-10||body.position.y<start-16)throw new Exception("Airdrop did not land on summit: "+body.position);
            var placement=UnityEngine.Object.FindAnyObjectByType<MachinePlacement>();foreach(var slot in rack.slots){if(!placement.Begin(slot.machine)||!slot.machine.gameObject.activeSelf||slot.Packed)throw new Exception("Cannot unpack "+slot.name);placement.Cancel();if(!slot.Packed)throw new Exception("Cannot cancel/repack "+slot.name);}
            Debug.Log("SUMMIT_AIRDROP_VERIFIED: serialized packing, physical landing, seven unpack/cancel cycles");EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
        }
    }
}
