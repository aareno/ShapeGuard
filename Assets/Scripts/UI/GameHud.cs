using UnityEngine;

namespace ShapeGuard
{
    public sealed class GameHud : MonoBehaviour
    {
        private const float CommandBarMaximumWidth = 600f;
        private const float FlowControlsWidth = 500f;
        private const float BuildControlWidth = 104f;
        private const float BottomBarGap = 12f;
        private GameController game;
        private GUIStyle panel;
        private GUIStyle button;
        private GUIStyle label;
        private GUIStyle title;
        private GUIStyle centered;
        private GUIStyle section;
        private GUIStyle upgradeReady;
        private GUIStyle statLabel;
        private GUIStyle statValue;
        private Font displayFont;
        private Texture2D gearIcon;
        private bool buildMenuOpen;
        private bool settingsOpen;
        private bool progressionOpen;
        private bool prestigeConfirmation;
        private Building sellConfirmationBuilding;
        private Rect buildButtonRect;
        private Vector2 buildScrollPosition;
        public bool IsBuildMenuOpen => buildMenuOpen;
        public bool IsSettingsOpen => settingsOpen;
        public bool IsProgressionOpen => progressionOpen;
        public bool IsSellConfirmationOpen => sellConfirmationBuilding != null;
        public Rect BuildMenuRect { get; private set; }

        private void Awake() => game = GetComponent<GameController>();

