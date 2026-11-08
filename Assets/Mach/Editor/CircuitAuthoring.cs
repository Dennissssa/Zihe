using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Mach.Editor
{
    public static partial class AmmoMachineAuthoring
    {
        const string CircuitFolder="Assets/Mach/Circuit";
        static Font circuitFont;
        [MenuItem("Mach/Author Robot Circuit System")]
        public static void AuthorCircuitSystem()
        {
            Directory.CreateDirectory(CircuitFolder+"/Meshes");Directory.CreateDirectory(CircuitFolder+"/Materials");AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerPickup>();var program=player.GetComponent<BehaviorProgram>();
            if(!program)
            {
                program=player.gameObject.AddComponent<BehaviorProgram>();program.pickup=player;program.walker=player.GetComponent<FirstPersonWalker>();program.tablet=player.GetComponent<HeldTablet>();program.tabletVisual=program.tablet.poseRoot.Find("RuggedTablet");
                if(!program.tabletVisual)throw new Exception("Tablet visual not found");
                program.nodes=new CircuitNode[32];for(int i=0;i<32;i++)program.nodes[i]=new CircuitNode();program.nodes[0]=new CircuitNode{active=true,kind=CircuitKind.Start,position=new Vector2(-460,205)};
                circuitFont=ProjectTypography.Font;
                AuthorCircuitCanvas(program);
                meshFolder=CircuitFolder+"/Meshes";meshPrefix="Circuit_";meshIndex=0;
                AuthorRemote(program);
                var station=new GameObject("Temporary behavior buttons").transform;
                TB("Control station table",station,new Vector3(0,.7f,3.5f),new Vector3(4.4f,1.4f,.6f),LoadTruckMaterial("Graphite armor"),true,.05f);
                CircuitKind[] kinds={CircuitKind.Facing,CircuitKind.Fire,CircuitKind.DriveTo,CircuitKind.Wait};
                for(int i=0;i<4;i++)AuthorCircuitButton(station,program,kinds[i],"",new Vector3(-1.65f+i*1.1f,1.5f,3.20f),null);
                for(int i=0;i<3;i++)
                {
                    var marker=new GameObject("Circuit target "+(char)('A'+i)).transform;marker.position=new Vector3(-9+i*6,.04f,11);
                    var practice=marker.gameObject.AddComponent<CircuitPracticeTarget>();practice.targetName="POINT "+(char)('A'+i);
                    TB("Target plinth",marker,new Vector3(0,.03f,0),new Vector3(.8f,.055f,.8f),LoadTruckMaterial("Graphite armor"),false,.03f);
                    var target=TB("Practice target",marker,new Vector3(0,.65f,0),new Vector3(.55f,1.1f,.15f),LoadTruckMaterial("Ivory enamel"),true,.04f);target.GetComponent<Collider>().isTrigger=true;
                    Text("Target label",marker,practice.targetName,new Vector3(0,1.35f,-.09f),.19f,LoadTruckMaterial("Graphite armor"));practice.label=marker.GetChild(marker.childCount-1).GetComponent<TextMesh>();
                    AuthorCircuitButton(station,program,CircuitKind.Point,((char)('A'+i)).ToString(),new Vector3(-1.2f+i*1.2f,1.0f,2.66f),marker);
                }
                foreach(var ammo in UnityEngine.Object.FindObjectsByType<AmmoSelectionConsole>()){ammo.program=program;PrefabUtility.RecordPrefabInstancePropertyModifications(ammo);}
                foreach(var rate in UnityEngine.Object.FindObjectsByType<FireRateConsole>()){rate.program=program;PrefabUtility.RecordPrefabInstancePropertyModifications(rate);}
                var rig=UnityEngine.Object.FindAnyObjectByType<RobotAssemblyRig>();var runner=rig.gameObject.AddComponent<RobotCircuitRunner>();runner.rig=rig;runner.muzzle=rig.gunElevation.Find("Muzzle visual");
                var dock=rig.tabletSocket.parent.gameObject.AddComponent<RobotTabletDock>();dock.robot=runner;
                var dockCollider=dock.gameObject.AddComponent<BoxCollider>();dockCollider.center=new Vector3(0,.14f,-.08f);dockCollider.size=new Vector3(.64f,.79f,.19f);
                var body=rig.gameObject.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
                var trace=new GameObject("Authored reusable shot tracer",typeof(LineRenderer));trace.transform.SetParent(rig.transform,false);runner.tracer=trace.GetComponent<LineRenderer>();runner.tracer.positionCount=2;runner.tracer.startWidth=.014f;runner.tracer.endWidth=.004f;runner.tracer.enabled=false;runner.tracer.useWorldSpace=true;
                var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.color=new Color(1,.6f,.2f);AssetDatabase.CreateAsset(material,CircuitFolder+"/Materials/Shot trace.mat");runner.tracer.sharedMaterial=material;
                var flash=new GameObject("Authored muzzle flash",typeof(Light));flash.transform.SetParent(runner.muzzle,false);flash.transform.localPosition=new Vector3(0,0,.14f);runner.muzzleFlash=flash.GetComponent<Light>();runner.muzzleFlash.range=2;runner.muzzleFlash.intensity=3;runner.muzzleFlash.enabled=false;
                Text("Robot execution status",rig.transform,"READY / INSERT TABLET",new Vector3(0,1.1f,-1.08f),.12f,LoadTruckMaterial("Amber marker"));runner.display=rig.transform.GetChild(rig.transform.childCount-1).GetComponent<TextMesh>();
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();meshFolder=Folder+"/Meshes";meshPrefix="Part_";
            }
            foreach(var graphic in program.board.GetComponentsInChildren<Graphic>(true))
                if(!graphic.GetComponent<CanvasRenderer>())graphic.gameObject.AddComponent<CanvasRenderer>();
            if(!program.tablet.tabletCamera.GetComponent<TabletCanvasRendering>())program.tablet.tabletCamera.gameObject.AddComponent<TabletCanvasRendering>();
            var controls=player.transform.Find("HUD/Controls").GetComponent<UnityEngine.UI.Text>();controls.text="WASD Move | Shift Sprint | Space Jump | Tab Circuit | R Remote | P Mark Point | E Interact";controls.fontSize=17;EditorSceneManager.SaveScene(scene);
            VerifyCircuitSystem();TabletAuthoring.VerifyAndBuild();
        }
        static RectTransform UIRect(Transform parent,string name,Vector2 position,Vector2 size)
        {var go=new GameObject(name,typeof(RectTransform));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;}
        static Image UIPanel(Transform parent,string name,Vector2 position,Vector2 size,Color color)
        {var rect=UIRect(parent,name,position,size);var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
        static UnityEngine.UI.Text UILabel(Transform parent,string name,string text,Vector2 position,Vector2 size,int fontSize,Color color)
        {var rect=UIRect(parent,name,position,size);var label=rect.gameObject.AddComponent<UnityEngine.UI.Text>();label.font=circuitFont;label.text=text;label.fontSize=fontSize;label.color=color;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;return label;}
        static RectTransform UIButton(Transform parent,string name,string text,Vector2 position,Vector2 size,Color color,int font=20)
        {var image=UIPanel(parent,name,position,size,color);UILabel(image.transform,"Label",text,Vector2.zero,size,font,Color.white);return image.rectTransform;}
        static void AuthorCircuitCanvas(BehaviorProgram program)
        {
            var root=UIRect(program.tabletVisual,"Circuit board screen",Vector2.zero,new Vector2(1200,800));root.localPosition=new Vector3(0,0,-.040f);root.localScale=Vector3.one*.00037f;
            var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=program.tablet.tabletCamera;canvas.sortingOrder=5;
            var view=root.gameObject.AddComponent<CircuitBoardView>();view.program=program;view.canvas=canvas;program.board=view;
            UIPanel(root,"Screen background",Vector2.zero,new Vector2(1200,800),new Color(.025f,.045f,.06f));
            UILabel(root,"Heading","ROBOT / CIRCUIT",new Vector2(-365,348),new Vector2(440,48),32,new Color(.6f,.88f,.98f));
            view.checkButton=UIButton(root,"Check program","CHECK",new Vector2(145,350),new Vector2(150,48),new Color(.11f,.4f,.37f));
            view.unlinkButton=UIButton(root,"Clear selected wires","UNLINK",new Vector2(315,350),new Vector2(150,48),new Color(.25f,.3f,.36f));
            view.deleteButton=UIButton(root,"Delete selected node","DELETE",new Vector2(485,350),new Vector2(150,48),new Color(.48f,.17f,.13f));
            var surface=UIPanel(root,"Graph viewport",new Vector2(0,5),new Vector2(1155,580),new Color(.055f,.082f,.10f));view.surface=surface.rectTransform;surface.gameObject.AddComponent<RectMask2D>();
            view.workspace=UIRect(surface.transform,"Editable graph",Vector2.zero,new Vector2(1155,580));
            var wire=UIRect(view.workspace,"Execution and data wires",Vector2.zero,new Vector2(1155,580));wire.gameObject.AddComponent<CanvasRenderer>();view.wires=wire.gameObject.AddComponent<CircuitWireGraphic>();view.wires.board=view;view.wires.raycastTarget=false;
            view.slots=new CircuitSlot[32];
            for(int i=0;i<32;i++)
            {
                var background=UIPanel(view.workspace,"Block slot "+i,Vector2.zero,new Vector2(160,68),new Color(.14f,.28f,.36f));
                var slot=new CircuitSlot{rect=background.rectTransform,background=background};view.slots[i]=slot;
                slot.title=UILabel(background.transform,"Block name","EMPTY",Vector2.zero,new Vector2(135,54),22,Color.white);
                slot.input=UIButton(background.transform,"EXEC input","IN",new Vector2(-80,0),new Vector2(32,28),new Color(.15f,.5f,.72f),14);
                slot.output=UIButton(background.transform,"EXEC output",">",new Vector2(80,0),new Vector2(32,28),new Color(.15f,.5f,.72f),20);
                slot.data=UIButton(background.transform,"DATA output","OUT",new Vector2(0,-38),new Vector2(48,22),new Color(.67f,.49f,.12f),13);
                slot.parameter=UIButton(background.transform,"PARAM input","PARAM",new Vector2(0,38),new Vector2(60,22),new Color(.67f,.49f,.12f),13);
                AuthorPort(slot.input,true,1,new Vector2(32,68));
                AuthorPort(slot.output,false,0,new Vector2(32,68));
                AuthorPort(slot.data,true,3,new Vector2(68,22));
                AuthorPort(slot.parameter,false,2,new Vector2(68,22));
            }
            view.status=UILabel(root,"Validation and status","Collect blocks at the machines. Tab to wire.",new Vector2(0,-320),new Vector2(1140,52),23,new Color(1,.8f,.4f));
            view.help=UILabel(root,"Help","BLUE: EXEC > IN   |   GOLD: DATA > PARAM\nDrag blocks / empty space   -   Wheel zoom   -   Tab close   -   R remote",new Vector2(0,-374),new Vector2(1140,50),18,new Color(.6f,.72f,.8f));
            foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
            view.Refresh();
        }
        static void AuthorPort(RectTransform rect,bool socket,int direction,Vector2 size)
        {
            var old=rect.GetComponent<Image>();var tint=old.color;
            UnityEngine.Object.DestroyImmediate(old);
            var graphic=rect.gameObject.AddComponent<CircuitPortGraphic>();graphic.color=direction<2?CircuitBoardView.ExecutionPortColor:CircuitBoardView.ParameterPortColor;graphic.socket=socket;graphic.direction=direction;
            rect.sizeDelta=size;
            foreach(var label in rect.GetComponentsInChildren<Text>(true))label.text="";
            var button=rect.GetComponent<Button>();if(button)button.targetGraphic=graphic;
        }
        static void AuthorRemote(BehaviorProgram program)
        {
            var remote=program.gameObject.AddComponent<CeaseFireRemote>();remote.program=program;remote.uiCamera=program.tablet.tabletCamera;program.remote=remote;
            var housing=UIRect(program.pickup.viewCamera.transform,"Handheld cease-fire remote",Vector2.zero,new Vector2(320,450));housing.localPosition=new Vector3(.31f,-.08f,.66f);housing.localRotation=Quaternion.Euler(0,-12,0);remote.panel=housing;
            TB("Remote ivory shell",housing,Vector3.zero,new Vector3(.23f,.30f,.042f),LoadTruckMaterial("Ivory enamel"),false,.025f);
            TB("Remote top antenna",housing,new Vector3(.07f,.20f,.005f),new Vector3(.023f,.14f,.025f),LoadTruckMaterial("Graphite armor"),false,.007f);
            var screen=UIRect(housing,"Remote face",Vector2.zero,new Vector2(320,420));screen.localPosition=new Vector3(0,0,-.026f);screen.localScale=Vector3.one*.00061f;var canvas=screen.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=remote.uiCamera;canvas.sortingOrder=6;
            UIPanel(screen,"Faceplate",Vector2.zero,new Vector2(320,420),new Color(.055f,.068f,.076f));UILabel(screen,"Title","FIELD REMOTE",new Vector2(0,163),new Vector2(300,50),26,Color.white);
            remote.state=UILabel(screen,"State","ROBOT CONTROL\nR: LOWER REMOTE",new Vector2(0,85),new Vector2(290,90),20,new Color(.5f,.8f,.9f));
            remote.stopButton=UIButton(screen,"Physical red stop button","CEASE\nFIRE",new Vector2(0,-50),new Vector2(245,140),new Color(.76f,.09f,.025f),35);
            UILabel(screen,"Label","FIRE STOP ONLY",new Vector2(0,-162),new Vector2(280,40),19,new Color(.8f,.8f,.7f));foreach(var t in housing.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;housing.gameObject.SetActive(false);
        }
        static void AuthorCircuitButton(Transform parent,BehaviorProgram program,CircuitKind kind,string value,Vector3 position,Transform point)
        {
            var body=TB(kind+" "+value+" temporary button",parent,position,new Vector3(.66f,.30f,.22f),LoadTruckMaterial("Ivory enamel"),true,.03f);
            var provider=body.AddComponent<CircuitWorldButton>();provider.program=program;provider.kind=kind;provider.value=value;provider.point=point;
            provider.cap=TB("Press cap",body.transform,new Vector3(0,0,-.135f),new Vector3(.55f,.22f,.065f),LoadTruckMaterial(kind==CircuitKind.Fire?"Tail lamp":"Graphite armor"),false,.025f).transform;
            Text("Button label",provider.cap,kind.ToString().ToUpper()+ (value==""?"":" "+value),new Vector3(0,0,-.04f),.095f,LoadTruckMaterial("Headlamp lens"));
        }
        public static void VerifyCircuitSystem()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var program=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();if(!program||!program.remote||program.board.slots.Length!=32||!UnityEngine.Object.FindAnyObjectByType<RobotCircuitRunner>())throw new Exception("Circuit setup missing");
            foreach(var graphic in program.GetComponentsInChildren<Graphic>(true))
                if(!graphic.GetComponent<CanvasRenderer>())throw new Exception("Saved UI missing CanvasRenderer: "+graphic.name);
            Canvas.ForceUpdateCanvases();
            if(program.Validate(out _))throw new Exception("Empty circuit should fail");
            foreach(var ammo in UnityEngine.Object.FindObjectsByType<AmmoSelectionConsole>())if(ammo.program!=program)throw new Exception("Ammo machine not wired");
            foreach(var rate in UnityEngine.Object.FindObjectsByType<FireRateConsole>())if(rate.program!=program)throw new Exception("Rate machine not wired");
            Debug.Log("CIRCUIT_AUTHORING_VERIFIED: 32 saved UI slots, world function buttons, machine confirmation links, robot dock, handheld stop remote.");
        }
    }
}

