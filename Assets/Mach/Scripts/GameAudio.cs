using UnityEngine;
namespace Mach
{
    public enum SoundCue {Connect,Disconnect,Cancel,Error,Button,Dock,Undock,Pickup,Place,Pack,Unpack,Release,Deploy,Land,Shot,EngineStart,EngineStop,DoorOpen,DoorClose,RobotStart,RobotStop}
    public sealed class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance {get;private set;}
        [Range(0,1)] public float masterVolume=.7f,interfaceVolume=.65f,effectsVolume=.75f,loopVolume=.65f;
        public AudioClip[] cues;
        public AudioClip aircraftLoop,truckLoop,robotLoop,servoLoop,windLoop;
        public AudioClip vehicleImpactLight,vehicleImpactHeavy;
        public AudioSource interfaceSource;public AudioSource[] spatialVoices;
        public int PlayedCount {get;private set;}
        int nextVoice;float[] lastPlayed;
        void Awake(){Instance=this;lastPlayed=new float[System.Enum.GetValues(typeof(SoundCue)).Length];for(int i=0;i<lastPlayed.Length;i++)lastPlayed[i]=-10;}
        void OnDestroy(){if(Instance==this)Instance=null;}
        public static void Play(SoundCue cue,Vector3 position=default,bool ui=false)
        {
            var audio=Instance;if(!audio||!Application.isPlaying)return;audio.PlayCue(cue,position,ui);
        }
        public void PlayCue(SoundCue cue,Vector3 position,bool ui)
        {
            int i=(int)cue;if(cues==null||i>=cues.Length||!cues[i])return;
            if(lastPlayed!=null&&Time.unscaledTime-lastPlayed[i]<.04f)return;if(lastPlayed!=null)lastPlayed[i]=Time.unscaledTime;
            var source=ui?interfaceSource:spatialVoices!=null&&spatialVoices.Length>0?spatialVoices[nextVoice++%spatialVoices.Length]:null;if(!source)return;
            source.volume=masterVolume*(ui?interfaceVolume:effectsVolume);source.pitch=1;
            if(!ui){source.Stop();source.transform.position=position;}
            source.PlayOneShot(cues[i]);PlayedCount++;
        }
        public void PlayVehicleImpact(Vector3 position,float strength,float gain)
        {
            if(spatialVoices==null||spatialVoices.Length==0)return;
            var clip=strength<.35f?vehicleImpactLight:vehicleImpactHeavy;
            if(!clip)clip=vehicleImpactHeavy?vehicleImpactHeavy:vehicleImpactLight;
            if(!clip)return;
            var source=spatialVoices[nextVoice++%spatialVoices.Length];if(!source)return;
            source.Stop();source.transform.position=position;
            source.volume=Mathf.Clamp01(masterVolume*effectsVolume*gain*Mathf.Lerp(.18f,1,Mathf.Sqrt(Mathf.Clamp01(strength))));
            source.pitch=Random.Range(.96f,1.04f)*Mathf.Lerp(1.08f,.92f,strength);
            source.PlayOneShot(clip);PlayedCount++;
        }
    }
}
