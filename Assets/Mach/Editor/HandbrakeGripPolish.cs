using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Mach.Editor
{
    [InitializeOnLoad] public static class HandbrakeGripPolish
    {
        static HandbrakeGripPolish(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Temp/HandbrakeGrip.request";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
            File.Delete(request);
            try{Apply();File.WriteAllText("Logs/handbrake-grip-applied.txt","Saved");}
            catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/handbrake-grip-error.txt",e.ToString());}
        }
        static void Edit(GameObject root,Material material)
        {
            foreach(var truck in root.GetComponentsInChildren<UtilityTruck>(true))
            foreach(var part in truck.GetComponentsInChildren<Transform>(true))
            {
                if(part.name!="Handbrake grip")continue;
                part.localScale=new Vector3(1.35f,1.35f,1.25f);
                EditorUtility.SetDirty(part);if(PrefabUtility.IsPartOfPrefabInstance(part))PrefabUtility.RecordPrefabInstancePropertyModifications(part);
                var renderer=part.GetComponent<Renderer>();if(!renderer)continue;
                renderer.sharedMaterial=material;EditorUtility.SetDirty(renderer);if(PrefabUtility.IsPartOfPrefabInstance(renderer))PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }
        static void Apply()
        {
            const string materialPath="Assets/Mach/UtilityTruck/Materials/Handbrake light grey.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,materialPath);}
            material.SetColor("_BaseColor",new Color(.55f,.59f,.61f));material.SetFloat("_Metallic",.1f);material.SetFloat("_Smoothness",.24f);EditorUtility.SetDirty(material);
            var active=SceneManager.GetActiveScene();const string prefabPath="Assets/Mach/UtilityTruck/UtilityTruck.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
            try{Edit(prefab,material);PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
            foreach(var path in new[]{"Assets/Mach/Scenes/WalkingPickup.unity","Assets/Mach/Scenes/Tutorial.unity"})
            {
                var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                foreach(var root in scene.GetRootGameObjects())Edit(root,material);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();
        }
    }
}
