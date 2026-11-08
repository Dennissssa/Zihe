using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
    public static partial class AmmoMachineAuthoring
    {
        const string RobotFolder="Assets/Mach/TrackedRobot";
        static Material robotIvory,robotDark,robotRubber,robotSteel,robotAmber,robotBlue;
        [MenuItem("Mach/Author Tracked Robot Parts")]
        public static void AuthorRobotParts()
        {
            Directory.CreateDirectory(RobotFolder+"/Meshes");Directory.CreateDirectory(RobotFolder+"/Parts");Directory.CreateDirectory(RobotFolder+"/Materials");AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            robotIvory=LoadTruckMaterial("Ivory enamel");robotDark=LoadTruckMaterial("Graphite armor");robotRubber=LoadTruckMaterial("Tire rubber");robotSteel=LoadTruckMaterial("Brushed fittings");robotAmber=LoadTruckMaterial("Amber marker");
            robotBlue=AssetDatabase.LoadAssetAtPath<Material>(RobotFolder+"/Materials/Blue lens.mat");if(!robotBlue){robotBlue=new Material(Shader.Find("Universal Render Pipeline/Lit"));robotBlue.color=new Color(.025f,.36f,.75f);robotBlue.SetColor("_EmissionColor",new Color(0,.22f,.7f));robotBlue.EnableKeyword("_EMISSION");robotBlue.SetFloat("_Smoothness",.7f);AssetDatabase.CreateAsset(robotBlue,RobotFolder+"/Materials/Blue lens.mat");}
            meshFolder=RobotFolder+"/Meshes";meshPrefix="Robot_";meshIndex=0;
            var model=new GameObject("TrackedRobot");root=model.transform;
            try
            {
                var rig=model.AddComponent<RobotAssemblyRig>();
                var chassis=Anchor(root,"Chassis",Vector3.zero);
                SidePrism("Sloped chassis armor",chassis,0,1.27f,new[]{new Vector2(-1.0f,.36f),new Vector2(1.04f,.36f),new Vector2(1.07f,.56f),new Vector2(.70f,.83f),new Vector2(-.8f,.83f),new Vector2(-1.02f,.66f)},robotIvory);
                var hullCollider=chassis.gameObject.AddComponent<BoxCollider>();hullCollider.center=new Vector3(0,.58f,0);hullCollider.size=new Vector3(1.25f,.46f,1.93f);
                TB("Front lower bumper",chassis,new Vector3(0,.35f,1.035f),new Vector3(.94f,.18f,.13f),robotDark,false,.035f);
                foreach(float x in new[]{-.55f,.55f}){TB("Front bumper cheek",chassis,new Vector3(x,.48f,1.02f),new Vector3(.15f,.3f,.12f),robotDark,false,.025f);TB("Front amber marker",chassis,new Vector3(x,.57f,1.089f),new Vector3(.105f,.037f,.015f),robotAmber,false,.006f);TB("Upper track shoulder",chassis,new Vector3(x,.84f,-.04f),new Vector3(.22f,.065f,1.74f),robotIvory,false,.025f);}
                var hatch=TB("Front inspection hatch",chassis,new Vector3(0,.746f,.83f),new Vector3(.40f,.022f,.23f),robotDark,false,.025f);hatch.transform.localRotation=Quaternion.Euler(36,0,0);
                TB("Rear service grille",chassis,new Vector3(0,.61f,-1.034f),new Vector3(.68f,.16f,.015f),robotDark,false,.013f);
                for(int i=0;i<7;i++)TB("Rear grille fin",chassis,new Vector3((i-3)*.082f,.61f,-1.045f),new Vector3(.025f,.12f,.016f),robotSteel,false,.004f);
                rig.leftTrack=BuildRobotTrack(root,-1);rig.rightTrack=BuildRobotTrack(root,1);
                var bearing=Anchor(chassis,"Turret bearing",new Vector3(0,.86f,-.04f));bearing.localRotation=Quaternion.Euler(90,0,0);RobotCylinder("Azimuth bearing",bearing,.56f,.13f,24,robotDark);
                var yaw=Anchor(root,"Turret azimuth pivot",new Vector3(0,.95f,-.04f));rig.turretYaw=yaw;
                TB("Turret lower deck",yaw,new Vector3(0,.10f,0),new Vector3(1.12f,.21f,1.11f),robotIvory,true,.1f);
                TB("Turret front center",yaw,new Vector3(0,.19f,.48f),new Vector3(.42f,.28f,.16f),robotDark,false,.04f);
                foreach(float side in new[]{-1f,1f})
                {
                    SidePrism("Faceted turret cheek",yaw,side*.43f,.28f,new[]{new Vector2(-.58f,.08f),new Vector2(.59f,.08f),new Vector2(.59f,.25f),new Vector2(.28f,.36f),new Vector2(.11f,.66f),new Vector2(-.41f,.70f),new Vector2(-.59f,.56f)},robotIvory);
                    var trunnion=Anchor(yaw,"Elevation bearing",new Vector3(side*.3f,.49f,.015f));trunnion.localRotation=Quaternion.Euler(0,90,0);RobotCylinder("Pitch bearing housing",trunnion,.17f,.1f,16,robotDark);
                    TB("Side cooling grille",yaw,new Vector3(side*.575f,.47f,-.28f),new Vector3(.016f,.22f,.28f),robotDark,false,.012f);
                    for(int i=0;i<3;i++)TB("Cooling fin",yaw,new Vector3(side*.587f,.40f+i*.069f,-.28f),new Vector3(.013f,.023f,.24f),robotRubber,false,.004f);
                }
                var pitch=Anchor(yaw,"Gun elevation pivot",new Vector3(0,.49f,.015f));rig.gunElevation=pitch;
                TB("Main upper housing",pitch,new Vector3(0,.12f,-.12f),new Vector3(.61f,.49f,.91f),robotIvory,true,.07f);
                TB("Top service lid",pitch,new Vector3(0,.38f,-.13f),new Vector3(.43f,.023f,.61f),robotDark,false,.032f);
                TB("Top latch",pitch,new Vector3(0,.398f,.075f),new Vector3(.16f,.015f,.035f),robotSteel,false,.006f);
                TB("Barrel root bezel",pitch,new Vector3(0,.03f,.38f),new Vector3(.39f,.35f,.11f),robotDark,false,.04f);
                TB("Mantlet ivory shroud",pitch,new Vector3(0,.025f,.57f),new Vector3(.30f,.29f,.40f),robotIvory,false,.045f);
                TB("Long faceted barrel jacket",pitch,new Vector3(0,.035f,1.02f),new Vector3(.205f,.20f,.98f),robotDark,false,.025f);
                TB("Barrel upper rail",pitch,new Vector3(0,.145f,1.01f),new Vector3(.15f,.025f,.87f),robotSteel,false,.006f);
                foreach(float side in new[]{-1f,1f})
                {
                    TB("Barrel side armor",pitch,new Vector3(side*.11f,.025f,1.13f),new Vector3(.025f,.17f,.62f),robotIvory,false,.012f);
                    TB("Barrel side recess",pitch,new Vector3(side*.126f,-.005f,.96f),new Vector3(.01f,.025f,.20f),robotRubber,false,.006f);
                    TB("Barrel amber inset",pitch,new Vector3(side*.117f,.075f,.72f),new Vector3(.012f,.026f,.11f),robotAmber,false,.004f);
                }
                var muzzle=Anchor(pitch,"Muzzle visual",new Vector3(0,.035f,1.57f));RobotTube(muzzle,.082f,.055f,.24f,16,robotDark);
                var sensors=Anchor(pitch,"Optical sensor pod",new Vector3(-.40f,.20f,.25f));
                TB("Ivory optical housing",sensors,new Vector3(0,.17f,-.09f),new Vector3(.29f,.28f,.43f),robotIvory,false,.04f);
                TB("Optic faceplate",sensors,new Vector3(0,.17f,.138f),new Vector3(.245f,.235f,.035f),robotDark,false,.025f);
                var lens=Anchor(sensors,"Main optic",new Vector3(0,.17f,.165f));RobotCylinder("Optic bezel",lens,.081f,.04f,16,robotRubber);RobotCylinder("Blue optic glass",Anchor(lens,"Glass mount",new Vector3(0,0,.025f)),.047f,.012f,16,robotBlue);
                var secondary=Anchor(sensors,"Secondary optic",new Vector3(0,-.065f,.13f));RobotCylinder("Secondary housing",secondary,.081f,.08f,12,robotDark);RobotCylinder("Secondary glass",Anchor(secondary,"Small lens mount",new Vector3(0,0,.047f)),.03f,.009f,12,robotBlue);
                foreach(float x in new[]{-.097f,.097f})foreach(float y in new[]{.075f,.265f})RobotCylinder("Optic screw",Anchor(sensors,"Fastener",new Vector3(x,y,.162f)),.01f,.014f,6,robotSteel);
                var dock=Anchor(yaw,"Rear tablet docking module",new Vector3(0,.58f,-.69f));
                TB("Dock back plate",dock,new Vector3(0,.13f,.03f),new Vector3(.51f,.74f,.065f),robotDark,false,.04f);
                TB("Empty tablet recess",dock,new Vector3(0,.14f,-.012f),new Vector3(.436f,.66f,.016f),robotRubber,false,.025f);
                foreach(float side in new[]{-1f,1f}){TB("Dock guide rail",dock,new Vector3(side*.247f,.14f,-.047f),new Vector3(.045f,.69f,.10f),robotIvory,false,.016f);TB("Dock clamp",dock,new Vector3(side*.27f,-.03f,-.06f),new Vector3(.11f,.31f,.15f),robotDark,false,.025f);TB("Dock clamp ivory cover",dock,new Vector3(side*.285f,-.03f,-.145f),new Vector3(.07f,.22f,.022f),robotIvory,false,.01f);TB("Dock amber status",dock,new Vector3(side*.275f,-.10f,-.159f),new Vector3(.019f,.075f,.01f),robotAmber,false,.004f);}
                foreach(float y in new[]{-.21f,.49f})TB("Dock upper lower lip",dock,new Vector3(0,y,-.035f),new Vector3(.49f,.038f,.09f),robotIvory,false,.011f);
                rig.tabletSocket=Anchor(dock,"TabletSocket",new Vector3(0,.14f,-.057f));rig.tabletSocket.localRotation=Quaternion.Euler(0,0,90);
                for(int i=0;i<5;i++)TB("Dock electrical contact",dock,new Vector3((i-2)*.023f,-.176f,-.028f),new Vector3(.012f,.018f,.01f),robotAmber,false,.002f);
                rig.leftTrack.Advance(0);rig.rightTrack.Advance(0);
                SaveRobotPart(chassis,"Chassis");SaveRobotPart(rig.leftTrack.transform,"LeftTrack");SaveRobotPart(rig.rightTrack.transform,"RightTrack");SaveRobotPart(pitch,"ElevationModule");SaveRobotPart(dock,"TabletDock");SaveRobotPart(yaw,"TurretAssembly");
                var prefab=PrefabUtility.SaveAsPrefabAsset(model,RobotFolder+"/TrackedRobot.prefab");
                var existing=UnityEngine.Object.FindAnyObjectByType<RobotAssemblyRig>();if(existing && existing.gameObject!=model)UnityEngine.Object.DestroyImmediate(existing.gameObject);
                UnityEngine.Object.DestroyImmediate(model);model=null;
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);instance.transform.position=new Vector3(-5,.015f,4);instance.transform.rotation=Quaternion.Euler(0,145,0);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                VerifyRobotParts();TabletAuthoring.VerifyAndBuild();
            }
            finally{if(model)UnityEngine.Object.DestroyImmediate(model);meshFolder=Folder+"/Meshes";meshPrefix="Part_";}
        }
        static void SaveRobotPart(Transform part,string name)=>PrefabUtility.SaveAsPrefabAssetAndConnect(part.gameObject,RobotFolder+"/Parts/"+name+".prefab",InteractionMode.AutomatedAction);
        static RobotTrackModule BuildRobotTrack(Transform parent,float side)
        {
            var module=Anchor(parent,side<0?"Left track module":"Right track module",new Vector3(side*.8f,0,0));var track=module.gameObject.AddComponent<RobotTrackModule>();
            TB("Track inner beam",module,new Vector3(0,.4f,0),new Vector3(.24f,.25f,1.45f),robotDark,true,.035f);
            var wheels=new List<Transform>();
            for(int i=0;i<5;i++)
            {
                float z=(i-2)*.36f,y=i==0||i==4?.41f:.27f,r=i==0||i==4?.30f:.19f;
                var wheel=Anchor(module,"Track wheel "+i,new Vector3(0,y,z));wheels.Add(wheel);
                var hub=Anchor(wheel,"Axle aligned geometry",Vector3.zero);hub.localRotation=Quaternion.Euler(0,90,0);
                RobotCylinder("Rubber roller",hub,r,.28f,16,robotRubber);RobotCylinder("Outer steel wheel",Anchor(hub,"Outside hub",new Vector3(0,0,side*.153f)),r*.86f,.027f,12,robotSteel);RobotCylinder("Wheel center",Anchor(hub,"Axle cap",new Vector3(0,0,side*.174f)),r*.26f,.026f,10,robotDark);
            }
            track.wheels=wheels.ToArray();track.links=new Transform[40];
            for(int i=0;i<track.links.Length;i++)
            {
                var link=TB("Tread link "+i.ToString("D2"),module,Vector3.zero,new Vector3(.36f,.065f,.123f),robotDark,false,.012f);track.links[i]=link.transform;
                TB("Raised tread grip",link.transform,new Vector3(0,.038f,0),new Vector3(.28f,.015f,.042f),robotRubber,false,.005f);
            }
            TB("Ivory side armor",module,new Vector3(side*.18f,.54f,0),new Vector3(.065f,.32f,.95f),robotIvory,false,.035f);
            track.Advance(0);return track;
        }
        static void RobotCylinder(string name,Transform parent,float radius,float depth,int sides,Material material)
        {
            var mesh=new Builder();for(int i=0;i<sides;i++){float a=i*2*Mathf.PI/sides,b=(i+1)*2*Mathf.PI/sides;var A=new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,-depth/2);var B=new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,-depth/2);var C=new Vector3(B.x,B.y,depth/2);var D=new Vector3(A.x,A.y,depth/2);mesh.Quad(A,B,C,D);mesh.Tri(Vector3.back*depth/2,B,A);mesh.Tri(Vector3.forward*depth/2,D,C);}Save(name,parent,Vector3.zero,mesh,material);
        }
        static void RobotTube(Transform parent,float radius,float inner,float depth,int sides,Material material)
        {
            var mesh=new Builder();for(int i=0;i<sides;i++){float a=i*2*Mathf.PI/sides,b=(i+1)*2*Mathf.PI/sides;Vector3 P(float r,float z,float t)=>new Vector3(Mathf.Cos(t)*r,Mathf.Sin(t)*r,z);mesh.Quad(P(radius,-depth/2,a),P(radius,-depth/2,b),P(radius,depth/2,b),P(radius,depth/2,a));mesh.Quad(P(inner,-depth/2,b),P(inner,-depth/2,a),P(inner,depth/2,a),P(inner,depth/2,b));mesh.Quad(P(radius,depth/2,a),P(radius,depth/2,b),P(inner,depth/2,b),P(inner,depth/2,a));}Save("Open muzzle rim",parent,Vector3.zero,mesh,material);
        }
        public static void VerifyRobotParts()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var rig=UnityEngine.Object.FindAnyObjectByType<RobotAssemblyRig>();if(!rig || rig.leftTrack.links.Length!=40 || rig.rightTrack.links.Length!=40 || !rig.tabletSocket)throw new Exception("Robot parts or dock missing");
            foreach(var filter in rig.GetComponentsInChildren<MeshFilter>())if(!filter.sharedMesh || !AssetDatabase.Contains(filter.sharedMesh))throw new Exception("Robot mesh not saved");
            rig.targetYaw=70;rig.targetElevation=30;for(int i=0;i<120;i++)rig.Advance(1f/60);if(Mathf.Abs(Mathf.DeltaAngle(rig.turretYaw.localEulerAngles.y,70))>.1f || Mathf.Abs(Mathf.DeltaAngle(rig.gunElevation.localEulerAngles.x,-30))>.1f)throw new Exception("Turret axes failed");
            rig.previewTransmission=true;rig.forwardCommand=1;rig.steeringCommand=0;rig.Advance(.1f);if(rig.LeftBeltSpeed!=rig.RightBeltSpeed)throw new Exception("Straight track mixing failed");rig.forwardCommand=0;rig.steeringCommand=1;rig.Advance(.1f);if(rig.LeftBeltSpeed*rig.RightBeltSpeed>=0)throw new Exception("Neutral turn mixing failed");rig.forwardCommand=.7f;rig.steeringCommand=.2f;rig.Advance(.1f);if(!(rig.LeftBeltSpeed>rig.RightBeltSpeed && rig.RightBeltSpeed>0))throw new Exception("Differential arc failed");
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);rig=UnityEngine.Object.FindAnyObjectByType<RobotAssemblyRig>();RobotPreview(rig,"QA/TrackedRobot.png",new Vector3(-3.7f,2.8f,4.7f));RobotPreview(rig,"QA/TrackedRobot_Rear.png",new Vector3(3.2f,2.6f,-4.2f));
            rig.targetYaw=40;rig.targetElevation=32;for(int i=0;i<120;i++)rig.Advance(1f/60);RobotPreview(rig,"QA/TrackedRobot_Articulation.png",new Vector3(-3.7f,2.8f,4.7f));
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Debug.Log("ROBOT_PARTS_VERIFIED: saved modular prefabs, turret yaw/elevation, 80 looping links, forward/neutral-turn/arc differential mix, empty tablet socket.");
        }
        static void RobotPreview(RobotAssemblyRig rig,string path,Vector3 offset)
        {
            var p=rig.transform.position;var q=rig.transform.rotation;rig.transform.SetPositionAndRotation(new Vector3(0,30,0),Quaternion.identity);var go=new GameObject("Temporary robot preview",typeof(Camera));var camera=go.GetComponent<Camera>();camera.transform.position=new Vector3(0,30,0)+offset;camera.transform.LookAt(new Vector3(0,30.98f,.14f));camera.orthographic=true;camera.orthographicSize=1.58f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.75f,.75f,.7f);
            bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;var rt=new RenderTexture(1400,1100,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1400,1100,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1400,1100),0,0);tex.Apply();Directory.CreateDirectory("QA");File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);ShaderUtil.allowAsyncCompilation=async;rig.transform.SetPositionAndRotation(p,q);
        }
    }
}
