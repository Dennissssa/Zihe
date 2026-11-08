using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
    [InitializeOnLoad] public static class SplitConsoleLive
    {
        static SplitConsoleLive(){EditorApplication.update+=Poll;}
        static void Poll(){if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/SplitConsole.request"))return;File.Delete("Temp/SplitConsole.request");try{AmmoMachineAuthoring.SplitConsoles();File.WriteAllText("Logs/split-console-applied.txt","Saved Facing, Driving and Fire machines.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/split-console-error.txt",e.ToString());}}
    }
    public static partial class AmmoMachineAuthoring
    {
        const string SplitFolder="Assets/Mach/BehaviorConsoles";
        static void DedicatedKey(Transform face,NavigationConsole console,string label,Vector2 pos,Vector2 size,int action,Material color,Material print)
        {
            var key=new GameObject(label+" button").transform;key.SetParent(face,false);key.localPosition=new Vector3(pos.x,pos.y,-.035f);
            Panel("Gasket",key,Vector3.zero,size.x+.07f,size.y+.065f,.024f,.02f,rubber);
            Ring("Socket",key,new Vector3(0,0,-.018f),size.x+.055f,size.y+.05f,size.x+.01f,size.y+.01f,.018f,.035f,metal);
            var cap=new GameObject("Moving cap").transform;cap.SetParent(key,false);cap.localPosition=new Vector3(0,0,-.06f);
            Panel("Keycap",cap,Vector3.zero,size.x,size.y,.018f,.03f,color);
            if(action==0||action==-1)
            {
                float sign=action==0?1:-1;var icon=new Builder();
                var a=new Vector3(-.047f,-sign*.025f,0);var b=new Vector3(0,sign*.04f,0);var c=new Vector3(.047f,-sign*.025f,0);
                if(sign>0)icon.Tri(a,b,c);else icon.Tri(c,b,a);
                Save("Arrow",cap,new Vector3(0,0,-.019f),icon,print);
            }
            else Text("Legend",cap,label,new Vector3(0,0,-.019f),.055f,print);
            foreach(float x in new[]{-(size.x+.032f)/2,(size.x+.032f)/2})Bolt(key,new Vector3(x,0,-.04f),.009f);
            var hit=key.gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(0,0,-.052f);hit.size=new Vector3(size.x+.06f,size.y+.05f,.08f);
            var button=key.gameObject.AddComponent<CircuitWorldButton>();button.navigation=console;button.navigationAction=action;button.cap=cap;button.pressDirection=Vector3.forward;
        }
        public static void SplitBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);SplitConsoles();VerifySplit();}
        public static void SplitConsoles()
        {
            var program=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();if(!program||program.gameObject.scene.path!=WalkingSceneAuthoring.ScenePath)throw new Exception("Open existing WalkingPickup scene");
            foreach(var c in UnityEngine.Object.FindObjectsByType<NavigationConsole>())if(c.dedicated)return;
            var source=UnityEngine.Object.FindAnyObjectByType<NavigationConsole>();if(!source)throw new Exception("Original navigation console missing");
            Directory.CreateDirectory(SplitFolder+"/Meshes");AssetDatabase.Refresh();
            var saveRoot=root;var saveFolder=meshFolder;var savePrefix=meshPrefix;int saveIndex=meshIndex;
            meshFolder=SplitFolder+"/Meshes";meshPrefix="Control_";meshIndex=0;
            ivory=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Warm ivory enamel.mat");metal=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Graphite steel.mat");rubber=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Rubber seals.mat");
            var print=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Button ivory print.mat");var green=AssetDatabase.LoadAssetAtPath<Material>(RateFolder+"/Materials/Confirm sage.mat");
            try
            {
                for(int i=0;i<2;i++)
                {
                    var copy=UnityEngine.Object.Instantiate(source.gameObject);copy.name=i==0?"FacingConsole":"DrivingConsole";Undo.RegisterCreatedObjectUndo(copy,"Split navigation console");
                    var c=copy.GetComponent<NavigationConsole>();c.dedicated=true;c.mode=i==0?CircuitKind.Facing:CircuitKind.DriveTo;
                    foreach(var key in copy.GetComponentsInChildren<CircuitWorldButton>())UnityEngine.Object.DestroyImmediate(key.gameObject);
                    var face=copy.transform.Find("Inclined control panel");face.Find("Heading").GetComponent<TextMesh>().text=i==0?"FACING / CONTROL":"DRIVING / CONTROL";
                    DedicatedKey(face,c,"UP",new Vector2(-.255f,.59f),new Vector2(.33f,.188f),0,metal,print);
                    DedicatedKey(face,c,"DOWN",new Vector2(-.255f,.245f),new Vector2(.33f,.188f),-1,metal,print);
                    DedicatedKey(face,c,i==0?"FACING":"DRIVING",new Vector2(.255f,.418f),new Vector2(.33f,.50f),3,green,print);
                    c.Refresh();copy.GetComponent<MovableMachine>().displayName=i==0?"Facing console":"Driving console";
                    var position=source.transform.position+source.transform.right*(i*1.45f);var rotation=source.transform.rotation;var scale=source.transform.lossyScale;
                    c.program=null;copy.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);copy.transform.localScale=Vector3.one;
                    PrefabUtility.SaveAsPrefabAsset(copy,SplitFolder+"/"+copy.name+".prefab");c.program=program;copy.transform.SetPositionAndRotation(position,rotation);copy.transform.localScale=scale;
                }
                root=new GameObject("FireConsole").transform;Undo.RegisterCreatedObjectUndo(root.gameObject,"Create Fire console");
                Hull("Compact ivory cabinet",new[]{new Vector2(.10f,-.28f),new Vector2(.34f,-.28f),new Vector2(.92f,-.13f),new Vector2(.92f,.20f),new Vector2(.10f,.20f)},.32f,.024f,ivory);
                Hull("Graphite foot",new[]{new Vector2(.025f,-.32f),new Vector2(.11f,-.28f),new Vector2(.11f,.20f),new Vector2(.025f,.26f)},.36f,.02f,metal);
                Box("Top protective rail",root,new Vector3(0,.928f,.038f),new Vector3(.53f,.025f,.29f),metal);
                var panel=new GameObject("Sloped single control").transform;panel.SetParent(root,false);panel.localPosition=new Vector3(0,.34f,-.28f);panel.localRotation=Quaternion.Euler(14.5f,0,0);
                var fire=root.gameObject.AddComponent<NavigationConsole>();fire.dedicated=true;fire.mode=CircuitKind.Fire;
                DedicatedKey(panel,fire,"FIRE",new Vector2(0,.30f),new Vector2(.43f,.36f),3,green,print);
                var body=root.gameObject.AddComponent<MeshCollider>();body.sharedMesh=root.Find("Compact ivory cabinet").GetComponent<MeshFilter>().sharedMesh;body.convex=true;
                var mover=root.gameObject.AddComponent<MovableMachine>();var original=source.GetComponent<MovableMachine>();mover.displayName="Fire console";mover.validGhost=original.validGhost;mover.invalidGhost=original.invalidGhost;mover.boundsCenter=new Vector3(0,.48f,-.03f);mover.boundsSize=new Vector3(.72f,.94f,.65f);
                // Carry from the casing: the FIRE key remains the only physical button.
                root.gameObject.AddComponent<MachineMoveHandle>().machine=mover;
                PrefabUtility.SaveAsPrefabAsset(root.gameObject,SplitFolder+"/FireConsole.prefab");fire.program=program;
                root.SetPositionAndRotation(source.transform.position+source.transform.right*2.75f,source.transform.rotation);root.localScale=source.transform.lossyScale;
                Undo.RecordObject(source.gameObject,"Retire combined navigation console");source.gameObject.SetActive(false);
                foreach(var b in UnityEngine.Object.FindObjectsByType<CircuitWorldButton>())if(!b.navigation&&b.kind==CircuitKind.Fire){Undo.RecordObject(b.gameObject,"Replace temporary fire button");b.gameObject.SetActive(false);}
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(program.gameObject.scene);EditorSceneManager.SaveScene(program.gameObject.scene);
            }
            finally{root=saveRoot;meshFolder=saveFolder;meshPrefix=savePrefix;meshIndex=saveIndex;}
        }
        public static void VerifySplit()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);int count=0;Physics.SyncTransforms();
            foreach(var c in UnityEngine.Object.FindObjectsByType<NavigationConsole>())
            {
                if(!c.dedicated)throw new Exception("Combined machine still active");count++;
                var keys=c.GetComponentsInChildren<CircuitWorldButton>();if(keys.Length!=(c.mode==CircuitKind.Fire?1:3))throw new Exception("Wrong button count");
                foreach(var k in keys){var o=k.transform.TransformPoint(new Vector3(0,0,-.55f));if(!Physics.Raycast(o,k.transform.forward,out var hit,.8f)||hit.collider.GetComponent<CircuitWorldButton>()!=k)throw new Exception("Blocked key "+k.name);}
                c.program.ClearCircuit();
                if(c.mode!=CircuitKind.Fire){c.selectedPoint=0;c.Activate(-1);if(c.selectedPoint!=7)throw new Exception("Down wrap");c.Activate(0);if(c.selectedPoint!=0)throw new Exception("Up wrap");}
                c.Activate(3);int actions=0,points=0;foreach(var n in c.program.nodes)if(n.active){if(n.kind==c.mode)actions++;if(n.kind==CircuitKind.Point)points++;}
                if(actions!=1||points!=(c.mode==CircuitKind.Fire?0:1))throw new Exception("Confirm output");
                RenderPreview(c.gameObject,"Logs/"+c.name+"-preview.png");
            }
            if(count!=3)throw new Exception("Three machines required");EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Debug.Log("SPLIT_CONSOLES_VERIFIED");
        }
    }
}
