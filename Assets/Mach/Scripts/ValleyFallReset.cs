using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mach
{
    public sealed class ValleyFallReset : MonoBehaviour
    {
        public Transform player;
        public Image blackout;
        public float deathHeight = -5f;
        public float fadeSeconds = .65f;
        bool resetting;
        float elapsed;

        void Update()
        {
            if(!resetting)
            {
                if(!player || player.position.y >= deathHeight)return;
                resetting=true;
                Time.timeScale=0;
            }
            elapsed+=Time.unscaledDeltaTime;
            if(blackout)blackout.color=new Color(0,0,0,Mathf.Clamp01(elapsed/Mathf.Max(.01f,fadeSeconds)));
            if(elapsed<fadeSeconds)return;
            Time.timeScale=1;
            SceneManager.LoadScene(gameObject.scene.path);
        }
        void OnDisable(){if(resetting)Time.timeScale=1;}
    }
}
