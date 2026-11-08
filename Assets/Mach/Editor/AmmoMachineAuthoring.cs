using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mach.Editor
{
    // Editor-only authoring. All geometry is stored as ordinary editable assets.
    public static partial class AmmoMachineAuthoring
    {
        const string Folder = "Assets/Mach/AmmoMachine";
        const string PrefabPath = Folder + "/AmmoSwitchMachine.prefab";
        static Material ivory, metal, rubber, glass, well, brass, ink;
        static Transform root;
        static int meshIndex;
        static string meshPrefix = "Part_";
        static string meshFolder = Folder + "/Meshes";

        [MenuItem("Mach/Author Ammo Switch Machine")]
        public static void Author()
        {
            if (File.Exists(PrefabPath))
            {
                Debug.Log("Ammo machine already exists. Preserving the authored prefab and scene.");
                return;
            }
            Directory.CreateDirectory(Folder + "/Meshes");
            Directory.CreateDirectory(Folder + "/Materials");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            ivory = Mat("Warm ivory enamel", new Color(.72f, .69f, .61f), .25f, .29f);
            metal = Mat("Graphite steel", new Color(.105f, .115f, .12f), .65f, .3f);
            rubber = Mat("Rubber seals", new Color(.024f, .029f, .03f), .05f, .18f);
            glass = Mat("Smoked screen", new Color(.012f, .018f, .02f), 0f, .25f);
            glass.SetFloat("_SpecularHighlights",0);glass.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            well = Mat("Recess interior", new Color(.085f, .092f, .092f), .4f, .23f);
            brass = Mat("Warm etched markings", new Color(.63f, .52f, .32f), .5f, .25f);
            ink = Mat("Lettering", new Color(.47f, .53f, .51f), .15f, .3f);
            root = new GameObject("Ammo Switch Machine").transform;
            meshIndex = 0;

            // Side silhouette, clockwise as viewed from the right: low nose, sloping console, rear.
            Vector2[] profile = { new Vector2(.16f,-.51f), new Vector2(.47f,-.51f),
                new Vector2(1.68f,.217f), new Vector2(1.68f,.48f), new Vector2(.16f,.48f) };
            Hull("01 • Beveled ivory cabinet", profile, .79f, .028f, ivory);
            Box("02 • Floating base seal", root, new Vector3(0,.145f,0), new Vector3(1.46f,.08f,.9f), rubber);
            Hull("03 • Splayed graphite plinth", new [] { new Vector2(.025f,-.55f), new Vector2(.14f,-.47f),
                new Vector2(.14f,.45f), new Vector2(.025f,.53f) }, .8f,.035f,metal);

            var face = new GameObject("04 • Sloped front assembly").transform;
            face.SetParent(root, false);
            face.localPosition = new Vector3(0,.47f,-.51f);
            face.localRotation = Quaternion.Euler(31,0,0);
            // Negative local Z faces outwards; rings have a genuine open center.
            Panel("Display gasket", face, new Vector3(0,.89f,-.012f), 1.37f,.86f,.035f,.035f,rubber);
            Ring("Display bolted bezel", face, new Vector3(0,.89f,-.034f),1.36f,.85f,1.17f,.67f,.035f,.041f,metal);
            Panel("Dark display glass", face, new Vector3(0,.89f,-.039f),1.175f,.675f,.04f,.012f,glass);
            foreach (float x in new [] { -.625f,.625f })
                foreach (float y in new [] { .516f,1.263f }) Bolt(face,new Vector3(x,y,-.083f),.023f);
            var recesses = new GameObject("Three recessed ammo selectors").transform;
            recesses.SetParent(face,false);
            for (int i=0;i<3;i++)
            {
                float x = (i-1)*.47f;
                var slot = new GameObject("Slot " + (i+1) + " • recessed cartridge bay").transform;
                slot.SetParent(recesses,false);
                slot.localPosition = new Vector3(x,.25f,-.024f);
                // Raised rim and sloped inner walls enclose a visibly lower floor.
                Panel("Bay floor",slot,new Vector3(0,0,-.004f),.35f,.277f,.025f,.01f,well);
                Ring("Recess walls",slot,Vector3.zero,.408f,.352f,.284f,.227f,.024f,.075f,well);
                Ring("Bolted rim",slot,new Vector3(0,0,-.075f),.438f,.383f,.354f,.299f,.027f,.018f,metal);
                foreach(float bx in new [] { -.191f,.191f })
                    foreach(float by in new [] { -.163f,.163f }) Bolt(slot,new Vector3(bx,by,-.1f),.014f);
                // Flat engraved cartridge pictogram, separate from the cavity geometry.
                Box("Cartridge icon body",slot,new Vector3(0,-.008f,-.012f),new Vector3(.037f,.085f,.002f),brass);
                Box("Cartridge icon base",slot,new Vector3(0,-.052f,-.012f),new Vector3(.05f,.009f,.002f),brass);
                Panel("Cartridge icon tip",slot,new Vector3(0,.044f,-.012f),.034f,.026f,.012f,.002f,brass);
                Text("Bay index", face, "0"+(i+1),new Vector3(x,.018f,-.022f),.032f,ink);
            }
            Text("Machine label",face,"A M M O   /   S E L E C T",new Vector3(0,.44f,-.027f),.025f,metal);
            foreach(float x in new [] { -.734f,.734f })
                foreach(float y in new [] { .04f,.458f,1.34f }) Bolt(face,new Vector3(x,y,-.046f),.025f);

            var nose = new GameObject("05 • Front rotary selector").transform;
            nose.SetParent(root,false);
            nose.localPosition = new Vector3(0,.31f,-.532f);
            Panel("Lower fascia",nose,Vector3.zero,1.49f,.28f,.027f,.032f,ivory);
            Disc("Selector rubber socket",nose,new Vector3(0,0,-.038f),.136f,.032f,16,rubber);
            Disc("Selector outer ring",nose,new Vector3(0,0,-.062f),.119f,.031f,16,metal);
            Disc("Selector knob",nose,new Vector3(0,0,-.09f),.099f,.036f,12,metal);
            Box("Selector position mark",nose,new Vector3(0,.076f,-.111f),new Vector3(.012f,.022f,.003f),brass);
            foreach(float x in new [] { -.675f,.675f })
                foreach(float y in new [] { -.09f,.09f }) Bolt(nose,new Vector3(x,y,-.039f),.023f);

            foreach(float side in new [] {-1f,1f})
            {
                var plate = new GameObject(side < 0 ? "06 • Left armor plate" : "07 • Right armor plate").transform;
                plate.SetParent(root,false);
                plate.localPosition = new Vector3(side*.797f,.83f,.24f);
                plate.localRotation = Quaternion.Euler(0,side < 0 ? 90 : -90,0);
                Panel("Side reinforcement",plate,Vector3.zero,.36f,.88f,.055f,.025f,metal);
                foreach(float y in new [] { -.35f,.35f }) Bolt(plate,new Vector3(0,y,-.035f),.024f);
                for(int i=0;i<5;i++) Box("Vent " + (i+1),plate,new Vector3(0,(i-2)*.062f,-.018f),new Vector3(.215f,.019f,.006f),rubber);
            }
            Box("08 • Rear service panel",root,new Vector3(0,.86f,.492f),new Vector3(1.3f,1.23f,.025f),metal);
            Box("Top protective rail",root,new Vector3(0,1.687f,.36f),new Vector3(1.43f,.025f,.18f),metal);
            var collider = root.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0,.85f,0);
            collider.size = new Vector3(1.64f,1.7f,1.12f);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject,PrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            var placed = (GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            placed.transform.position = new Vector3(-2.65f,0,-.1f);
            placed.transform.rotation = Quaternion.Euler(0,18,0);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Verify();
            RenderPreview(placed);
            Debug.Log("AMMO_MACHINE_AUTHORED: prefab, saved meshes/materials, scene instance and preview.");
        }

        static Material Mat(string name, Color color, float metallic,float smoothness)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name=name; m.color=color; m.SetFloat("_Metallic",metallic); m.SetFloat("_Smoothness",smoothness);
            AssetDatabase.CreateAsset(m,Folder+"/Materials/"+name+".mat"); return m;
        }
        static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent,false); go.transform.localPosition=position;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=mat;return go;
        }
        static Vector2[] Outline(float w,float h,float c)
        {
            return new [] {new Vector2(-w/2+c,-h/2),new Vector2(w/2-c,-h/2),new Vector2(w/2,-h/2+c),
                new Vector2(w/2,h/2-c),new Vector2(w/2-c,h/2),new Vector2(-w/2+c,h/2),
                new Vector2(-w/2,h/2-c),new Vector2(-w/2,-h/2+c)};
        }
        static void Panel(string name,Transform parent,Vector3 p,float w,float h,float c,float depth,Material m)
        {
            var a=Outline(w,h,c);var b=Outline(w-.018f,h-.018f,Mathf.Max(.002f,c-.005f));
            var mesh=new Builder();
            for(int i=0;i<a.Length;i++) {int j=(i+1)%a.Length;
                mesh.Quad(V(b[i],-depth/2),V(b[j],-depth/2),V(a[j],depth/2),V(a[i],depth/2));
                mesh.Tri(Vector3.back*depth/2,V(b[j],-depth/2),V(b[i],-depth/2));
                mesh.Tri(Vector3.forward*depth/2,V(a[i],depth/2),V(a[j],depth/2)); }
            Save(name,parent,p,mesh,m);
        }
        static void Ring(string name,Transform parent,Vector3 p,float w,float h,float iw,float ih,float c,float d,Material m)
        {
            var a=Outline(w,h,c);var inner=Outline(iw,ih,c*.75f);var outerTop=Outline(w-.013f,h-.013f,c);
            var mesh=new Builder();
            for(int i=0;i<8;i++){int j=(i+1)%8;
                mesh.Quad(V(outerTop[i],-d),V(inner[i],-d),V(inner[j],-d),V(outerTop[j],-d));
                mesh.Quad(V(a[i],0),V(outerTop[i],-d),V(outerTop[j],-d),V(a[j],0));
                mesh.Quad(V(inner[i],-d),V(inner[i]*.8f,0),V(inner[j]*.8f,0),V(inner[j],-d)); }
            Save(name,parent,p,mesh,m);
        }
        static Vector3 V(Vector2 p,float z) => new Vector3(p.x,p.y,z);
        static void Disc(string name,Transform parent,Vector3 p,float r,float d,int n,Material m)
        {
            var mesh=new Builder();
            for(int i=0;i<n;i++){
                float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;
                var u=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;var v=new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r;
                mesh.Quad(V(u*.9f,-d/2),V(v*.9f,-d/2),V(v,d/2),V(u,d/2));
                mesh.Tri(new Vector3(0,0,-d/2),V(v*.9f,-d/2),V(u*.9f,-d/2)); }
            Save(name,parent,p,mesh,m);
        }
        static void Bolt(Transform parent,Vector3 p,float r) => Disc("Hex fastener",parent,p,r,.012f,6,metal);
        static void Hull(string name,Vector2[] profile,float halfWidth,float bevel,Material mat)
        {
            var mesh=new Builder(); Vector2 center=Vector2.zero;foreach(var v in profile)center+=v;center/=profile.Length;
            foreach(float sign in new [] {-1f,1f})for(int i=0;i<profile.Length;i++){
                int j=(i+1)%profile.Length;
                var a=profile[i];var b=profile[j];var ai=Vector2.MoveTowards(a,center,bevel);var bi=Vector2.MoveTowards(b,center,bevel);
                Vector3 A=new Vector3(sign*(halfWidth-bevel),a.x,a.y),B=new Vector3(sign*(halfWidth-bevel),b.x,b.y);
                Vector3 C=new Vector3(sign*halfWidth,bi.x,bi.y),D=new Vector3(sign*halfWidth,ai.x,ai.y);
                if(sign>0){mesh.Quad(A,B,C,D);mesh.Tri(new Vector3(sign*halfWidth,center.x,center.y),D,C);}
                else {mesh.Quad(D,C,B,A);mesh.Tri(new Vector3(sign*halfWidth,center.x,center.y),C,D);}
                if(sign>0)mesh.Quad(new Vector3(-halfWidth+bevel,a.x,a.y),new Vector3(-halfWidth+bevel,b.x,b.y),B,A);
            }
            Save(name,root,Vector3.zero,mesh,mat);
        }
        static void Text(string name,Transform parent,string content,Vector3 p,float size,Material mat)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=p;
            go.transform.localRotation=Quaternion.identity;
            var text=go.AddComponent<TextMesh>();text.font=ProjectTypography.Font;if(text.font)text.GetComponent<Renderer>().sharedMaterial=text.font.material;text.text=content;text.characterSize=size*.125f;text.fontSize=64;
            text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=mat.color;
        }
        static void Save(string name,Transform parent,Vector3 p,Builder data,Material m)
        {
            var mesh=new Mesh {name=name};mesh.SetVertices(data.vertices);mesh.SetTriangles(data.triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,meshFolder+"/"+meshPrefix+(meshIndex++).ToString("D3")+".asset");
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=p;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=m;
        }
        class Builder
        {
            public readonly List<Vector3> vertices=new List<Vector3>();public readonly List<int> triangles=new List<int>();
            public void Tri(Vector3 a,Vector3 b,Vector3 c){int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);}
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){Tri(a,b,c);Tri(a,c,d);}
        }
        public static void Verify()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if(!prefab || !prefab.GetComponent<Collider>())throw new Exception("Machine prefab/collider missing");
            foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>())if(!filter.sharedMesh)throw new Exception("Unsaved mesh");
            foreach(var renderer in prefab.GetComponentsInChildren<Renderer>())if(!renderer.sharedMaterial)throw new Exception("Missing material");
            if(UnityEngine.Object.FindObjectsByType<PickupItem>().Length!=3)throw new Exception("Existing pickup objects changed");
            Debug.Log("AMMO_MACHINE_VERIFIED: saved mesh references, materials, collider, original pickup objects.");
        }
        public static void PolishAndPreview()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var screenMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Smoked screen.mat");
            screenMaterial.color=new Color(.012f,.018f,.02f);screenMaterial.SetFloat("_Metallic",0);
            screenMaterial.SetFloat("_Smoothness",.25f);screenMaterial.SetFloat("_SpecularHighlights",0);
            screenMaterial.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");EditorUtility.SetDirty(screenMaterial);
            var prefab=PrefabUtility.LoadPrefabContents(PrefabPath);
            foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>())
            {
                string n=filter.name;
                bool disc=n=="Hex fastener" || n.StartsWith("Selector ") && n!="Selector position mark";
                bool panel=n=="Display gasket" || n=="Dark display glass" || n=="Bay floor" || n=="Cartridge icon tip" || n=="Lower fascia" || n=="Side reinforcement";
                if(!disc && !panel)continue;
                var mesh=filter.sharedMesh;var v=mesh.vertices;var t=mesh.triangles;int stride=disc ? 9 : 12;
                for(int i=0;i<t.Length;i+=stride)for(int j=i;j<i+6;j+=3)
                {
                    Vector3 normal=Vector3.Cross(v[t[j+1]]-v[t[j]],v[t[j+2]]-v[t[j]]);
                    Vector3 center=(v[t[j]]+v[t[j+1]]+v[t[j+2]])/3;center.z=0;
                    if(Vector3.Dot(normal,center)<0){int swap=t[j+1];t[j+1]=t[j+2];t[j+2]=swap;}
                }
                mesh.triangles=t;mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);
            }
            foreach(var text in prefab.GetComponentsInChildren<TextMesh>())
            {
                text.transform.localRotation=Quaternion.identity;
                text.characterSize=(text.name=="Machine label" ? .025f : .032f)*.125f;
            }
            PrefabUtility.SaveAsPrefabAsset(prefab,PrefabPath);PrefabUtility.UnloadPrefabContents(prefab);
            AssetDatabase.SaveAssets();
            var scene=EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            Verify();
            var placed=GameObject.Find("AmmoSwitchMachine");
            if(!placed)throw new Exception("Scene instance missing");
            RenderPreview(placed);
            Debug.Log("AMMO_MACHINE_POLISHED_AND_VERIFIED");
        }
        static void RenderPreview(GameObject placed,string output="QA/AmmoSwitchMachine.png")
        {
            var originalPosition=placed.transform.position;var originalRotation=placed.transform.rotation;
            placed.transform.position=new Vector3(0,30,0);placed.transform.rotation=Quaternion.identity;
            var cameraGo=new GameObject("Temporary asset preview camera",typeof(Camera));var camera=cameraGo.GetComponent<Camera>();
            camera.transform.position=new Vector3(-2.65f,32.05f,-3.5f);camera.transform.LookAt(new Vector3(0,30.86f,0));
            camera.orthographic=true;camera.orthographicSize=1.18f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.81f,.8f,.76f);
            var sceneLights=UnityEngine.Object.FindObjectsByType<Light>();
            var enabledLights=new List<Light>();foreach(var light in sceneLights)if(light.enabled){enabledLights.Add(light);light.enabled=false;}
            var key=new GameObject("Temporary preview key",typeof(Light));
            key.transform.rotation=Quaternion.Euler(40,-35,0);key.GetComponent<Light>().type=LightType.Directional;key.GetComponent<Light>().intensity=1.6f;
            var fill=new GameObject("Temporary preview fill",typeof(Light));
            fill.transform.rotation=Quaternion.Euler(20,135,0);fill.GetComponent<Light>().type=LightType.Directional;fill.GetComponent<Light>().intensity=.65f;
            bool asyncCompilation=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            var rt=new RenderTexture(1200,1200,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();camera.Render();
            RenderTexture.active=rt;var tex=new Texture2D(1200,1200,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1200,1200),0,0);tex.Apply();
            Directory.CreateDirectory("QA");File.WriteAllBytes(output,tex.EncodeToPNG());
            RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraGo);
            ShaderUtil.allowAsyncCompilation=asyncCompilation;
            UnityEngine.Object.DestroyImmediate(key);UnityEngine.Object.DestroyImmediate(fill);
            foreach(var light in enabledLights)light.enabled=true;
            placed.transform.SetPositionAndRotation(originalPosition,originalRotation);
        }
    }
}
