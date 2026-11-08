using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Mach.Editor
{
    [InitializeOnLoad] public static class TruckCabRevision
    {
        static TruckCabRevision(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Temp/TruckCabRevision.request";
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
            File.Delete(request);
            try{Apply();File.WriteAllText("Logs/truck-cab-revision-applied.txt","Saved");}
            catch(Exception e){File.WriteAllText("Logs/truck-cab-revision-error.txt",e.ToString());Debug.LogException(e);}
        }
        static void Dirty(UnityEngine.Object obj){EditorUtility.SetDirty(obj);if(PrefabUtility.IsPartOfPrefabInstance(obj))PrefabUtility.RecordPrefabInstancePropertyModifications(obj);}
        static void UpdateTruck(UtilityTruck truck,StringBuilder layout)
        {
            // Capture only the existing cabin layout; never rebuild, reposition or restore deleted objects.
            layout.AppendLine(truck.gameObject.scene.path+" / "+truck.name);
            foreach(var t in truck.GetComponentsInChildren<Transform>(true).Where(t=>t.name.EndsWith("live gauge")||t.name=="Headlights switch"||t.name=="Tailgate switch"))
                layout.AppendLine(t.name+" position="+t.localPosition+" rotation="+t.localEulerAngles+" scale="+t.localScale);
            truck.maxSpeed=120/2.236936f;truck.fuelCapacityGallons=100;truck.redlineRpm=8000;truck.upshiftRpm=5500;truck.downshiftRpm=2200;
            truck.gearRatios=new[]{3.2f,2.1f,1.45f,1f};truck.finalDrive=3.7f;Dirty(truck);
            foreach(var panel in new[]{truck.driverDoor,truck.passengerDoor,truck.tailgate})if(panel)
                foreach(var c in panel.GetComponentsInChildren<Collider>(true)){c.isTrigger=false;Dirty(c);}
            foreach(var dashboard in truck.GetComponentsInChildren<TruckDashboard>(true))
            {
                dashboard.maximumMph=120;dashboard.maximumRpm=8000;
                if(dashboard.fuelNeedle){dashboard.fuelNeedle.localRotation=Quaternion.Euler(0,0,-120);Dirty(dashboard.fuelNeedle);}
                if(dashboard.fuelReadout){dashboard.fuelReadout.text="F";Dirty(dashboard.fuelReadout);}
                foreach(var label in dashboard.GetComponentsInChildren<TextMesh>(true))
                    if(label.name=="Gauge units"&&label.text=="KM/H"){label.text="MPH";Dirty(label);}
                Dirty(dashboard);
            }
            foreach(var control in truck.GetComponentsInChildren<TruckCabControl>(true))
            {
                if(control.action!=TruckCabAction.Lights&&control.action!=TruckCabAction.Tailgate)continue;
                // Assign just the button cap, keeping its label white and all moved transforms intact.
                control.stateRenderer=control.GetComponent<Renderer>();
                if(!control.stateRenderer)control.stateRenderer=control.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r=>!r.GetComponent<TextMesh>());
                if(control.stateRenderer)
                {
                    const string path="Assets/Mach/UtilityTruck/Materials/Cab switch state.mat";
                    var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(!material){material=new Material(control.stateRenderer.sharedMaterial);material.name="Cab switch state";material.SetColor("_BaseColor",control.inactiveColor);AssetDatabase.CreateAsset(material,path);}
                    control.stateRenderer.sharedMaterial=material;Dirty(control.stateRenderer);
                }
                Dirty(control);control.RefreshStateColor();
            }
        }
        static void Apply()
        {
            var layout=new StringBuilder();var active=SceneManager.GetActiveScene();
            const string path="Assets/Mach/UtilityTruck/UtilityTruck.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try{UpdateTruck(prefab.GetComponent<UtilityTruck>(),layout);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
            foreach(var scenePath in new[]{"Assets/Mach/Scenes/WalkingPickup.unity","Assets/Mach/Scenes/Tutorial.unity"})
            {
                var scene=SceneManager.GetSceneByPath(scenePath);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
                foreach(var root in scene.GetRootGameObjects())foreach(var truck in root.GetComponentsInChildren<UtilityTruck>(true))UpdateTruck(truck,layout);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();File.WriteAllText("Logs/truck-cab-current-layout.txt",layout.ToString());
        }
    }
}
