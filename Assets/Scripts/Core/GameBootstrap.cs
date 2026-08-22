using UnityEngine;

namespace ShapeGuard
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (Object.FindAnyObjectByType<GameController>() != null) return;
            new GameObject("Shape Guard").AddComponent<GameController>();
        }
    }
}
