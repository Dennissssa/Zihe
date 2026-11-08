using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
namespace Mach.Editor
{
    [InitializeOnLoad]
    public static class GhostDefenseAuthoring
    {
        const string Folder="Assets/Mach/GhostDefense";
        static GhostDefenseAuthoring(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Temp/GhostDefense.request";
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;
            File.Delete(request);try{Apply();File.WriteAllText("Logs/ghost-defense-applied.txt","Ghost defense authored and saved.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/ghost-defense-error.txt",e.ToString());}
        }
        static Material Mat(string name,Color color)
        {
            string p=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}m.color=color;return m;
        }
        static GameObject Box(string name,Transform parent,Vector3 pos,Vector3 size,Material mat,bool solid=true)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        static TextMesh Label(Transform parent,Vector3 pos,string text)
        {
            var g=new GameObject("Label");g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localRotation=Quaternion.Euler(0,180,0);var t=g.AddComponent<TextMesh>();t.text=text;t.fontSize=64;t.characterSize=.045f;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=Color.white;
            foreach(var id in AssetDatabase.FindAssets("college t:Font")){var font=AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(id));if(font){t.font=font;g.GetComponent<Renderer>().sharedMaterial=font.material;break;}}return t;
        }
        static Mesh GhostMesh()
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Ghost.asset");if(mesh)return mesh;
            var v=new List<Vector3>();var triangles=new List<int>();
            Vector3 P(int ring,int i){float a=i*Mathf.PI*2/12;float r=ring==0?.43f:ring==1?.46f:ring==2?.35f:.04f;float y=ring==0?(i%2==0?.07f:.24f):ring==1?.72f:ring==2?1.02f:1.17f;return new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r);}
            void Tri(Vector3 a,Vector3 b,Vector3 c){int n=v.Count;v.Add(a);v.Add(b);v.Add(c);triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);}
            for(int r=0;r<3;r++)for(int i=0;i<12;i++){Tri(P(r,i),P(r+1,i),P(r+1,i+1));Tri(P(r,i),P(r+1,i+1),P(r,i+1));}
            for(int i=0;i<12;i++)Tri(P(3,i),new Vector3(0,1.18f,0),P(3,i+1));
            mesh=new Mesh{name="Low poly scalloped ghost"};mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/Ghost.asset");return mesh;
        }
        static float Ground(Vector3 p)
        {if(Physics.Raycast(p+Vector3.up*150,Vector3.down,out var h,300,~(1<<30),QueryTriggerInteraction.Ignore))return h.point.y;return 0;}
        public static void ApplyBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Apply();}
        public static void Apply()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            if(GameObject.Find("Ghost Defense Setup"))return;
            var cream=Mat("Cream",new Color(.83f,.8f,.68f));var dark=Mat("Graphite",new Color(.10f,.14f,.17f));var white=Mat("Eyes",Color.white);var blue=Mat("Pupils",new Color(.10f,.22f,.5f));
            string[] names={"Red","Pink","Cyan","Orange"};Color[] colors={new Color(.95f,.23f,.25f),new Color(1,.52f,.72f),new Color(.2f,.83f,.91f),new Color(1,.62f,.18f)};
            var prefabs=new GhostEnemy[4];
            for(int i=0;i<4;i++)
            {
                var g=new GameObject("Ghost "+names[i]);var e=g.AddComponent<GhostEnemy>();var agent=g.GetComponent<NavMeshAgent>();agent.radius=.43f;agent.height=1.2f;agent.stoppingDistance=.2f;agent.angularSpeed=240;agent.acceleration=8;
                var collider=g.AddComponent<CapsuleCollider>();collider.radius=.43f;collider.height=1.2f;collider.center=Vector3.up*.6f;
                var visual=new GameObject("Visual");visual.transform.SetParent(g.transform,false);e.visual=visual.transform;
                visual.AddComponent<MeshFilter>().sharedMesh=GhostMesh();visual.AddComponent<MeshRenderer>().sharedMaterial=Mat(names[i],colors[i]);
                foreach(float x in new[]{-.17f,.17f}){Box("Eye",visual.transform,new Vector3(x,.78f,.39f),new Vector3(.22f,.3f,.08f),white,false);Box("Pupil",visual.transform,new Vector3(x,.77f,.445f),new Vector3(.095f,.16f,.035f),blue,false);}
                prefabs[i]=PrefabUtility.SaveAsPrefabAsset(g,Folder+"/Ghost "+names[i]+".prefab").GetComponent<GhostEnemy>();UnityEngine.Object.DestroyImmediate(g);
            }
            var root=new GameObject("Ghost Defense Setup");var nav=root.AddComponent<GhostDefense>();var surface=root.AddComponent<NavMeshSurface>();nav.surface=surface;surface.collectObjects=CollectObjects.All;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.layerMask=~(1<<30);surface.overrideVoxelSize=true;surface.voxelSize=.25f;
            var home=new GameObject("Home Base");home.transform.SetParent(root.transform);var hp=home.AddComponent<DefenseBase>();var hc=home.AddComponent<BoxCollider>();hc.center=Vector3.up*.7f;hc.size=new Vector3(1.8f,1.4f,1.8f);
            Box("Base",home.transform,new Vector3(0,.15f,0),new Vector3(2,.3f,2),dark,false);Box("Core",home.transform,new Vector3(0,.8f,0),new Vector3(1.7f,1.1f,1.7f),cream,false);Box("Blue crest",home.transform,new Vector3(0,.85f,.87f),new Vector3(.6f,.6f,.05f),blue,false);hp.label=Label(home.transform,new Vector3(0,1.65f,0),"HOME BASE\nHP 30 / 30");
            home.transform.position=new Vector3(9,Ground(new Vector3(9,0,15)),15);
            var cage=new GameObject("Ghost Spawner - Four Colors");cage.transform.SetParent(root.transform);var sp=cage.AddComponent<GhostSpawner>();sp.navigation=nav;sp.target=hp;sp.monsters=prefabs;
            Box("Plinth",cage.transform,new Vector3(0,.15f,0),new Vector3(2.7f,.3f,2.7f),dark);Box("Roof",cage.transform,new Vector3(0,2.1f,0),new Vector3(2.7f,.2f,2.7f),cream);
            foreach(float x in new[]{-1.15f,1.15f})foreach(float z in new[]{-1.15f,1.15f})Box("Corner post",cage.transform,new Vector3(x,1.1f,z),new Vector3(.12f,1.9f,.12f),cream);
            for(int i=0;i<4;i++){var g=UnityEngine.Object.Instantiate(prefabs[i].visual.gameObject,cage.transform);g.name=names[i]+" preview";g.transform.localPosition=new Vector3((i-1.5f)*.58f,.35f,0);g.transform.localScale=Vector3.one*.55f;}
            sp.label=Label(cage.transform,new Vector3(0,2.6f,0),"GHOST SPAWNER\nRED / PINK / CYAN / ORANGE");cage.transform.position=new Vector3(9,Ground(new Vector3(9,0,30)),30);
            EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);AssetDatabase.SaveAssets();
        }
        public static void VerifyAndBuild()
        {
            ApplyBatch();var nav=UnityEngine.Object.FindAnyObjectByType<GhostDefense>();nav.surface.BuildNavMesh();
            var sp=UnityEngine.Object.FindAnyObjectByType<GhostSpawner>();
            if(sp.monsters.Length!=4||!NavMesh.SamplePosition(sp.transform.position+Vector3.right*2,out var start,2,NavMesh.AllAreas)||!NavMesh.SamplePosition(sp.target.transform.position,out var end,3,NavMesh.AllAreas))throw new Exception("Missing ghost assets or navigation endpoints");
            var path=new NavMeshPath();if(!NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("No ghost route to base");
            foreach(var prefab in sp.monsters)if(prefab.maxHealth!=1||!prefab.GetComponent<NavMeshAgent>())throw new Exception("Invalid ghost prefab");
            sp.target.Damage(1);if(sp.target.currentHealth!=29)throw new Exception("Base damage failed");
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Debug.Log("GHOST_DEFENSE_VERIFIED");TabletAuthoring.VerifyAndBuild();
        }
    }
}
