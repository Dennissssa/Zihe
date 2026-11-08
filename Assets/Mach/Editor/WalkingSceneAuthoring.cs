using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mach.Editor
{
    public static class WalkingSceneAuthoring
    {
        const string Root = "Assets/Mach";
        public const string ScenePath = Root + "/Scenes/WalkingPickup.unity";

        [MenuItem("Mach/Create Walking Pickup Scene")]
        public static void Create()
        {
            if (File.Exists(ScenePath))
            {
                Debug.Log("WalkingPickup already exists; preserving your scene edits.");
                return;
            }
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Materials");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            var previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            var floor = Material("Floor", new Color(0.23f, 0.27f, 0.3f));
            var wall = Material("Wall", new Color(0.48f, 0.54f, 0.58f));
            var orange = Material("PickupOrange", new Color(1f, 0.45f, 0.12f));
            var blue = Material("PickupBlue", new Color(0.08f, 0.65f, 0.9f));
            Cube("Floor", new Vector3(0, -0.15f, 0), new Vector3(12, 0.3f, 12), floor);
            Cube("Back Wall", new Vector3(0, 1.5f, 6), new Vector3(12, 3, 0.3f), wall);
            Cube("Front Wall", new Vector3(0, 1.5f, -6), new Vector3(12, 3, 0.3f), wall);
            Cube("Left Wall", new Vector3(-6, 1.5f, 0), new Vector3(0.3f, 3, 12), wall);
            Cube("Right Wall", new Vector3(6, 1.5f, 0), new Vector3(0.3f, 3, 12), wall);
            Cube("Workbench", new Vector3(0, 0.5f, 0), new Vector3(3, 1, 1.1f), floor);
            var crate = Cube("Orange Crate", new Vector3(0, 1.3f, 0), Vector3.one * 0.6f, orange);
            crate.AddComponent<Rigidbody>().mass = 1.5f;
            crate.AddComponent<PickupItem>().displayName = "Orange Crate";
            PrefabUtility.SaveAsPrefabAssetAndConnect(crate, Root + "/Prefabs/PickupCrate.prefab", InteractionMode.AutomatedAction);
            var second = Cube("Blue Crate", new Vector3(1, 1.25f, 0), Vector3.one * 0.5f, blue);
            second.AddComponent<Rigidbody>();
            second.AddComponent<PickupItem>().displayName = "Blue Crate";
            var third = Cube("Floor Crate", new Vector3(-2, 0.3f, 1), Vector3.one * 0.6f, orange);
            third.AddComponent<Rigidbody>();
            third.AddComponent<PickupItem>().displayName = "Floor Crate";

            var player = new GameObject("First Person Player");
            player.layer = 2;
            player.transform.position = new Vector3(0, 0.05f, -2.5f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0, 0.9f, 0);
            controller.stepOffset = 0.25f;
            var cameraObject = new GameObject("Player Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 1.65f, 0);
            var camera = cameraObject.GetComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.fieldOfView = 75;
            player.AddComponent<FirstPersonWalker>().view = camera.transform;
            var pickup = player.AddComponent<PlayerPickup>();
            pickup.viewCamera = camera;

            var hud = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
            hud.transform.SetParent(player.transform, false);
            hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = hud.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            Label(hud.transform, "Crosshair", "+", new Vector2(0, 0), new Vector2(40, 40), 26);
            pickup.prompt = Label(hud.transform, "Interaction Prompt", "", new Vector2(0, -65), new Vector2(600, 40), 22);
            Label(hud.transform, "Controls", "WASD  Move    |    Mouse  Look    |    E  Pick up / Drop    |    Esc  Cursor", new Vector2(0, -310), new Vector2(1200, 40), 19);
            PrefabUtility.SaveAsPrefabAssetAndConnect(player, Root + "/Prefabs/FirstPersonPlayer.prefab", InteractionMode.AutomatedAction);

            var light = new GameObject("Sun", typeof(Light));
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            light.GetComponent<Light>().type = LightType.Directional;
            light.GetComponent<Light>().intensity = 2;
            light.GetComponent<Light>().shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.55f, 0.6f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            Debug.Log("Authored and saved scene, player prefab, pickup prefab, materials and HUD.");
        }

        static Material Material(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            AssetDatabase.CreateAsset(material, Root + "/Materials/" + name + ".mat");
            return material;
        }

        static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        static Text Label(Transform parent, string name, string content, Vector2 position, Vector2 size, int fontSize)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = obj.GetComponent<Text>();
            text.font = ProjectTypography.Font;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        public static void VerifyAndBuild()
        {
            Create();
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var player = UnityEngine.Object.FindAnyObjectByType<FirstPersonWalker>();
            var pickup = player.GetComponent<PlayerPickup>();
            var items = UnityEngine.Object.FindObjectsByType<PickupItem>();
            if (!player.view || !pickup.prompt || items.Length != 3) throw new Exception("Missing saved scene setup.");
            Physics.SyncTransforms();
            var item = Array.Find(items, x => x.name == "Orange Crate");
            pickup.viewCamera.transform.LookAt(item.transform);
            Physics.SyncTransforms();
            if (pickup.FindTarget() != item || !pickup.TryPickUp(item) || item.Body.useGravity)
                throw new Exception("Center ray pickup failed.");
            pickup.Drop();
            if (pickup.HeldItem || !item.Body.useGravity || item.Body.isKinematic)
                throw new Exception("Drop did not restore physics.");
            var blocker = Cube("Verification occluder", new Vector3(0, 1.5f, -1.2f), new Vector3(2, 2, 0.2f), null);
            Physics.SyncTransforms();
            if (pickup.FindTarget() != null) throw new Exception("Pickup passed through a wall.");
            UnityEngine.Object.DestroyImmediate(blocker);
            EditorSceneManager.OpenScene(ScenePath); // Discard verification-only changes.
            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/WalkingPickup/WalkingPickup.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Player build failed: " + report.summary.result);
            Debug.Log("MACH_VERIFICATION_PASSED: serialized scene, ray pickup, drop restoration, wall occlusion and Windows player build.");
        }
    }
}

