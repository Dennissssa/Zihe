using UnityEngine;
namespace Mach
{
 public sealed class EquipmentSlot : MonoBehaviour
 {
  public EquipmentRack rack;public MovableMachine machine;public GameObject box;
  [field: SerializeField] public bool Packed {get;private set;}
  Material[][] materials;Renderer[] visuals;
  void Cache(){if(visuals!=null)return;visuals=box.GetComponentsInChildren<Renderer>(true);materials=new Material[visuals.Length][];for(int i=0;i<visuals.Length;i++)materials[i]=visuals[i].sharedMaterials;}
  public void Preview(bool show)
  {
   Cache();box.SetActive(show||Packed);var legend=transform.Find("Socket legend");if(legend)legend.gameObject.SetActive(!show&&!Packed);
   for(int i=0;i<visuals.Length;i++){if(show&&!Packed&&!visuals[i].GetComponent<TextMesh>()){var m=new Material[materials[i].Length];for(int j=0;j<m.Length;j++)m[j]=machine.validGhost;visuals[i].sharedMaterials=m;}else visuals[i].sharedMaterials=materials[i];}
  }
  public void Pack()
  {
   if(machine.IsPreview)machine.EndPreview(true,rack.transform);
   machine.transform.SetParent(rack.transform,true);machine.transform.position=transform.position;
   machine.gameObject.SetActive(false);Packed=true;Preview(false);
  }
  public void Unpack()
  {
   Packed=false;Preview(false);machine.gameObject.SetActive(true);
  }
 }
}
