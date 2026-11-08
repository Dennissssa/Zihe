using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Mach.Editor
{
    public static class ProjectTypography
    {
        public static Font Font=>AssetDatabase.LoadAssetAtPath<Font>("Assets/Mach/Fonts/college.ttf");
        static int Apply(GameObject root)
        {
            int count=0;
            foreach(var label in root.GetComponentsInChildren<Text>(true))
            {
                label.font=Font;label.fontStyle=FontStyle.Normal;
                label.resizeTextForBestFit=true;label.resizeTextMinSize=Mathf.Max(9,label.fontSize-6);label.resizeTextMaxSize=label.fontSize;
                EditorUtility.SetDirty(label);PrefabUtility.RecordPrefabInstancePropertyModifications(label);count++;
            }
            foreach(var label in root.GetComponentsInChildren<TextMesh>(true))
            {
                label.font=Font;label.fontStyle=FontStyle.Normal;label.GetComponent<Renderer>().sharedMaterial=Font.material;
                EditorUtility.SetDirty(label);PrefabUtility.RecordPrefabInstancePropertyModifications(label);PrefabUtility.RecordPrefabInstancePropertyModifications(label.GetComponent<Renderer>());count++;
            }
            return count;
        }
        [MenuItem("Mach/Apply College Typography")]
        public static void ApplyCollege()
        {
            AssetDatabase.Refresh();if(!Font)throw new Exception("College font missing");
            int count=0;
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Mach"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);
                try{count+=Apply(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            var scene=EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            foreach(var root in scene.GetRootGameObjects())count+=Apply(root);
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var label in root.GetComponentsInChildren<Text>(true))if(label.font!=Font)throw new Exception("UI font not replaced: "+label.name);
                foreach(var label in root.GetComponentsInChildren<TextMesh>(true))if(label.font!=Font||label.GetComponent<Renderer>().sharedMaterial!=Font.material)throw new Exception("Mesh font not replaced: "+label.name);
            }
            Debug.Log("COLLEGE_TYPOGRAPHY_VERIFIED: "+count+" labels updated across prefabs and existing scene.");
            TabletAuthoring.VerifyAndBuild();
        }
    }
}
