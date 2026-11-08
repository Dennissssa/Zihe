using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class StartMenuVerification
 {
  static int phase;static double began;
  static StartMenuVerification(){EditorApplication.update+=Poll;}
  public static void Run(){SessionState.SetFloat("Menu.OldSensitivity",GamePreferences.Sensitivity);SessionState.SetFloat("Menu.OldVolume",GamePreferences.Volume);SessionState.SetBool("Menu.OldLanguage",GamePreferences.Chinese);SessionState.SetBool("Menu.OldInvert",GamePreferences.InvertY);SessionState.SetBool("Menu.Verify",true);EditorSceneManager.OpenScene(StartMenuAuthoring.Path);EditorApplication.EnterPlaymode();}
  static void Finish(int code){GamePreferences.SetSensitivity(SessionState.GetFloat("Menu.OldSensitivity",1));GamePreferences.SetVolume(SessionState.GetFloat("Menu.OldVolume",1));GamePreferences.SetLanguage(SessionState.GetBool("Menu.OldLanguage",false));GamePreferences.SetInvertY(SessionState.GetBool("Menu.OldInvert",false));GamePreferences.Save();SessionState.SetBool("Menu.Verify",false);EditorApplication.Exit(code);}
  static void Check(bool value,string message){if(!value)throw new Exception(message);}
  static void Poll()
  {
   if(!SessionState.GetBool("Menu.Verify",false)||!EditorApplication.isPlaying)return;
   try{
    if(began==0)began=EditorApplication.timeSinceStartup;if(EditorApplication.timeSinceStartup-began>90)throw new Exception("Menu verification timed out");
    if((phase==0||phase==2)&&Time.timeSinceLevelLoad<1)return;
    var menu=UnityEngine.Object.FindAnyObjectByType<StartMenu>();
    if(phase==0&&menu){menu.settingsButton.onClick.Invoke();Check(menu.settings.activeSelf&&!menu.home.activeSelf,"Settings page");menu.sensitivity.value=1.75f;menu.volume.value=.35f;Check(Mathf.Abs(GamePreferences.Sensitivity-1.75f)<.001f&&Mathf.Abs(AudioListener.volume-.35f)<.001f,"Settings values");menu.chineseButton.onClick.Invoke();Check(GamePreferences.Chinese&&menu.backButton.GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("返回"),"Chinese translation");var font=Resources.Load<Font>("ChineseUI");Check(font&&font.HasCharacter('设')&&font.HasCharacter('置'),"Chinese glyphs");GamePreferences.SetInvertY(false);Check(GamePreferences.Look(new Vector2(2,3),2)==new Vector2(7,10.5f),"Look sensitivity");menu.backButton.onClick.Invoke();Check(menu.home.activeSelf,"Back navigation");phase=1;menu.tutorialButton.onClick.Invoke();}
    else if(phase==1&&SceneManager.GetActiveScene().path=="Assets/Mach/Scenes/Tutorial.unity"){Check(UnityEngine.Object.FindAnyObjectByType<TutorialDirector>()!=null,"Tutorial entry");phase=2;SceneManager.LoadScene(StartMenuAuthoring.Path);}
    else if(phase==2&&menu){Check(Mathf.Abs(menu.sensitivity.value-1.75f)<.001f&&GamePreferences.Chinese,"Settings persisted across scenes");phase=3;menu.playButton.onClick.Invoke();}
    else if(phase==3&&SceneManager.GetActiveScene().path=="Assets/Mach/Scenes/WalkingPickup.unity"){Debug.Log("START_MENU_RUNTIME_VERIFIED: settings, Chinese glyphs, persistence, tutorial and gameplay routing");Finish(0);}
   }catch(Exception e){Debug.LogException(e);Finish(1);}
  }
 }
}

