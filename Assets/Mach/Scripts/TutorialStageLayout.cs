using System;
using System.Linq;
using UnityEngine;
namespace Mach
{
 [Serializable] public class TutorialLayoutItem
 {
  public Transform target;
  public Transform pose;
  public bool visible=true;
 }
 public sealed class TutorialStageLayout:MonoBehaviour
 {
  public TutorialDirector director;
  [Header("Carry lesson detection")]
  [Tooltip("Machine to carry onto the placement pad. Leave empty to automatically use the single visible movable machine in Items.")]
  public MovableMachine carryTarget;
  public MovableMachine ResolveCarryTarget()
  {
   if(carryTarget)return carryTarget;
   var candidates=items?.Where(i=>i.visible&&i.target).Select(i=>i.target.GetComponent<MovableMachine>()).Where(m=>m).Distinct().ToArray();
   return candidates!=null&&candidates.Length==1?candidates[0]:null;
  }
  [Tooltip("Width (X) and depth (Z), in metres.")] public Vector2 areaSize=new Vector2(22,38);
  public TutorialLayoutItem[] items;
  public Transform pointA,pointB;
  public Transform beaconTargetOne,beaconTargetTwo;
  public BeaconDroneStation droneStation;
  public GameObject decorations;
  public Transform fences;
  public void Apply()
  {
   var character=director.program.GetComponent<CharacterController>();bool enabled=character&&character.enabled;if(enabled)character.enabled=false;
   foreach(var slot in director.rack.slots)if(slot.machine&&!items.Any(i=>i.target==slot.machine.transform))slot.machine.gameObject.SetActive(false);
   // Place parents before children, independently of the preview currently saved in the scene.
   foreach(var group in items.Where(i=>i.target).GroupBy(i=>i.target))
   {
    var item=group.FirstOrDefault(i=>i.visible)??group.First();
    if(item.pose)
    {
     item.target.SetPositionAndRotation(item.pose.position,item.pose.rotation);
     if(Application.isPlaying)
     {
      var motor=item.target.GetComponent<RobotGroundMotor>();
      if(motor)motor.ResetForLesson(item.pose.position,item.pose.rotation);
      else if(item.target.TryGetComponent<Rigidbody>(out var body))
      {
       body.position=item.pose.position;body.rotation=item.pose.rotation;
       if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
      }
     }
    }
    item.target.gameObject.SetActive(group.Any(i=>i.visible));
   }
   if(Application.isPlaying)Physics.SyncTransforms();
   if(enabled)character.enabled=true;
   director.zoneCenter=transform.position;director.zoneSize=areaSize;
   foreach(var page in director.pages)if(page.layout&&page.layout.decorations)page.layout.decorations.SetActive(page.layout==this);
  }
  void OnDrawGizmosSelected()
  {
   Gizmos.color=new Color(.2f,.8f,1,.7f);Gizmos.DrawWireCube(transform.position+Vector3.up,new Vector3(areaSize.x,2,areaSize.y));
   if(items!=null)foreach(var item in items)if(item.pose&&item.visible){Gizmos.color=item.target==director.program.transform?Color.yellow:Color.cyan;Gizmos.DrawWireSphere(item.pose.position+Vector3.up*.2f,.25f);Gizmos.DrawRay(item.pose.position+Vector3.up*.2f,item.pose.forward);}
   Gizmos.color=Color.green;if(pointA)Gizmos.DrawWireSphere(pointA.position,.3f);if(pointB)Gizmos.DrawWireSphere(pointB.position,.3f);
  }
 }
}
