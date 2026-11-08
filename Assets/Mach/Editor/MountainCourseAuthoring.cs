using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
namespace Mach.Editor
{
    [InitializeOnLoad]
    public static class MountainCourseAuthoring
    {
        const string Folder="Assets/Mach/MountainCourse";
        static readonly Vector3 Center=new Vector3(0,0,115);
        static MountainCourseAuthoring(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Temp/MountainCourse.request";
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;
            File.Delete(request);try{Apply();File.WriteAllText("Logs/mountain-course-applied.txt","Mountain saved");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/mountain-course-error.txt",e.ToString());}
        }
        static Material Mat(string name,Color color)
        {var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};m.SetFloat("_Smoothness",.08f);AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;}
        static GameObject Box(string name,Transform root,Vector3 p,Vector3 size,Material mat)
        {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;return g;}
        static Vector3 Rad(float degree,float radius,float y){float a=degree*Mathf.Deg2Rad;return Center+new Vector3(Mathf.Cos(a)*radius,y,Mathf.Sin(a)*radius);}
        static Vector3 Road(int i)=>Rad(-90+i*10,Mathf.Lerp(75,38,i/30f),36*i/30f);
        static GameObject MeshObject(string name,Transform root,List<Vector3> vertices,Material material)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);var indices=new int[vertices.Count];for(int i=0;i<indices.Length;i++)indices[i]=i;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/"+name+".asset");
            var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));g.transform.SetParent(root,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<Renderer>().sharedMaterial=material;g.GetComponent<MeshCollider>().sharedMesh=mesh;return g;
        }
        static void Quad(List<Vector3> v,Vector3 a,Vector3 b,Vector3 c,Vector3 d){v.AddRange(new[]{a,b,c,a,c,d});}
        public static void ApplyBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Apply();}
        public static void Apply()
        {
            if(GameObject.Find("Low Poly Mountain"))return;
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Logs/Before-mountain-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            var old=GameObject.Find("White Driving Course");if(old)Undo.DestroyObjectImmediate(old);
            var root=new GameObject("Low Poly Mountain");Undo.RegisterCreatedObjectUndo(root,"Build mountain");
            var rock=Mat("White rock",new Color(.74f,.76f,.75f));var road=Mat("Pale mountain road",new Color(.57f,.63f,.66f));var white=Mat("Summit chalk",new Color(.87f,.86f,.82f));
            Box("Foot loading yard",root.transform,new Vector3(0,-1,-2),new Vector3(90,2,86),white);
            var v=new List<Vector3>();
            Vector3 Ring(int i,int ring)
            {
                int j=i%36;float progress=Mathf.Min(j,30)/30f;float r=Mathf.Lerp(75,38,progress),height=36*progress-.35f;
                if(j>30){r=Mathf.Lerp(38,75,(j-30)/6f);height=Mathf.Lerp(35.65f,-.35f,(j-30)/6f);}
                return Rad(-90+j*10,ring==0?114:ring==1?r+9:ring==2?r-9:28,ring==0?-2:ring==3?36:height);
            }
            for(int i=0;i<36;i++)for(int ring=0;ring<3;ring++)Quad(v,Ring(i,ring),Ring(i,ring+1),Ring(i+1,ring+1),Ring(i+1,ring));
            for(int i=0;i<36;i++)v.AddRange(new[]{Center+Vector3.up*36,Ring(i+1,3),Ring(i,3)});
            MeshObject("Faceted mountain and summit",root.transform,v,rock);
            var roadMesh=new List<Vector3>();
            for(int i=0;i<30;i++)
            {
                var a=Road(i);var b=Road(i+1);var ra=(a-Center-Vector3.up*a.y).normalized*9;var rb=(b-Center-Vector3.up*b.y).normalized*9;
                Quad(roadMesh,a+ra,a-ra,b-rb,b+rb);
                var edge=Box("Outer safety curb",root.transform,(a+ra+b+rb)*.5f+Vector3.up*.5f,new Vector3(.45f,1,Vector3.Distance(a+ra,b+rb)+.12f),white);edge.transform.rotation=Quaternion.LookRotation(b+rb-a-ra);
            }
            var end=Road(30);var inward=(Center+Vector3.up*36-end).normalized;var side=Vector3.Cross(Vector3.up,inward)*9;
            Quad(roadMesh,end+side,end-side,end+inward*18-side,end+inward*18+side);
            MeshObject("Continuous uphill road",root.transform,roadMesh,road);
            // Flat central yard plus three simple empty buildings with open fronts.
            for(int i=0;i<3;i++)
            {
                var house=new GameObject("Summit house "+(i+1)+" - empty interior");house.transform.SetParent(root.transform,false);house.transform.position=Center+new Vector3((i-1)*15,36,14);
                Box("Floor",house.transform,new Vector3(0,.08f,0),new Vector3(11,.16f,10),white);
                Box("Back wall",house.transform,new Vector3(0,2.5f,5),new Vector3(11,5,.4f),white);
                foreach(float x in new[]{-5.5f,5.5f})Box("Side wall",house.transform,new Vector3(x,2.5f,0),new Vector3(.4f,5,10),white);
                Box("Roof",house.transform,new Vector3(0,5.15f,0),new Vector3(12,.3f,11),white);
                foreach(float x in new[]{-4.1f,4.1f})Box("Front wall",house.transform,new Vector3(x,2.5f,-5),new Vector3(2.8f,5,.4f),white);
            }
            var home=UnityEngine.Object.FindAnyObjectByType<DefenseBase>();if(home){Undo.RecordObject(home.transform,"Place summit base");home.transform.position=Center+new Vector3(0,36,-4);}
            var sp=UnityEngine.Object.FindAnyObjectByType<GhostSpawner>();if(sp){Undo.RecordObject(sp.transform,"Place summit spawner");sp.transform.position=Center+new Vector3(0,36.16f,14);}
            EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);AssetDatabase.SaveAssets();
        }
        public static void VerifyAndBuild()
        {
            ApplyBatch();var nav=UnityEngine.Object.FindAnyObjectByType<GhostDefense>();nav.surface.BuildNavMesh();
            foreach(var origin in new[]{new Vector3(0,0,25),Center+new Vector3(2,36.16f,14)})
            {
                if(!NavMesh.SamplePosition(origin,out var a,3,NavMesh.AllAreas)||!NavMesh.SamplePosition(Center+new Vector3(0,36,-7),out var b,3,NavMesh.AllAreas))throw new Exception("Missing mountain NavMesh");
                var path=new NavMeshPath();if(!NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Mountain route disconnected");
            }
            for(int i=1;i<30;i++)if(!Physics.Raycast(Road(i)+Vector3.up*2,Vector3.down,out var h,3)||Mathf.Abs(h.point.y-Road(i).y)>.1f)throw new Exception("Road obstructed at "+i);
            Capture();Debug.Log("MOUNTAIN_ROUTE_VERIFIED");EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);TabletAuthoring.VerifyAndBuild();
        }
        static void Capture()
        {
            var go=new GameObject("Mountain preview",typeof(Camera));var c=go.GetComponent<Camera>();c.transform.position=new Vector3(210,215,-125);c.transform.LookAt(Center+Vector3.up*12);c.farClipPlane=1200;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.52f,.6f,.65f);
            var data=c.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.renderPostProcessing=true;
            var rt=new RenderTexture(1400,1000,24);c.targetTexture=rt;UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(c,new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1400,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1400,1000),0,0);tex.Apply();Directory.CreateDirectory("QA");File.WriteAllBytes("QA/Mountain.png",tex.EncodeToPNG());RenderTexture.active=old;c.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
