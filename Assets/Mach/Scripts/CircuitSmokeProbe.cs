using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Mach
{
    public sealed class CircuitSmokeProbe : MonoBehaviour
    {
        string savePath,backup;bool hadSave;BehaviorProgram program;
        bool Check(bool ok,string message){if(ok){Debug.Log("CIRCUIT_CHECK: "+message);return true;}Debug.LogError("CIRCUIT_FAILED: "+message);Restore();Application.Quit(1);return false;}
        void Restore(){if(savePath==null)return;if(hadSave)File.WriteAllText(savePath,backup);else if(File.Exists(savePath))File.Delete(savePath);savePath=null;}
        void OnDestroy()=>Restore();
        IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-circuit-smoke")<0)yield break;
            Application.runInBackground=true;program=GetComponent<BehaviorProgram>();yield return new WaitForSeconds(1);program.walker.enabled=false;
            int initialCount=0;foreach(var node in program.nodes)if(node.active)initialCount++;
            if(!Check(initialCount==1&&program.nodes[0].kind==CircuitKind.Start&&program.nodes[0].next==-1,"Fresh session contains only disconnected START"))yield break;
            savePath=Path.Combine(Application.persistentDataPath,"mach-circuit-v1.json");hadSave=File.Exists(savePath);if(hadSave)backup=File.ReadAllText(savePath);if(Array.IndexOf(Environment.GetCommandLineArgs(),"-circuit-reset-save")>=0)hadSave=false;
            for(int i=0;i<program.nodes.Length;i++)program.nodes[i]=new CircuitNode();program.nodes[0]=new CircuitNode{active=true,kind=CircuitKind.Start,position=new Vector2(-455,195)};
            if(program.pointMarkers!=null&&program.pointMarkers.Length==3)
            {
                var marker=program.pointMarkers[0];var mover=marker.GetComponent<MovableMachine>();var placement=program.pickup.placement;
                Vector3 original=marker.transform.position;var camera=program.pickup.viewCamera;Quaternion cameraRotation=camera.transform.rotation;
                marker.transform.position=new Vector3(1,0,-1);Physics.SyncTransforms();yield return null;
                camera.transform.rotation=Quaternion.LookRotation(marker.Position+Vector3.up*.07f-camera.transform.position);
                if(!Check(placement.FindHandle()?.machine==mover,"Point base can be targeted for E movement"))yield break;
                int pointBlock=program.Add(CircuitKind.Point,"A",marker.Position);program.nodes[pointBlock].pointId=marker.pointId;
                var savedNode=program.nodes[pointBlock].Copy();Vector3 committed=marker.Position;
                if(!Check(placement.Begin(mover)&&placement.PreviewAt(new Vector3(2,0,-1),true),"Point supports valid ghost placement"))yield break;
                if(!Check(program.ResolvePoint(savedNode)==committed,"Point keeps committed location during ghost preview"))yield break;
                if(!Check(placement.Confirm()&&Vector3.Distance(program.ResolvePoint(savedNode),new Vector3(2,0,-1))<.01f,"Placement updates existing Point snapshot coordinates"))yield break;
                yield return null;placement.Begin(mover);placement.PreviewAt(new Vector3(2.5f,0,-1),true);placement.Cancel();
                if(!Check(Vector3.Distance(marker.Position,new Vector3(2,0,-1))<.01f,"Cancel restores committed Point position"))yield break;
                foreach(var collider in marker.GetComponentsInChildren<Collider>())if(!Check(collider.isTrigger,"Point has no solid blocking collider"))yield break;
                program.Remove(pointBlock);marker.transform.position=original;camera.transform.rotation=cameraRotation;Physics.SyncTransforms();
                Vector3 cameraPosition=camera.transform.position;program.tablet.poseRoot.gameObject.SetActive(false);
                camera.transform.position=new Vector3(-3,2.4f,4);camera.transform.LookAt(new Vector3(-3,.8f,11));
                yield return Capture(camera,"Point_Markers.png");camera.transform.position=cameraPosition;camera.transform.rotation=cameraRotation;program.tablet.poseRoot.gameObject.SetActive(true);
            }
            var robot=FindAnyObjectByType<RobotCircuitRunner>();
            if(!Check(!program.Validate(out _),"Empty program rejected"))yield break;
            FindAnyObjectByType<AmmoSelectionConsole>().Select();FindAnyObjectByType<FireRateConsole>().Confirm();
            if(!Check(program.nodes[1].kind==CircuitKind.Ammo&&program.nodes[2].kind==CircuitKind.Rate,"Machine confirmation creates distinct blocks"))yield break;
            int facing=program.Add(CircuitKind.Facing),point=program.Add(CircuitKind.Point,"TEST",robot.transform.position+robot.transform.forward*7),fire=program.Add(CircuitKind.Fire);
            program.nodes[1].position=new Vector2(-240,195);program.nodes[2].position=new Vector2(-25,195);program.nodes[facing].position=new Vector2(190,195);program.nodes[fire].position=new Vector2(405,195);program.nodes[point].position=new Vector2(190,-10);
            program.Connect(1,2,false);program.Connect(2,facing,false);program.Connect(facing,fire,false);program.Connect(point,facing,true);
            program.tablet.SetRaised(true);yield return new WaitForSeconds(.8f);
            if(!Check(program.Editing && Cursor.lockState!=CursorLockMode.Locked,"Tablet editing releases pointer"))yield break;
            // Exercise the actual port hit-testing with screen-space mouse input.
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;
            var mouse=Mouse.current??InputSystem.AddDevice<Mouse>();
            Vector2 zoomPosition=program.tablet.tabletCamera.WorldToScreenPoint(program.board.surface.position);
            InputSystem.Update();InputState.Change(mouse,new MouseState{position=zoomPosition,scroll=new Vector2(0,120)});yield return new WaitForEndOfFrame();
            if(!Check(program.board.workspace.localScale.x>1,"Mouse wheel zooms the tablet graph in"))yield break;
            InputSystem.Update();InputState.Change(mouse,new MouseState{position=zoomPosition,scroll=new Vector2(0,-120)});yield return new WaitForEndOfFrame();
            if(!Check(Mathf.Abs(program.board.workspace.localScale.x-1)<.01f,"Mouse wheel zooms the tablet graph out"))yield break;
            InputSystem.Update();InputState.Change(mouse,new MouseState{position=zoomPosition});
            var outPort=program.board.slots[0].output;var inPort=program.board.slots[1].input;
            Vector2 a=program.tablet.tabletCamera.WorldToScreenPoint(outPort.position),b=program.tablet.tabletCamera.WorldToScreenPoint(inPort.position);
            InputSystem.Update();InputState.Change(mouse,new MouseState{position=a}.WithButton(MouseButton.Left));yield return new WaitForEndOfFrame();
            InputSystem.Update();InputState.Change(mouse,new MouseState{position=a});yield return new WaitForEndOfFrame();
            InputSystem.Update();InputState.Change(mouse,new MouseState{position=b}.WithButton(MouseButton.Left));yield return new WaitForEndOfFrame();
            InputSystem.Update();InputState.Change(mouse,new MouseState{position=b});yield return new WaitForEndOfFrame();
            if(!Check(program.nodes[0].next==1,"Pointer clicks connect EXEC ports"))yield break;
            if(!Check(program.Validate(out _),"Arbitrary assembled circuit validates"))yield break;
            program.nodes[0].next=2;if(!Check(!program.Validate(out var error)&&error.Contains("AMMO"),"Missing ammunition rejected"))yield break;program.nodes[0].next=1;
            program.nodes[1].next=facing;if(!Check(!program.Validate(out error)&&error.Contains("RATE"),"Missing rate rejected"))yield break;program.nodes[1].next=2;
            program.nodes[2].next=fire;if(!Check(!program.Validate(out error)&&error.Contains("FACING"),"Missing base facing rejected"))yield break;program.nodes[2].next=facing;
            if(!Check(!program.Connect(point,fire,true),"Invalid typed connection rejected"))yield break;
            program.Validate(out error);program.Say(error);program.board.Refresh();yield return new WaitForEndOfFrame();yield return Capture(program.pickup.viewCamera,"Circuit_Tablet.png");
            program.nodes[fire].next=facing;program.tablet.SetRaised(false);yield return new WaitForSeconds(.7f);program.ToggleDock(robot);
            if(!Check(program.Docked&&program.tabletVisual.parent==robot.rig.tabletSocket&&!program.tablet.enabled,"Physical tablet docking transfers tablet and starts robot"))yield break;
            yield return new WaitForSeconds(3);
            if(!Check(robot.Firing&&robot.ShotCount>0,"Robot executes configuration, facing and persistent firing"))yield break;
            program.remote.SetRaised(true);yield return new WaitForEndOfFrame();yield return Capture(program.pickup.viewCamera,"Circuit_Remote.png");
            Vector2 stop=program.remote.uiCamera.WorldToScreenPoint(program.remote.stopButton.position);
            InputSystem.Update();InputState.Change(mouse,new MouseState{position=stop}.WithButton(MouseButton.Left));yield return null;yield return new WaitForEndOfFrame();
            InputSystem.Update();InputState.Change(mouse,new MouseState{position=stop});int shots=robot.ShotCount;
            yield return new WaitForSeconds(1.5f);
            if(!Check(robot.FireLatched&&!robot.Firing&&robot.ShotCount==shots&&robot.Running,"Remote red button latches cease-fire despite program loop"))yield break;
            program.remote.SetRaised(false);program.ToggleDock(robot);
            if(!Check(!program.Docked&&!robot.Running&&program.tabletVisual.parent==program.tablet.poseRoot&&program.tablet.enabled,"Removal stops program and restores carried tablet"))yield break;
            int drive=program.Add(CircuitKind.DriveTo),destination=program.Add(CircuitKind.Point,"MOVE TEST",robot.transform.position+robot.transform.forward*2.2f);
            program.nodes[facing].next=drive;program.nodes[fire].next=-1;program.Connect(destination,drive,true);Vector3 previous=robot.transform.position;
            program.ToggleDock(robot);yield return new WaitForSeconds(5);
            if(!Check(Vector3.Distance(previous,robot.transform.position)>1.5f&&!robot.Running,"Autonomous Drive To reaches a freely wired destination"))yield break;
            program.ToggleDock(robot);
            if(program.board.clearButton)
            {
                program.tablet.SetRaised(true);yield return new WaitForSeconds(.8f);
                if(program.board.checkButton.GetComponentInParent<TabletPhysicalButton>())
                {
                    int extra=program.Add(CircuitKind.Wait),end=program.Add(CircuitKind.Wait);program.nodes[extra].next=end;
                    yield return null;
                    yield return ClickRect(program.board.slots[extra].rect);


                    yield return ClickRect(program.board.deleteButton);
                    if(!Check(!program.Valid(extra),"Physical CLEAR deletes selected block"))yield break;
                    yield return ClickRect(program.board.checkButton);
                    if(!Check(program.Status.Contains("Circuit ready"),"Physical CHECK validates circuit"))yield break;
                }
                Vector2 clear=program.tablet.tabletCamera.WorldToScreenPoint(program.board.clearButton.position);
                var physical=program.board.clearButton.GetComponentInParent<TabletPhysicalButton>();Vector3 resting=physical?physical.transform.localPosition:Vector3.zero;
                InputSystem.Update();InputState.Change(mouse,new MouseState{position=clear}.WithButton(MouseButton.Left));yield return new WaitForEndOfFrame();
                int remaining=0;foreach(var node in program.nodes)if(node.active)remaining++;
                if(!Check(remaining==1&&program.nodes[0].kind==CircuitKind.Start&&program.nodes[0].next==-1,"One click clears all blocks and wires except START"))yield break;
                InputSystem.Update();InputState.Change(mouse,new MouseState{position=clear});
                if(physical){yield return new WaitForSeconds(.045f);if(!Check(physical.transform.localPosition.z>resting.z,"Physical key depresses"))yield break;yield return new WaitForSeconds(.3f);if(!Check(Vector3.Distance(physical.transform.localPosition,resting)<.0001f,"Physical key springs back"))yield break;}
                yield return Capture(program.pickup.viewCamera,"Circuit_Cleared.png");
            }
            Restore();Debug.Log("CIRCUIT_RUNTIME_PASSED");Application.Quit(0);
        }
        IEnumerator ClickRect(RectTransform rect)
        {
            Vector2 position=program.tablet.tabletCamera.WorldToScreenPoint(rect.position);
            InputSystem.Update();InputState.Change(Mouse.current,new MouseState{position=position}.WithButton(MouseButton.Left));yield return new WaitForEndOfFrame();
            InputSystem.Update();InputState.Change(Mouse.current,new MouseState{position=position});yield return new WaitForEndOfFrame();
        }
        static IEnumerator Capture(Camera camera,string name)
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../CircuitQA"));Directory.CreateDirectory(folder);
            var rt=new RenderTexture(1600,1000,24);camera.targetTexture=rt;
            Canvas.ForceUpdateCanvases();yield return null;yield return new WaitForEndOfFrame();
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(folder,name),tex.EncodeToPNG());
            RenderTexture.active=null;camera.targetTexture=null;rt.Release();Destroy(rt);Destroy(tex);
        }
    }
}


