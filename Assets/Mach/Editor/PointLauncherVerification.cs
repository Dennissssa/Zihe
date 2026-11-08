using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
 public static class PointLauncherVerification
 {
  static void Check(bool b,string msg){if(!b)throw new Exception(msg);Debug.Log("LAUNCHER_PASS: "+msg);}
  public static void Verify()
  {
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
   var p=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();var gun=p.GetComponent<PointLauncher>();Check(gun&&p.pointMarkers.Length==8,"Authored launcher and eight marker identities");
   for(int i=0;i<8;i++)Check(p.pointMarkers[i].pointId==((char)('A'+i)).ToString()&&p.pointMarkers[i].stored==(i>=3),"Initial inventory "+(char)('A'+i));
   gun.SetRaised(true);gun.AdvancePose(.05f);Check(Vector3.Distance(gun.poseRoot.localPosition,gun.readyPosition)>.02f,"Intermediate raise pose");for(int i=0;i<100;i++)gun.AdvancePose(.02f);Check(Vector3.Distance(gun.poseRoot.localPosition,gun.readyPosition)<.002f,"Raised pose reached");gun.Select(-1);Check(gun.screen.text=="H","Selection wraps to H and screen is letter only");gun.Select(1);Check(gun.screen.text=="A","Selection wraps to A");
   var marker=p.pointMarkers[3];var surface=GameObject.CreatePrimitive(PrimitiveType.Cube);surface.name="Verification wall";surface.transform.position=new Vector3(500,20,505);surface.transform.localScale=new Vector3(12,12,.5f);Physics.SyncTransforms();
   Check(marker.Launch(new Vector3(500,20,500),Vector3.forward,p.transform),"Stored D launches");for(int i=0;i<100;i++)marker.Advance(.02f);
   Check(!marker.InFlight&&!marker.stored&&marker.transform.parent==surface.transform,"Projectile attaches to collider");Check(Vector3.Dot(marker.transform.up,Vector3.back)>.99f,"Base aligns to wall normal");Check(marker.Deployment>.99f&&marker.heightHandle.GetComponent<Collider>().enabled,"Laser and height sphere deploy");Check(!marker.Launch(Vector3.zero,Vector3.forward,p.transform),"Deployed marker cannot be duplicated");
   var recall=new GameObject("Verification receiver");recall.transform.position=new Vector3(500,20,500);Vector3 target=marker.Position;Check(marker.Recall(recall.transform),"Recall starts");marker.Advance(.1f);Check(marker.Deployment>0&&marker.Deployment<1&&!marker.heightHandle.GetComponent<Collider>().enabled,"Retraction animated and handle disabled");for(int i=0;i<150;i++)marker.Advance(.02f);
   Check(marker.stored&&!marker.gameObject.activeSelf,"Recall stores existing identity");Check(Vector3.Distance(marker.Position,target)<.01f,"Circuit target does not chase recalled equipment");
   Check(marker.Launch(new Vector3(500,20,500),Vector3.forward,p.transform),"Recalled marker fires again");for(int i=0;i<100;i++)marker.Advance(.02f);Check(marker.Deployment>.99f&&marker.heightHandle.localScale.x>.1f,"Second deployment restores sphere size");
   var mover=marker.GetComponent<MovableMachine>();mover.BeginPreview();for(int i=0;i<30;i++)marker.Advance(.02f);Check(marker.Deployment==0&&!marker.heightHandle.GetComponent<Collider>().enabled,"Manual carrying folds laser and sphere");mover.EndPreview(false);for(int i=0;i<30;i++)marker.Advance(.02f);Check(marker.Deployment==1,"Cancel carrying redeploys");
   marker.Recall(recall.transform);for(int i=0;i<150;i++)marker.Advance(.02f);marker.Launch(new Vector3(550,30,500),Vector3.up,p.transform);for(int i=0;i<300;i++)marker.Advance(.02f);Check(marker.stored&&!marker.InFlight,"Missed shot returns to inventory");
   gun.SetRaised(false);for(int i=0;i<100;i++)gun.AdvancePose(.02f);Check(!gun.poseRoot.gameObject.activeSelf,"Lower animation hides model");
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);Debug.Log("POINT_LAUNCHER_VERIFIED");
  }
  public static void Preview()
  {
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var p=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();var gun=p.GetComponent<PointLauncher>();gun.SetRaised(true);for(int i=0;i<100;i++)gun.AdvancePose(.02f);p.tablet.poseRoot.gameObject.SetActive(false);
   var go=new GameObject("Preview camera");var cam=go.AddComponent<Camera>();cam.CopyFrom(p.tablet.tabletCamera);go.transform.SetPositionAndRotation(p.tablet.tabletCamera.transform.position,p.tablet.tabletCamera.transform.rotation);cam.cullingMask=1<<30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.22f,.27f,.3f);var lightObject=new GameObject("Preview key light");var keyLight=lightObject.AddComponent<Light>();keyLight.type=LightType.Directional;keyLight.intensity=2;keyLight.cullingMask=1<<30;lightObject.transform.rotation=Quaternion.Euler(35,-35,0);var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes("../Logs/point-launcher-preview.png",texture.EncodeToPNG());RenderTexture.active=previous;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
  }
  public static void VerifyAimAndPreview()
  {
   EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);var p=UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();var gun=p.GetComponent<PointLauncher>();gun.SetRaised(true);for(int i=0;i<100;i++)gun.AdvancePose(.02f);
   float error=Vector3.Angle(gun.muzzle.forward,gun.AimTarget()-gun.muzzle.position);Check(error<.2f,"Muzzle axis aligns with crosshair target: "+error+" degrees");
   Preview();
  }
 }
}

