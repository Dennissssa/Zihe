using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class TutorialRefinementRequest
 {
  static TutorialRefinementRequest(){EditorApplication.update+=Poll;}
  static void Poll(){if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/TutorialRefine.request"))return;if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=TutorialAuthoring.Path)return;File.Delete("Temp/TutorialRefine.request");try{TutorialAuthoring.RefineCurrent();File.WriteAllText("Logs/tutorial-refine-applied.txt","11 focused lessons and six bays saved.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/tutorial-refine-error.txt",e.ToString());}}
 }
}
