using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Mach.Editor
{
    [InitializeOnLoad] public static class CompactRequirements
    {
        static CompactRequirements(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Temp/RequirementsSpacingRestore.request";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
            File.Delete(request);
            try
            {
                var active=SceneManager.GetActiveScene();const string path="Assets/Mach/Scenes/Tutorial.unity";
                var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                var director=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TutorialDirector>(true)).First();
                var text=director.objective;var panel=text.GetComponentInParent<TutorialHintBackground>();
                panel.transform.localScale=Vector3.one*.825f;
                text.text=System.Text.RegularExpressions.Regex.Replace(text.text,@"\n+(?=<color=)","\n\n");
                EditorUtility.SetDirty(text);EditorUtility.SetDirty(panel.transform);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                if(opened)EditorSceneManager.CloseScene(scene,true);if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);
                File.WriteAllText("Logs/requirements-spacing-restored.txt","Saved");
            }
            catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/requirements-spacing-restore-error.txt",e.ToString());}
        }
    }
}
