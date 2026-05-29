using CubeNinja.Data;
using UnityEditor;
using UnityEngine;

namespace CubeNinja.Editor
{
    public static class DefinitionValidator
    {
        [MenuItem("CubeNinja/Validate Definitions")]
        public static void ValidateDefinitions()
        {
            var settingGuids = AssetDatabase.FindAssets("t:CubeSpawnSettings");
            var issues = 0;

            foreach (var guid in settingGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var settings = AssetDatabase.LoadAssetAtPath<CubeSpawnSettings>(path);
                if (settings == null)
                {
                    continue;
                }

                if (settings.CubeTypes == null || settings.CubeTypes.Length == 0)
                {
                    Debug.LogWarning($"Cube spawn settings at {path} has no cube types assigned.");
                    issues++;
                    continue;
                }

                var totalWeight = 0f;
                for (var i = 0; i < settings.CubeTypes.Length; i++)
                {
                    var entry = settings.CubeTypes[i];
                    if (entry.CubeType == null)
                    {
                        Debug.LogWarning($"Cube spawn settings at {path} has an empty cube type entry at index {i}.");
                        issues++;
                        continue;
                    }

                    totalWeight += entry.Weight;
                }

                if (totalWeight <= 0f)
                {
                    Debug.LogWarning($"Cube spawn settings at {path} has no positive spawn weights.");
                    issues++;
                }
            }

            if (settingGuids.Length == 0)
            {
                Debug.LogWarning("No CubeSpawnSettings assets found.");
                issues++;
            }

            if (issues == 0)
            {
                Debug.Log("CubeNinja definition validation passed.");
            }
        }
    }
}
