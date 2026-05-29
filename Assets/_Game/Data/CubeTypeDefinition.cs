using UnityEngine;

namespace CubeNinja.Data
{
    [CreateAssetMenu(menuName = "CubeNinja/Data/Cube Type", fileName = "CubeType")]
    public sealed class CubeTypeDefinition : ScriptableObject
    {
        [SerializeField] private string typeId = "cube.regular";
        [SerializeField] private string displayName = "Regular Cube";
        [SerializeField] private Color color = Color.green;
        [SerializeField] private int pointValue = 1;
        [SerializeField] private CubeClickOutcome clickOutcome = CubeClickOutcome.Score;

        public string TypeId => typeId;
        public string DisplayName => displayName;
        public Color Color => color;
        public int PointValue => Mathf.Max(0, pointValue);
        public CubeClickOutcome ClickOutcome => clickOutcome;
        public bool AwardsPoints => clickOutcome == CubeClickOutcome.Score;

        public static CubeTypeDefinition CreateRuntime(
            string id,
            string display,
            Color tint,
            int points,
            CubeClickOutcome outcome)
        {
            var definition = CreateInstance<CubeTypeDefinition>();
            definition.typeId = id;
            definition.displayName = display;
            definition.color = tint;
            definition.pointValue = Mathf.Max(0, points);
            definition.clickOutcome = outcome;
            return definition;
        }
    }
}
