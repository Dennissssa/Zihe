using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mach.Editor
{
    public static partial class AmmoMachineAuthoring
    {
        const string ButtonsFolder = Folder + "/Buttons";

        [MenuItem("Mach/Add Fitted Machine Buttons")]
        public static void AddButtons()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            Directory.CreateDirectory(ButtonsFolder);
            AssetDatabase.Refresh();
            var gray = ButtonMaterial("Button graphite",new Color(.095f,.105f,.11f),.2f);
            var red = ButtonMaterial("Button vermilion",new Color(.78f,.045f,.016f),.08f);
            var seal = AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Rubber seals.mat");
            var white = AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Button ivory print.mat");
            if(!white)
            {
                white=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                white.SetColor("_BaseColor",new Color(.95f,.91f,.79f));
                AssetDatabase.CreateAsset(white,Folder+"/Materials/Button ivory print.mat");
            }
            string[] names={"Button_Left","Button_Select","Button_Right"};
            var machine=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var bays=machine.transform.Find("04 • Sloped front assembly/Three recessed ammo selectors");
                if(!bays || bays.childCount!=3)throw new Exception("Expected three existing button bays.");
                for(int i=0;i<3;i++)
                {
                    string path=ButtonsFolder+"/"+names[i]+".prefab";
                    var button=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if(!button)
                    {
                        meshPrefix=names[i]+"_";meshIndex=0;
                        var assembly=new GameObject(names[i]);
                        // Mounting origin matches the bay center. Local -Z is the press face.
                        Panel("Recessed plunger",assembly.transform,new Vector3(0,0,-.063f),.22f,.16f,.018f,.052f,seal);
                        Panel("Perimeter rubber seal",assembly.transform,new Vector3(0,0,-.097f),.332f,.278f,.027f,.01f,seal);
                        Panel("Beveled keycap",assembly.transform,new Vector3(0,0,-.119f),.326f,.272f,.028f,.034f,i==1?red:gray);
                        var symbol=new Builder();
                        if(i==1) SelectPrint(symbol);
                        else
                        {
                            float direction=i==0?-1:1;
                            Vector3 a=new Vector3(direction*.058f,0,0);
                            Vector3 b=new Vector3(-direction*.045f,.058f,0);
                            Vector3 c=new Vector3(-direction*.045f,-.058f,0);
                            if(i==0)symbol.Tri(a,b,c);else symbol.Tri(a,c,b);
                        }
                        Save(i==1?"SELECT • ivory face print":i==0?"Left arrow • ivory face print":"Right arrow • ivory face print",
                            assembly.transform,new Vector3(0,0,-.1365f),symbol,white);
                        button=PrefabUtility.SaveAsPrefabAsset(assembly,path);
                        UnityEngine.Object.DestroyImmediate(assembly);
                    }
                    var bay=bays.GetChild(i);
                    // The old decorative cartridge is superseded by the physical button.
                    for(int j=bay.childCount-1;j>=0;j--)
                        if(bay.GetChild(j).name.StartsWith("Cartridge icon"))UnityEngine.Object.DestroyImmediate(bay.GetChild(j).gameObject);
                    if(!bay.Find(names[i]))
                    {
                        var instance=(GameObject)PrefabUtility.InstantiatePrefab(button,bay);
                        instance.transform.localPosition=Vector3.zero;
                        instance.transform.localRotation=Quaternion.identity;
                    }
                    var key=bay.Find(names[i]+"/Beveled keycap");
                    if(!key)key=bay.Find(names[i]+"/Animated keycap/Beveled keycap");
                    if(!key)throw new Exception("Button cap missing.");
                    var bounds=key.GetComponent<MeshFilter>().sharedMesh.bounds;
                    if(bounds.size.x>=.354f || bounds.size.y>=.299f)throw new Exception("Button does not fit opening.");
                    if(key.localPosition.z+bounds.max.z>=-.093f)throw new Exception("Button intersects rim opening.");
                }
                PrefabUtility.SaveAsPrefabAsset(machine,PrefabPath);
            }
            finally {PrefabUtility.UnloadPrefabContents(machine);meshPrefix="Part_";}
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            Verify();
            var placed=GameObject.Find("AmmoSwitchMachine");
            if(!placed)throw new Exception("Existing machine scene instance missing.");
            for(int i=0;i<3;i++)
            {
                var bays=placed.transform.Find("04 • Sloped front assembly/Three recessed ammo selectors");
                if(!bays.GetChild(i).Find(names[i]))throw new Exception("Saved scene did not inherit button prefab.");
            }
            RenderPreview(placed);
            File.Copy("QA/AmmoSwitchMachine.png","QA/AmmoSwitchMachine_Buttons.png",true);
            Debug.Log("AMMO_BUTTONS_VERIFIED: three nested button prefabs, fitted cap dimensions/clearance, scene inheritance and render.");
        }

        static Material ButtonMaterial(string name,Color color,float metallic)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/"+name+".mat");
            return material?material:Mat(name,color,metallic,.3f);
        }
        static void PrintBar(Builder mesh,float x,float y,float w,float h)
        {
            mesh.Quad(new Vector3(x,y,0),new Vector3(x,y+h,0),new Vector3(x+w,y+h,0),new Vector3(x+w,y,0));
        }
        static void FirePrint(Builder mesh)
        {
            const float bottom=-.04f, height=.08f, stroke=.011f, width=.043f;
            float x=-.1085f;
            // F
            PrintBar(mesh,x,bottom,stroke,height);PrintBar(mesh,x+stroke,.029f,width-stroke,stroke);PrintBar(mesh,x+stroke,-.002f,.028f,stroke);
            // I
            x+=.058f;PrintBar(mesh,x,.029f,width,stroke);PrintBar(mesh,x,bottom,width,stroke);PrintBar(mesh,x+.016f,bottom+stroke,stroke,height-2*stroke);
            // R, with an open counter and diagonal leg.
            x+=.058f;PrintBar(mesh,x,bottom,stroke,height);PrintBar(mesh,x+stroke,.029f,.025f,stroke);
            PrintBar(mesh,x+.032f,.001f,stroke,.028f);PrintBar(mesh,x+stroke,-.01f,.032f,stroke);
            mesh.Quad(new Vector3(x+.015f,-.01f,0),new Vector3(x+.028f,-.01f,0),new Vector3(x+.048f,bottom,0),new Vector3(x+.034f,bottom,0));
            // E
            x+=.058f;PrintBar(mesh,x,bottom,stroke,height);PrintBar(mesh,x+stroke,.029f,width-stroke,stroke);
            PrintBar(mesh,x+stroke,-.0055f,.027f,stroke);PrintBar(mesh,x+stroke,bottom,width-stroke,stroke);
        }
    }
}
