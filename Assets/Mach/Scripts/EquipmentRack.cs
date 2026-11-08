using UnityEngine;
namespace Mach
{
 public sealed class EquipmentRack : MonoBehaviour
 {
  public EquipmentSlot[] slots;
  public EquipmentSlot SlotFor(MovableMachine machine){foreach(var s in slots)if(s.machine==machine)return s;return null;}
 }
}
