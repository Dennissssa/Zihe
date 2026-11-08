using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mach.Editor
{
    public static partial class AmmoMachineAuthoring
    {
        static void SelectPrint(Builder mesh)
        {
            string[] glyphs={"111/100/111/001/111","111/100/110/100/111","100/100/100/100/111","111/100/110/100/111","111/100/100/100/111","111/010/010/010/010"};
            const float cell=.0105f;
            for(int letter=0;letter<6;letter++)
            {
                var rows=glyphs[letter].Split('/');
                for(int y=0;y<5;y++)for(int x=0;x<3;x++)if(rows[y][x]=='1')PrintBar(mesh,-.12075f+letter*4*cell+x*cell,(2-y)*cell,cell,cell);
            }
        }
        [MenuItem("Mach/Enable Ammo Selection")]
        public static void EnableAmmoSelection()
        {
            string oldPath=ButtonsFolder+"/Button_Fire.prefab",selectPath=ButtonsFolder+"/Button_Select.prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(oldPath))
            {
                string error=AssetDatabase.MoveAsset(oldPath,selectPath);if(!string.IsNullOrEmpty(error))throw new Exception(error);
            }
            string[] names={"Button_Left","Button_Select","Button_Right"};
            for(int i=0;i<3;i++)
            {
                string path=ButtonsFolder+"/"+names[i]+".prefab";var button=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    button.name=names[i];
                    var moving=button.transform.Find("Animated keycap");
                    if(!moving)
                    {
                        moving=new GameObject("Animated keycap").transform;moving.SetParent(button.transform,false);
                        var cap=button.transform.Find("Beveled keycap");cap.SetParent(moving,false);
                        Transform label=null;foreach(Transform child in button.transform)if(child.name.Contains("face print")){label=child;break;}
                        if(label)label.SetParent(moving,false);
                    }
                    if(i==1 && !moving.Find("SELECT • ivory face print"))
                    {
                        var old=moving.Find("FIRE • ivory face print");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
                        meshPrefix="Selection_";meshIndex=0;var print=new Builder();SelectPrint(print);
                        Save("SELECT • ivory face print",moving,new Vector3(0,-.00525f,-.1365f),print,AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Button ivory print.mat"));meshPrefix="Part_";
                    }
                    var collider=button.GetComponent<BoxCollider>();if(!collider)collider=button.AddComponent<BoxCollider>();
                    collider.center=new Vector3(0,0,-.113f);collider.size=new Vector3(.334f,.28f,.068f);
                    var control=button.GetComponent<AmmoSelectionButton>();if(!control)control=button.AddComponent<AmmoSelectionButton>();
                    control.command=(AmmoSelectionButton.Command)i;control.movingCap=moving;
                    PrefabUtility.SaveAsPrefabAsset(button,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(button);}
            }
            var machine=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var state=machine.GetComponent<AmmoSelectionConsole>();if(!state)state=machine.AddComponent<AmmoSelectionConsole>();
                var buttons=machine.GetComponentsInChildren<AmmoSelectionButton>();
                foreach(var button in buttons){button.console=state;if(button.command==AmmoSelectionButton.Command.Select)button.name="Button_Select";}
                var box=machine.GetComponent<BoxCollider>();if(box)UnityEngine.Object.DestroyImmediate(box);
                var body=machine.GetComponent<MeshCollider>();if(!body)body=machine.AddComponent<MeshCollider>();
                body.sharedMesh=machine.transform.Find("01 • Beveled ivory cabinet").GetComponent<MeshFilter>().sharedMesh;
                var face=machine.transform.Find("04 • Sloped front assembly");
                var screenRoot=face.Find("Ammo selection display");
                if(!screenRoot)
                {
                    screenRoot=new GameObject("Ammo selection display").transform;screenRoot.SetParent(face,false);
                    var cyan=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mach/FireRateConsole/Materials/Screen cyan.mat");
                    Text("Title",screenRoot,"AMMUNITION",new Vector3(0,1.15f,-.05f),.045f,cyan);
                    Text("Preview name",screenRoot,"JHP",new Vector3(0,.966f,-.05f),.155f,cyan);
                    Text("Ammo index",screenRoot,"01 / 05",new Vector3(0,.842f,-.05f),.032f,cyan);
                    Text("Selected status",screenRoot,"SELECTED / JHP",new Vector3(0,.645f,-.05f),.035f,cyan);
                    state.optionLabels=new TextMesh[5];
                    for(int i=0;i<5;i++)
                    {
                        Text("Option "+i,screenRoot,state.ammunition[i],new Vector3((i-2)*.202f,.723f,-.05f),.032f,cyan);
                        state.optionLabels[i]=screenRoot.Find("Option "+i).GetComponent<TextMesh>();
                    }
                }
                state.selectedText=screenRoot.Find("Preview name").GetComponent<TextMesh>();state.indexText=screenRoot.Find("Ammo index").GetComponent<TextMesh>();state.statusText=screenRoot.Find("Selected status").GetComponent<TextMesh>();
                state.statusText.transform.localPosition=new Vector3(0,.645f,-.05f);
                state.RefreshDisplay();PrefabUtility.SaveAsPrefabAsset(machine,PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(machine);}
            AssetDatabase.SaveAssets();VerifyAmmoSelection();
            TabletAuthoring.VerifyAndBuild();
        }
        public static void VerifyAmmoSelection()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var state=UnityEngine.Object.FindAnyObjectByType<AmmoSelectionConsole>();if(!state || state.ammunition.Length!=5)throw new Exception("Five ammo options not saved.");
            var buttons=state.GetComponentsInChildren<AmmoSelectionButton>();if(buttons.Length!=3)throw new Exception("Three buttons required.");
            var left=Array.Find(buttons,x=>x.command==AmmoSelectionButton.Command.Previous);var right=Array.Find(buttons,x=>x.command==AmmoSelectionButton.Command.Next);var select=Array.Find(buttons,x=>x.command==AmmoSelectionButton.Command.Select);
            left.Activate();if(state.PreviewIndex!=4 || state.SelectedIndex!=0)throw new Exception("Previous wrap/pending selection failed.");
            select.Activate();if(state.SelectedAmmo!="THUNDER")throw new Exception("Select failed.");
            for(int i=0;i<5;i++){right.Activate();if(state.PreviewIndex!=i || state.SelectedAmmo!="THUNDER")throw new Exception("Next cycle changed committed ammo.");}
            for(int f=0;f<3;f++)select.Animate(1f/60);if(select.movingCap.localPosition.z<.005f)throw new Exception("Button does not depress.");
            for(int f=0;f<60;f++)select.Animate(1f/60);if(select.movingCap.localPosition.z>.0001f)throw new Exception("Button does not return.");
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerPickup>();var camera=player.viewCamera;
            foreach(var button in buttons)
            {
                camera.transform.position=button.transform.position-button.transform.forward*.85f;camera.transform.LookAt(button.transform.position);
                Physics.SyncTransforms();if(player.FindAmmoButton()!=button)throw new Exception("Ammo button ray blocked by housing.");
                var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.position=camera.transform.position+camera.transform.forward*.4f;block.transform.localScale=Vector3.one*.2f;Physics.SyncTransforms();
                if(player.FindAmmoButton()!=null)throw new Exception("Interaction ignores obstruction.");UnityEngine.Object.DestroyImmediate(block);
                player.heldTablet.SetRaised(true);if(player.FindAmmoButton()!=null)throw new Exception("Tablet should block console interaction.");player.heldTablet.SnapToRest();
            }
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);state=UnityEngine.Object.FindAnyObjectByType<AmmoSelectionConsole>();
            RenderPreview(state.gameObject,"QA/AmmoSelectionConsole.png");
            Debug.Log("AMMO_SELECTION_VERIFIED: five types, previous/next wrap, pending versus selected, key travel/return, center ray, occlusion, tablet blocking, saved scene inheritance.");
        }
    }
}
