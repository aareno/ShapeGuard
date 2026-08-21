using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MeadowGuard
{
    public sealed class GameController : MonoBehaviour
    {
        public readonly Vector3[] Path =
        {
            new(-11.5f, 2.9f), new(-7.5f, 2.9f), new(-5.7f, .4f), new(-2.2f, .4f),
            new(-.2f, -2.5f), new(3.2f, -2.5f), new(5.1f, .2f), new(8.7f, .2f)
        };

        public int Gold { get; private set; } = 260;
        public int Ore { get; private set; } = 140;
        public int ClearedWave { get; private set; } = 1;
        public int ActiveWave { get; private set; }
        public int CoreHealth { get; private set; } = 10;
        public int CoreMaxHealth => 10;
        public bool ChallengeQueued { get; private set; }
        public bool IsChallenge { get; private set; }
        public bool IsGameOver => transitioning;
        public string Announcement { get; private set; } = "Wave 1: gold farming";

        public IReadOnlyList<Building> Buildings => buildings;
        public Building SelectedBuilding { get; private set; }
        public BuildingKind? PlacementKind { get; private set; }

        private readonly List<Enemy> enemies = new();
        private readonly List<Building> buildings = new();
        private Camera gameCamera;
        private GameObject placementGhost;
        private int spawnRemaining;
        private int spawnedCount;
        private float spawnTimer;
        private float announcementTimer;
        private bool transitioning;
        private float transitionTimer;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            SetupCamera();
            CreateWorld();
            gameObject.AddComponent<GameHud>();
            StartWave(false);
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
            gameCamera.orthographicSize = 8.1f;
            gameCamera.transform.position = new Vector3(0, 0, -10);
            gameCamera.backgroundColor = new Color(.09f, .16f, .12f);
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private void CreateWorld()
        {
            var texture = Resources.Load<Texture2D>("Art/MeadowGround");
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Clamp;
                var ground = new GameObject("Meadow Ground");
                var renderer = ground.AddComponent<SpriteRenderer>();
                renderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * .5f, 100);
                renderer.sortingOrder = -10;
                ground.transform.localScale = new Vector3(2.15f, 1.36f, 1);
            }

            var pathObject = new GameObject("Monster Path");
            var line = pathObject.AddComponent<LineRenderer>();
            line.positionCount = Path.Length;
            line.SetPositions(Path);
            line.startWidth = 1.5f;
            line.endWidth = 1.5f;
            line.numCornerVertices = 8;
            line.numCapVertices = 8;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(.48f, .32f, .16f, .72f);
            line.endColor = new Color(.48f, .32f, .16f, .72f);
            line.sortingOrder = -2;

            var core = new GameObject("Village Core");
            core.transform.position = Path[^1];
            VisualFactory.Part(core.transform, "Shadow", VisualFactory.Circle, new Color(.06f, .08f, .05f, .4f),
                new Vector3(.18f, -.2f), new Vector3(2.1f, 1.15f), 1);
            VisualFactory.Part(core.transform, "Stone", VisualFactory.Circle, new Color(.24f, .29f, .34f), Vector3.zero, Vector3.one * 1.75f, 2);
            VisualFactory.Part(core.transform, "Crystal", VisualFactory.Square, new Color(.35f, .92f, 1f),
                new Vector3(0, .15f), new Vector3(.75f, 1.12f), 3).transform.localRotation = Quaternion.Euler(0, 0, 45);
        }

        private void Update()
        {
            announcementTimer -= Time.deltaTime;
            HandleWave();
            HandlePointer();
        }

        private void HandleWave()
        {
            if (transitioning)
            {
                transitionTimer -= Time.deltaTime;
                if (transitionTimer <= 0)
                {
                    transitioning = false;
                    StartWave(ChallengeQueued);
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
                    spawnTimer = Mathf.Max(.3f, 1.05f - ActiveWave * .025f);
                }
            }
            else if (enemies.Count == 0)
            {
                FinishWave();
            }
        }

        private void StartWave(bool challenge)
        {
            IsChallenge = challenge;
            ChallengeQueued = false;
            ActiveWave = challenge ? ClearedWave + 1 : ClearedWave;
            CoreHealth = CoreMaxHealth;
            spawnRemaining = 6 + ActiveWave * 2;
            spawnedCount = 0;
            spawnTimer = .4f;
            Announce(challenge ? $"CHALLENGE WAVE {ActiveWave}" : $"Wave {ActiveWave}: gold farming", 2.2f);
        }

        private void SpawnEnemy()
        {
            var enemyObject = new GameObject($"Monster {spawnedCount + 1}");
            var enemy = enemyObject.AddComponent<Enemy>();
            enemies.Add(enemy);
            var elite = (spawnedCount + 1) % 6 == 0;
            enemy.Initialize(this, ActiveWave, elite);
        }

        private void FinishWave()
        {
            if (IsChallenge)
            {
                ClearedWave = ActiveWave;
                var bonus = 25 + ClearedWave * 5;
                Ore += bonus;
                Announce($"Wave {ActiveWave} cleared! +{bonus} ore", 2.5f);
            }
            else Announce($"Farm wave complete — earned gold", 1.8f);
            transitioning = true;
            transitionTimer = 2f;
        }

        private void FailWave()
        {
            foreach (var enemy in enemies.ToArray()) if (enemy != null) Destroy(enemy.gameObject);
            enemies.Clear();
            ChallengeQueued = false;
            Announce(IsChallenge
                ? $"Wave {ActiveWave} lost — returning to wave {ClearedWave}"
                : $"Core breached — replaying wave {ClearedWave}", 3f);
            transitioning = true;
            transitionTimer = 2.5f;
            IsChallenge = false;
        }

        private void HandlePointer()
        {
            var mouse = Mouse.current;
            if (mouse == null || gameCamera == null) return;
            var screen = mouse.position.ReadValue();
            var world = gameCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -gameCamera.transform.position.z));
            world.z = 0;

            if (PlacementKind.HasValue)
            {
                if (placementGhost == null) CreateGhost();
                var snapped = Snap(world);
                placementGhost.transform.position = snapped;
                var valid = CanPlace(snapped);
                placementGhost.GetComponentInChildren<SpriteRenderer>().color = valid
                    ? new Color(.4f, 1f, .55f, .58f) : new Color(1f, .25f, .22f, .58f);
                if (mouse.rightButton.wasPressedThisFrame) CancelPlacement();
                if (mouse.leftButton.wasPressedThisFrame && !PointerOverHud(screen) && valid) PlaceBuilding(snapped);
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame && !PointerOverHud(screen))
            {
                SelectedBuilding = null;
                var bestDistance = .75f;
                foreach (var building in buildings)
                {
                    var distance = Vector2.Distance(world, building.transform.position);
                    if (distance >= bestDistance) continue;
                    bestDistance = distance;
                    SelectedBuilding = building;
                }
            }
        }

        private static bool PointerOverHud(Vector2 screen) => screen.y < 145 || screen.y > Screen.height - 95;
        private static Vector3 Snap(Vector3 point) => new(Mathf.Round(point.x * 2) / 2f, Mathf.Round(point.y * 2) / 2f, 0);

        private bool CanPlace(Vector3 point)
        {
            if (point.x < -10.4f || point.x > 7.1f || point.y < -5.8f || point.y > 5.8f) return false;
            foreach (var building in buildings)
                if (Vector2.Distance(point, building.transform.position) < 1.45f) return false;
            for (var i = 0; i < Path.Length - 1; i++)
                if (DistanceToSegment(point, Path[i], Path[i + 1]) < 1.35f) return false;
            return true;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var delta = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(point - a, delta) / delta.sqrMagnitude);
            return Vector2.Distance(point, a + delta * t);
        }

        public void BeginPlacement(BuildingKind kind)
        {
            SelectedBuilding = null;
            PlacementKind = kind;
            CreateGhost();
        }

        private void CreateGhost()
        {
            if (placementGhost != null) Destroy(placementGhost);
            placementGhost = new GameObject("Placement Preview");
            VisualFactory.Part(placementGhost.transform, "Range", VisualFactory.Circle, Color.white, Vector3.zero,
                PlacementKind == BuildingKind.Cannon ? Vector3.one * 6.8f : Vector3.one * 1.35f, 3);
        }

        public void CancelPlacement()
        {
            PlacementKind = null;
            if (placementGhost != null) Destroy(placementGhost);
        }

        private void PlaceBuilding(Vector3 point)
        {
            var kind = PlacementKind.Value;
            var cost = Balance.PlaceCost(kind);
            if (Gold < cost) { Announce("Not enough gold", 1.5f); return; }
            Gold -= cost;
            var buildingObject = new GameObject(Balance.Name(kind));
            buildingObject.transform.position = point;
            var building = buildingObject.AddComponent<Building>();
            building.Initialize(this, kind);
            buildings.Add(building);
            SelectedBuilding = building;
            CancelPlacement();
        }

        public void QueueNextWave()
        {
            if (ChallengeQueued || IsChallenge) return;
            ChallengeQueued = true;
            Announce($"Wave {ClearedWave + 1} queued", 1.8f);
        }

        public bool TryUpgradeSelected()
        {
            if (SelectedBuilding == null) return false;
            if (SelectedBuilding.TryUpgrade()) { Announce("Building upgraded!", 1.5f); return true; }
            Announce($"Need more {SelectedBuilding.UpgradeCurrency}", 1.5f);
            return false;
        }

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

        public Enemy FindClosestEnemy(Vector3 point, float range)
        {
            Enemy best = null;
            var bestSqr = range * range;
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                var sqr = (enemy.transform.position - point).sqrMagnitude;
                if (sqr >= bestSqr) continue;
                bestSqr = sqr;
                best = enemy;
            }
            return best;
        }

        public void EnemyDefeated(Enemy enemy, int reward)
        {
            enemies.Remove(enemy);
            Gold += reward;
        }

        public void EnemyReachedCore(Enemy enemy)
        {
            enemies.Remove(enemy);
            CoreHealth--;
            if (CoreHealth <= 0) FailWave();
        }

        public void AddGold(int amount) => Gold += amount;
        public void AddOre(int amount) => Ore += amount;

        public void Announce(string message, float duration)
        {
            Announcement = message;
            announcementTimer = duration;
        }

        public bool ShowAnnouncement => announcementTimer > 0;

        public void ShowTracer(Vector3 start, Vector3 end, Color color)
        {
            var tracer = new GameObject("Cannon Shot");
            var line = tracer.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = .09f;
            line.endWidth = .03f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = line.endColor = color;
            line.sortingOrder = 20;
            Destroy(tracer, .08f);
        }
    }
}
