using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class EquipmentRackLive
 {
  static EquipmentRackLive(){EditorApplication.update+=Poll;}
  static void Poll(){if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/EquipmentRack.request"))return;File.Delete("Temp/EquipmentRack.request");try{AmmoMachineAuthoring.AuthorEquipmentRack();File.WriteAllText("Logs/equipment-rack-applied.txt","Equipment rack saved in truck bed.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/equipment-rack-error.txt",e.ToString());}}
 }
 public static partial class AmmoMachineAuthoring
 {
  const string RackFolder="Assets/Mach/EquipmentRack";
  static Transform RackPart(string name,Transform parent,Vector3 position,Vector3 size,Material mat)
  {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;t.localRotation=Quaternion.Euler(90,0,0);Panel("Chamfered shell",t,Vector3.zero,size.x,size.z,.025f,size.y,mat);return t;}
  public static void RackBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);AuthorEquipmentRack();VerifyEquipmentRack();}
  public static void AuthorEquipmentRack()
  {
   if(UnityEngine.Object.FindAnyObjectByType<EquipmentRack>())return;
   var truck=UnityEngine.Object.FindAnyObjectByType<UtilityTruck>();if(!truck||truck.gameObject.scene.path!=WalkingSceneAuthoring.ScenePath)throw new Exception("Open truck scene first");
   var machines=new List<MovableMachine>();var labels=new List<string>();
   void Add(Component c,string label){if(!c||!c.GetComponent<MovableMachine>())throw new Exception("Missing machine "+label);machines.Add(c.GetComponent<MovableMachine>());labels.Add(label);}
   Add(UnityEngine.Object.FindAnyObjectByType<AmmoSelectionConsole>(),"AMMO");Add(UnityEngine.Object.FindAnyObjectByType<FireRateConsole>(),"RATE");
   foreach(var kind in new[]{CircuitKind.Facing,CircuitKind.DriveTo,CircuitKind.Fire,CircuitKind.Wait}){NavigationConsole found=null;foreach(var c in UnityEngine.Object.FindObjectsByType<NavigationConsole>())if(c.mode==kind)found=c;Add(found,kind==CircuitKind.DriveTo?"DRIVING":kind==CircuitKind.Wait?"TIMER":kind.ToString().ToUpper());}
   Add(UnityEngine.Object.FindAnyObjectByType<RobotAssemblyRig>(),"ROBOT");
   Transform floor=null;foreach(var t in truck.GetComponentsInChildren<Transform>())if(t.name=="Cargo bed floor")floor=t;if(!floor)throw new Exception("Truck floor missing");
   var bounds=new Bounds();bool first=true;foreach(var v in floor.GetComponent<MeshFilter>().sharedMesh.vertices){var p=truck.transform.InverseTransformPoint(floor.TransformPoint(v));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
   float w=bounds.size.x-.16f,d=bounds.size.z-.16f;
   Directory.CreateDirectory(RackFolder+"/Meshes");AssetDatabase.Refresh();var savedRoot=root;var savedFolder=meshFolder;var savedPrefix=meshPrefix;int savedIndex=meshIndex;meshFolder=RackFolder+"/Meshes";meshPrefix="Rack_";meshIndex=0;
   ivory=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Warm ivory enamel.mat");metal=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Graphite steel.mat");rubber=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Rubber seals.mat");var print=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Button ivory print.mat");var cyan=AssetDatabase.LoadAssetAtPath<Material>(RateFolder+"/Materials/Screen cyan.mat");
   try
   {
    root=new GameObject("EquipmentRack").transform;Undo.RegisterCreatedObjectUndo(root.gameObject,"Create equipment rack");var rack=root.gameObject.AddComponent<EquipmentRack>();var mover=root.gameObject.AddComponent<MovableMachine>();mover.displayName="Equipment rack";mover.boundsCenter=new Vector3(0,.23f,0);mover.boundsSize=new Vector3(w,.46f,d);mover.validGhost=machines[0].validGhost;mover.invalidGhost=machines[0].invalidGhost;
    RackPart("Reinforced floor",root,new Vector3(0,.04f,0),new Vector3(w,.08f,d),metal);
    var collider=root.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,.04f,0);collider.size=new Vector3(w,.08f,d);
    foreach(float side in new[]{-1f,1f}){RackPart("Side rail",root,new Vector3(side*(w/2-.035f),.23f,0),new Vector3(.07f,.38f,d),ivory);RackPart("End rail",root,new Vector3(0,.23f,side*(d/2-.035f)),new Vector3(w,.38f,.07f),ivory);foreach(float end in new[]{-1f,1f})RackPart("Corner protector",root,new Vector3(side*(w/2-.04f),.23f,end*(d/2-.045f)),new Vector3(.12f,.44f,.13f),metal);}
    CarryKnob(root,new Vector3(0,.26f,-d/2-.02f),mover,print);
    foreach(float side in new[]{-1f,1f}){Box("Latch",root,new Vector3(side*w*.34f,.23f,-d/2-.012f),new Vector3(.09f,.18f,.05f),metal);Box("Blue latch tab",root,new Vector3(side*w*.34f,.24f,-d/2-.041f),new Vector3(.045f,.06f,.012f),cyan);}
    rack.slots=new EquipmentSlot[7];float cellW=(w-.18f)/3,cellD=(d-.18f)/3;
    for(int i=0;i<7;i++)
    {
     bool robot=i==6;float bw=robot?w-.24f:cellW-.035f,bd=cellD-.04f;var t=new GameObject(labels[i]+" fixed slot").transform;t.SetParent(root,false);t.localPosition=new Vector3(robot?0:(i%3-1)*cellW,.09f,robot?-cellD:cellD-(i/3)*cellD);
     var slot=t.gameObject.AddComponent<EquipmentSlot>();slot.rack=rack;slot.machine=machines[i];rack.slots[i]=slot;
     RackPart("Empty socket",t,new Vector3(0,.015f,0),new Vector3(bw+.012f,.025f,bd+.012f),rubber);
     var labelPlane=new GameObject("Socket legend").transform;labelPlane.SetParent(t,false);labelPlane.localRotation=Quaternion.Euler(90,0,0);Text("Label",labelPlane,labels[i],new Vector3(0,0,-.031f),.046f,print);
     var box=new GameObject(labels[i]+" packed box");box.transform.SetParent(t,false);slot.box=box;
     RackPart("Graphite case",box.transform,new Vector3(0,.145f,0),new Vector3(bw,.27f,bd),metal);var lid=RackPart("Ivory lid",box.transform,new Vector3(0,.291f,0),new Vector3(bw-.045f,.038f,bd-.045f),ivory);Text("Contents",lid,labels[i],new Vector3(0,0,-.023f),robot?.085f:.051f,metal);
     Box("Front latch",box.transform,new Vector3(0,.23f,-bd/2-.008f),new Vector3(.085f,.06f,.02f),rubber);
     var hit=box.AddComponent<BoxCollider>();hit.center=new Vector3(0,.15f,0);hit.size=new Vector3(bw,.31f,bd);box.AddComponent<MachineMoveHandle>().machine=machines[i];box.SetActive(false);
    }
    PrefabUtility.SaveAsPrefabAsset(root.gameObject,RackFolder+"/EquipmentRack.prefab");root.SetParent(truck.transform,false);root.localPosition=new Vector3(bounds.center.x,bounds.max.y+.032f,bounds.center.z);
    AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(truck.gameObject.scene);EditorSceneManager.SaveScene(truck.gameObject.scene);File.WriteAllText("Logs/equipment-rack-dimensions.txt",$"Truck bed {bounds.size}; rack {w:F3} x {d:F3}; 7 slots");
   }
   finally{root=savedRoot;meshFolder=savedFolder;meshPrefix=savedPrefix;meshIndex=savedIndex;}
  }
  public static void VerifyEquipmentRack()
  {
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var rack=UnityEngine.Object.FindAnyObjectByType<EquipmentRack>();if(!rack||rack.slots.Length!=7)throw new Exception("Seven slots required");var placement=UnityEngine.Object.FindAnyObjectByType<MachinePlacement>();
   foreach(var slot in rack.slots)
   {
    var m=slot.machine;var original=m.transform.position;var scale=m.transform.localScale;m.BeginPreview();slot.Preview(true);slot.Pack();if(m.gameObject.activeSelf||!slot.Packed||!slot.box.activeSelf||m.transform.parent!=rack.transform)throw new Exception("Pack failed");
    if(!placement.Begin(m)||!m.gameObject.activeSelf||slot.Packed)throw new Exception("Unpack failed");placement.Cancel();if(!slot.Packed||m.gameObject.activeSelf)throw new Exception("Cancel must repack");
    slot.Unpack();m.BeginPreview();m.transform.position=original;m.EndPreview(true);if(m.transform.localScale!=scale)throw new Exception("Scale changed");
   }
   foreach(var s in rack.slots){s.machine.BeginPreview();s.Pack();}var before=rack.slots[0].box.transform.position;rack.transform.position+=Vector3.right;if(Vector3.Distance(rack.slots[0].box.transform.position,before+Vector3.right)>.001f)throw new Exception("Cargo detached");
   rack.transform.SetParent(null,true);rack.transform.position+=Vector3.up*5;var probe=rack.slots[0];probe.Unpack();if(!placement.Begin(probe.machine))throw new Exception("Begin packing preview");placement.viewCamera.transform.position=rack.transform.TransformPoint(new Vector3(0,2,0));placement.viewCamera.transform.LookAt(rack.transform.position);Physics.SyncTransforms();placement.UpdatePreview();if(placement.IsPlacing)placement.FinishPacking();if(!probe.Packed){Physics.Raycast(placement.viewCamera.transform.position,placement.viewCamera.transform.forward,out var obstacle,4);throw new Exception("Rack ray preview not accepted: "+(obstacle.collider?obstacle.collider.name:"no hit"));}
   var robotSlot=rack.slots[6];rack.transform.SetParent(null,true);robotSlot.Unpack();robotSlot.machine.BeginPreview();robotSlot.machine.EndPreview(true);if(robotSlot.machine.GetComponent<Rigidbody>().isKinematic)throw new Exception("Robot ground physics stuck after rack unload");robotSlot.machine.BeginPreview();robotSlot.Pack();
   var previewScale=rack.transform.localScale;rack.transform.localScale=Vector3.one*.67f;RenderPreview(rack.gameObject,"Logs/equipment-rack-preview.png");rack.transform.localScale=previewScale;EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Debug.Log("EQUIPMENT_RACK_VERIFIED");
  }
 }
}




