using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mach.Editor
{
    public static partial class AmmoMachineAuthoring
    {
        [MenuItem("Mach/Refine Truck Proportions")]
        public static void RefineTruck()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var contents=PrefabUtility.LoadPrefabContents(TruckPrefab);
            root=contents.transform;meshFolder=TruckFolder+"/Meshes";meshPrefix="Refined_";meshIndex=0;
            try
            {
                while(root.childCount>0)UnityEngine.Object.DestroyImmediate(root.GetChild(0).gameObject);
                truckCream=LoadTruckMaterial("Ivory enamel");truckDark=LoadTruckMaterial("Graphite armor");truckRubber=LoadTruckMaterial("Tire rubber");truckMetal=LoadTruckMaterial("Brushed fittings");truckGlass=LoadTruckMaterial("Transparent glazing");truckAmber=LoadTruckMaterial("Amber marker");truckRed=LoadTruckMaterial("Tail lamp");truckLight=LoadTruckMaterial("Headlamp lens");
                truckCream.color=new Color(.73f,.70f,.61f);truckGlass.color=new Color(.13f,.18f,.19f,.30f);EditorUtility.SetDirty(truckCream);EditorUtility.SetDirty(truckGlass);
                var truck=contents.GetComponent<UtilityTruck>();truck.steeringLockSeconds=.5f;truck.steeringReturnSeconds=.75f;truck.steeringWheelLockDegrees=270;
                // Side elevation: short cab, long cargo bed, broad nose and substantial tires.
                TB("Chassis ladder",root,new Vector3(0,.66f,-.45f),new Vector3(1.66f,.2f,5.45f),truckDark,true,.035f);
                foreach(float x in new[]{-.72f,.72f})TB("Frame rail",root,new Vector3(x,.52f,-.4f),new Vector3(.13f,.16f,5.3f),truckMetal,false,.018f);
                TB("Cabin floor",root,new Vector3(0,1.04f,-.12f),new Vector3(1.96f,.15f,1.6f),truckCream,true,.04f);
                // Chamfered bonnet narrows toward the grille instead of forming a square block.
                TruckLoft("Tapered engine bonnet",root,new[]{new Vector3(.85f,1.15f,.70f),new Vector3(.85f,1.83f,.70f),new Vector3(.65f,1.72f,2.43f),new Vector3(.65f,1.10f,2.43f)},truckCream);
                var engineCollider=root.Find("Tapered engine bonnet").gameObject.AddComponent<BoxCollider>();engineCollider.center=new Vector3(0,1.43f,1.54f);engineCollider.size=new Vector3(1.29f,.54f,1.7f);
                TB("Bonnet spine",root,new Vector3(0,1.785f,1.56f),new Vector3(.035f,.024f,1.56f),truckCream,false,.008f).transform.localRotation=Quaternion.Euler(3.6f,0,0);
                TB("Firewall",root,new Vector3(0,1.56f,.61f),new Vector3(1.88f,.41f,.17f),truckCream,true,.035f);
                TB("Dashboard padded top",root,new Vector3(0,1.73f,.49f),new Vector3(1.8f,.14f,.24f),truckDark,false,.045f);
                TB("Rear cab lower wall",root,new Vector3(0,1.48f,-.91f),new Vector3(1.97f,.82f,.13f),truckCream,true,.035f);
                foreach(float x in new[]{-.86f,.86f})TB("Rear window side panel",root,new Vector3(x,2.22f,-.91f),new Vector3(.25f,.67f,.12f),truckCream,true,.025f);
                foreach(float y in new[]{1.99f,2.5f})TB("Rear window cross panel",root,new Vector3(0,y,-.91f),new Vector3(1.72f,.15f,.12f),truckCream,true,.025f);
                foreach(float x in new[]{-.745f,.745f})TB("Rear window vertical seal",root,new Vector3(x,2.245f,-.922f),new Vector3(.033f,.4f,.13f),truckDark,false,.009f);
                foreach(float y in new[]{2.055f,2.435f})TB("Rear window horizontal seal",root,new Vector3(0,y,-.922f),new Vector3(1.51f,.033f,.13f),truckDark,false,.009f);
                TB("Rear window glass",root,new Vector3(0,2.245f,-.925f),new Vector3(1.46f,.38f,.013f),truckGlass,false,.01f);
                TB("Roof",root,new Vector3(0,2.63f,-.15f),new Vector3(2.09f,.16f,1.83f),truckDark,true,.08f);
                TB("Roof inset",root,new Vector3(0,2.717f,-.17f),new Vector3(1.67f,.035f,1.35f),truckDark,false,.035f);
                TB("Front visor",root,new Vector3(0,2.56f,.72f),new Vector3(2.09f,.095f,.18f),truckDark,false,.035f);
                foreach(float x in new[]{-.96f,.96f})
                {
                    TB("Rear cab pillar",root,new Vector3(x,2.16f,-.87f),new Vector3(.16f,.83f,.17f),truckCream,true,.025f);
                    TruckBar("Sloping windshield pillar",root,new Vector3(x,1.82f,.79f),new Vector3(x,2.56f,.61f),.10f,truckCream);
                    TB("Cab roof side rail",root,new Vector3(x,2.55f,-.12f),new Vector3(.12f,.11f,1.5f),truckCream,true,.024f);
                }
                var glass=TB("Raked split windshield",root,new Vector3(0,2.18f,.704f),new Vector3(1.81f,.735f,.017f),truckGlass,false,.008f);glass.transform.localRotation=Quaternion.Euler(-13.7f,0,0);
                TruckBar("Windshield center seal",root,new Vector3(0,1.823f,.803f),new Vector3(0,2.546f,.624f),.025f,truckDark);
                TB("Windshield lower seal",root,new Vector3(0,1.816f,.797f),new Vector3(1.9f,.056f,.048f),truckDark,false,.012f);
                foreach(float x in new[]{-.48f,.48f})TruckBar("Windshield wiper",root,new Vector3(x-.2f,1.851f,.817f),new Vector3(x+.15f,1.88f,.81f),.016f,truckDark);
                for(int i=0;i<2;i++)
                {
                    float x=i==0?-.48f:.48f;
                    var seat=TB(i==0?"Driver seat":"Passenger seat",root,new Vector3(x,1.27f,-.27f),new Vector3(.66f,.19f,.59f),truckRubber,true,.065f);
                    TB("Seat back",root,new Vector3(x,1.72f,-.55f),new Vector3(.64f,.74f,.15f),truckRubber,true,.07f);
                    TB("Headrest",root,new Vector3(x,2.15f,-.57f),new Vector3(.36f,.23f,.14f),truckRubber,false,.04f);
                    if(i==0){var entry=seat.AddComponent<TruckInteractable>();entry.truck=truck;entry.driverEntry=true;entry.panelName="Driver seat";}
                }
                var steering=Anchor(root,"Steering wheel",new Vector3(-.48f,1.76f,.26f));steering.localRotation=Quaternion.Euler(-62,0,0);truck.steeringWheel=steering;
                BuildRoundSteeringRim(steering);
                for(int i=0;i<3;i++){float a=i*120*Mathf.Deg2Rad;TruckBar("Steering spoke",steering,Vector3.zero,new Vector3(Mathf.Sin(a)*.19f,0,Mathf.Cos(a)*.19f),.034f,truckMetal);}
                TB("Steering hub",steering,Vector3.zero,new Vector3(.09f,.055f,.09f),truckDark,false,.02f);
                TruckBar("Steering column",root,new Vector3(-.48f,1.74f,.29f),new Vector3(-.48f,1.51f,.65f),.055f,truckDark);
                foreach(float x in new[]{-.52f,-.29f,.03f}){var gauge=Anchor(root,"Dashboard gauge",new Vector3(x,1.72f,.358f));Disc("Gauge bezel",gauge,Vector3.zero,.065f,.02f,12,truckMetal);Disc("Gauge face",gauge,new Vector3(0,0,-.016f),.055f,.01f,12,truckRubber);TruckBar("Gauge needle",gauge,new Vector3(-.025f,-.025f,-.025f),new Vector3(.025f,.022f,-.025f),.006f,truckLight);}
                truck.driverDoor=RefinedDoor(truck,-1);truck.passengerDoor=RefinedDoor(truck,1);root.Find("Driver seat").GetComponent<TruckInteractable>().panel=truck.driverDoor;
                foreach(float side in new[]{-1f,1f})
                {
                    TB("Running board",root,new Vector3(side*1.07f,.78f,-.12f),new Vector3(.4f,.13f,1.47f),truckDark,true,.035f);
                    TB("Step ivory edge",root,new Vector3(side*1.245f,.735f,-.12f),new Vector3(.035f,.14f,1.4f),truckCream,false,.015f);
                    TB("Utility locker",root,new Vector3(side*.99f,.85f,-1.17f),new Vector3(.25f,.35f,.39f),truckDark,true,.025f);
                    RefinedFender(side,1.69f);RefinedFender(side,-2.14f);
                    TB("Bonnet side latch",root,new Vector3(side*.8f,1.67f,.89f),new Vector3(.06f,.23f,.07f),truckDark,false,.013f);
                    TB("Amber bonnet marker",root,new Vector3(side*.91f,1.73f,.70f),new Vector3(.03f,.22f,.036f),truckAmber,false,.01f);
                }
                // Long bed sides follow the rear wheel opening instead of covering the tires.
                TB("Cargo bed floor",root,new Vector3(0,1.24f,-2.22f),new Vector3(1.99f,.13f,2.50f),truckCream,true,.025f);
                for(int i=0;i<8;i++)TB("Cargo floor rib",root,new Vector3((i-3.5f)*.225f,1.319f,-2.22f),new Vector3(.025f,.018f,2.36f),truckMetal,false,.005f);
                foreach(float side in new[]{-1f,1f})
                {
                    var outline=new[]{new Vector2(-3.47f,.91f),new Vector2(-2.91f,.91f),new Vector2(-2.77f,1.34f),new Vector2(-2.54f,1.48f),new Vector2(-1.75f,1.48f),new Vector2(-1.51f,1.34f),new Vector2(-1.36f,.91f),new Vector2(-.98f,.91f),new Vector2(-.98f,1.88f),new Vector2(-3.47f,1.88f)};
                    SidePrism("Sculpted cargo side",root,side*.99f,.13f,outline,truckCream);
                    var cargoCollider=root.GetChild(root.childCount-1).gameObject.AddComponent<BoxCollider>();cargoCollider.center=new Vector3(side*.99f,1.72f,-2.22f);cargoCollider.size=new Vector3(.13f,.33f,2.49f);
                    TB("Bed rail cap",root,new Vector3(side*1.0f,1.91f,-2.23f),new Vector3(.16f,.055f,2.52f),truckCream,false,.013f);
                    TB("Front bed inspection hatch",root,new Vector3(side*1.063f,1.47f,-1.19f),new Vector3(.017f,.56f,.33f),truckCream,false,.024f);
                    foreach(float z in new[]{-1.3f,-1.07f})foreach(float y in new[]{1.24f,1.7f})TB("Hatch fastener",root,new Vector3(side*1.077f,y,z),new Vector3(.018f,.024f,.024f),truckMetal,false,.007f);
                    TB("Rear lamp housing",root,new Vector3(side*1.045f,1.44f,-3.48f),new Vector3(.15f,.5f,.105f),truckDark,false,.025f);
                    TB("Tail red lens",root,new Vector3(side*1.045f,1.56f,-3.54f),new Vector3(.091f,.17f,.021f),truckRed,false,.008f);
                    TB("Tail amber lens",root,new Vector3(side*1.045f,1.4f,-3.54f),new Vector3(.091f,.1f,.021f),truckAmber,false,.008f);
                    TB("Tail reverse lens",root,new Vector3(side*1.045f,1.28f,-3.54f),new Vector3(.091f,.07f,.021f),truckLight,false,.008f);
                    TB("Rear mud flap",root,new Vector3(side*1.04f,.66f,-2.91f),new Vector3(.38f,.57f,.032f),truckRubber,false,.009f);
                }
                var gate=Anchor(root,"Animated tailgate",new Vector3(0,1.29f,-3.46f));truck.tailgate=gate.gameObject.AddComponent<TruckHingedPanel>();truck.tailgate.axis=Vector3.right;truck.tailgate.openAngle=-90;
                var interaction=gate.gameObject.AddComponent<TruckInteractable>();interaction.truck=truck;interaction.panel=truck.tailgate;interaction.panelName="Tailgate";
                TB("Tailgate panel",gate,new Vector3(0,.29f,0),new Vector3(1.89f,.58f,.1f),truckCream,true,.035f);
                TB("Tailgate pressed inset",gate,new Vector3(0,.29f,-.055f),new Vector3(1.61f,.38f,.019f),truckCream,false,.035f);
                TB("Tailgate pull handle",gate,new Vector3(0,.30f,-.077f),new Vector3(.26f,.06f,.04f),truckDark,true,.015f);
                TB("Tailgate rail",gate,new Vector3(0,.61f,0),new Vector3(1.93f,.053f,.15f),truckCream,false,.014f);
                foreach(float x in new[]{-.63f,.63f})TB("Tailgate hinge",gate,new Vector3(x,0,-.045f),new Vector3(.18f,.062f,.08f),truckMetal,false,.016f);
                TB("Rear bumper",root,new Vector3(0,.78f,-3.53f),new Vector3(2.23f,.24f,.24f),truckDark,true,.045f);
                TB("Front grille surround",root,new Vector3(0,1.34f,2.43f),new Vector3(1.32f,.84f,.15f),truckCream,true,.09f);
                TB("Radiator recess",root,new Vector3(0,1.35f,2.514f),new Vector3(1.16f,.69f,.025f),truckRubber,false,.04f);
                for(int i=0;i<13;i++)TB("Grille vertical fin",root,new Vector3((i-6)*.084f,1.35f,2.537f),new Vector3(.018f,.66f,.025f),truckMetal,false,.006f);
                foreach(float side in new[]{-1f,1f})
                {
                    var lamp=Anchor(root,"Round headlight",new Vector3(side*.88f,1.36f,2.43f));lamp.localRotation=Quaternion.Euler(0,180,0);
                    Disc("Headlight housing",lamp,Vector3.zero,.215f,.22f,16,truckDark);Disc("Chrome lens ring",lamp,new Vector3(0,0,-.12f),.185f,.025f,16,truckMetal);Disc("Headlight lens",lamp,new Vector3(0,0,-.138f),.165f,.012f,16,truckLight);
                    TruckBar("Headlight guard upright",root,new Vector3(side*1.115f,1.03f,2.66f),new Vector3(side*1.115f,1.58f,2.66f),.035f,truckDark);
                    TruckBar("Headlight guard top",root,new Vector3(side*.68f,1.59f,2.66f),new Vector3(side*1.115f,1.59f,2.66f),.035f,truckDark);
                    TruckBar("Headlight guard lower",root,new Vector3(side*.68f,1.08f,2.66f),new Vector3(side*1.115f,1.08f,2.66f),.035f,truckDark);
                    TB("Bumper wing",root,new Vector3(side*.77f,.88f,2.63f),new Vector3(.82f,.33f,.29f),truckDark,true,.035f);
                }
                TB("Bumper crossmember",root,new Vector3(0,.80f,2.59f),new Vector3(2.32f,.16f,.24f),truckDark,true,.03f);
                var winch=Anchor(root,"Winch drum",new Vector3(0,1.01f,2.64f));winch.localRotation=Quaternion.Euler(0,90,0);Disc("Cable spool",winch,Vector3.zero,.115f,.4f,12,truckMetal);
                for(int i=0;i<9;i++) {var band=Anchor(root,"Winch cable winding",new Vector3((i-4)*.037f,1.01f,2.64f));band.localRotation=Quaternion.Euler(0,90,0);Disc("Cable turn",band,Vector3.zero,.121f,.012f,12,truckDark);}
                foreach(float x in new[]{-.32f,.32f})TB("Winch mounting",root,new Vector3(x,1.015f,2.64f),new Vector3(.12f,.22f,.24f),truckDark,false,.03f);
                foreach(float x in new[]{-.84f,.84f}){TruckBar("Tow loop left",root,new Vector3(x-.045f,.92f,2.81f),new Vector3(x-.045f,.77f,2.81f),.035f,truckMetal);TruckBar("Tow loop bottom",root,new Vector3(x-.045f,.77f,2.81f),new Vector3(x+.045f,.77f,2.81f),.035f,truckMetal);TruckBar("Tow loop right",root,new Vector3(x+.045f,.77f,2.81f),new Vector3(x+.045f,.92f,2.81f),.035f,truckMetal);}
                truck.wheels=new WheelCollider[4];truck.wheelVisuals=new Transform[4];
                for(int i=0;i<4;i++)
                {
                    float x=i%2==0?-1.14f:1.14f,z=i<2?1.69f:-2.14f;
                    var suspension=Anchor(root,"Suspension "+i,new Vector3(x,.82f,z));var wc=suspension.gameObject.AddComponent<WheelCollider>();wc.radius=.61f;wc.suspensionDistance=.32f;wc.mass=40;wc.wheelDampingRate=1.3f;wc.forceAppPointDistance=.2f;
                    wc.suspensionSpring=new JointSpring{spring=44000,damper=6500,targetPosition=.5f};var f=wc.forwardFriction;f.stiffness=1.5f;wc.forwardFriction=f;f=wc.sidewaysFriction;f.stiffness=1.7f;wc.sidewaysFriction=f;
                    truck.wheels[i]=wc;truck.wheelVisuals[i]=TruckWheel(root,new Vector3(x,.66f,z));truck.wheelVisuals[i].localScale=new Vector3(1.40f,1.11f,1.11f);
                }
                foreach(float z in new[]{1.69f,-2.14f})TB("Axle housing",root,new Vector3(0,.6f,z),new Vector3(2.19f,.13f,.13f),truckDark,false,.025f);
                truck.driverAnchor=Anchor(root,"Driver camera anchor",new Vector3(-.48f,.43f,-.37f));truck.leftExit=Anchor(root,"Left safe exit",new Vector3(-1.96f,.05f,-.12f));truck.rightExit=Anchor(root,"Right safe exit",new Vector3(1.96f,.05f,-.12f));
                BuildTruckInterior(contents);
                PrefabUtility.SaveAsPrefabAsset(contents,TruckPrefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);meshFolder=Folder+"/Meshes";meshPrefix="Part_";}
            AssetDatabase.SaveAssets();VerifyTruck();TruckOrthographics();TabletAuthoring.VerifyAndBuild();
        }
        static Material LoadTruckMaterial(string name)=>AssetDatabase.LoadAssetAtPath<Material>(TruckFolder+"/Materials/"+name+".mat");
        public static void FinishTruckDrivingView()
        {
            var contents=PrefabUtility.LoadPrefabContents(TruckPrefab);
            try
            {
                var truck=contents.GetComponent<UtilityTruck>();truck.driverAnchor.localPosition=new Vector3(-.48f,.43f,-.37f);truck.steeringWheel.localRotation=Quaternion.Euler(-62,0,0);
                foreach(var filter in contents.GetComponentsInChildren<MeshFilter>())if(filter.name=="Sculpted cargo side")
                {
                    var mesh=filter.sharedMesh;var vertices=mesh.vertices;var triangles=mesh.triangles;bool inverted=false;
                    for(int i=0;i<triangles.Length;i+=3){var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];if(Mathf.Abs(a.z+.98f)<.001f && Mathf.Abs(b.z+.98f)<.001f && Mathf.Abs(c.z+.98f)<.001f && Vector3.Cross(b-a,c-a).z<0)inverted=true;}
                    if(inverted){for(int i=0;i<triangles.Length;i+=3){var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];if(Mathf.Abs(a.x-b.x)+Mathf.Abs(a.x-c.x)<.001f)continue;int t=triangles[i];triangles[i]=triangles[i+2];triangles[i+2]=t;}mesh.triangles=triangles;mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);}
                }
                PrefabUtility.SaveAsPrefabAsset(contents,TruckPrefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            AssetDatabase.SaveAssets();VerifyTruck();TruckOrthographics();TabletAuthoring.VerifyAndBuild();
        }
        static void TruckBar(string name,Transform parent,Vector3 a,Vector3 b,float width,Material material)
        {var go=TB(name,parent,(a+b)*.5f,new Vector3(width,width,Vector3.Distance(a,b)),material,false,width*.23f);go.transform.localRotation=Quaternion.LookRotation(b-a,Mathf.Abs(Vector3.Dot((b-a).normalized,Vector3.up))>.99f?Vector3.forward:Vector3.up);}
        static void TruckLoft(string name,Transform parent,Vector3[] points,Material material)
        {
            var mesh=new Builder();for(int i=0;i<points.Length;i++){int j=(i+1)%points.Length;var a=points[i];var b=points[j];mesh.Quad(new Vector3(-a.x,a.y,a.z),a,b,new Vector3(-b.x,b.y,b.z));}
            mesh.Quad(points[3],points[2],points[1],points[0]);mesh.Quad(new Vector3(-points[0].x,points[0].y,points[0].z),new Vector3(-points[1].x,points[1].y,points[1].z),new Vector3(-points[2].x,points[2].y,points[2].z),new Vector3(-points[3].x,points[3].y,points[3].z));for(int i=0;i<mesh.triangles.Count;i+=3){int t=mesh.triangles[i];mesh.triangles[i]=mesh.triangles[i+2];mesh.triangles[i+2]=t;}Save(name,parent,Vector3.zero,mesh,material);
        }
        // Ear clipping keeps the concave wheel cut-out open.
        static void SidePrism(string name,Transform parent,float x,float depth,Vector2[] outline,Material material)
        {
            var mesh=new Builder();var ids=new System.Collections.Generic.List<int>();for(int i=0;i<outline.Length;i++)ids.Add(i);
            float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
            Vector3 P(int i,float s)=>new Vector3(x+s*depth*.5f,outline[i].y,outline[i].x);
            int guard=0;while(ids.Count>2 && guard++<100){bool cut=false;for(int n=0;n<ids.Count;n++){int a=ids[(n+ids.Count-1)%ids.Count],b=ids[n],c=ids[(n+1)%ids.Count];if(Cross(outline[b]-outline[a],outline[c]-outline[b])<=0)continue;bool inside=false;foreach(int k in ids){if(k==a||k==b||k==c)continue;var p=outline[k];if(Cross(outline[b]-outline[a],p-outline[a])>=0 && Cross(outline[c]-outline[b],p-outline[b])>=0 && Cross(outline[a]-outline[c],p-outline[c])>=0){inside=true;break;}}if(inside)continue;mesh.Tri(P(c,1),P(b,1),P(a,1));mesh.Tri(P(a,-1),P(b,-1),P(c,-1));ids.RemoveAt(n);cut=true;break;}if(!cut)throw new Exception("Invalid side outline "+name);}
            for(int i=0;i<outline.Length;i++){int j=(i+1)%outline.Length;mesh.Quad(P(i,1),P(j,1),P(j,-1),P(i,-1));}Save(name,parent,Vector3.zero,mesh,material);
        }
        static void RefinedFender(float side,float z)
        {
            // Continuous faceted arch with connected panels, ivory flare and dark liner.
            foreach(bool liner in new[]{false,true})
            {
                float inner=liner?.705f:.75f,outer=liner?.755f:.86f,x0=side*.82f,x1=side*(liner?1.442f:1.43f);var mesh=new Builder();
                for(int i=0;i<8;i++){float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;Vector3 V(float x,float r,float t)=>new Vector3(x,.66f+Mathf.Sin(t)*r,z+Mathf.Cos(t)*r);Vector3 A=V(x0,inner,a),B=V(x0,inner,b),C=V(x0,outer,b),D=V(x0,outer,a),E=V(x1,inner,a),F=V(x1,inner,b),G=V(x1,outer,b),H=V(x1,outer,a);mesh.Quad(E,F,G,H);mesh.Quad(D,C,B,A);mesh.Quad(H,G,C,D);mesh.Quad(A,B,F,E);if(i==0)mesh.Quad(A,E,H,D);if(i==7)mesh.Quad(F,B,C,G);}
                // Mirror flips winding on the left-hand arch.
                if(side<0)for(int i=0;i<mesh.triangles.Count;i+=3){int t=mesh.triangles[i];mesh.triangles[i]=mesh.triangles[i+2];mesh.triangles[i+2]=t;}
                Save(liner?"Continuous black arch liner":"Continuous ivory wheel flare",root,Vector3.zero,mesh,liner?truckDark:truckCream);
            }
        }
        static TruckHingedPanel RefinedDoor(UtilityTruck truck,float side)
        {
            var pivot=Anchor(root,side<0?"Driver door hinge":"Passenger door hinge",new Vector3(side*.99f,1.08f,.61f));var panel=pivot.gameObject.AddComponent<TruckHingedPanel>();panel.openAngle=side<0?78:-78;
            var interaction=pivot.gameObject.AddComponent<TruckInteractable>();interaction.truck=truck;interaction.panel=panel;interaction.driverEntry=false;interaction.panelName=side<0?"Driver door":"Passenger door";
            TB("Plain ivory door",pivot,new Vector3(0,.37f,-.71f),new Vector3(.10f,.75f,1.39f),truckCream,true,.055f);
            // Open window aperture made of four rails and glass.
            TB("Door window glass",pivot,new Vector3(0,1.06f,-.73f),new Vector3(.017f,.53f,1.14f),truckGlass,false,.025f);
            foreach(float z in new[]{-.105f,-1.355f})TB("Door window upright",pivot,new Vector3(0,1.07f,z),new Vector3(.1f,.65f,.105f),truckCream,true,.025f);
            foreach(float y in new[]{.752f,1.398f})TB("Door window horizontal rail",pivot,new Vector3(0,y,-.73f),new Vector3(.10f,.11f,1.35f),truckCream,true,.025f);
            TB("Door pull handle",pivot,new Vector3(side*.064f,.55f,-1.12f),new Vector3(.05f,.15f,.057f),truckMetal,true,.013f);
            TB("Inner armrest",pivot,new Vector3(-side*.08f,.58f,-.8f),new Vector3(.13f,.08f,.51f),truckDark,false,.025f);
            TruckBar("Diagonal mirror stalk",pivot,new Vector3(side*.04f,.6f,-.1f),new Vector3(side*.36f,1.02f,.20f),.034f,truckDark);
            var mirror=Anchor(pivot,"Round wing mirror",new Vector3(side*.36f,1.06f,.20f));mirror.localRotation=Quaternion.Euler(0,side*20,0);Disc("Mirror shell",mirror,Vector3.zero,.128f,.052f,12,truckDark);Disc("Mirror reflective face",mirror,new Vector3(0,0,-.032f),.104f,.013f,12,truckMetal);var backing=Anchor(mirror,"Closed mirror rear",new Vector3(0,0,.023f));backing.localRotation=Quaternion.Euler(0,180,0);Disc("Mirror rear cap",backing,Vector3.zero,.127f,.018f,12,truckDark);
            return panel;
        }
        static void TruckOrthographics()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var truck=UnityEngine.Object.FindAnyObjectByType<UtilityTruck>();truck.transform.SetPositionAndRotation(new Vector3(0,30,0),Quaternion.identity);
            string[] names={"Front","Side","Rear"};Vector3[] offsets={new Vector3(0,0,10),new Vector3(-10,0,0),new Vector3(0,0,-10)};
            for(int i=0;i<3;i++)
            {
                var go=new GameObject("Temporary orthographic",typeof(Camera));var camera=go.GetComponent<Camera>();camera.transform.position=new Vector3(0,31.4f,-.4f)+offsets[i];camera.transform.LookAt(new Vector3(0,31.4f,-.4f));camera.orthographic=true;camera.orthographicSize=i==1?2.4f:1.75f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.75f,.75f,.7f);
                var rt=new RenderTexture(1400,900,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1400,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1400,900),0,0);tex.Apply();File.WriteAllBytes("QA/Truck_"+names[i]+".png",tex.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
            }
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
        }
    }
}

