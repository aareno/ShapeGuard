using UnityEngine;

namespace MeadowGuard
{
    public sealed class GameHud : MonoBehaviour
    {
        private GameController game;
        private GUIStyle title;
        private GUIStyle resource;
        private GUIStyle button;
        private GUIStyle panel;
        private GUIStyle center;

        private void Awake() => game = GetComponent<GameController>();

        private void BuildStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            resource = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            button = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 10, 10) };
            center = new GUIStyle(title) { alignment = TextAnchor.MiddleCenter, fontSize = 28 };
        }

        private void OnGUI()
        {
            BuildStyles();
            DrawTopBar();
            DrawBuildBar();
            DrawSelection();
            if (game.ShowAnnouncement)
            {
                var rect = new Rect(Screen.width * .5f - 260, 105, 520, 58);
                GUI.Box(rect, GUIContent.none, panel);
                GUI.Label(rect, game.Announcement, center);
            }
        }

        private void DrawTopBar()
        {
            GUI.Box(new Rect(12, 12, Screen.width - 24, 72), GUIContent.none, panel);
            GUI.Label(new Rect(30, 25, 210, 42), "MEADOW GUARD", title);
            GUI.Label(new Rect(245, 25, 145, 40), $"GOLD  {game.Gold}", resource);
            GUI.Label(new Rect(390, 25, 145, 40), $"ORE  {game.Ore}", resource);
            GUI.Label(new Rect(535, 25, 175, 40), $"CORE  {game.CoreHealth}/{game.CoreMaxHealth}", resource);
            GUI.Label(new Rect(710, 25, 150, 40), $"WAVE  {game.ActiveWave}", resource);

            var queuedLabel = game.IsChallenge ? "CHALLENGE ACTIVE" : game.ChallengeQueued ? $"WAVE {game.ClearedWave + 1} QUEUED" : $"START WAVE {game.ClearedWave + 1}";
            GUI.enabled = !game.ChallengeQueued && !game.IsChallenge;
            if (GUI.Button(new Rect(Screen.width - 230, 24, 198, 45), queuedLabel, button)) game.QueueNextWave();
            GUI.enabled = true;
        }

        private void DrawBuildBar()
        {
            var width = Mathf.Min(680, Screen.width - 24);
            var x = (Screen.width - width) * .5f;
            var y = Screen.height - 105;
            GUI.Box(new Rect(x, y, width, 90), GUIContent.none, panel);
            DrawBuildButton(new Rect(x + 14, y + 13, 200, 62), BuildingKind.Cannon, "Shoots monsters\nUpgrade: ORE");
            DrawBuildButton(new Rect(x + 240, y + 13, 200, 62), BuildingKind.GoldCollector, "Produces gold\nUpgrade: GOLD");
            DrawBuildButton(new Rect(x + 466, y + 13, 200, 62), BuildingKind.OreCollector, "Produces ore\nUpgrade: GOLD");
        }

        private void DrawBuildButton(Rect rect, BuildingKind kind, string detail)
        {
            var selected = game.PlacementKind == kind ? "  [PLACING]" : "";
            var label = $"{Balance.Name(kind)} — {Balance.PlaceCost(kind)} gold{selected}\n{detail}";
            if (GUI.Button(rect, label, button))
            {
                if (game.PlacementKind == kind) game.CancelPlacement();
                else game.BeginPlacement(kind);
            }
        }

        private void DrawSelection()
        {
            var selected = game.SelectedBuilding;
            if (selected == null) return;
            var rect = new Rect(18, Screen.height - 235, 275, 112);
            GUI.Box(rect, GUIContent.none, panel);
            GUI.Label(new Rect(rect.x + 12, rect.y + 8, 245, 26), $"{Balance.Name(selected.Kind)}  •  Level {selected.Level}", title);
            GUI.Label(new Rect(rect.x + 12, rect.y + 40, 245, 22), $"Upgrade cost: {selected.UpgradeCost} {selected.UpgradeCurrency}");
            if (GUI.Button(new Rect(rect.x + 12, rect.y + 67, 245, 34), "UPGRADE", button)) game.TryUpgradeSelected();
        }
    }
}
