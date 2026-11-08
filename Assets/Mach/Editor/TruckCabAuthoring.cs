using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace Mach.Editor
{
    [InitializeOnLoad] public static class TruckCabRequest
    {
        static TruckCabRequest(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Temp/TruckCab.request";
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
            File.Delete(request);
            try{AmmoMachineAuthoring.AuthorTruckCab();File.WriteAllText("Logs/truck-cab-applied.txt","Saved");}
            catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/truck-cab-error.txt",e.ToString());}
        }
    }
    public static partial class AmmoMachineAuthoring
    {
        static Transform CabFind(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
        static void CabDirty(Object value){EditorUtility.SetDirty(value);if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);}
        static void CabHide(Transform value){if(!value)return;value.gameObject.SetActive(false);CabDirty(value.gameObject);}
        static T CabComponent<T>(GameObject go) where T:Component {var c=go.GetComponent<T>();return c?c:go.AddComponent<T>();}
        static void CabHit(Transform target,bool trigger)
        {
            var collider=target.GetComponent<Collider>();
            if(!collider)
            {
                var box=target.gameObject.AddComponent<BoxCollider>();var filter=target.GetComponent<MeshFilter>();
                if(filter&&filter.sharedMesh){box.center=filter.sharedMesh.bounds.center;box.size=filter.sharedMesh.bounds.size;}
                collider=box;
            }
            collider.enabled=true;collider.isTrigger=trigger;CabDirty(collider);
        }
        static void CabControl(UtilityTruck truck,Transform target,TruckCabAction action)
        {
            if(!target)return;var control=CabComponent<TruckCabControl>(target.gameObject);control.truck=truck;control.action=action;CabDirty(control);
            foreach(var mesh in target.GetComponentsInChildren<MeshFilter>(true))CabHit(mesh.transform,true);
        }
        static void WireTruckCab(UtilityTruck truck)
        {
            var root=truck.transform;
            for(int index=0;index<2;index++)
            {
                var seat=CabFind(root,index==0?"Driver seat":"Passenger seat");if(!seat)continue;
                float x=root.InverseTransformPoint(seat.position).x;
                var parts=new[]{seat}.Concat(root.GetComponentsInChildren<Transform>(true).Where(t=>(t.name=="Seat back"||t.name=="Headrest")&&Mathf.Abs(root.InverseTransformPoint(t.position).x-x)<.25f)).ToArray();
                foreach(var part in parts)
                {
                    CabHit(part,false);var entry=CabComponent<TruckInteractable>(part.gameObject);entry.truck=truck;entry.panel=index==0?truck.driverDoor:truck.passengerDoor;entry.driverEntry=index==0;entry.seatIndex=index;entry.panelName=index==0?"Driver seat":"Passenger seat";
                    var group=CabComponent<InteractionOutlineGroup>(part.gameObject);group.parts=parts;CabDirty(entry);CabDirty(group);
                }
            }
            if(!truck.passengerAnchor)
            {
                var driverSeat=CabFind(root,"Driver seat");var passengerSeat=CabFind(root,"Passenger seat");
                var anchor=Anchor(root,"Passenger camera anchor",root.InverseTransformPoint(truck.driverAnchor.position));
                if(driverSeat&&passengerSeat)anchor.position+=passengerSeat.position-driverSeat.position;
                anchor.rotation=truck.driverAnchor.rotation;truck.passengerAnchor=anchor;
            }
            foreach(var panel in new[]{truck.driverDoor,truck.passengerDoor,truck.tailgate})if(panel)
                foreach(var collider in panel.GetComponentsInChildren<Collider>(true)){collider.isTrigger=false;CabDirty(collider);}
            CabControl(truck,truck.steeringWheel,TruckCabAction.Steering);
            var lighting=CabComponent<TruckLighting>(truck.gameObject);lighting.truck=truck;
            lighting.headLenses=truck.GetComponentsInChildren<Renderer>(true).Where(r=>r.name=="Headlight lens"||r.name=="Headlight glass").ToArray();
            lighting.headLights=truck.GetComponentsInChildren<Light>(true).Where(l=>l.name=="Authored headlamp beam").ToArray();CabDirty(lighting);
            var existing=CabFind(root,"Interactive cab instruments");
            if(!existing)BuildInteractiveCab(truck);
            CabDirty(truck);
        }
        static void BuildInteractiveCab(UtilityTruck truck)
        {
            string oldFolder=meshFolder,oldPrefix=meshPrefix;int oldIndex=meshIndex;
            meshFolder=TruckFolder+"/Meshes";meshPrefix="CabControls_";meshIndex=0;
            truckCream=LoadTruckMaterial("Ivory enamel");truckDark=LoadTruckMaterial("Graphite armor");truckRubber=LoadTruckMaterial("Tire rubber");truckMetal=LoadTruckMaterial("Brushed fittings");truckAmber=LoadTruckMaterial("Amber marker");truckLight=LoadTruckMaterial("Headlamp lens");
            try
            {
                var root=truck.transform;var interior=CabFind(root,"Interior detail assembly");
                // Only replace obsolete dashboard decorations, preserving the authored cabin and seats.
                foreach(var t in root.GetComponentsInChildren<Transform>(true).ToArray())
                    if(t.name=="Dashboard gauge"||t.name=="Instrument"||(interior&&t.IsChildOf(interior)&&(t.name.StartsWith("Radio ")||t.name=="Tuning dial index"||t.name=="Auxiliary instrument bezel"||t.name=="Auxiliary instrument face"||t.name=="Auxiliary needle"||t.name=="Instrument tick"||t.name=="Instrument legend")))CabHide(t);
                var assembly=Anchor(root,"Interactive cab instruments",Vector3.zero);
                var dashboard=assembly.gameObject.AddComponent<TruckDashboard>();dashboard.truck=truck;
                var oldGauges=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Dashboard gauge").OrderBy(t=>root.InverseTransformPoint(t.position).x).ToArray();
                string[] names={"RPM","KM/H","FUEL"};
                for(int i=0;i<3;i++)
                {
                    Vector3 position=oldGauges.Length==3?root.InverseTransformPoint(oldGauges[i].position):new Vector3(-.68f+i*.21f,1.68f,.32f);
                    position.z-=.02f;
                    var gauge=Anchor(assembly,names[i]+" live gauge",position);
                    Disc("Instrument bezel",gauge,Vector3.zero,.087f,.018f,24,truckMetal);
                    Disc("Instrument face",gauge,new Vector3(0,0,-.014f),.077f,.012f,24,truckRubber);
                    for(int tick=0;tick<=8;tick++)
                    {
                        float angle=Mathf.Lerp(-120,120,tick/8f),rad=angle*Mathf.Deg2Rad;
                        var mark=TB("Scale mark",gauge,new Vector3(Mathf.Sin(rad)*.063f,Mathf.Cos(rad)*.063f,-.023f),new Vector3(.003f,.010f,.003f),truckLight,false,.001f);
                        mark.transform.localRotation=Quaternion.Euler(0,0,-angle);
                    }
                    var needle=Anchor(gauge,"Live needle pivot",new Vector3(0,0,-.027f));
                    TB("Live needle",needle,new Vector3(0,.025f,0),new Vector3(.004f,.055f,.003f),truckAmber,false,.001f);
                    needle.localRotation=Quaternion.Euler(0,0,-120);
                    Text("Gauge units",gauge,names[i],new Vector3(0,-.044f,-.027f),.033f,truckLight);
                    Text("Gauge value",gauge,i==2?"100%":"0",new Vector3(0,-.020f,-.031f),.034f,truckLight);
                    var value=gauge.Find("Gauge value").GetComponent<TextMesh>();
                    if(i==0){dashboard.rpmNeedle=needle;dashboard.rpmReadout=value;}
                    else if(i==1){dashboard.speedNeedle=needle;dashboard.speedReadout=value;}
                    else{dashboard.fuelNeedle=needle;dashboard.fuelReadout=value;}
                }
                var grip=CabFind(root,"Handbrake grip");
                if(grip)
                {
                    var pivot=Anchor(assembly,"Handbrake animated pivot",root.InverseTransformPoint(grip.position)+new Vector3(0,-.025f,-.085f));
                    grip.SetParent(pivot,true);dashboard.handbrakePivot=pivot;CabControl(truck,pivot,TruckCabAction.Handbrake);
                }
                var center=CabFind(root,"Center control stack");
                Vector3 centerPosition=center?root.InverseTransformPoint(center.position):new Vector3(.22f,1.48f,.338f);
                for(int i=0;i<2;i++)
                {
                    var button=TB(i==0?"Headlights switch":"Tailgate switch",assembly,centerPosition+new Vector3(i==0?-.095f:.095f,.12f,-.065f),new Vector3(.16f,.09f,.03f),truckDark,true,.012f);
                    Text("Switch label",button.transform,i==0?"LIGHTS":"GATE",new Vector3(0,0,-.019f),.044f,truckLight);
                    CabControl(truck,button.transform,i==0?TruckCabAction.Lights:TruckCabAction.Tailgate);
                }
                if(center)foreach(var t in center.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("LIGHT ")).ToArray())CabHide(t);
            }
            finally{meshFolder=oldFolder;meshPrefix=oldPrefix;meshIndex=oldIndex;}
        }
        [MenuItem("Mach/Truck/Add cabin interactions")]
        public static void AuthorTruckCab()
        {
            var active=SceneManager.GetActiveScene();
            var prefab=PrefabUtility.LoadPrefabContents(TruckPrefab);
            try{WireTruckCab(prefab.GetComponent<UtilityTruck>());PrefabUtility.SaveAsPrefabAsset(prefab,TruckPrefab);}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            foreach(var path in new[]{"Assets/Mach/Scenes/WalkingPickup.unity","Assets/Mach/Scenes/Tutorial.unity"})
            {
                var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                foreach(var root in scene.GetRootGameObjects())foreach(var truck in root.GetComponentsInChildren<UtilityTruck>(true))WireTruckCab(truck);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();
        }
    }
}
