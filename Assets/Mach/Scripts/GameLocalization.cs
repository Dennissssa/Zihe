using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
namespace Mach
{
 public static class GameLocalization
 {
  static Dictionary<string,string> entries;static Regex pattern;
  public static string Chinese(string source)
  {
   if(string.IsNullOrEmpty(source))return source;
   if(entries==null){entries=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);var asset=Resources.Load<TextAsset>("ChineseCatalog");foreach(var line in asset.text.Split('\n')){var parts=line.TrimEnd('\r').Split('\t');if(parts.Length==2&&!string.IsNullOrEmpty(parts[0]))entries[parts[0]]=parts[1];}pattern=new Regex(@"(?<![A-Za-z])(?:"+string.Join("|",entries.Keys.OrderByDescending(k=>k.Length).Select(Regex.Escape))+@")(?![A-Za-z])",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);}
   source=source.Replace("CEASE\nFIRE","CEASE FIRE").Replace("CLEAR\nALL","CLEAR ALL");
   source=Regex.Replace(source,@"Carry (.+?) by its round knob to the red/orange pad farther ahead\. Press E to confirm placement\.",m=>"对准 "+m.Groups[1].Value+" 的圆形搬运按钮，按 E 拿起，搬到前方红橙色区域，再按 E 确认放置。");
   source=Regex.Replace(source,@"Place (.+?) on the marked pad",m=>"把 "+m.Groups[1].Value+" 放到指定区域");
   // Translate text only: preserve rich-text markup, key colours and data IDs.
   return Regex.Replace(source,@"<[^>]+>|\{[^}]+\}|[^<{]+",m=>m.Value.StartsWith("<")||m.Value.StartsWith("{")?m.Value:pattern.Replace(m.Value,x=>entries[x.Value]));
  }
 }
}
