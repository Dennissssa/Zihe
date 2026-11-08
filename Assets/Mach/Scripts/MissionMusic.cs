using UnityEngine;
namespace Mach {
    public sealed class MissionMusic : MonoBehaviour {
        public GhostDefense defense;
        public AudioSource exploration,combat;
        [Range(0,1)] public float volume=.5f;
        [Range(0,1)] public float combatVolume=.8f;
        [Min(.01f)] public float fadeSeconds=.5f;
        bool engaged;
        float mix,gain=1;
        void Start(){
            if(!exploration||!combat)return;
            exploration.loop=combat.loop=true;
            exploration.volume=volume;combat.volume=0;
            exploration.Play();
        }
        void Update(){
            if(!defense||!exploration||!combat)return;
            if(!engaged && defense.WaveId>0){engaged=true;combat.Play();}
            float step=Time.unscaledDeltaTime/Mathf.Max(.01f,fadeSeconds);
            if(engaged)mix=Mathf.MoveTowards(mix,1,step);
            if(defense.Lost||defense.Won)gain=Mathf.MoveTowards(gain,0,step);
            exploration.volume=volume*gain*(1-mix);combat.volume=combatVolume*gain*mix;
            if((mix==1||gain==0)&&exploration.isPlaying)exploration.Stop();
            if(gain==0&&combat.isPlaying)combat.Stop();
        }
    }
}
