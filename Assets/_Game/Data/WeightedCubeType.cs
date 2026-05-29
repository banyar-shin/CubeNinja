using System;
using UnityEngine;

namespace CubeNinja.Data
{
    [Serializable]
    public struct WeightedCubeType
    {
        [SerializeField] private CubeTypeDefinition cubeType;
        [SerializeField] private float weight;

        public WeightedCubeType(CubeTypeDefinition cubeType, float weight)
        {
            this.cubeType = cubeType;
            this.weight = weight;
        }

        public CubeTypeDefinition CubeType => cubeType;
        public float Weight => Mathf.Max(0f, weight);
    }
}
