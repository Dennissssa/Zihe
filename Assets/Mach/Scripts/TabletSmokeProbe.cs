using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Mach
{
    // Opt-in build verification. Attached by the development-build processor only.
    public sealed class TabletSmokeProbe : MonoBehaviour
    {
        IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-tablet-smoke")<0)yield break;
            Application.runInBackground=true;
            var tablet=GetComponent<HeldTablet>();
            GetComponent<FirstPersonWalker>().enabled=false;Cursor.lockState=CursorLockMode.None;
            yield return new WaitForSecondsRealtime(1);
            var keyboard=InputSystem.AddDevice<Keyboard>();
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../TabletQA"));Directory.CreateDirectory(output);
            tablet.SnapToRest();yield return new WaitForEndOfFrame();Capture(GetComponentInChildren<Camera>(),Path.Combine(output,"resting.png"));
            InputSystem.Update();InputState.Change(keyboard,new KeyboardState(Key.Tab));
            tablet.HandleInput(keyboard,true);
            if(!Check(tablet.IsRaised,"Tab did not raise",output))yield break;
            InputSystem.Update();InputState.Change(keyboard,new KeyboardState());
            tablet.HandleInput(keyboard,true);
            if(!Check(tablet.IsRaised,"Release changed toggle",output))yield break;
            yield return new WaitForSecondsRealtime(.8f);
            if(!Check(tablet.BlocksWorldInteraction && GetComponent<PlayerPickup>().FindTarget()==null,"Pickup not blocked",output))yield break;
            yield return new WaitForEndOfFrame();Capture(GetComponentInChildren<Camera>(),Path.Combine(output,"raised.png"));
            InputSystem.Update();InputState.Change(keyboard,new KeyboardState(Key.Tab));tablet.HandleInput(keyboard,true);
            if(!Check(!tablet.IsRaised,"Second Tab did not lower",output))yield break;
            InputSystem.Update();InputState.Change(keyboard,new KeyboardState());
            yield return new WaitForSecondsRealtime(.8f);
            if(!Check(!tablet.BlocksWorldInteraction,"Interaction did not resume",output))yield break;
            tablet.SetRaised(true);InputSystem.Update();InputState.Change(keyboard,new KeyboardState(Key.Escape));tablet.HandleInput(keyboard,true);
            if(!Check(!tablet.IsRaised,"Escape did not lower",output))yield break;
            InputSystem.RemoveDevice(keyboard);
            File.WriteAllText(Path.Combine(output,"result.txt"),"TABLET_RUNTIME_PASSED: Tab raise, release keeps raised, second Tab lowers, Escape lowers, pickup blocked/resumed, runtime screenshots.");
            Debug.Log("TABLET_RUNTIME_PASSED");yield return new WaitForSecondsRealtime(.5f);Application.Quit(0);
        }
        static bool Check(bool pass,string message,string output)
        {
            if(pass)return true;
            File.WriteAllText(Path.Combine(output,"result.txt"),"FAILED: "+message);Debug.LogError(message);Application.Quit(1);return false;
        }
        static void Capture(Camera camera,string path)
        {
            var rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;camera.Render();
            RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
            tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
            RenderTexture.active=null;camera.targetTexture=null;rt.Release();Destroy(rt);Destroy(tex);
        }
    }
}
