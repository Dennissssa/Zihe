using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
namespace Mach
{
 public sealed class StartMenu:MonoBehaviour
 {
  public GameObject home,settings,loading;
  public Button quitButton;
  public Button tutorialButton,playButton,settingsButton,backButton,englishButton,chineseButton,invertButton,defaultsButton,fullscreenButton;
  public Slider sensitivity,volume;
  public Text sensitivityValue,volumeValue,invertValue,loadingText;
  public string tutorialScene="Assets/Mach/Scenes/Tutorial.unity",gameScene="Assets/Mach/Scenes/WalkingPickup.unity";
  bool busy;
  void Start()
  {
   Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;PlayerLookInput.Discard();AudioListener.volume=GamePreferences.Volume;
   tutorialButton.onClick.AddListener(()=>Launch(true));playButton.onClick.AddListener(()=>Launch(false));settingsButton.onClick.AddListener(()=>OpenSettings(true));backButton.onClick.AddListener(()=>OpenSettings(false));
   sensitivity.onValueChanged.AddListener(v=>{GamePreferences.SetSensitivity(v);RefreshValues();});volume.onValueChanged.AddListener(v=>{GamePreferences.SetVolume(v);RefreshValues();});
   englishButton.onClick.AddListener(()=>Language(false));chineseButton.onClick.AddListener(()=>Language(true));
   invertButton.onClick.AddListener(()=>{GamePreferences.SetInvertY(!GamePreferences.InvertY);RefreshValues();});defaultsButton.onClick.AddListener(()=>{GamePreferences.Defaults();Refresh();});
   fullscreenButton.onClick.AddListener(()=>Screen.fullScreen=!Screen.fullScreen);
   if(quitButton){
#if UNITY_WEBGL && !UNITY_EDITOR
    quitButton.gameObject.SetActive(false);
#else
    quitButton.onClick.AddListener(QuitGame);
#endif
   }
   settings.SetActive(false);home.SetActive(true);loading.SetActive(false);Refresh();Select(tutorialButton);
  }
  void Select(Button button){if(EventSystem.current)EventSystem.current.SetSelectedGameObject(button.gameObject);}
  public void QuitGame(){GamePreferences.Save();
#if UNITY_EDITOR
   UnityEditor.EditorApplication.isPlaying=false;
#elif !UNITY_WEBGL
   Application.Quit();
#endif
  }
  void Language(bool chinese){GamePreferences.SetLanguage(chinese);GamePreferences.Save();Refresh();}
  public void OpenSettings(bool show){if(busy)return;settings.SetActive(show);home.SetActive(!show);GamePreferences.Save();Refresh();Select(show?backButton:settingsButton);}
  public void Refresh(){foreach(var label in GetComponentsInChildren<MenuBilingualText>(true))label.Refresh();sensitivity.SetValueWithoutNotify(GamePreferences.Sensitivity);volume.SetValueWithoutNotify(GamePreferences.Volume);RefreshValues();}
  void RefreshValues()
  {
   sensitivityValue.text=GamePreferences.Sensitivity.ToString("0.00")+" x";volumeValue.text=Mathf.RoundToInt(GamePreferences.Volume*100)+" %";
   invertValue.text=GamePreferences.Chinese?(GamePreferences.InvertY?"开启":"关闭"):(GamePreferences.InvertY?"ON":"OFF");
   englishButton.image.color=GamePreferences.Chinese?new Color(.12f,.17f,.2f):new Color(.15f,.47f,.58f);chineseButton.image.color=GamePreferences.Chinese?new Color(.15f,.47f,.58f):new Color(.12f,.17f,.2f);
  }
  public void Launch(bool tutorial)
  {
   if(busy)return;busy=true;GamePreferences.Save();if(tutorial)TutorialDirector.BeginNewTutorial();StartCoroutine(Load(tutorial?tutorialScene:gameScene));
  }
  IEnumerator Load(string path)
  {
   home.SetActive(false);settings.SetActive(false);loading.SetActive(true);loadingText.text=GamePreferences.Chinese?"正在载入…":"LOADING...";
   yield return null;
   var operation=SceneManager.LoadSceneAsync(path);
   while(!operation.isDone){loadingText.text=(GamePreferences.Chinese?"正在载入  ":"LOADING  ")+Mathf.RoundToInt(Mathf.Clamp01(operation.progress/.9f)*100)+" %";yield return null;}
  }
  void Update(){if(!busy&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame&&settings.activeSelf)OpenSettings(false);}
  void OnDisable(){GamePreferences.Save();}
 }
}
