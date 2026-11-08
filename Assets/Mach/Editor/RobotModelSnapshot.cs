using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace Mach.Editor
{
    [InitializeOnLoad]
    public static class RobotModelSnapshot
    {
        static RobotModelSnapshot(){EditorApplication.update+=ReadRequest;}
        static void ReadRequest()
        {
            const string request="Temp/ReadRobotModel.request";
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
            File.Delete(request);
            var report=new StringBuilder();
            report.AppendLine("Live editor model snapshot: "+System.DateTime.UtcNow.ToString("o"));
            report.AppendLine("Play Mode: "+EditorApplication.isPlaying);
            foreach(var robot in Object.FindObjectsByType<RobotAssemblyRig>(FindObjectsInactive.Include))
            {
                report.AppendLine("SCENE "+robot.gameObject.scene.path+" unsaved="+robot.gameObject.scene.isDirty);
                foreach(var t in robot.GetComponentsInChildren<Transform>(true))
                {
                    string path=AnimationUtility.CalculateTransformPath(t,robot.transform);
                    report.AppendLine(path+" | position="+t.localPosition.ToString("F4")+" rotation="+t.localEulerAngles.ToString("F4")+" scale="+t.localScale.ToString("F4")+" active="+t.gameObject.activeSelf);
                    var mesh=t.GetComponent<MeshFilter>();if(mesh&&mesh.sharedMesh)report.AppendLine("  mesh="+AssetDatabase.GetAssetPath(mesh.sharedMesh)+" vertices="+mesh.sharedMesh.vertexCount+" bounds="+mesh.sharedMesh.bounds);
                    var renderer=t.GetComponent<Renderer>();if(renderer)foreach(var mat in renderer.sharedMaterials)if(mat)report.AppendLine("  material="+AssetDatabase.GetAssetPath(mat));
                }
            }
            File.WriteAllText("Logs/robot-live-model-baseline.txt",report.ToString());
        }
    }
}
