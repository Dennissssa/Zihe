using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Mach.Editor
{
    [InitializeOnLoad] public static class TutorialVideoAuthoring
    {
        static TutorialVideoAuthoring(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Temp/TutorialVideos.request";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
            File.Delete(request);
            try{Apply();File.WriteAllText("Logs/tutorial-videos-applied.txt","Saved first five lesson videos");}
            catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/tutorial-videos-error.txt",e.ToString());}
        }
        static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        static void Apply()
        {
            const string path="Assets/Mach/Scenes/Tutorial.unity";
            var active=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var director=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TutorialDirector>(true)).First();
            var guide=director.guidePanel;var video=guide.GetComponent<TutorialGuideVideo>();
            if(!video)
            {
                video=guide.AddComponent<TutorialGuideVideo>();video.director=director;
                var player=guide.AddComponent<VideoPlayer>();player.playOnAwake=false;player.audioOutputMode=VideoAudioOutputMode.None;player.renderMode=VideoRenderMode.RenderTexture;player.source=VideoSource.Url;player.isLooping=true;player.skipOnDrop=true;player.timeUpdateMode=VideoTimeUpdateMode.UnscaledGameTime;video.player=player;
                var frame=Rect("Lesson video frame",guide.transform,new Vector2(-245,30),new Vector2(900,506));var bg=frame.gameObject.AddComponent<Image>();bg.color=new Color(.035f,.045f,.055f,1);bg.raycastTarget=false;video.videoFrame=frame.gameObject;
                var picture=Rect("Video picture",frame,Vector2.zero,new Vector2(900,506));video.image=picture.gameObject.AddComponent<RawImage>();video.image.color=Color.white;video.image.raycastTarget=false;video.image.enabled=false;
                var fit=picture.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=16f/9f;
                var hint=Rect("Video loading status",frame,Vector2.zero,new Vector2(820,100));var label=hint.gameObject.AddComponent<Text>();label.font=director.body.font;label.fontSize=22;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(.7f,.78f,.82f);label.text="LV1 - LV5 / VIDEO";label.raycastTarget=false;video.status=label;
            }
            director.guideVideo=video;
            // Author a visible, editable first-page layout; later lessons keep their existing diagrams.
            if(director.editorPreviewPage<5)
            {
                director.body.rectTransform.anchoredPosition=video.textPosition;director.body.rectTransform.sizeDelta=video.textSize;
                foreach(var page in director.pages)if(page.diagram)page.diagram.SetActive(false);
                video.videoFrame.SetActive(true);
            }
            EditorUtility.SetDirty(director);EditorUtility.SetDirty(video);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            if(opened)EditorSceneManager.CloseScene(scene,true);if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();
        }
    }
}
