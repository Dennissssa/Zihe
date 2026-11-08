using UnityEngine;
namespace Mach
{
 public static class GamePreferences
 {
  const string Prefix="Mach.Settings.";
  public static float Sensitivity=>Mathf.Clamp(PlayerPrefs.GetFloat(Prefix+"Sensitivity",1),.25f,3);
  public static float Volume=>Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix+"Volume",1));
  public static bool Chinese=>PlayerPrefs.GetInt(Prefix+"Language",0)==1;
  public static bool InvertY=>PlayerPrefs.GetInt(Prefix+"InvertY",0)==1;
  public static void SetSensitivity(float value){PlayerPrefs.SetFloat(Prefix+"Sensitivity",Mathf.Clamp(value,.25f,3));}
  public static void SetVolume(float value){PlayerPrefs.SetFloat(Prefix+"Volume",Mathf.Clamp01(value));AudioListener.volume=Volume;}
  public static void SetLanguage(bool chinese){PlayerPrefs.SetInt(Prefix+"Language",chinese?1:0);}
  public static void SetInvertY(bool invert){PlayerPrefs.SetInt(Prefix+"InvertY",invert?1:0);}
  public static void Save()=>PlayerPrefs.Save();
  public static void Defaults(){SetSensitivity(1);SetVolume(1);SetInvertY(false);Save();}
  public static Vector2 Look(Vector2 raw,float authoredSensitivity)=>Vector2.Scale(raw,new Vector2(1,InvertY?-1:1))*authoredSensitivity*Sensitivity;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Apply(){AudioListener.volume=Volume;}
 }
}
