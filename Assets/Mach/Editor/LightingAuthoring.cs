using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace Mach.Editor
{
    [InitializeOnLoad]
    public static class LightingAuthoring
    {
        const string Folder="Assets/Mach/Lighting";
        const string Request="Temp/MachLighting.request";
        static LightingAuthoring(){EditorApplication.update+=CheckRequest;}
        static void CheckRequest()
        {
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Request))return;
            if(SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;
            File.Delete(Request);
            try{ApplyCurrentScene();}catch(Exception e){File.WriteAllText("Logs/lighting-apply-error.txt",e.ToString());Debug.LogException(e);}
        }
        static Material Emissive(Renderer renderer,string role,Color color,float strength)
        {
            string path=Folder+"/"+role+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(renderer.sharedMaterial);material.name=role;material.shader=Shader.Find("Universal Render Pipeline/Lit");AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color);material.SetColor("_EmissionColor",color*strength);material.EnableKeyword("_EMISSION");material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;EditorUtility.SetDirty(material);
            Undo.RecordObject(renderer,"Assign lamp emission");renderer.sharedMaterial=material;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);return material;
        }
        static Light Lamp(Transform parent,string name,Vector3 worldPosition,Vector3 direction,Color color,float intensity,float range,bool spot=false)
        {
            var child=parent.Find(name);if(!child){var go=new GameObject(name,typeof(Light));Undo.RegisterCreatedObjectUndo(go,"Add authored lamp");child=go.transform;child.SetParent(parent,false);}
            child.position=worldPosition;child.rotation=Quaternion.LookRotation(direction);
            var lamp=child.GetComponent<Light>();lamp.type=spot?LightType.Spot:LightType.Point;lamp.color=color;lamp.intensity=intensity;lamp.range=range;lamp.spotAngle=60;lamp.innerSpotAngle=36;lamp.shadows=spot?LightShadows.Soft:LightShadows.None;lamp.renderMode=LightRenderMode.ForcePixel;lamp.cullingMask=~(1<<30);return lamp;
        }
        [MenuItem("Mach/Lighting/Apply to Current Scene (Preserve Models)")]
        public static void ApplyCurrentScene()
        {
            var scene=SceneManager.GetActiveScene();if(scene.path!=WalkingSceneAuthoring.ScenePath)throw new Exception("Open the existing WalkingPickup scene first.");
            Directory.CreateDirectory(Folder);Directory.CreateDirectory("Logs");AssetDatabase.Refresh();
            // Snapshot every existing transform and mesh; lamp authoring must preserve all user modelling edits.
            var original=new Dictionary<Transform,(Vector3,Quaternion,Vector3,Transform)>();var meshes=new Dictionary<MeshFilter,Mesh>();
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))original[t]=(t.localPosition,t.localRotation,t.localScale,t.parent);
            foreach(var root in scene.GetRootGameObjects())foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true))meshes[mesh]=mesh.sharedMesh;
            var truck=UnityEngine.Object.FindAnyObjectByType<UtilityTruck>();if(!truck)throw new Exception("Truck missing");
            var control=truck.GetComponent<TruckLighting>();if(!control)control=Undo.AddComponent<TruckLighting>(truck.gameObject);Undo.RecordObject(control,"Wire brake lamps");control.truck=truck;
            control.tailColor=new Color(1,.003f,.001f);control.brakeEmission=10;
            var tails=new List<Renderer>();var lights=new List<Light>();int heads=0,markers=0,instruments=0;
            foreach(var r in truck.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(r.GetComponent<TextMesh>())continue;string name=r.name.ToLowerInvariant();
                if(name=="headlight lens"||name=="headlight glass")
                {Emissive(r,"Headlamp emission",new Color(1,.88f,.65f),11);Lamp(r.transform,"Authored headlamp beam",r.bounds.center+truck.transform.forward*.08f,truck.transform.forward-Vector3.up*.08f,new Color(1,.91f,.76f),7,26,true);heads++;}
                else if(name=="tail red lens"||name=="red lens")
                {Emissive(r,"Tail lamp emission",control.tailColor,control.tailEmission);tails.Add(r);lights.Add(Lamp(r.transform,"Authored tail glow",r.bounds.center-truck.transform.forward*.05f,-truck.transform.forward,control.tailColor,.25f,1.6f));}
                else if(r.sharedMaterial&&r.sharedMaterial.name=="Amber marker"&& !name.Contains("legend"))
                {Emissive(r,"Amber marker emission",new Color(1,.3f,.015f),5);markers++;}
                else if(name.Contains("instrument tick")||name.Contains("needle")||name.Contains("dial index")||name.EndsWith(" indicator"))
                {Emissive(r,"Instrument backlight emission",new Color(.3f,1,.76f),3.5f);instruments++;}
            }
            control.tailLenses=tails.ToArray();control.tailLights=lights.ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(control);
            var robot=UnityEngine.Object.FindAnyObjectByType<RobotAssemblyRig>();
            foreach(var r in robot.GetComponentsInChildren<MeshRenderer>(true))
            {
                string name=r.name.ToLowerInvariant();if(name=="front amber marker"||name=="dock amber status")
                {Emissive(r,"Robot marker emission",new Color(1,.35f,.018f),7);Lamp(r.transform,"Authored robot marker glow",r.bounds.center+Vector3.up*.03f,robot.transform.forward,new Color(1,.4f,.04f),.35f,.9f);markers++;}
                else if(name=="blue optic glass"||name=="secondary glass")Emissive(r,"Robot optic emission",new Color(.015f,.35f,1),5);
            }
            foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                if(r.gameObject.scene!=scene||r.GetComponent<TextMesh>())continue;
                if(r.name=="Portrait display glass"||r.name=="Dark display glass")
                {var material=Emissive(r,"Instrument screen emission",new Color(.012f,.05f,.08f),2);material.SetFloat("_Smoothness",0);material.SetFloat("_Metallic",0);material.SetFloat("_SpecularHighlights",0);material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");instruments++;}
            }
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Folder+"/Industrial glow.asset");
            if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Folder+"/Industrial glow.asset");}
            if(!profile.TryGet<Bloom>(out var bloom))bloom=profile.Add<Bloom>(true);bloom.active=true;bloom.threshold.Override(.85f);bloom.intensity.Override(1.05f);bloom.scatter.Override(.72f);bloom.highQualityFiltering.Override(true);
            if(!profile.TryGet<ColorAdjustments>(out var grading))grading=profile.Add<ColorAdjustments>(true);grading.postExposure.Override(-.2f);grading.contrast.Override(10);grading.saturation.Override(-6);
            if(!profile.TryGet<Tonemapping>(out var tone))tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
            foreach(var component in profile.components){if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,profile);EditorUtility.SetDirty(component);}EditorUtility.SetDirty(profile);
            var volumeObject=GameObject.Find("Industrial lighting and bloom");if(!volumeObject){volumeObject=new GameObject("Industrial lighting and bloom",typeof(Volume));Undo.RegisterCreatedObjectUndo(volumeObject,"Add lighting post process");}
            var volume=volumeObject.GetComponent<Volume>();volume.isGlobal=true;volume.priority=10;volume.sharedProfile=profile;
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerPickup>();var main=player.viewCamera;var mainData=main.GetUniversalAdditionalCameraData();main.allowHDR=true;mainData.renderPostProcessing=mainData.cameraStack.Count==0;PrefabUtility.RecordPrefabInstancePropertyModifications(main);PrefabUtility.RecordPrefabInstancePropertyModifications(mainData);
            var font=ProjectTypography.Font;var fontMaterials=new Material[2];
            for(int i=0;i<2;i++){string path=Folder+(i==0?"/Depth tested labels.mat":"/Emissive display text.mat");var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){material=new Material(Shader.Find("Mach/Emissive World Font"));AssetDatabase.CreateAsset(material,path);}material.SetFloat("_GlowStrength",i==0?1:3);material.mainTexture=font.material.mainTexture;EditorUtility.SetDirty(material);fontMaterials[i]=material;}
            foreach(var label in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include))
            {
                if(label.gameObject.scene!=scene||label.font!=font)continue;
                bool display=label.transform.parent.name=="Ammo selection display"||(label.GetComponentInParent<FireRateConsole>()&&label.transform.parent.name=="03 • Inclined controls");
                var renderer=label.GetComponent<Renderer>();Undo.RecordObject(renderer,"Set readable illuminated labels");renderer.sharedMaterial=fontMaterials[display?1:0];PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            var atlas=player.GetComponent<FontEmissionAtlas>();if(!atlas)atlas=Undo.AddComponent<FontEmissionAtlas>(player.gameObject);atlas.font=font;atlas.materials=fontMaterials;
            foreach(var overlay in mainData.cameraStack){overlay.allowHDR=true;var data=overlay.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.volumeLayerMask=~0;PrefabUtility.RecordPrefabInstancePropertyModifications(overlay);PrefabUtility.RecordPrefabInstancePropertyModifications(data);}
            foreach(var pair in original){var t=pair.Key;var v=pair.Value;if(!t||t.localPosition!=v.Item1||t.localRotation!=v.Item2||t.localScale!=v.Item3||t.parent!=v.Item4)throw new Exception("Existing transform changed: "+(t?t.name:"deleted"));}
            foreach(var pair in meshes)if(!pair.Key||pair.Key.sharedMesh!=pair.Value)throw new Exception("Existing model mesh changed");
            if(heads!=2||tails.Count!=2)throw new Exception("Expected 2 headlights and 2 tail lenses; found "+heads+" / "+tails.Count);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            string result="LIGHTING_APPLIED: "+heads+" headlights, "+tails.Count+" brake/tail lamps, "+markers+" markers, "+instruments+" instruments. Preserved "+original.Count+" existing transforms and "+meshes.Count+" mesh references.";
            Debug.Log(result);File.WriteAllText("Logs/lighting-applied.txt",DateTime.Now+"\n"+result);
        }
        public static void ApplyBatch()
        {EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);ApplyCurrentScene();}
    }
}
