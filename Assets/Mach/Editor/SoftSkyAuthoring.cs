using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Mach.Editor
{
    [InitializeOnLoad] public static class SoftSkyAuthoring
    {
        static SoftSkyAuthoring(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Temp/SoftSky.request"))return;
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=WalkingSceneAuthoring.ScenePath)return;
            File.Delete("Temp/SoftSky.request");try{Apply();File.WriteAllText("Logs/soft-sky-applied.txt","Soft pale blue gradient sky saved.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/soft-sky-error.txt",e.ToString());}
        }
        public static void Apply()
        {
            var shader=Shader.Find("Mach/Soft Blue Sky");if(!shader||ShaderUtil.ShaderHasError(shader))throw new Exception("Sky shader unavailable or invalid");
            const string path="Assets/Mach/Lighting/Soft pale blue sky.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(shader){name="Soft pale blue sky"};AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_ZenithColor",new Color(.68f,.83f,.93f));mat.SetColor("_HorizonColor",new Color(.89f,.95f,.98f));mat.SetColor("_NadirColor",new Color(.83f,.90f,.95f));mat.SetFloat("_Exposure",1);EditorUtility.SetDirty(mat);
            RenderSettings.skybox=mat;
            var player=UnityEngine.Object.FindAnyObjectByType<FirstPersonWalker>();var camera=player?player.GetComponentInChildren<Camera>():Camera.main;
            if(camera){Undo.RecordObject(camera,"Pale blue sky background");camera.clearFlags=CameraClearFlags.Skybox;PrefabUtility.RecordPrefabInstancePropertyModifications(camera);var sky=camera.GetComponent<Skybox>();if(sky){Undo.RecordObject(sky,"Sky override");sky.material=mat;PrefabUtility.RecordPrefabInstancePropertyModifications(sky);}}
            AssetDatabase.SaveAssets();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            if(camera)Preview(camera);
        }
        public static void Preview(Camera source)
        {
            var go=new GameObject("Temporary sky preview",typeof(Camera));var c=go.GetComponent<Camera>();c.CopyFrom(source);c.enabled=false;c.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
            var rt=new RenderTexture(1280,720,24);var previous=RenderTexture.active;bool async=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            try{c.targetTexture=rt;c.Render();c.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Logs/soft-sky-preview.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
            finally{RenderTexture.active=previous;c.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);ShaderUtil.allowAsyncCompilation=async;}
        }
    }
}

