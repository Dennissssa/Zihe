using UnityEngine;
namespace Mach {
 public sealed class ResortLaserFence : MonoBehaviour {
  public Transform player;
  public Vector3[] perimeter;
  public float fenceHeight=4.5f, contactWidth=.55f, knockbackSpeed=9f, shockCooldown=.65f;
  public AudioSource shockAudio;
  public Transform vehicle;
  public MissionBriefing briefing;
  public Vector2 vehicleHalfSize=new Vector2(1.7f,3.8f);
  Vector3 previousVehicle; bool hasVehicle;
  Vector3 previous; bool hasPrevious; float nextShock;
  void LateUpdate(){
   if(!player||perimeter==null||perimeter.Length<3)return;
   var controller=player.GetComponent<CharacterController>();
   if(!controller||!controller.enabled){hasPrevious=false;return;}
   Vector3 p=transform.InverseTransformPoint(player.position);
   if(!hasPrevious){previous=p;hasPrevious=true;}
   if(p.y>fenceHeight||p.y+controller.height<0){previous=p;return;}
   for(int i=0;i<perimeter.Length;i++){
    Vector3 a=perimeter[i],b=perimeter[(i+1)%perimeter.Length];
    Vector3 edge=b-a;edge.y=0;float length=edge.magnitude;edge/=length;
    Vector3 normal=new Vector3(edge.z,0,-edge.x);
    float side=Vector3.Dot(p-a,normal),oldSide=Vector3.Dot(previous-a,normal);
    float along=Vector3.Dot(p-a,edge);
    bool crossed=side*oldSide<0;
    if(crossed){float t=oldSide/(oldSide-side);along=Vector3.Dot(Vector3.Lerp(previous,p,t)-a,edge);}
    if(along<-.25f||along>length+.25f||(!crossed&&Mathf.Abs(side)>contactWidth))continue;
    float sign=Mathf.Abs(oldSide)>.01f?Mathf.Sign(oldSide):1;
    Vector3 target=a+edge*Mathf.Clamp(along,0,length)+normal*sign*(contactWidth+.15f);target.y=p.y;
    controller.Move(transform.TransformPoint(target)-player.position);
    if(Time.time>=nextShock){
     nextShock=Time.time+shockCooldown;
     var walker=player.GetComponent<FirstPersonWalker>();
     if(walker)walker.ApplyExternalImpulse(transform.TransformDirection(normal*sign)*knockbackSpeed);
     Shock(player.position);
    }
    break;
   }
   previous=transform.InverseTransformPoint(player.position);
  }
  void Shock(Vector3 position){
   if(briefing)briefing.ShowRestricted();
   if(shockAudio){shockAudio.transform.position=position;if(GameAudio.Instance)shockAudio.volume=GameAudio.Instance.masterVolume*GameAudio.Instance.effectsVolume;shockAudio.Play();}
  }
  void FixedUpdate(){
   if(!vehicle||perimeter==null||perimeter.Length<3)return;
   var body=vehicle.GetComponent<Rigidbody>();if(!body)return;
   var p=transform.InverseTransformPoint(body.position);
   if(!hasVehicle){previousVehicle=p;hasVehicle=true;}
   if(p.y>fenceHeight+2||p.y+4<0){previousVehicle=p;return;}
   for(int i=0;i<perimeter.Length;i++){
    var a=perimeter[i];var b=perimeter[(i+1)%perimeter.Length];var edge=(b-a).normalized;
    var normal=new Vector3(edge.z,0,-edge.x);var worldNormal=transform.TransformDirection(normal);
    float radius=Mathf.Abs(Vector3.Dot(vehicle.right,worldNormal))*vehicleHalfSize.x+Mathf.Abs(Vector3.Dot(vehicle.forward,worldNormal))*vehicleHalfSize.y+.2f;
    float side=Vector3.Dot(p-a,normal),old=Vector3.Dot(previousVehicle-a,normal),along=Vector3.Dot(p-a,edge);
    bool crossed=side*old<0;
    if(crossed)along=Vector3.Dot(Vector3.Lerp(previousVehicle,p,old/(old-side))-a,edge);
    if(along<-radius||along>Vector3.Distance(a,b)+radius||(!crossed&&Mathf.Abs(side)>radius))continue;
    float sign=Mathf.Abs(old)>.01f?Mathf.Sign(old):1;
    var correction=normal*(sign*(radius+.15f)-side);
    body.position+=transform.TransformVector(correction);
    body.linearVelocity=Vector3.ProjectOnPlane(body.linearVelocity,worldNormal)+worldNormal*sign*2;
    body.angularVelocity*=.3f;
    if(Time.time>=nextShock){nextShock=Time.time+shockCooldown;Shock(body.position);}
    break;
   }
   previousVehicle=transform.InverseTransformPoint(body.position);
  }
 }
}
