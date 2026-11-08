using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mach {
    // A single local checkpoint per gameplay scene. Read into memory before reloading
    // so a failed read never destroys the current session.
    public sealed class MissionCheckpoint : MonoBehaviour {
        [Serializable] class Pose {
            public Vector3 position; public Quaternion rotation;
            public Pose(Transform t){position=t.position;rotation=t.rotation;}
            public void Apply(Transform t){t.SetPositionAndRotation(position,rotation);}
        }
        [Serializable] class Item {
            public string id,parent; public Pose pose;public bool active,packed,stored;
            public float height,seconds;public int selectedPoint;
        }
        [Serializable] class Enemy {public int origin,target,prefab;public Vector3 position;public float hp;}
        [Serializable] class Cage {public float hp,delay;public bool spawning;public int sequence;}
        [Serializable] class Data {
            public int version=1;public string scene;public int lesson=-1;
            public Pose player,truck;public int seat=-1,launcherSelection;public bool brake,headlights,leftDoor,rightDoor,tailgate;
            public BeaconDroneStation.FlightSnapshot flight; public CircuitNode[] circuit;public bool docked;public RobotCircuitRunner.Playback playback;
            public Item[] items;public Cage[] cages;public float[] bases;public Enemy[] enemies;
            public int wave,spawned,defeated;public bool active,won;public float airdrop;
        }
        static Data pending;
        public static bool Restoring=>pending!=null;
        string Slot=>"Mach.Checkpoint.v1."+gameObject.scene.name;
        readonly Dictionary<string,MovableMachine> machines=new Dictionary<string,MovableMachine>();
        readonly Dictionary<Transform,string> identities=new Dictionary<Transform,string>();
        BehaviorProgram program;UtilityTruck truck;GhostDefense defense;TutorialDirector tutorial;
        RobotCircuitRunner robot;EquipmentRack rack;EquipmentAirdrop drop;
        T Find<T>() where T:Component=>FindAnyObjectByType<T>(FindObjectsInactive.Include);
        void Awake(){
            program=Find<BehaviorProgram>();truck=Find<UtilityTruck>();defense=Find<GhostDefense>();tutorial=Find<TutorialDirector>();
            robot=Find<RobotCircuitRunner>();rack=Find<EquipmentRack>();drop=Find<EquipmentAirdrop>();
            foreach(var m in FindObjectsByType<MovableMachine>(FindObjectsInactive.Include)){
                if(m.gameObject.scene!=gameObject.scene)continue;
                string id=Address(m.transform);machines.Add(id,m);identities[m.transform]=id;
            }
        }
        static string Address(Transform t){
            if(!t)return "";
            string leaf=Uri.EscapeDataString(t.name)+":"+t.GetSiblingIndex();
            return t.parent?Address(t.parent)+"/"+leaf:leaf;
        }
        Transform Resolve(string path){
            if(string.IsNullOrEmpty(path))return null;
            if(machines.TryGetValue(path,out var machine))return machine.transform;
            Transform parent=null;
            foreach(var part in path.Split('/')){
                int colon=part.LastIndexOf(':');if(colon<0)return null;
                string name=Uri.UnescapeDataString(part.Substring(0,colon));int index=int.Parse(part.Substring(colon+1));
                Transform next=null;
                if(!parent){var roots=gameObject.scene.GetRootGameObjects();if(index<roots.Length&&roots[index].name==name)next=roots[index].transform;else foreach(var root in roots)if(root.name==name){next=root.transform;break;}}
                else {if(index<parent.childCount&&parent.GetChild(index).name==name)next=parent.GetChild(index);else for(int j=0;j<parent.childCount;j++)if(parent.GetChild(j).name==name){next=parent.GetChild(j);break;}}
                if(!next)return null;parent=next;
            }
            return parent;
        }
        public string Save(){
            var d=new Data{scene=gameObject.scene.path};
            if(tutorial)d.lesson=tutorial.Stage;
            else {
                if(machines.Values.Any(m=>m.IsPreview)||program.pickup.HeldItem)return Message("Finish moving or recalling the item first.","请先放下物件或等信标回收完成。");
                var drone=Find<BeaconDroneStation>();if(drone)d.flight=drone.CaptureFlight();
                d.player=new Pose(program.transform);d.truck=new Pose(truck.transform);d.brake=truck.parkingBrake;
                var driver=program.GetComponent<TruckDriver>();d.seat=driver.CurrentTruck?driver.CurrentSeat:-1;
                d.leftDoor=truck.driverDoor.IsOpen;d.rightDoor=truck.passengerDoor.IsOpen;d.tailgate=truck.tailgate.IsOpen;
                var lights=truck.GetComponentInChildren<TruckLighting>(true);d.headlights=lights&&lights.headlightsOn;
                d.launcherSelection=program.GetComponent<PointLauncher>().selected;d.circuit=program.Snapshot();d.docked=program.Docked;
                if(d.docked)d.playback=robot.CapturePlayback();if(drop)d.airdrop=drop.Elapsed;
                d.items=machines.Select(pair=>{
                    var m=pair.Value;var p=m.transform.parent;var slot=rack?rack.SlotFor(m):null;var marker=m.GetComponent<CircuitPointMarker>();var console=m.GetComponent<NavigationConsole>();
                    return new Item{id=pair.Key,pose=new Pose(m.transform),parent=p&&identities.TryGetValue(p,out var id)?id:Address(p),active=m.gameObject.activeSelf,packed=slot&&slot.Packed,stored=marker&&marker.stored,height=marker?marker.targetHeight:0,selectedPoint=console?console.selectedPoint:0,seconds=console?console.seconds:0};
                }).ToArray();
                d.wave=defense.WaveId;d.spawned=defense.Spawned;d.defeated=defense.Defeated;d.active=defense.Active;d.won=defense.Won;
                d.bases=defense.protectedBases.Select(x=>x.currentHealth).ToArray();
                d.cages=defense.objectiveSpawners.Select(x=>new Cage{hp=x.currentHealth,spawning=x.spawning,sequence=x.SpawnSequence,delay=x.RemainingDelay}).ToArray();
                d.enemies=GhostEnemy.Live.Where(x=>x.wave==defense&&x.currentHealth>0).Select(x=>new Enemy{origin=Array.IndexOf(defense.objectiveSpawners,x.origin),target=Array.IndexOf(defense.protectedBases,x.target),prefab=x.prefabIndex,hp=x.currentHealth,position=x.transform.position}).ToArray();
            }
            PlayerPrefs.SetString(Slot,JsonUtility.ToJson(d));PlayerPrefs.Save();return tutorial?Message("Lesson checkpoint saved.","已保存当前教学关。"):Message("Mission saved.","任务已保存。");
        }
        public string Load(){
            if(!PlayerPrefs.HasKey(Slot))return Message("No save in this scene.","此关卡还没有存档。");
            var d=JsonUtility.FromJson<Data>(PlayerPrefs.GetString(Slot));
            if(d==null||d.version!=1||d.scene!=gameObject.scene.path || (!tutorial&&(d.items==null||d.circuit==null||d.bases?.Length!=defense.protectedBases.Length||d.cages?.Length!=defense.objectiveSpawners.Length)))return Message("Incompatible save.","存档不兼容。");
            if(tutorial)TutorialDirector.ResumeLesson(d.lesson);else pending=d;
            Time.timeScale=1;AudioListener.pause=false;SceneManager.LoadScene(gameObject.scene.path);return "";
        }
        IEnumerator Start(){
            if(pending==null||pending.scene!=gameObject.scene.path)yield break;
            var d=pending;yield return null;pending=null;
            if(drop)drop.Advance(Mathf.Max(0,d.airdrop-drop.Elapsed));
            d.truck.Apply(truck.transform);truck.Body.position=d.truck.position;truck.Body.rotation=d.truck.rotation;
            truck.parkingBrake=d.brake;
            // Restore the rack before its contents, so parent movement cannot offset a restored item.
            foreach(var item in d.items.OrderBy(i=>machines.TryGetValue(i.id,out var m)&&m.GetComponent<EquipmentRack>()?0:1)){
                if(!machines.TryGetValue(item.id,out var m))continue;var slot=rack?rack.SlotFor(m):null;
                if(item.packed && slot){slot.Pack();continue;}
                if(slot&&slot.Packed)slot.Unpack();
                m.gameObject.SetActive(true);m.BeginPreview();item.pose.Apply(m.transform);m.EndPreview(true,Resolve(item.parent));
                var marker=m.GetComponent<CircuitPointMarker>();if(marker){marker.stored=item.stored;marker.SetHeight(item.height);}
                var console=m.GetComponent<NavigationConsole>();if(console){console.selectedPoint=item.selectedPoint;console.seconds=item.seconds;console.Refresh();}
                m.gameObject.SetActive(item.active);
            }
            var cc=program.GetComponent<CharacterController>();cc.enabled=false;d.player.Apply(program.transform);cc.enabled=true;
            var driver=program.GetComponent<TruckDriver>();
            if(d.seat>=0){var door=d.seat==0?truck.driverDoor:truck.passengerDoor;door.SetOpen(true);driver.Enter(truck,d.seat);}
            truck.driverDoor.SetOpen(d.leftDoor);truck.passengerDoor.SetOpen(d.rightDoor);truck.tailgate.SetOpen(d.tailgate);
            var lighting=truck.GetComponentInChildren<TruckLighting>(true);if(lighting){lighting.headlightsOn=d.headlights;lighting.UpdateLights(true);}
            program.nodes=d.circuit;program.Save();program.GetComponent<PointLauncher>().selected=d.launcherSelection;
            if(d.docked&&robot&&robot.gameObject.activeInHierarchy){program.ToggleDock(robot);if(program.Docked&&d.playback!=null)robot.RestorePlayback(d.playback);}
            for(int i=0;i<d.bases.Length;i++){var home=defense.protectedBases[i];home.currentHealth=d.bases[i];home.Damage(0);}
            defense.RestoreProgress(d.wave,d.spawned,d.defeated,d.active,d.won);
            for(int i=0;i<d.cages.Length;i++){var cage=defense.objectiveSpawners[i];var state=d.cages[i];cage.currentHealth=state.hp;cage.spawning=state.spawning;cage.RestoreTiming(state.sequence,state.delay);}
            foreach(var e in d.enemies)if(e.origin>=0&&e.target>=0)defense.objectiveSpawners[e.origin].SpawnGhost(e.prefab,e.position,defense.protectedBases[e.target],e.hp);
            Physics.SyncTransforms();
            var station=Find<BeaconDroneStation>();if(station)station.RestoreFlight(d.flight,program.pickup);
        }
        static string Message(string en,string cn)=>GamePreferences.Chinese?cn:en;
    }
}
