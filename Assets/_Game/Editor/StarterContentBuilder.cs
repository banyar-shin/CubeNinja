using CubeNinja.Data;
using CubeNinja.Gameplay;
using CubeNinja.Run;
using CubeNinja.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CubeNinja.Editor
{
    public static class StarterContentBuilder
    {
        private const string ConfigFolder = "Assets/_Game/Data/Configs";
        private const string PrefabFolder = "Assets/_Game/Prefabs";
        private const string ScenePath = "Assets/_Game/Scenes/CubeNinja.unity";

        [MenuItem("CubeNinja/Rebuild Starter Content")]
        public static void RebuildStarterContent()
        {
            EnsureFolders();

            var regular = GetOrCreateAsset<CubeTypeDefinition>($"{ConfigFolder}/RegularCube.asset");
            ConfigureCubeType(regular, "cube.regular", "Regular Cube", new Color(0.62f, 0.82f, 0.64f, 1f), 1, CubeClickOutcome.Score);

            var bonus = GetOrCreateAsset<CubeTypeDefinition>($"{ConfigFolder}/BonusCube.asset");
            ConfigureCubeType(bonus, "cube.bonus", "Bonus Cube", new Color(1f, 0.42f, 0.34f, 1f), 2, CubeClickOutcome.Score);

            var danger = GetOrCreateAsset<CubeTypeDefinition>($"{ConfigFolder}/DangerCube.asset");
            ConfigureCubeType(danger, "cube.danger", "Danger Cube", new Color(0.115f, 0.09f, 0.17f, 1f), 0, CubeClickOutcome.LoseLife);

            var settings = GetOrCreateAsset<CubeSpawnSettings>($"{ConfigFolder}/DefaultCubeSpawnSettings.asset");
            ConfigureSpawnSettings(settings, regular, bonus, danger);

            var prefab = BuildCubePrefab();
            BuildScene(settings, prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CubeNinja starter content rebuilt.");
        }

        private static CubeTarget BuildCubePrefab()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "CubeTarget";
            cube.GetComponent<BoxCollider>().isTrigger = true;
            cube.AddComponent<Rigidbody>();
            cube.AddComponent<CubeTarget>();

            var prefabPath = $"{PrefabFolder}/CubeTarget.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(cube, prefabPath);
            Object.DestroyImmediate(cube);
            return prefab.GetComponent<CubeTarget>();
        }

        private static void BuildScene(CubeSpawnSettings settings, CubeTarget cubePrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;

            var hudObject = new GameObject("HUD");
            var hud = hudObject.AddComponent<HudPresenter>();
            hud.SetCamera(camera);

            var runtimeObject = new GameObject("CubeNinja Runtime");
            var director = runtimeObject.AddComponent<CubeNinjaRunDirector>();
            var serializedDirector = new SerializedObject(director);
            serializedDirector.FindProperty("spawnSettings").objectReferenceValue = settings;
            serializedDirector.FindProperty("mainCamera").objectReferenceValue = camera;
            serializedDirector.FindProperty("hudPresenter").objectReferenceValue = hud;
            serializedDirector.FindProperty("cubePrefab").objectReferenceValue = cubePrefab;
            serializedDirector.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void ConfigureCubeType(
            CubeTypeDefinition asset,
            string id,
            string displayName,
            Color color,
            int pointValue,
            CubeClickOutcome clickOutcome)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("typeId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("color").colorValue = color;
            serialized.FindProperty("pointValue").intValue = pointValue;
            serialized.FindProperty("clickOutcome").enumValueIndex = (int)clickOutcome;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureSpawnSettings(
            CubeSpawnSettings settings,
            CubeTypeDefinition regular,
            CubeTypeDefinition bonus,
            CubeTypeDefinition danger)
        {
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("startingLives").intValue = 3;
            serialized.FindProperty("spawnIntervalRange").vector2Value = new Vector2(0.35f, 0.8f);
            serialized.FindProperty("launchVelocityRange").vector2Value = new Vector2(8.5f, 12f);
            serialized.FindProperty("horizontalVelocityRange").floatValue = 2.5f;
            serialized.FindProperty("angularVelocityRange").floatValue = 8f;
            serialized.FindProperty("cubeScale").floatValue = 1.275f;
            serialized.FindProperty("spawnPadding").floatValue = 0.8f;
            serialized.FindProperty("missPadding").floatValue = 1.1f;
            serialized.FindProperty("comboWindowSeconds").floatValue = 0.5f;

            var cubeTypes = serialized.FindProperty("cubeTypes");
            cubeTypes.arraySize = 3;
            SetWeightedCube(cubeTypes.GetArrayElementAtIndex(0), regular, 0.7f);
            SetWeightedCube(cubeTypes.GetArrayElementAtIndex(1), bonus, 0.2f);
            SetWeightedCube(cubeTypes.GetArrayElementAtIndex(2), danger, 0.1f);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static void SetWeightedCube(SerializedProperty element, CubeTypeDefinition cubeType, float weight)
        {
            element.FindPropertyRelative("cubeType").objectReferenceValue = cubeType;
            element.FindPropertyRelative("weight").floatValue = weight;
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "_Game");
            EnsureFolder("Assets/_Game", "Data");
            EnsureFolder("Assets/_Game/Data", "Configs");
            EnsureFolder("Assets/_Game", "Prefabs");
            EnsureFolder("Assets/_Game", "Scenes");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
