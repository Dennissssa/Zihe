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
        const string TruckFolder="Assets/Mach/UtilityTruck";
        const string TruckPrefab=TruckFolder+"/UtilityTruck.prefab";
        static Material truckCream,truckDark,truckRubber,truckMetal,truckGlass,truckAmber,truckRed,truckLight;
        [MenuItem("Mach/Author Utility Truck")]
        public static void AuthorTruck()
        {
            Directory.CreateDirectory(TruckFolder+"/Meshes");Directory.CreateDirectory(TruckFolder+"/Materials");AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(TruckPrefab);
            if(!prefab)
            {
                truckCream=TruckMaterial("Ivory enamel",new Color(.65f,.64f,.54f),.2f);
                truckDark=TruckMaterial("Graphite armor",new Color(.065f,.075f,.077f),.35f);
                truckRubber=TruckMaterial("Tire rubber",new Color(.024f,.029f,.03f),0);
                truckMetal=TruckMaterial("Brushed fittings",new Color(.34f,.37f,.37f),.7f);
                truckAmber=TruckMaterial("Amber marker",new Color(.85f,.32f,.055f),.2f);
                truckRed=TruckMaterial("Tail lamp",new Color(.62f,.025f,.012f),.2f);
                truckLight=TruckMaterial("Headlamp lens",new Color(.82f,.88f,.77f),.2f);
                truckGlass=TruckMaterial("Transparent glazing",new Color(.2f,.28f,.29f,.17f),.1f);
                truckGlass.SetFloat("_Surface",1);truckGlass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);truckGlass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);truckGlass.SetFloat("_ZWrite",0);truckGlass.SetFloat("_Cull",0);truckGlass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");truckGlass.renderQueue=3000;truckGlass.SetOverrideTag("RenderType","Transparent");truckGlass.SetShaderPassEnabled("ShadowCaster",false);
                root=new GameObject("UtilityTruck").transform;meshFolder=TruckFolder+"/Meshes";meshPrefix="Truck_";meshIndex=0;
                try
                {
                    var truck=root.gameObject.AddComponent<UtilityTruck>();var body=root.gameObject.GetComponent<Rigidbody>();body.mass=2100;body.linearDamping=.04f;body.angularDamping=.8f;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                    TB("Chassis ladder",root,new Vector3(0,.69f,-.12f),new Vector3(1.66f,.22f,4.7f),truckDark,true,.035f);
                    foreach(float x in new[]{-.72f,.72f})TB("Frame rail",root,new Vector3(x,.49f,-.2f),new Vector3(.13f,.17f,4.6f),truckMetal,false,.016f);
                    TB("Cabin floor",root,new Vector3(0,1.05f,.04f),new Vector3(1.94f,.17f,1.78f),truckCream,true,.03f);
                    TB("Engine bonnet",root,new Vector3(0,1.47f,1.63f),new Vector3(1.79f,.53f,1.35f),truckCream,true,.08f);
                    TB("Bonnet center crease",root,new Vector3(0,1.746f,1.68f),new Vector3(.07f,.025f,1.22f),truckCream,false,.01f);
                    TB("Firewall / dashboard",root,new Vector3(0,1.55f,.77f),new Vector3(1.82f,.35f,.2f),truckDark,true,.035f);
                    TB("Roof",root,new Vector3(0,2.61f,.02f),new Vector3(2.12f,.16f,1.99f),truckDark,true,.06f);
                    TB("Roof inset",root,new Vector3(0,2.7f,.05f),new Vector3(1.75f,.04f,1.58f),truckDark,false,.02f);
                    foreach(float x in new[]{-.95f,.95f})
                    {
                        TB("Rear cab pillar",root,new Vector3(x,1.9f,-.83f),new Vector3(.13f,1.31f,.14f),truckCream,true,.025f);
                        var pillar=TB("Windshield pillar",root,new Vector3(x,2.13f,.83f),new Vector3(.11f,.84f,.12f),truckCream,true,.02f);pillar.transform.localRotation=Quaternion.Euler(-9,0,0);
                        TB("Roof side rail",root,new Vector3(x,2.52f,.03f),new Vector3(.13f,.12f,1.72f),truckCream,true,.025f);
                    }
                    TB("Rear cab lower wall",root,new Vector3(0,1.48f,-.83f),new Vector3(1.9f,.68f,.12f),truckCream,true,.025f);
                    TB("Rear window",root,new Vector3(0,2.13f,-.84f),new Vector3(1.74f,.57f,.021f),truckGlass,false,.008f);
                    TB("Rear window header",root,new Vector3(0,2.47f,-.84f),new Vector3(1.82f,.13f,.1f),truckCream,true,.015f);
                    var windshield=TB("Clear split windshield",root,new Vector3(0,2.14f,.828f),new Vector3(1.78f,.73f,.015f),truckGlass,false,.005f);windshield.transform.localRotation=Quaternion.Euler(-9,0,0);
                    TB("Windshield center post",root,new Vector3(0,2.14f,.844f),new Vector3(.028f,.73f,.035f),truckDark,false,.005f);
                    TB("Windshield lower seal",root,new Vector3(0,1.775f,.9f),new Vector3(1.84f,.05f,.06f),truckDark,false,.009f);
                    for(int i=0;i<2;i++)
                    {
                        float x=i==0?-.47f:.47f;
                        var seat=TB(i==0?"Driver seat":"Passenger seat",root,new Vector3(x,1.28f,-.18f),new Vector3(.66f,.18f,.63f),truckRubber,true,.08f);
                        TB("Seat back",root,new Vector3(x,1.71f,-.48f),new Vector3(.65f,.75f,.17f),truckRubber,true,.075f);
                        TB("Headrest",root,new Vector3(x,2.14f,-.49f),new Vector3(.37f,.25f,.15f),truckRubber,false,.045f);
                        if(i==0){var entry=seat.AddComponent<TruckInteractable>();entry.truck=truck;entry.driverEntry=true;entry.panelName="Driver seat";}
                    }
                    var wheel=new GameObject("Steering wheel").transform;wheel.SetParent(root,false);wheel.localPosition=new Vector3(-.47f,1.73f,.43f);wheel.localRotation=Quaternion.Euler(62,0,0);truck.steeringWheel=wheel;
                    for(int i=0;i<12;i++){float a=i*30*Mathf.Deg2Rad;var rim=TB("Steering rim",wheel,new Vector3(Mathf.Sin(a)*.185f,0,Mathf.Cos(a)*.185f),new Vector3(.11f,.027f,.035f),truckRubber,false,.012f);rim.transform.localRotation=Quaternion.Euler(0,i*30,0);}
                    TB("Steering spoke",wheel,Vector3.zero,new Vector3(.34f,.026f,.036f),truckMetal,false,.007f);TB("Steering hub",wheel,Vector3.zero,new Vector3(.09f,.055f,.09f),truckDark,false,.016f);
                    foreach(float x in new[]{-.48f,-.2f,.1f})TB("Instrument",root,new Vector3(x,1.69f,.655f),new Vector3(.16f,.1f,.014f),truckLight,false,.008f);
                    truck.driverDoor=TruckDoor(truck,-1);truck.passengerDoor=TruckDoor(truck,1);root.Find("Driver seat").GetComponent<TruckInteractable>().panel=truck.driverDoor;
                    foreach(float side in new[]{-1f,1f})
                    {
                        TB("Running board",root,new Vector3(side*1.08f,.78f,-.04f),new Vector3(.34f,.1f,1.52f),truckDark,true,.025f);
                        TB("Under-bed utility locker",root,new Vector3(side*.93f,.78f,-1.14f),new Vector3(.28f,.28f,.64f),truckDark,true,.03f);
                        foreach(float z in new[]{1.63f,-1.69f})Fender(root,side,z);
                    }
                    TB("Cargo bed floor",root,new Vector3(0,1.03f,-1.78f),new Vector3(1.96f,.14f,1.86f),truckCream,true,.025f);
                    for(int i=0;i<8;i++)TB("Bed floor rib",root,new Vector3((i-3.5f)*.22f,1.111f,-1.77f),new Vector3(.035f,.025f,1.69f),truckDark,false,.006f);
                    foreach(float x in new[]{-.98f,.98f})
                    {
                        TB("Cargo side wall",root,new Vector3(x,1.4f,-1.77f),new Vector3(.13f,.68f,1.9f),truckCream,true,.028f);
                        TB("Bed rail cap",root,new Vector3(x,1.756f,-1.78f),new Vector3(.16f,.055f,1.92f),truckDark,false,.015f);
                        TB("Rear lamp surround",root,new Vector3(x,1.45f,-2.748f),new Vector3(.16f,.42f,.1f),truckDark,false,.02f);
                        TB("Rear red lamp",root,new Vector3(x,1.52f,-2.805f),new Vector3(.095f,.19f,.02f),truckRed,false,.008f);
                        TB("Reverse lens",root,new Vector3(x,1.366f,-2.805f),new Vector3(.095f,.073f,.02f),truckLight,false,.008f);
                    }
                    var gate=new GameObject("Animated tailgate").transform;gate.SetParent(root,false);gate.localPosition=new Vector3(0,1.09f,-2.705f);
                    truck.tailgate=gate.gameObject.AddComponent<TruckHingedPanel>();truck.tailgate.axis=Vector3.right;truck.tailgate.openAngle=-90;
                    var gateInteraction=gate.gameObject.AddComponent<TruckInteractable>();gateInteraction.truck=truck;gateInteraction.panel=truck.tailgate;gateInteraction.panelName="Tailgate";
                    TB("Tailgate panel",gate,new Vector3(0,.3f,0),new Vector3(1.81f,.6f,.11f),truckCream,true,.03f);
                    TB("Tailgate pull handle",gate,new Vector3(0,.4f,-.075f),new Vector3(.27f,.065f,.04f),truckDark,true,.012f);
                    TB("Tailgate rail",gate,new Vector3(0,.62f,0),new Vector3(1.84f,.055f,.14f),truckDark,false,.013f);
                    TB("Rear bumper",root,new Vector3(0,.77f,-2.81f),new Vector3(2.02f,.2f,.22f),truckDark,true,.035f);
                    TB("Front grille surround",root,new Vector3(0,1.3f,2.342f),new Vector3(1.15f,.76f,.14f),truckCream,true,.045f);
                    TB("Radiator shadow",root,new Vector3(0,1.31f,2.425f),new Vector3(.98f,.65f,.025f),truckRubber,false,.015f);
                    for(int i=0;i<11;i++)TB("Vertical grille fin",root,new Vector3((i-5)*.082f,1.31f,2.45f),new Vector3(.017f,.62f,.03f),truckMetal,false,.004f);
                    foreach(float x in new[]{-.8f,.8f})
                    {
                        var lamp=new GameObject("Round headlight").transform;lamp.SetParent(root,false);lamp.localPosition=new Vector3(x,1.32f,2.38f);lamp.localRotation=Quaternion.Euler(0,180,0);
                        Disc("Headlight housing",lamp,Vector3.zero,.2f,.18f,12,truckDark);Disc("Headlight glass",lamp,new Vector3(0,0,-.105f),.158f,.025f,12,truckLight);
                        TB("Lamp protection bar",root,new Vector3(x,1.3f,2.58f),new Vector3(.38f,.024f,.035f),truckMetal,false,.007f);
                        TB("Amber side marker",root,new Vector3(x,1.68f,2.17f),new Vector3(.16f,.055f,.042f),truckAmber,false,.012f);
                    }
                    TB("Front steel bumper",root,new Vector3(0,.83f,2.6f),new Vector3(2.19f,.31f,.28f),truckDark,true,.035f);
                    TB("Winch housing",root,new Vector3(0,.99f,2.69f),new Vector3(.54f,.25f,.22f),truckMetal,false,.035f);
                    TB("Winch cable drum",root,new Vector3(0,1.0f,2.83f),new Vector3(.31f,.15f,.12f),truckDark,false,.028f);
                    foreach(float x in new[]{-.8f,.8f})TB("Tow shackle",root,new Vector3(x,.81f,2.77f),new Vector3(.075f,.18f,.055f),truckMetal,false,.02f);
                    truck.wheels=new WheelCollider[4];truck.wheelVisuals=new Transform[4];
                    for(int i=0;i<4;i++)
                    {
                        float x=i%2==0?-1.04f:1.04f,z=i<2?1.63f:-1.69f;
                        var axle=new GameObject("Suspension "+i).transform;axle.SetParent(root,false);axle.localPosition=new Vector3(x,.76f,z);
                        var wc=axle.gameObject.AddComponent<WheelCollider>();wc.radius=.55f;wc.suspensionDistance=.32f;wc.mass=35;wc.wheelDampingRate=1.3f;
                        wc.suspensionSpring=new JointSpring{spring=44000,damper=6500,targetPosition=.5f};wc.forceAppPointDistance=.2f;
                        var forward=wc.forwardFriction;forward.stiffness=1.5f;wc.forwardFriction=forward;var sideways=wc.sidewaysFriction;sideways.stiffness=1.7f;wc.sidewaysFriction=sideways;
                        truck.wheels[i]=wc;truck.wheelVisuals[i]=TruckWheel(root,new Vector3(x,.6f,z));
                    }
                    foreach(float z in new[]{1.63f,-1.69f})TB("Axle housing",root,new Vector3(0,.52f,z),new Vector3(1.95f,.12f,.12f),truckDark,false,.025f);
                    truck.driverAnchor=Anchor(root,"Driver camera anchor",new Vector3(-.47f,.49f,.06f));truck.leftExit=Anchor(root,"Left safe exit",new Vector3(-1.88f,.05f,.05f));truck.rightExit=Anchor(root,"Right safe exit",new Vector3(1.88f,.05f,.05f));
                    prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,TruckPrefab);UnityEngine.Object.DestroyImmediate(root.gameObject);
                }
                finally{meshFolder=Folder+"/Meshes";meshPrefix="Part_";}
            }
            var player=PrefabUtility.LoadPrefabContents("Assets/Mach/Prefabs/FirstPersonPlayer.prefab");
            try
            {
                var driver=player.GetComponent<TruckDriver>();if(!driver)driver=player.AddComponent<TruckDriver>();driver.pickup=player.GetComponent<PlayerPickup>();driver.walker=player.GetComponent<FirstPersonWalker>();driver.tablet=player.GetComponent<HeldTablet>();driver.placement=player.GetComponent<MachinePlacement>();driver.controls=player.transform.Find("HUD/Controls").GetComponent<UnityEngine.UI.Text>();driver.pickup.truckDriver=driver;
                PrefabUtility.SaveAsPrefabAsset(player,"Assets/Mach/Prefabs/FirstPersonPlayer.prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(player);}
            if(!UnityEngine.Object.FindAnyObjectByType<UtilityTruck>())
            {
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);instance.transform.position=new Vector3(5,.08f,3);instance.transform.rotation=Quaternion.Euler(0,160,0);
                GameObject.Find("Floor").transform.localScale=new Vector3(40,.3f,40);
                foreach(string name in new[]{"Back Wall","Front Wall","Left Wall","Right Wall"})
                {
                    var wall=GameObject.Find(name);if(!wall)continue;
                    bool side=name=="Left Wall" || name=="Right Wall";float sign=name=="Left Wall" || name=="Front Wall"?-1:1;
                    wall.transform.position=side?new Vector3(sign*20,1.5f,0):new Vector3(0,1.5f,sign*20);wall.transform.localScale=side?new Vector3(.3f,3,40):new Vector3(40,3,.3f);
                }
                var bumpMat=AssetDatabase.LoadAssetAtPath<Material>(TruckFolder+"/Materials/Graphite armor.mat");
                for(int i=0;i<3;i++){var bump=GameObject.CreatePrimitive(PrimitiveType.Cube);bump.name="Suspension test bump "+(i+1);bump.transform.position=new Vector3(7,.07f,8+i*2);bump.transform.localScale=new Vector3(5,.14f,.32f);bump.GetComponent<Renderer>().sharedMaterial=bumpMat;}
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();VerifyTruck();TabletAuthoring.VerifyAndBuild();
        }
        static Material TruckMaterial(string name,Color color,float metallic)
        {
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=color;material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",.24f);AssetDatabase.CreateAsset(material,TruckFolder+"/Materials/"+name+".mat");return material;
        }
        static Transform Anchor(Transform parent,string name,Vector3 p){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=p;return go.transform;}
        static GameObject TB(string name,Transform parent,Vector3 position,Vector3 size,Material mat,bool collider,float bevel)
        {
            bevel=Mathf.Min(bevel,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.45f);Vector3 half=size*.5f,core=half-Vector3.one*bevel;
            var mesh=new Builder();
            for(int axis=0;axis<3;axis++)foreach(float sign in new[]{-1f,1f})
            {
                int u=(axis+1)%3,v=(axis+2)%3;float[] us={-half[u],-core[u],core[u],half[u]},vs={-half[v],-core[v],core[v],half[v]};
                var grid=new Vector3[4,4];
                for(int a=0;a<4;a++)for(int b=0;b<4;b++){Vector3 p=Vector3.zero;p[axis]=sign*half[axis];p[u]=us[a];p[v]=vs[b];Vector3 q=new Vector3(Mathf.Clamp(p.x,-core.x,core.x),Mathf.Clamp(p.y,-core.y,core.y),Mathf.Clamp(p.z,-core.z,core.z));grid[a,b]=q+(p-q).normalized*bevel;}
                for(int a=0;a<3;a++)for(int b=0;b<3;b++)if(sign>0)mesh.Quad(grid[a,b],grid[a+1,b],grid[a+1,b+1],grid[a,b+1]);else mesh.Quad(grid[a,b+1],grid[a+1,b+1],grid[a+1,b],grid[a,b]);
            }
            Save(name,parent,position,mesh,mat);var go=parent.GetChild(parent.childCount-1).gameObject;if(collider){var box=go.AddComponent<BoxCollider>();box.size=size;}return go;
        }
        static TruckHingedPanel TruckDoor(UtilityTruck truck,float side)
        {
            var pivot=Anchor(root,side<0?"Driver door hinge":"Passenger door hinge",new Vector3(side*1.0f,1.08f,.77f));var panel=pivot.gameObject.AddComponent<TruckHingedPanel>();panel.openAngle=side<0?78:-78;
            var interact=pivot.gameObject.AddComponent<TruckInteractable>();interact.truck=truck;interact.panel=panel;interact.driverEntry=false;interact.panelName=side<0?"Driver door":"Passenger door";
            TB("Plain ivory door • no insignia",pivot,new Vector3(0,.37f,-.73f),new Vector3(.095f,.73f,1.39f),truckCream,true,.035f);
            TB("Door window glass",pivot,new Vector3(0,1.065f,-.73f),new Vector3(.02f,.57f,1.21f),truckGlass,false,.006f);
            foreach(float z in new[]{-.075f,-1.385f})TB("Door window upright",pivot,new Vector3(0,1.065f,z),new Vector3(.1f,.7f,.075f),truckCream,true,.018f);
            TB("Door window top",pivot,new Vector3(0,1.415f,-.73f),new Vector3(.1f,.1f,1.4f),truckCream,true,.019f);
            TB("Door pull handle",pivot,new Vector3(side*.063f,.52f,-1.16f),new Vector3(.047f,.064f,.18f),truckMetal,true,.016f);
            TB("Inside armrest",pivot,new Vector3(-side*.08f,.6f,-.8f),new Vector3(.13f,.09f,.56f),truckDark,false,.025f);
            TB("Mirror stalk",pivot,new Vector3(side*.22f,.93f,-.08f),new Vector3(.42f,.044f,.04f),truckDark,false,.012f);
            TB("Wing mirror",pivot,new Vector3(side*.43f,1.025f,-.08f),new Vector3(.045f,.23f,.17f),truckDark,false,.04f);
            return panel;
        }
        static Transform TruckWheel(Transform parent,Vector3 center)
        {
            var wheel=Anchor(parent,"Wheel visual",center);var mesh=new Builder();
            Vector2[] profile={new Vector2(-.2f,.23f),new Vector2(-.2f,.45f),new Vector2(-.145f,.55f),new Vector2(.145f,.55f),new Vector2(.2f,.45f),new Vector2(.2f,.23f)};
            for(int i=0;i<16;i++)for(int j=0;j<profile.Length;j++){int k=(j+1)%profile.Length;float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;Vector3 A=new Vector3(profile[j].x,Mathf.Cos(a)*profile[j].y,Mathf.Sin(a)*profile[j].y),B=new Vector3(profile[j].x,Mathf.Cos(b)*profile[j].y,Mathf.Sin(b)*profile[j].y),C=new Vector3(profile[k].x,Mathf.Cos(b)*profile[k].y,Mathf.Sin(b)*profile[k].y),D=new Vector3(profile[k].x,Mathf.Cos(a)*profile[k].y,Mathf.Sin(a)*profile[k].y);mesh.Quad(A,B,C,D);}
            Save("Faceted tire",wheel,Vector3.zero,mesh,truckRubber);
            for(int i=0;i<24;i++)foreach(float lane in new[]{-1f,1f}){float degrees=i*15f+(lane>0?5:0),a=degrees*Mathf.Deg2Rad;var tread=TB("Staggered tread block",wheel,new Vector3(lane*.092f,Mathf.Cos(a)*.553f,Mathf.Sin(a)*.553f),new Vector3(.17f,.05f,.09f),truckRubber,false,.007f);tread.transform.localRotation=Quaternion.Euler(degrees,0,0)*Quaternion.Euler(0,lane*13,0);}
            foreach(float side in new[]{-1f,1f})
            {
                var hub=Anchor(wheel,"Wheel hub",new Vector3(side*.205f,0,0));hub.localRotation=Quaternion.Euler(0,side<0?90:-90,0);
                Disc("Steel wheel rim",hub,Vector3.zero,.278f,.045f,12,truckDark);Disc("Axle cap",hub,new Vector3(0,0,-.035f),.12f,.04f,10,truckMetal);
                for(int bolt=0;bolt<6;bolt++){float a=bolt*Mathf.PI/3;Disc("Wheel nut",hub,new Vector3(Mathf.Cos(a)*.19f,Mathf.Sin(a)*.19f,-.032f),.017f,.012f,6,truckMetal);}
            }
            return wheel;
        }
        static void Fender(Transform parent,float side,float z)
        {
            var mesh=new Builder();
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;
                var part=TB("Angular wheel arch",parent,new Vector3(side*1.055f,.6f+Mathf.Sin((a+b)*.5f)*.715f,z+Mathf.Cos((a+b)*.5f)*.715f),new Vector3(.39f,.11f,.39f),truckCream,false,.022f);
                part.transform.localRotation=Quaternion.Euler((a+b)*.5f*Mathf.Rad2Deg-90,0,0);
            }
        }
        public static void VerifyTruck()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var truck=UnityEngine.Object.FindAnyObjectByType<UtilityTruck>();if(!truck || truck.wheels.Length!=4)throw new Exception("Authored truck missing.");
            foreach(var wheel in truck.wheels)if(wheel.suspensionDistance<.2f || wheel.suspensionSpring.spring<=0)throw new Exception("Suspension missing.");
            foreach(var panel in new[]{truck.driverDoor,truck.passengerDoor,truck.tailgate}){panel.SetOpen(true);for(int i=0;i<90;i++)panel.Animate(1f/60);if(Mathf.Abs(panel.Angle-panel.openAngle)>.1f)throw new Exception("Panel animation failed.");panel.SetOpen(false);for(int i=0;i<90;i++)panel.Animate(1f/60);}
            var pickup=UnityEngine.Object.FindAnyObjectByType<PlayerPickup>();var door=truck.driverDoor.GetComponent<TruckInteractable>();pickup.viewCamera.transform.position=truck.transform.TransformPoint(new Vector3(-2,1.5f,0));pickup.viewCamera.transform.LookAt(truck.driverDoor.transform.TransformPoint(new Vector3(0,.4f,-.7f)));Physics.SyncTransforms();
            if(pickup.FindTruckTarget()!=door)throw new Exception("Driver door cannot be targeted.");
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);truck=UnityEngine.Object.FindAnyObjectByType<UtilityTruck>();TruckPreview(truck,"QA/UtilityTruck.png");
            truck.driverDoor.SetOpen(true);truck.tailgate.SetOpen(true);for(int i=0;i<90;i++){truck.driverDoor.Animate(1f/60);truck.tailgate.Animate(1f/60);}TruckPreview(truck,"QA/UtilityTruck_Open.png");
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Debug.Log("TRUCK_AUTHORING_VERIFIED: existing scene, saved prefab, 4 physical suspension wheels, door/tailgate animation, driver door targeting.");
        }
        static void TruckPreview(UtilityTruck truck,string path)
        {
            Vector3 p=truck.transform.position;Quaternion q=truck.transform.rotation;truck.transform.SetPositionAndRotation(new Vector3(0,30,0),Quaternion.identity);
            var go=new GameObject("Temporary truck preview",typeof(Camera));var camera=go.GetComponent<Camera>();camera.transform.position=new Vector3(-7,34.1f,8);camera.transform.LookAt(new Vector3(0,31.2f,0));camera.orthographic=true;camera.orthographicSize=3.6f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.75f,.75f,.7f);
            bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;var rt=new RenderTexture(1400,1000,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1400,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1400,1000),0,0);tex.Apply();Directory.CreateDirectory("QA");File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);ShaderUtil.allowAsyncCompilation=async;truck.transform.SetPositionAndRotation(p,q);
        }
    }
}
