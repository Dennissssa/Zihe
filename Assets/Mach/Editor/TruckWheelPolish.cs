using System;
using UnityEditor;
using UnityEngine;
namespace Mach.Editor
{
    public static partial class AmmoMachineAuthoring
    {
        [MenuItem("Mach/Set Tire Width and Round Steering Wheel")]
        public static void PolishTruckWheels()
        {
            var contents=PrefabUtility.LoadPrefabContents(TruckPrefab);
            try
            {
                var truck=contents.GetComponent<UtilityTruck>();
                for(int i=0;i<4;i++)
                {
                    var visual=truck.wheelVisuals[i];visual.localScale=new Vector3(1.4f,1.11f,1.11f);
                    float x=i%2==0?-1.14f:1.14f;
                    var p=visual.localPosition;p.x=x;visual.localPosition=p;
                    p=truck.wheels[i].transform.localPosition;p.x=x;truck.wheels[i].transform.localPosition=p;
                }
                foreach(var filter in contents.GetComponentsInChildren<MeshFilter>())
                {
                    if(filter.name=="Continuous ivory wheel flare" || filter.name=="Continuous black arch liner")
                    {
                        var mesh=filter.sharedMesh;var vertices=mesh.vertices;
                        float outer=filter.name=="Continuous black arch liner"?1.442f:1.43f;
                        {for(int i=0;i<vertices.Length;i++)if(Mathf.Abs(vertices[i].x)>1.2f)vertices[i].x=Mathf.Sign(vertices[i].x)*outer;mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);}
                    }
                    if(filter.name=="Axle housing")filter.transform.localScale=new Vector3(2.19f/filter.sharedMesh.bounds.size.x,1,1);
                }
                for(int i=truck.steeringWheel.childCount-1;i>=0;i--){var child=truck.steeringWheel.GetChild(i);if(child.name=="Rim segment" || child.name=="Continuous round steering rim")UnityEngine.Object.DestroyImmediate(child.gameObject);}
                BuildRoundSteeringRim(truck.steeringWheel);
                PrefabUtility.SaveAsPrefabAsset(contents,TruckPrefab);
                foreach(var visual in truck.wheelVisuals)if(Mathf.Abs(visual.localScale.x-1.4f)>.001f)throw new Exception("Tire width does not match two thirds of previous width");
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
            AssetDatabase.SaveAssets();VerifyTruck();TruckOrthographics();TabletAuthoring.VerifyAndBuild();
        }
        static void BuildRoundSteeringRim(Transform parent)
        {
            const int segments=64,tubeSegments=12;const float radius=.205f,tube=.015f;
            var vertices=new Vector3[segments*tubeSegments];var normals=new Vector3[vertices.Length];var triangles=new int[vertices.Length*6];
            for(int i=0;i<segments;i++)for(int j=0;j<tubeSegments;j++)
            {
                float a=i*Mathf.PI*2/segments,b=j*Mathf.PI*2/tubeSegments;
                var radial=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));var normal=radial*Mathf.Cos(b)+Vector3.up*Mathf.Sin(b);int n=i*tubeSegments+j;
                vertices[n]=radial*radius+normal*tube;normals[n]=normal;
                int nextI=(i+1)%segments,nextJ=(j+1)%tubeSegments,o=n*6;
                triangles[o]=n;triangles[o+1]=nextI*tubeSegments+j;triangles[o+2]=nextI*tubeSegments+nextJ;
                triangles[o+3]=n;triangles[o+4]=nextI*tubeSegments+nextJ;triangles[o+5]=i*tubeSegments+nextJ;
            }
            string path=TruckFolder+"/Meshes/ContinuousSteeringRim.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew=!mesh;if(isNew)mesh=new Mesh();else mesh.Clear();mesh.name="Continuous round steering rim";mesh.vertices=vertices;mesh.normals=normals;mesh.triangles=triangles;mesh.RecalculateBounds();
            if(isNew)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);
            var go=new GameObject("Continuous round steering rim",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=LoadTruckMaterial("Tire rubber");
        }
    }
}

