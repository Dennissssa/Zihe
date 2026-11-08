using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mach.Editor
{
    public static partial class TabletAuthoring
    {
        [MenuItem("Mach/Remove Visible Tablet Arm")]
        public static void RemoveVisibleArm()
        {
            var player=PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                var tablet=player.GetComponent<HeldTablet>();
                if(!tablet || !tablet.poseRoot)throw new Exception("Authored tablet required.");
                var hand=tablet.poseRoot.Find("Left hand • supporting grip");
                if(hand)UnityEngine.Object.DestroyImmediate(hand.gameObject);
                tablet.forearmPivot=null;
                tablet.poseRoot.name="Tablet • animated pose";
                PrefabUtility.SaveAsPrefabAsset(player,PlayerPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(player);}
            AssetDatabase.SaveAssets();VerifyAndPreview();
            var saved=UnityEngine.Object.FindAnyObjectByType<HeldTablet>();
            if(saved.forearmPivot || saved.poseRoot.childCount!=1 || !saved.poseRoot.GetChild(0).name.Contains("Tablet"))
                throw new Exception("Unexpected visible objects remain in tablet pose.");
            Debug.Log("TABLET_ARM_REMOVED: only tablet remains in saved player prefab and existing scene instance.");
        }
        [MenuItem("Mach/Refine Natural Tablet Grip")]
        public static void RefineGrip()
        {
            var player=PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                var tablet=player.GetComponent<HeldTablet>();
                if(!tablet || !tablet.poseRoot)throw new Exception("Authored tablet required.");
                glove=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Work glove.mat");
                cloth=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Sleeve canvas.mat");
                rubber=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Grip rubber.mat");
                var hand=tablet.poseRoot.Find("Left hand • supporting grip");
                // Replace only the prior generated hand, not the tablet or scene.
                if(hand && !hand.Find("Rounded palm"))
                {
                    for(int i=hand.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(hand.GetChild(i).gameObject);
                    Sweep("Rounded palm",hand,new[]{new Vector3(-.287f,-.184f,.04f),new Vector3(-.287f,-.16f,.041f),new Vector3(-.285f,-.12f,.044f),new Vector3(-.287f,-.074f,.042f),new Vector3(-.29f,-.065f,.041f)},
                        new[]{new Vector2(.02f,.014f),new Vector2(.039f,.024f),new Vector2(.043f,.026f),new Vector2(.031f,.02f),new Vector2(.01f,.009f)},glove);
                    for(int f=0;f<4;f++)
                    {
                        float y=-.143f+f*.026f, r=f==0?.009f:.0105f;
                        Sweep("Curled glove finger "+(f+1),hand,new[]{new Vector3(-.294f,y,.05f),new Vector3(-.324f,y,.032f),new Vector3(-.331f,y,.002f),new Vector3(-.321f,y,-.027f),new Vector3(-.305f,y,-.030f)},
                            new[]{new Vector2(r*.8f,r*.8f),new Vector2(r,r),new Vector2(r,r),new Vector2(r*.88f,r*.88f),new Vector2(.004f,.004f)},glove);
                    }
                    Sweep("Curved gripping thumb",hand,new[]{new Vector3(-.279f,-.172f,.012f),new Vector3(-.268f,-.159f,-.025f),new Vector3(-.261f,-.13f,-.043f),new Vector3(-.264f,-.105f,-.044f)},
                        new[]{new Vector2(.015f,.017f),new Vector2(.016f,.015f),new Vector2(.014f,.013f),new Vector2(.006f,.006f)},glove);
                    Sweep("Soft wrist seal",hand,new[]{new Vector3(-.298f,-.227f,.053f),new Vector3(-.295f,-.218f,.048f),new Vector3(-.29f,-.185f,.044f),new Vector3(-.287f,-.18f,.044f)},
                        new[]{new Vector2(.035f,.031f),new Vector2(.04f,.034f),new Vector2(.036f,.027f),new Vector2(.03f,.024f)},rubber);
                    var pivot=new GameObject("Forearm • wrist articulation").transform;pivot.SetParent(hand,false);pivot.localPosition=new Vector3(-.294f,-.211f,.051f);
                    Sweep("Tapered cloth forearm",pivot,new[]{new Vector3(0,.014f,0),new Vector3(-.001f,0,.003f),new Vector3(-.017f,-.06f,.019f),new Vector3(-.043f,-.16f,.041f),new Vector3(-.074f,-.29f,.07f),new Vector3(-.081f,-.33f,.078f)},
                        new[]{new Vector2(.032f,.028f),new Vector2(.04f,.033f),new Vector2(.045f,.038f),new Vector2(.053f,.045f),new Vector2(.064f,.053f),new Vector2(.062f,.052f)},cloth);
                    Sweep("Rolled fabric cuff",pivot,new[]{new Vector3(0,.012f,0),new Vector3(-.001f,.006f,.002f),new Vector3(-.004f,-.009f,.007f),new Vector3(-.005f,-.015f,.008f)},
                        new[]{new Vector2(.037f,.032f),new Vector2(.043f,.037f),new Vector2(.043f,.037f),new Vector2(.038f,.031f)},cloth);
                }
                tablet.forearmPivot=hand.Find("Forearm • wrist articulation");
                tablet.transitionSeconds=.52f;tablet.secondaryMotion=.65f;
                tablet.liftOffset=new Vector3(.035f,.12f,.035f);tablet.gatherOffset=new Vector3(-.055f,-.075f,.045f);
                tablet.liftAngles=new Vector3(17,-14,-12);tablet.gatherAngles=new Vector3(-4,5,3);
                foreach(var t in hand.GetComponentsInChildren<Transform>())t.gameObject.layer=ViewLayer;
                foreach(var r in hand.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
                tablet.SnapToRest();PrefabUtility.SaveAsPrefabAsset(player,PlayerPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(player);}
            AssetDatabase.SaveAssets();VerifyAndPreview();
            var controller=UnityEngine.Object.FindAnyObjectByType<HeldTablet>();var main=controller.GetComponentInChildren<Camera>();
            controller.SnapToRest();controller.SetRaised(true);
            for(int i=0;i<9;i++)controller.AdvancePose(1f/60);
            var before=controller.poseRoot.localPosition;controller.SetRaised(false);
            if(Vector3.Distance(before,controller.poseRoot.localPosition)>.00001f)throw new Exception("Reversal teleported pose.");
            controller.AdvancePose(1f/60);
            if(Vector3.Distance(before,controller.poseRoot.localPosition)>.06f)throw new Exception("Reversal discontinuity.");
            Directory.CreateDirectory("QA/TabletMotion");controller.SnapToRest();
            main.aspect=16f/9f;controller.tabletCamera.aspect=16f/9f;
            for(int frame=0;frame<48;frame++)
            {
                controller.SetRaised(frame<24);
                controller.AdvancePose(1f/30);
                Preview(main,controller,frame<24,"QA/TabletMotion/"+frame.ToString("D3")+".png",960,540,false);
            }
            main.ResetAspect();controller.tabletCamera.ResetAspect();controller.SnapToRest();
            Debug.Log("NATURAL_GRIP_VERIFIED: rounded grip, articulated forearm, two control poses, continuous mid-lift reversal and animation frames.");
            VerifyAndBuild();
        }
        static void Sweep(string name,Transform parent,Vector3[] points,Vector2[] radii,Material mat)
        {
            var centers=new List<Vector3>();var widths=new List<Vector2>();
            for(int i=0;i<points.Length-1;i++)for(int step=0;step<5;step++)
            {
                float t=step/5f,t2=t*t,t3=t2*t;Vector3 a=points[Mathf.Max(0,i-1)],b=points[i],c=points[i+1],d=points[Mathf.Min(points.Length-1,i+2)];
                centers.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t2+(-a+3*b-3*c+d)*t3));
                widths.Add(Vector2.Lerp(radii[i],radii[i+1],Mathf.SmoothStep(0,1,t)));
            }
            centers.Add(points[points.Length-1]);widths.Add(radii[radii.Length-1]);
            var vertices=new List<Vector3>();var triangles=new List<int>();const int sides=16;
            for(int ring=0;ring<centers.Count;ring++)
            {
                var tangent=(centers[Mathf.Min(ring+1,centers.Count-1)]-centers[Mathf.Max(0,ring-1)]).normalized;
                var axis=Mathf.Abs(Vector3.Dot(tangent,Vector3.forward))>.9f?Vector3.up:Vector3.forward;
                var right=Vector3.Cross(tangent,axis).normalized;var up=Vector3.Cross(right,tangent).normalized;
                for(int k=0;k<sides;k++){float angle=k*2*Mathf.PI/sides;vertices.Add(centers[ring]+right*Mathf.Cos(angle)*widths[ring].x+up*Mathf.Sin(angle)*widths[ring].y);}
                if(ring>0)for(int k=0;k<sides;k++){int a=(ring-1)*sides+k,b=(ring-1)*sides+(k+1)%sides,c=ring*sides+(k+1)%sides,d=ring*sides+k;triangles.AddRange(new[]{a,c,b,a,d,c});}
            }
            int start=vertices.Count;vertices.Add(centers[0]);int end=vertices.Count;vertices.Add(centers[centers.Count-1]);
            for(int k=0;k<sides;k++){triangles.AddRange(new[]{start,(k+1)%sides,k});int n=(centers.Count-1)*sides;triangles.AddRange(new[]{end,n+k,n+(k+1)%sides});}
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            // Ensure outward normals independent of sweep direction.
            var v=mesh.vertices;var norms=mesh.normals;int mid=(centers.Count/2)*sides;
            if(Vector3.Dot(norms[mid],v[mid]-centers[centers.Count/2])<0){for(int i=0;i<triangles.Count;i+=3){int t=triangles[i];triangles[i]=triangles[i+1];triangles[i+1]=t;}mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();}
            string path=AssetDatabase.GenerateUniqueAssetPath(Folder+"/Meshes/Natural_"+name.Replace(" ","_")+".asset");AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;
        }
    }
}
