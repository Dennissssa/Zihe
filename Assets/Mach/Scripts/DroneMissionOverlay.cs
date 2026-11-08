using System.Collections.Generic;
using UnityEngine;

namespace Mach {
    public sealed class DroneMissionOverlay : MonoBehaviour {
        public GhostDefense defense;
        public EnemyRoutePreview[] routes;
        public Material silhouetteMaterial;
        public static DroneMissionOverlay Current { get; private set; }
        readonly Dictionary<Component,MeshRenderer[]> targets = new Dictionary<Component,MeshRenderer[]>();

        RobotCircuitRunner robot;
        MeshRenderer[] robotMeshes;
        void Start(){robot=FindAnyObjectByType<RobotCircuitRunner>(FindObjectsInactive.Include);if(robot)robotMeshes=robot.GetComponentsInChildren<MeshRenderer>(true);}
        void OnEnable() {
            Current=this;
            targets.Clear();
            if(!defense)return;
            foreach(var spawner in defense.objectiveSpawners) Cache(spawner);
            foreach(var home in defense.protectedBases) Cache(home);
        }
        void Cache(Component target) {
            if(target) targets[target]=target.GetComponentsInChildren<MeshRenderer>(true);
        }
        public Renderer[] VisibleTargets(bool hostile) {
            var visible=new List<Renderer>();
            foreach(var pair in targets) {
                if(!pair.Key || !pair.Key.gameObject.activeInHierarchy)continue;
                if(pair.Key is GhostSpawner spawner){if(!hostile || spawner.Destroyed)continue;}
                else if(hostile || ((DefenseBase)pair.Key).Destroyed)continue;
                foreach(var mesh in pair.Value)
                    if(mesh && mesh.enabled && mesh.gameObject.activeInHierarchy && !mesh.GetComponent<TextMesh>())visible.Add(mesh);
            }
            if(defense && defense.Active){
                if(hostile){
                    foreach(var enemy in GhostEnemy.Live)foreach(var mesh in enemy.OutlineMeshes)
                        if(mesh && mesh.enabled && mesh.gameObject.activeInHierarchy)visible.Add(mesh);
                    if(robot && robot.tracer && robot.tracer.enabled)visible.Add(robot.tracer);
                } else if(robot && robotMeshes!=null && robot.gameObject.activeInHierarchy){
                    foreach(var mesh in robotMeshes)if(mesh && mesh.enabled && mesh.gameObject.activeInHierarchy && !mesh.GetComponent<TextMesh>())visible.Add(mesh);
                }
            }
            return visible.ToArray();
        }
        public Renderer[] VisibleRoutes() {
            var visible=new List<Renderer>();
            foreach(var route in routes)if(route && route.line && route.line.enabled && route.line.positionCount>1)visible.Add(route.line);
            return visible.ToArray();
        }
        void OnDisable(){if(Current==this)Current=null;}
    }
}
