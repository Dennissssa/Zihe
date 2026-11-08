using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Mach.Editor
{
    [InitializeOnLoad]
    public static class PointHeightAuthoring
    {
        const string Request="Temp/PointHeight.request";
        static PointHeightAuthoring(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Request))return;
            if(SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;
            File.Delete(Request);
            try{Apply();}catch(Exception e){File.WriteAllText("Logs/point-height-error.txt",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("Mach/Points/Add Height Handles Only")]
        public static void Apply()
        {
            var program=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();
            if(!program||program.pointMarkers.Length!=3)throw new Exception("Expected existing A/B/C markers");
            foreach(var marker in program.pointMarkers)
            {
                if(marker.heightHandle)continue;
                var beam=marker.transform.Find("Point "+marker.pointId+" laser beam - no collider").GetComponent<Renderer>();
                var bounds=beam.bounds;
                Undo.RecordObject(marker,"Add point height control");
                marker.minHeight=bounds.min.y-marker.transform.position.y+.08f;
                marker.maxHeight=bounds.max.y-marker.transform.position.y-.08f;
                var handle=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                handle.name="Point "+marker.pointId+" Y height handle";
                Undo.RegisterCreatedObjectUndo(handle,"Add point height control");
                handle.transform.SetParent(marker.transform,false);
                handle.transform.localScale=Vector3.one*.19f;
                handle.GetComponent<SphereCollider>().isTrigger=true;
                handle.GetComponent<SphereCollider>().radius=.7f;
                handle.GetComponent<Renderer>().sharedMaterial=marker.transform.Find("Bright laser core").GetComponent<Renderer>().sharedMaterial;
                handle.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                marker.heightHandle=handle.transform;marker.SetHeight((marker.minHeight+marker.maxHeight)*.5f);
                EditorUtility.SetDirty(marker);
            }
            var drag=program.GetComponent<PointHeightDrag>();if(!drag)drag=Undo.AddComponent<PointHeightDrag>(program.gameObject);
            drag.pickup=program.GetComponent<PlayerPickup>();EditorUtility.SetDirty(drag);
            EditorSceneManager.MarkSceneDirty(program.gameObject.scene);EditorSceneManager.SaveScene(program.gameObject.scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/point-height-applied.txt",DateTime.Now+"\nSaved three trigger-only Y handles and player drag component; existing model transforms untouched.");
        }
        public static void VerifyAndBuild()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var program=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();
            if(!program.GetComponent<PointHeightDrag>())throw new Exception("Missing saved height drag component");
            foreach(var marker in program.pointMarkers)
            {
                if(!marker.heightHandle||!marker.heightHandle.GetComponent<Collider>().isTrigger)throw new Exception("Missing nonblocking handle");
                var position=marker.transform.position;float height=marker.targetHeight;
                marker.SetHeight(marker.maxHeight+10);if(Mathf.Abs(marker.Position.y-position.y-marker.maxHeight)>.001f)throw new Exception("Upper clamp failed");
                marker.SetHeight(marker.minHeight-10);if(Mathf.Abs(marker.Position.y-position.y-marker.minHeight)>.001f)throw new Exception("Lower clamp failed");
                if(marker.Position.x!=position.x||marker.Position.z!=position.z||marker.transform.position!=position)throw new Exception("Height changed X/Z or emitter");
                marker.SetHeight(height);
            }
            Physics.SyncTransforms();
            var truck=UnityEngine.Object.FindAnyObjectByType<UtilityTruck>();var driver=program.GetComponent<TruckDriver>();
            if(!driver.FindLeftExit(truck,out var exit)||Vector3.Dot(exit-truck.driverAnchor.position,-truck.transform.right)<1.99f)throw new Exception("No clear left exit two metres from seat");
            Debug.Log("POINT_HEIGHT_AND_LEFT_EXIT_VERIFIED");
            TabletAuthoring.VerifyAndBuild();
        }
    }
}
