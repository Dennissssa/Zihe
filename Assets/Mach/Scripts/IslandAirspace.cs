using UnityEngine;
namespace Mach {
 [ExecuteAlways]
 public sealed class IslandAirspace:MonoBehaviour {
  public static IslandAirspace Active {get;private set;}
  public Vector3 center=new Vector3(300,0,-80);
  public Vector2 radii=new Vector2(390,510);
  public float ceiling=196;
  void OnEnable(){Active=this;} void OnDisable(){if(Active==this)Active=null;}
  public Vector3 Constrain(Vector3 point){
   var p=point-center;float x=p.x/Mathf.Max(1,radii.x),z=p.z/Mathf.Max(1,radii.y);
   float distance=Mathf.Sqrt(x*x+z*z);
   if(distance>1){point.x=center.x+p.x/distance;point.z=center.z+p.z/distance;}
   point.y=Mathf.Min(point.y,ceiling);return point;
  }
  void OnDrawGizmosSelected(){
   Gizmos.color=Color.cyan;
   for(int i=0;i<96;i++){
    float a=i*Mathf.PI*2/96,b=(i+1)*Mathf.PI*2/96;
    Gizmos.DrawLine(new Vector3(center.x+Mathf.Cos(a)*radii.x,ceiling,center.z+Mathf.Sin(a)*radii.y),new Vector3(center.x+Mathf.Cos(b)*radii.x,ceiling,center.z+Mathf.Sin(b)*radii.y));
   }
  }
 }
}
