using System;
using System.Collections;
using UnityEngine;
namespace Mach
{
    // Development-build verification only; never attached to the authored scene.
    public sealed class TruckSmokeProbe : MonoBehaviour
    {
        bool Check(bool ok,string message){if(ok){Debug.Log("TRUCK_CHECK: "+message);return true;}Debug.LogError("TRUCK_FAILED: "+message);Application.Quit(1);return false;}
        IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-truck-smoke")<0)yield break;
            Application.runInBackground=true;
            var truck=FindFirstObjectByType<UtilityTruck>();var driver=GetComponent<TruckDriver>();
            truck.Body.position=new Vector3(7,.2f,3);truck.Body.rotation=Quaternion.identity;
            driver.walker.enabled=false;driver.pickup.enabled=false;driver.enabled=false;
            yield return new WaitForSeconds(2);
            int grounded=0;foreach(var wheel in truck.wheels)if(wheel.isGrounded)grounded++;
            if(!Check(grounded==4,"Four suspension wheels grounded"))yield break;
            truck.driverDoor.GetComponent<TruckInteractable>().Activate();truck.tailgate.GetComponent<TruckInteractable>().Activate();
            yield return new WaitForSeconds(1);
            if(!Check(Mathf.Abs(truck.driverDoor.Angle)>70 && Mathf.Abs(truck.tailgate.Angle)>80,"Door and tailgate animated open"))yield break;
            truck.tailgate.GetComponent<TruckInteractable>().Activate();
            if(!Check(driver.Enter(truck),"Board driver seat"))yield break;
            if(!Check(!driver.walker.enabled && !GetComponent<CharacterController>().enabled && !driver.tablet.poseRoot.gameObject.activeSelf,"Walking and tablet suspended"))yield break;
            var steeringAxis=truck.steeringWheel.localRotation*Vector3.up;
            int lockTicks=Mathf.RoundToInt(.5f/Time.fixedDeltaTime),halfTicks=lockTicks/2;
            var camera=driver.pickup.viewCamera;var rimPoint=Vector3.forward*.2f;
            float centerX=camera.WorldToViewportPoint(truck.steeringWheel.TransformPoint(rimPoint)).x;
            for(int i=0;i<5;i++){truck.SetInputs(0,1,true);yield return new WaitForFixedUpdate();}
            yield return new WaitForEndOfFrame();
            if(!Check(camera.WorldToViewportPoint(truck.steeringWheel.TransformPoint(rimPoint)).x>centerX+.025f,"Right input moves top of steering wheel right in driver view"))yield break;
            for(int i=5;i<halfTicks;i++){truck.SetInputs(0,1,true);yield return new WaitForFixedUpdate();}
            if(!Check(Mathf.Abs(truck.SteeringInput-.5f)<.09f && truck.wheels[0].steerAngle>12 && truck.wheels[0].steerAngle<19,"Half steering lock after approximately 0.25 seconds"))yield break;
            for(int i=halfTicks;i<lockTicks;i++){truck.SetInputs(0,1,true);yield return new WaitForFixedUpdate();}
            yield return null;
            if(!Check(truck.SteeringInput>.97f && Vector3.Dot(steeringAxis,truck.steeringWheel.localRotation*Vector3.up)>.999f,"Full lock after 0.5 seconds; steering wheel rotates around fixed column"))yield break;
            if(!Check(Vector3.Dot(truck.wheelVisuals[0].right,truck.transform.rotation*Quaternion.Euler(0,truck.wheels[0].steerAngle,0)*Vector3.right)>.995f,"Front tire axle follows physical yaw without tilting"))yield break;
            for(int i=0;i<halfTicks;i++){truck.SetInputs(0,-1,true);yield return new WaitForFixedUpdate();}
            if(!Check(truck.SteeringInput>.4f && truck.SteeringInput<.6f,"Opposite input unwinds instead of snapping across"))yield break;
            for(int i=0;i<lockTicks*2-halfTicks;i++){truck.SetInputs(0,-1,true);yield return new WaitForFixedUpdate();}
            if(!Check(truck.SteeringInput<-.97f && truck.wheels[0].steerAngle<-29,"Left steering reaches matching physical lock"))yield break;
            for(int i=0;i<Mathf.CeilToInt(.8f/Time.fixedDeltaTime);i++){truck.SetInputs(0,0,true);yield return new WaitForFixedUpdate();}
            if(!Check(Mathf.Abs(truck.SteeringInput)<.01f,"Smooth return to center"))yield break;
            yield return new WaitForEndOfFrame();Capture(driver.pickup.viewCamera,"Truck_Driver.png");
            var cameraRotation=camera.transform.localRotation;camera.transform.localRotation=Quaternion.Euler(18,20,0);Capture(camera,"Truck_Interior.png");camera.transform.localRotation=cameraRotation;
            for(int i=0;i<5;i++){truck.SetInputs(0,-1,true);yield return new WaitForFixedUpdate();}
            yield return new WaitForEndOfFrame();
            if(!Check(camera.WorldToViewportPoint(truck.steeringWheel.TransformPoint(rimPoint)).x<centerX-.025f,"Left input moves top of steering wheel left in driver view"))yield break;
            for(int i=0;i<40;i++){truck.SetInputs(0,0,true);yield return new WaitForFixedUpdate();}
            var start=truck.transform.position;float min=10,max=-10;
            truck.SetInputs(1,0,false);
            for(int i=0;i<120;i++){
                truck.SetInputs(1,0,false);yield return new WaitForFixedUpdate();truck.wheels[0].GetWorldPose(out var position,out var rotation);
                float travel=truck.transform.InverseTransformPoint(position).y;min=Mathf.Min(min,travel);max=Mathf.Max(max,travel);
            }
            if(!Check(Vector3.Distance(start,truck.transform.position)>4 && truck.Speed>2,"Powered driving displacement: "+Vector3.Distance(start,truck.transform.position)+" speed: "+truck.Speed+" position: "+truck.transform.position+" rpm: "+truck.wheels[0].rpm+" torque: "+truck.wheels[0].motorTorque))yield break;
            if(!Check(max-min>.015f,"Suspension travel over test bumps: "+(max-min)))yield break;
            yield return null;
            if(!Check(Vector3.Dot(truck.wheelVisuals[0].right,truck.transform.right)>.999f && Mathf.Abs(truck.wheels[0].rpm)>20,"Rolling tire keeps its X axle aligned"))yield break;
            if(!Check(!driver.Exit(),"Moving exit blocked"))yield break;
            truck.SetInputs(0,1,true);yield return new WaitForSeconds(2);
            if(!Check(truck.Speed<.8f && truck.wheels[0].steerAngle>10,"Brake and front steering"))yield break;
            if(!Check(driver.Exit(),"Safe exit"))yield break;
            if(!Check(driver.walker.enabled && driver.pickup.enabled && GetComponent<CharacterController>().enabled && driver.tablet.poseRoot.gameObject.activeSelf && !truck.Driver,"Walking and tablet restored"))yield break;
            Debug.Log("TRUCK_RUNTIME_PASSED");Application.Quit(0);
        }
        static void Capture(Camera camera,string name)
        {
            string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../TruckQA"));System.IO.Directory.CreateDirectory(folder);
            var rt=new RenderTexture(1400,900,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var tex=new Texture2D(1400,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1400,900),0,0);tex.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,name),tex.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;rt.Release();Destroy(rt);Destroy(tex);
        }
    }
}
