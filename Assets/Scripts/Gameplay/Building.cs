using UnityEngine;

namespace MeadowGuard
{
    public sealed class Building : MonoBehaviour
    {
        public BuildingKind Kind { get; private set; }
        public int Level { get; private set; } = 1;
        public float SelectionRadius => .75f;
        public int UpgradeCost => Kind == BuildingKind.Cannon ? 35 * Level : 90 * Level;
        public string UpgradeCurrency => Kind == BuildingKind.Cannon ? "ore" : "gold";

        private GameController game;
        private Transform movingPart;
        private float timer;

        public void Initialize(GameController owner, BuildingKind kind)
        {
            game = owner;
            Kind = kind;
            name = $"{Balance.Name(kind)} L1";
            BuildVisual();
        }

        private void BuildVisual()
        {
            var shadow = VisualFactory.Part(transform, "Shadow", VisualFactory.Circle,
                new Color(0.08f, .12f, .08f, .35f), new Vector3(.12f, -.12f, 0), new Vector3(1.45f, .8f), 4);
            shadow.transform.localRotation = Quaternion.Euler(0, 0, -8);
            VisualFactory.Part(transform, "Base", VisualFactory.Circle, new Color(.30f, .25f, .19f), Vector3.zero,
                Vector3.one * 1.35f, 5);
            VisualFactory.Part(transform, "Rim", VisualFactory.Circle, Balance.Color(Kind), Vector3.zero,
                Vector3.one * 1.08f, 6);

            if (Kind == BuildingKind.Cannon)
            {
                movingPart = new GameObject("Turret").transform;
                movingPart.SetParent(transform, false);
                VisualFactory.Part(movingPart, "Barrel", VisualFactory.Square, new Color(.12f, .16f, .2f),
                    new Vector3(.52f, 0, 0), new Vector3(.95f, .25f), 8);
                VisualFactory.Part(movingPart, "Cap", VisualFactory.Circle, new Color(.42f, .49f, .55f),
                    Vector3.zero, Vector3.one * .78f, 9);
            }
            else
            {
                VisualFactory.Part(transform, Kind == BuildingKind.GoldCollector ? "Coin" : "Crystal",
                    Kind == BuildingKind.GoldCollector ? VisualFactory.Circle : VisualFactory.Square,
                    Kind == BuildingKind.GoldCollector ? new Color(1f, .88f, .25f) : new Color(.48f, .95f, 1f),
                    new Vector3(0, .18f, 0), Kind == BuildingKind.GoldCollector ? new Vector3(.58f, .58f) : new Vector3(.5f, .72f), 8);
                movingPart = transform;
            }
        }

        private void Update()
        {
            if (game == null || game.IsGameOver) return;
            timer -= Time.deltaTime;
            if (Kind == BuildingKind.Cannon) TickCannon();
            else TickCollector();
        }

        private void TickCannon()
        {
            var target = game.FindClosestEnemy(transform.position, 3.4f + Level * .35f);
            if (target == null) return;
            var direction = target.transform.position - transform.position;
            movingPart.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            if (timer > 0) return;
            timer = Mathf.Max(.24f, .82f - Level * .07f);
            target.TakeDamage(10f + Level * 7f);
            game.ShowTracer(transform.position, target.transform.position, new Color(1f, .84f, .28f));
        }

        private void TickCollector()
        {
            if (timer > 0) return;
            timer = Mathf.Max(2.5f, 5.5f - Level * .35f);
            var amount = 3 + Level * 2;
            if (Kind == BuildingKind.GoldCollector) game.AddGold(amount);
            else game.AddOre(amount);
            transform.localScale = Vector3.one * 1.12f;
        }

        private void LateUpdate()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 8f);
        }

        public bool TryUpgrade()
        {
            if (!game.TrySpend(UpgradeCurrency, UpgradeCost)) return false;
            Level++;
            name = $"{Balance.Name(Kind)} L{Level}";
            transform.localScale = Vector3.one * 1.2f;
            return true;
        }
    }
}
