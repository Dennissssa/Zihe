using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Mach.Editor
{
    public static partial class TabletAuthoring
    {
        const string Folder="Assets/Mach/Tablet";
        const string PlayerPath="Assets/Mach/Prefabs/FirstPersonPlayer.prefab";
        const int ViewLayer=30;
        static Material cream, graphite, screen, rubber, red, glove, cloth;
        static int index;
        [MenuItem("Mach/Author Held Tablet")]
        public static void Author()
        {
            Directory.CreateDirectory(Folder+"/Meshes");Directory.CreateDirectory(Folder+"/Materials");AssetDatabase.Refresh();
            string path=Folder+"/RuggedTablet.prefab";
            var tablet=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!tablet)
            {
                cream=Mat("Ivory housing",new Color(.72f,.7f,.61f),.18f);
                graphite=Mat("Graphite armor",new Color(.07f,.08f,.085f),.35f);
                rubber=Mat("Grip rubber",new Color(.019f,.024f,.025f),.05f);
                screen=Mat("Dark screen",new Color(.009f,.016f,.023f),0);
                screen.SetFloat("_SpecularHighlights",0);screen.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                red=Mat("Red power switch",new Color(.68f,.018f,.022f),.1f);
                glove=Mat("Work glove",new Color(.13f,.115f,.085f),.02f);
                cloth=Mat("Sleeve canvas",new Color(.075f,.09f,.08f),0);
                var model=new GameObject("RuggedTablet");
                Part("Ivory beveled shell",model.transform,Vector3.zero,.64f,.42f,.04f,.03f,cream);
                Part("Rear service cover",model.transform,new Vector3(0,0,.023f),.5f,.32f,.008f,.02f,cream);
                Part("Screen rubber seal",model.transform,new Vector3(0,0,-.022f),.513f,.374f,.009f,.018f,rubber);
                Frame("Bolted display bezel",model.transform,.5f,.359f,.449f,.305f,-.023f,-.038f,graphite);
                Part("Display glass",model.transform,new Vector3(0,0,-.028f),.45f,.306f,.007f,.01f,screen);
                foreach(float x in new[]{-.287f,.287f})
                {
                    Part("Side rubber grip",model.transform,new Vector3(x,0,-.004f),.059f,.267f,.052f,.012f,graphite);
                    for(int i=0;i<4;i++)Part("Grip groove",model.transform,new Vector3(x,-.045f-i*.019f,-.0305f),.037f,.004f,.001f,.001f,rubber);
                    foreach(float y in new[]{-.18f,.18f})Part("Protective corner bumper",model.transform,new Vector3(x,y,0),.074f,.067f,.054f,.021f,graphite);
                }
                Part("Power button socket",model.transform,new Vector3(-.287f,.075f,-.032f),.032f,.061f,.009f,.006f,rubber);
                Part("Red power button",model.transform,new Vector3(-.287f,.075f,-.039f),.022f,.047f,.007f,.004f,red);
                foreach(float y in new[]{-.204f,.204f})Part("Edge protection rail",model.transform,new Vector3(0,y,0),.42f,.012f,.045f,.004f,graphite);
                foreach(float x in new[]{-.237f,.237f})foreach(float y in new[]{-.165f,.165f})Bolt(model.transform,new Vector3(x,y,-.042f));
                foreach(float x in new[]{-.253f,.253f})foreach(float y in new[]{-.191f,.191f})Bolt(model.transform,new Vector3(x,y,-.026f));
                tablet=PrefabUtility.SaveAsPrefabAsset(model,path);UnityEngine.Object.DestroyImmediate(model);
            }
            var player=PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                var main=player.GetComponentInChildren<Camera>();
                var controller=player.GetComponent<HeldTablet>();
                if(!controller)
                {
                    controller=player.AddComponent<HeldTablet>();
                    var cameraObject=new GameObject("Tablet Overlay Camera",typeof(Camera),typeof(UniversalAdditionalCameraData));
                    cameraObject.transform.SetParent(main.transform,false);
                    var camera=cameraObject.GetComponent<Camera>();camera.fieldOfView=55;camera.nearClipPlane=.025f;camera.farClipPlane=3;
                    camera.cullingMask=1<<ViewLayer;camera.allowHDR=true;camera.allowMSAA=true;
                    var overlay=cameraObject.GetComponent<UniversalAdditionalCameraData>();overlay.renderType=CameraRenderType.Overlay;overlay.renderShadows=false;
                    var overlaySettings=new SerializedObject(overlay);overlaySettings.FindProperty("m_ClearDepth").boolValue=true;overlaySettings.ApplyModifiedPropertiesWithoutUndo();
                    var mainData=main.GetUniversalAdditionalCameraData();mainData.cameraStack.Add(camera);main.cullingMask&=~(1<<ViewLayer);
                    var pose=new GameObject("Tablet and left hand • animated pose").transform;pose.SetParent(main.transform,false);
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(tablet,pose);
                    var hand=new GameObject("Left hand • supporting grip").transform;hand.SetParent(pose,false);
                    glove=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Work glove.mat");
                    cloth=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Sleeve canvas.mat");
                    rubber=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Grip rubber.mat");
                    var palm=Part("Gloved palm behind tablet",hand,new Vector3(-.288f,-.114f,.049f),.093f,.126f,.05f,.022f,glove);
                    palm.transform.localRotation=Quaternion.Euler(0,0,-12);
                    for(int i=0;i<4;i++)
                    {
                        Part("Curled finger "+(i+1),hand,new Vector3(-.327f,-.137f+i*.026f,.006f),.038f,.023f,.055f,.009f,glove);
                        Part("Finger pad "+(i+1),hand,new Vector3(-.32f,-.137f+i*.026f,-.025f),.026f,.022f,.021f,.008f,glove);
                    }
                    var thumb=Part("Thumb over lower left grip",hand,new Vector3(-.266f,-.135f,-.046f),.035f,.073f,.025f,.011f,glove);
                    thumb.transform.localRotation=Quaternion.Euler(0,-10,-27);
                    var wrist=Part("Glove cuff",hand,new Vector3(-.308f,-.207f,.061f),.093f,.076f,.07f,.014f,rubber);
                    wrist.transform.localRotation=Quaternion.Euler(0,0,-16);
                    var sleeve=Part("Left forearm sleeve",hand,new Vector3(-.357f,-.347f,.095f),.106f,.245f,.092f,.018f,cloth);
                    sleeve.transform.localRotation=Quaternion.Euler(-8,0,-18);
                    foreach(var t in pose.GetComponentsInChildren<Transform>(true))t.gameObject.layer=ViewLayer;
                    foreach(var r in pose.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
                    controller.tabletCamera=camera;controller.poseRoot=pose;
                    controller.crosshair=player.transform.Find("HUD/Crosshair").GetComponent<Graphic>();
                    camera.aspect=16f/9f;controller.SnapToRest();camera.ResetAspect();
                    player.GetComponent<PlayerPickup>().heldTablet=controller;
                    var controls=player.transform.Find("HUD/Controls").GetComponent<Text>();
                    controls.text="WASD Move  |  Mouse Look  |  E Pick up / Drop  |  Tab Tablet  |  Esc Cursor";
                }
                PrefabUtility.SaveAsPrefabAsset(player,PlayerPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(player);}
            AssetDatabase.SaveAssets();
            VerifyAndPreview();
        }
        static Material Mat(string name,Color color,float metallic)
        {
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=color;
            material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",.28f);
            AssetDatabase.CreateAsset(material,Folder+"/Materials/"+name+".mat");return material;
        }
        static Vector3[] Loop(float w,float h,float c,float z)
        {
            return new[]{new Vector3(-w/2+c,-h/2,z),new Vector3(w/2-c,-h/2,z),new Vector3(w/2,-h/2+c,z),new Vector3(w/2,h/2-c,z),
                new Vector3(w/2-c,h/2,z),new Vector3(-w/2+c,h/2,z),new Vector3(-w/2,h/2-c,z),new Vector3(-w/2,-h/2+c,z)};
        }
        static GameObject Part(string name,Transform parent,Vector3 position,float w,float h,float depth,float corner,Material mat)
        {
            float bevel=Mathf.Min(.007f,Mathf.Min(w,h)*.15f);
            var outer=Loop(w,h,corner,0);var front=Loop(w-2*bevel,h-2*bevel,Mathf.Max(.0001f,corner-bevel),-depth/2);
            var back=Loop(w-2*bevel,h-2*bevel,Mathf.Max(.0001f,corner-bevel),depth/2);var data=new MeshData();
            for(int i=0;i<8;i++){int j=(i+1)%8;data.Quad(front[i],front[j],outer[j],outer[i]);data.Quad(outer[i],outer[j],back[j],back[i]);
                data.Tri(Vector3.back*depth/2,front[j],front[i]);data.Tri(Vector3.forward*depth/2,back[i],back[j]);}
            return Save(name,parent,position,data,mat);
        }
        static void Frame(string name,Transform parent,float w,float h,float iw,float ih,float back,float front,Material mat)
        {
            var outer=Loop(w,h,.015f,back);var lip=Loop(w-.008f,h-.008f,.012f,front);var inner=Loop(iw,ih,.009f,front);
            var innerBack=Loop(iw-.008f,ih-.008f,.009f,back);var data=new MeshData();
            for(int i=0;i<8;i++){int j=(i+1)%8;data.Quad(outer[i],lip[i],lip[j],outer[j]);data.Quad(lip[i],inner[i],inner[j],lip[j]);data.Quad(inner[i],innerBack[i],innerBack[j],inner[j]);}
            Save(name,parent,Vector3.zero,data,mat);
        }
        static void Bolt(Transform parent,Vector3 p)
        {
            var data=new MeshData();for(int i=0;i<6;i++){float a=i*Mathf.PI/3,b=(i+1)*Mathf.PI/3;
                var u=new Vector3(Mathf.Cos(a)*.0055f,Mathf.Sin(a)*.0055f,0);var v=new Vector3(Mathf.Cos(b)*.0055f,Mathf.Sin(b)*.0055f,0);
                data.Tri(Vector3.back*.002f,v+Vector3.back*.002f,u+Vector3.back*.002f);data.Quad(u+Vector3.back*.002f,v+Vector3.back*.002f,v,u);}
            Save("Hex bezel screw",parent,p,data,graphite);
        }
        sealed class MeshData
        {
            public List<Vector3> vertices=new List<Vector3>();public List<int> indices=new List<int>();
            public void Tri(Vector3 a,Vector3 b,Vector3 c){int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);indices.Add(n);indices.Add(n+1);indices.Add(n+2);}
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){Tri(a,b,c);Tri(a,c,d);}
        }
        static GameObject Save(string name,Transform parent,Vector3 p,MeshData data,Material mat)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(data.vertices);mesh.SetTriangles(data.indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path;do{path=Folder+"/Meshes/Tablet_"+(index++).ToString("D3")+".asset";}while(File.Exists(path));
            AssetDatabase.CreateAsset(mesh,path);var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=p;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;return go;
        }
        public static void VerifyAndPreview()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var tablet=UnityEngine.Object.FindAnyObjectByType<HeldTablet>();
            if(!tablet || !tablet.poseRoot || !tablet.tabletCamera)throw new Exception("Missing serialized tablet.");
            var main=tablet.GetComponentInChildren<Camera>();
            if(!main.GetUniversalAdditionalCameraData().cameraStack.Contains(tablet.tabletCamera))throw new Exception("Overlay not stacked.");
            if(tablet.poseRoot.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Viewmodel intercepts world physics.");
            var pickup=tablet.GetComponent<PlayerPickup>();
            if(pickup.heldTablet!=tablet)throw new Exception("Pickup tablet reference missing.");
            foreach(var m in tablet.poseRoot.GetComponentsInChildren<MeshFilter>())if(!m.sharedMesh || !AssetDatabase.Contains(m.sharedMesh))throw new Exception("Unsaved tablet mesh.");
                tablet.SnapToRest();tablet.SetRaised(true);
                tablet.AdvancePose(.14f);if(!tablet.BlocksWorldInteraction || pickup.FindTarget()!=null)throw new Exception("Reading did not block world pickup.");
                for(int frame=0;frame<120;frame++)tablet.AdvancePose(1f/60);
                var readingViewport=tablet.tabletCamera.WorldToViewportPoint(tablet.poseRoot.position);
                if(Vector2.Distance(new Vector2(readingViewport.x,readingViewport.y),tablet.readingAnchor)>.001f||Mathf.Abs(readingViewport.z-tablet.readingDistance)>.001f)throw new Exception("Reading pose not centered at configured distance.");
                tablet.SetRaised(false);
                for(int frame=0;frame<120;frame++)tablet.AdvancePose(1f/60);
                if(tablet.BlocksWorldInteraction)throw new Exception("Second Tab failed to lower.");
                tablet.SnapToRest();
            Directory.CreateDirectory("QA");
            Preview(main,tablet,false,"QA/Tablet_Resting.png",1600,900);
            Preview(main,tablet,true,"QA/Tablet_Raised.png",1600,900);
            Preview(main,tablet,true,"QA/Tablet_Raised_4x3.png",1200,900);
            tablet.SnapToRest();
            Debug.Log("TABLET_VERIFIED: serialized model/hand/overlay, pickup blocking, poses and 16:9/4:3 previews.");
        }
        static void Preview(Camera camera,HeldTablet tablet,bool raised,string path,int w,int h,bool settle=true)
        {
            bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            var rt=new RenderTexture(w,h,24){antiAliasing=4};camera.targetTexture=rt;camera.aspect=(float)w/h;tablet.tabletCamera.aspect=(float)w/h;
            if(settle){tablet.SetRaised(raised);for(int frame=0;frame<120;frame++)tablet.AdvancePose(1f/60);}
            camera.Render();camera.Render();RenderTexture.active=rt;
            var tex=new Texture2D(w,h,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
            RenderTexture.active=null;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);
            camera.ResetAspect();tablet.tabletCamera.ResetAspect();ShaderUtil.allowAsyncCompilation=async;
        }
        public static void VerifyAndBuild()
        {
            VerifyAndPreview();EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var result=BuildPipeline.BuildPlayer(new[]{WalkingSceneAuthoring.ScenePath},"Builds/WalkingPickup/WalkingPickup.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Tablet build failed: "+result.summary.result);
            Debug.Log("TABLET_BUILD_PASSED");
        }
    }
}
