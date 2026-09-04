using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShapeGuard
{
    public sealed class GameController : MonoBehaviour
    {
        private const int SaveVersion = 2;
        private const float AutosaveInterval = 5f;
        private const float HoldToMoveDelay = .35f;
        private const float ArcBurstCooldown = 18f;
        private const float CoreRepairCooldown = 32f;

        [Serializable]
        private sealed class SaveData
        {
            public int version = SaveVersion;
            public int gold;
            public int ore;
            public int clearedWave;
            public float gameSpeed;
            public List<int> unlockedPaths = new();
            public List<BuildingSaveData> buildings = new();
            public List<BuildingSaveData> savedLayout = new();
            public List<int> unlockedBuildingTypes = new();
            public int coreShards;
            public int highestBossWave;
            public int bossesDefeated;
            public int prestigeCount;
            public int permanentPowerLevel;
            public int permanentEconomyLevel;
            public int permanentCoreLevel;
            public int offlineLevel;
            public bool autoWaveUnlocked;
            public bool autoUpgradeUnlocked;
            public bool autoWaveEnabled;
            public bool autoUpgradeEnabled;
            public bool stopBeforeBoss;
            public long lastSavedUnix;
        }

        [Serializable]
        private sealed class BuildingSaveData
        {
            public BuildingType type;
            public int level;
            public int masteryRank;
            public int evolution;
            public float x;
            public float y;
        }

        public int Gold { get; private set; } = GameBalance.StartingGold;
        public int Ore { get; private set; } = GameBalance.StartingOre;
        public int ClearedWave { get; private set; }
        public int ActiveWave { get; private set; }
        public int CoreHealth { get; private set; } = GameBalance.CoreHealth;
        public int UnlockedPaths
        {
            get
            {
                var count = 0;
                foreach (var unlocked in pathUnlocked) if (unlocked) count++;
                return count;
            }
        }
        public int PathUnlocksAvailable => Mathf.Clamp(
            ClearedWave / GameBalance.PathUnlockInterval - (UnlockedPaths - 1), 0,
            pathUnlocked.Length - UnlockedPaths);
        public int WavesUntilNextPathUnlock
        {
            get
            {
                var remainder = ClearedWave % GameBalance.PathUnlockInterval;
                return remainder == 0 ? GameBalance.PathUnlockInterval : GameBalance.PathUnlockInterval - remainder;
            }
        }
        public int MaxCoreHealth => GameBalance.CoreHealth + PermanentCoreLevel * 2;
        public float DefenseDamageMultiplier => (IsPathUnlocked(1) ? 1.2f : 1f) *
            (1f + PermanentPowerLevel * .1f);
        public float OreAmountMultiplier => (IsPathUnlocked(2) ? 1.25f : 1f) *
            (1f + PermanentEconomyLevel * .1f);
        public float DefenseRangeMultiplier => IsPathUnlocked(3) ? 1.2f : 1f;
        public float DefenseFireIntervalMultiplier => 1f;
        public float GoldRewardMultiplier => 1f + PermanentEconomyLevel * .1f;
        public float DefenseCostMultiplier => 1f;
        public float OreIntervalMultiplier => 1f;
        public float CollectorCostMultiplier => 1f;
        public bool HasStarted { get; private set; }
        public bool ProgressionActive { get; private set; }
        public bool ProgressionQueued { get; private set; }
        public bool IsTransitioning { get; private set; }
        public float GameSpeed { get; private set; } = GameBalance.DefaultGameSpeed;
        public string Announcement { get; private set; }
        public bool ShowAnnouncement => announcementTimer > 0;
        public int HoveredPath { get; private set; } = -1;
        public Building SelectedBuilding { get; private set; }
        public BuildingType? PlacementType { get; private set; }
        public int EnemiesAlive => enemies.Count;
        public int EnemiesRemaining => enemies.Count + Mathf.Max(0, spawnRemaining);
        public float ArcBurstCooldownRemaining => arcBurstCooldown;
        public float CoreRepairCooldownRemaining => coreRepairCooldown;
        public bool CanUseArcBurst => HasStarted && !IsTransitioning && enemies.Count > 0 && arcBurstCooldown <= 0;
        public bool CanUseCoreRepair => HasStarted && !IsTransitioning && CoreHealth < MaxCoreHealth && coreRepairCooldown <= 0;
        public int CoreShards { get; private set; }
        public int HighestBossWave { get; private set; }
        public int BossesDefeated { get; private set; }
        public int PrestigeCount { get; private set; }
        public int PermanentPowerLevel { get; private set; }
        public int PermanentEconomyLevel { get; private set; }
        public int PermanentCoreLevel { get; private set; }
        public int OfflineLevel { get; private set; }
        public bool AutoWaveUnlocked { get; private set; }
        public bool AutoUpgradeUnlocked { get; private set; }
        public bool AutoWaveEnabled { get; private set; }
        public bool AutoUpgradeEnabled { get; private set; }
        public bool StopBeforeBoss { get; private set; }
        public bool IsBossWave => GameBalance.IsBossWave(ActiveWave);
        public bool CanPrestige => ClearedWave >= GameBalance.PrestigeUnlockWave;
        public int PrestigeReward => GameBalance.PrestigeShardReward(ClearedWave);
        public int OfflineHourCap => 4 + Mathf.Min(2, OfflineLevel) * 4;
        public int PendingOfflineOre { get; private set; }
        public bool HasSavedLayout => savedLayout.Count > 0;

        private readonly List<Vector3[]> paths = new();
        private readonly List<LineRenderer> pathLines = new();
        private readonly List<LineRenderer> pathGlows = new();
        private readonly List<SpriteRenderer> pathNodes = new();
        private readonly List<SpriteRenderer> pathNodeGlyphs = new();
        private readonly List<SpriteRenderer> pathNodeHalos = new();
        private readonly List<float> pathNodeHaloScales = new();
        private readonly List<GameObject> pathPortals = new();
        private readonly bool[] pathUnlocked = new bool[GameBalance.PathNames.Length];
        private readonly List<int> frontierPaths = new();
        private readonly List<Enemy> enemies = new();
        private readonly List<Building> buildings = new();
        private readonly List<(SpriteRenderer renderer, Color color)> previewParts = new();
        private readonly bool[] permanentBlueprints = new bool[Enum.GetValues(typeof(BuildingType)).Length];
        private readonly List<BuildingSaveData> savedLayout = new();
        private Camera gameCamera;
        private GameHud gameHud;
        private GameAudio gameAudio;
        private Rect visualBounds;
        private GameObject placementPreview;
        private int spawnRemaining;
        private int spawnedCount;
        private float spawnTimer;
        private float transitionTimer;
        private float announcementTimer;
        private bool cameraDragging;
        private Vector2 lastDragPosition;
        private Building pressedBuilding;
        private Building movingBuilding;
        private Vector3 moveOrigin;
        private float buildingHoldTimer;
        private bool saveDirty;
        private float autosaveTimer;
        private float arcBurstCooldown;
        private float coreRepairCooldown;
        private float automationTimer;
        private float waveElapsed;
        private bool bossDefeatedThisWave;
        private int lastBossShardReward;

        private static string SavePath => Path.Combine(Application.persistentDataPath, "shape-guard-save.json");

        private void Awake()
        {
            GameSettings.LoadAndApply();
            QualitySettings.antiAliasing = 0;
            pathUnlocked[0] = true;
            Time.timeScale = GameSpeed;
            SetupCamera();
            gameAudio = gameObject.AddComponent<GameAudio>();
            gameObject.AddComponent<RetroScreenFx>();
            CreateMap();
            var loaded = LoadProgress();
            gameHud = gameObject.AddComponent<GameHud>();
            if (loaded)
            {
                StartWave(AutoWaveEnabled);
                if (PendingOfflineOre > 0)
                    Announce($"WELCOME BACK - +{PendingOfflineOre} OFFLINE ORE", 4f);
            }
            else Announce("Build, then start Wave 1", 5f);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveProgress();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) SaveProgress();
        }

        private void OnApplicationQuit()
        {
            SaveProgress();
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        public void CycleGameSpeed()
        {
            GameSpeed = GameSpeed < 1.5f ? 2f : GameSpeed < 2.5f ? 3f : 1f;
            Time.timeScale = GameSpeed;
            gameAudio?.Play(GameSound.Select, .75f);
            saveDirty = true;
            SaveProgress();
        }

        private void SetupCamera()
        {
            gameCamera = Camera.main;
            if (gameCamera == null)
            {
                var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
                gameCamera = cameraObject.AddComponent<Camera>();
            }
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = 7.5f;
            gameCamera.allowHDR = false;
            gameCamera.allowMSAA = false;
            gameCamera.transform.position = new Vector3(MapLayout.CorePosition.x, MapLayout.CorePosition.y, -10);
            gameCamera.backgroundColor = GameBalance.Ground;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private void CreateMap()
        {
            foreach (var path in MapLayout.CreatePaths()) paths.Add(path);
            var visualHalfHeight = GameBalance.CameraMaximumZoom + 24f;
            var visualHalfWidth = visualHalfHeight * Mathf.Max(1f, gameCamera.aspect) + 24f;
            visualBounds = Rect.MinMaxRect(-visualHalfWidth, -visualHalfHeight,
                visualHalfWidth, visualHalfHeight);
            var ground = new GameObject("Starless Void");
            var groundRenderer = ground.AddComponent<SpriteRenderer>();
            groundRenderer.sprite = VisualFactory.Square;
            groundRenderer.color = GameBalance.Ground;
            groundRenderer.sortingOrder = -10;
            ground.transform.position = visualBounds.center;
            ground.transform.localScale = new Vector3(visualBounds.width, visualBounds.height, 1);
            CreateStarField();
            CreateProgressionContours();

            for (var index = 0; index < paths.Count; index++)
            {
                var pathObject = new GameObject($"Path {index + 1}{(index == 0 ? " - Open" : " - Future")}");
                var glowObject = new GameObject("Path Aura");
                glowObject.transform.SetParent(pathObject.transform, false);
                var glow = glowObject.AddComponent<LineRenderer>();
                var branchWidth = Mathf.Lerp(.86f, .4f, PathDepth(index) / 4f);
                ConfigurePathLine(glow, paths[index], branchWidth * 2.35f, -3);
                pathGlows.Add(glow);
                var line = pathObject.AddComponent<LineRenderer>();
                ConfigurePathLine(line, paths[index], branchWidth, -2);
                pathLines.Add(line);

                var node = new GameObject($"Skill Node {index + 1}");
                node.transform.position = paths[index][0];
                node.transform.rotation = Quaternion.Euler(0, 0, 45);
                var defenseUnlock = GameBalance.DefenseUnlockedByPath(index);
                var special = defenseUnlock.HasValue;
                var frameSize = special ? 2.18f : 1.55f;
                var haloSize = special ? 2.78f : 2.02f;
                var frameSprite = special ? VisualFactory.PolygonOutline(8) : VisualFactory.PolygonOutline(4);
                VisualFactory.Part(node.transform, "Node Shadow", frameSprite,
                    new Color(0, 0, 0, .78f), Vector3.zero, Vector3.one * (frameSize + .3f), 0);
                VisualFactory.Part(node.transform, "Node Plate",
                    special ? VisualFactory.Polygon(8) : VisualFactory.Polygon(4),
                    new Color(GameBalance.Ground.r, GameBalance.Ground.g, GameBalance.Ground.b, .96f),
                    Vector3.zero, Vector3.one * (frameSize - .18f), 1);
                var halo = VisualFactory.Part(node.transform, special ? "Defense Unlock Halo" : "Available Halo",
                    VisualFactory.Ring, Color.clear, Vector3.zero, Vector3.one * haloSize, 1);
                pathNodeHalos.Add(halo);
                pathNodeHaloScales.Add(haloSize);
                pathNodes.Add(VisualFactory.Part(node.transform, GameBalance.PathNames[index],
                    frameSprite, GameBalance.PathLocked, Vector3.zero, Vector3.one * frameSize, 2));
                var glyph = VisualFactory.Part(node.transform, $"{GameBalance.PathReward(index)} Glyph",
                    PathGlyph(index), PathRewardColor(index), Vector3.zero,
                    Vector3.one * (special ? .92f : .56f), 3);
                glyph.transform.localRotation = Quaternion.Euler(0, 0, -45);
                pathNodeGlyphs.Add(glyph);

                var portal = new GameObject($"Enemy Portal {index + 1}");
                portal.transform.position = paths[index][0];
                VisualFactory.GlowPart(portal.transform, "Rift", VisualFactory.PolygonOutline(6), GameBalance.Enemy,
                    Vector3.zero, Vector3.one * 1.4f, 5, 1.5f);
                VisualFactory.Part(portal.transform, "Opening", VisualFactory.Circle, GameBalance.Ground,
                    Vector3.zero, Vector3.one * .62f, 6);
                portal.SetActive(false);
                pathPortals.Add(portal);
            }
            RefreshPathColors();

            var core = new GameObject("Core");
            core.transform.position = MapLayout.CorePosition;
            VisualFactory.Part(core.transform, "North Coupler", VisualFactory.Square, new Color(.31f, .30f, .26f),
                new Vector3(0, 2.05f, 0), new Vector3(.72f, 1.15f, 1), 0);
            VisualFactory.Part(core.transform, "East Coupler", VisualFactory.Square, new Color(.31f, .30f, .26f),
                new Vector3(2.05f, 0, 0), new Vector3(1.15f, .72f, 1), 0);
            VisualFactory.Part(core.transform, "South Coupler", VisualFactory.Square, new Color(.31f, .30f, .26f),
                new Vector3(0, -2.05f, 0), new Vector3(.72f, 1.15f, 1), 0);
            VisualFactory.Part(core.transform, "West Coupler", VisualFactory.Square, new Color(.31f, .30f, .26f),
                new Vector3(-2.05f, 0, 0), new Vector3(1.15f, .72f, 1), 0);
            VisualFactory.Part(core.transform, "Core Shadow", VisualFactory.Polygon(8), new Color(0, 0, 0, .72f),
                new Vector3(.2f, -.22f, 0), Vector3.one * 4.65f, 0);
            VisualFactory.Part(core.transform, "Outer Casing", VisualFactory.Polygon(8), new Color(.32f, .31f, .27f),
                Vector3.zero, Vector3.one * 4.2f, 1);
            VisualFactory.Part(core.transform, "Outer Rim", VisualFactory.PolygonOutline(8), new Color(.67f, .65f, .55f),
                Vector3.zero, Vector3.one * 3.82f, 2);
            VisualFactory.Part(core.transform, "Inner Casing", VisualFactory.Polygon(8), new Color(.15f, .15f, .13f),
                Vector3.zero, Vector3.one * 2.62f, 3);
            VisualFactory.GlowPart(core.transform, "Core Light", VisualFactory.PolygonOutline(8), GameBalance.Text,
                Vector3.zero, Vector3.one * 2.08f, 5, 1.22f);
        }

        private static void ConfigurePathLine(LineRenderer line, Vector3[] path, float width, int order)
        {
            line.positionCount = 2;
            line.SetPosition(0, path[0]);
            line.SetPosition(1, path[1]);
            line.startWidth = width * .72f;
            line.endWidth = width;
            line.numCornerVertices = 0;
            line.numCapVertices = 0;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.sortingOrder = order;
        }

        private void CreateStarField()
        {
            var stars = new GameObject("Dust and Distant Stars");
            var random = new System.Random(7341);
            for (var index = 0; index < 260; index++)
            {
                var x = Mathf.Lerp(visualBounds.xMin, visualBounds.xMax, (float)random.NextDouble());
                var y = Mathf.Lerp(visualBounds.yMin, visualBounds.yMax, (float)random.NextDouble());
                var size = index % 17 == 0 ? .34f : index % 5 == 0 ? .2f : .11f;
                var warmth = index % 4 == 0;
                var color = warmth ? new Color(.72f, .53f, .24f, .42f) : new Color(.42f, .48f, .49f, .25f);
                VisualFactory.Part(stars.transform, $"Star {index + 1}", VisualFactory.Square, color,
                    new Vector3(x, y, 0), Vector3.one * size, -8);
            }
        }

        private static void CreateProgressionContours()
        {
            var contours = new GameObject("Progression Contours");
            for (var index = 0; index < MapLayout.TierFractions.Length; index++)
            {
                var progress = index / (MapLayout.TierFractions.Length - 1f);
                var alpha = index == 0 ? .105f : Mathf.Lerp(.065f, .025f, progress);
                var color = index % 2 == 0
                    ? new Color(GameBalance.Ore.r, GameBalance.Ore.g, GameBalance.Ore.b, alpha)
                    : new Color(GameBalance.Gold.r, GameBalance.Gold.g, GameBalance.Gold.b, alpha);
                var contourObject = new GameObject($"Tier {index + 1}");
                contourObject.transform.SetParent(contours.transform, false);
                var contour = contourObject.AddComponent<LineRenderer>();
                contour.loop = true;
                contour.positionCount = 160;
                contour.useWorldSpace = false;
                contour.material = new Material(Shader.Find("Sprites/Default"));
                contour.startColor = contour.endColor = color;
                contour.startWidth = contour.endWidth = Mathf.Lerp(.24f, .12f, progress);
                contour.sortingOrder = -6;
                for (var point = 0; point < contour.positionCount; point++)
                {
                    var angle = Mathf.PI * 2f * point / contour.positionCount;
                    contour.SetPosition(point, MapLayout.PointOnTier(MapLayout.TierFractions[index], angle));
                }
            }
        }

        private static Sprite PathGlyph(int pathIndex)
        {
            var defense = GameBalance.DefenseUnlockedByPath(pathIndex);
            if (defense.HasValue) return VisualFactory.PolygonOutline(GameBalance.DefenseSides(defense.Value));
            return GameBalance.PathReward(pathIndex) switch
            {
                PathRewardKind.Start => VisualFactory.PolygonOutline(8),
                PathRewardKind.Damage => VisualFactory.Polygon(3),
                PathRewardKind.Range => VisualFactory.Ring,
                PathRewardKind.Speed => VisualFactory.PolygonOutline(3),
                PathRewardKind.Gold => VisualFactory.Circle,
                PathRewardKind.Ore => VisualFactory.Polygon(4),
                PathRewardKind.Core => VisualFactory.PolygonOutline(6),
                _ => VisualFactory.PolygonOutline(4)
            };
        }

        private static Color PathRewardColor(int pathIndex)
        {
            var defense = GameBalance.DefenseUnlockedByPath(pathIndex);
            if (defense.HasValue) return GameBalance.BuildingColor(defense.Value);
            var pathDefense = GameBalance.DefenseForPath(pathIndex);
            if (pathDefense.HasValue) return GameBalance.BuildingColor(pathDefense.Value);
            return GameBalance.PathReward(pathIndex) switch
            {
                PathRewardKind.Damage => GameBalance.Defense,
                PathRewardKind.Range => GameBalance.Ore,
                PathRewardKind.Speed => new Color(1f, .48f, .24f),
                PathRewardKind.Gold => GameBalance.Gold,
                PathRewardKind.Ore => GameBalance.Collector,
                PathRewardKind.Core => GameBalance.GreenBranch,
                PathRewardKind.Efficiency => GameBalance.Text,
                _ => Color.white
            };
        }

        private void RefreshPathColors()
        {
            for (var index = 0; index < pathLines.Count; index++)
            {
                var unlocked = IsPathUnlocked(index);
                var available = CanUnlockPath(index);
                var branchColor = BranchColor(index);
                var color = unlocked ? branchColor : available ? GameBalance.Gold : GameBalance.PathLocked;
                pathLines[index].startColor = color;
                pathLines[index].endColor = color;
                var glowColor = unlocked || available
                    ? new Color(color.r, color.g, color.b, .14f)
                    : new Color(color.r, color.g, color.b, .28f);
                pathGlows[index].startColor = glowColor;
                pathGlows[index].endColor = glowColor;
                pathLines[index].sortingOrder = unlocked ? -1 : -2;
                pathLines[index].gameObject.name = $"Path {index + 1} - {(unlocked ? GameBalance.PathNames[index] : available ? "Choice Available" : "Locked")}";
                pathNodes[index].color = color;
                var rewardColor = PathRewardColor(index);
                pathNodeGlyphs[index].color = unlocked
                    ? rewardColor
                    : available
                        ? Color.Lerp(rewardColor, Color.white, .42f)
                        : new Color(rewardColor.r * .34f, rewardColor.g * .34f,
                            rewardColor.b * .34f, .72f);
                var defenseUnlock = GameBalance.DefenseUnlockedByPath(index).HasValue;
                pathNodeHalos[index].enabled = defenseUnlock || available;
                pathNodeHalos[index].color = available
                    ? new Color(GameBalance.Gold.r, GameBalance.Gold.g, GameBalance.Gold.b, .7f)
                    : unlocked
                        ? new Color(rewardColor.r, rewardColor.g, rewardColor.b, .48f)
                        : new Color(rewardColor.r, rewardColor.g, rewardColor.b, .2f);
                pathPortals[index].SetActive(IsFrontierPath(index));
            }
        }

        private void AnimatePathNodes()
        {
            var pulse = (Mathf.Sin(Time.unscaledTime * 3.2f) + 1f) * .5f;
            for (var index = 0; index < pathNodeHalos.Count; index++)
            {
                var halo = pathNodeHalos[index];
                if (!halo.enabled) continue;
                var available = CanUnlockPath(index);
                var scale = pathNodeHaloScales[index] * (available ? Mathf.Lerp(.96f, 1.1f, pulse) : 1f);
                halo.transform.localScale = Vector3.one * scale;
                if (!available) continue;
                halo.color = new Color(GameBalance.Gold.r, GameBalance.Gold.g, GameBalance.Gold.b,
                    Mathf.Lerp(.38f, .82f, pulse));
            }
        }

        private static Color BranchColor(int pathIndex)
        {
            var defense = GameBalance.DefenseForPath(pathIndex);
            if (defense.HasValue) return GameBalance.BuildingColor(defense.Value);
            var root = pathIndex;
            while (MapLayout.PathParents[root] >= 0) root = MapLayout.PathParents[root];
            return root switch
            {
                0 => GameBalance.BlueBranch,
                1 => GameBalance.PurpleBranch,
                2 => GameBalance.Gold,
                3 => GameBalance.GreenBranch,
                _ => GameBalance.PathOpen
            };
        }

        private bool IsFrontierPath(int pathIndex)
        {
            if (!IsPathUnlocked(pathIndex)) return false;
            for (var index = 0; index < pathUnlocked.Length; index++)
                if (pathUnlocked[index] && MapLayout.PathParents[index] == pathIndex) return false;
            return true;
        }

        private void RefreshFrontierPaths()
        {
            frontierPaths.Clear();
            for (var index = 0; index < pathUnlocked.Length; index++)
                if (IsFrontierPath(index)) frontierPaths.Add(index);
        }

        private static int PathDepth(int pathIndex)
        {
            var depth = 0;
            while (pathIndex >= 0 && MapLayout.PathParents[pathIndex] >= 0)
            {
                depth++;
                pathIndex = MapLayout.PathParents[pathIndex];
            }
            return depth;
        }

        private static float RouteLength(IReadOnlyList<Vector3> route)
        {
            var length = 0f;
            for (var index = 0; index < route.Count - 1; index++)
                length += Vector3.Distance(route[index], route[index + 1]);
            return length;
        }

        private void Update()
        {
            announcementTimer -= Time.deltaTime;
            arcBurstCooldown = Mathf.Max(0, arcBurstCooldown - Time.deltaTime);
            coreRepairCooldown = Mathf.Max(0, coreRepairCooldown - Time.deltaTime);
            automationTimer -= Time.deltaTime;
            if (AutoUpgradeEnabled && AutoUpgradeUnlocked && automationTimer <= 0)
            {
                AutoUpgradeCheapestBuilding();
                automationTimer = 1.25f;
            }
            autosaveTimer += Time.unscaledDeltaTime;
            if (saveDirty && autosaveTimer >= AutosaveInterval) SaveProgress();
            if (gameHud != null && (gameHud.IsSettingsOpen || gameHud.IsProgressionOpen))
            {
                HoveredPath = -1;
                cameraDragging = false;
                pressedBuilding = null;
                AnimatePathNodes();
                HandleWave();
                return;
            }
            UpdateHoveredPath();
            AnimatePathNodes();
            HandleCamera();
            HandleWave();
            HandlePointer();
        }

        private void HandleCamera()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            var screen = mouse.position.ReadValue();
            var world = gameCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10));
            if (mouse.leftButton.wasPressedThisFrame && !PlacementType.HasValue && HoveredPath < 0 &&
                FindBuildingAt(world) == null && !PointerOverHud(screen))
            {
                cameraDragging = true;
                lastDragPosition = screen;
            }
            if (mouse.leftButton.wasReleasedThisFrame || movingBuilding != null) cameraDragging = false;
            if (cameraDragging && mouse.leftButton.isPressed)
            {
                var delta = screen - lastDragPosition;
                var unitsPerPixel = gameCamera.orthographicSize * 2f / Mathf.Max(1, Screen.height);
                gameCamera.transform.position -= new Vector3(delta.x, delta.y, 0) * unitsPerPixel;
                lastDragPosition = screen;
                ClampCamera();
            }

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < .01f) return;
            var before = gameCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10));
            gameCamera.orthographicSize = Mathf.Clamp(
                gameCamera.orthographicSize - Mathf.Sign(scroll) * GameBalance.CameraZoomStep,
                GameBalance.CameraMinimumZoom,
                GameBalance.CameraMaximumZoom);
            var after = gameCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10));
            gameCamera.transform.position += before - after;
            ClampCamera();
        }

        private void ClampCamera()
        {
            var halfHeight = gameCamera.orthographicSize;
            var halfWidth = halfHeight * gameCamera.aspect;
            var position = gameCamera.transform.position;
            position.x = halfWidth * 2 >= MapLayout.Bounds.width ? MapLayout.Bounds.center.x :
                Mathf.Clamp(position.x, MapLayout.Bounds.xMin + halfWidth, MapLayout.Bounds.xMax - halfWidth);
            position.y = halfHeight * 2 >= MapLayout.Bounds.height ? MapLayout.Bounds.center.y :
                Mathf.Clamp(position.y, MapLayout.Bounds.yMin + halfHeight, MapLayout.Bounds.yMax - halfHeight);
            position.z = -10;
            gameCamera.transform.position = position;
        }

        private void HandleWave()
        {
            if (!HasStarted) return;
            if (!IsTransitioning) waveElapsed += Time.deltaTime;
            if (IsTransitioning)
            {
                transitionTimer -= Time.deltaTime;
                if (transitionTimer <= 0)
                {
                    IsTransitioning = false;
                    StartWave(ProgressionQueued);
                }
                return;
            }
            if (spawnRemaining > 0)
            {
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0)
                {
                    SpawnEnemy();
                    spawnRemaining--;
                    spawnedCount++;
                    spawnTimer = GameBalance.EnemySpawnInterval(ActiveWave);
                }
            }
            else if (enemies.Count == 0) FinishWave();
        }

        private void StartWave(bool progression)
        {
            HasStarted = true;
            ProgressionActive = progression;
            ProgressionQueued = false;
            ActiveWave = progression ? ClearedWave + 1 : Mathf.Max(1, ClearedWave);
            CoreHealth = MaxCoreHealth;
            waveElapsed = 0f;
            bossDefeatedThisWave = false;
            lastBossShardReward = 0;
            RefreshFrontierPaths();
            var totalDepth = 0f;
            foreach (var pathIndex in frontierPaths) totalDepth += PathDepth(pathIndex);
            var averageDepth = frontierPaths.Count > 0 ? totalDepth / frontierPaths.Count : 0f;
            spawnRemaining = GameBalance.FrontierEnemyCount(ActiveWave, frontierPaths.Count, averageDepth);
            if (GameBalance.IsBossWave(ActiveWave)) spawnRemaining++;
            spawnedCount = 0;
            spawnTimer = .35f;
            gameAudio?.Play(GameSound.WaveStart);
            Announce(GameBalance.IsBossWave(ActiveWave)
                ? $"BOSS WAVE {ActiveWave} - {GameBalance.BossName(GameBalance.BossForWave(ActiveWave))}"
                : progression ? $"PROGRESSION WAVE {ActiveWave}" : $"FARMING WAVE {ActiveWave}", 2.5f);
        }

        private void SpawnEnemy()
        {
            if (frontierPaths.Count == 0) RefreshFrontierPaths();
            var pathIndex = frontierPaths.Count > 0 ? frontierPaths[spawnedCount % frontierPaths.Count] : 0;
            var boss = GameBalance.IsBossWave(ActiveWave) && spawnRemaining == 1;
            var enemyObject = new GameObject(boss
                ? GameBalance.BossName(GameBalance.BossForWave(ActiveWave))
                : $"Red Circle - Path {pathIndex + 1}");
            var enemy = enemyObject.AddComponent<Enemy>();
            enemies.Add(enemy);
            var startingRouteLength = RouteLength(paths[0]);
            var speedMultiplier = GameBalance.RouteSpeedMultiplier(RouteLength(paths[pathIndex]), startingRouteLength);
            enemy.Initialize(this, paths[pathIndex], ActiveWave, speedMultiplier, boss);
        }

        private void FinishWave()
        {
            gameAudio?.Play(GameSound.WaveClear);
            if (GameBalance.IsBossWave(ActiveWave) && bossDefeatedThisWave && waveElapsed <= 75f)
            {
                CoreShards = Mathf.Min(2000000000, CoreShards + 1);
                lastBossShardReward++;
            }
            if (ProgressionActive)
            {
                var oldUnlockCount = PathUnlocksAvailable;
                ClearedWave = ActiveWave;
                ProgressionQueued = true;
                if (PathUnlocksAvailable > oldUnlockCount)
                {
                    RefreshPathColors();
                    Announce("NEW PATH UNLOCK - CHOOSE A SKILL", 2.8f);
                }
                else if (bossDefeatedThisWave)
                    Announce($"BOSS DEFEATED - +{lastBossShardReward} CORE SHARDS", 3.2f);
                else Announce($"WAVE {ActiveWave} CLEARED - WAVE {ActiveWave + 1} NEXT", 2.3f);
                if (StopBeforeBoss && GameBalance.IsBossWave(ActiveWave + 1))
                {
                    ProgressionQueued = false;
                    ProgressionActive = false;
                    Announce($"BOSS {ActiveWave + 1} READY - AUTO-ADVANCE PAUSED", 3f);
                }
            }
            else Announce($"FARM WAVE {ActiveWave} COMPLETE", 1.8f);
            saveDirty = true;
            SaveProgress();
            IsTransitioning = true;
            transitionTimer = 2f;
        }

        private void FailWave()
        {
            foreach (var enemy in enemies.ToArray()) if (enemy != null) Destroy(enemy.gameObject);
            enemies.Clear();
            ProgressionQueued = false;
            ProgressionActive = false;
            IsTransitioning = true;
            transitionTimer = 2.5f;
            Announce(ClearedWave == 0 ? "FAILED - FARMING WAVE 1" : $"FAILED - FARMING WAVE {ClearedWave}", 2.5f);
        }

        public void StartOrQueueProgression()
        {
            if (ProgressionActive || ProgressionQueued) return;
            if (!HasStarted) StartWave(true);
            else
            {
                ProgressionQueued = true;
                Announce($"WAVE {ClearedWave + 1} QUEUED", 1.8f);
            }
        }

        private void HandlePointer()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            var screen = mouse.position.ReadValue();
            var world = gameCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10));
            world.z = 0;

            if (PlacementType.HasValue)
            {
                var point = Snap(world);
                placementPreview.transform.position = point;
                var valid = CanPlace(point);
                foreach (var part in previewParts)
                    part.renderer.color = valid
                        ? new Color(part.color.r, part.color.g, part.color.b, part.color.a * .78f)
                        : new Color(1f, .2f, .2f, part.color.a * .85f);
                if (mouse.rightButton.wasPressedThisFrame) CancelPlacement();
                if (mouse.leftButton.wasPressedThisFrame && !PointerOverHud(screen) && valid) Place(point);
                return;
            }

            if (movingBuilding != null)
            {
                var point = Snap(world);
                var valid = !PointerOverHud(screen) && CanPlace(point, movingBuilding);
                movingBuilding.transform.position = point;
                movingBuilding.SetMoveValidity(valid);
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    FinishBuildingMove(false);
                    return;
                }
                if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed) FinishBuildingMove(valid);
                return;
            }

            if (pressedBuilding != null)
            {
                if (mouse.leftButton.isPressed)
                {
                    buildingHoldTimer += Time.unscaledDeltaTime;
                    if (buildingHoldTimer >= HoldToMoveDelay)
                    {
                        movingBuilding = pressedBuilding;
                        pressedBuilding = null;
                        moveOrigin = movingBuilding.transform.position;
                        movingBuilding.BeginMove();
                        cameraDragging = false;
                        Announce("MOVE BUILDING - RELEASE TO PLACE", 1.5f);
                    }
                }
                else pressedBuilding = null;
                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame || PointerOverHud(screen)) return;
            var clickedBuilding = FindBuildingAt(world);
            if (clickedBuilding != null)
            {
                SelectedBuilding = clickedBuilding;
                pressedBuilding = clickedBuilding;
                buildingHoldTimer = 0;
                return;
            }
            if (HoveredPath > 0 && !IsPathUnlocked(HoveredPath))
            {
                UnlockPath(HoveredPath);
                return;
            }
            SelectedBuilding = null;
        }

        private Building FindBuildingAt(Vector3 point)
        {
            Building closestBuilding = null;
            var closest = .8f;
            foreach (var building in buildings)
            {
                if (building == null) continue;
                var distance = Vector2.Distance(point, building.transform.position);
                if (distance >= closest) continue;
                closest = distance;
                closestBuilding = building;
            }
            return closestBuilding;
        }

        private void FinishBuildingMove(bool keepPosition)
        {
            if (movingBuilding == null) return;
            if (!keepPosition) movingBuilding.transform.position = moveOrigin;
            var movedPosition = movingBuilding.transform.position;
            movingBuilding.EndMove();
            movingBuilding = null;
            pressedBuilding = null;
            if (!keepPosition)
            {
                gameAudio?.Play(GameSound.Denied);
                Announce("INVALID LOCATION - MOVE CANCELLED", 1.5f);
                return;
            }
            gameAudio?.Play(GameSound.Build, .65f, movedPosition);
            Announce("BUILDING MOVED", 1.2f);
            saveDirty = true;
            SaveProgress();
        }

        private bool PointerOverHud(Vector2 screen)
        {
            if (gameHud != null && (gameHud.IsSettingsOpen || gameHud.IsSellConfirmationOpen ||
                gameHud.IsProgressionOpen)) return true;
            if (screen.y < 146 || screen.y > Screen.height - 96) return true;
            var guiPoint = new Vector2(screen.x, Screen.height - screen.y);
            if (gameHud != null && gameHud.IsBuildMenuOpen && gameHud.BuildMenuRect.Contains(guiPoint)) return true;
            if (SelectedBuilding == null) return false;
            return SelectionPanelRect().Contains(guiPoint);
        }

        private void UpdateHoveredPath()
        {
            HoveredPath = -1;
            var mouse = Mouse.current;
            if (mouse == null || PlacementType.HasValue || pressedBuilding != null || movingBuilding != null) return;
            var screen = mouse.position.ReadValue();
            if (PointerOverHud(screen)) return;
            var world = gameCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10));
            var unitsPerPixel = gameCamera.orthographicSize * 2f / Mathf.Max(1, Screen.height);
            var screenSpaceRadius = unitsPerPixel * 12f;
            var closest = float.MaxValue;
            for (var index = 0; index < paths.Count; index++)
            {
                var nodeRadius = Mathf.Max(
                    GameBalance.DefenseUnlockedByPath(index).HasValue ? 1.28f : .82f,
                    screenSpaceRadius);
                var distance = Vector2.Distance(world, paths[index][0]);
                if (distance > nodeRadius || distance >= closest) continue;
                closest = distance;
                HoveredPath = index;
            }
        }

        public Rect SelectionPanelRect()
        {
            const float width = 360;
            var height = SelectedBuilding != null && GameBalance.IsDefense(SelectedBuilding.Type) ? 214f : 166f;
            if (SelectedBuilding == null) return new Rect(12, 100, width, height);
            var screen = gameCamera.WorldToScreenPoint(SelectedBuilding.transform.position);
            var x = Mathf.Clamp(screen.x - width * .5f, 12, Screen.width - width - 12);
            var y = Mathf.Clamp(Screen.height - screen.y - height - 28, 104, Screen.height - 152 - height);
            return new Rect(x, y, width, height);
        }

        public void BeginPlacement(BuildingType type)
        {
            if (!IsBuildingUnlocked(type))
            {
                var path = GameBalance.BuildingUnlockPath(type);
                gameAudio?.Play(GameSound.Denied);
                Announce($"UNLOCK {GameBalance.PathNames[path].ToUpperInvariant()} FIRST", 1.8f);
                return;
            }
            gameAudio?.Play(GameSound.Select, .8f);
            SelectedBuilding = null;
            PlacementType = type;
            if (placementPreview != null) Destroy(placementPreview);
            placementPreview = new GameObject("Placement Preview");
            previewParts.Clear();
            if (GameBalance.IsDefense(type))
            {
                var sides = GameBalance.DefenseSides(type);
                var color = GameBalance.BuildingColor(type);
                AddPreviewStatic("Shadow", VisualFactory.Circle, new Color(0, 0, 0, .55f),
                    new Vector3(.09f, -.12f, 0), new Vector3(1.42f, .72f, 1), 18);
                AddPreviewStatic("Dark Housing", VisualFactory.Polygon(sides), GameBalance.Ground,
                    Vector3.zero, Vector3.one * 1.15f, 19);
                AddPreview("Defense Glow", VisualFactory.PolygonOutline(sides),
                    new Color(color.r, color.g, color.b, .16f),
                    Vector3.zero, Vector3.one * 1.72f, 20);
                AddPreview("Defense Frame", VisualFactory.PolygonOutline(sides), color,
                    Vector3.zero, Vector3.one * 1.28f, 21);
                if (type == BuildingType.ArcDefense)
                    AddPreviewStatic("Arc Core", VisualFactory.PolygonOutline(4), GameBalance.Text,
                        Vector3.zero, Vector3.one * .34f, 22);
                else if (type == BuildingType.PierceDefense)
                    AddPreviewStatic("Rail", VisualFactory.Square, GameBalance.Text,
                        Vector3.zero, new Vector3(.12f, .62f, 1), 22);
                else if (type == BuildingType.SupportDefense)
                    AddPreviewStatic("Support Core", VisualFactory.Ring, GameBalance.Text,
                        Vector3.zero, Vector3.one * .44f, 22);
                else if (type == BuildingType.BlastDefense)
                    AddPreviewStatic("Blast Core", VisualFactory.Circle, GameBalance.Gold,
                        Vector3.zero, Vector3.one * .28f, 22);
                else if (type == BuildingType.FrostDefense)
                    AddPreviewStatic("Frost Crystal", VisualFactory.PolygonOutline(4), GameBalance.Text,
                        Vector3.zero, Vector3.one * .46f, 22);
                else if (type == BuildingType.PrismDefense)
                    AddPreviewStatic("Prism Core", VisualFactory.Polygon(3), GameBalance.Text,
                        Vector3.zero, Vector3.one * .38f, 22);
                else if (type == BuildingType.PulseDefense)
                {
                    AddPreviewStatic("Pulse Core", VisualFactory.Ring, GameBalance.Text,
                        Vector3.zero, Vector3.one * .5f, 22);
                    AddPreviewStatic("Pulse Center", VisualFactory.Circle, color,
                        Vector3.zero, Vector3.one * .16f, 23);
                }
                else if (type == BuildingType.VolleyDefense)
                    AddPreviewStatic("Volley Rails", VisualFactory.Square, GameBalance.Text,
                        Vector3.zero, new Vector3(.42f, .14f, 1), 22);
                else
                    AddPreviewStatic("Emitter", VisualFactory.Circle, GameBalance.Text,
                        new Vector3(0, -.05f, 0), Vector3.one * .16f, 22);
            }
            else
            {
                AddPreviewStatic("Shadow", VisualFactory.Circle, new Color(0, 0, 0, .55f),
                    new Vector3(.09f, -.12f, 0), new Vector3(1.42f, .72f, 1), 18);
                AddPreviewStatic("Dark Housing", VisualFactory.Circle, GameBalance.Ground,
                    Vector3.zero, Vector3.one * 1.18f, 19);
                AddPreview("Collector Glow", VisualFactory.Ring,
                    new Color(GameBalance.Collector.r, GameBalance.Collector.g, GameBalance.Collector.b, .16f),
                    Vector3.zero, Vector3.one * 1.76f, 20);
                AddPreview("Collector Ring", VisualFactory.Ring, GameBalance.Collector,
                    Vector3.zero, Vector3.one * 1.3f, 21);
                AddPreview("Ore Glow", VisualFactory.PolygonOutline(4),
                    new Color(GameBalance.Ore.r, GameBalance.Ore.g, GameBalance.Ore.b, .16f),
                    new Vector3(0, .02f, 0), Vector3.one * .69f, 22);
                AddPreview("Ore Crystal", VisualFactory.PolygonOutline(4), GameBalance.Ore,
                    new Vector3(0, .02f, 0), Vector3.one * .55f, 23);
            }
        }

        private void AddPreview(string name, Sprite sprite, Color color, Vector3 position, Vector3 scale, int order)
        {
            var renderer = VisualFactory.Part(placementPreview.transform, name, sprite, color, position, scale, order);
            previewParts.Add((renderer, color));
        }

        private void AddPreviewStatic(string name, Sprite sprite, Color color, Vector3 position, Vector3 scale, int order)
        {
            VisualFactory.Part(placementPreview.transform, name, sprite, color, position, scale, order);
        }

        public void CancelPlacement()
        {
            PlacementType = null;
            if (placementPreview != null) Destroy(placementPreview);
            previewParts.Clear();
        }

        private void Place(Vector3 point)
        {
            var type = PlacementType.Value;
            if (!IsBuildingUnlocked(type))
            {
                CancelPlacement();
                gameAudio?.Play(GameSound.Denied);
                Announce("DEFENSE IS STILL LOCKED", 1.5f);
                return;
            }
            if (!TrySpend(GameBalance.Currency(type), GetBuildCost(type)))
            {
                gameAudio?.Play(GameSound.Denied);
                Announce($"NOT ENOUGH {GameBalance.Currency(type).ToUpperInvariant()}", 1.5f);
                return;
            }
            var buildingObject = new GameObject(GameBalance.Name(type));
            buildingObject.transform.position = point;
            var building = buildingObject.AddComponent<Building>();
            building.Initialize(this, type);
            buildings.Add(building);
            gameAudio?.Play(GameSound.Build, 1f, point);
            SelectedBuilding = building;
            CancelPlacement();
            saveDirty = true;
            SaveProgress();
        }

        private bool CanPlace(Vector3 point, Building ignoredBuilding = null)
        {
            if (!MapLayout.Bounds.Contains(point)) return false;
            if (Vector2.Distance(point, MapLayout.CorePosition) < 3.2f) return false;
            foreach (var building in buildings)
            {
                if (building == ignoredBuilding) continue;
                if (Vector2.Distance(point, building.transform.position) < 1.5f) return false;
            }
            foreach (var path in paths)
            for (var index = 0; index < path.Length - 1; index++)
                if (DistanceToSegment(point, path[index], path[index + 1]) < 1.25f) return false;
            return true;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            var t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
            return Vector2.Distance(point, start + segment * t);
        }

        private static Vector3 Snap(Vector3 point) => new(Mathf.Round(point.x * 2) * .5f, Mathf.Round(point.y * 2) * .5f);

        public bool TrySpend(string currency, int amount)
        {
            if (currency == "ore")
            {
                if (Ore < amount) return false;
                Ore -= amount;
                return true;
            }
            if (Gold < amount) return false;
            Gold -= amount;
            return true;
        }

        public int GetBuildCost(BuildingType type)
        {
            var existingCount = 0;
            foreach (var building in buildings)
                if (building != null && building.Type == type) existingCount++;
            var multiplier = GameBalance.IsDefense(type) ? DefenseCostMultiplierFor(type) : CollectorCostMultiplier;
            return Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(
                2000000000f, GameBalance.BuildCost(type, existingCount) * multiplier)));
        }

        public bool IsPathUnlocked(int index) => index >= 0 && index < pathUnlocked.Length && pathUnlocked[index];

        public bool IsBuildingUnlocked(BuildingType type)
        {
            var path = GameBalance.BuildingUnlockPath(type);
            return path < 0 || permanentBlueprints[(int)type] || IsPathUnlocked(path);
        }

        private int DefenseUpgradeCount(BuildingType type, PathRewardKind reward)
        {
            var count = 0;
            for (var index = 12; index < pathUnlocked.Length; index++)
                if (pathUnlocked[index] && GameBalance.DefenseForPath(index) == type &&
                    GameBalance.PathReward(index) == reward) count++;
            return count;
        }

        public float DefenseDamageMultiplierFor(BuildingType type) => DefenseDamageMultiplier *
            (1f + DefenseUpgradeCount(type, PathRewardKind.Damage) * .15f +
             DefenseUpgradeCount(type, PathRewardKind.Efficiency) * .05f);

        public float DefenseRangeMultiplierFor(BuildingType type) => DefenseRangeMultiplier *
            (1f + DefenseUpgradeCount(type, PathRewardKind.Range) * .1f);

        public float DefenseFireIntervalMultiplierFor(BuildingType type) => DefenseFireIntervalMultiplier *
            Mathf.Pow(.9f, DefenseUpgradeCount(type, PathRewardKind.Speed));

        public float DefenseCostMultiplierFor(BuildingType type) => DefenseCostMultiplier *
            Mathf.Pow(.95f, DefenseUpgradeCount(type, PathRewardKind.Efficiency));

        public int UnlockedDefensePathCount(BuildingType type)
        {
            if (type == BuildingType.TriangleDefense) return UnlockedPaths;
            if (!GameBalance.IsDefense(type)) return 0;
            var count = 0;
            for (var index = 0; index < pathUnlocked.Length; index++)
                if (pathUnlocked[index] && GameBalance.DefenseForPath(index) == type) count++;
            return count;
        }

        public bool CanUnlockPath(int index)
        {
            if (index <= 0 || index >= pathUnlocked.Length || pathUnlocked[index] || PathUnlocksAvailable <= 0) return false;
            var parent = MapLayout.PathParents[index];
            return parent < 0 || IsPathUnlocked(parent);
        }

        public void UnlockPath(int index)
        {
            if (!CanUnlockPath(index))
            {
                if (index > 0 && index < pathUnlocked.Length)
                {
                    gameAudio?.Play(GameSound.Denied);
                    Announce(PathUnlocksAvailable <= 0
                        ? $"CLEAR {WavesUntilNextPathUnlock} MORE WAVES FOR AN UNLOCK"
                        : "UNLOCK ITS PARENT PATH FIRST", 1.8f);
                }
                return;
            }

            var oldMaxHealth = MaxCoreHealth;
            pathUnlocked[index] = true;
            CoreHealth += MaxCoreHealth - oldMaxHealth;
            RefreshFrontierPaths();
            RefreshPathColors();
            var unlockedDefense = GameBalance.DefenseUnlockedByPath(index);
            if (unlockedDefense.HasValue) permanentBlueprints[(int)unlockedDefense.Value] = true;
            gameAudio?.Play(unlockedDefense.HasValue ? GameSound.TowerUnlock : GameSound.PathUnlock);
            Announce(unlockedDefense.HasValue
                ? $"{GameBalance.Name(unlockedDefense.Value).ToUpperInvariant()} UNLOCKED"
                : $"{GameBalance.PathNames[index].ToUpperInvariant()} - {GameBalance.PathBonuses[index]}", 3f);
            saveDirty = true;
            SaveProgress();
        }

        public void UpgradeSelected()
        {
            if (SelectedBuilding == null) return;
            if (SelectedBuilding.Upgrade())
            {
                gameAudio?.Play(GameSound.Upgrade, 1f, SelectedBuilding.transform.position);
                Announce("UPGRADED", 1.4f);
                saveDirty = true;
                SaveProgress();
            }
            else
            {
                gameAudio?.Play(GameSound.Denied);
                Announce($"NEED MORE {SelectedBuilding.UpgradeCurrency.ToUpperInvariant()}", 1.4f);
            }
        }

        public void UpgradeSelectedMastery()
        {
            var building = SelectedBuilding;
            if (building == null || !GameBalance.IsDefense(building.Type) || building.MasteryRank >= 3) return;
            if (building.UpgradeMastery())
            {
                gameAudio?.Play(GameSound.TowerUnlock, 1f, building.transform.position);
                ShowImpact(building.transform.position, GameBalance.Gold, 1.5f, true);
                Announce($"{GameBalance.Name(building.Type).ToUpperInvariant()} MASTERY {building.MasteryRank}", 2.5f);
                saveDirty = true;
                SaveProgress();
                return;
            }
            gameAudio?.Play(GameSound.Denied);
            var pathCount = UnlockedDefensePathCount(building.Type);
            Announce(pathCount < building.MasteryRequiredPaths
                ? $"UNLOCK {building.MasteryRequiredPaths} NODES ON THIS DEFENSE PATH"
                : building.Level < building.MasteryRequiredLevel
                    ? $"UPGRADE DEFENSE TO LEVEL {building.MasteryRequiredLevel}"
                    : $"NEED {building.MasteryCost} ORE", 2.2f);
        }

        public void ChooseSelectedEvolution(int evolution)
        {
            var building = SelectedBuilding;
            if (building == null || !building.ChooseEvolution(evolution)) return;
            gameAudio?.Play(GameSound.TowerUnlock, 1f, building.transform.position);
            ShowImpact(building.transform.position, evolution == 1 ? GameBalance.Gold : GameBalance.Ore, 1.8f, true);
            Announce($"{GameBalance.EvolutionName(building.Type, evolution)} UNLOCKED", 3f);
            saveDirty = true;
            SaveProgress();
        }

        public int GetSellRefund(Building building)
        {
            if (building == null) return 0;
            var sameTypeCount = 0;
            foreach (var candidate in buildings)
                if (candidate != null && candidate.Type == building.Type) sameTypeCount++;

            var multiplier = GameBalance.IsDefense(building.Type)
                ? DefenseCostMultiplierFor(building.Type) : CollectorCostMultiplier;
            var invested = (double)GameBalance.BuildCost(building.Type, Mathf.Max(0, sameTypeCount - 1)) * multiplier;
            for (var level = 1; level < building.Level; level++)
                invested += (double)GameBalance.UpgradeCost(building.Type, level) * multiplier;
            for (var rank = 1; rank <= building.MasteryRank; rank++)
                invested += GameBalance.MasteryUpgradeCost(building.Type, rank);
            return Mathf.Max(1, (int)Math.Min(2000000000d, Math.Round(invested * .7d)));
        }

        public void SellSelected()
        {
            var building = SelectedBuilding;
            if (building == null) return;

            var refund = GetSellRefund(building);
            var currency = building.UpgradeCurrency;
            var position = building.transform.position;
            buildings.Remove(building);
            SelectedBuilding = null;
            pressedBuilding = null;
            if (movingBuilding == building) movingBuilding = null;

            if (currency == "ore") Ore = (int)Math.Min(2000000000L, (long)Ore + refund);
            else Gold = (int)Math.Min(2000000000L, (long)Gold + refund);
            gameAudio?.Play(GameSound.Build, .65f, position);
            ShowImpact(position, GameBalance.BuildingColor(building.Type), .9f);
            Destroy(building.gameObject);
            Announce($"SOLD +{refund} {currency.ToUpperInvariant()}", 1.5f);
            saveDirty = true;
            SaveProgress();
        }

        public int PermanentUpgradeCost(int upgrade)
        {
            var level = upgrade switch
            {
                0 => PermanentPowerLevel,
                1 => PermanentEconomyLevel,
                2 => PermanentCoreLevel,
                _ => OfflineLevel
            };
            return Mathf.Min(1000, 1 + level * (upgrade == 3 ? 2 : 1));
        }

        public bool BuyPermanentUpgrade(int upgrade)
        {
            if (upgrade < 0 || upgrade > 3) return false;
            if (upgrade == 3 && OfflineLevel >= 2) return false;
            var cost = PermanentUpgradeCost(upgrade);
            if (CoreShards < cost) return false;
            CoreShards -= cost;
            switch (upgrade)
            {
                case 0: PermanentPowerLevel++; break;
                case 1: PermanentEconomyLevel++; break;
                case 2:
                    PermanentCoreLevel++;
                    CoreHealth += 2;
                    break;
                case 3: OfflineLevel++; break;
            }
            gameAudio?.Play(GameSound.Upgrade);
            Announce("PERMANENT CORE UPGRADE INSTALLED", 2f);
            saveDirty = true;
            SaveProgress();
            return true;
        }

        public int AutomationUnlockCost(int automation) => automation == 0 ? 2 : 3;

        public bool UnlockAutomation(int automation)
        {
            if (automation == 0 && AutoWaveUnlocked || automation == 1 && AutoUpgradeUnlocked) return false;
            var cost = AutomationUnlockCost(automation);
            if (CoreShards < cost) return false;
            CoreShards -= cost;
            if (automation == 0) AutoWaveUnlocked = AutoWaveEnabled = true;
            else AutoUpgradeUnlocked = AutoUpgradeEnabled = true;
            Announce(automation == 0 ? "AUTO-ADVANCE UNLOCKED" : "AUTO-UPGRADE UNLOCKED", 2.5f);
            saveDirty = true;
            SaveProgress();
            return true;
        }

        public void ToggleAutomation(int automation)
        {
            if (automation == 0 && AutoWaveUnlocked) AutoWaveEnabled = !AutoWaveEnabled;
            else if (automation == 1 && AutoUpgradeUnlocked) AutoUpgradeEnabled = !AutoUpgradeEnabled;
            else if (automation == 2) StopBeforeBoss = !StopBeforeBoss;
            else return;
            saveDirty = true;
            SaveProgress();
        }

        private void AutoUpgradeCheapestBuilding()
        {
            Building cheapest = null;
            var cheapestCost = int.MaxValue;
            foreach (var building in buildings)
            {
                if (building == null || !building.CanAffordUpgrade || building.UpgradeCost >= cheapestCost) continue;
                cheapest = building;
                cheapestCost = building.UpgradeCost;
            }
            if (cheapest == null || !cheapest.Upgrade()) return;
            gameAudio?.Play(GameSound.Upgrade, .35f, cheapest.transform.position);
            saveDirty = true;
        }

        public void CaptureLayout()
        {
            savedLayout.Clear();
            foreach (var building in buildings)
            {
                if (building == null) continue;
                var position = building == movingBuilding ? moveOrigin : building.transform.position;
                savedLayout.Add(new BuildingSaveData
                {
                    type = building.Type,
                    level = building.Level,
                    masteryRank = building.MasteryRank,
                    evolution = building.Evolution,
                    x = position.x,
                    y = position.y
                });
            }
            Announce($"LAYOUT SAVED - {savedLayout.Count} BUILDINGS", 2f);
            saveDirty = true;
            SaveProgress();
        }

        public void RestoreLayout()
        {
            var restored = 0;
            foreach (var saved in savedLayout)
            {
                var position = new Vector3(saved.x, saved.y, 0);
                if (!IsBuildingUnlocked(saved.type) || !CanPlace(position)) continue;
                if (!TrySpend(GameBalance.Currency(saved.type), GetBuildCost(saved.type))) continue;
                var buildingObject = new GameObject(GameBalance.Name(saved.type));
                buildingObject.transform.position = position;
                var building = buildingObject.AddComponent<Building>();
                building.Initialize(this, saved.type);
                buildings.Add(building);
                while (building.Level < saved.level && building.CanAffordUpgrade) building.Upgrade();
                while (building.MasteryRank < saved.masteryRank && building.CanUpgradeMastery)
                    building.UpgradeMastery();
                if (saved.evolution > 0) building.ChooseEvolution(saved.evolution);
                restored++;
            }
            Announce(restored > 0 ? $"RESTORED {restored} BUILDINGS" : "LAYOUT NEEDS MORE RESOURCES OR UNLOCKS", 2.5f);
            saveDirty = true;
            SaveProgress();
        }

        public void Prestige()
        {
            if (!CanPrestige) return;
            if (buildings.Count > 0) CaptureLayout();
            var reward = PrestigeReward;
            CoreShards = Mathf.Min(2000000000, CoreShards + reward);
            PrestigeCount++;

            foreach (var enemy in enemies.ToArray()) if (enemy != null) Destroy(enemy.gameObject);
            enemies.Clear();
            foreach (var building in buildings.ToArray()) if (building != null) Destroy(building.gameObject);
            buildings.Clear();
            CancelPlacement();
            SelectedBuilding = null;
            pressedBuilding = null;
            movingBuilding = null;

            Array.Clear(pathUnlocked, 0, pathUnlocked.Length);
            pathUnlocked[0] = true;
            ClearedWave = 0;
            ActiveWave = 0;
            Gold = GameBalance.StartingGold + PermanentEconomyLevel * 25 + PrestigeCount * 5;
            Ore = GameBalance.StartingOre + PermanentPowerLevel * 15 + PrestigeCount * 3;
            CoreHealth = MaxCoreHealth;
            HasStarted = false;
            ProgressionActive = false;
            ProgressionQueued = false;
            IsTransitioning = false;
            RefreshFrontierPaths();
            RefreshPathColors();
            gameAudio?.Play(GameSound.TowerUnlock);
            Announce($"CORE REBOOTED - +{reward} CORE SHARDS", 4f);
            saveDirty = true;
            SaveProgress();
        }

        public void ActivateArcBurst()
        {
            if (!CanUseArcBurst) return;
            arcBurstCooldown = ArcBurstCooldown;
            var targets = new List<Enemy>(enemies);
            targets.Sort((left, right) => left == null ? 1 : right == null ? -1 :
                left.transform.position.sqrMagnitude.CompareTo(right.transform.position.sqrMagnitude));
            var damage = GameBalance.EnemyHealth(Mathf.Max(1, ActiveWave)) * .45f;
            var hitCount = Mathf.Min(12, targets.Count);
            for (var index = 0; index < hitCount; index++)
            {
                var target = targets[index];
                if (target == null) continue;
                ShowTracer(MapLayout.CorePosition, target.transform.position);
                target.TakeDamage(damage);
            }
            gameAudio?.Play(GameSound.Ability);
            Announce($"ARC BURST - {hitCount} TARGETS HIT", 1.8f);
        }

        public void ActivateCoreRepair()
        {
            if (!CanUseCoreRepair) return;
            coreRepairCooldown = CoreRepairCooldown;
            var restored = Mathf.Max(3, Mathf.CeilToInt(MaxCoreHealth * .25f));
            var previousHealth = CoreHealth;
            CoreHealth = Mathf.Min(MaxCoreHealth, CoreHealth + restored);
            gameAudio?.Play(GameSound.Repair);
            Announce($"CORE REPAIR +{CoreHealth - previousHealth}", 1.8f);
        }

        public Enemy FindClosestEnemy(Vector3 position, float range)
        {
            Enemy closest = null;
            var distance = range * range;
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                var candidate = (enemy.transform.position - position).sqrMagnitude;
                if (candidate >= distance) continue;
                distance = candidate;
                closest = enemy;
            }
            return closest;
        }

        public List<Enemy> FindEnemiesInRange(Vector3 position, float range)
        {
            var results = new List<Enemy>();
            var rangeSquared = range * range;
            foreach (var enemy in enemies)
                if (enemy != null && (enemy.transform.position - position).sqrMagnitude <= rangeSquared)
                    results.Add(enemy);
            return results;
        }

        public List<Enemy> FindEnemiesAlongLine(Vector3 start, Vector3 end, float radius)
        {
            var results = new List<Enemy>();
            foreach (var enemy in enemies)
                if (enemy != null && DistanceToSegment(enemy.transform.position, start, end) <= radius)
                    results.Add(enemy);
            return results;
        }

        public float SupportDamageMultiplierAt(Vector3 position, Building ignoredBuilding)
        {
            var multiplier = 1f;
            foreach (var building in buildings)
            {
                if (building == null || building == ignoredBuilding || building.IsBeingMoved ||
                    building.Type != BuildingType.SupportDefense) continue;
                if (Vector2.Distance(position, building.transform.position) <= building.Range)
                    multiplier += building.SupportBoost;
            }
            return Mathf.Min(2.5f, multiplier);
        }

        public void EnemyKilled(Enemy enemy, int reward)
        {
            if (enemy != null) gameAudio?.Play(GameSound.EnemyKill, 1f, enemy.transform.position);
            enemies.Remove(enemy);
            var adjustedReward = Mathf.RoundToInt(Mathf.Min(2000000000f, reward * GoldRewardMultiplier));
            Gold = (int)Math.Min(2000000000L, (long)Gold + adjustedReward);
            if (enemy != null && enemy.IsBoss)
            {
                bossDefeatedThisWave = true;
                BossesDefeated++;
                var firstClear = ActiveWave > HighestBossWave;
                HighestBossWave = Mathf.Max(HighestBossWave, ActiveWave);
                lastBossShardReward = GameBalance.BossShardReward(ActiveWave) + (firstClear ? 1 : 0);
                CoreShards = Mathf.Min(2000000000, CoreShards + lastBossShardReward);
                ShowImpact(enemy.transform.position, GameBalance.Gold, 4f, true);
            }
            saveDirty = true;
        }

        public void SpawnBossMinions(Vector3 position, Vector3[] route, int routeIndex, int count)
        {
            if (route == null || route.Length < 2) return;
            var remaining = Mathf.Max(1, route.Length - routeIndex);
            var minionRoute = new Vector3[remaining];
            minionRoute[0] = position;
            for (var index = 1; index < remaining; index++) minionRoute[index] = route[routeIndex + index];
            for (var index = 0; index < count; index++)
            {
                var enemyObject = new GameObject("Brood Spawn");
                var enemy = enemyObject.AddComponent<Enemy>();
                enemies.Add(enemy);
                enemy.Initialize(this, minionRoute, Mathf.Max(1, ActiveWave - 3), 1.2f);
                enemyObject.transform.position += UnityEngine.Random.insideUnitSphere * .35f;
            }
            ShowImpact(position, GameBalance.Enemy, 1.8f, true);
        }

        public int DrainOreForBoss(int amount)
        {
            var drained = Mathf.Min(Ore, Mathf.Max(0, amount));
            Ore -= drained;
            if (drained > 0) Announce($"THE LEECH DRAINED {drained} ORE", 1.4f);
            return drained;
        }

        public void EnemyReachedCore(Enemy enemy, int damage)
        {
            gameAudio?.Play(GameSound.CoreHit, 1f, MapLayout.CorePosition);
            enemies.Remove(enemy);
            CoreHealth = Mathf.Max(0, CoreHealth - Mathf.Max(1, damage));
            if (CoreHealth <= 0) FailWave();
        }

        public void AddOre(int amount)
        {
            Ore = (int)Math.Min(2000000000L, (long)Ore + amount);
            saveDirty = true;
        }

        public void PlaySound(GameSound sound, float volume = 1f, Vector3? position = null) =>
            gameAudio?.Play(sound, volume, position);

        public void PlayAttackSound(BuildingType type, Vector3 position)
        {
            var sound = type switch
            {
                BuildingType.ArcDefense => GameSound.ArcShot,
                BuildingType.PrismDefense => GameSound.ArcShot,
                BuildingType.PierceDefense => GameSound.PierceShot,
                BuildingType.VolleyDefense => GameSound.PierceShot,
                BuildingType.BlastDefense => GameSound.BlastShot,
                BuildingType.PulseDefense => GameSound.BlastShot,
                _ => GameSound.TriangleShot
            };
            gameAudio?.Play(sound, 1f, position);
        }

        private void Announce(string text, float duration) { Announcement = text; announcementTimer = duration; }

        private bool LoadProgress()
        {
            if (!File.Exists(SavePath)) return false;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                if (data == null || data.version <= 0 || data.version > SaveVersion) return false;

                Gold = Mathf.Max(0, data.gold);
                Ore = Mathf.Max(0, data.ore);
                ClearedWave = Mathf.Max(0, data.clearedWave);
                CoreShards = Mathf.Max(0, data.coreShards);
                HighestBossWave = Mathf.Max(0, data.highestBossWave);
                BossesDefeated = Mathf.Max(0, data.bossesDefeated);
                PrestigeCount = Mathf.Max(0, data.prestigeCount);
                PermanentPowerLevel = Mathf.Max(0, data.permanentPowerLevel);
                PermanentEconomyLevel = Mathf.Max(0, data.permanentEconomyLevel);
                PermanentCoreLevel = Mathf.Max(0, data.permanentCoreLevel);
                OfflineLevel = Mathf.Clamp(data.offlineLevel, 0, 2);
                AutoWaveUnlocked = data.autoWaveUnlocked;
                AutoUpgradeUnlocked = data.autoUpgradeUnlocked;
                AutoWaveEnabled = AutoWaveUnlocked && data.autoWaveEnabled;
                AutoUpgradeEnabled = AutoUpgradeUnlocked && data.autoUpgradeEnabled;
                StopBeforeBoss = data.stopBeforeBoss;
                CoreHealth = MaxCoreHealth;
                GameSpeed = data.gameSpeed >= 2.5f ? 3f : data.gameSpeed >= 1.5f ? 2f : 1f;
                Time.timeScale = GameSpeed;

                Array.Clear(pathUnlocked, 0, pathUnlocked.Length);
                if (data.unlockedPaths != null && data.unlockedPaths.Count > 0)
                {
                    foreach (var pathIndex in data.unlockedPaths)
                        if (pathIndex >= 0 && pathIndex < pathUnlocked.Length) pathUnlocked[pathIndex] = true;
                }
                else
                {
                    // Saves made before player-selected paths preserve their previously earned path count.
                    var legacyPathCount = Mathf.Clamp(1 + ClearedWave / 10, 1, pathUnlocked.Length);
                    for (var index = 0; index < legacyPathCount; index++) pathUnlocked[index] = true;
                }
                pathUnlocked[0] = true;
                RefreshPathColors();

                Array.Clear(permanentBlueprints, 0, permanentBlueprints.Length);
                if (data.unlockedBuildingTypes != null)
                    foreach (var typeIndex in data.unlockedBuildingTypes)
                        if (typeIndex >= 0 && typeIndex < permanentBlueprints.Length)
                            permanentBlueprints[typeIndex] = true;
                // Version-one saves infer permanent blueprints from their unlocked path nodes.
                if (data.version == 1)
                    for (var index = 0; index < pathUnlocked.Length; index++)
                    {
                        var defense = GameBalance.DefenseUnlockedByPath(index);
                        if (defense.HasValue && pathUnlocked[index]) permanentBlueprints[(int)defense.Value] = true;
                    }

                if (data.buildings != null)
                {
                    foreach (var savedBuilding in data.buildings)
                    {
                        if (!Enum.IsDefined(typeof(BuildingType), savedBuilding.type)) continue;
                        var position = new Vector3(savedBuilding.x, savedBuilding.y, 0);
                        if (!MapLayout.Bounds.Contains(position)) continue;
                        var buildingObject = new GameObject(GameBalance.Name(savedBuilding.type));
                        buildingObject.transform.position = position;
                        var building = buildingObject.AddComponent<Building>();
                        building.Initialize(this, savedBuilding.type, savedBuilding.level,
                            savedBuilding.masteryRank, savedBuilding.evolution);
                        buildings.Add(building);
                    }
                }

                savedLayout.Clear();
                if (data.savedLayout != null)
                    foreach (var saved in data.savedLayout)
                        if (Enum.IsDefined(typeof(BuildingType), saved.type)) savedLayout.Add(saved);

                if (data.lastSavedUnix > 0 && buildings.Count > 0)
                {
                    var elapsed = Math.Max(0, DateTimeOffset.UtcNow.ToUnixTimeSeconds() - data.lastSavedUnix);
                    elapsed = Math.Min(elapsed, OfflineHourCap * 3600L);
                    double orePerSecond = 0;
                    foreach (var building in buildings)
                        if (building != null && building.Type == BuildingType.OreCollector)
                            orePerSecond += building.OrePerSecond;
                    PendingOfflineOre = (int)Math.Min(2000000000d, Math.Floor(orePerSecond * elapsed * .65d));
                    Ore = (int)Math.Min(2000000000L, (long)Ore + PendingOfflineOre);
                }

                saveDirty = false;
                autosaveTimer = 0;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not load Shape Guard progress: {exception.Message}");
                return false;
            }
        }

        private void SaveProgress()
        {
            try
            {
                var data = new SaveData
                {
                    gold = Gold,
                    ore = Ore,
                    clearedWave = ClearedWave,
                    gameSpeed = GameSpeed,
                    coreShards = CoreShards,
                    highestBossWave = HighestBossWave,
                    bossesDefeated = BossesDefeated,
                    prestigeCount = PrestigeCount,
                    permanentPowerLevel = PermanentPowerLevel,
                    permanentEconomyLevel = PermanentEconomyLevel,
                    permanentCoreLevel = PermanentCoreLevel,
                    offlineLevel = OfflineLevel,
                    autoWaveUnlocked = AutoWaveUnlocked,
                    autoUpgradeUnlocked = AutoUpgradeUnlocked,
                    autoWaveEnabled = AutoWaveEnabled,
                    autoUpgradeEnabled = AutoUpgradeEnabled,
                    stopBeforeBoss = StopBeforeBoss,
                    lastSavedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                for (var index = 0; index < pathUnlocked.Length; index++)
                    if (pathUnlocked[index]) data.unlockedPaths.Add(index);
                for (var index = 0; index < permanentBlueprints.Length; index++)
                    if (permanentBlueprints[index]) data.unlockedBuildingTypes.Add(index);
                foreach (var building in buildings)
                {
                    if (building == null) continue;
                    var position = building == movingBuilding ? moveOrigin : building.transform.position;
                    data.buildings.Add(new BuildingSaveData
                    {
                        type = building.Type,
                        level = building.Level,
                        masteryRank = building.MasteryRank,
                        evolution = building.Evolution,
                        x = position.x,
                        y = position.y
                    });
                }
                foreach (var saved in savedLayout)
                    data.savedLayout.Add(new BuildingSaveData
                    {
                        type = saved.type,
                        level = saved.level,
                        masteryRank = saved.masteryRank,
                        evolution = saved.evolution,
                        x = saved.x,
                        y = saved.y
                    });

                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
                saveDirty = false;
                autosaveTimer = 0;
            }
            catch (Exception exception)
            {
                autosaveTimer = 0;
                Debug.LogWarning($"Could not save Shape Guard progress: {exception.Message}");
            }
        }

        public void ShowTracer(Vector3 start, Vector3 end, Color? color = null, float width = .07f,
            float duration = .08f, bool jagged = false)
        {
            CombatFx.Tracer(start, end, color ?? GameBalance.Defense, width, duration, jagged);
        }

        public void ShowImpact(Vector3 position, Color color, float size = 1f, bool heavy = false) =>
            CombatFx.Impact(position, color, size, heavy);

        public void ShowEnemyDeath(Vector3 position, Color color) => CombatFx.EnemyBurst(position, color);
    }
}
