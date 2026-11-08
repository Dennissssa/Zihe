using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Mach.Editor
{
    [InitializeOnLoad]
    public static class CircuitPolishAuthoring
    {
        const string Request="Temp/CircuitPolish.request";
        static CircuitPolishAuthoring(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Request))return;
            if(SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;File.Delete(Request);
            try{Apply();}catch(Exception e){File.WriteAllText("Logs/circuit-polish-error.txt",e.ToString());Debug.LogException(e);}
        }
        public static void ApplyBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Apply();}
        public static void Apply()
        {
            const string path="Assets/Mach/Circuit/Rounded block.png";
            var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                float dx=Mathf.Max(0,Mathf.Abs(x-31.5f)-23.5f),dy=Mathf.Max(0,Mathf.Abs(y-31.5f)-23.5f);
                tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(8-Mathf.Sqrt(dx*dx+dy*dy))));
            }
            tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spriteBorder=new Vector4(10,10,10,10);importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(!sprite)throw new Exception("Rounded sprite import failed");var board=UnityEngine.Object.FindAnyObjectByType<CircuitBoardView>();
            foreach(var slot in board.slots){Undo.RecordObject(slot.background,"Small rounded node corners");slot.background.sprite=sprite;slot.background.type=Image.Type.Sliced;slot.background.pixelsPerUnitMultiplier=1.3f;EditorUtility.SetDirty(slot.background);PrefabUtility.RecordPrefabInstancePropertyModifications(slot.background);}
            board.wires.transform.SetAsFirstSibling();board.wires.RefreshRoutes();
            EditorSceneManager.MarkSceneDirty(board.gameObject.scene);EditorSceneManager.SaveScene(board.gameObject.scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/circuit-polish-applied.txt",DateTime.Now+"\n32 existing node backgrounds now mildly rounded; wire layer remains behind nodes. No tablet model changes.");
        }
        public static void VerifyAndBuild()
        {
            VerifyEdgeCases();
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            var board=UnityEngine.Object.FindAnyObjectByType<CircuitBoardView>();foreach(var s in board.slots)if(!s.background.sprite||s.background.type!=Image.Type.Sliced)throw new Exception("Rounded node not saved");
            var obstacle=new Rect(-60,-60,120,120);var blocks=new List<Rect>{obstacle};var paths=new List<List<Vector2>>();
            var first=CircuitWireRouter.Route(new Vector2(-180,0),new Vector2(180,0),blocks,paths);if(first==null)throw new Exception("No detour");
            foreach(var p in first)if(obstacle.Contains(p))throw new Exception("Route crosses block");paths.Add(first);
            var second=CircuitWireRouter.Route(new Vector2(-180,12),new Vector2(180,12),blocks,paths);if(second==null)throw new Exception("No second lane");
            for(int i=1;i<first.Count;i++)for(int j=1;j<second.Count;j++)if((first[i]==second[j]&&first[i-1]==second[j-1])||(first[i]==second[j-1]&&first[i-1]==second[j]))throw new Exception("Distinct wires share lane");
            var moved=new Rect(-60,-120,120,120);var shifted=CircuitWireRouter.Route(new Vector2(-180,0),new Vector2(180,0),new List<Rect>{moved},new List<List<Vector2>>());
            if(shifted==null)throw new Exception("Moving obstacle failed reroute");foreach(var p in shifted)if(moved.Contains(p))throw new Exception("Moved obstacle intersection");
            var prog=board.program;foreach(var n in prog.nodes)n.active=false;prog.nodes[0]=new CircuitNode{active=true,kind=CircuitKind.Start,position=new Vector2(-460,205)};
            for(int i=1;i<32;i++){var pos=prog.FindFreeNodePosition();foreach(var n in prog.nodes)if(n.active&&Mathf.Abs(n.position.x-pos.x)<240&&Mathf.Abs(n.position.y-pos.y)<120)throw new Exception("Spawn overlap");prog.nodes[i]=new CircuitNode{active=true,position=pos};}
            foreach(var n in prog.nodes)n.active=false;
            prog.nodes[0]=new CircuitNode{active=true,kind=CircuitKind.Start,position=new Vector2(-420,165),next=2};
            prog.nodes[1]=new CircuitNode{active=true,kind=CircuitKind.Wait,value="1",position=new Vector2(-120,165)};
            prog.nodes[2]=new CircuitNode{active=true,kind=CircuitKind.Facing,position=new Vector2(240,165),next=3,pointInput=4};
            prog.nodes[3]=new CircuitNode{active=true,kind=CircuitKind.Fire,position=new Vector2(240,-75),ammoInput=5,rateInput=6};
            prog.nodes[4]=new CircuitNode{active=true,kind=CircuitKind.Point,value="A",position=new Vector2(-420,-75)};
            prog.nodes[5]=new CircuitNode{active=true,kind=CircuitKind.Ammo,value="AP",position=new Vector2(-420,-255)};
            prog.nodes[6]=new CircuitNode{active=true,kind=CircuitKind.Rate,value="1",position=new Vector2(-120,-255)};
            for(var t=board.canvas.transform;t;t=t.parent)t.gameObject.SetActive(true);
            board.Refresh();Canvas.ForceUpdateCanvases();board.wires.RefreshRoutes();if(board.wires.UnroutableCount!=0)throw new Exception("Sample board failed routing");
            Capture(board);
            // Restore the saved scene after isolated layout tests, then build without a sample graph.
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Debug.Log("CIRCUIT_POLISH_VERIFIED: rounded slots, obstacle detours, distinct lanes, 32 separated spawns");TabletAuthoring.VerifyAndBuild();
        }
        static void VerifyEdgeCases()
        {
            var cases=new[]{
                new List<Rect>{new Rect(-60,-60,120,120)},
                new List<Rect>{new Rect(-60,3,120,120),new Rect(-60,-123,120,120)},
                new List<Rect>{new Rect(-190,-20,60,40),new Rect(-60,-60,120,120)},
                new List<Rect>{new Rect(-60,-60,120,120),new Rect(100000,100000,160,68)}
            };
            for(int c=0;c<cases.Length;c++)
            {
                var path=CircuitWireRouter.EdgeRoute(new Vector2(-180,0),new Vector2(180,0),cases[c]);
                if(path.Count<2||path[0]!=new Vector2(-180,0)||path[path.Count-1]!=new Vector2(180,0))throw new Exception("Missing fallback wire "+c);
                for(int i=1;i<path.Count;i++)
                {
                    if(path[i].x!=path[i-1].x&&path[i].y!=path[i-1].y)throw new Exception("Non orthogonal fallback");
                    if(c!=2)for(int j=1;j<100;j++)foreach(var rect in cases[c])if(rect.Contains(Vector2.Lerp(path[i-1],path[i],j/100f)))throw new Exception("Edge fallback crosses body "+c);
                }
            }
            Debug.Log("EDGE_ROUTING_VERIFIED: blocked route, narrow corridor, enclosed endpoint, distant node");
        }
        static void Capture(CircuitBoardView board)
        {
            var go=new GameObject("Circuit preview camera",typeof(Camera));var cam=go.GetComponent<Camera>();
            cam.transform.position=board.surface.position-board.surface.forward*2;cam.transform.rotation=Quaternion.LookRotation(board.surface.forward,board.surface.up);
            cam.orthographic=true;cam.orthographicSize=board.surface.rect.height*Mathf.Abs(board.surface.lossyScale.y)*.65f;cam.nearClipPlane=.01f;cam.farClipPlane=10;cam.cullingMask=1<<30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.045f,.06f);
            var rt=new RenderTexture(1600,1000,24);cam.targetTexture=rt;Canvas.ForceUpdateCanvases();
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(cam,new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=rt});
            var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();Directory.CreateDirectory("QA");File.WriteAllBytes("QA/Circuit_Routing.png",tex.EncodeToPNG());
            RenderTexture.active=old;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
        }
    }
}

