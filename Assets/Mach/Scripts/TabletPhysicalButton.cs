using UnityEngine;
namespace Mach
{
    public sealed class TabletPhysicalButton : MonoBehaviour
    {
        Vector3 rest;float hold,depth;
        void Awake()=>rest=transform.localPosition;
        public void Press()=>hold=.09f;
        void Update()
        {
            hold-=Time.unscaledDeltaTime;
            depth=Mathf.MoveTowards(depth,hold>0?.0035f:0,Time.unscaledDeltaTime*.045f);
            transform.localPosition=rest+Vector3.forward*depth;
        }
        void OnDisable(){transform.localPosition=rest;hold=depth=0;}
    }
}
