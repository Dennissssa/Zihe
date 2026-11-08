using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
    public static partial class AmmoMachineAuthoring
    {
        [MenuItem("Mach/Author Movable Laser Points")]
        public static void AuthorPointMarkers()
        {
            var scene=EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var program=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();
            var machine=UnityEngine.Object.FindAnyObjectByType<MovableMachine>();
            var good=machine.validGhost;var bad=machine.invalidGhost;
            meshFolder=CircuitFolder+"/Meshes";meshPrefix="Point_";meshIndex=0;
            var markers=new CircuitPointMarker[3];
            for(int i=0;i<3;i++)
            {
                string id=((char)('A'+i)).ToString();var root=GameObject.Find("Circuit target "+id);
                if(!root)throw new Exception("Missing existing Point "+id);
                var marker=root.GetComponent<CircuitPointMarker>();
                if(!marker)
                {
                    foreach(var child in root.GetComponentsInChildren<Transform>())if(child!=root.transform&&child.parent==root.transform)UnityEngine.Object.DestroyImmediate(child.gameObject);
                    var old=root.GetComponent<CircuitPracticeTarget>();if(old)UnityEngine.Object.DestroyImmediate(old);
                    root.transform.position=new Vector3(root.transform.position.x,0,root.transform.position.z);
                    marker=root.AddComponent<CircuitPointMarker>();marker.pointId=id;
                    var mover=root.AddComponent<MovableMachine>();mover.displayName="POINT "+id;mover.boundsCenter=new Vector3(0,.06f,0);mover.boundsSize=new Vector3(.32f,.12f,.32f);mover.validGhost=good;mover.invalidGhost=bad;
                    var body=TB("Point "+id+" emitter",root.transform,new Vector3(0,.045f,0),new Vector3(.32f,.09f,.32f),LoadTruckMaterial("Ivory enamel"),false,.025f);
                    var hit=body.AddComponent<BoxCollider>();hit.size=new Vector3(.36f,.16f,.36f);hit.center=new Vector3(0,.025f,0);hit.isTrigger=true;
                    body.AddComponent<MachineMoveHandle>().machine=mover;
                    Color color=i==0?new Color(.05f,.65f,1):i==1?new Color(1,.55f,.035f):new Color(.3f,1,.16f);
                    var beamMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));beamMaterial.color=color;AssetDatabase.CreateAsset(beamMaterial,CircuitFolder+"/Materials/Point "+id+" laser.mat");
                    TB("Emitter lens",root.transform,new Vector3(0,.105f,0),new Vector3(.15f,.03f,.15f),beamMaterial,false,.02f);
                    var beam=GameObject.CreatePrimitive(PrimitiveType.Cylinder);beam.name="Point "+id+" laser beam - no collider";beam.transform.SetParent(root.transform,false);beam.transform.localPosition=new Vector3(0,1.02f,0);beam.transform.localScale=new Vector3(.045f,.9f,.045f);UnityEngine.Object.DestroyImmediate(beam.GetComponent<Collider>());beam.GetComponent<Renderer>().sharedMaterial=beamMaterial;beam.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                    Text("Point ID",root.transform,"POINT "+id,new Vector3(0,.055f,-.167f),.065f,LoadTruckMaterial("Graphite armor"));
                }
                if(!root.transform.Find("Bright laser core"))
                {
                    var beam=root.transform.Find("Point "+id+" laser beam - no collider");
                    var material=beam.GetComponent<Renderer>().sharedMaterial;var tint=material.color;tint.a=.3f;material.color=tint;
                    material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.One);material.SetFloat("_ZWrite",0);material.SetOverrideTag("RenderType","Transparent");material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;EditorUtility.SetDirty(material);
                    var coreMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));coreMaterial.color=Color.Lerp(new Color(tint.r,tint.g,tint.b),Color.white,.8f);AssetDatabase.CreateAsset(coreMaterial,CircuitFolder+"/Materials/Point "+id+" core.mat");
                    var core=GameObject.CreatePrimitive(PrimitiveType.Cylinder);core.name="Bright laser core";core.transform.SetParent(root.transform,false);core.transform.localPosition=beam.localPosition;core.transform.localScale=new Vector3(.014f,.9f,.014f);UnityEngine.Object.DestroyImmediate(core.GetComponent<Collider>());core.GetComponent<Renderer>().sharedMaterial=coreMaterial;core.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                markers[i]=marker;
            }
            program.pointMarkers=markers;EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            VerifyPointMarkers();TabletAuthoring.VerifyAndBuild();
        }
        public static void VerifyPointMarkers()
        {
            var program=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();
            if(program.pointMarkers==null||program.pointMarkers.Length!=3)throw new Exception("Expected three laser points");
            foreach(var marker in program.pointMarkers)
            {
                if(!marker.GetComponent<MovableMachine>()||!marker.GetComponentInChildren<MachineMoveHandle>())throw new Exception("Point move interaction missing");
                foreach(var collider in marker.GetComponentsInChildren<Collider>())if(!collider.isTrigger)throw new Exception("Point must not block movement");
            }
            Debug.Log("POINT_MARKERS_VERIFIED: Three saved movable emitters; beams have no colliders; bases only have interaction triggers.");
        }
    }
}
