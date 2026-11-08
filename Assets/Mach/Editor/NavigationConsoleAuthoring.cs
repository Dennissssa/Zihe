using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
    [InitializeOnLoad] public static class NavigationConsoleLiveAuthoring
    {
        static NavigationConsoleLiveAuthoring(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/NavigationConsole.request"))return;
            File.Delete("Temp/NavigationConsole.request");
            try{AmmoMachineAuthoring.AuthorNavigationConsole();File.WriteAllText("Logs/navigation-applied.txt","Navigation console saved in existing scene.");}
            catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/navigation-error.txt",e.ToString());}
        }
    }
    public static partial class AmmoMachineAuthoring
    {
        const string NavFolder="Assets/Mach/NavigationConsole";
        public static void NavigationBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);AuthorNavigationConsole();VerifyNavigation();}
        [MenuItem("Mach/Author Navigation Console")]
        public static void AuthorNavigationConsole()
        {
            var program=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();
            if(!program||program.gameObject.scene.path!=WalkingSceneAuthoring.ScenePath)throw new Exception("Open WalkingPickup scene first.");
            if(UnityEngine.Object.FindAnyObjectByType<NavigationConsole>())return;
            var rate=UnityEngine.Object.FindAnyObjectByType<FireRateConsole>();if(!rate)throw new Exception("Reference console missing.");
            Directory.CreateDirectory(NavFolder+"/Meshes");AssetDatabase.Refresh();
            var oldRoot=root;var oldFolder=meshFolder;var oldPrefix=meshPrefix;int oldIndex=meshIndex;
            ivory=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Warm ivory enamel.mat");
            metal=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Graphite steel.mat");
            rubber=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Rubber seals.mat");
            glass=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Smoked screen.mat");
            var cyan=AssetDatabase.LoadAssetAtPath<Material>(RateFolder+"/Materials/Screen cyan.mat");
            var sage=AssetDatabase.LoadAssetAtPath<Material>(RateFolder+"/Materials/Confirm sage.mat");
            var print=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Button ivory print.mat");
            root=new GameObject("NavigationConsole").transform;Undo.RegisterCreatedObjectUndo(root.gameObject,"Create navigation console");
            meshFolder=NavFolder+"/Meshes";meshPrefix="Nav_";meshIndex=0;
            try
            {
                Hull("Ivory chamfered cabinet",new[]{new Vector2(.13f,-.36f),new Vector2(.4f,-.36f),new Vector2(1.8f,-.011f),new Vector2(1.8f,.30f),new Vector2(.13f,.30f)},.57f,.025f,ivory);
                Hull("Graphite plinth",new[]{new Vector2(.025f,-.40f),new Vector2(.14f,-.34f),new Vector2(.14f,.28f),new Vector2(.025f,.35f)},.61f,.025f,metal);
                Box("Top rail",root,new Vector3(0,1.808f,.14f),new Vector3(.99f,.027f,.27f),metal);
                var face=new GameObject("Inclined control panel").transform;face.SetParent(root,false);face.localPosition=new Vector3(0,.4f,-.36f);face.localRotation=Quaternion.Euler(14,0,0);
                Panel("Display gasket",face,new Vector3(0,1.09f,-.018f),.98f,.63f,.035f,.024f,rubber);
                Ring("Bolted display bezel",face,new Vector3(0,1.09f,-.035f),.96f,.61f,.82f,.47f,.025f,.038f,metal);
                Panel("Display glass",face,new Vector3(0,1.09f,-.042f),.825f,.478f,.018f,.008f,glass);
                var console=root.gameObject.AddComponent<NavigationConsole>();
                Text("Heading",face,"NAV / CONTROL",new Vector3(0,1.255f,-.052f),.042f,cyan);
                Text("Target",face,"POINT A",new Vector3(0,1.125f,-.052f),.11f,cyan);console.targetText=face.Find("Target").GetComponent<TextMesh>();
                Text("Mode",face,"FACING",new Vector3(0,.999f,-.052f),.052f,cyan);console.modeText=face.Find("Mode").GetComponent<TextMesh>();
                Text("Status",face,"SELECT / CONFIRM",new Vector3(0,.908f,-.052f),.024f,cyan);console.statusText=face.Find("Status").GetComponent<TextMesh>();
                foreach(float x in new[]{-.447f,.447f})foreach(float y in new[]{.821f,1.36f})Bolt(face,new Vector3(x,y,-.068f),.014f);
                string[] labels={"POINT >","FACING","DRIVING","CONFIRM"};
                Vector2[] positions={new Vector2(-.255f,.59f),new Vector2(.255f,.59f),new Vector2(-.255f,.245f),new Vector2(.255f,.245f)};
                for(int i=0;i<4;i++)
                {
                    var key=new GameObject(labels[i]+" button").transform;key.SetParent(face,false);key.localPosition=new Vector3(positions[i].x,positions[i].y,-.035f);
                    Panel("Seal",key,Vector3.zero,.425f,.282f,.026f,.018f,rubber);
                    Ring("Socket",key,new Vector3(0,0,-.018f),.412f,.27f,.345f,.20f,.02f,.038f,metal);
                    var cap=new GameObject("Moving cap").transform;cap.SetParent(key,false);cap.localPosition=new Vector3(0,0,-.055f);
                    Panel("Chamfered key",cap,Vector3.zero,.33f,.188f,.019f,.03f,i==3?sage:metal);
                    Text("Legend",cap,labels[i],new Vector3(0,0,-.018f),.045f,print);
                    foreach(float x in new[]{-.187f,.187f})Bolt(key,new Vector3(x,0,-.041f),.01f);
                    var box=key.gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,0,-.048f);box.size=new Vector3(.40f,.255f,.075f);
                    var button=key.gameObject.AddComponent<CircuitWorldButton>();button.navigation=console;button.navigationAction=i;button.cap=cap;button.pressDirection=Vector3.forward;
                }
                foreach(float side in new[]{-1f,1f})
                {
                    var plate=new GameObject("Service armor "+side).transform;plate.SetParent(root,false);plate.localPosition=new Vector3(side*.578f,.93f,.06f);plate.localRotation=Quaternion.Euler(0,side<0?90:-90,0);
                    Panel("Side armor",plate,Vector3.zero,.32f,1.15f,.045f,.02f,metal);
                    for(int i=0;i<6;i++)Box("Vent "+i,plate,new Vector3(0,(i-2.5f)*.072f,-.013f),new Vector3(.22f,.018f,.006f),rubber);
                    foreach(float y in new[]{-.49f,.49f})Bolt(plate,new Vector3(0,y,-.023f),.015f);
                }
                Panel("Lower fascia",root,new Vector3(0,.27f,-.382f),1.06f,.24f,.028f,.024f,ivory);
                Disc("Carry socket",root,new Vector3(0,.27f,-.408f),.105f,.026f,16,rubber);
                Disc("Carry button",root,new Vector3(0,.27f,-.439f),.086f,.033f,16,metal);
                Text("Carry label",root,"MOVE",new Vector3(0,.27f,-.459f),.033f,print);
                var body=root.gameObject.AddComponent<MeshCollider>();body.sharedMesh=root.Find("Ivory chamfered cabinet").GetComponent<MeshFilter>().sharedMesh;body.convex=true;
                var mover=root.gameObject.AddComponent<MovableMachine>();mover.displayName="Navigation console";mover.boundsCenter=new Vector3(0,.916f,-.025f);mover.boundsSize=new Vector3(1.22f,1.81f,.88f);
                mover.validGhost=AssetDatabase.LoadAssetAtPath<Material>(PlacementFolder+"/Valid cyan ghost.mat");mover.invalidGhost=AssetDatabase.LoadAssetAtPath<Material>(PlacementFolder+"/Blocked red ghost.mat");
                var knob=root.Find("Carry button");var kc=knob.gameObject.AddComponent<SphereCollider>();kc.radius=.093f;knob.gameObject.AddComponent<MachineMoveHandle>().machine=mover;
                PrefabUtility.SaveAsPrefabAsset(root.gameObject,NavFolder+"/NavigationConsole.prefab");
                console.program=program;
                root.position=rate.transform.position+rate.transform.right*1.55f;root.rotation=rate.transform.rotation;root.localScale=rate.transform.lossyScale;
                foreach(var old in UnityEngine.Object.FindObjectsByType<CircuitWorldButton>())if(!old.navigation&&(old.kind==CircuitKind.Facing||old.kind==CircuitKind.DriveTo)){Undo.RecordObject(old.gameObject,"Replace temporary navigation controls");old.gameObject.SetActive(false);}
                EditorSceneManager.MarkSceneDirty(program.gameObject.scene);EditorSceneManager.SaveScene(program.gameObject.scene);AssetDatabase.SaveAssets();
            }
            finally{root=oldRoot;meshFolder=oldFolder;meshPrefix=oldPrefix;meshIndex=oldIndex;}
        }
        public static void VerifyNavigation()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var c=UnityEngine.Object.FindAnyObjectByType<NavigationConsole>();if(!c)throw new Exception("Console missing");
            if(c.GetComponentsInChildren<CircuitWorldButton>().Length!=4)throw new Exception("Four buttons required");
            Physics.SyncTransforms();
            foreach(var b in c.GetComponentsInChildren<CircuitWorldButton>())
            {
                var origin=b.transform.TransformPoint(new Vector3(0,0,-.6f));
                if(!Physics.Raycast(origin,b.transform.forward,out var hit,1)||hit.collider.GetComponent<CircuitWorldButton>()!=b)throw new Exception("Button obstructed: "+b.name);
            }
            var p=c.program;p.ClearCircuit();c.selectedPoint=7;c.Activate(0);if(c.selectedPoint!=0)throw new Exception("Point wrap");
            c.Activate(1);if(!c.Confirm())throw new Exception("Facing confirm");c.Activate(2);if(!c.Confirm())throw new Exception("Driving confirm");
            int point=0,facing=0,drive=0;foreach(var n in p.nodes)if(n.active){if(n.kind==CircuitKind.Point&&n.pointId=="A")point++;if(n.kind==CircuitKind.Facing)facing++;if(n.kind==CircuitKind.DriveTo)drive++;}
            if(point!=1||facing!=1||drive!=1)throw new Exception("Node output mismatch");
            RenderPreview(c.gameObject,"Logs/navigation-preview.png");
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Debug.Log("NAVIGATION_CONSOLE_VERIFIED");
        }
    }
}
