using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class RackPolishLive
 {
  static RackPolishLive(){EditorApplication.update+=Poll;}
  static void Poll(){if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/RackPolish.request"))return;File.Delete("Temp/RackPolish.request");try{AmmoMachineAuthoring.PolishRack();File.WriteAllText("Logs/rack-polish-applied.txt","Detailed case modules saved.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/rack-polish-error.txt",e.ToString());}}
 }
 public static partial class AmmoMachineAuthoring
 {
  public static void RackPolishBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);PolishRack();VerifyRackPlacement();}
  public static void PolishRack()
  {
   var rack=UnityEngine.Object.FindAnyObjectByType<EquipmentRack>();if(!rack||rack.gameObject.scene.path!=WalkingSceneAuthoring.ScenePath)throw new Exception("Current rack missing");
   Directory.CreateDirectory(RackFolder+"/DetailMeshes");AssetDatabase.Refresh();var savedFolder=meshFolder;var savedPrefix=meshPrefix;int savedIndex=meshIndex;meshFolder=RackFolder+"/DetailMeshes";meshPrefix="Detail_";meshIndex=0;while(File.Exists(meshFolder+"/Detail_"+meshIndex.ToString("D3")+".asset"))meshIndex++;
   metal=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Graphite steel.mat");rubber=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Rubber seals.mat");var print=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Button ivory print.mat");
   var blue=AssetDatabase.LoadAssetAtPath<Material>(RateFolder+"/Materials/Screen cyan.mat");var red=AssetDatabase.LoadAssetAtPath<Material>(ErgFolder+"/Fire red.mat");var green=AssetDatabase.LoadAssetAtPath<Material>(RateFolder+"/Materials/Confirm sage.mat");
   try
   {
    for(int i=0;i<rack.slots.Length;i++)
    {
     var slot=rack.slots[i];var box=slot.box.transform;if(box.Find("Case hardware"))continue;
     var detail=new GameObject("Case hardware").transform;detail.SetParent(box,false);Undo.RegisterCreatedObjectUndo(detail.gameObject,"Detail packed equipment");var size=box.GetComponent<BoxCollider>().size;float w=size.x,d=size.z;
     foreach(float x in new[]{-1f,1f})foreach(float z in new[]{-1f,1f})
     {
      RackPart("Corner bumper",detail,new Vector3(x*(w/2-.045f),.158f,z*(d/2-.045f)),new Vector3(.095f,.294f,.095f),metal);
      RackPart("Lid corner reinforcement",detail,new Vector3(x*(w/2-.053f),.318f,z*(d/2-.053f)),new Vector3(.11f,.018f,.11f),metal);
     }
     foreach(float side in new[]{-1f,1f})
     {
      Box("Recessed handle mount",detail,new Vector3(side*.10f,.145f,-d/2-.012f),new Vector3(.04f,.067f,.035f),metal);
      Box("Hinge",detail,new Vector3(side*w*.25f,.25f,d/2+.006f),new Vector3(.075f,.055f,.025f),metal);
     }
     Box("Carry grip",detail,new Vector3(0,.146f,-d/2-.034f),new Vector3(.23f,.033f,.025f),rubber);
     Box("Latch clasp",detail,new Vector3(0,.249f,-d/2-.022f),new Vector3(.09f,.067f,.022f),metal);
     var tint=i==1||i==4?red:i==2||i==3?green:blue;
     Box("Category latch insert",detail,new Vector3(0,.25f,-d/2-.036f),new Vector3(.035f,.032f,.008f),tint);
     RackPart("Lid gasket line",detail,new Vector3(0,.276f,0),new Vector3(w-.023f,.009f,d-.023f),rubber);
     var label=box.Find("Ivory lid/Contents");if(label)label.GetComponent<TextMesh>().characterSize*=1.4f;
     var plate=RackPart("Serial plate",detail,new Vector3(0,.319f,-d*.27f),new Vector3(Mathf.Min(.30f,w-.12f),.009f,.083f),metal);
     Text("Serial",plate,"KIT / "+(i+1).ToString("D2"),new Vector3(0,0,-.006f),.032f,print);
     RackPart("Color identifier",detail,new Vector3(0,.317f,d*.31f),new Vector3(w*.42f,.012f,.026f),tint);
    }
    // Covers the latch overhang without changing the supporting footprint.
    var mover=rack.GetComponent<MovableMachine>();EditorUtility.SetDirty(mover);
    var prefabCopy=UnityEngine.Object.Instantiate(rack.gameObject);prefabCopy.name="EquipmentRack";prefabCopy.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);prefabCopy.transform.localScale=Vector3.one;PrefabUtility.SaveAsPrefabAsset(prefabCopy,RackFolder+"/EquipmentRack.prefab");UnityEngine.Object.DestroyImmediate(prefabCopy);
    AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(rack.gameObject.scene);EditorSceneManager.SaveScene(rack.gameObject.scene);
   }
   finally{meshFolder=savedFolder;meshPrefix=savedPrefix;meshIndex=savedIndex;}
  }
  public static void VerifyRackPlacement()
  {
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var rack=UnityEngine.Object.FindAnyObjectByType<EquipmentRack>();var placement=UnityEngine.Object.FindAnyObjectByType<MachinePlacement>();var pad=GameObject.CreatePrimitive(PrimitiveType.Cube);pad.name="Placement test ground";pad.transform.position=new Vector3(300,5,300);pad.transform.localScale=new Vector3(10,.2f,10);
   foreach(bool filled in new[]{false,true})
   {
    if(filled)foreach(var slot in rack.slots){slot.machine.BeginPreview();slot.Pack();}
    foreach(float slope in new[]{0f,1.49f,8f})
    {
     pad.transform.rotation=Quaternion.Euler(0,0,slope);var top=pad.transform.position+pad.transform.up*.1f;placement.viewCamera.transform.position=top+pad.transform.rotation*new Vector3(0,1.7f,-2);placement.viewCamera.transform.LookAt(top);Physics.SyncTransforms();
     if(!placement.Begin(rack.GetComponent<MovableMachine>()))throw new Exception("Begin rack");placement.UpdatePreview();if(!placement.CanPlace)throw new Exception($"Rack filled={filled} slope={slope}: {placement.PlacementError}");if(!placement.Confirm())throw new Exception("Confirm ground");
     Debug.Log($"RACK_PLACE_PASS: filled={filled}, slope={slope}");
    }
   }
   var slot0=rack.slots[0];slot0.Unpack();placement.Begin(slot0.machine);placement.viewCamera.transform.position=rack.transform.TransformPoint(new Vector3(0,2,0));placement.viewCamera.transform.LookAt(rack.transform.position);Physics.SyncTransforms();for(int i=0;i<100;i++)placement.UpdatePreview();if(slot0.Packed||!placement.IsPlacing)throw new Exception("Packed without E");if(!placement.Confirm()||!slot0.Packed)throw new Exception("E confirm packing failed");
   var scale=rack.transform.localScale;rack.transform.localScale=Vector3.one*.67f;RenderPreview(rack.gameObject,"Logs/equipment-rack-detailed.png");rack.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(pad);EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Debug.Log("RACK_POLISH_VERIFIED");
  }
 }
}
