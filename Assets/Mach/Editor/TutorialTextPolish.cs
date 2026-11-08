using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class TutorialTextPolish
 {
  static TutorialTextPolish(){EditorApplication.update+=Poll;}
  static void Poll(){if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/TutorialText.request"))return;if(EditorSceneManager.GetActiveScene().path!=TutorialAuthoring.Path)return;File.Delete("Temp/TutorialText.request");Apply();File.WriteAllText("Logs/tutorial-text-applied.txt","Saved concise guides and translucent panel.");}
  static string K(string key)=>"<color=#FFD479><b>"+key+"</b></color>";
  [MenuItem("Mach/Tutorial/Apply concise guide styling")]
  public static void Apply()
  {
   if(EditorSceneManager.GetActiveScene().path!=TutorialAuthoring.Path)throw new System.InvalidOperationException("Open Tutorial first.");
   var t=Object.FindAnyObjectByType<TutorialDirector>();if(t.pages.Length!=11)throw new System.InvalidOperationException("Expected eleven existing lessons.");
   Undo.RecordObject(t,"Simplify tutorial text");
   string[] copy={
    K("WASD")+" move   "+K("MOUSE")+" look\n"+K("E")+" carry / place   "+K("SCROLL")+" rotate\n\nWalk to the blue pad. Carry AMMO by its round knob onto the orange pad.\n\n"+K("RMB")+" cancels carrying.",
    K("LMB")+" choose ammo   "+K("TAB")+" open tablet\n\nClick the AMMO arrows, then SELECT. Find the new Ammo block on your tablet.\n\nAmmo tells FIRE what to shoot.",
    K("LMB")+" choose rate   "+K("TAB")+" open tablet\n\nClick CHANGE, then the red confirm button. Find the Rate block.\n\nRate tells FIRE how fast to shoot.",
    K("RMB")+" launcher   "+K("SCROLL")+" choose beacon\n"+K("LMB")+" launch   "+K("E")+" recall\n\nLaunch A, recall it, then launch it again.\n\nBeacons give actions a target location.",
    K("LMB")+" select / connect   "+K("TAB")+" tablet\n\nChoose A on DRIVING and press its green button.\n\nConnect START to DRIVING, and A to PARAM. Click CHECK, then dock the tablet behind the robot.",
    K("LMB")+" select / connect   "+K("SCROLL")+" zoom\n\nSelect DRIVING and press UNLINK. Reconnect A to PARAM, then CHECK.\n\nWhite wires set action order. Gold wires supply values. Click either end first.",
    K("LMB")+" choose target / connect\n\nChoose A on FACING and press its green button.\n\nConnect START to FACING, and A to PARAM. CHECK and dock.\n\nFACING turns the turret towards A.",
    K("LMB")+" add blocks / connect   "+K("TAB")+" tablet\n\nAdd FIRE, Ammo and Rate using their machines.\n\nConnect START to FIRE. Connect both Ammo and Rate to the same FIRE PARAM.\n\nCHECK, dock and watch it fire. Remove the tablet to stop.",
    K("HOLD LMB")+" adjust time   "+K("TAB")+" tablet\n\nSet TIMER to 2 seconds and confirm.\n\nBuild FACING > FIRE > WAIT > CEASE FIRE. Add A, Ammo + Rate, and Duration to their PARAM inputs.\n\nCHECK and dock. WAIT delays stopping the fire.",
    K("E")+" carry / pack / enter   "+K("LMB")+" door\n"+K("WASD")+" drive   "+K("SPACE")+" brake\n\nPack all seven machines. Attach the tray to the truck.\n\nDrive 8 metres, stop and exit. Unload the tray and take out one machine.",
    K("R")+" remote   "+K("LMB")+" ENGAGE\n\nBuild START > FACING > FIRE. Connect A to FACING; Ammo + Rate to FIRE. CHECK and dock.\n\nPress ENGAGE when ready. Defeat three ghosts and protect the base."
   };
   for(int i=0;i<copy.Length;i++)t.pages[i].guide=copy[i];
   Undo.RecordObjects(new Object[]{t.body,t.guidePanel.GetComponent<Image>(),t.body.rectTransform},"Style tutorial panel");
   var bg=t.guidePanel.GetComponent<Image>();var c=bg.color;c.a=.8f;bg.color=c;
   t.body.supportRichText=true;t.body.fontSize=26;t.body.lineSpacing=1.3f;t.body.alignment=TextAnchor.UpperLeft;t.body.verticalOverflow=VerticalWrapMode.Truncate;
   t.plainTextPosition=new Vector2(0,15);t.plainTextSize=new Vector2(1150,560);t.diagramTextPosition=new Vector2(-340,15);t.diagramTextSize=new Vector2(620,560);
   t.body.text=t.pages[0].guide;t.body.rectTransform.anchoredPosition=t.plainTextPosition;t.body.rectTransform.sizeDelta=t.plainTextSize;
   foreach(var page in t.pages)if(page.diagram)page.diagram.SetActive(false);
   EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
  }
  public static void VerifyBatch(){EditorSceneManager.OpenScene(TutorialAuthoring.Path);Apply();var t=Object.FindAnyObjectByType<TutorialDirector>();foreach(var p in t.pages){t.body.rectTransform.sizeDelta=p.diagram?t.diagramTextSize:t.plainTextSize;t.body.text=p.guide;Canvas.ForceUpdateCanvases();if(t.body.preferredHeight>t.body.rectTransform.rect.height)throw new System.Exception("Text overflow: "+p.title);}TutorialVerification.Verify();Debug.Log("TUTORIAL_TEXT_VERIFIED");}
 }
}
