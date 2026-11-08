using UnityEngine;
namespace Mach {
 [CreateAssetMenu(menuName="Mach/Retro image settings")]
 public sealed class RetroImageSettings:ScriptableObject {
  public bool enableFilter=true;
  [Range(0,1)] public float intensity=.65f;
  [Range(180,1080)] public int verticalResolution=480;
  [Range(8,128)] public int colorSteps=40;
  [Range(0,1)] public float dithering=.15f;
  [Range(0,.3f)] public float scanlines=.025f;
  [Range(0,1)] public float bloom=.12f;
  [Range(.1f,3)] public float bloomThreshold=.8f;
  [Range(1,8)] public float bloomRadius=3;
 }
}
