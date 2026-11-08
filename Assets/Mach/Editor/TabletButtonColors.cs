using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mach.Editor
{
    [InitializeOnLoad]
    public static class TabletButtonColors
    {
        const string Request = "Temp/TabletButtonColors.request";
        static TabletButtonColors() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (Application.isBatchMode || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
            if (SceneManager.GetActiveScene().path != WalkingSceneAuthoring.ScenePath) return;
            File.Delete(Request);
            try { Apply(); } catch (Exception e) { File.WriteAllText("Logs/tablet-button-colors-error.txt", e.ToString()); Debug.LogException(e); }
        }
        public static void ApplyBatch()
        {
            EditorSceneManager.OpenScene(WalkingSceneAuthoring.ScenePath);
            Apply();
        }
        [MenuItem("Mach/Tablet/Apply Button Colors Only")]
        public static void Apply()
        {
            var program = UnityEngine.Object.FindAnyObjectByType<BehaviorProgram>();
            if (!program || !program.tabletVisual) throw new Exception("Current tablet is missing.");
            var model = program.tabletVisual;
            string[] names = { "Red power button", "UNLINK cap", "CLEAR cap", "CLEAR ALL cap" };
            string[] roles = { "Check blue", "Unlink yellow", "Clear orange", "Clear orange" };
            Color[] colors = { new Color(.08f,.42f,.9f), new Color(1f,.78f,.08f), new Color(1f,.34f,.055f), new Color(1f,.34f,.055f) };
            var renderers = new Renderer[4];
            for (int i=0;i<4;i++)
                foreach (var t in model.GetComponentsInChildren<Transform>(true))
                    if (t.name == names[i]) renderers[i] = t.GetComponent<Renderer>();
            foreach (var r in renderers) if (!r || !r.sharedMaterial) throw new Exception("Expected four existing tablet button caps.");
            const string folder = "Assets/Mach/Tablet/ButtonColors";
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            for (int i=0;i<4;i++)
            {
                string path = folder + "/" + roles[i] + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material) { material = new Material(renderers[i].sharedMaterial); AssetDatabase.CreateAsset(material,path); }
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",colors[i]);
                if (material.HasProperty("_Color")) material.SetColor("_Color",colors[i]);
                material.DisableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor",Color.black);
                EditorUtility.SetDirty(material);
                Undo.RecordObject(renderers[i],"Tablet button color");
                renderers[i].sharedMaterial=material;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderers[i]);
            }
            EditorSceneManager.MarkSceneDirty(model.gameObject.scene);
            EditorSceneManager.SaveScene(model.gameObject.scene); AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/tablet-button-colors.txt",DateTime.Now+"\nUpdated only four existing tablet cap material references: CHECK blue, UNLINK yellow, CLEAR orange, CLEAR ALL orange. No transforms or meshes modified.");
        }
    }
}
