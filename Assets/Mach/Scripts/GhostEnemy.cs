using UnityEngine;
using UnityEngine.AI;
namespace Mach
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class GhostEnemy : MonoBehaviour
    {
        public float maxHealth=1,currentHealth=1,moveSpeed=2,contactDamage=1,attackInterval=1,attackRange=.75f;
        public DefenseBase target;
        [HideInInspector] public GhostDefense wave;
        public static readonly System.Collections.Generic.HashSet<GhostEnemy> Live=new System.Collections.Generic.HashSet<GhostEnemy>();
        public MeshRenderer[] OutlineMeshes {get;private set;}
        void OnEnable(){OutlineMeshes=GetComponentsInChildren<MeshRenderer>(true);Live.Add(this);}
        void OnDisable(){Live.Remove(this);}
        public GhostSpawner origin; public int prefabIndex; public float restoredHealth=-1;
        bool dead;
        public Transform visual;
        public float bobHeight=.06f,bobSpeed=4;
        NavMeshAgent agent;float attackTimer,repath;Vector3 rest;
        void Start(){currentHealth=restoredHealth>=0?restoredHealth:maxHealth;agent=GetComponent<NavMeshAgent>();agent.speed=moveSpeed;if(visual)rest=visual.localPosition;}
        void Update()
        {
            if(MissionPauseMenu.BlocksGameplay)return;
            if(visual)visual.localPosition=rest+Vector3.up*Mathf.Sin(Time.time*bobSpeed)*bobHeight;
            if(!agent||!agent.isOnNavMesh)return;
            if(!target||target.Destroyed){agent.isStopped=true;return;}
            agent.speed=moveSpeed;attackTimer-=Time.deltaTime;repath-=Time.deltaTime;
            var collider=target.GetComponent<Collider>();var point=collider?collider.ClosestPoint(transform.position):target.transform.position;
            bool touching=Vector3.Distance(transform.position,point)<=attackRange+agent.radius;
            agent.isStopped=touching;
            if(touching){if(attackTimer<=0){target.Damage(contactDamage);attackTimer=Mathf.Max(.05f,attackInterval);}return;}
            if(repath<=0){repath=1;if(GhostSpawner.TryBaseApproach(transform.position,target,agent.areaMask,out var goal))agent.SetDestination(goal);}
        }
        public void Damage(float amount){if(dead)return;currentHealth-=Mathf.Max(0,amount);if(currentHealth<=0){dead=true;if(wave)wave.ReportDefeat();Destroy(gameObject);}}
    }
}
