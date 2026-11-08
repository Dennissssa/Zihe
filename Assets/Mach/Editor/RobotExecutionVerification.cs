using System;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
    public static class RobotExecutionVerification
    {
        static void Circuit(BehaviorProgram p,bool drive,Vector3 point)
        {
            for(int i=0;i<p.nodes.Length;i++)p.nodes[i]=new CircuitNode();
            p.nodes[0]=new CircuitNode{active=true,kind=CircuitKind.Start,next=drive?6:1};
            p.nodes[1]=new CircuitNode{active=true,kind=CircuitKind.Facing,pointInput=3,next=2};
            p.nodes[2]=new CircuitNode{active=true,kind=CircuitKind.Fire,ammoInput=4,rateInput=5};
            p.nodes[3]=new CircuitNode{active=true,kind=CircuitKind.Point,point=point};
            p.nodes[4]=new CircuitNode{active=true,kind=CircuitKind.Ammo,value="AP"};
            p.nodes[5]=new CircuitNode{active=true,kind=CircuitKind.Rate,value="1"};
            p.nodes[6]=new CircuitNode{active=drive,kind=CircuitKind.DriveTo,pointInput=3,next=1};
        }
        public static void VerifyAndBuild()
        {VerifyOnly();TabletAuthoring.VerifyAndBuild();}
        public static void VerifyOnly()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var p=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();var r=UnityEngine.Object.FindAnyObjectByType<RobotCircuitRunner>();var motor=r.groundMotor;var body=motor.body;
            body.interpolation=RigidbodyInterpolation.None;var mode=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
            try
            {
                Circuit(p,false,r.transform.position+r.transform.forward*10+Vector3.up);
                p.ToggleDock(r);
                for(int i=0;i<400;i++){r.Tick(.02f);r.rig.Advance(.02f);motor.Step(.02f);Physics.Simulate(.02f);}
                Debug.Log("EXECUTION_DIAGNOSTIC: dock="+p.Docked+" status="+p.Status+" running="+r.Running+" node="+r.CurrentNode+" shots="+r.ShotCount+" position="+r.transform.position+" yaw="+r.rig.targetYaw+" elev="+r.rig.targetElevation+" localGun="+r.rig.gunElevation.localEulerAngles);
                if(!p.Docked||r.ShotCount<2)throw new Exception("Dock -> Facing -> Fire failed with physical body");
                p.ToggleDock(r);Vector3 initial=body.position;Circuit(p,true,initial+Vector3.ProjectOnPlane(r.transform.forward,Vector3.up).normalized*3+Vector3.up);
                p.ToggleDock(r);
                for(int i=0;i<750;i++){r.Tick(.02f);r.rig.Advance(.02f);motor.Step(.02f);Physics.Simulate(.02f);}
                Debug.Log("DRIVE_EXECUTION_DIAGNOSTIC: status="+p.Status+" node="+r.CurrentNode+" shots="+r.ShotCount+" position="+body.position+" moved="+Vector3.Distance(initial,body.position)+" grounded="+motor.Grounded);
                if(r.ShotCount<2)throw new Exception("Drive -> Facing -> Fire failed");
                Debug.Log("ROBOT_EXECUTION_VERIFIED");
            }
            finally{Physics.simulationMode=mode;}
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
        }
    }
}


