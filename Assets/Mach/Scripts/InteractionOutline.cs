using System;
using System.Linq;
using UnityEngine;
namespace Mach
{
 [DisallowMultipleComponent] public sealed class InteractionOutline:MonoBehaviour
 {
  public PlayerPickup pickup;
  [Range(1,6)] public float width=2.5f;
  public Color color=Color.white;
  Transform previous;Renderer[] renderers=Array.Empty<Renderer>();
  public Renderer[] Targets()
  {
   var driver=pickup?pickup.truckDriver:null;
   var target=Time.timeScale<=0?null:driver&&driver.CurrentTruck?driver.OutlineTarget:pickup&&pickup.isActiveAndEnabled?pickup.OutlineTarget:null;
   if(!target){previous=null;renderers=Array.Empty<Renderer>();return renderers;}
   if(target!=previous){previous=target;var group=target.GetComponent<InteractionOutlineGroup>();var roots=group&&group.parts!=null&&group.parts.Length>0?group.parts:new[]{target};renderers=roots.Where(t=>t).SelectMany(t=>t.GetComponentsInChildren<Renderer>(true)).Where(r=>(r is MeshRenderer||r is SkinnedMeshRenderer)&&!r.GetComponent<TextMesh>()).Distinct().ToArray();}
   return renderers;
  }
 }
}