        private void CreateStyles()
        {
            if (panel != null) return;
            displayFont = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New", "monospace" }, 18);
            gearIcon = CreateGearIcon();
            panel = new GUIStyle(GUI.skin.box)
            {
                border = new RectOffset(1, 1, 1, 1),
                padding = new RectOffset(10, 10, 8, 8),
                normal = { background = FramedTexture(GameBalance.Panel, new Color(.24f, .24f, .22f, .9f)) }
            };
            button = new GUIStyle(GUI.skin.button)
            {
                font = displayFont,
                fontSize = 17,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(1, 1, 1, 1),
                normal = { background = FramedTexture(new Color(.055f, .06f, .06f, .98f), new Color(.28f, .29f, .27f)), textColor = GameBalance.Text },
                hover = { background = FramedTexture(new Color(.09f, .11f, .11f), GameBalance.Ore), textColor = Color.white },
                active = { background = FramedTexture(new Color(.13f, .16f, .16f), GameBalance.Gold), textColor = GameBalance.Gold }
            };
            label = new GUIStyle(GUI.skin.label)
            {
                font = displayFont,
                fontSize = 18,
                normal = { textColor = GameBalance.Text }
            };
            title = new GUIStyle(label) { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = new Color(.9f, .89f, .81f) } };
            centered = new GUIStyle(title) { alignment = TextAnchor.MiddleCenter, fontSize = 24 };
            section = new GUIStyle(label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(.57f, .59f, .56f) }
            };
            statLabel = new GUIStyle(section) { fontSize = 12 };
            statValue = new GUIStyle(label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(.94f, .93f, .86f) }
            };
            upgradeReady = new GUIStyle(button)
            {
                fontStyle = FontStyle.Bold,
                normal =
                {
                    background = FramedTexture(new Color(.12f, .11f, .055f, .98f), GameBalance.Gold),
                    textColor = GameBalance.Gold
                },
                hover =
                {
                    background = FramedTexture(new Color(.20f, .17f, .065f, 1f), new Color(1f, .92f, .55f)),
                    textColor = Color.white
                },
                active =
                {
                    background = FramedTexture(new Color(.28f, .22f, .07f, 1f), Color.white),
                    textColor = Color.white
                }
            };
        }

        private static Texture2D FramedTexture(Color fill, Color edge)
        {
            var texture = new Texture2D(3, 3) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 3; y++)
            for (var x = 0; x < 3; x++)
                texture.SetPixel(x, y, x == 0 || x == 2 || y == 0 || y == 2 ? edge : fill);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateGearIcon()
        {
            const int size = 36;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Settings Gear",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2((x + .5f) / size * 2f - 1f, (y + .5f) / size * 2f - 1f);
                var radius = point.magnitude;
                var angle = Mathf.Atan2(point.y, point.x);
                var tooth = Mathf.Cos(angle * 8f) > .25f;
                var outerRadius = tooth ? .88f : .68f;
                var filled = radius <= outerRadius && radius >= .25f;
                pixels[y * size + x] = filled ? GameBalance.Text : Color.clear;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            CreateStyles();
            if (sellConfirmationBuilding != null)
            {
                DrawSellConfirmation();
                return;
            }
            if (progressionOpen)
            {
                DrawProgression();
                return;
            }
            DrawTopBar();
            if (settingsOpen)
            {
                DrawSettings();
                return;
            }
            DrawCommandBar();
            DrawBuildControl();
            DrawBuildMenu();
            DrawFlowControls();
            DrawSelection();
            DrawPathTooltip();
            if (!game.ShowAnnouncement) return;
            var rect = new Rect(Screen.width * .5f - 260, 100, 520, 54);
            GUI.Box(rect, GUIContent.none, panel);
            GUI.Label(rect, game.Announcement, centered);
        }

        private void DrawTopBar()
        {
            GUI.Box(new Rect(12, 12, Screen.width - 24, 86), GUIContent.none, panel);
            var previousColor = GUI.color;
            GUI.color = new Color(GameBalance.Gold.r, GameBalance.Gold.g, GameBalance.Gold.b, .38f);
            GUI.DrawTexture(new Rect(24, 94, Screen.width - 48, 1), Texture2D.whiteTexture);
            GUI.color = previousColor;
            if (GUI.Button(new Rect(24, 29, 48, 48), new GUIContent(gearIcon, "Settings"), button))
            {
                if (settingsOpen) CloseSettings();
                else OpenSettings();
            }
            DrawStat(86, "SHARDS", FormatNumber(game.CoreShards), 120);
            DrawStat(218, "GOLD", FormatNumber(game.Gold), 112);
            DrawStat(330, "ORE", FormatNumber(game.Ore), 112);
            DrawStat(442, "CORE", $"{game.CoreHealth}/{game.MaxCoreHealth}", 112);
            DrawStat(554, game.IsBossWave ? "BOSS WAVE" : "WAVE", game.HasStarted ? game.ActiveWave.ToString() : "READY", 112);
            DrawStat(Screen.width - 230, "ENEMIES LEFT", game.EnemiesRemaining.ToString(), 190);
        }

        private void DrawStat(float x, string name, string value, float width)
        {
            GUI.Label(new Rect(x, 23, width, 20), name, statLabel);
            GUI.Label(new Rect(x, 43, width, 31), value, statValue);
        }

        private static string FormatNumber(long value)
        {
            if (value < 1000) return value.ToString();
            if (value < 1000000) return $"{value / 1000f:0.0}K";
            if (value < 1000000000) return $"{value / 1000000f:0.0}M";
            return $"{value / 1000000000f:0.0}B";
        }

        private void DrawCommandBar()
        {
            var width = Mathf.Min(CommandBarMaximumWidth,
                Screen.width - FlowControlsWidth - BuildControlWidth - BottomBarGap * 3 - 24);
            const float height = 108;
            const float x = 12;
            var y = Screen.height - height - 12;
            GUI.Box(new Rect(x, y, width, height), GUIContent.none, panel);

            GUI.Label(new Rect(x + 14, y + 5, width - 28, 20), "SKILLS", section);
            var skillWidth = Mathf.Min(130, (width - 42) * .5f);
            DrawAbilityButton(new Rect(x + 14, y + 28, skillWidth, 66), "ARC",
                game.CanUseArcBurst, game.ArcBurstCooldownRemaining, game.ActivateArcBurst);
            DrawAbilityButton(new Rect(x + 28 + skillWidth, y + 28, skillWidth, 66), "REPAIR",
                game.CanUseCoreRepair, game.CoreRepairCooldownRemaining, game.ActivateCoreRepair);
            var progressionX = x + 42 + skillWidth * 2;
            var progressionWidth = Mathf.Max(100, width - (progressionX - x) - 14);
            if (GUI.Button(new Rect(progressionX, y + 28, progressionWidth, 66),
                    $"CORE\n{game.CoreShards} SHARDS", upgradeReady))
                OpenProgression();
        }

        private void DrawBuildMenu()
        {
            if (!buildMenuOpen)
            {
                BuildMenuRect = Rect.zero;
                return;
            }

            const float width = FlowControlsWidth;
            var x = Screen.width - width - 12;
            const float y = 108;
            var height = Mathf.Max(220, Screen.height - y - 132);
            BuildMenuRect = new Rect(x, y, width, height);
            GUI.Box(BuildMenuRect, GUIContent.none, panel);
            GUI.Label(new Rect(x + 16, y + 10, width - 32, 28), "BUILDINGS", title);

            var types = (BuildingType[])System.Enum.GetValues(typeof(BuildingType));
            var contentHeight = Mathf.Max(height - 54, types.Length * 82 + 8);
            var viewport = new Rect(x + 12, y + 46, width - 24, height - 58);
            buildScrollPosition = GUI.BeginScrollView(viewport, buildScrollPosition,
                new Rect(0, 0, viewport.width - 18, contentHeight), false, true);
            for (var index = 0; index < types.Length; index++)
                DrawBuildButton(new Rect(4, 4 + index * 82, viewport.width - 30, 70), types[index]);
            GUI.EndScrollView();

            var current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 &&
                !BuildMenuRect.Contains(current.mousePosition) && !buildButtonRect.Contains(current.mousePosition))
                buildMenuOpen = false;
        }

        private void DrawBuildControl()
        {
            const float width = BuildControlWidth;
            const float height = 108;
            var gamePanelX = Screen.width - FlowControlsWidth - 12;
            var x = gamePanelX - width - BottomBarGap;
            var y = Screen.height - height - 12;
            GUI.Box(new Rect(x, y, width, height), GUIContent.none, panel);

            buildButtonRect = new Rect(x + 10, y + 10, width - 20, height - 20);
            var buildText = game.PlacementType.HasValue ? "CANCEL" : buildMenuOpen ? "CLOSE" : "BUILD";
            if (!GUI.Button(buildButtonRect, buildText, button)) return;
            if (game.PlacementType.HasValue)
            {
                game.CancelPlacement();
                buildMenuOpen = false;
            }
            else buildMenuOpen = !buildMenuOpen;
        }

        private void DrawFlowControls()
        {
            const float width = FlowControlsWidth;
            const float height = 108;
            var x = Screen.width - width - 12;
            var y = Screen.height - height - 12;
            GUI.Box(new Rect(x, y, width, height), GUIContent.none, panel);
            GUI.Label(new Rect(x + 14, y + 5, width - 28, 20), "GAME", section);

            if (GUI.Button(new Rect(x + 14, y + 28, 72, 66), $"SPEED\n{game.GameSpeed:0}x", button))
                game.CycleGameSpeed();

            var text = game.ProgressionActive ? $"WAVE {game.ActiveWave} ACTIVE" : game.ProgressionQueued
                ? $"WAVE {game.ClearedWave + 1} QUEUED" : $"START WAVE {game.ClearedWave + 1}";
            GUI.enabled = !game.ProgressionActive && !game.ProgressionQueued;
            if (GUI.Button(new Rect(x + 98, y + 28, 388, 66), text, button)) game.StartOrQueueProgression();
            GUI.enabled = true;
        }

        private void OpenSettings()
        {
            settingsOpen = true;
            buildMenuOpen = false;
            game.CancelPlacement();
            game.PlaySound(GameSound.Select, .7f);
        }

        private void OpenProgression()
        {
            progressionOpen = true;
            prestigeConfirmation = false;
            buildMenuOpen = false;
            game.CancelPlacement();
            game.PlaySound(GameSound.Select, .7f);
        }

        private void CloseProgression()
        {
            progressionOpen = false;
            prestigeConfirmation = false;
            game.PlaySound(GameSound.Select, .7f);
        }

        private void DrawProgression()
        {
            if (prestigeConfirmation)
            {
                DrawPrestigeConfirmation();
                return;
            }
            var current = Event.current;
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                CloseProgression();
                current.Use();
                return;
            }

            var oldColor = GUI.color;
            GUI.color = new Color(0, 0, 0, .76f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = oldColor;

            const float width = 780f;
            const float height = 650f;
            var x = Mathf.Max(12f, (Screen.width - width) * .5f);
            var y = Mathf.Max(12f, (Screen.height - height) * .5f);
            var rect = new Rect(x, y, Mathf.Min(width, Screen.width - 24f), Mathf.Min(height, Screen.height - 24f));
            GUI.Box(rect, GUIContent.none, panel);
            GUI.Label(new Rect(rect.x + 22, rect.y + 14, rect.width - 44, 34), "CORE PROGRESSION", title);
            GUI.Label(new Rect(rect.x + 22, rect.y + 50, rect.width - 44, 24),
                $"{game.CoreShards} CORE SHARDS   •   BEST BOSS {game.HighestBossWave}   •   REBOOTS {game.PrestigeCount}", label);

            GUI.Label(new Rect(rect.x + 22, rect.y + 84, rect.width - 44, 20), "PERMANENT UPGRADES", section);
            var cardWidth = (rect.width - 54) * .5f;
            DrawPermanentUpgrade(new Rect(rect.x + 22, rect.y + 108, cardWidth, 58), 0,
                "CORE POWER", $"LEVEL {game.PermanentPowerLevel}  •  +10% DEFENSE POWER");
            DrawPermanentUpgrade(new Rect(rect.x + 32 + cardWidth, rect.y + 108, cardWidth, 58), 1,
                "INDUSTRY", $"LEVEL {game.PermanentEconomyLevel}  •  +10% GOLD AND ORE");
            DrawPermanentUpgrade(new Rect(rect.x + 22, rect.y + 176, cardWidth, 58), 2,
                "CORE PLATING", $"LEVEL {game.PermanentCoreLevel}  •  +2 CORE HEALTH");
            DrawPermanentUpgrade(new Rect(rect.x + 32 + cardWidth, rect.y + 176, cardWidth, 58), 3,
                "IDLE STORAGE", $"LEVEL {game.OfflineLevel}/2  •  {game.OfflineHourCap}H OFFLINE CAP");

            GUI.Label(new Rect(rect.x + 22, rect.y + 252, rect.width - 44, 20), "AUTOMATION", section);
            DrawAutomationButton(new Rect(rect.x + 22, rect.y + 276, cardWidth, 54), 0,
                "AUTO-ADVANCE", game.AutoWaveUnlocked, game.AutoWaveEnabled);
            DrawAutomationButton(new Rect(rect.x + 32 + cardWidth, rect.y + 276, cardWidth, 54), 1,
                "AUTO-UPGRADE CHEAPEST", game.AutoUpgradeUnlocked, game.AutoUpgradeEnabled);
            if (GUI.Button(new Rect(rect.x + 22, rect.y + 340, cardWidth, 50),
                    $"STOP BEFORE BOSS\n{(game.StopBeforeBoss ? "ON" : "OFF")}", button))
                game.ToggleAutomation(2);
            GUI.Label(new Rect(rect.x + 32 + cardWidth, rect.y + 344, cardWidth, 42),
                "Bosses appear every 10 waves. Major bosses appear every 50.", section);

            GUI.Label(new Rect(rect.x + 22, rect.y + 406, rect.width - 44, 20), "LAYOUT BLUEPRINT", section);
            if (GUI.Button(new Rect(rect.x + 22, rect.y + 430, cardWidth, 48), "SAVE CURRENT LAYOUT", button))
                game.CaptureLayout();
            GUI.enabled = game.HasSavedLayout;
            if (GUI.Button(new Rect(rect.x + 32 + cardWidth, rect.y + 430, cardWidth, 48),
                    game.HasSavedLayout ? "RESTORE AFFORDABLE BUILDINGS" : "NO SAVED LAYOUT", button))
                game.RestoreLayout();
            GUI.enabled = true;

            GUI.Label(new Rect(rect.x + 22, rect.y + 494, rect.width - 44, 38),
                "Defense mastery unlocks by exploring that defense's path and reaching the required tower level. " +
                "Purchase all three ranks with Ore to choose an evolution.", section);

            GUI.enabled = game.CanPrestige;
            var prestigeText = game.CanPrestige
                ? $"REBOOT CORE  •  +{game.PrestigeReward} SHARDS"
                : $"REBOOT UNLOCKS AT WAVE {GameBalance.PrestigeUnlockWave}";
            if (GUI.Button(new Rect(rect.x + 22, rect.y + rect.height - 72, rect.width - 188, 50),
                    prestigeText, upgradeReady)) prestigeConfirmation = true;
            GUI.enabled = true;
            if (GUI.Button(new Rect(rect.x + rect.width - 154, rect.y + rect.height - 72, 132, 50),
                    "CLOSE", button)) CloseProgression();
        }

        private void DrawPermanentUpgrade(Rect rect, int upgrade, string name, string detail)
        {
            var maxed = upgrade == 3 && game.OfflineLevel >= 2;
            var cost = game.PermanentUpgradeCost(upgrade);
            GUI.enabled = !maxed && game.CoreShards >= cost;
            if (GUI.Button(rect, $"{name}  •  {(maxed ? "MAX" : cost + " SHARDS")}\n{detail}", button))
                game.BuyPermanentUpgrade(upgrade);
            GUI.enabled = true;
        }

        private void DrawAutomationButton(Rect rect, int automation, string name, bool unlocked, bool enabled)
        {
            var cost = game.AutomationUnlockCost(automation);
            GUI.enabled = unlocked || game.CoreShards >= cost;
            var state = unlocked ? enabled ? "ON" : "OFF" : $"UNLOCK {cost} SHARDS";
            if (GUI.Button(rect, $"{name}\n{state}", button))
            {
                if (unlocked) game.ToggleAutomation(automation);
                else game.UnlockAutomation(automation);
            }
            GUI.enabled = true;
        }

        private void DrawPrestigeConfirmation()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                prestigeConfirmation = false;
                Event.current.Use();
                return;
            }
            GUI.color = new Color(0, 0, 0, .82f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            const float width = 520f;
            const float height = 260f;
            var rect = new Rect((Screen.width - width) * .5f, (Screen.height - height) * .5f, width, height);
            GUI.Box(rect, GUIContent.none, panel);
            GUI.Label(new Rect(rect.x + 24, rect.y + 20, rect.width - 48, 34), "REBOOT THE CORE?", centered);
            GUI.Label(new Rect(rect.x + 28, rect.y + 67, rect.width - 56, 72),
                $"Reset waves, resources, paths, buildings, and their mastery ranks. Keep blueprints, automation, " +
                $"layouts, and permanent Core upgrades.\n\nREWARD: {game.PrestigeReward} CORE SHARDS", label);
            if (GUI.Button(new Rect(rect.x + 28, rect.y + 190, 210, 46), "CANCEL", button))
                prestigeConfirmation = false;
            if (!GUI.Button(new Rect(rect.x + rect.width - 238, rect.y + 190, 210, 46), "CONFIRM REBOOT", upgradeReady))
                return;
            CloseProgression();
            game.Prestige();
        }

        private void CloseSettings()
        {
            settingsOpen = false;
            game.PlaySound(GameSound.Select, .7f);
        }

        private void DrawSettings()
        {
            var current = Event.current;
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                CloseSettings();
                current.Use();
                return;
            }

            var oldColor = GUI.color;
            GUI.color = new Color(0, 0, 0, .72f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = oldColor;

            const float width = 640f;
            const float height = 510f;
            var x = Mathf.Max(12f, (Screen.width - width) * .5f);
            var y = Mathf.Max(12f, (Screen.height - height) * .5f);
            var rect = new Rect(x, y, Mathf.Min(width, Screen.width - 24f), Mathf.Min(height, Screen.height - 24f));
            GUI.Box(rect, GUIContent.none, panel);
            GUI.Label(new Rect(rect.x + 22, rect.y + 16, rect.width - 44, 34), "SETTINGS", title);
            GUI.Label(new Rect(rect.x + 22, rect.y + 53, rect.width - 44, 20), "AUDIO", section);

            GUI.Label(new Rect(rect.x + 22, rect.y + 80, 150, 28), "MASTER VOLUME", label);
            var volume = GUI.HorizontalSlider(new Rect(rect.x + 184, rect.y + 87, rect.width - 300, 22),
                GameSettings.MasterVolume, 0f, 1f);
            if (!Mathf.Approximately(volume, GameSettings.MasterVolume)) GameSettings.SetMasterVolume(volume);
            GUI.Label(new Rect(rect.x + rect.width - 96, rect.y + 80, 70, 28),
                $"{GameSettings.MasterVolume * 100f:0}%", label);

            GUI.Label(new Rect(rect.x + 22, rect.y + 126, rect.width - 44, 20), "DISPLAY MODE", section);
            var modeWidth = (rect.width - 60) / 3f;
            DrawDisplayModeButton(new Rect(rect.x + 22, rect.y + 153, modeWidth, 42),
                "FULLSCREEN", FullScreenMode.ExclusiveFullScreen);
            DrawDisplayModeButton(new Rect(rect.x + 30 + modeWidth, rect.y + 153, modeWidth, 42),
                "BORDERLESS", FullScreenMode.FullScreenWindow);
            DrawDisplayModeButton(new Rect(rect.x + 38 + modeWidth * 2, rect.y + 153, modeWidth, 42),
                "WINDOWED", FullScreenMode.Windowed);

            GUI.Label(new Rect(rect.x + 22, rect.y + 216, rect.width - 44, 20), "RESOLUTION", section);
            if (GUI.Button(new Rect(rect.x + 22, rect.y + 243, 54, 42), "<", button)) GameSettings.CycleResolution(-1);
            GUI.Box(new Rect(rect.x + 84, rect.y + 243, rect.width - 168, 42), GUIContent.none, panel);
            GUI.Label(new Rect(rect.x + 84, rect.y + 248, rect.width - 168, 32), GameSettings.ResolutionLabel, centered);
            if (GUI.Button(new Rect(rect.x + rect.width - 76, rect.y + 243, 54, 42), ">", button))
                GameSettings.CycleResolution(1);

            GUI.Label(new Rect(rect.x + 22, rect.y + 307, rect.width - 44, 20), "PERFORMANCE", section);
            if (GUI.Button(new Rect(rect.x + 22, rect.y + 334, (rect.width - 52) * .5f, 48),
                    $"V-SYNC\n{(GameSettings.VSync ? "ON" : "OFF")}", button)) GameSettings.ToggleVSync();
            if (GUI.Button(new Rect(rect.x + 30 + (rect.width - 52) * .5f, rect.y + 334,
                    (rect.width - 52) * .5f, 48), $"FRAME LIMIT\n{GameSettings.FrameRateLabel}", button))
                GameSettings.CycleFrameRate();

            GUI.Label(new Rect(rect.x + 22, rect.y + 393, rect.width - 44, 22),
                GameSettings.VSync ? "Frame limit is controlled by your display while V-Sync is on." :
                "V-Sync is off; the selected frame limit is active.", section);
            if (GUI.Button(new Rect(rect.x + 22, rect.y + 438, 190, 48), "RESET DEFAULTS", button))
                GameSettings.ResetDefaults();
            if (GUI.Button(new Rect(rect.x + rect.width - 172, rect.y + 438, 150, 48), "CLOSE", upgradeReady))
                CloseSettings();
        }

        private void DrawDisplayModeButton(Rect rect, string text, FullScreenMode mode)
        {
            if (GUI.Button(rect, GameSettings.DisplayMode == mode ? $"{text}\nSELECTED" : text,
                    GameSettings.DisplayMode == mode ? upgradeReady : button))
                GameSettings.SetDisplayMode(mode);
        }

        private void DrawAbilityButton(Rect rect, string name, bool available, float cooldown,
            System.Action activate)
        {
            GUI.enabled = available;
            var status = cooldown > 0 ? $"{cooldown:0}s" : available ? "READY" : "LOCKED";
            if (GUI.Button(rect, $"{name}\n{status}", button)) activate();
            GUI.enabled = true;
        }

        private static void DrawDivider(float x, float y, float height)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(.45f, .45f, .40f, .5f);
            GUI.DrawTexture(new Rect(x, y, 1, height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void DrawSelection()
        {
            var selected = game.SelectedBuilding;
            if (selected == null) return;
            var rect = game.SelectionPanelRect();
            GUI.Box(rect, GUIContent.none, panel);
            GUI.Label(new Rect(rect.x + 16, rect.y + 10, rect.width - 32, 24), "STRUCTURE CONTROL", section);
            GUI.Label(new Rect(rect.x + 16, rect.y + 35, rect.width - 32, 30),
                $"{GameBalance.Name(selected.Type)}  L{selected.Level}", title);

            var defense = GameBalance.IsDefense(selected.Type);
            var pathCount = defense ? game.UnlockedDefensePathCount(selected.Type) : 0;
            var evolution = selected.Evolution;
            var evolutionText = evolution > 0 ? $"  •  {GameBalance.EvolutionName(selected.Type, evolution)}" : "";
            GUI.Label(new Rect(rect.x + 16, rect.y + 65, rect.width - 32, 19),
                defense ? $"MASTERY {selected.MasteryRank}/3  •  PATH NODES {pathCount}{evolutionText}" :
                    "ECONOMY STRUCTURE  •  NO DEFENSE MASTERY", section);

            var stats = selected.Type switch
            {
                BuildingType.OreCollector => $"ORE GENERATION   {selected.OrePerSecond:0.00}/SEC",
                BuildingType.SupportDefense => $"DAMAGE BOOST   +{selected.SupportBoost * 100f:0}%   RANGE {selected.Range:0.0}",
                _ => $"DAMAGE {selected.Damage:0}   DPS {selected.Dps:0.0}   RANGE {selected.Range:0.0}"
            };
            GUI.Label(new Rect(rect.x + 16, rect.y + 84, rect.width - 32, 24), stats, label);
            GUI.enabled = selected.CanAffordUpgrade;
            var upgradeText = selected.CanAffordUpgrade
                ? $"UPGRADE  -  {selected.UpgradeCost} {selected.UpgradeCurrency.ToUpperInvariant()}"
                : $"NEED {selected.UpgradeCost} {selected.UpgradeCurrency.ToUpperInvariant()}";
            const float sellWidth = 112f;
            var upgradeWidth = rect.width - 32 - sellWidth - 8f;
            if (GUI.Button(new Rect(rect.x + 16, rect.y + 116, upgradeWidth, 34), upgradeText,
                    selected.CanAffordUpgrade ? upgradeReady : button))
                game.UpgradeSelected();
            GUI.enabled = true;
            if (GUI.Button(new Rect(rect.x + rect.width - 16 - sellWidth, rect.y + 116, sellWidth, 34),
                    $"SELL +{game.GetSellRefund(selected)}", button))
            {
                sellConfirmationBuilding = selected;
                buildMenuOpen = false;
            }

            if (!defense) return;
            if (selected.MasteryRank < 3)
            {
                var status = pathCount < selected.MasteryRequiredPaths
                    ? $"REQUIRES {selected.MasteryRequiredPaths} PATH NODES"
                    : selected.Level < selected.MasteryRequiredLevel
                        ? $"REQUIRES DEFENSE LEVEL {selected.MasteryRequiredLevel}"
                        : selected.CanUpgradeMastery ? "READY" : $"NEED {selected.MasteryCost} ORE";
                if (GUI.Button(new Rect(rect.x + 16, rect.y + 158, rect.width - 32, 42),
                        $"UNLOCK MASTERY {selected.NextMasteryRank}  •  {selected.MasteryCost} ORE\n{status}",
                        selected.CanUpgradeMastery ? upgradeReady : button))
                    game.UpgradeSelectedMastery();
                return;
            }
            if (evolution != 0)
            {
                GUI.Label(new Rect(rect.x + 16, rect.y + 164, rect.width - 32, 30),
                    GameBalance.EvolutionBonus(selected.Type, evolution), centered);
                return;
            }
            var evolutionWidth = (rect.width - 40) * .5f;
            if (GUI.Button(new Rect(rect.x + 16, rect.y + 158, evolutionWidth, 42),
                    $"{GameBalance.EvolutionName(selected.Type, 1)}\n{GameBalance.EvolutionBonus(selected.Type, 1)}", upgradeReady))
                game.ChooseSelectedEvolution(1);
            if (GUI.Button(new Rect(rect.x + 24 + evolutionWidth, rect.y + 158, evolutionWidth, 42),
                    $"{GameBalance.EvolutionName(selected.Type, 2)}\n{GameBalance.EvolutionBonus(selected.Type, 2)}", button))
                game.ChooseSelectedEvolution(2);
        }

        private void DrawSellConfirmation()
        {
            if (sellConfirmationBuilding == null || game.SelectedBuilding != sellConfirmationBuilding)
            {
                sellConfirmationBuilding = null;
                return;
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                sellConfirmationBuilding = null;
                Event.current.Use();
                return;
            }

            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, .72f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;

            const float width = 440f;
            const float height = 210f;
            var rect = new Rect((Screen.width - width) * .5f, (Screen.height - height) * .5f, width, height);
            var refund = game.GetSellRefund(sellConfirmationBuilding);
            var currency = sellConfirmationBuilding.UpgradeCurrency.ToUpperInvariant();

            GUI.Box(rect, GUIContent.none, panel);
            GUI.Label(new Rect(rect.x + 24, rect.y + 20, rect.width - 48, 34), "CONFIRM SALE", centered);
            GUI.Label(new Rect(rect.x + 24, rect.y + 65, rect.width - 48, 28),
                $"Sell {GameBalance.Name(sellConfirmationBuilding.Type)} L{sellConfirmationBuilding.Level}?", centered);
            GUI.Label(new Rect(rect.x + 24, rect.y + 99, rect.width - 48, 26),
                $"YOU WILL RECEIVE {refund} {currency}", section);

            if (GUI.Button(new Rect(rect.x + 24, rect.y + 144, 184, 44), "CANCEL", button))
            {
                sellConfirmationBuilding = null;
                return;
            }

            if (!GUI.Button(new Rect(rect.x + rect.width - 208, rect.y + 144, 184, 44), "CONFIRM SELL", upgradeReady))
                return;
            sellConfirmationBuilding = null;
            game.SellSelected();
        }

        private void DrawBuildButton(Rect rect, BuildingType type)
        {
            var name = GameBalance.Name(type).ToUpperInvariant();
            var unlocked = game.IsBuildingUnlocked(type);
            var placing = game.PlacementType == type ? "\nPLACING" : "";
            var path = GameBalance.BuildingUnlockPath(type);
            var status = unlocked
                ? $"{game.GetBuildCost(type)} {GameBalance.Currency(type).ToUpperInvariant()}{placing}"
                : $"LOCKED • {GameBalance.PathNames[path].ToUpperInvariant()}";
            var text = $"{name}\n{GameBalance.Role(type)}\n{status}";
            GUI.enabled = unlocked;
            var clicked = GUI.Button(rect, text, button);
            GUI.enabled = true;
            if (!clicked) return;
            if (game.PlacementType == type) game.CancelPlacement();
            else game.BeginPlacement(type);
            buildMenuOpen = false;
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
                status = $"LOCKED - NEXT PATH UNLOCK IN {game.WavesUntilNextPathUnlock} WAVES";
            }
            else
            {
                var parent = MapLayout.PathParents[index];
                status = parent >= 0 ? $"LOCKED - REQUIRES {GameBalance.PathNames[parent]}" : "LOCKED";
            }
            GUI.Label(new Rect(x + 12, y + 61, width - 24, 22), status, label);
        }
    }

    /// <summary>Subtle low-fi display texture behind the HUD, inspired by sixth-generation console output.</summary>
    public sealed class RetroScreenFx : MonoBehaviour
    {
        private Texture2D grain;
        private Texture2D vignette;

        private void Awake()
        {
            grain = CreateGrain();
            vignette = CreateVignette();
        }

        private void OnDestroy()
        {
            if (grain != null) Destroy(grain);
            if (vignette != null) Destroy(vignette);
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || grain == null || vignette == null) return;
            var oldDepth = GUI.depth;
            GUI.depth = 100;
            var oldColor = GUI.color;
            GUI.color = Color.white;
            var tilesX = Screen.width / 192f;
            var tilesY = Screen.height / 192f;
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, Screen.width, Screen.height), grain,
                new Rect(0, 0, tilesX, tilesY), true);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignette, ScaleMode.StretchToFill, true);
            GUI.color = oldColor;
            GUI.depth = oldDepth;
        }

        private static Texture2D CreateGrain()
        {
            const int size = 192;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Retro Dither and Scanlines",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            var random = new System.Random(1989);
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var scanline = (y & 1) == 0 ? .018f : 0f;
                var speck = random.NextDouble() > .82 ? .018f : 0f;
                pixels[y * size + x] = new Color(.55f, .50f, .39f, scanline + speck);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateVignette()
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Retro Edge Vignette",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var px = (x + .5f) / size * 2f - 1f;
                var py = (y + .5f) / size * 2f - 1f;
                var edge = Mathf.Clamp01((px * px + py * py - .42f) / 1.25f);
                pixels[y * size + x] = new Color(0, 0, 0, edge * .34f);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
