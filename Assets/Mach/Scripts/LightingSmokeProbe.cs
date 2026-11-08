using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Mach
{
    public sealed class LightingSmokeProbe : MonoBehaviour
    {
        void Check(bool value,string message){if(!value)throw new Exception("LIGHTING_FAILED: "+message);Debug.Log("LIGHTING_CHECK: "+message);}
        IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-lighting-smoke")<0)yield break;
            Application.runInBackground=true;yield return new WaitForSeconds(1);
            var player=GetComponent<PlayerPickup>();GetComponent<FirstPersonWalker>().enabled=false;GetComponent<HeldTablet>().poseRoot.gameObject.SetActive(false);
            var truck=FindAnyObjectByType<UtilityTruck>();var rig=truck.GetComponent<TruckLighting>();truck.Body.isKinematic=true;
            Check(rig&&rig.tailLights.Length==2&&rig.tailLenses.Length==2,"Two authored brake/tail lamp pairs");
            int headlights=0;foreach(var light in truck.GetComponentsInChildren<Light>())if(light.type==LightType.Spot&&light.enabled&&light.range>=20)headlights++;
            Check(headlights==2,"Two forward spot headlights illuminate the world");
            var volume=GameObject.Find("Industrial lighting and bloom").GetComponent<Volume>();
            Check(volume.sharedProfile.TryGet<Bloom>(out var bloom)&&bloom.active&&bloom.intensity.value>0,"Bloom profile is enabled");
            Check(player.heldTablet.tabletCamera.GetUniversalAdditionalCameraData().renderPostProcessing,"Final camera stack applies post processing");
            var camera=player.viewCamera;
            camera.transform.position=truck.transform.TransformPoint(new Vector3(4,2.8f,6));camera.transform.LookAt(truck.transform.TransformPoint(new Vector3(0,1,0)));
            yield return Capture(camera,"Lighting_Truck_Front.png");
            truck.SetDriver(GetComponent<TruckDriver>());truck.SetInputs(0,0,false);rig.UpdateLights(true);float tailIntensity=rig.tailLights[0].intensity;
            camera.transform.position=truck.transform.TransformPoint(new Vector3(3,2.5f,-7));camera.transform.LookAt(truck.transform.TransformPoint(new Vector3(0,1,-1.4f)));
            yield return Capture(camera,"Lighting_Tail.png");
            truck.SetInputs(0,0,true);rig.UpdateLights(true);
            var properties=new MaterialPropertyBlock();rig.tailLenses[0].GetPropertyBlock(properties);
            Check(rig.Braking&&rig.tailLights[0].intensity>tailIntensity*3&&properties.GetColor("_EmissionColor").r>rig.tailEmission*2,"Braking brightens both light and emissive lens");
            yield return Capture(camera,"Lighting_Brake.png");
            truck.SetInputs(0,0,false);rig.UpdateLights(true);Check(!rig.Braking&&Mathf.Approximately(rig.tailLights[0].intensity,tailIntensity),"Releasing brake restores normal tail light");truck.SetDriver(null);
            var robot=FindAnyObjectByType<RobotAssemblyRig>();int markers=0;foreach(var light in robot.GetComponentsInChildren<Light>())if(light.name=="Authored robot marker glow")markers++;
            Check(markers>=4,"Robot width and dock status lamps authored");
            camera.transform.position=robot.transform.TransformPoint(new Vector3(2.7f,2,3.8f));camera.transform.LookAt(robot.transform.TransformPoint(new Vector3(0,.9f,0)));
            yield return Capture(camera,"Lighting_Robot.png");
            var console=FindAnyObjectByType<FireRateConsole>();var screen=console.transform.Find("03 • Inclined controls/Portrait display glass");
            Check(screen&&screen.GetComponent<Renderer>().sharedMaterial.IsKeywordEnabled("_EMISSION"),"Instrument display material is emissive");
            camera.transform.position=console.transform.position-console.transform.forward*2+Vector3.up*1.3f;camera.transform.LookAt(console.transform.position+Vector3.up*.95f);
            yield return Capture(camera,"Lighting_Instrument.png");
            Debug.Log("LIGHTING_RUNTIME_PASSED");Application.Quit(0);
        }
        static IEnumerator Capture(Camera camera,string name)
        {
            var rt=new RenderTexture(1600,1000,24,RenderTextureFormat.DefaultHDR,RenderTextureReadWrite.Linear);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();yield return null;yield return new WaitForEndOfFrame();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});
            var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();var pixels=image.GetPixels();for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;image.SetPixels(pixels);image.Apply();
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../LightingQA"));Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name),image.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;rt.Release();Destroy(rt);Destroy(image);
        }
    }
}

