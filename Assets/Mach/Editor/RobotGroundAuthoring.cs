using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
    [InitializeOnLoad]
    public static class RobotGroundAuthoring
    {
        static RobotGroundAuthoring(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Temp/RobotGround.request";
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;
            File.Delete(request);try{Apply();File.WriteAllText("Logs/robot-ground-applied.txt","Robot rigidbody locomotion saved");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/robot-ground-error.txt",e.ToString());}
        }
        public static void ApplyBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Apply();}
        public static void Apply()
        {
            const string materialPath="Assets/Mach/TrackedRobot/Materials/Ground drive.physicsMaterial";
            var contact=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(materialPath);
            if(!contact){contact=new PhysicsMaterial("Motor controlled traction");AssetDatabase.CreateAsset(contact,materialPath);}
            contact.staticFriction=0;contact.dynamicFriction=0;contact.frictionCombine=PhysicsMaterialCombine.Minimum;contact.bounciness=0;EditorUtility.SetDirty(contact);
            foreach(var runner in UnityEngine.Object.FindObjectsByType<RobotCircuitRunner>())
            {
                var rig=runner.rig;
                foreach(var mesh in runner.GetComponentsInChildren<MeshCollider>(true)){Undo.RecordObject(mesh,"Dynamic robot collision");mesh.convex=true;PrefabUtility.RecordPrefabInstancePropertyModifications(mesh);}
                var body=runner.GetComponent<Rigidbody>();if(!body)body=Undo.AddComponent<Rigidbody>(runner.gameObject);
                Undo.RecordObject(body,"Robot gravity and contacts");body.isKinematic=false;body.useGravity=true;body.mass=200;body.linearDamping=.1f;body.angularDamping=2;body.constraints=RigidbodyConstraints.None;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.centerOfMass=new Vector3(0,.35f,0);body.solverIterations=12;body.solverVelocityIterations=6;
                foreach(var track in new[]{rig.leftTrack,rig.rightTrack})
                {
                    var capsule=track.GetComponent<CapsuleCollider>();if(!capsule)capsule=Undo.AddComponent<CapsuleCollider>(track.gameObject);
                    Undo.RecordObject(capsule,"Rounded track ground contact");capsule.center=new Vector3(0,.4f,0);capsule.direction=2;capsule.radius=.32f;capsule.height=2.1f;PrefabUtility.RecordPrefabInstancePropertyModifications(capsule);
                }
                var motor=runner.GetComponent<RobotGroundMotor>();if(!motor)motor=Undo.AddComponent<RobotGroundMotor>(runner.gameObject);Undo.RecordObject(motor,"Robot ground motor");motor.body=body;motor.rig=rig;Undo.RecordObject(runner,"Connect physical drive");runner.groundMotor=motor;
                foreach(var c in runner.GetComponentsInChildren<Collider>(true))if(!c.isTrigger){Undo.RecordObject(c,"Controlled track traction");c.sharedMaterial=contact;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
                EditorUtility.SetDirty(body);EditorUtility.SetDirty(motor);EditorUtility.SetDirty(runner);PrefabUtility.RecordPrefabInstancePropertyModifications(body);PrefabUtility.RecordPrefabInstancePropertyModifications(runner);PrefabUtility.RecordPrefabInstancePropertyModifications(motor);
                EditorSceneManager.MarkSceneDirty(runner.gameObject.scene);
            }
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        static void Check(bool pass,string label){if(!pass)throw new Exception("ROBOT PHYSICS FAILED: "+label);Debug.Log("ROBOT_PHYSICS_PASS: "+label);}
        public static void VerifyAndBuild()
        {
            ApplyBatch();var motor=UnityEngine.Object.FindAnyObjectByType<RobotGroundMotor>();var body=motor.body;body.interpolation=RigidbodyInterpolation.None;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(500,49.5f,490);floor.transform.localScale=new Vector3(40,1,42);
            var ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);ramp.transform.position=new Vector3(500,52.105f,519.66f);ramp.transform.rotation=Quaternion.Euler(-15,0,0);ramp.transform.localScale=new Vector3(12,1,20);
            var previous=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
            void Step(int count){for(int i=0;i<count;i++){motor.Step(.02f);Physics.Simulate(.02f);}}
            try
            {
                body.position=new Vector3(500,53,504);body.rotation=Quaternion.identity;body.linearVelocity=body.angularVelocity=Vector3.zero;Physics.SyncTransforms();motor.Stop();Step(150);
                Check(body.position.y<50.3f&&body.position.y>49.7f,"Gravity lands on collider at arbitrary world Y=50");
                motor.SetDrive(3,0);Step(260);Debug.Log("CLIMB_POSITION "+body.position+" up "+motor.transform.up+" grounded "+motor.Grounded+" scale "+motor.transform.lossyScale+" velocity "+body.linearVelocity+" normal "+motor.GroundNormal);
                Check(body.position.z>515&&body.position.y>51,"Physical traction climbs ramp and gains height");
                Check(Vector3.Angle(motor.transform.up,ramp.transform.up)<12,"Body pitches with slope");
                motor.SetDrive(-3,0);Step(260);Check(body.position.z<512&&body.position.y<51,"Reversing descends to lower ground");
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(500,52,509);wall.transform.localScale=new Vector3(12,4,.5f);Physics.SyncTransforms();
                motor.SetDrive(3,0);Step(180);Check(body.position.z<508.2f,"Solid wall blocks the body instead of teleporting through it");
                body.position=new Vector3(550,58,500);body.linearVelocity=Vector3.zero;motor.Stop();Physics.SyncTransforms();Step(30);Check(body.position.y<57,"Unsupported robot falls under gravity");
            }
            finally{Physics.simulationMode=previous;}
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Debug.Log("ROBOT_GROUND_VERIFIED");TabletAuthoring.VerifyAndBuild();
        }
    }
}

