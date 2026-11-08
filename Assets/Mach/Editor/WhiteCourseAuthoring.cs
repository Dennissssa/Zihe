using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
namespace Mach.Editor
{
    [InitializeOnLoad]
    public static class WhiteCourseAuthoring
    {
        const string Folder="Assets/Mach/WhiteCourse";
        static WhiteCourseAuthoring(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Temp/WhiteCourse.request";
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;
            File.Delete(request);try{Apply();File.WriteAllText("Logs/white-course-applied.txt","White course saved");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/white-course-error.txt",e.ToString());}
        }
        static Material Mat(string name,Color color)
        {string p=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}m.color=color;m.SetFloat("_Smoothness",.12f);return m;}
        static void Box(string name,Transform root,Vector3 p,Vector3 size,Material mat)
        {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;}
        static void Strip(string name,Transform root,Vector3[] centers,float[] widths,float lift,Material mat,bool solid)
        {
            var v=new List<Vector3>();var t=new List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=v.Count;v.AddRange(new[]{a,b,c,d});t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            for(int i=0;i<centers.Length-1;i++)
            {
                Vector3 a=centers[i]+Vector3.up*lift-Vector3.right*widths[i],b=centers[i]+Vector3.up*lift+Vector3.right*widths[i],c=centers[i+1]+Vector3.up*lift+Vector3.right*widths[i+1],d=centers[i+1]+Vector3.up*lift-Vector3.right*widths[i+1];
                Quad(a,d,c,b);
                if(solid){Quad(a,a-Vector3.up*8,d-Vector3.up*8,d);Quad(b,c,c-Vector3.up*8,b-Vector3.up*8);}
            }
            if(solid)foreach(int i in new[]{0,centers.Length-1}){var a=centers[i]-Vector3.right*widths[i];var b=centers[i]+Vector3.right*widths[i];if(i==0)Quad(a,b,b-Vector3.up*8,a-Vector3.up*8);else Quad(b,a,a-Vector3.up*8,b-Vector3.up*8);}
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/"+name+".asset");
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=mat;if(solid)go.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        public static void ApplyBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Apply();}
        public static void Apply()
        {
            if(GameObject.Find("White Driving Course"))return;
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();Directory.CreateDirectory("Logs");
            string backup="Logs/Before-white-course-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),backup,true);
            foreach(string name in new[]{"Terrain","Island sea","Island distant sea"}){var old=GameObject.Find(name);if(old)Undo.DestroyObjectImmediate(old);}
            var floor=GameObject.Find("Floor");if(floor){Undo.RecordObject(floor,"Replace old ground");floor.SetActive(false);}
            var root=new GameObject("White Driving Course");Undo.RegisterCreatedObjectUndo(root,"Create white driving course");
            var white=Mat("Chalk white",new Color(.84f,.85f,.84f));var road=Mat("Route white",new Color(.73f,.77f,.79f));var edge=Mat("Edge white",new Color(.93f,.92f,.88f));
            var p=new[]{new Vector3(0,0,-45),new Vector3(0,0,35),new Vector3(10,1,75),new Vector3(-12,3,125),new Vector3(-12,3,160),new Vector3(18,6,210),new Vector3(18,6,285)};
            Strip("Faceted white platform",root.transform,p,new[]{45f,45f,23f,23f,23f,40f,40f},0,white,true);
            Strip("Driving route",root.transform,p,new[]{7f,7f,7f,7f,7f,7f,7f},.012f,road,false);
            // Low continuous edge curbs give readable boundaries without scenery clutter.
            float[] w={45,45,23,23,23,40,40};
            for(int i=0;i<p.Length-1;i++)foreach(int side in new[]{-1,1})
            {
                Vector3 a=p[i]+Vector3.right*w[i]*side,b=p[i+1]+Vector3.right*w[i+1]*side;var before=root.transform.childCount;
                Box("Low boundary",root.transform,(a+b)*.5f+Vector3.up*.45f,new Vector3(.5f,.9f,Vector3.Distance(a,b)+.2f),edge);
                root.transform.GetChild(before).rotation=Quaternion.LookRotation(b-a);
            }
            Box("Start edge",root.transform,new Vector3(0,.45f,-45),new Vector3(90,.9f,.5f),edge);
            Box("Base edge",root.transform,new Vector3(18,6.45f,285),new Vector3(80,.9f,.5f),edge);
            foreach(var pos in new[]{new Vector3(-28,0,22),new Vector3(27,1,78),new Vector3(-29,3,145),new Vector3(45,6,245)})
            {Box("Abstract landmark",root.transform,pos+Vector3.up*4,new Vector3(5,8,5),white);Box("Landmark cap",root.transform,pos+Vector3.up*8.15f,new Vector3(6,.3f,6),edge);}
            var home=UnityEngine.Object.FindAnyObjectByType<DefenseBase>();if(home){Undo.RecordObject(home.transform,"Move base to destination");home.transform.position=new Vector3(18,6,250);}
            var spawner=UnityEngine.Object.FindAnyObjectByType<GhostSpawner>();if(spawner){Undo.RecordObject(spawner.transform,"Move spawner near base");spawner.transform.position=new Vector3(-8,6,269);}
            EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);AssetDatabase.SaveAssets();
        }
        public static void VerifyAndBuild()
        {
            ApplyBatch();if(UnityEngine.Object.FindAnyObjectByType<Terrain>())throw new Exception("Terrain still active");
            var nav=UnityEngine.Object.FindAnyObjectByType<GhostDefense>();nav.surface.BuildNavMesh();
            foreach(var origin in new[]{new Vector3(0,0,20),new Vector3(-6,6,269)})
            {
                if(!NavMesh.SamplePosition(origin,out var a,3,NavMesh.AllAreas)||!NavMesh.SamplePosition(new Vector3(18,6,248),out var b,3,NavMesh.AllAreas))throw new Exception("Missing platform navigation");
                var path=new NavMeshPath();if(!NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Platform route disconnected");
            }
            Capture();EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Debug.Log("WHITE_COURSE_VERIFIED");TabletAuthoring.VerifyAndBuild();
        }
        static void Capture()
        {
            var go=new GameObject("Preview",typeof(Camera));var c=go.GetComponent<Camera>();c.transform.position=new Vector3(225,260,-100);c.transform.LookAt(new Vector3(0,0,120));c.farClipPlane=1500;c.backgroundColor=new Color(.67f,.73f,.78f);c.clearFlags=CameraClearFlags.SolidColor;
            var rt=new RenderTexture(1400,1000,24);c.targetTexture=rt;RenderPipeline.SubmitRenderRequest(c,new RenderPipeline.StandardRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1400,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1400,1000),0,0);tex.Apply();Directory.CreateDirectory("QA");File.WriteAllBytes("QA/WhiteCourse.png",tex.EncodeToPNG());RenderTexture.active=old;c.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
