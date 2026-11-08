using UnityEngine;
namespace Mach
{
    public sealed class DefenseBase : MonoBehaviour
    {
        public float maxHealth=30,currentHealth=30;
        public TextMesh label;
        public bool Destroyed=>currentHealth<=0;
        void Start(){currentHealth=maxHealth;Refresh();}
        public void Damage(float amount){currentHealth=Mathf.Max(0,currentHealth-Mathf.Max(0,amount));Refresh();}
        void Refresh(){if(label)label.text=Destroyed?"BASE DESTROYED":"HOME BASE\nHP "+currentHealth.ToString("0")+" / "+maxHealth.ToString("0");}
    }
}
