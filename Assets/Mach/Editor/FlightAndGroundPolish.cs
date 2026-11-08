using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
    [InitializeOnLoad] public static class FlightAndGroundPolish
    {
        static FlightAndGroundPolish(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/FlightAndGround.request"))return;
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;
            File.Delete("Temp/FlightAndGround.request");try{Apply();File.WriteAllText("Logs/flight-ground-applied.txt","Continuous approach, cargo reveal at five seconds, dark grey ground and lighter sky saved.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/flight-ground-error.txt",e.ToString());}
        }
        static Material GroundMaterial(string name,Color color)
        {
            var path="Assets/Mach/Lighting/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Smoothness",.05f);EditorUtility.SetDirty(m);return m;
        }
        public static void Apply()
        {
            var drop=UnityEngine.Object.FindAnyObjectByType<EquipmentAirdrop>();if(!drop||!drop.aircraft)throw new Exception("Missing supply aircraft");
            Undo.RecordObject(drop,"Continuous fly-in and reveal");Undo.RecordObject(drop.transform,"Independent cargo motion");Undo.RecordObject(drop.aircraft,"Aircraft starts far away");
            var releasePlane=drop.releasePosition-drop.aircraft.rotation*new Vector3(0,-1.65f,-2.5f);
            drop.flightDelay=0;drop.releaseAfter=5;drop.planeStart=releasePlane-drop.flightDirection.normalized*drop.flightSpeed*5;
            drop.transform.SetParent(null,true);drop.transform.SetPositionAndRotation(drop.releasePosition,Quaternion.identity);drop.aircraft.position=drop.planeStart;
            var body=drop.GetComponent<Rigidbody>();Undo.RecordObject(body,"Independent kinematic cargo");body.interpolation=RigidbodyInterpolation.None;body.isKinematic=true;body.useGravity=false;
            if(drop.cargoRenderers==null||drop.cargoRenderers.Length==0)drop.cargoRenderers=drop.GetComponentsInChildren<Renderer>(true).Where(r=>r.gameObject.activeInHierarchy&&r.enabled&&!r.transform.IsChildOf(drop.parachute)).ToArray();
            if(drop.cargoColliders==null||drop.cargoColliders.Length==0)drop.cargoColliders=drop.GetComponentsInChildren<Collider>(true).Where(c=>c.gameObject.activeInHierarchy&&c.enabled).ToArray();
            foreach(var r in drop.cargoRenderers){Undo.RecordObject(r,"Show cargo only at release");r.enabled=false;EditorUtility.SetDirty(r);}
            foreach(var c in drop.cargoColliders){Undo.RecordObject(c,"Enable cargo collision at release");c.enabled=false;EditorUtility.SetDirty(c);}
            var ground=GroundMaterial("Dark grey landscape",new Color(.26f,.28f,.30f));var road=GroundMaterial("Dark grey road",new Color(.32f,.34f,.36f));int changed=0;
            foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>())
            {
                bool route=r.name=="Continuous uphill road";bool terrain=r.name=="Faceted mountain and summit"||r.name=="Foot loading yard"||(r.name=="Floor"&&!r.transform.parent);
                if(!route&&!terrain)continue;Undo.RecordObject(r,"Dark grey terrain");r.sharedMaterial=route?road:ground;EditorUtility.SetDirty(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r);changed++;
            }
            var sky=RenderSettings.skybox;if(sky&&sky.HasProperty("_ZenithColor")){Undo.RecordObject(sky,"Lighter pale blue sky");sky.SetColor("_ZenithColor",new Color(.68f,.83f,.93f));sky.SetColor("_HorizonColor",new Color(.89f,.95f,.98f));sky.SetColor("_NadirColor",new Color(.83f,.90f,.95f));EditorUtility.SetDirty(sky);}
            EditorUtility.SetDirty(drop);EditorUtility.SetDirty(body);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(drop.gameObject.scene);EditorSceneManager.SaveScene(drop.gameObject.scene);
            File.WriteAllText("Logs/flight-ground-layout.txt",$"Aircraft starts {drop.planeStart}\nRelease after 5s at {drop.releasePosition}\nHidden cargo renderers {drop.cargoRenderers.Length}\nGround renderers recolored {changed}");
            var player=UnityEngine.Object.FindAnyObjectByType<FirstPersonWalker>();if(player)SoftSkyAuthoring.Preview(player.GetComponentInChildren<Camera>());
        }
        public static void Verify()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var drop=UnityEngine.Object.FindAnyObjectByType<EquipmentAirdrop>();var start=drop.aircraft.position;
            if(drop.transform.parent||drop.cargoRenderers.Any(r=>r.enabled))throw new Exception("Cargo visible or parented before release");
            drop.Advance(1);if(Vector3.Distance(start,drop.aircraft.position)<50||drop.Released)throw new Exception("Aircraft waits in sky");
            drop.Advance(3.99f);if(drop.Released||drop.cargoRenderers.Any(r=>r.enabled))throw new Exception("Early cargo reveal");
            drop.Advance(.011f);if(!drop.Released||drop.cargoRenderers.Any(r=>!r.enabled)||drop.cargoColliders.Any(c=>!c.enabled))throw new Exception("Cargo missing at release");
            float y=drop.transform.position.y;drop.Advance(1);if(drop.transform.position.y>=y||drop.transform.parent)throw new Exception("Cargo dragged by aircraft");
            drop.Advance(9.01f);if(!drop.Landed||Vector3.Distance(drop.transform.position,drop.landingPosition)>.01f)throw new Exception("Landing failed");
            var placement=UnityEngine.Object.FindAnyObjectByType<MachinePlacement>();if(!placement.Begin(drop.GetComponent<MovableMachine>()))throw new Exception("Cannot carry landed cargo");placement.Cancel();
            foreach(var slot in drop.GetComponent<EquipmentRack>().slots){if(!placement.Begin(slot.machine))throw new Exception("Cannot unpack "+slot.name);placement.Cancel();}
            Debug.Log("FLIGHT_GROUND_VERIFIED: continuous flight, invisible cargo until 5s, independent descent, landing and retrieval");EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
        }
    }
}
