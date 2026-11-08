using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mach.Editor
{
    public static partial class AmmoMachineAuthoring
    {
        const string PlacementFolder="Assets/Mach/Placement";
        [MenuItem("Mach/Enable Machine Placement")]
        public static void EnableMachinePlacement()
        {
            Directory.CreateDirectory(PlacementFolder);AssetDatabase.Refresh();
            var good=GhostMaterial("Valid cyan ghost",new Color(.12f,.78f,.83f,.27f));
            var bad=GhostMaterial("Blocked red ghost",new Color(.95f,.16f,.12f,.3f));
            ConfigureMovable(PrefabPath,"Ammo console","05 • Front rotary selector/Selector knob",good,bad);
            ConfigureMovable(RatePrefab,"Fire rate console","06 • Rotary trim control/Rotary knob",good,bad);
            const string playerPath="Assets/Mach/Prefabs/FirstPersonPlayer.prefab";
            var player=PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                var placement=player.GetComponent<MachinePlacement>();if(!placement)placement=player.AddComponent<MachinePlacement>();
                var pickup=player.GetComponent<PlayerPickup>();var tablet=player.GetComponent<HeldTablet>();
                placement.viewCamera=pickup.viewCamera;placement.pickup=pickup;placement.tablet=tablet;
                pickup.placement=placement;tablet.placement=placement;
                player.transform.Find("HUD/Controls").GetComponent<UnityEngine.UI.Text>().text="LMB Buttons  |  E Pick up / Move / Place  |  Wheel Rotate  |  Tab Tablet  |  Esc Cancel";
                PrefabUtility.SaveAsPrefabAsset(player,playerPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(player);}
            AssetDatabase.SaveAssets();VerifyMachinePlacement();TabletAuthoring.VerifyAndBuild();
        }
        static Material GhostMaterial(string name,Color color)
        {
            string path=PlacementFolder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material)return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=color;
            material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);material.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);
            material.SetFloat("_Smoothness",.2f);material.SetFloat("_Metallic",0);material.SetOverrideTag("RenderType","Transparent");material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=(int)RenderQueue.Transparent;
            material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(color.r,color.g,color.b)*.2f);material.SetShaderPassEnabled("ShadowCaster",false);
            AssetDatabase.CreateAsset(material,path);return material;
        }
        static void ConfigureMovable(string path,string name,string knobPath,Material good,Material bad)
        {
            var machine=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var mover=machine.GetComponent<MovableMachine>();if(!mover)mover=machine.AddComponent<MovableMachine>();mover.displayName=name;mover.validGhost=good;mover.invalidGhost=bad;
                var bounds=new Bounds();bool first=true;
                foreach(var mesh in machine.GetComponentsInChildren<MeshFilter>())foreach(var vertex in mesh.sharedMesh.vertices)
                {
                    var p=machine.transform.InverseTransformPoint(mesh.transform.TransformPoint(vertex));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);
                }
                mover.boundsCenter=bounds.center;mover.boundsSize=bounds.size;
                var knob=machine.transform.Find(knobPath);if(!knob)throw new Exception("Missing rotary handle.");
                var collider=knob.GetComponent<MeshCollider>();if(!collider)collider=knob.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=knob.GetComponent<MeshFilter>().sharedMesh;
                var handle=knob.GetComponent<MachineMoveHandle>();if(!handle)handle=knob.gameObject.AddComponent<MachineMoveHandle>();handle.machine=mover;
                PrefabUtility.SaveAsPrefabAsset(machine,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(machine);}
        }
        public static void VerifyMachinePlacement()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var placement=UnityEngine.Object.FindAnyObjectByType<MachinePlacement>();var machines=UnityEngine.Object.FindObjectsByType<MovableMachine>();
            if(!placement || machines.Length!=2)throw new Exception("Two movable consoles and player placement required.");
            foreach(var machine in machines)
            {
                var original=machine.transform.position;var orientation=machine.transform.rotation;
                var handle=machine.GetComponentInChildren<MachineMoveHandle>();var camera=placement.viewCamera;
                camera.transform.position=handle.transform.position-handle.transform.forward*.8f;camera.transform.LookAt(handle.transform.position);
                Physics.SyncTransforms();if(placement.FindHandle()!=handle)throw new Exception("Round handle ray inaccessible.");
                var renderer=machine.GetComponentInChildren<MeshRenderer>();var originalMaterial=renderer.sharedMaterial;
                if(!placement.Begin(machine))throw new Exception("Move did not begin.");
                foreach(var c in machine.GetComponentsInChildren<Collider>())if(c.enabled)throw new Exception("Ghost has active collision.");
                if(placement.pickup.FindAmmoButton() || placement.pickup.FindRateButton())throw new Exception("Button interaction active during placement.");
                camera.transform.position=new Vector3(3.3f,1.6f,-1);camera.transform.LookAt(new Vector3(3.3f,.5f,2));
                if(!placement.PreviewAt(new Vector3(3.3f,0,2),true))throw new Exception("Clear floor rejected.");
                placement.Rotate(1);if(Mathf.Abs(Mathf.DeltaAngle(machine.transform.eulerAngles.y,orientation.eulerAngles.y+15))>.01f)throw new Exception("Scroll rotation step incorrect.");
                if(!placement.PreviewAt(new Vector3(3.3f,0,2),true))throw new Exception("Rotated valid placement rejected.");
                if(placement.PreviewAt(new Vector3(3.3f,.4f,2),true) || placement.Confirm())throw new Exception("Floating placement allowed.");
                var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.position=new Vector3(3.3f,.6f,2);block.transform.localScale=Vector3.one;
                Physics.SyncTransforms();if(placement.PreviewAt(new Vector3(3.3f,0,2),true) || placement.Confirm())throw new Exception("Overlapping placement allowed.");UnityEngine.Object.DestroyImmediate(block);
                placement.Cancel();if(Vector3.Distance(machine.transform.position,original)>.001f || Quaternion.Angle(machine.transform.rotation,orientation)>.01f || renderer.sharedMaterial!=originalMaterial)throw new Exception("Cancel did not restore original.");
                placement.Begin(machine);placement.PreviewAt(new Vector3(3.3f,0,2),true);
                if(!placement.Confirm() || machine.IsPreview || renderer.sharedMaterial!=originalMaterial)throw new Exception("Valid confirm failed.");
                foreach(var c in machine.GetComponentsInChildren<Collider>())if(!c.enabled)throw new Exception("Placed machine collider not restored.");
                machine.transform.SetPositionAndRotation(original,orientation);Physics.SyncTransforms();
            }
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var ammo=UnityEngine.Object.FindAnyObjectByType<AmmoSelectionConsole>();var moving=ammo.GetComponent<MovableMachine>();
            moving.BeginPreview();moving.ShowValidity(true);RenderPreview(moving.gameObject,"QA/MachinePlacement_Ghost.png");moving.EndPreview(false);
            Debug.Log("MACHINE_PLACEMENT_VERIFIED: both round handles, transparent preview, collider/material restoration, 15-degree rotation, floor support, blocked/airborne rejection, confirm/cancel, interaction lockout.");
        }
    }
}
