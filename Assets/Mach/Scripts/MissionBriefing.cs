using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Mach {
 public sealed class MissionBriefing:MonoBehaviour {
  public GameObject panel;
  public Text heading,description,restricted;
  public GhostDefense defense;
  public Font englishFont,chineseFont;
  public bool showOnStart=true;
  [TextArea(6,14)] public string briefEnglish,briefChinese;
  float restrictedUntil,previousTimeScale,resultShownAt;
  bool showing,result;

  public void RefreshLanguage(){
   if(result)return;
   bool cn=GamePreferences.Chinese;
   foreach(var label in new[]{heading,description,restricted})if(label)label.font=cn?chineseFont:englishFont;
   if(heading)heading.text=cn?"疗养院行动 | 守住两个基地":"SANATORIUM / DEFEND BOTH BASES";
   if(description)description.text=cn?briefChinese:briefEnglish;
  }
  void Start(){
   RefreshLanguage();
   if(MissionCheckpoint.Restoring)showOnStart=false;
   if(restricted)restricted.gameObject.SetActive(false);
   if(panel)panel.SetActive(showOnStart);
   if(showOnStart)PauseForPage();
  }
  void PauseForPage(){
   if(!showing){previousTimeScale=Time.timeScale;showing=true;}
   Time.timeScale=0;
  }
  public void ShowRestricted(){
   if(!restricted||result)return;
   restricted.text="Restricted Area\n"+(GamePreferences.Chinese?"禁止人员及车辆进入 · 请使用机器人或无人机":"NO PERSONNEL OR VEHICLES / ROBOTS & DRONES ONLY");
   restricted.gameObject.SetActive(true);restrictedUntil=Time.unscaledTime+2.5f;
  }
  void ShowResult(bool won){
   result=true;resultShownAt=Time.unscaledTime;
   if(restricted)restricted.gameObject.SetActive(false);
   bool cn=GamePreferences.Chinese;
   heading.color=won?new Color(.35f,.96f,.78f):new Color(1,.38f,.24f);
   heading.text=won?(cn?"任务成功":"MISSION COMPLETE"):(cn?"任务失败":"MISSION FAILED");
   string reason=won?(cn?"两个基地都已保住。两处出生点已摧毁，剩余敌人已清除。":"Both bases survived. Both spawners were destroyed and all enemies were cleared."):(cn?"一个基地已被摧毁。必须同时守住 A 和 B 两个基地。":"A base was destroyed. Both Base A and Base B must survive.");
   description.text=reason+"\n\n"+(cn?"已消灭敌人  ":"ENEMIES DEFEATED  ")+defense.Defeated+"\n\n"+(cn?"[ENTER] 重新开始任务":"[ENTER] REPLAY MISSION");
   panel.SetActive(true);PauseForPage();
  }
  void Update(){
            if(MissionPauseMenu.BlocksGameplay)return;
   if(!result && defense && (defense.Lost||defense.Won))ShowResult(defense.Won&&!defense.Lost);
   if(showing && Keyboard.current!=null && Keyboard.current.enterKey.wasPressedThisFrame){
    if(result){if(Time.unscaledTime-resultShownAt>.4f){Time.timeScale=previousTimeScale;showing=false;SceneManager.LoadScene(gameObject.scene.path);}return;}
    showing=false;if(panel)panel.SetActive(false);Time.timeScale=previousTimeScale;
   }
   if(restricted&&restricted.gameObject.activeSelf&&Time.unscaledTime>restrictedUntil)restricted.gameObject.SetActive(false);
  }
  void OnDisable(){if(showing){Time.timeScale=previousTimeScale;showing=false;}}
 }
}
