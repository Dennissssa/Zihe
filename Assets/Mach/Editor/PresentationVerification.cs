using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class PresentationVerification
 {
  static int phase;static double began,stageTime;static Vector3 position;static Quaternion rotation;static bool initialized;
  static PresentationVerification(){EditorApplication.update+=Poll;}
  public static void Run(){EditorSceneManager.OpenScene(StartMenuAuthoring.Path);PresentationAuthoring.Apply();SessionState.SetBool("Presentation.OldLanguage",GamePreferences.Chinese);SessionState.SetBool("Presentation.Verify",true);EditorApplication.EnterPlaymode();}
  static void Check(bool condition,string why){if(!condition)throw new Exception(why);}
  static void Finish(int code){GamePreferences.SetLanguage(SessionState.GetBool("Presentation.OldLanguage",false));GamePreferences.Save();SessionState.SetBool("Presentation.Verify",false);EditorApplication.Exit(code);}
  static void Poll()
  {
   if(!SessionState.GetBool("Presentation.Verify",false)||!EditorApplication.isPlaying)return;
   try{
    if(began==0)began=EditorApplication.timeSinceStartup;if(EditorApplication.timeSinceStartup-began>100)throw new Exception("Presentation test timeout");
    var scene=SceneManager.GetActiveScene();
    if(phase==0){var p=UnityEngine.Object.FindAnyObjectByType<MenuRobotPerformance>();if(!p||Time.timeSinceLevelLoad<1)return;if(!initialized){initialized=true;position=p.transform.position;rotation=p.transform.rotation;stageTime=EditorApplication.timeSinceStartup;GamePreferences.SetLanguage(true);UnityEngine.Object.FindAnyObjectByType<StartMenu>().Refresh();}
     if(EditorApplication.timeSinceStartup-stageTime<8)return;Check(p.Shots>5,"Menu robot did not shoot");Check(p.transform.position==position&&p.transform.rotation==rotation,"Menu robot moved out of its stage");Check(!p.GetComponent<RobotCircuitRunner>()&&!p.GetComponent<Rigidbody>(),"Performance has gameplay physics");Check(p.leftTrack&&p.rightTrack&&p.leftTrack.beltTravel>0,"Tracks not animated");Check(Mathf.Abs(p.gun.localEulerAngles.x)>1,"Gun not animated");CaptureMenu();phase=1;SceneManager.LoadScene(TutorialAuthoring.Path);}
    else if(phase==1&&scene.path==TutorialAuthoring.Path){var t=UnityEngine.Object.FindAnyObjectByType<TutorialDirector>();if(!t||!t.GuideVisible)return;var loc=UnityEngine.Object.FindAnyObjectByType<LocalizedScene>();loc.Refresh();Check(t.heading.text.Contains("基础交互"),"Tutorial heading not translated");Check(t.body.text.Contains("圆形搬运按钮"),"Guide not translated");Check(t.body.font==loc.chineseFont,"Tutorial not using heavy font");File.WriteAllLines("Logs/tutorial-zh-audit.txt",t.pages.Select((p,i)=>GameLocalization.Chinese(p.title)+"\n"+GameLocalization.Chinese(t.GuideText(i))+"\n"+string.Join("\n",p.requirements.Select(GameLocalization.Chinese))));
     var board=t.program.board;int fire=t.program.Add(CircuitKind.Fire);t.program.Connect(0,fire,false);Check(!t.program.Validate(out var error)&&error.Contains("AMMO"),"English circuit semantics changed");board.Refresh();loc.Refresh();Check(board.status.text.Contains("已连接")||board.status.text.Contains("已添加"),"Dynamic status not translated");GamePreferences.SetLanguage(false);loc.Refresh();Check(t.heading.text.Contains("BASIC INTERACTION"),"English restore failed");GamePreferences.SetLanguage(true);phase=2;SceneManager.LoadScene("Assets/Mach/Scenes/WalkingPickup.unity");}
    else if(phase==2&&scene.path.EndsWith("WalkingPickup.unity")){var loc=UnityEngine.Object.FindAnyObjectByType<LocalizedScene>();var prog=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();if(!loc||!prog)return;prog.Say("FIRE needs its own AMMO parameter.");prog.board.Refresh();loc.Refresh();Check(prog.board.status.text=="开火模块需要连接弹药参数。","Gameplay error localization");Check(GameLocalization.Chinese("<color=#FFD479>[LMB]</color> Select ammunition")=="<color=#FFD479>[左键]</color> 选择弹药","Markup or interaction text");Debug.Log("PRESENTATION_RUNTIME_VERIFIED: stationary animation, random shooting, heavy Chinese, tutorial, dynamic errors, English restoration, gameplay identifiers");Finish(0);}
   }catch(Exception e){Debug.LogException(e);Finish(1);}
  }
  static void CaptureMenu(){var menu=UnityEngine.Object.FindAnyObjectByType<StartMenu>();var canvas=menu.GetComponent<Canvas>();var cam=Camera.main;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=.5f;var rt=new RenderTexture(1600,900,24);cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;var t=new Texture2D(1600,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1600,900),0,0);t.Apply();File.WriteAllBytes("Logs/menu-performance-zh.png",t.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(t);canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}
 }
}
