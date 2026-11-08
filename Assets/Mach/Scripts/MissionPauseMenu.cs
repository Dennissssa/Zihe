using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mach {
    [DefaultExecutionOrder(-10000)] public sealed class MissionPauseMenu : MonoBehaviour {
        public GameObject panel;public Text heading,status;public Text[] options;
        public Font englishFont,chineseFont;
        public MissionCheckpoint checkpoint;
        static MissionPauseMenu instance;static int changedFrame=-1;
        public static bool BlocksGameplay=>instance&&(instance.open||changedFrame==Time.frameCount);
        bool open,settings,listenerPaused;float oldScale;int row;
        CursorLockMode oldCursor;bool oldVisible;
        bool CN=>GamePreferences.Chinese;
        void Awake(){instance=this;panel.SetActive(false);}
        void SetOpen(bool value){
            changedFrame=Time.frameCount;open=value;
            if(value){oldScale=Time.timeScale;oldCursor=Cursor.lockState;oldVisible=Cursor.visible;listenerPaused=AudioListener.pause;
                Time.timeScale=0;AudioListener.pause=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;row=0;settings=false;status.text="";}
            else {Time.timeScale=oldScale;AudioListener.pause=listenerPaused;Cursor.lockState=oldCursor;Cursor.visible=oldVisible;PlayerLookInput.Discard();}
            panel.SetActive(value);if(value)Refresh();
        }
        void Refresh(){
            heading.text=settings?(CN?"设置":"SETTINGS"):(CN?"暂停":"PAUSED");
            string[] labels=settings?new[]{CN?"语言   中文":"LANGUAGE   ENGLISH",(CN?"音量  −   ":"VOLUME  −   ")+Mathf.RoundToInt(GamePreferences.Volume*100)+"%",CN?"音量  +":"VOLUME  +",CN?"返回":"BACK"}:CN?new[]{"设置","保存","读档","退出到主菜单"}:new[]{"SETTINGS","SAVE","LOAD","EXIT TO MAIN MENU"};
            var font=CN?chineseFont:englishFont;heading.font=status.font=font;
            for(int i=0;i<options.Length;i++){options[i].text=labels[i];options[i].font=font;options[i].color=i==row?new Color(.98f,.70f,.30f):Color.white;}
        }
        void Activate(int i){
            try{
                if(settings){
                    if(i==3){settings=false;row=0;}else if(i==0){GamePreferences.SetLanguage(!CN);FindAnyObjectByType<LocalizedScene>()?.Refresh();FindAnyObjectByType<MissionBriefing>()?.RefreshLanguage();}
                    else GamePreferences.SetVolume(GamePreferences.Volume+(i==1?-.1f:.1f));
                    GamePreferences.Save();
                }else if(i==0){settings=true;row=0;status.text="";}
                else if(i==1)status.text=checkpoint.Save();
                else if(i==2)status.text=checkpoint.Load();
                else{TutorialDirector.ResumeLesson(0);SetOpen(false);Time.timeScale=1;AudioListener.pause=false;SceneManager.LoadScene("StartMenu");return;}
            }catch(Exception ex){status.text=CN?"操作失败，存档未载入。":"Operation failed.";Debug.LogException(ex);}
            Refresh();
        }
        void Update(){
            var k=Keyboard.current;if(k==null)return;
            if(k.escapeKey.wasPressedThisFrame){if(open&&settings){settings=false;row=0;status.text="";Refresh();}else SetOpen(!open);return;}
            if(!open)return;
            if(k.upArrowKey.wasPressedThisFrame){row=(row+3)%4;Refresh();}else if(k.downArrowKey.wasPressedThisFrame){row=(row+1)%4;Refresh();}
            if(k.enterKey.wasPressedThisFrame){Activate(row);return;}
            var m=Mouse.current;if(m==null)return;
            if(m.delta.ReadValue().sqrMagnitude<.001f&&!m.leftButton.wasPressedThisFrame)return;
            for(int i=0;i<options.Length;i++)if(RectTransformUtility.RectangleContainsScreenPoint(options[i].rectTransform,m.position.ReadValue())){row=i;Refresh();if(m.leftButton.wasPressedThisFrame)Activate(i);break;}
        }
        void OnDestroy(){if(open){Time.timeScale=1;AudioListener.pause=false;}if(instance==this)instance=null;}
    }
}
