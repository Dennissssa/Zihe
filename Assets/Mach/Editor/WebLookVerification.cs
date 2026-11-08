using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Mach.Editor
{
 public static class WebLookVerification
 {
  public static void VerifyAndBuild()
  {
   if(PlayerLookInput.Finite(new Vector2(float.NaN,2))!=Vector2.zero||PlayerLookInput.Finite(new Vector2(2,float.PositiveInfinity))!=Vector2.zero)throw new Exception("Invalid input not rejected");
   if(PlayerLookInput.Finite(new Vector2(-12,8))!=new Vector2(-12,8))throw new Exception("Normal input changed");
   if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.WebGL)throw new Exception("Run with -buildTarget WebGL");
   PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;
   var report=BuildPipeline.BuildPlayer(new[]{"Assets/Mach/Scenes/StartMenu.unity","Assets/Mach/Scenes/Tutorial.unity","Assets/Mach/Scenes/WalkingPickup.unity"},"Builds/WebLookFix",BuildTarget.WebGL,BuildOptions.None);
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Web build failed: "+report.summary.result);
   Debug.Log("WEB_LOOK_BUILD_VERIFIED "+report.summary.totalSize+" bytes");
  }
 }
}

