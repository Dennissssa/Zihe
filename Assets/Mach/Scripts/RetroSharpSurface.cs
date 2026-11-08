using System.Collections.Generic;
using UnityEngine;
namespace Mach {
 // Authored on display meshes; registration also follows spawned tutorial prefabs.
 public sealed class RetroSharpSurface:MonoBehaviour {
  public static readonly HashSet<RetroSharpSurface> Active=new HashSet<RetroSharpSurface>();
  public Renderer[] surfaces;
  void OnEnable(){Active.Add(this);} void OnDisable(){Active.Remove(this);}
 }
}
