using UnityEngine;
namespace Mach
{
    // Authored from the actual bed mesh; only the equipment tray uses this assist.
    public sealed class TruckRackDock : MonoBehaviour
    {
        public Vector3 deckCenter;
        public Vector2 deckSize;
        [Min(0)] public float horizontalAllowance=.65f;
        [Min(0)] public float verticalAllowance=1.1f;
        public bool Accepts(Vector3 worldPoint)
        {
            var p=transform.InverseTransformPoint(worldPoint)-deckCenter;
            return Mathf.Abs(p.x)<=deckSize.x*.5f+horizontalAllowance && Mathf.Abs(p.z)<=deckSize.y*.5f+horizontalAllowance && Mathf.Abs(p.y)<=verticalAllowance;
        }
    }
}
