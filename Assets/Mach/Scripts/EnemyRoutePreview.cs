using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace Mach
{
 [RequireComponent(typeof(LineRenderer))] public sealed class EnemyRoutePreview:MonoBehaviour
 {
  public DefenseBase destination;public GhostSpawner spawner;public LineRenderer line;
  public float width=.65f,refreshSeconds=.75f;
  public Color color=new Color(.35f,.85f,1,.28f);
  float timer;NavMeshPath path;readonly List<Vector3> points=new List<Vector3>();
  void Update(){timer-=Time.deltaTime;if(timer>0)return;timer=refreshSeconds;Refresh();}
  public void Refresh()
  {
   if(!line)return;line.enabled=false;if(path==null)path=new NavMeshPath();
   var goal=destination?destination:(spawner?spawner.target:null);
   if(!spawner||spawner.Destroyed||!spawner.gameObject.activeInHierarchy||!goal||goal.Destroyed||!goal.gameObject.activeInHierarchy||!spawner.navigation||!spawner.navigation.Ready)return;
   if(!spawner.TryFindSpawn(goal,out var start)||!GhostSpawner.TryBaseApproach(start,goal,NavMesh.AllAreas,out var end)||!NavMesh.CalculatePath(start,end,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)return;
   points.Clear();var corners=path.corners;for(int i=0;i<corners.Length-1;i++){int count=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(corners[i],corners[i+1])/.4f));for(int j=0;j<count;j++){var p=Vector3.Lerp(corners[i],corners[i+1],j/(float)count);if(NavMesh.SamplePosition(p,out var hit,.5f,NavMesh.AllAreas))p=hit.position;points.Add(p+Vector3.up*.055f);}}
   if(corners.Length>0)points.Add(corners[corners.Length-1]+Vector3.up*.055f);line.positionCount=points.Count;line.SetPositions(points.ToArray());line.widthMultiplier=width;line.startColor=line.endColor=color;line.enabled=points.Count>1;
  }
  void OnDisable(){if(line)line.enabled=false;}
 }
}
