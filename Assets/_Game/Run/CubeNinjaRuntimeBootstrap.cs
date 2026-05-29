using UnityEngine;

namespace CubeNinja.Run
{
    public static class CubeNinjaRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRunDirector()
        {
            if (Object.FindFirstObjectByType<CubeNinjaRunDirector>() != null)
            {
                return;
            }

            var runtime = new GameObject("CubeNinja Runtime");
            runtime.AddComponent<CubeNinjaRunDirector>();
        }
    }
}
