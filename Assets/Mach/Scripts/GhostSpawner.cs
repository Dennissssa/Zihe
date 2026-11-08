using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace Mach
{
    public sealed class GhostSpawner : MonoBehaviour
    {
        public GhostDefense navigation;
        public bool destructible;
        public float maxHealth=20,currentHealth=20;
        public bool Destroyed=>destructible&&currentHealth<=0;
        public void Damage(float amount){if(!destructible||Destroyed)return;currentHealth=Mathf.Max(0,currentHealth-Mathf.Max(0,amount));if(Destroyed){spawning=false;if(label)label.text="SPAWNER DESTROYED";}}
        public DefenseBase target;
        public Transform spawnOrigin;
        public GhostEnemy[] monsters;
        [Tooltip("-1 cycles through all four colors; otherwise select the prefab index.")]
        public int monsterIndex=-1;
        public float spawnInterval=3,initialDelay=3,spawnRadius=2;
        public int maxAlive=12;
        public bool spawning=true;
        public TextMesh label;
        readonly List<GhostEnemy> alive=new List<GhostEnemy>();float timer;
        void Start(){timer=initialDelay;currentHealth=maxHealth;}
        int sequence,lastWave;
        public string SpawnStatus {get;private set;}="Waiting for tablet";
        public static bool TryBaseApproach(Vector3 start,DefenseBase target,int areaMask,out Vector3 destination)
        {
            destination=default;if(!target)return false;var box=target.GetComponent<Collider>();var bounds=box?box.bounds:new Bounds(target.transform.position,Vector3.one);
            float best=float.PositiveInfinity;
            for(int j=0;j<8;j++)
            {
                float a=j*Mathf.PI/4;var edge=new Vector3(bounds.center.x+Mathf.Cos(a)*(bounds.extents.x+.8f),bounds.min.y,bounds.center.z+Mathf.Sin(a)*(bounds.extents.z+.8f));
                if(!NavMesh.SamplePosition(edge,out var goal,3,areaMask))continue;
                if(goal.position.x>bounds.min.x-.1f&&goal.position.x<bounds.max.x+.1f&&goal.position.z>bounds.min.z-.1f&&goal.position.z<bounds.max.z+.1f)continue;
                var path=new NavMeshPath();if(!NavMesh.CalculatePath(start,goal.position,areaMask,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                float distance=0;for(int c=1;c<path.corners.Length;c++)distance+=Vector3.Distance(path.corners[c-1],path.corners[c]);
                if(distance<best){best=distance;destination=goal.position;}
            }
            return !float.IsPositiveInfinity(best);
        }
        public bool TryFindSpawn(out Vector3 position) => TryFindSpawn(target,out position);
        public bool TryFindSpawn(DefenseBase destination,out Vector3 position)
        {
            position=default;if(!destination||destination.Destroyed)return false;
            for(int i=0;i<8;i++)
            {
                float angle=(i*45)*Mathf.Deg2Rad;
                var p=(spawnOrigin?spawnOrigin.position:transform.position)+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*spawnRadius;
                if(!NavMesh.SamplePosition(p,out var start,1,NavMesh.AllAreas))continue;
                // The base roof can be nearer than the ground to its center.
                // Require a complete path to ground around its perimeter instead.
                if(TryBaseApproach(start.position,destination,NavMesh.AllAreas,out _)){position=start.position;return true;}
            }
            return false;
        }
        bool TryChooseRoute(out DefenseBase destination,out Vector3 position)
        {
            var bases=navigation.protectedBases;
            if(bases==null||bases.Length==0){destination=target;return TryFindSpawn(target,out position);}
            int first=Mathf.Max(0,System.Array.IndexOf(bases,target));
            for(int attempt=0;attempt<bases.Length;attempt++){
                destination=bases[(first+sequence+attempt)%bases.Length];
                if(TryFindSpawn(destination,out position))return true;
            }
            destination=null;position=default;return false;
        }
        public int SpawnSequence=>sequence;
        public float RemainingDelay=>timer;
        public void RestoreTiming(int spawned,float delay){sequence=spawned;timer=delay;lastWave=navigation.WaveId;}
        public void SpawnGhost(int index,Vector3 position,DefenseBase goal,float hp=-1){
            var ghost=Instantiate(monsters[index],position,Quaternion.identity);
            ghost.origin=this;ghost.prefabIndex=index;ghost.restoredHealth=hp;ghost.target=goal;ghost.wave=navigation;alive.Add(ghost);
        }
        void Update()
        {
            if(Destroyed){if(label)label.text="SPAWNER DESTROYED";return;}
            if(label)label.text="GHOST SPAWNER\n"+(monsterIndex<0?"RED / PINK / CYAN / ORANGE":monsters!=null&&monsterIndex<monsters.Length&&monsters[monsterIndex]?monsters[monsterIndex].name:"NO MONSTER");
            if(!spawning||!navigation||!navigation.Ready||!navigation.Active)return;
            if(lastWave!=navigation.WaveId){lastWave=navigation.WaveId;timer=initialDelay;}
            alive.RemoveAll(g=>!g);timer-=Time.deltaTime;
            if(timer>0||alive.Count>=maxAlive||monsters==null||monsters.Length==0)return;
            timer=Mathf.Max(.1f,spawnInterval);
            int index=monsterIndex<0?sequence%monsters.Length:Mathf.Clamp(monsterIndex,0,monsters.Length-1);
            if(!monsters[index])return;
            if(!TryChooseRoute(out var destination,out var position)){SpawnStatus="No walkable path from cage to base";if(label)label.text+="\n"+SpawnStatus;return;}
            if(!navigation.ClaimSpawn())return;
            SpawnGhost(index,position,destination);sequence++;SpawnStatus="Spawning";
        }
    }
}

