using System;
using UnityEngine;
using UnityEditor.SceneManagement;
namespace Mach.Editor
{
 public static class TruckDockVerification
 {
  public static void Verify()
  {
   GameAudioAuthoring.Verify();
   var truck=UnityEngine.Object.FindAnyObjectByType<UtilityTruck>();var dock=truck.GetComponent<TruckRackDock>();
   var placement=UnityEngine.Object.FindAnyObjectByType<MachinePlacement>();placement.viewCamera=null;placement.pickup=null;placement.tablet=null;
   var drop=UnityEngine.Object.FindAnyObjectByType<EquipmentAirdrop>();if(drop.aircraft.gameObject.activeSelf)throw new Exception("Aircraft visible before start");
   drop.Advance(4.99f);if(drop.aircraft.gameObject.activeSelf||drop.Released)throw new Exception("Aircraft appeared early");
   drop.Advance(.02f);if(!drop.aircraft.gameObject.activeSelf||drop.Released)throw new Exception("Aircraft failed five-second arrival");
   var planePosition=drop.aircraft.position;drop.Advance(1);if(Vector3.Distance(planePosition,drop.aircraft.position)<1)throw new Exception("Plane hovering");drop.Advance(24);
   var machine=drop.GetComponent<MovableMachine>();
   truck.transform.SetPositionAndRotation(new Vector3(0,100,0),Quaternion.Euler(0,37,0));Physics.SyncTransforms();
   if(!placement.Begin(machine))throw new Exception("Cannot begin rack placement");
   placement.Rotate(12);
   var target=truck.transform.TransformPoint(dock.deckCenter);
   if(!placement.PreviewAt(target+truck.transform.right*.7f,true,truck))throw new Exception("Offset docking failed: "+placement.PlacementError);
   if(Vector3.Distance(machine.transform.position,target)>.05f)throw new Exception("Not centered");
   if(Quaternion.Angle(machine.transform.rotation,truck.transform.rotation)>.01f)throw new Exception("Rack facing backwards");
   if(machine.transform.parent==truck.transform)throw new Exception("Attached before E confirmation");
   if(!placement.Confirm()||machine.transform.parent!=truck.transform)throw new Exception("Confirmation failed: "+placement.PlacementError);
      var floor=System.Array.Find(truck.GetComponentsInChildren<Collider>(),c=>c.name=="Cargo bed floor");var cargo=machine.GetComponent<BoxCollider>();
   if(!Physics.GetIgnoreCollision(cargo,floor))throw new Exception("Cargo fights carrier collider");
   placement.Begin(machine);placement.Rotate(2);
   if(Physics.GetIgnoreCollision(cargo,floor))throw new Exception("Unloading did not restore collisions");
   var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=target+Vector3.up*.3f;blocker.transform.localScale=Vector3.one*.2f;Physics.SyncTransforms();
   if(placement.PreviewAt(target,true,truck))throw new Exception("Occupied bed accepted");
   UnityEngine.Object.DestroyImmediate(blocker);placement.Cancel();
   if(dock.Accepts(target+Vector3.right*10))throw new Exception("Snap range too large");
   Debug.Log("TRUCK_DOCK_VERIFIED: offset and rotated tray aligns; E required; cargo attaches; obstacles rejected; bounded snap range");
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
  }
 }
}

