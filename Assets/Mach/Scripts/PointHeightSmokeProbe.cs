using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Mach
{
    public sealed class PointHeightSmokeProbe : MonoBehaviour
    {
        void Check(bool ok,string message){if(!ok){Application.Quit(1);throw new Exception("POINT_EXIT_FAILED: "+message);}Debug.Log("POINT_EXIT_CHECK: "+message);}
        IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-point-height-smoke")<0)yield break;
            Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;Time.captureDeltaTime=1f/60;yield return new WaitForSeconds(1);
            var driver=GetComponent<TruckDriver>();var pickup=GetComponent<PlayerPickup>();var walker=GetComponent<FirstPersonWalker>();
            var truck=FindAnyObjectByType<UtilityTruck>();truck.Body.isKinematic=true;
            truck.driverDoor.SetOpen(true);yield return new WaitForSeconds(1);
            Check(driver.Enter(truck),"Enter existing truck");
            Check(driver.Exit(),"Exit to clear left side");
            Check(Vector3.Dot(transform.position-truck.driverAnchor.position,-truck.transform.right)>1.99f,"Exit at least two metres left of seat");
            var exit=transform.position;yield return null;yield return new WaitForFixedUpdate();
            Check(Vector3.Distance(transform.position,exit)<.25f,"Player remains outside after controller resumes");
            var marker=GetComponent<BehaviorProgram>().pointMarkers[0];var drag=GetComponent<PointHeightDrag>();
            walker.enabled=false;GetComponent<CharacterController>().enabled=false;
            pickup.heldTablet.poseRoot.gameObject.SetActive(false);
            transform.position=marker.transform.position-Vector3.forward*1.5f+Vector3.up*.06f;
            transform.rotation=Quaternion.identity;
            pickup.viewCamera.transform.position=marker.heightHandle.position-Vector3.forward*1.5f;
            pickup.viewCamera.transform.LookAt(marker.heightHandle);
            Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;Physics.SyncTransforms();
            Check(drag.FindTarget()==marker,"Reticle selects trigger handle on beam");
            Vector3 before=marker.Position,baseBefore=marker.transform.position;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Left));yield return null;yield return null;
            Check(drag.IsDragging,"LMB starts height drag");
            walker.enabled=true;GetComponent<CharacterController>().enabled=true;walker.ResetMotionAndLook();
            var oldView=pickup.viewCamera.transform.rotation;var oldPosition=transform.position;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.W));
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{delta=new Vector2(250,120)}.WithButton(MouseButton.Left));yield return null;yield return null;
            Check(Quaternion.Angle(oldView,pickup.viewCamera.transform.rotation)>1,"Camera freely turns while dragging");
            float until=Time.unscaledTime+.25f;
            while(Time.unscaledTime<until){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.W));yield return null;}
            Check(Vector3.Distance(transform.position,oldPosition)>.01f,"Walking remains active while dragging: "+Vector3.Distance(transform.position,oldPosition)+" W="+Keyboard.current.wKey.isPressed+" dt="+Time.deltaTime+" enabled="+walker.enabled+" speed="+walker.walkSpeed+" collisions="+GetComponent<CharacterController>().collisionFlags);
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            Check(marker.Position.y>before.y,"Upward mouse movement raises target");
            Check(marker.Position.x==before.x&&marker.Position.z==before.z&&marker.transform.position==baseBefore,"Horizontal mouse input never moves X/Z or emitter");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;yield return null;
            Check(!drag.IsDragging,"Release LMB finishes drag");
            var mover=marker.GetComponent<MovableMachine>();var committed=marker.Position;
            mover.BeginPreview();marker.transform.position+=Vector3.right*2;yield return null;
            Check(marker.Position==committed,"E preview retains committed target including height");
            mover.EndPreview(false);yield return null;
            Check(marker.Position==committed,"Cancel placement retains height");
            Debug.Log("POINT_HEIGHT_EXIT_RUNTIME_PASSED");Application.Quit(0);
        }
    }
}
