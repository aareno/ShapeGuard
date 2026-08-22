using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShapeGuard
{
    public sealed class GameController : MonoBehaviour
    {
        private const int SaveVersion = 1;
        private const float AutosaveInterval = 5f;

        [Serializable]
        private sealed class SaveData
        {
            public int version = SaveVersion;
            public int gold;
            public int ore;
            public int clearedWave;
            public float gameSpeed;
            public List<BuildingSaveData> buildings = new();
        }

        [Serializable]
        private sealed class BuildingSaveData
        {
            public BuildingType type;
            public int level;
            public float x;
            public float y;
        }

        public int Gold { get; private set; } = GameBalance.StartingGold;
        public int Ore { get; private set; } = GameBalance.StartingOre;
        public int ClearedWave { get; private set; }
        public int ActiveWave { get; private set; }
        public int CoreHealth { get; private set; } = GameBalance.CoreHealth;
        public int UnlockedPaths => Mathf.Clamp(1 + ClearedWave / 10, 1, 10);
        public bool HasStarted { get; private set; }
        public bool ProgressionActive { get; private set; }
        public bool ProgressionQueued { get; private set; }
        public bool IsTransitioning { get; private set; }
        public float GameSpeed { get; private set; } = GameBalance.DefaultGameSpeed;
        public string Announcement { get; private set; }
        public bool ShowAnnouncement => announcementTimer > 0;
        public Building SelectedBuilding { get; private set; }
        public BuildingType? PlacementType { get; private set; }

        private readonly List<Vector3[]> paths = new();
        private readonly List<LineRenderer> pathLines = new();
        private readonly List<Enemy> enemies = new();
        private readonly List<Building> buildings = new();
        private readonly List<(SpriteRenderer renderer, Color color)> previewParts = new();
        private Camera gameCamera;
        private GameObject placementPreview;
        private int spawnRemaining;
        private int spawnedCount;
        private float spawnTimer;
        private float transitionTimer;
        private float announcementTimer;
        private bool cameraDragging;
        private Vector2 lastDragPosition;
        private bool saveDirty;
        private float autosaveTimer;

        private static string SavePath => Path.Combine(Application.persistentDataPath, "shape-guard-save.json");

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.antiAliasing = 4;
            Time.timeScale = GameSpeed;
            SetupCamera();
            CreateMap();
            var loaded = LoadProgress();
            gameObject.AddComponent<GameHud>();
            if (loaded) StartWave(false);
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
            gameCamera.transform.position = new Vector3(0, 0, -10);
            gameCamera.backgroundColor = GameBalance.Ground;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private void CreateMap()
        {
            foreach (var path in MapLayout.CreatePaths()) paths.Add(path);
            var ground = new GameObject("Large Minimal Map");
            var groundRenderer = ground.AddComponent<SpriteRenderer>();
            groundRenderer.sprite = VisualFactory.Square;
            groundRenderer.color = GameBalance.Ground;
            groundRenderer.sortingOrder = -10;
            ground.transform.position = MapLayout.Bounds.center;
            ground.transform.localScale = new Vector3(MapLayout.Bounds.width, MapLayout.Bounds.height, 1);

            for (var index = 0; index < paths.Count; index++)
            {
                var pathObject = new GameObject($"Path {index + 1}{(index == 0 ? " - Open" : " - Future")}");
                var line = pathObject.AddComponent<LineRenderer>();
                line.positionCount = paths[index].Length;
                line.SetPositions(paths[index]);
                line.startWidth = 1.15f;
                line.endWidth = 1.15f;
                line.numCornerVertices = 10;
                line.numCapVertices = 10;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.sortingOrder = -2;
                pathLines.Add(line);
            }
            RefreshPathColors();

            var core = new GameObject("Core");
            core.transform.position = MapLayout.CorePosition;
            VisualFactory.Part(core.transform, "Core", VisualFactory.Polygon(6), GameBalance.Gold,
                Vector3.zero, Vector3.one * 1.7f, 2);
            VisualFactory.Part(core.transform, "Center", VisualFactory.Circle, GameBalance.Ground,
                Vector3.zero, Vector3.one * .55f, 3);
        }

        private void RefreshPathColors()
        {
            for (var index = 0; index < pathLines.Count; index++)
            {
                var color = index < UnlockedPaths ? GameBalance.PathOpen : GameBalance.PathLocked;
                pathLines[index].startColor = color;
                pathLines[index].endColor = color;
                pathLines[index].sortingOrder = index < UnlockedPaths ? -1 : -2;
                pathLines[index].gameObject.name = $"Path {index + 1} - {(index < UnlockedPaths ? "Open" : $"Unlock Wave {index * 10}")}";
            }
        }

        private void Update()
        {
            announcementTimer -= Time.deltaTime;
            autosaveTimer += Time.unscaledDeltaTime;
            if (saveDirty && autosaveTimer >= AutosaveInterval) SaveProgress();
            HandleCamera();
            HandleWave();
            HandlePointer();
        }

        private void HandleCamera()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            var screen = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame && !PlacementType.HasValue && !PointerOverHud(screen))
            {
                cameraDragging = true;
                lastDragPosition = screen;
            }
            if (mouse.leftButton.wasReleasedThisFrame) cameraDragging = false;
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
                    spawnTimer = Mathf.Max(.28f, 1f - ActiveWave * .02f);
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
            CoreHealth = GameBalance.CoreHealth;
            spawnRemaining = 7 + ActiveWave * 2;
            spawnedCount = 0;
            spawnTimer = .35f;
            Announce(progression ? $"PROGRESSION WAVE {ActiveWave}" : $"FARMING WAVE {ActiveWave}", 2f);
        }

        private void SpawnEnemy()
        {
            var pathIndex = spawnedCount % UnlockedPaths;
            var enemyObject = new GameObject($"Red Circle - Path {pathIndex + 1}");
            var enemy = enemyObject.AddComponent<Enemy>();
            enemies.Add(enemy);
            enemy.Initialize(this, paths[pathIndex], ActiveWave);
        }

        private void FinishWave()
        {
            if (ProgressionActive)
            {
                var oldPathCount = UnlockedPaths;
                ClearedWave = ActiveWave;
                ProgressionQueued = true;
                if (UnlockedPaths > oldPathCount)
                {
                    RefreshPathColors();
                    Announce($"PATH {UnlockedPaths} OPENED - WAVE {ActiveWave + 1} NEXT", 2.8f);
                }
                else Announce($"WAVE {ActiveWave} CLEARED - WAVE {ActiveWave + 1} NEXT", 2.3f);
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
                    part.renderer.color = valid ? new Color(part.color.r, part.color.g, part.color.b, .78f) : new Color(1, .2f, .2f, .72f);
                if (mouse.rightButton.wasPressedThisFrame) CancelPlacement();
                if (mouse.leftButton.wasPressedThisFrame && !PointerOverHud(screen) && valid) Place(point);
                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame || PointerOverHud(screen)) return;
            SelectedBuilding = null;
            var closest = .8f;
            foreach (var building in buildings)
            {
                var distance = Vector2.Distance(world, building.transform.position);
                if (distance >= closest) continue;
                closest = distance;
                SelectedBuilding = building;
            }
        }

        private bool PointerOverHud(Vector2 screen)
        {
            if (screen.y < 135 || screen.y > Screen.height - 90) return true;
            if (SelectedBuilding == null) return false;
            return SelectionPanelRect().Contains(new Vector2(screen.x, Screen.height - screen.y));
        }

        public Rect SelectionPanelRect()
        {
            const float width = 280;
            const float height = 132;
            if (SelectedBuilding == null) return new Rect(12, 100, width, height);
            var screen = gameCamera.WorldToScreenPoint(SelectedBuilding.transform.position);
            var x = Mathf.Clamp(screen.x - width * .5f, 12, Screen.width - width - 12);
            var y = Mathf.Clamp(Screen.height - screen.y - height - 28, 94, Screen.height - 145 - height);
            return new Rect(x, y, width, height);
        }

        public void BeginPlacement(BuildingType type)
        {
            SelectedBuilding = null;
            PlacementType = type;
            if (placementPreview != null) Destroy(placementPreview);
            placementPreview = new GameObject("Placement Preview");
            previewParts.Clear();
            if (type == BuildingType.TriangleDefense)
                AddPreview("Triangle", VisualFactory.Polygon(3), GameBalance.Defense, Vector3.zero, Vector3.one * 1.2f, 20);
            else
            {
                AddPreview("Collector", VisualFactory.Circle, GameBalance.Collector, Vector3.zero, Vector3.one * 1.3f, 20);
                AddPreview("Ore", VisualFactory.Polygon(4), GameBalance.Ore, new Vector3(0, .08f, 0), Vector3.one * .58f, 21);
            }
        }

        private void AddPreview(string name, Sprite sprite, Color color, Vector3 position, Vector3 scale, int order)
        {
            var renderer = VisualFactory.Part(placementPreview.transform, name, sprite, color, position, scale, order);
            previewParts.Add((renderer, color));
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
            if (!TrySpend(GameBalance.Currency(type), GameBalance.Cost(type)))
            {
                Announce($"NOT ENOUGH {GameBalance.Currency(type).ToUpperInvariant()}", 1.5f);
                return;
            }
            var buildingObject = new GameObject(GameBalance.Name(type));
            buildingObject.transform.position = point;
            var building = buildingObject.AddComponent<Building>();
            building.Initialize(this, type);
            buildings.Add(building);
            SelectedBuilding = building;
            CancelPlacement();
            saveDirty = true;
            SaveProgress();
        }

        private bool CanPlace(Vector3 point)
        {
            if (!MapLayout.Bounds.Contains(point)) return false;
            if (Vector2.Distance(point, MapLayout.CorePosition) < 1.8f) return false;
            foreach (var building in buildings)
                if (Vector2.Distance(point, building.transform.position) < 1.5f) return false;
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

        public void UpgradeSelected()
        {
            if (SelectedBuilding == null) return;
            if (SelectedBuilding.Upgrade())
            {
                Announce("UPGRADED", 1.4f);
                saveDirty = true;
                SaveProgress();
            }
            else Announce($"NEED MORE {SelectedBuilding.UpgradeCurrency.ToUpperInvariant()}", 1.4f);
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

        public void EnemyKilled(Enemy enemy, int reward)
        {
            enemies.Remove(enemy);
            Gold += reward;
            saveDirty = true;
        }

        public void EnemyReachedCore(Enemy enemy, int damage)
        {
            enemies.Remove(enemy);
            CoreHealth = Mathf.Max(0, CoreHealth - damage);
            if (CoreHealth <= 0) FailWave();
        }

        public void AddOre(int amount)
        {
            Ore += amount;
            saveDirty = true;
        }
        private void Announce(string text, float duration) { Announcement = text; announcementTimer = duration; }

        private bool LoadProgress()
        {
            if (!File.Exists(SavePath)) return false;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                if (data == null || data.version != SaveVersion) return false;

                Gold = Mathf.Max(0, data.gold);
                Ore = Mathf.Max(0, data.ore);
                ClearedWave = Mathf.Max(0, data.clearedWave);
                GameSpeed = data.gameSpeed >= 2.5f ? 3f : data.gameSpeed >= 1.5f ? 2f : 1f;
                Time.timeScale = GameSpeed;
                RefreshPathColors();

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
                        building.Initialize(this, savedBuilding.type, savedBuilding.level);
                        buildings.Add(building);
                    }
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
                    gameSpeed = GameSpeed
                };
                foreach (var building in buildings)
                {
                    if (building == null) continue;
                    data.buildings.Add(new BuildingSaveData
                    {
                        type = building.Type,
                        level = building.Level,
                        x = building.transform.position.x,
                        y = building.transform.position.y
                    });
                }

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

        public void ShowTracer(Vector3 start, Vector3 end)
        {
            var shot = new GameObject("Triangle Shot");
            var line = shot.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = .07f;
            line.endWidth = .025f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = line.endColor = GameBalance.Defense;
            line.sortingOrder = 30;
            Destroy(shot, .08f);
        }
    }
}
