using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
    [InitializeOnLoad] public static class RackLabelLive
    {
        static RackLabelLive(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/RackLabels.request"))return;
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;
            File.Delete("Temp/RackLabels.request");
            try{AmmoMachineAuthoring.ApplyRackLabels();File.WriteAllText("Logs/rack-labels-applied.txt","Large category panels saved: AMMO/RATE/FIRE red; FACING/DRIVING/TIMER green; ROBOT cobalt blue.");}
            catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/rack-labels-error.txt",e.ToString());}
        }
    }
    public static partial class AmmoMachineAuthoring
    {
        public static void RackLabelsBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);ApplyRackLabels();Debug.Log("RACK_LABELS_SAVED");}
        public static void ApplyRackLabels()
        {
            var rack=UnityEngine.Object.FindAnyObjectByType<EquipmentRack>();
            if(!rack||rack.gameObject.scene.path!=WalkingSceneAuthoring.ScenePath)throw new Exception("Open the equipment rack scene first.");
            var savedFolder=meshFolder;var savedPrefix=meshPrefix;int savedIndex=meshIndex;
            meshFolder=RackFolder+"/DetailMeshes";meshPrefix="Label_";meshIndex=0;
            Directory.CreateDirectory(meshFolder);while(File.Exists(meshFolder+"/Label_"+meshIndex.ToString("D3")+".asset"))meshIndex++;
            try
            {
                UpdateRackLabels(rack);
                var prefabPath=RackFolder+"/EquipmentRack.prefab";
                var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
                try{UpdateRackLabels(prefab.GetComponent<EquipmentRack>());PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(rack.gameObject.scene);EditorSceneManager.SaveScene(rack.gameObject.scene);
                // Preview a detached copy so the scene's empty/packed state is unchanged.
                var preview=UnityEngine.Object.Instantiate(rack.gameObject);preview.transform.SetParent(null);preview.transform.localScale=Vector3.one*.67f;
                try{foreach(var s in preview.GetComponent<EquipmentRack>().slots){s.box.SetActive(true);var legend=s.transform.Find("Socket legend");if(legend)legend.gameObject.SetActive(false);}RenderPreview(preview,"Logs/equipment-rack-category-labels.png");}
                finally{UnityEngine.Object.DestroyImmediate(preview);}
            }
            finally{meshFolder=savedFolder;meshPrefix=savedPrefix;meshIndex=savedIndex;}
        }
        static void UpdateRackLabels(EquipmentRack rack)
        {
            var red=AssetDatabase.LoadAssetAtPath<Material>(ErgFolder+"/Fire red.mat");
            var green=AssetDatabase.LoadAssetAtPath<Material>(RateFolder+"/Materials/Confirm sage.mat");
            var ivoryBase=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Warm ivory enamel.mat");
            const string robotColorPath=RackFolder+"/Cobalt blue label.mat";
            var robotColor=AssetDatabase.LoadAssetAtPath<Material>(robotColorPath);
            if(!robotColor){robotColor=new Material(ivoryBase){name="Cobalt blue label"};AssetDatabase.CreateAsset(robotColor,robotColorPath);}
            robotColor.color=new Color(0f,.278f,.671f);robotColor.DisableKeyword("_EMISSION");EditorUtility.SetDirty(robotColor);
            var ink=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Graphite steel.mat");
            var beltMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Rubber seals.mat");
            if(!red||!green||!robotColor||!ink)throw new Exception("Missing existing palette material");
            float left=float.PositiveInfinity,right=float.NegativeInfinity,firstRow=float.NegativeInfinity,secondRow=float.PositiveInfinity;
            foreach(var s in rack.slots)if(s.box.transform.Find("Ivory lid/Contents").GetComponent<TextMesh>().text.Trim().ToUpperInvariant()!="ROBOT")
            {var p=s.transform.localPosition;left=Mathf.Min(left,p.x);right=Mathf.Max(right,p.x);firstRow=Mathf.Max(firstRow,p.z);secondRow=Mathf.Min(secondRow,p.z);}
            foreach(var slot in rack.slots)
            {
                var box=slot.box.transform;var label=box.Find("Ivory lid/Contents").GetComponent<TextMesh>();
                string name=label.text.Trim().ToUpperInvariant();var tint=name=="ROBOT"?robotColor:name=="AMMO"||name=="RATE"||name=="FIRE"?red:green;
                int column=name=="FACING"||name=="AMMO"?0:name=="DRIVING"||name=="RATE"?1:2;
                if(name!="ROBOT"){Undo.RecordObject(slot.transform,"Group equipment by category");var p=slot.transform.localPosition;p.x=Mathf.Lerp(left,right,column*.5f);p.z=tint==green?firstRow:secondRow;slot.transform.localPosition=p;}
                foreach(var r in box.Find("Ivory lid").GetComponentsInChildren<MeshRenderer>(true))if(!r.GetComponent<TextMesh>()){Undo.RecordObject(r,"White label background");r.sharedMaterial=ivoryBase;EditorUtility.SetDirty(r);}
                foreach(string part in new[]{"Category latch insert","Color identifier"}){var item=box.Find("Case hardware/"+part);if(item)foreach(var r in item.GetComponentsInChildren<MeshRenderer>(true)){Undo.RecordObject(r,"Match category details");r.sharedMaterial=tint;EditorUtility.SetDirty(r);}}
                Undo.RecordObject(label,"Large readable equipment name");label.characterSize=name=="ROBOT"?.014f:.010f;label.color=Color.white;EditorUtility.SetDirty(label);
                var size=box.GetComponent<BoxCollider>().size;
                var card=box.Find("Category card");
                if(!card){card=RackPart("Category card",box,new Vector3(0,.317f,0),new Vector3(Mathf.Min(size.x*.70f,.90f),.004f,size.z*.31f),tint);Undo.RegisterCreatedObjectUndo(card.gameObject,"Inset color card");}
                foreach(var r in card.GetComponentsInChildren<MeshRenderer>(true))r.sharedMaterial=tint;
                Undo.RecordObject(label.transform,"Text above color card");label.transform.localPosition=new Vector3(0,0,-.033f);
                var hardware=box.Find("Case hardware");
                if(hardware)for(int i=hardware.childCount-1;i>=0;i--)
                {
                    var part=hardware.GetChild(i);
                    if(part.name=="Lid corner reinforcement"||part.name=="Corner bumper")Undo.DestroyObjectImmediate(part.gameObject);
                }
                if(!box.Find("Continuous black perimeter belt"))CreateRackBelt(box,size,beltMaterial);
                var panel=box.Find("Front category panel");
                if(!panel){panel=RackPart("Front category panel",box,new Vector3(0,.157f,-size.z*.5f-.004f),new Vector3(size.x-.16f,.008f,.205f),tint);panel.localRotation=Quaternion.identity;Undo.RegisterCreatedObjectUndo(panel.gameObject,"Large front category panel");}
                foreach(var r in panel.GetComponentsInChildren<MeshRenderer>(true))if(!r.GetComponent<TextMesh>())r.sharedMaterial=ivoryBase;
            }
        }
        static void CreateRackBelt(Transform box,Vector3 size,Material material)
        {
            var vertices=new Vector3[32];var triangles=new int[192];
            for(int loop=0;loop<4;loop++)
            {
                bool inner=loop>=2;float x=size.x*.5f+(inner?-.047f:.008f),z=size.z*.5f+(inner?-.047f:.008f),chamfer=inner?.018f:.035f;
                float y=loop==0||loop==3?.258f:.329f;
                Vector2[] outline={new Vector2(-x+chamfer,-z),new Vector2(x-chamfer,-z),new Vector2(x,-z+chamfer),new Vector2(x,z-chamfer),new Vector2(x-chamfer,z),new Vector2(-x+chamfer,z),new Vector2(-x,z-chamfer),new Vector2(-x,-z+chamfer)};
                for(int i=0;i<8;i++)vertices[loop*8+i]=new Vector3(outline[i].x,y,outline[i].y);
            }
            int index=0;
            for(int loop=0;loop<4;loop++)for(int i=0;i<8;i++)
            {
                int a=loop*8+i,b=loop*8+(i+1)%8,c=((loop+1)%4)*8+(i+1)%8,d=((loop+1)%4)*8+i;
                triangles[index++]=a;triangles[index++]=c;triangles[index++]=b;triangles[index++]=a;triangles[index++]=d;triangles[index++]=c;
            }
            var mesh=new Mesh{name="Continuous chamfered rubber belt",vertices=vertices,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,meshFolder+"/"+meshPrefix+(meshIndex++).ToString("D3")+".asset");
            var go=new GameObject("Continuous black perimeter belt",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(box,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;Undo.RegisterCreatedObjectUndo(go,"Continuous case protection belt");
        }
    }
}

