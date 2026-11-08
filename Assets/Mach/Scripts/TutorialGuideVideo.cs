using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
namespace Mach
{
    public sealed class TutorialGuideVideo : MonoBehaviour
    {
        public VideoPlayer player;
        public GameObject videoFrame;
        public RawImage image;
        public Text status;
        public TutorialDirector director;
        public string[] files={"lv1.mp4","lv2.mp4","lv3.mp4","lv4.mp4","lv5.mp4"};
        public Vector2 textPosition=new Vector2(460,45),textSize=new Vector2(420,440);
        public int videoTextSize=24;
        RenderTexture buffer;
        int originalFont;float originalSpacing;Vector2 buttonPosition;
        bool originalBestFit;int originalMin,originalMax;
        bool layoutChanged,subscribed;
        void Awake()
        {
            originalFont=director.body.fontSize;originalSpacing=director.body.lineSpacing;buttonPosition=director.continueButton.anchoredPosition;
            originalBestFit=director.body.resizeTextForBestFit;originalMin=director.body.resizeTextMinSize;originalMax=director.body.resizeTextMaxSize;
            player.prepareCompleted+=Ready;player.errorReceived+=Failed;subscribed=true;
        }
        public bool Show(int stage)
        {
            StopVideo();
            if(stage<0||stage>=files.Length||string.IsNullOrEmpty(files[stage]))return false;
            videoFrame.SetActive(true);image.enabled=false;status.text="Loading video...";status.gameObject.SetActive(true);
            director.body.rectTransform.anchoredPosition=textPosition;director.body.rectTransform.sizeDelta=textSize;
            director.body.fontSize=videoTextSize;director.body.lineSpacing=1.12f;
            director.body.resizeTextForBestFit=true;director.body.resizeTextMinSize=18;director.body.resizeTextMaxSize=videoTextSize;
            director.continueButton.anchoredPosition=new Vector2(510,-320);layoutChanged=true;
            player.source=VideoSource.Url;player.url=Application.streamingAssetsPath.TrimEnd('/','\\')+"/Videos/Tutorial/"+files[stage];
            player.audioOutputMode=VideoAudioOutputMode.None;player.playOnAwake=false;player.isLooping=true;
            player.waitForFirstFrame=true;player.skipOnDrop=true;player.timeUpdateMode=VideoTimeUpdateMode.UnscaledGameTime;
            player.Prepare();return true;
        }
        void Ready(VideoPlayer source)
        {
            if(!isActiveAndEnabled||!videoFrame.activeSelf)return;
            // One capped, depth-free surface shared by the currently playing lesson only.
            float aspect=source.height>0?(float)source.width/source.height:16f/9f;
            int width=Mathf.Min(1280,source.width>0?(int)source.width:1280);
            int height=Mathf.Clamp(Mathf.RoundToInt(width/aspect),2,720);
            width=Mathf.Clamp(Mathf.RoundToInt(height*aspect),2,1280);
            buffer=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32){name="Current tutorial video",useMipMap=false,autoGenerateMips=false};buffer.Create();
            source.renderMode=VideoRenderMode.RenderTexture;source.targetTexture=buffer;image.texture=buffer;
            var fit=image.GetComponent<AspectRatioFitter>();if(fit)fit.aspectRatio=aspect;
            image.enabled=true;status.gameObject.SetActive(false);source.Play();
        }
        void Failed(VideoPlayer source,string message)
        {
            status.text="Video unavailable / continue with the instructions";status.gameObject.SetActive(true);image.enabled=false;
            Debug.LogWarning("Tutorial video: "+message,this);
        }
        public void StopVideo()
        {
            if(player){player.Stop();player.targetTexture=null;}
            if(image){image.texture=null;image.enabled=false;}
            if(buffer){buffer.Release();Destroy(buffer);buffer=null;}
            if(videoFrame)videoFrame.SetActive(false);
            if(layoutChanged&&director){director.body.fontSize=originalFont;director.body.lineSpacing=originalSpacing;director.body.resizeTextForBestFit=originalBestFit;director.body.resizeTextMinSize=originalMin;director.body.resizeTextMaxSize=originalMax;director.continueButton.anchoredPosition=buttonPosition;layoutChanged=false;}
        }
        void OnDisable()=>StopVideo();
        void OnDestroy(){if(subscribed&&player){player.prepareCompleted-=Ready;player.errorReceived-=Failed;}StopVideo();}
    }
}
