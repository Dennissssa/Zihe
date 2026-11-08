using UnityEngine;

namespace Mach
{
    public sealed class FireRateButton : MonoBehaviour
    {
        public FireRateConsole console;
        public bool isConfirm;
        public Transform keycap;
        public Vector3 releasedPosition=new Vector3(0,0,-.101f);
        float pressTime;
        public string Prompt => isConfirm ? "[LMB] Confirm fire rate" : "[LMB] Change fire rate";
        public void Activate()
        {
            if(!console)return;GameAudio.Play(SoundCue.Button,transform.position);
            if(isConfirm)console.Confirm();else console.Cycle();
            pressTime=.15f;
        }
        void Update()
        {
            pressTime=Mathf.Max(0,pressTime-Time.deltaTime);
            if(keycap)keycap.localPosition=Vector3.Lerp(keycap.localPosition,releasedPosition+Vector3.forward*(pressTime>0?.009f:0),1-Mathf.Exp(-28*Time.deltaTime));
        }
    }
}

