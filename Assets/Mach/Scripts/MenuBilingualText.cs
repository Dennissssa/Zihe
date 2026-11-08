using UnityEngine;
using UnityEngine.UI;
namespace Mach
{
 [RequireComponent(typeof(Text))] public sealed class MenuBilingualText:MonoBehaviour
 {
  [TextArea] public string english,chinese;
  public Font englishFont,chineseFont;
  public void Refresh(){var text=GetComponent<Text>();text.text=GamePreferences.Chinese?chinese:english;text.font=GamePreferences.Chinese?chineseFont:englishFont;text.fontStyle=FontStyle.Normal;}
 }
}

