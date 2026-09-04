using UnityEngine;

namespace ShapeGuard
{
    public sealed class Building : MonoBehaviour
    {
        public BuildingType Type { get; private set; }
        public int Level { get; private set; } = 1;
        public int MasteryRank { get; private set; }
        public int Evolution { get; private set; }
        public bool IsBeingMoved { get; private set; }
        public int NextMasteryRank => Mathf.Min(3, MasteryRank + 1);
        public int MasteryCost => GameBalance.MasteryUpgradeCost(Type, NextMasteryRank);
        public int MasteryRequiredLevel => GameBalance.MasteryRequiredBuildingLevel(NextMasteryRank);
        public int MasteryRequiredPaths => GameBalance.MasteryRequiredPathNodes(NextMasteryRank);
        public bool CanUpgradeMastery => game != null && GameBalance.IsDefense(Type) && MasteryRank < 3 &&
            Level >= MasteryRequiredLevel && game.UnlockedDefensePathCount(Type) >= MasteryRequiredPaths &&
            game.Ore >= MasteryCost;
        public int UpgradeCost
        {
            get
            {
                var baseCost = GameBalance.UpgradeCost(Type, Level);
                var multiplier = game == null ? 1f : GameBalance.IsDefense(Type)
                    ? game.DefenseCostMultiplierFor(Type) : game.CollectorCostMultiplier;
                return Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(2000000000f, baseCost * multiplier)));
            }
        }
        public string UpgradeCurrency => GameBalance.Currency(Type);
        public bool CanAffordUpgrade => game != null && (UpgradeCurrency == "ore" ? game.Ore : game.Gold) >= UpgradeCost;
        public float Range => GameBalance.BuildingRange(Type, Level) *
            (game != null ? game.DefenseRangeMultiplierFor(Type) : 1f) *
            (1f + MasteryRank * .04f) * (Evolution == 2
                ? Type == BuildingType.SupportDefense ? 1.2f : 1.15f : 1f);
        public float Damage => GameBalance.BuildingDamage(Type, Level) * (game != null
            ? game.DefenseDamageMultiplierFor(Type) * game.SupportDamageMultiplierAt(transform.position, this) : 1f) *
            (1f + MasteryRank * .12f) * (Evolution == 1 ? 1.3f : 1f);
        public float FireInterval => Mathf.Max(.18f,
            GameBalance.BuildingFireInterval(Type, Level) *
            (game != null ? game.DefenseFireIntervalMultiplierFor(Type) : 1f) *
            Mathf.Pow(.95f, MasteryRank) * (Evolution == 2 ? .8f : 1f));
        public float Dps => Damage / FireInterval;
        public float SupportBoost => GameBalance.SupportBoost(Level) *
            (game != null ? game.DefenseDamageMultiplierFor(Type) : 1f) *
            (1f + MasteryRank * .12f) * (Evolution == 1 ? 1.25f : 1f);
        public float OrePerSecond => OreAmount / OreInterval;
        public int OreAmount => Mathf.RoundToInt(Mathf.Min(2000000000f, GameBalance.CollectorOreAmount(Level) *
            (game != null ? game.OreAmountMultiplier : 1f)));
        public float OreInterval => Mathf.Max(1.5f, GameBalance.CollectorOreInterval(Level) *
            (game != null ? game.OreIntervalMultiplier : 1f));

        private GameController game;
        private SpriteRenderer rangeRing;
        private SpriteRenderer bodyRenderer;
        private Color bodyColor;
        private float timer;
        private SpriteRenderer milestoneRing;
        private int visualTier = -1;
        private int visualEvolution = -1;
        private int visualMastery = -1;

        public void Initialize(GameController owner, BuildingType type, int level = 1, int masteryRank = 0,
            int evolution = 0)
        {
            game = owner;
            Type = type;
            bodyColor = GameBalance.BuildingColor(type);
            Level = Mathf.Max(1, level);
            MasteryRank = GameBalance.IsDefense(type) ? Mathf.Clamp(masteryRank, 0, 3) : 0;
            Evolution = MasteryRank >= 3 ? Mathf.Clamp(evolution, 0, 2) : 0;
            name = $"{GameBalance.Name(type)} L{Level}";
            BuildVisual();
            milestoneRing = VisualFactory.GlowPart(transform, "Milestone Frame",
                GameBalance.IsDefense(type) ? VisualFactory.PolygonOutline(GameBalance.DefenseSides(type)) : VisualFactory.Ring,
                bodyColor, Vector3.zero, Vector3.one * 1.55f, 5, 1.4f);
            RefreshMilestoneVisual();
        }

        private void BuildVisual()
        {
            VisualFactory.Part(transform, "Shadow", VisualFactory.Circle, new Color(0, 0, 0, .55f),
                new Vector3(.09f, -.12f, 0), new Vector3(1.42f, .72f, 1), 3);
            if (GameBalance.IsDefense(Type))
            {
                rangeRing = VisualFactory.Part(transform, "Range", VisualFactory.Ring,
                    new Color(bodyColor.r, bodyColor.g, bodyColor.b, .26f), Vector3.zero,
                    Vector3.one * (Range * 2f / .95f), 2);
                rangeRing.enabled = false;
                BuildDefenseVisual();
            }
            else
            {
                VisualFactory.Part(transform, "Dark Housing", VisualFactory.Circle, GameBalance.Ground,
                    Vector3.zero, Vector3.one * 1.18f, 4);
                bodyRenderer = VisualFactory.GlowPart(transform, "Collector Ring", VisualFactory.Ring, bodyColor,
                    Vector3.zero, Vector3.one * 1.3f, 6);
                VisualFactory.GlowPart(transform, "Ore Crystal", VisualFactory.PolygonOutline(4), GameBalance.Ore,
                    new Vector3(0, .02f, 0), Vector3.one * .55f, 7, 1.25f);
            }
        }

        private void BuildDefenseVisual()
        {
            var sides = GameBalance.DefenseSides(Type);
            VisualFactory.Part(transform, "Dark Housing", VisualFactory.Polygon(sides), GameBalance.Ground,
                Vector3.zero, Vector3.one * 1.15f, 4);
            bodyRenderer = VisualFactory.GlowPart(transform, "Defense Frame", VisualFactory.PolygonOutline(sides),
                bodyColor, Vector3.zero, Vector3.one * 1.28f, 6);

            if (Type == BuildingType.ArcDefense)
            {
                VisualFactory.Part(transform, "Arc Core", VisualFactory.PolygonOutline(4), GameBalance.Text,
                    Vector3.zero, Vector3.one * .34f, 7);
            }
            else if (Type == BuildingType.PierceDefense)
            {
                VisualFactory.Part(transform, "Rail", VisualFactory.Square, GameBalance.Text,
                    Vector3.zero, new Vector3(.12f, .62f, 1), 7);
            }
            else if (Type == BuildingType.SupportDefense)
            {
                VisualFactory.Part(transform, "Support Core", VisualFactory.Ring, GameBalance.Text,
                    Vector3.zero, Vector3.one * .44f, 7);
            }
            else if (Type == BuildingType.BlastDefense)
            {
                VisualFactory.Part(transform, "Blast Core", VisualFactory.Circle, GameBalance.Gold,
                    Vector3.zero, Vector3.one * .28f, 7);
            }
            else if (Type == BuildingType.FrostDefense)
            {
                VisualFactory.Part(transform, "Frost Crystal", VisualFactory.PolygonOutline(4), GameBalance.Text,
                    Vector3.zero, Vector3.one * .46f, 7);
            }
            else if (Type == BuildingType.PrismDefense)
            {
                VisualFactory.Part(transform, "Prism Core", VisualFactory.Polygon(3), GameBalance.Text,
                    Vector3.zero, Vector3.one * .38f, 7);
            }
            else if (Type == BuildingType.PulseDefense)
            {
                VisualFactory.Part(transform, "Pulse Core", VisualFactory.Ring, GameBalance.Text,
                    Vector3.zero, Vector3.one * .5f, 7);
                VisualFactory.Part(transform, "Pulse Center", VisualFactory.Circle, bodyColor,
                    Vector3.zero, Vector3.one * .16f, 8);
            }
            else if (Type == BuildingType.VolleyDefense)
            {
                VisualFactory.Part(transform, "Volley Rails", VisualFactory.Square, GameBalance.Text,
                    Vector3.zero, new Vector3(.42f, .14f, 1), 7);
            }
            else
            {
                VisualFactory.Part(transform, "Emitter", VisualFactory.Circle, GameBalance.Text,
                    new Vector3(0, -.05f, 0), Vector3.one * .16f, 7);
            }
        }

        private void Update()
        {
            if (game == null) return;
            RefreshMilestoneVisual();
            if (rangeRing != null)
            {
                rangeRing.enabled = !IsBeingMoved && game.SelectedBuilding == this;
                rangeRing.transform.localScale = Vector3.one * (Range * 2f / .95f);
            }
            if (IsBeingMoved) return;
            if (game.IsTransitioning) return;
            timer -= Time.deltaTime;
            if (Type == BuildingType.OreCollector) CollectOre();
            else if (Type != BuildingType.SupportDefense) Attack();
        }

        private void Attack()
        {
            if (timer > 0) return;
            switch (Type)
            {
                case BuildingType.ArcDefense: AttackArc(); break;
                case BuildingType.PierceDefense: AttackPierce(); break;
                case BuildingType.BlastDefense: AttackBlast(); break;
                case BuildingType.FrostDefense: AttackFrost(); break;
                case BuildingType.PrismDefense: AttackPrism(); break;
                case BuildingType.PulseDefense: AttackPulse(); break;
                case BuildingType.VolleyDefense: AttackVolley(); break;
                default: AttackStandard(); break;
            }
        }

        private void AttackStandard()
        {
            var target = game.FindClosestEnemy(transform.position, Range);
            if (target == null) return;
            timer = FireInterval;
            game.PlayAttackSound(Type, transform.position);
            target.TakeDamage(Damage);
            game.ShowTracer(transform.position, target.transform.position, bodyColor);
        }

        private void AttackArc()
        {
            var targets = game.FindEnemiesInRange(transform.position, Range);
            if (targets.Count == 0) return;
            targets.Sort((left, right) =>
                (left.transform.position - transform.position).sqrMagnitude.CompareTo(
                    (right.transform.position - transform.position).sqrMagnitude));
            timer = FireInterval;
            game.PlayAttackSound(Type, transform.position);
            var start = transform.position;
            var hitCount = Mathf.Min(3 + Level / 8, Mathf.Min(6, targets.Count));
            for (var index = 0; index < hitCount; index++)
            {
                var target = targets[index];
                game.ShowTracer(start, target.transform.position, bodyColor, .1f, .12f);
                target.TakeDamage(Damage * Mathf.Pow(.78f, index));
                start = target.transform.position;
            }
        }

        private void AttackPierce()
        {
            var target = game.FindClosestEnemy(transform.position, Range);
            if (target == null) return;
            timer = FireInterval;
            game.PlayAttackSound(Type, transform.position);
            var direction = (target.transform.position - transform.position).normalized;
            var end = transform.position + direction * Range;
            var targets = game.FindEnemiesAlongLine(transform.position, end, .58f);
            foreach (var enemy in targets) enemy.TakeDamage(Damage);
            game.ShowTracer(transform.position, end, bodyColor, .15f, .14f);
        }

        private void AttackBlast()
        {
            var target = game.FindClosestEnemy(transform.position, Range);
            if (target == null) return;
            timer = FireInterval;
            game.PlayAttackSound(Type, transform.position);
            var impact = target.transform.position;
            var targets = game.FindEnemiesInRange(impact, Mathf.Min(3f, 1.8f + Level * .025f));
            foreach (var enemy in targets) enemy.TakeDamage(enemy == target ? Damage : Damage * .72f);
            game.ShowTracer(transform.position, impact, bodyColor, .13f, .16f);
            foreach (var enemy in targets)
                if (enemy != null && enemy != target)
                    game.ShowTracer(impact, enemy.transform.position, bodyColor, .045f, .11f);
        }

        private void AttackFrost()
        {
            var target = game.FindClosestEnemy(transform.position, Range);
            if (target == null) return;
            timer = FireInterval;
            game.PlayAttackSound(Type, transform.position);
            target.TakeDamage(Damage, transform.position, bodyColor);
            target.ApplySlow(.58f, 1.45f + Mathf.Min(1.1f, Level * .025f));
            game.ShowTracer(transform.position, target.transform.position, bodyColor, .09f, .13f);
        }

        private void AttackPrism()
        {
            var targets = game.FindEnemiesInRange(transform.position, Range);
            if (targets.Count == 0) return;
            targets.Sort((left, right) =>
                (left.transform.position - transform.position).sqrMagnitude.CompareTo(
                    (right.transform.position - transform.position).sqrMagnitude));
            timer = FireInterval;
            game.PlayAttackSound(Type, transform.position);
            var hitCount = Mathf.Min(3 + Level / 18, Mathf.Min(5, targets.Count));
            for (var index = 0; index < hitCount; index++)
            {
                var target = targets[index];
                target.TakeDamage(Damage * Mathf.Pow(.76f, index), transform.position, bodyColor);
                game.ShowTracer(transform.position, target.transform.position, bodyColor, .075f, .12f);
            }
        }

        private void AttackPulse()
        {
            var targets = game.FindEnemiesInRange(transform.position, Range);
            if (targets.Count == 0) return;
            timer = FireInterval;
            game.PlayAttackSound(Type, transform.position);
            foreach (var target in targets) target.TakeDamage(Damage, transform.position, bodyColor);
            game.ShowImpact(transform.position, bodyColor, Mathf.Max(1f, Range * .22f), true);
        }

        private void AttackVolley()
        {
            var targets = game.FindEnemiesInRange(transform.position, Range);
            if (targets.Count == 0) return;
            targets.Sort((left, right) =>
                (left.transform.position - transform.position).sqrMagnitude.CompareTo(
                    (right.transform.position - transform.position).sqrMagnitude));
            timer = FireInterval;
            game.PlayAttackSound(Type, transform.position);
            var hitCount = Mathf.Min(3 + Level / 12, Mathf.Min(6, targets.Count));
            for (var index = 0; index < hitCount; index++)
            {
                var target = targets[index];
                target.TakeDamage(Damage, transform.position, bodyColor);
                game.ShowTracer(transform.position, target.transform.position, bodyColor, .065f, .1f);
            }
        }

        private void CollectOre()
        {
            if (timer > 0) return;
            timer = OreInterval;
            game.AddOre(OreAmount);
            game.PlaySound(GameSound.OreCollected, .8f, transform.position);
            transform.localScale = Vector3.one * 1.12f;
        }

        private void LateUpdate() => transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 8f);

        public void BeginMove()
        {
            IsBeingMoved = true;
            if (bodyRenderer != null) bodyRenderer.color = new Color(bodyColor.r, bodyColor.g, bodyColor.b, .75f);
        }

        public void SetMoveValidity(bool valid)
        {
            if (bodyRenderer != null) bodyRenderer.color = valid
                ? new Color(bodyColor.r, bodyColor.g, bodyColor.b, .75f)
                : new Color(1f, .2f, .2f, .8f);
        }

        public void EndMove()
        {
            IsBeingMoved = false;
            if (bodyRenderer != null) bodyRenderer.color = bodyColor;
        }

        public bool Upgrade()
        {
            if (!game.TrySpend(UpgradeCurrency, UpgradeCost)) return false;
            Level++;
            name = $"{GameBalance.Name(Type)} L{Level}";
            RefreshMilestoneVisual();
            if (rangeRing != null) rangeRing.transform.localScale = Vector3.one * (Range * 2f / .95f);
            transform.localScale = Vector3.one * 1.18f;
            return true;
        }

        private void RefreshMilestoneVisual()
        {
            if (milestoneRing == null || game == null) return;
            var tier = Level >= 50 ? 4 : Level >= 25 ? 3 : Level >= 10 ? 2 : Level >= 5 ? 1 : 0;
            var evolution = Evolution;
            if (tier == visualTier && evolution == visualEvolution && MasteryRank == visualMastery) return;
            visualTier = tier;
            visualEvolution = evolution;
            visualMastery = MasteryRank;
            milestoneRing.enabled = tier > 0 || evolution > 0 || MasteryRank > 0;
            milestoneRing.color = evolution == 1 ? GameBalance.Gold : evolution == 2 ? GameBalance.Ore : bodyColor;
            milestoneRing.transform.localScale = Vector3.one *
                (1.48f + tier * .11f + MasteryRank * .05f + (evolution > 0 ? .12f : 0f));
            var evolutionName = evolution > 0 ? $" [{GameBalance.EvolutionName(Type, evolution)}]" :
                MasteryRank > 0 ? $" [MASTERY {MasteryRank}]" : "";
            name = $"{GameBalance.Name(Type)} L{Level}{evolutionName}";
        }

        public bool UpgradeMastery()
        {
            if (!CanUpgradeMastery || !game.TrySpend("ore", MasteryCost)) return false;
            MasteryRank++;
            RefreshMilestoneVisual();
            transform.localScale = Vector3.one * 1.28f;
            return true;
        }

        public bool ChooseEvolution(int evolution)
        {
            if (MasteryRank < 3 || Evolution != 0 || evolution < 1 || evolution > 2) return false;
            Evolution = evolution;
            RefreshMilestoneVisual();
            transform.localScale = Vector3.one * 1.35f;
            return true;
        }
    }
}
