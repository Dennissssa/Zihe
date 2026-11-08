using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
    [InitializeOnLoad] public static class DropLandingRefinement
    {
        static DropLandingRefinement(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/DropLanding.request"))return;
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;
            File.Delete("Temp/DropLanding.request");try{Apply();File.WriteAllText("Logs/drop-landing-applied.txt","Forward landing and five-second wait saved.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/drop-landing-error.txt",e.ToString());}
        }
        public static void Apply()
        {
            var drop=UnityEngine.Object.FindAnyObjectByType<EquipmentAirdrop>();if(!drop||!drop.aircraft)throw new Exception("Missing existing supply flight");
            Undo.RecordObject(drop,"Forward safe landing and delayed flight");
            var target=drop.landingPosition+Vector3.ProjectOnPlane(drop.flightDirection,Vector3.up).normalized*25;
            target.y=-100;Physics.SyncTransforms();target=drop.GroundClearance(target);if(target.y<-90)throw new Exception("No ground in front of old landing position");
            drop.landingPosition=target;drop.landingNormal=Vector3.up;drop.flightDelay=5;
            var planeRelease=target-drop.flightDirection.normalized*20;planeRelease.y=drop.planeStart.y;
            drop.releaseAfter=drop.flightDelay+Vector3.Dot(planeRelease-drop.planeStart,drop.flightDirection.normalized)/drop.flightSpeed;
            drop.releasePosition=planeRelease+drop.aircraft.rotation*drop.transform.localPosition;
            EditorUtility.SetDirty(drop);EditorSceneManager.MarkSceneDirty(drop.gameObject.scene);EditorSceneManager.SaveScene(drop.gameObject.scene);
            File.WriteAllText("Logs/drop-landing-layout.txt",$"Wait {drop.flightDelay}s\nRelease {drop.releaseAfter:F2}s\nLanding {drop.landingPosition}\nGlide {drop.descentSeconds}s");
        }
        public static void Verify()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var drop=UnityEngine.Object.FindAnyObjectByType<EquipmentAirdrop>();var start=drop.aircraft.position;
            drop.Advance(4.99f);if(drop.Released||Vector3.Distance(start,drop.aircraft.position)>.001f)throw new Exception("Plane moved during initial five-second wait");
            drop.Advance(drop.releaseAfter-4.99f+.001f);if(!drop.Released)throw new Exception("No aircraft release");
            for(int i=0;i<101;i++)
            {
                drop.Advance(.1f);Physics.SyncTransforms();var p=drop.transform.position;
                if(drop.GroundClearance(p).y>p.y+.001f)throw new Exception("Pallet penetrates ground during descent");
            }
            if(!drop.Landed||drop.landingPosition.z>5)throw new Exception("Wrong forward landing");
            var floor=drop.GetComponent<BoxCollider>();foreach(var other in Physics.OverlapBox(floor.bounds.center,floor.bounds.extents,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
            {if(other.transform.IsChildOf(drop.transform))continue;if(Physics.ComputePenetration(floor,floor.transform.position,floor.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out float distance)&&distance>.005f)throw new Exception("Landing overlaps "+other.name);}
            var placement=UnityEngine.Object.FindAnyObjectByType<MachinePlacement>();if(!placement.Begin(drop.GetComponent<MovableMachine>()))throw new Exception("Landed rack cannot be carried");placement.Cancel();
            Debug.Log("FORWARD_DROP_VERIFIED: initial five-second wait, flight release, 10-second terrain-safe descent, clear landing and pickup");EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
        }
    }
}
