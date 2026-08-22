using UnityEngine;

namespace ShapeGuard
{
    public sealed class GameHud : MonoBehaviour
    {
        private GameController game;
        private GUIStyle panel;
        private GUIStyle button;
        private GUIStyle label;
        private GUIStyle title;
        private GUIStyle centered;

        private void Awake() => game = GetComponent<GameController>();

        private void CreateStyles()
        {
            if (panel != null) return;
            panel = new GUIStyle(GUI.skin.box) { normal = { background = Texture(GameBalance.Panel) } };
            button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = Texture(new Color(.13f, .18f, .16f)), textColor = GameBalance.Text },
                hover = { background = Texture(new Color(.20f, .28f, .24f)), textColor = GameBalance.Text },
                active = { background = Texture(GameBalance.Defense), textColor = GameBalance.Ground }
            };
            label = new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = GameBalance.Text } };
            title = new GUIStyle(label) { fontSize = 21, fontStyle = FontStyle.Bold };
            centered = new GUIStyle(title) { alignment = TextAnchor.MiddleCenter, fontSize = 24 };
        }

        private static Texture2D Texture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            CreateStyles();
            DrawTopBar();
            DrawBuildBar();
            DrawSelection();
            DrawPathTooltip();
            if (!game.ShowAnnouncement) return;
            var rect = new Rect(Screen.width * .5f - 260, 100, 520, 54);
            GUI.Box(rect, GUIContent.none, panel);
            GUI.Label(rect, game.Announcement, centered);
        }

        private void DrawTopBar()
        {
            GUI.Box(new Rect(12, 12, Screen.width - 24, 68), GUIContent.none, panel);
            GUI.Label(new Rect(28, 25, 170, 38), "SHAPE GUARD", title);
            GUI.Label(new Rect(205, 25, 120, 38), $"GOLD  {game.Gold}", label);
            GUI.Label(new Rect(325, 25, 110, 38), $"ORE  {game.Ore}", label);
            GUI.Label(new Rect(435, 25, 115, 38), $"CORE  {game.CoreHealth}/{game.MaxCoreHealth}", label);
            GUI.Label(new Rect(550, 25, 110, 38), game.HasStarted ? $"WAVE  {game.ActiveWave}" : "WAVE  READY", label);
            GUI.Label(new Rect(660, 25, 170, 38), $"PATHS {game.UnlockedPaths}/{GameBalance.PathNames.Length}  +{game.PathUnlocksAvailable}", label);

            var text = game.ProgressionActive ? "PROGRESSION ACTIVE" : game.ProgressionQueued
                ? $"WAVE {game.ClearedWave + 1} QUEUED" : $"START WAVE {game.ClearedWave + 1}";
            if (GUI.Button(new Rect(Screen.width - 326, 23, 94, 46), $"SPEED {game.GameSpeed:0}x", button)) game.CycleGameSpeed();
            GUI.enabled = !game.ProgressionActive && !game.ProgressionQueued;
            if (GUI.Button(new Rect(Screen.width - 220, 23, 192, 46), text, button)) game.StartOrQueueProgression();
            GUI.enabled = true;
        }

        private void DrawBuildBar()
        {
            const float width = 470;
            var x = (Screen.width - width) * .5f;
            var y = Screen.height - 102;
            GUI.Box(new Rect(x, y, width, 88), GUIContent.none, panel);
            DrawBuildButton(new Rect(x + 12, y + 12, 214, 64), BuildingType.TriangleDefense,
                "Attacks red circles\nUpgrades cost ore");
            DrawBuildButton(new Rect(x + 244, y + 12, 214, 64), BuildingType.OreCollector,
                "Generates ore\nUpgrades cost gold");
        }

        private void DrawBuildButton(Rect rect, BuildingType type, string detail)
        {
            var placing = game.PlacementType == type ? "  [PLACING]" : "";
            var text = $"{GameBalance.Name(type)} - {game.GetBuildCost(type)} {GameBalance.Currency(type)}{placing}\n{detail}";
            if (!GUI.Button(rect, text, button)) return;
            if (game.PlacementType == type) game.CancelPlacement();
            else game.BeginPlacement(type);
        }

        private void DrawSelection()
        {
            var selected = game.SelectedBuilding;
            if (selected == null) return;
            var rect = game.SelectionPanelRect();
            GUI.Box(rect, GUIContent.none, panel);
            GUI.Label(new Rect(rect.x + 12, rect.y + 8, 256, 27), $"{GameBalance.Name(selected.Type)}  L{selected.Level}", title);
            if (selected.Type == BuildingType.TriangleDefense)
            {
                GUI.Label(new Rect(rect.x + 12, rect.y + 40, 256, 20), $"Damage {selected.Damage:0}  |  DPS {selected.Dps:0.0}  |  Range {selected.Range:0.0}", label);
            }
            else GUI.Label(new Rect(rect.x + 12, rect.y + 40, 256, 20), $"Ore generation  {selected.OrePerSecond:0.00}/sec", label);
            GUI.Label(new Rect(rect.x + 12, rect.y + 62, 256, 20), $"Upgrade: {selected.UpgradeCost} {selected.UpgradeCurrency}", label);
            if (GUI.Button(new Rect(rect.x + 12, rect.y + 88, 256, 32), "UPGRADE", button)) game.UpgradeSelected();
        }

        private void DrawPathTooltip()
        {
            var index = game.HoveredPath;
            if (index < 0) return;
            var mouse = Event.current.mousePosition;
            const float width = 310;
            const float height = 92;
            var x = Mathf.Clamp(mouse.x + 18, 12, Screen.width - width - 12);
            var y = Mathf.Clamp(mouse.y + 18, 88, Screen.height - height - 112);
            var rect = new Rect(x, y, width, height);
            GUI.Box(rect, GUIContent.none, panel);
            GUI.Label(new Rect(x + 12, y + 7, width - 24, 27), GameBalance.PathNames[index], title);
            GUI.Label(new Rect(x + 12, y + 34, width - 24, 22), GameBalance.PathBonuses[index], label);

            string status;
            if (game.IsPathUnlocked(index)) status = "UNLOCKED";
            else if (game.CanUnlockPath(index)) status = "YELLOW - CLICK TO UNLOCK";
            else if (game.PathUnlocksAvailable <= 0)
            {
                var waves = 10 - game.ClearedWave % 10;
                status = $"LOCKED - NEXT PATH UNLOCK IN {waves} WAVES";
            }
            else
            {
                var parent = MapLayout.PathParents[index];
                status = parent >= 0 ? $"LOCKED - REQUIRES {GameBalance.PathNames[parent]}" : "LOCKED";
            }
            GUI.Label(new Rect(x + 12, y + 61, width - 24, 22), status, label);
        }
    }
}
