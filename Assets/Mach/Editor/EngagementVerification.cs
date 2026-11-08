using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
    public static class EngagementVerification
    {
        static void Check(bool value,string message){if(!value)throw new Exception(message);Debug.Log("ENGAGEMENT_PASS: "+message);}
        public static void Verify()
        {
            const string playerPath="Assets/Mach/Prefabs/FirstPersonPlayer.prefab";
            AssetDatabase.ImportAsset(playerPath,ImportAssetOptions.ForceUpdate);
            var player=PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                Check(player&&player.GetComponent<PlayerPickup>(),"Player prefab imports and loads");
                Check(string.IsNullOrWhiteSpace(player.transform.Find("HUD/Controls").GetComponent<UnityEngine.UI.Text>().text),"Prefab controls are empty");
            }
            finally{if(player)PrefabUtility.UnloadPrefabContents(player);}
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var p=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();
            var r=UnityEngine.Object.FindAnyObjectByType<RobotCircuitRunner>();
            var d=UnityEngine.Object.FindAnyObjectByType<GhostDefense>();
            var remote=p.remote;
            d.SendMessage("Start");
            Check(d.Ready&&!d.Active,"Navigation ready without starting a wave");
            for(int i=0;i<p.nodes.Length;i++)p.nodes[i]=new CircuitNode();
            p.nodes[0]=new CircuitNode{active=true,kind=CircuitKind.Start,next=1};
            p.nodes[1]=new CircuitNode{active=true,kind=CircuitKind.Facing,next=2,pointInput=3};
            p.nodes[2]=new CircuitNode{active=true,kind=CircuitKind.Fire,ammoInput=4,rateInput=5};
            p.nodes[3]=new CircuitNode{active=true,kind=CircuitKind.Point,point=r.transform.position+r.transform.forward*10+Vector3.up};
            p.nodes[4]=new CircuitNode{active=true,kind=CircuitKind.Ammo,value="AP"};
            p.nodes[5]=new CircuitNode{active=true,kind=CircuitKind.Rate,value="1"};
            p.ToggleDock(r);
            for(int i=0;i<300;i++){r.Tick(.02f);r.rig.Advance(.02f);}
            Check(p.Docked&&r.ShotCount>0&&!d.Active,"Tablet runs robot without spawning enemies");
            Vector3 raised=remote.panel.localPosition;
            remote.SetRaised(true);remote.AdvancePose(.1f);
            Check(remote.panel.gameObject.activeSelf&&Vector3.Distance(raised,remote.panel.localPosition)>.01f,"Remote lifts through intermediate pose");
            for(int i=0;i<100;i++)remote.AdvancePose(.02f);
            Check(Vector3.Distance(raised,remote.panel.localPosition)<.002f,"Remote reaches authored pose");
            remote.PressEngage();int wave=d.WaveId;remote.PressEngage();
            Check(d.Active&&d.WaveId==wave&&r.Firing,"Engage starts one wave without stopping robot");
            int count=0;while(d.ClaimSpawn())count++;
            Check(count==d.totalEnemies,"Global spawn quota unchanged");
            remote.SetRaised(false);remote.AdvancePose(.1f);remote.SetRaised(true);
            for(int i=0;i<100;i++)remote.AdvancePose(.02f);
            Check(Vector3.Distance(raised,remote.panel.localPosition)<.002f,"Lift reverses smoothly");
            remote.SetRaised(false);for(int i=0;i<100;i++)remote.AdvancePose(.02f);
            Check(!remote.panel.gameObject.activeSelf,"Remote hides after lowering");
            p.ToggleDock(r);Check(!p.Docked&&!r.Running&&!r.Firing,"Removing tablet stops robot");
            Check(string.IsNullOrWhiteSpace(p.board.help.text),"Tablet key overlay removed");
            Check(string.IsNullOrWhiteSpace(p.GetComponent<TruckDriver>().controls.text),"Persistent HUD controls removed");
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            Debug.Log("ENGAGEMENT_VERIFIED");
        }
    }
}
