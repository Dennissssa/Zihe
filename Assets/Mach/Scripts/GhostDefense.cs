using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
namespace Mach
{
    [DefaultExecutionOrder(-200)]
    public sealed class GhostDefense : MonoBehaviour
    {
        public NavMeshSurface surface;
        public bool rebuildOnStart=true;
        public bool Ready {get;private set;}
        public int totalEnemies=50;
        public DefenseBase home;
        public DefenseBase[] protectedBases;
        public GhostSpawner[] objectiveSpawners;
        bool BasesLost {
            get {if(home&&home.Destroyed)return true;if(protectedBases!=null)foreach(var b in protectedBases)if(b&&b.Destroyed)return true;return false;}
        }
        public bool Lost=>BasesLost;
        bool ObjectivesCleared {
            get {if(objectiveSpawners==null||objectiveSpawners.Length==0)return Spawned==quota;
                foreach(var spawner in objectiveSpawners)if(spawner&&!spawner.Destroyed)return false;return true;}
        }
        void Evaluate(){if(!Active)return;if(BasesLost){Active=false;}else if(ObjectivesCleared&&Defeated==Spawned){Won=true;Active=false;}RefreshLabel();}
        public TextMesh waveLabel;
        public int Spawned {get;private set;}
        public int Defeated {get;private set;}
        public bool Active {get;private set;}
        public bool Won {get;private set;}
        public int WaveId {get;private set;}
        int quota;
        public void BeginWave()
        {
            if(Active||Won||!home||BasesLost)return;
            quota=Mathf.Max(1,totalEnemies);Spawned=Defeated=0;WaveId++;Active=true;RefreshLabel();
        }
        public void RestoreProgress(int wave,int spawned,int defeated,bool active,bool won){
            WaveId=wave;Spawned=spawned;Defeated=defeated;Active=active;Won=won;quota=Mathf.Max(1,totalEnemies);RefreshLabel();
        }
        public bool ClaimSpawn()
        {if(!Active||Won||!home||BasesLost||Spawned>=quota)return false;Spawned++;RefreshLabel();return true;}
        public void ReportDefeat(){if(!Active)return;Defeated++;Evaluate();}
        void Update(){if(Active)Evaluate();}
        void RefreshLabel(){if(waveLabel)waveLabel.text=Won?"VICTORY":BasesLost?"BASE LOST":Active?"WAVE  "+Defeated+" / "+quota+" DEFEATED\nSPAWNED "+Spawned:"AWAITING ENGAGEMENT";}
        void Start(){if(rebuildOnStart&&surface)surface.BuildNavMesh();Ready=surface&&surface.navMeshData;RefreshLabel();}
    }
}

