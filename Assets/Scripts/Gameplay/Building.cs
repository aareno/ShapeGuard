using UnityEngine;

namespace ShapeGuard
{
    public sealed class Building : MonoBehaviour
    {
        public BuildingType Type { get; private set; }
        public int Level { get; private set; } = 1;
        public int UpgradeCost
        {
            get
            {
                var baseCost = Type == BuildingType.TriangleDefense ? Level * 35 : Level * 80;
                var multiplier = Type == BuildingType.TriangleDefense && game != null ? game.DefenseCostMultiplier : 1f;
                return Mathf.Max(1, Mathf.RoundToInt(baseCost * multiplier));
            }
        }
        public string UpgradeCurrency => Type == BuildingType.TriangleDefense ? "ore" : "gold";
        public float Range => (3.8f + (Level - 1) * .35f) * (game != null ? game.DefenseRangeMultiplier : 1f);
        public float Damage => (14f + Level * 6f) * (game != null ? game.DefenseDamageMultiplier : 1f);
        public float FireInterval => Mathf.Max(.28f, (.9f - Level * .07f) * (game != null ? game.DefenseFireIntervalMultiplier : 1f));
        public float Dps => Damage / FireInterval;
        public float OrePerSecond => OreAmount / OreInterval;
        public int OreAmount => Mathf.RoundToInt((3 + Level * 2) * (game != null ? game.OreAmountMultiplier : 1f));
        public float OreInterval => Mathf.Max(2f, (5.5f - Level * .35f) * (game != null ? game.OreIntervalMultiplier : 1f));

        private GameController game;
        private SpriteRenderer rangeRing;
        private float timer;

        public void Initialize(GameController owner, BuildingType type, int level = 1)
        {
            game = owner;
            Type = type;
            Level = Mathf.Max(1, level);
            name = $"{GameBalance.Name(type)} L{Level}";
            BuildVisual();
        }

        private void BuildVisual()
        {
            VisualFactory.Part(transform, "Shadow", VisualFactory.Circle, new Color(0, 0, 0, .25f),
                new Vector3(.1f, -.12f, 0), new Vector3(1.35f, .7f, 1), 3);
            if (Type == BuildingType.TriangleDefense)
            {
                rangeRing = VisualFactory.Part(transform, "Range", VisualFactory.Ring,
                    new Color(GameBalance.Text.r, GameBalance.Text.g, GameBalance.Text.b, .48f), Vector3.zero,
                    Vector3.one * (Range * 2f / .95f), 2);
                rangeRing.enabled = false;
                VisualFactory.Part(transform, "Triangle", VisualFactory.Polygon(3), GameBalance.Defense,
                    Vector3.zero, Vector3.one * 1.2f, 5);
            }
            else
            {
                VisualFactory.Part(transform, "Collector Base", VisualFactory.Circle, GameBalance.Collector,
                    Vector3.zero, Vector3.one * 1.3f, 5);
                VisualFactory.Part(transform, "Ore", VisualFactory.Polygon(4), GameBalance.Ore,
                    new Vector3(0, .08f, 0), Vector3.one * .58f, 6);
            }
        }

        private void Update()
        {
            if (game == null) return;
            if (rangeRing != null)
            {
                rangeRing.enabled = game.SelectedBuilding == this;
                rangeRing.transform.localScale = Vector3.one * (Range * 2f / .95f);
            }
            if (game.IsTransitioning) return;
            timer -= Time.deltaTime;
            if (Type == BuildingType.TriangleDefense) Attack();
            else CollectOre();
        }

        private void Attack()
        {
            var target = game.FindClosestEnemy(transform.position, Range);
            if (target == null || timer > 0) return;
            timer = FireInterval;
            target.TakeDamage(Damage);
            game.ShowTracer(transform.position, target.transform.position);
        }

        private void CollectOre()
        {
            if (timer > 0) return;
            timer = OreInterval;
            game.AddOre(OreAmount);
            transform.localScale = Vector3.one * 1.12f;
        }

        private void LateUpdate() => transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 8f);

        public bool Upgrade()
        {
            if (!game.TrySpend(UpgradeCurrency, UpgradeCost)) return false;
            Level++;
            name = $"{GameBalance.Name(Type)} L{Level}";
            if (rangeRing != null) rangeRing.transform.localScale = Vector3.one * (Range * 2f / .95f);
            transform.localScale = Vector3.one * 1.18f;
            return true;
        }
    }
}
