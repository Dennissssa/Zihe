using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Mach.Editor
{
    [InitializeOnLoad]
    public static class IslandAuthoring
    {
        const string Folder="Assets/Mach/Island";
        const string Request="Temp/Island.request";
        const float Sea=-3f;
        static IslandAuthoring(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Request))return;
            if(SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;
            File.Delete(Request);
            try{Apply();}catch(Exception e){File.WriteAllText("Logs/island-error.txt",e.ToString());Debug.LogException(e);}
        }
        static float Hill(float x,float z,float cx,float cz,float width,float height)
        {return height*Mathf.Exp(-((x-cx)*(x-cx)+(z-cz)*(z-cz))/(width*width));}
        public static float Height(float x,float z)
        {
            float dx=(x-5)/205f,dz=(z-70)/245f;
            float angle=Mathf.Atan2(dz,dx);
            float radius=Mathf.Sqrt(dx*dx+dz*dz)/(1+.075f*Mathf.Sin(angle*3+.8f)+.05f*Mathf.Sin(angle*5-1.1f));
            // A curved bay on the eastern shore; a broad, walkable interior with two ridges.
            radius+=.18f*Mathf.Exp(-((x-170)*(x-170)/4200f+(z-15)*(z-15)/5200f));
            float shore=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.16f,.85f,radius));
            float inland=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.91f,.64f,radius));
            float hills=Hill(x,z,-70,115,57,27)+Hill(x,z,55,200,67,35)+Hill(x,z,115,110,43,10);
            float detail=(Mathf.PerlinNoise(x*.022f+80,z*.022f+30)-.5f)*2.4f;
            float height=Mathf.Lerp(-15,1.5f,shore)+(hills+detail+2)*inland;
            // Blend to the existing prototype's ground without moving its models.
            float baseBlend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(30,62,new Vector2(x,z).magnitude));
            return Mathf.Lerp(-.035f,height,baseBlend);
        }
        static TerrainLayer Layer(string name,Color color,int seed)
        {
            string path=Folder+"/"+name;
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path+".asset");
            if(!tex){tex=new Texture2D(64,64,TextureFormat.RGB24,true);AssetDatabase.CreateAsset(tex,path+".asset");}
            var colors=new Color[64*64];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)colors[y*64+x]=color*Mathf.Lerp(.94f,1.06f,Mathf.PerlinNoise(x*.13f+seed,y*.13f+seed));
            tex.SetPixels(colors);tex.Apply();tex.wrapMode=TextureWrapMode.Repeat;EditorUtility.SetDirty(tex);
            var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path+".terrainlayer");
            if(!layer){layer=new TerrainLayer();AssetDatabase.CreateAsset(layer,path+".terrainlayer");}
            layer.diffuseTexture=tex;layer.tileSize=new Vector2(18,18);layer.metallic=0;layer.smoothness=.05f;EditorUtility.SetDirty(layer);return layer;
        }
        [MenuItem("Mach/Map/Shape Existing Terrain As Island")]
        public static void Apply()
        {
            var terrain=GameObject.Find("Terrain")?.GetComponent<Terrain>();
            if(!terrain||!terrain.terrainData)throw new Exception("Expected the existing Terrain");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var data=terrain.terrainData;
            if(data.size.x<600||data.size.z<600)throw new Exception("Terrain is smaller than the planned island; inspect its size before applying");
            Undo.RegisterCompleteObjectUndo(data,"Shape island terrain");Undo.RecordObject(terrain.transform,"Island sea floor depth");
            Vector3 origin=terrain.transform.position;origin.y=-20;terrain.transform.position=origin;
            data.heightmapResolution=513;data.size=new Vector3(data.size.x,100,data.size.z);
            var heights=new float[513,513];
            for(int y=0;y<513;y++)for(int x=0;x<513;x++)heights[y,x]=(Height(origin.x+x*data.size.x/512,origin.z+y*data.size.z/512)-origin.y)/data.size.y;
            data.SetHeights(0,0,heights);
            data.terrainLayers=new[]{Layer("Coastal sand",new Color(.68f,.60f,.40f),12),Layer("Island grass",new Color(.29f,.39f,.19f),39),Layer("Ridge stone",new Color(.43f,.45f,.42f),66)};
            data.alphamapResolution=256;var splat=new float[256,256,3];
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {
                float u=x/255f,v=y/255f,h=data.GetInterpolatedHeight(u,v)+origin.y,slope=data.GetSteepness(u,v);
                float grass=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.4f,3.3f,h));
                float rock=Mathf.Max(Mathf.SmoothStep(0,1,Mathf.InverseLerp(18,31,h)),Mathf.SmoothStep(0,1,Mathf.InverseLerp(23,38,slope)))*.82f;
                splat[y,x,0]=(1-grass)*(1-rock);splat[y,x,1]=grass*(1-rock);splat[y,x,2]=rock;
            }
            data.SetAlphamaps(0,0,splat);EditorUtility.SetDirty(data);
            var terrainMat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Island terrain.mat");
            if(!terrainMat){terrainMat=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));AssetDatabase.CreateAsset(terrainMat,Folder+"/Island terrain.mat");}
            terrain.materialTemplate=terrainMat;terrain.heightmapPixelError=3;terrain.drawInstanced=true;EditorUtility.SetDirty(terrain);
            // Keep the equipment apron; open the old test enclosure to the landscape.
            foreach(string wall in new[]{"Back Wall","Front Wall","Left Wall","Right Wall"})
            {var go=GameObject.Find(wall);if(go){Undo.RecordObject(go,"Open island access");go.SetActive(false);}}
            CreateSea(origin,data.size);
            terrain.Flush();Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);EditorSceneManager.SaveScene(terrain.gameObject.scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/island-applied.txt",DateTime.Now+"\nShaped existing "+AssetDatabase.GetAssetPath(data)+"; terrain size "+data.size+"; 513 heightmap; three terrain layers; sea -3m; original equipment unchanged.");
        }
        static void CreateSea(Vector3 origin,Vector3 size)
        {
            var go=GameObject.Find("Island sea");if(!go){go=new GameObject("Island sea",typeof(MeshFilter),typeof(MeshRenderer));Undo.RegisterCreatedObjectUndo(go,"Add sea surface");}
            const int n=180;var vertices=new Vector3[(n+1)*(n+1)];var colors=new Color[vertices.Length];var triangles=new int[n*n*6];
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)
            {
                int i=z*(n+1)+x;float wx=origin.x+size.x*x/n,wz=origin.z+size.z*z/n;
                vertices[i]=new Vector3(wx,Sea,wz);
                float depth=Sea-Height(wx,wz),shallow=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,9,depth));
                Color c=Color.Lerp(new Color(.045f,.23f,.32f),new Color(.22f,.59f,.55f),shallow);
                float foam=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.03f,.7f,depth));
                c=Color.Lerp(c,new Color(.66f,.81f,.71f),foam*.65f);
                colors[i]=c*Mathf.Lerp(.97f,1.03f,Mathf.PerlinNoise(wx*.08f,wz*.08f));
                if(x<n&&z<n){int t=(z*n+x)*6;triangles[t]=i;triangles[t+1]=i+n+1;triangles[t+2]=i+1;triangles[t+3]=i+1;triangles[t+4]=i+n+1;triangles[t+5]=i+n+2;}
            }
            string meshPath=Folder+"/Coastal water mesh.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,meshPath);}else mesh.Clear();
            mesh.name="Coastal water mesh";mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Sea.mat");if(!mat){mat=new Material(Shader.Find("Mach/Island Water"));AssetDatabase.CreateAsset(mat,Folder+"/Sea.mat");}
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=mat;go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            var horizon=GameObject.Find("Island distant sea");if(!horizon){horizon=GameObject.CreatePrimitive(PrimitiveType.Plane);horizon.name="Island distant sea";Undo.RegisterCreatedObjectUndo(horizon,"Add ocean horizon");UnityEngine.Object.DestroyImmediate(horizon.GetComponent<Collider>());}
            horizon.transform.position=new Vector3(0,Sea-.02f,0);horizon.transform.localScale=new Vector3(1200,1,1200);
            var far=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Distant sea.mat");if(!far){far=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(far,Folder+"/Distant sea.mat");}far.color=new Color(.045f,.23f,.32f);EditorUtility.SetDirty(far);
            horizon.GetComponent<Renderer>().sharedMaterial=far;horizon.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        public static void VerifyAndBuild()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var terrain=GameObject.Find("Terrain").GetComponent<Terrain>();
            if(terrain.terrainData.terrainLayers.Length!=3||!GameObject.Find("Island sea"))throw new Exception("Island assets missing");
            float ground=terrain.SampleHeight(Vector3.zero)+terrain.transform.position.y;
            if(Mathf.Abs(ground)>.15f)throw new Exception("Equipment ground moved: "+ground);
            foreach(string wall in new[]{"Back Wall","Front Wall","Left Wall","Right Wall"})if(GameObject.Find(wall))throw new Exception("Island access blocked by "+wall);
            Capture(new Vector3(385,290,-400),new Vector3(5,0,70),"QA/Island_Overview.png");
            Capture(new Vector3(12,6,-18),new Vector3(-30,10,130),"QA/Island_Ground.png");
            if(ShaderUtil.ShaderHasError(Shader.Find("Mach/Island Water")))throw new Exception("Island water shader failed compilation");
            Debug.Log("ISLAND_AUTHORED_VERIFIED");TabletAuthoring.VerifyAndBuild();
        }
        static void Capture(Vector3 position,Vector3 target,string path)
        {
            Directory.CreateDirectory("QA");var go=new GameObject("Island verification camera",typeof(Camera));var cam=go.GetComponent<Camera>();
            cam.transform.position=position;cam.transform.LookAt(target);cam.fieldOfView=55;cam.farClipPlane=8000;cam.allowHDR=true;
            var additional=cam.GetUniversalAdditionalCameraData();additional.renderPostProcessing=true;additional.volumeLayerMask=~0;
            var rt=new RenderTexture(1600,1000,24,RenderTextureFormat.DefaultHDR);cam.targetTexture=rt;
            var prev=RenderTexture.active;bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            RenderPipeline.SubmitRenderRequest(cam,new RenderPipeline.StandardRequest{destination=rt});RenderTexture.active=rt;
            var tex=new Texture2D(1600,1000,TextureFormat.RGBAFloat,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();
            var pixels=tex.GetPixels();for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;
            var png=new Texture2D(1600,1000,TextureFormat.RGB24,false);png.SetPixels(pixels);png.Apply();File.WriteAllBytes(path,png.EncodeToPNG());UnityEngine.Object.DestroyImmediate(png);
            ShaderUtil.allowAsyncCompilation=async;RenderTexture.active=prev;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
