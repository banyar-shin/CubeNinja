using UnityEngine;

namespace CubeNinja.Data
{
    [CreateAssetMenu(menuName = "CubeNinja/Data/Cube Spawn Settings", fileName = "CubeSpawnSettings")]
    public sealed class CubeSpawnSettings : ScriptableObject
    {
        [SerializeField] private int startingLives = 3;
        [SerializeField] private Vector2 spawnIntervalRange = new Vector2(0.35f, 0.8f);
        [SerializeField] private Vector2 launchVelocityRange = new Vector2(8.5f, 12f);
        [SerializeField] private float horizontalVelocityRange = 2.5f;
        [SerializeField] private float angularVelocityRange = 8f;
        [SerializeField] private float cubeScale = 1.275f;
        [SerializeField] private float spawnPadding = 0.8f;
        [SerializeField] private float missPadding = 1.1f;
        [SerializeField] private float comboWindowSeconds = 0.5f;
        [SerializeField] private WeightedCubeType[] cubeTypes;

        public int StartingLives => Mathf.Max(1, startingLives);
        public Vector2 SpawnIntervalRange => SanitizeRange(spawnIntervalRange, 0.05f);
        public Vector2 LaunchVelocityRange => SanitizeRange(launchVelocityRange, 0.1f);
        public float HorizontalVelocityRange => Mathf.Max(0f, horizontalVelocityRange);
        public float AngularVelocityRange => Mathf.Max(0f, angularVelocityRange);
        public float CubeScale => Mathf.Max(0.1f, cubeScale);
        public float SpawnPadding => Mathf.Max(0f, spawnPadding);
        public float MissPadding => Mathf.Max(0.1f, missPadding);
        public float ComboWindowSeconds => Mathf.Max(0.01f, comboWindowSeconds);
        public WeightedCubeType[] CubeTypes => cubeTypes;

        public CubeTypeDefinition PickCubeType(float normalizedRoll)
        {
            if (cubeTypes == null || cubeTypes.Length == 0)
            {
                return null;
            }

            var totalWeight = 0f;
            for (var i = 0; i < cubeTypes.Length; i++)
            {
                if (cubeTypes[i].CubeType != null)
                {
                    totalWeight += cubeTypes[i].Weight;
                }
            }

            if (totalWeight <= 0f)
            {
                return FirstDefinedCubeType();
            }

            var target = Mathf.Clamp01(normalizedRoll) * totalWeight;
            var accumulated = 0f;

            for (var i = 0; i < cubeTypes.Length; i++)
            {
                var entry = cubeTypes[i];
                if (entry.CubeType == null)
                {
                    continue;
                }

                accumulated += entry.Weight;
                if (target <= accumulated)
                {
                    return entry.CubeType;
                }
            }

            return FirstDefinedCubeType();
        }

        public static CubeSpawnSettings CreateDefaultRuntime()
        {
            var settings = CreateInstance<CubeSpawnSettings>();
            var regular = CubeTypeDefinition.CreateRuntime(
                "cube.regular",
                "Regular Cube",
                new Color(0.62f, 0.82f, 0.64f, 1f),
                1,
                CubeClickOutcome.Score);
            var bonus = CubeTypeDefinition.CreateRuntime(
                "cube.bonus",
                "Bonus Cube",
                new Color(1f, 0.42f, 0.34f, 1f),
                2,
                CubeClickOutcome.Score);
            var danger = CubeTypeDefinition.CreateRuntime(
                "cube.danger",
                "Danger Cube",
                new Color(0.115f, 0.09f, 0.17f, 1f),
                0,
                CubeClickOutcome.LoseLife);

            settings.cubeTypes = new[]
            {
                new WeightedCubeType(regular, 0.7f),
                new WeightedCubeType(bonus, 0.2f),
                new WeightedCubeType(danger, 0.1f)
            };

            return settings;
        }

        private CubeTypeDefinition FirstDefinedCubeType()
        {
            for (var i = 0; i < cubeTypes.Length; i++)
            {
                if (cubeTypes[i].CubeType != null)
                {
                    return cubeTypes[i].CubeType;
                }
            }

            return null;
        }

        private static Vector2 SanitizeRange(Vector2 range, float minimum)
        {
            var x = Mathf.Max(minimum, range.x);
            var y = Mathf.Max(minimum, range.y);
            return x <= y ? new Vector2(x, y) : new Vector2(y, x);
        }
    }
}
