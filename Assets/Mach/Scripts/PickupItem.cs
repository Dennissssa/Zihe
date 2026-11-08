using UnityEngine;

namespace Mach
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PickupItem : MonoBehaviour
    {
        public string displayName = "Object";
        public Rigidbody Body => GetComponent<Rigidbody>();
    }
}
