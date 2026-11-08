using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
 [InitializeOnLoad] public static class PointLauncherAuthoring
 {
  const string Folder="Assets/Mach/PointLauncher";
  static int meshIndex;
  static PointLauncherAuthoring(){EditorApplication.update+=Poll;EditorApplication.update+=PollRefine;EditorApplication.update+=PollAim;}
  static void PollAim()
  {
   if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PointLauncherAim.request"))return;
   File.Delete("Temp/PointLauncherAim.request");var gun=UnityEngine.Object.FindAnyObjectByType<PointLauncher>();if(!gun)return;
   Undo.RecordObject(gun,"Raise launcher to right");Undo.RecordObject(gun.poseRoot,"Raise launcher to right");gun.readyPosition=new Vector3(.29f,-.17f,.50f);gun.poseRoot.localPosition=gun.readyPosition;
   EditorUtility.SetDirty(gun);PrefabUtility.RecordPrefabInstancePropertyModifications(gun);EditorSceneManager.MarkSceneDirty(gun.gameObject.scene);EditorSceneManager.SaveScene(gun.gameObject.scene);File.WriteAllText("Logs/launcher-aim-applied.txt","Right/up pose saved; model unchanged.");
  }
  static void PollRefine(){if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PointLauncherRefine.request"))return;File.Delete("Temp/PointLauncherRefine.request");try{Refine();File.WriteAllText("Logs/launcher-refined.txt","Futuristic launcher and close held pose saved.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/launcher-refine-error.txt",e.ToString());}}
  public static void RefineBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Refine();}
  public static void Refine()
  {
   var gun=UnityEngine.Object.FindAnyObjectByType<PointLauncher>();if(!gun||gun.gameObject.scene.path!=WalkingSceneAuthoring.ScenePath)throw new Exception("Current launcher scene missing");
   var model=gun.poseRoot.GetChild(0);meshIndex=0;while(File.Exists(Folder+"/Meshes/Launcher_"+meshIndex+".asset"))meshIndex++;
   var ivory=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Launcher Ivory enamel.mat");var dark=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Launcher Graphite armor.mat");
   var glow=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Cyan energy.mat");if(!glow){glow=new Material(Shader.Find("Universal Render Pipeline/Unlit"));glow.SetColor("_BaseColor",new Color(.08f,1.5f,2.2f));AssetDatabase.CreateAsset(glow,Folder+"/Cyan energy.mat");}
   void Add(string name,Vector3 position,Vector3 size,Material mat){if(!model.Find(name))Undo.RegisterCreatedObjectUndo(Part(model,name,position,size,mat),"Detail launcher");}
   void Pose(string name,Vector3 position,Vector3 scale){var t=model.Find(name);if(!t)return;Undo.RecordObject(t,"Refine launcher profile");t.localPosition=position;t.localScale=scale;}
   Add("Upper floating graphite spine",new Vector3(0,.107f,.18f),new Vector3(.11f,.035f,.53f),dark);
   Add("Spine ivory armor",new Vector3(0,.135f,.22f),new Vector3(.072f,.018f,.34f),ivory);
   foreach(float side in new[]{-1f,1f})
   {
    string suffix=side<0?" left":" right";
    Add("Recessed accelerator rail"+suffix,new Vector3(side*.078f,.018f,.34f),new Vector3(.018f,.078f,.29f),dark);
    Add("Cyan accelerator seam"+suffix,new Vector3(side*.089f,.026f,.34f),new Vector3(.006f,.009f,.23f),glow);
    Add("Forward floating cheek"+suffix,new Vector3(side*.095f,-.018f,.40f),new Vector3(.024f,.07f,.15f),ivory);
    Add("Muzzle charging slit"+suffix,new Vector3(side*.094f,.01f,.55f),new Vector3(.008f,.062f,.032f),glow);
   }
   for(int i=0;i<3;i++)Add("Receiver cooling slot "+i,new Vector3(-.104f,-.048f,-.085f+i*.035f),new Vector3(.012f,.01f,.022f),dark);
   Add("Screen illuminated lower edge",new Vector3(-.113f,-.025f,-.015f),new Vector3(.007f,.005f,.055f),glow);
   Pose("Compact top sensor",new Vector3(0,.155f,-.03f),new Vector3(1.15f,.62f,.86f));
   Pose("Sensor bezel",new Vector3(0,.155f,.04f),new Vector3(1.12f,.65f,1));
   Pose("Blue sensor glass",new Vector3(0,.155f,.051f),new Vector3(1.12f,.65f,1));
   Pose("Rear sight recess",new Vector3(0,.155f,-.098f),new Vector3(1.12f,.65f,1));
   Add("Rear sensor scan line",new Vector3(0,.155f,-.103f),new Vector3(.074f,.007f,.004f),glow);
   Pose("Rear tubular housing",new Vector3(0,0,-.24f),new Vector3(1,1,.55f));
   Pose("Rear end cap",new Vector3(0,0,-.31f),Vector3.one);
   Undo.RecordObject(gun,"Close launcher framing");Undo.RecordObject(gun.poseRoot,"Close launcher framing");
   gun.readyPosition=new Vector3(.22f,-.22f,.50f);gun.readyAngles=new Vector3(0,-13,-5);gun.poseRoot.localScale=Vector3.one*.85f;gun.poseRoot.localPosition=gun.readyPosition;gun.poseRoot.localRotation=Quaternion.Euler(gun.readyAngles);EditorUtility.SetDirty(gun);
   PrefabUtility.RecordPrefabInstancePropertyModifications(gun);PrefabUtility.SaveAsPrefabAsset(model.gameObject,Folder+"/PointLauncher.prefab");EditorSceneManager.MarkSceneDirty(gun.gameObject.scene);EditorSceneManager.SaveScene(gun.gameObject.scene);AssetDatabase.SaveAssets();
  }
  static void Poll(){if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/PointLauncher.request"))return;File.Delete("Temp/PointLauncher.request");try{Apply();File.WriteAllText("Logs/point-launcher-applied.txt","Saved existing scene with launcher and eight point identities.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/point-launcher-error.txt",e.ToString());}}
  public static void ApplyBatch(){EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Apply();}
  public static void FinishMaterials()
  {
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var gun=UnityEngine.Object.FindAnyObjectByType<PointLauncher>();
   var shader=Shader.Find("Mach/Launcher Faceted");if(!shader)throw new Exception("Launcher shader missing");
   foreach(var r in gun.poseRoot.GetComponentsInChildren<MeshRenderer>(true))
   {
    if(r.GetComponent<TextMesh>()||!r.sharedMaterial)continue;string original=r.sharedMaterial.name;
    if(original!="Ivory enamel"&&original!="Graphite armor")continue;
    string path=Folder+"/Launcher "+original+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(!material){material=new Material(shader);material.SetColor("_BaseColor",original=="Ivory enamel"?new Color(.73f,.7f,.61f):new Color(.065f,.075f,.077f));AssetDatabase.CreateAsset(material,path);}r.sharedMaterial=material;EditorUtility.SetDirty(r);
   }
   PrefabUtility.SaveAsPrefabAsset(gun.poseRoot.GetChild(0).gameObject,Folder+"/PointLauncher.prefab");EditorSceneManager.MarkSceneDirty(gun.gameObject.scene);EditorSceneManager.SaveScene(gun.gameObject.scene);AssetDatabase.SaveAssets();
  }
  static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Mach/UtilityTruck/Materials/"+name+".mat");
  static GameObject Part(Transform parent,string name,Vector3 p,Vector3 size,Material mat)
  {
   var v=new List<Vector3>();var triangles=new List<int>();
   void Tri(Vector3 a,Vector3 b,Vector3 c){int n=v.Count;v.Add(a);v.Add(b);v.Add(c);triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);}
   Vector3[] Ring(float w,float h,float z){float c=Mathf.Min(w,h)*.16f;return new[]{new Vector3(-w/2+c,-h/2,z),new Vector3(w/2-c,-h/2,z),new Vector3(w/2,-h/2+c,z),new Vector3(w/2,h/2-c,z),new Vector3(w/2-c,h/2,z),new Vector3(-w/2+c,h/2,z),new Vector3(-w/2,h/2-c,z),new Vector3(-w/2,-h/2+c,z)};}
   var a=Ring(size.x,size.y,-size.z/2);var b=Ring(size.x,size.y,size.z/2);
   for(int i=0;i<8;i++){int j=(i+1)%8;Tri(new Vector3(0,0,-size.z/2),a[j],a[i]);Tri(new Vector3(0,0,size.z/2),b[i],b[j]);Tri(a[i],a[j],b[j]);Tri(a[i],b[j],b[i]);}
   var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/Meshes/Launcher_"+(meshIndex++)+".asset");
   var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=p;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;go.layer=30;return go;
  }
  public static void Apply()
  {
   var p=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();if(!p||p.gameObject.scene.path!=WalkingSceneAuthoring.ScenePath)throw new Exception("Open current walking scene first");
   if(p.GetComponent<PointLauncher>())throw new Exception("Launcher already authored; refusing to rebuild edited model");
   Directory.CreateDirectory(Folder+"/Meshes");AssetDatabase.Refresh();meshIndex=0;while(File.Exists(Folder+"/Meshes/Launcher_"+meshIndex+".asset"))meshIndex++;
   var root=new GameObject("Held point launcher pose");Undo.RegisterCreatedObjectUndo(root,"Author point launcher");root.layer=30;root.transform.SetParent(p.tablet.tabletCamera.transform,false);
   var cream=Mat("Ivory enamel");var dark=Mat("Graphite armor");var blue=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mach/Tablet/ButtonColors/Check blue.mat");var amber=Mat("Amber marker");
   var recess=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Recess.mat");if(!recess){recess=new Material(Shader.Find("Universal Render Pipeline/Unlit"));recess.SetColor("_BaseColor",new Color(.012f,.023f,.03f));AssetDatabase.CreateAsset(recess,Folder+"/Recess.mat");}
   var model=new GameObject("Point launcher model");model.layer=30;model.transform.SetParent(root.transform,false);var t=model.transform;
   Part(t,"Receiver ivory shell",Vector3.zero,new Vector3(.19f,.18f,.34f),cream);
   Part(t,"Front graphite collar",new Vector3(0,0,.17f),new Vector3(.21f,.20f,.055f),dark);
   Part(t,"Rear graphite collar",new Vector3(0,0,-.18f),new Vector3(.21f,.20f,.045f),dark);
   Part(t,"Long faceted launch tube",new Vector3(0,0,.36f),new Vector3(.14f,.14f,.34f),cream);
   Part(t,"Muzzle rim",new Vector3(0,0,.55f),new Vector3(.19f,.19f,.08f),dark);
   Part(t,"Ivory muzzle band",new Vector3(0,0,.49f),new Vector3(.18f,.18f,.045f),cream);
   Part(t,"Recessed dark muzzle",new Vector3(0,0,.591f),new Vector3(.135f,.135f,.005f),recess);
   Part(t,"Rear tubular housing",new Vector3(0,0,-.29f),new Vector3(.135f,.135f,.18f),cream);
   Part(t,"Rear end cap",new Vector3(0,0,-.405f),new Vector3(.18f,.18f,.055f),dark);
   Part(t,"Main handle",new Vector3(0,-.19f,-.07f),new Vector3(.067f,.22f,.085f),cream);
   Part(t,"Main grip sole",new Vector3(0,-.303f,-.07f),new Vector3(.079f,.028f,.095f),dark);
   Part(t,"Foregrip support",new Vector3(0,-.10f,.24f),new Vector3(.075f,.06f,.16f),dark);
   Part(t,"Forward grip",new Vector3(0,-.205f,.27f),new Vector3(.065f,.18f,.075f),cream);
   Part(t,"Foregrip cap",new Vector3(0,-.30f,.27f),new Vector3(.08f,.025f,.085f),dark);
   Part(t,"Trigger guard lower",new Vector3(0,-.17f,.035f),new Vector3(.035f,.018f,.12f),dark);
   Part(t,"Trigger guard front",new Vector3(0,-.13f,.093f),new Vector3(.035f,.08f,.02f),dark);
   Part(t,"Trigger",new Vector3(0,-.115f,.02f),new Vector3(.025f,.06f,.022f),dark);
   Part(t,"Optic mount",new Vector3(0,.11f,-.03f),new Vector3(.12f,.035f,.16f),dark);
   Part(t,"Compact top sensor",new Vector3(0,.165f,-.03f),new Vector3(.12f,.09f,.15f),cream);
   Part(t,"Sensor bezel",new Vector3(0,.165f,.05f),new Vector3(.10f,.072f,.018f),dark);
   Part(t,"Blue sensor glass",new Vector3(0,.165f,.06f),new Vector3(.073f,.048f,.006f),blue);
   Part(t,"Rear sight recess",new Vector3(0,.165f,-.108f),new Vector3(.094f,.065f,.008f),recess);
   Part(t,"Amber actuator",new Vector3(-.105f,-.055f,.11f),new Vector3(.012f,.027f,.035f),amber);
   Part(t,"Side screen bezel",new Vector3(-.101f,.018f,-.015f),new Vector3(.019f,.073f,.075f),recess);
   var label=new GameObject("Selected point letter",typeof(TextMesh));label.layer=30;label.transform.SetParent(t,false);label.transform.localPosition=new Vector3(-.113f,.018f,-.015f);label.transform.localRotation=Quaternion.Euler(0,90,0);var text=label.GetComponent<TextMesh>();text.text="A";text.font=p.board.status.font;text.fontSize=64;text.characterSize=.008f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=Color.cyan;label.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
   var muzzle=new GameObject("Launch and recall socket");muzzle.layer=30;muzzle.transform.SetParent(t,false);muzzle.transform.localPosition=new Vector3(0,0,.61f);
   PrefabUtility.SaveAsPrefabAsset(model,Folder+"/PointLauncher.prefab");
   var launcher=Undo.AddComponent<PointLauncher>(p.gameObject);launcher.program=p;launcher.poseRoot=root.transform;launcher.muzzle=muzzle.transform;launcher.screen=text;root.transform.localPosition=launcher.readyPosition;root.transform.localRotation=Quaternion.Euler(launcher.readyAngles);root.transform.localScale=Vector3.one*.75f;root.SetActive(false);
   var points=new CircuitPointMarker[8];Array.Copy(p.pointMarkers,points,Mathf.Min(3,p.pointMarkers.Length));
   for(int i=3;i<8;i++){var clone=UnityEngine.Object.Instantiate(points[0].gameObject);clone.name="Circuit target "+(char)('A'+i);Undo.RegisterCreatedObjectUndo(clone,"Author stored point");points[i]=clone.GetComponent<CircuitPointMarker>();points[i].pointId=((char)('A'+i)).ToString();points[i].stored=true;clone.GetComponent<MovableMachine>().displayName="POINT "+points[i].pointId;foreach(var textLabel in clone.GetComponentsInChildren<TextMesh>(true))textLabel.text="POINT "+points[i].pointId;clone.transform.SetParent(null);clone.SetActive(false);}
   foreach(var marker in points){Undo.RecordObject(marker,"Connect retractable marker visuals");foreach(var child in marker.GetComponentsInChildren<Transform>(true)){if(child.name.Contains("laser beam"))marker.laserBeam=child;if(child.name=="Bright laser core")marker.laserCore=child;}EditorUtility.SetDirty(marker);}
   Undo.RecordObject(p,"Eight marker inventory");p.pointMarkers=points;PrefabUtility.RecordPrefabInstancePropertyModifications(p);EditorUtility.SetDirty(p);EditorUtility.SetDirty(launcher);
   EditorSceneManager.MarkSceneDirty(p.gameObject.scene);EditorSceneManager.SaveScene(p.gameObject.scene);AssetDatabase.SaveAssets();
  }
 }
}


