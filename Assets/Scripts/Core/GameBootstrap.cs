using UnityEngine;

namespace MeadowGuard
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGame()
        {
            if (Object.FindAnyObjectByType<GameController>() != null) return;
            var root = new GameObject("Meadow Guard");
            root.AddComponent<GameController>();
        }
    }
}
