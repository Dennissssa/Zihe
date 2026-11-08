using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mach.Editor
{
    public static partial class AmmoMachineAuthoring
    {
        const string RateFolder="Assets/Mach/FireRateConsole";
        const string RatePrefab=RateFolder+"/FireRateConsole.prefab";
        [MenuItem("Mach/Author Fire Rate Console")]
        public static void AuthorRateConsole()
        {
            var scene=EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            Directory.CreateDirectory(RateFolder+"/Meshes");Directory.CreateDirectory(RateFolder+"/Materials");AssetDatabase.Refresh();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(RatePrefab);
            if(!prefab)
            {
                ivory=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Warm ivory enamel.mat");
                metal=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Graphite steel.mat");
                rubber=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Rubber seals.mat");
                glass=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Smoked screen.mat");
                well=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Recess interior.mat");
                brass=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Warm etched markings.mat");
                var marking=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Button ivory print.mat");
                var green=new Material(Shader.Find("Universal Render Pipeline/Lit"));green.color=new Color(.13f,.3f,.23f);green.SetFloat("_Smoothness",.3f);
                AssetDatabase.CreateAsset(green,RateFolder+"/Materials/Confirm sage.mat");
                var cyan=new Material(Shader.Find("Universal Render Pipeline/Unlit"));cyan.SetColor("_BaseColor",new Color(.27f,.69f,.76f));
                AssetDatabase.CreateAsset(cyan,RateFolder+"/Materials/Screen cyan.mat");
                root=new GameObject("FireRateConsole").transform;meshFolder=RateFolder+"/Meshes";meshPrefix="Rate_";meshIndex=0;
                try
                {
                    Hull("01 • Ivory upright cabinet",new[]{new Vector2(.13f,-.36f),new Vector2(.4f,-.36f),new Vector2(1.8f,-.011f),new Vector2(1.8f,.30f),new Vector2(.13f,.30f)},.43f,.024f,ivory);
                    Hull("02 • Graphite base",new[]{new Vector2(.025f,-.40f),new Vector2(.14f,-.34f),new Vector2(.14f,.28f),new Vector2(.025f,.35f)},.47f,.027f,metal);
                    Box("Base rubber separator",root,new Vector3(0,.15f,0),new Vector3(.81f,.04f,.59f),rubber);
                    Box("Top protective plate",root,new Vector3(0,1.808f,.139f),new Vector3(.72f,.027f,.27f),metal);
                    var face=new GameObject("03 • Inclined controls").transform;face.SetParent(root,false);face.localPosition=new Vector3(0,.4f,-.36f);face.localRotation=Quaternion.Euler(14,0,0);
                    Panel("Display seal",face,new Vector3(0,.98f,-.018f),.674f,.88f,.029f,.027f,rubber);
                    Ring("Portrait screen bezel",face,new Vector3(0,.98f,-.031f),.658f,.862f,.495f,.682f,.027f,.035f,metal);
                    Panel("Portrait display glass",face,new Vector3(0,.98f,-.04f),.502f,.69f,.018f,.008f,glass);
                    foreach(float x in new[]{-.294f,.294f})foreach(float y in new[]{.598f,1.362f})Bolt(face,new Vector3(x,y,-.074f),.02f);
                    foreach(float x in new[]{-.374f,.374f})foreach(float y in new[]{.045f,.515f,1.39f})Bolt(face,new Vector3(x,y,-.036f),.02f);
                    var state=root.gameObject.AddComponent<FireRateConsole>();
                    Text("Display title",face,"FIRE RATE",new Vector3(0,1.213f,-.05f),.06f,cyan);
                    Text("Selected rate",face,"LOW",new Vector3(0,1.025f,-.05f),.16f,cyan);
                    Text("Preset index",face,"LOW   /   MED   /   HIGH",new Vector3(0,.86f,-.05f),.027f,cyan);
                    Text("Confirm status",face,"CONFIRMED",new Vector3(0,.752f,-.05f),.025f,cyan);
                    state.selectedText=face.Find("Selected rate").GetComponent<TextMesh>();state.statusText=face.Find("Confirm status").GetComponent<TextMesh>();
                    for(int i=0;i<2;i++)
                    {
                        var button=new GameObject(i==0?"04 • Fire rate button":"05 • Confirm button").transform;button.SetParent(face,false);button.localPosition=new Vector3(i==0?-.195f:.195f,.275f,-.018f);
                        Panel("Socket gasket",button,Vector3.zero,.314f,.305f,.028f,.018f,rubber);
                        Ring("Bolted button surround",button,new Vector3(0,0,-.019f),.304f,.293f,.241f,.23f,.025f,.048f,metal);
                        var moving=new GameObject("Moving keycap").transform;moving.SetParent(button,false);moving.localPosition=new Vector3(0,0,-.081f);
                        Panel("Raised keycap",moving,Vector3.zero,.224f,.21f,.022f,.034f,i==0?metal:green);
                        var icon=new Builder();
                        if(i==0)
                        {
                            for(int b=0;b<3;b++){float x=-.061f+b*.043f,h=.037f+b*.028f;icon.Quad(new Vector3(x,-.05f,0),new Vector3(x,-.05f+h,0),new Vector3(x+.024f,-.05f+h,0),new Vector3(x+.024f,-.05f,0));}
                        }
                        else
                        {
                            icon.Quad(new Vector3(-.073f,-.002f,0),new Vector3(-.055f,.018f,0),new Vector3(-.014f,-.022f,0),new Vector3(-.033f,-.045f,0));
                            icon.Quad(new Vector3(-.033f,-.045f,0),new Vector3(-.014f,-.022f,0),new Vector3(.054f,.064f,0),new Vector3(.074f,.047f,0));
                        }
                        Save("Button symbol",moving,new Vector3(0,0,-.018f),icon,marking);
                        foreach(float x in new[]{-.128f,.128f})foreach(float y in new[]{-.124f,.124f})Bolt(button,new Vector3(x,y,-.077f),.012f);
                        Text("Button label",face,i==0?"FIRE RATE":"CONFIRM",new Vector3(i==0?-.195f:.195f,.066f,-.025f),.046f,metal);
                        var collider=button.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,0,-.067f);collider.size=new Vector3(.27f,.26f,.07f);
                        var control=button.gameObject.AddComponent<FireRateButton>();control.console=state;control.isConfirm=i==1;control.keycap=moving;control.releasedPosition=moving.localPosition;
                    }
                    var nose=new GameObject("06 • Rotary trim control").transform;nose.SetParent(root,false);nose.localPosition=new Vector3(0,.268f,-.383f);
                    Panel("Front lower fascia",nose,Vector3.zero,.798f,.24f,.022f,.024f,ivory);
                    Disc("Rotary rubber socket",nose,new Vector3(0,0,-.025f),.112f,.02f,16,rubber);
                    Disc("Rotary collar",nose,new Vector3(0,0,-.047f),.102f,.026f,16,metal);
                    Disc("Rotary knob",nose,new Vector3(0,0,-.074f),.086f,.036f,12,metal);
                    Box("Gold index mark",nose,new Vector3(0,.067f,-.094f),new Vector3(.009f,.023f,.002f),brass);
                    foreach(float x in new[]{-.349f,.349f})foreach(float y in new[]{-.082f,.082f})Bolt(nose,new Vector3(x,y,-.028f),.019f);
                    foreach(float side in new[]{-1f,1f})
                    {
                        var plate=new GameObject(side<0?"07 • Left service armor":"08 • Right service armor").transform;plate.SetParent(root,false);plate.localPosition=new Vector3(side*.438f,.84f,.03f);plate.localRotation=Quaternion.Euler(0,side<0?90:-90,0);
                        Panel("Side plate",plate,Vector3.zero,.29f,1.02f,.045f,.02f,metal);
                        foreach(float y in new[]{-.434f,.434f})Bolt(plate,new Vector3(0,y,-.027f),.019f);
                        for(int v=0;v<6;v++)Box("Side vent "+v,plate,new Vector3(0,(v-2.5f)*.057f,-.013f),new Vector3(.184f,.015f,.006f),rubber);
                    }
                    Box("Rear maintenance cover",root,new Vector3(0,.94f,.315f),new Vector3(.65f,1.43f,.027f),metal);
                    var body=root.gameObject.AddComponent<MeshCollider>();body.sharedMesh=root.Find("01 • Ivory upright cabinet").GetComponent<MeshFilter>().sharedMesh;
                    prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,RatePrefab);UnityEngine.Object.DestroyImmediate(root.gameObject);
                }
                finally{meshFolder=Folder+"/Meshes";meshPrefix="Part_";}
            }
            if(!UnityEngine.Object.FindAnyObjectByType<FireRateConsole>())
            {
                var placed=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);placed.transform.position=new Vector3(2.35f,0,-.15f);placed.transform.rotation=Quaternion.Euler(0,-18,0);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();VerifyRateConsole();
        }
        public static void VerifyRateConsole()
        {
            var scene=EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var console=UnityEngine.Object.FindAnyObjectByType<FireRateConsole>();if(!console)throw new Exception("Console not saved in existing scene.");
            var buttons=console.GetComponentsInChildren<FireRateButton>();if(buttons.Length!=2)throw new Exception("Two buttons required.");
            var change=Array.Find(buttons,b=>!b.isConfirm);var confirm=Array.Find(buttons,b=>b.isConfirm);
            change.Activate();if(console.SelectedIndex!=1 || console.ConfirmedIndex!=0 || !console.HasPendingChange)throw new Exception("Cycle must remain pending.");
            confirm.Activate();if(console.ConfirmedIndex!=1 || console.HasPendingChange)throw new Exception("Confirm failed.");
            change.Activate();change.Activate();if(console.SelectedIndex!=0 || console.ConfirmedIndex!=1)throw new Exception("Cycle wrap failed.");
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerPickup>();var camera=player.viewCamera;
            foreach(var button in buttons)
            {
                camera.transform.position=button.transform.position-button.transform.forward*.85f;camera.transform.LookAt(button.transform.position);
                Physics.SyncTransforms();if(player.FindRateButton()!=button)throw new Exception("Center ray cannot reach "+button.name);
                var obstruction=GameObject.CreatePrimitive(PrimitiveType.Cube);obstruction.transform.position=camera.transform.position+camera.transform.forward*.4f;obstruction.transform.localScale=Vector3.one*.18f;
                Physics.SyncTransforms();if(player.FindRateButton()!=null)throw new Exception("Interaction passed through obstruction.");UnityEngine.Object.DestroyImmediate(obstruction);
            }
            scene=EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            Verify();var tablet=UnityEngine.Object.FindAnyObjectByType<HeldTablet>();if(tablet && tablet.poseRoot.childCount!=1)throw new Exception("Removed arm returned.");
            console=UnityEngine.Object.FindAnyObjectByType<FireRateConsole>();RenderPreview(console.gameObject,"QA/FireRateConsole.png");
            Debug.Log("FIRE_RATE_VERIFIED: existing scene preserved, two buttons, pending/confirmed modes, wrap, center-ray interaction and occlusion, tablet remains armless.");
        }
    }
}
