using UnityEngine;

namespace Mach
{
    public sealed class AmmoSelectionButton : MonoBehaviour
    {
        public enum Command { Previous, Select, Next }
        public Command command;
        public AmmoSelectionConsole console;
        public Transform movingCap;
        public float pressDepth=.012f;
        float hold,depth,velocity;
        public string Prompt=>command==Command.Select?"[LMB] Select ammunition":command==Command.Previous?"[LMB] Previous ammunition":"[LMB] Next ammunition";
        public void Activate()
        {
            if(!console)return;GameAudio.Play(SoundCue.Button,transform.position);
            if(command==Command.Select)console.Select();else console.Browse(command==Command.Previous?-1:1);
            hold=.13f;
        }
        public void Animate(float deltaTime)
        {
            hold=Mathf.Max(0,hold-deltaTime);
            depth=Mathf.SmoothDamp(depth,hold>0?pressDepth:0,ref velocity,hold>0?.025f:.065f,Mathf.Infinity,deltaTime);
            if(movingCap)movingCap.localPosition=Vector3.forward*depth;
        }
        void Update()=>Animate(Time.deltaTime);
        void OnDisable(){hold=depth=velocity=0;if(movingCap)movingCap.localPosition=Vector3.zero;}
    }
}

