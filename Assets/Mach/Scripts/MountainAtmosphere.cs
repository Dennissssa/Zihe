using UnityEngine;
namespace Mach {
 [ExecuteAlways]
 public sealed class MountainAtmosphere : MonoBehaviour {
  public static MountainAtmosphere Active { get; private set; }
  [Min(0)] public float distanceDensity = .00055f;
  [Min(0)] public float valleyDensity = .018f;
  public float valleyHeight = -12f;
  [Min(.001f)] public float heightFalloff = .07f;
  public Color distantColor = new Color(.49f,.58f,.68f,1);
  public Color valleyColor = new Color(.25f,.31f,.39f,1);
  void OnEnable(){Active=this;}
  void OnDisable(){if(Active==this)Active=null;}
 }
}
