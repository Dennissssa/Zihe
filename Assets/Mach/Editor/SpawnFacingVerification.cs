using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
namespace Mach.Editor
{
    public static class SpawnFacingVerification
    {
        static void Check(bool condition,string message){if(!condition)throw new Exception(message);Debug.Log("SPAWN_FACING_PASS: "+message);}
        public static void VerifyAndBuild()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var defense=UnityEngine.Object.FindAnyObjectByType<GhostDefense>();
            typeof(GhostDefense).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(defense,null);
            var spawner=UnityEngine.Object.FindAnyObjectByType<GhostSpawner>();
            Debug.Log("CAGE_POSITION: "+spawner.transform.position+" radius "+spawner.spawnRadius);
            if(NavMesh.SamplePosition(spawner.target.transform.position,out var oldGoal,3,NavMesh.AllAreas))Debug.Log("OLD_BASE_GOAL: "+oldGoal.position+" base origin "+spawner.target.transform.position);
            Check(spawner.TryFindSpawn(out var spawn),"Actual cage has complete route to base perimeter");
            Check(GhostSpawner.TryBaseApproach(spawn,spawner.target,NavMesh.AllAreas,out var approach),"Enemy uses same reachable ground target");
            var prefab=spawner.monsters[0];var agent=prefab.GetComponent<NavMeshAgent>();
            Check(Vector3.Distance(approach,spawner.target.GetComponent<Collider>().ClosestPoint(approach))+agent.stoppingDistance<=prefab.attackRange+agent.radius,"Approach position is within base attack reach");
            var program=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();
            for(int i=0;i<program.nodes.Length;i++)program.nodes[i]=new CircuitNode();
            program.nodes[0]=new CircuitNode{active=true,kind=CircuitKind.Start,next=1};
            program.nodes[1]=new CircuitNode{active=true,kind=CircuitKind.Fire,next=2,ammoInput=4,rateInput=5};
            program.nodes[2]=new CircuitNode{active=true,kind=CircuitKind.Facing,pointInput=3};
            program.nodes[3]=new CircuitNode{active=true,kind=CircuitKind.Point,point=new Vector3(0,37,120)};
            program.nodes[4]=new CircuitNode{active=true,kind=CircuitKind.Ammo,value="AP"};
            program.nodes[5]=new CircuitNode{active=true,kind=CircuitKind.Rate,value="1"};
            Check(program.Validate(out var error),"Fire before Facing validates: "+error);
            program.nodes[1].next=-1;Check(!program.Validate(out error),"Missing connected Facing still rejected");program.nodes[1].next=2;
            var robot=UnityEngine.Object.FindAnyObjectByType<RobotCircuitRunner>();program.ToggleDock(robot);
            Check(program.Docked&&defense.Active,"Actual tablet insertion triggers wave");
            robot.Tick(.01f);robot.Tick(.01f);Check(robot.Firing,"Fire executes before Facing without basic-facing error");
            spawner.initialDelay=0;typeof(GhostSpawner).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(spawner,null);
            Check(defense.Spawned==1&&UnityEngine.Object.FindAnyObjectByType<GhostEnemy>(),"Actual spawner creates enemy and consumes shared quota");
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Debug.Log("SPAWN_FACING_VERIFIED");TabletAuthoring.VerifyAndBuild();
        }
    }
}
