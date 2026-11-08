using UnityEngine;
namespace Mach
{
    public sealed class CircuitPracticeTarget : MonoBehaviour
    {
        public TextMesh label;public string targetName;public int hits;
        public void Hit(string ammo){hits++;if(label)label.text=targetName+"\n"+ammo+"  "+hits+" HITS";}
    }
}
