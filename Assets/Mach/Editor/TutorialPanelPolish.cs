using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Mach.Editor
{
    [InitializeOnLoad] public static class TutorialPanelPolish
    {
        static TutorialPanelPolish(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Temp/TutorialPanelPolish.request";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
            File.Delete(request);
            try{Apply();File.WriteAllText("Logs/tutorial-panel-polish-applied.txt","Saved");}
            catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/tutorial-panel-polish-error.txt",e.ToString());}
        }
        static void Hint(Text text,bool right,int fontSize,float width)
        {
            if(!text)return;
            var wrapper=text.transform.parent.GetComponent<TutorialHintBackground>();
            if(!wrapper)
            {
                var go=new GameObject(right?"Requirements background":"Key hints background",typeof(RectTransform),typeof(Image),typeof(TutorialHintBackground));
                go.transform.SetParent(text.transform.parent,false);go.transform.SetSiblingIndex(text.transform.GetSiblingIndex());
                wrapper=go.GetComponent<TutorialHintBackground>();wrapper.content=text;wrapper.background=go.GetComponent<Image>();text.transform.SetParent(go.transform,false);
            }
            var root=(RectTransform)wrapper.transform;root.anchorMin=root.anchorMax=root.pivot=new Vector2(right?1:0,1);root.anchoredPosition=new Vector2(right?-24:24,-24);root.sizeDelta=new Vector2(width,300);
            wrapper.background.color=new Color(.025f,.035f,.045f,.78f);wrapper.background.raycastTarget=false;
            var rect=text.rectTransform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=new Vector2(1,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-20);rect.sizeDelta=new Vector2(-40,260);
            text.fontSize=fontSize;text.resizeTextForBestFit=false;text.lineSpacing=1.2f;text.alignment=TextAnchor.UpperLeft;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;text.raycastTarget=false;
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,text.preferredHeight+44);
            EditorUtility.SetDirty(text);EditorUtility.SetDirty(wrapper);
        }
        static void Apply()
        {
            var active=SceneManager.GetActiveScene();const string path="Assets/Mach/Scenes/Tutorial.unity";
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var t=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TutorialDirector>(true)).First();
            var panel=(RectTransform)t.guidePanel.transform;panel.anchorMin=Vector2.zero;panel.anchorMax=Vector2.one;panel.offsetMin=new Vector2(24,24);panel.offsetMax=new Vector2(-24,-24);
            t.guidePanel.GetComponent<Image>().color=new Color(.018f,.025f,.035f,.94f);
            foreach(var page in t.pages)page.keys=System.Text.RegularExpressions.Regex.Replace(page.keys??"",@"[ \t]+(?=<color=)","\n");
            if(t.keyHints)t.keyHints.text=System.Text.RegularExpressions.Regex.Replace(t.keyHints.text,@"[ \t]+(?=<color=)","\n");
            Hint(t.keyHints,false,26,480);Hint(t.objective,true,24,520);
            EditorUtility.SetDirty(t);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            if(opened)EditorSceneManager.CloseScene(scene,true);if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();
        }
    }
}
