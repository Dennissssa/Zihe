using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Mach
{
 // Authored in each scene. Only presentation changes; program identifiers stay English.
 [DefaultExecutionOrder(32000)] public sealed class LocalizedScene:MonoBehaviour
 {
  public Font chineseFont;
  sealed class Label {public Text ui;public TextMesh mesh;public Font font;public FontStyle style;public Material material;public string source,last,translated;public bool language;public bool alive=>ui||mesh;public string value{get=>ui?ui.text:mesh.text;set{if(ui)ui.text=value;else mesh.text=value;}}}
  readonly List<Label> labels=new List<Label>();readonly HashSet<Component> known=new HashSet<Component>();float discover;
  void OnEnable(){Scan();Refresh();}
  void Scan(){foreach(var root in gameObject.scene.GetRootGameObjects()){foreach(var t in root.GetComponentsInChildren<Text>(true))if(!t.GetComponent<MenuBilingualText>()&&known.Add(t))labels.Add(new Label{ui=t,font=t.font,style=t.fontStyle,source=t.text});foreach(var t in root.GetComponentsInChildren<TextMesh>(true))if(known.Add(t))labels.Add(new Label{mesh=t,font=t.font,style=t.fontStyle,material=t.GetComponent<Renderer>().sharedMaterial,source=t.text});}}
  public void Refresh()
  {
   bool zh=GamePreferences.Chinese;
   foreach(var l in labels){if(!l.alive)continue;var current=l.value;if(current!=l.last&&current!=l.source){l.source=current;l.translated=null;}if(zh&&l.translated==null)l.translated=GameLocalization.Chinese(l.source);string value=zh?l.translated:l.source;
    if(l.last==null||l.language!=zh){if(l.ui){l.ui.font=zh?chineseFont:l.font;l.ui.fontStyle=zh?FontStyle.Normal:l.style;}else{l.mesh.font=zh?chineseFont:l.font;l.mesh.fontStyle=zh?FontStyle.Normal:l.style;l.mesh.GetComponent<Renderer>().sharedMaterial=zh?chineseFont.material:l.material;}l.language=zh;}
    if(zh&&l.mesh&&l.mesh.GetComponent<Renderer>().sharedMaterial!=chineseFont.material)l.mesh.GetComponent<Renderer>().sharedMaterial=chineseFont.material;if(current!=value)l.value=value;l.last=value;
   }
  }
  void LateUpdate(){if(Time.unscaledTime>=discover){discover=Time.unscaledTime+.5f;Scan();}Refresh();}
 }
}
