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
    public static class SummitFixAuthoring
    {
        static SummitFixAuthoring(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Temp/SummitFix.request";
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;
            File.Delete(request);try{Apply();File.WriteAllText("Logs/summit-fix-applied.txt","Saved summit repair and shared wave");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/summit-fix-error.txt",e.ToString());}
        }
        static Vector3 P(int j,int ring)
        {
            j%=36;float u=Mathf.Min(j,30)/30f,r=Mathf.Lerp(75,38,u),y=36*u-.35f;
            if(j>30){r=Mathf.Lerp(38,75,(j-30)/6f);y=Mathf.Lerp(35.65f,-.35f,(j-30)/6f);}
            // Extend the summit beyond the road end: no separate overlapping board.
            if(j>=30){r=38;y=36;}
            float radius=ring==0?114:ring==1?r+9:ring==2?r-9:28;
            float a=(-90+j*10)*Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a)*radius,ring==0?-2:ring==3?36:y,115+Mathf.Sin(a)*radius);
        }
        static void ReplaceMesh(GameObject go,Vector3[] vertices,string path)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}mesh.Clear();mesh.vertices=vertices;var ids=new int[vertices.Length];for(int i=0;i<ids.Length;i++)ids[i]=i;mesh.triangles=ids;mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshCollider>().sharedMesh=null;go.GetComponent<MeshCollider>().sharedMesh=mesh;
        }
        static void Move(GameObject go,Vector3 point)
        {if(!go)return;Undo.RecordObject(go.transform,"Move equipment to summit");go.transform.SetParent(null,true);go.transform.position=point;PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);}
        public static void ApplyBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Apply();}
        public static void Apply()
        {
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Logs/Before-summit-fix-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);
            var road=GameObject.Find("Continuous uphill road");var source=road.GetComponent<MeshFilter>().sharedMesh;var vertices=source.vertices;
            // The first 30 road quads remain; remove the six vertices of the added board.
            Array.Resize(ref vertices,180);ReplaceMesh(road,vertices,"Assets/Mach/MountainCourse/Repaired uphill road.asset");
            var v=new List<Vector3>();for(int i=0;i<36;i++)for(int r=0;r<3;r++)v.AddRange(new[]{P(i,r),P(i,r+1),P(i+1,r+1),P(i,r),P(i+1,r+1),P(i+1,r)});
            for(int i=0;i<36;i++)v.AddRange(new[]{new Vector3(0,36,115),P(i+1,3),P(i,3)});
            ReplaceMesh(GameObject.Find("Faceted mountain and summit"),v.ToArray(),"Assets/Mach/MountainCourse/Repaired summit.asset");
            int machine=0,point=0;
            var machines=UnityEngine.Object.FindObjectsByType<MovableMachine>();
            // Use stable names for future authoring rather than deprecated engine instance IDs.
            Array.Sort(machines,(a,b)=>string.CompareOrdinal(a.name,b.name));
            foreach(var m in machines)
            {
                Vector3 pos=m.GetComponent<CircuitPointMarker>()?new Vector3(-8+point++*8,36,96):new Vector3(-13,36,106+machine++*6);
                pos.y-= (m.boundsCenter.y-m.boundsSize.y*.5f)*m.transform.lossyScale.y;Move(m.gameObject,pos);
            }
            foreach(var robot in UnityEngine.Object.FindObjectsByType<RobotCircuitRunner>())Move(robot.gameObject,new Vector3(0,36,102));
            string[] names={"Workbench","Orange Crate","Blue Crate","Floor Crate","Temporary behavior buttons"};
            for(int i=0;i<names.Length;i++)
            {
                var go=GameObject.Find(names[i]);if(!go)continue;float bottom=float.PositiveInfinity;
                foreach(var c in go.GetComponentsInChildren<Collider>())if(!c.isTrigger)bottom=Mathf.Min(bottom,c.bounds.min.y);
                float height=float.IsPositiveInfinity(bottom)?0:go.transform.position.y-bottom;
                Move(go,new Vector3(12+(i%2)*5,36+height,102+(i/2)*5));
            }
            for(int i=1;i<=3;i++)Move(GameObject.Find("Suspension test bump "+i),new Vector3(16,36.07f,115+i*2));
            var defense=UnityEngine.Object.FindAnyObjectByType<GhostDefense>();defense.home=UnityEngine.Object.FindAnyObjectByType<DefenseBase>();defense.totalEnemies=50;
            if(!defense.waveLabel)
            {
                var label=UnityEngine.Object.Instantiate(defense.home.label,defense.home.transform);label.name="Shared wave status";label.transform.localPosition+=Vector3.up*.9f;label.text="INSERT TABLET TO BEGIN";defense.waveLabel=label;
            }
            EditorUtility.SetDirty(defense);Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(defense.gameObject.scene);EditorSceneManager.SaveScene(defense.gameObject.scene);AssetDatabase.SaveAssets();
        }
        public static void VerifyAndBuild()
        {
            ApplyBatch();var nav=UnityEngine.Object.FindAnyObjectByType<GhostDefense>();
            if(nav.Active||nav.ClaimSpawn())throw new Exception("Wave started before tablet");nav.BeginWave();nav.BeginWave();
            for(int i=0;i<50;i++)if(!nav.ClaimSpawn())throw new Exception("Shared quota too small");
            if(nav.ClaimSpawn())throw new Exception("Shared quota exceeded");
            for(int i=0;i<49;i++)nav.ReportDefeat();if(nav.Won)throw new Exception("Premature victory");nav.ReportDefeat();if(!nav.Won||nav.Active)throw new Exception("No victory");nav.BeginWave();if(nav.Active)throw new Exception("Won wave restarted");
            // Sample the full vehicle corridor around the summit exit, not just the center line.
            for(int j=28;j<=33;j++)for(int offset=-3;offset<=3;offset+=3)
            {
                float radius=j<=30?Mathf.Lerp(75,38,j/30f):38;float angle=(-90+j*10)*Mathf.Deg2Rad;float y=j<=30?j*1.2f:36;
                var p=new Vector3(Mathf.Cos(angle)*(radius+offset),y,115+Mathf.Sin(angle)*(radius+offset));
                if(!Physics.Raycast(p+Vector3.up*3,Vector3.down,out var hit,4)||Mathf.Abs(hit.point.y-y)>.45f)throw new Exception("Broken summit corridor "+j+" / "+offset);
            }
            nav.surface.BuildNavMesh();if(!NavMesh.SamplePosition(new Vector3(0,0,25),out var a,3,NavMesh.AllAreas)||!NavMesh.SamplePosition(new Vector3(0,36,108),out var b,3,NavMesh.AllAreas))throw new Exception("Missing NavMesh");var path=new NavMeshPath();if(!NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("No summit path");
            Debug.Log("SUMMIT_AND_SHARED_WAVE_VERIFIED");EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);TabletAuthoring.VerifyAndBuild();
        }
    }
}
