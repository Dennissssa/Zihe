using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class BeaconRecallAuthoring
 {
  static BeaconRecallAuthoring(){EditorApplication.update+=Poll;}
  static void Poll(){if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/BeaconRecall.request"))return;if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;File.Delete("Temp/BeaconRecall.request");try{Apply();File.WriteAllText("Logs/beacon-recall-applied.txt","Eight beacon colors authored; selected remote recall enabled.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/beacon-recall-error.txt",e.ToString());}}
  public static void ApplyBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Apply();}
  public static void Apply()
  {
   string folder="Assets/Mach/PointLauncher/BeaconColors";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
   string[] palette={"#FF5151","#4595FF","#58DF76","#FFD34A","#BA79FF","#FF923E","#3FE0DD","#FF78BD"};
   var program=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();
   foreach(var point in program.pointMarkers)
   {
    int index=point.pointId[0]-'A';if(index<0||index>=8)continue;ColorUtility.TryParseHtmlString(palette[index],out var color);
    Undo.RecordObject(point,"Beacon identification color");point.beaconColor=color;EditorUtility.SetDirty(point);
    int part=0;foreach(var target in new[]{point.laserBeam,point.laserCore,point.heightHandle})
    {
     if(!target)continue;
     foreach(var renderer in target.GetComponentsInChildren<Renderer>(true))
     {
      string path=folder+"/"+point.pointId+"_"+(part++)+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
      if(!material){material=new Material(renderer.sharedMaterial);AssetDatabase.CreateAsset(material,path);}
      Color tint=color;tint.a=renderer.sharedMaterial.color.a;material.color=tint;
      if(material.HasProperty("_EmissionColor")){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*1.4f);}
      EditorUtility.SetDirty(material);Undo.RecordObject(renderer,"Beacon visual color");renderer.sharedMaterial=material;EditorUtility.SetDirty(renderer);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
     }
    }
    foreach(var label in point.GetComponentsInChildren<TextMesh>(true)){Undo.RecordObject(label,"Beacon label color");label.color=color;EditorUtility.SetDirty(label);}
    PrefabUtility.RecordPrefabInstancePropertyModifications(point);
   }
   var launcher=program.GetComponent<PointLauncher>();launcher.Select(0);
   AssetDatabase.SaveAssets();var scene=program.gameObject.scene;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  public static void Verify()
  {
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var program=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();var launcher=program.GetComponent<PointLauncher>();
   launcher.SetRaised(true);
   for(int i=0;i<8;i++)
   {
    var point=program.pointMarkers[i];launcher.selected=i;
    if(!point.Launch(launcher.muzzle.position,Vector3.forward,program.transform))throw new Exception("Launch failed");
    point.Attach(new Vector3(300+i*10,40,300),Vector3.up,null);point.Advance(.4f);
    var original=program.pointMarkers[i];
    if(launcher.FindRecallTarget()!=point||!launcher.RecallSelected()||launcher.RecallSelected())throw new Exception("Remote recall / duplicate guard failed");
    for(int n=0;n<150&&!point.stored;n++)point.Advance(.02f);
    if(!point.stored||point.gameObject.activeSelf||program.pointMarkers[i]!=original)throw new Exception("Inventory / reference failed");
    launcher.Select(0);if(launcher.screen.color!=point.beaconColor)throw new Exception("Screen color mismatch");
    for(int j=0;j<i;j++)if(point.beaconColor==program.pointMarkers[j].beaconColor)throw new Exception("Duplicate beacon colors");
    if(!point.Launch(launcher.muzzle.position,Vector3.forward,program.transform))throw new Exception("Cannot refire");
    point.Attach(new Vector3(300,40,300),Vector3.up,null);point.Recall(launcher.muzzle);for(int n=0;n<150&&!point.stored;n++)point.Advance(.02f);
   }
   Debug.Log("BEACON_RECALL_VERIFIED: A-H remote recall from >400m, repeat guard, inventory identity, refire, eight distinct colors and matching screen");EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
  }
 }
}

