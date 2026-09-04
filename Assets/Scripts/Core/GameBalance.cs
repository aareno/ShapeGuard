using System.Collections.Generic;
using UnityEngine;

namespace ShapeGuard
{
    // Explicit values preserve compatibility with existing save files.
    public enum BuildingType
    {
        TriangleDefense = 0,
        OreCollector = 1,
        ArcDefense = 2,
        PierceDefense = 3,
        SupportDefense = 4,
        BlastDefense = 5,
        FrostDefense = 6,
        PrismDefense = 7,
        PulseDefense = 8,
        VolleyDefense = 9
    }

    public enum PathRewardKind
    {
        Start,
        Damage,
        Range,
        Speed,
        Gold,
        Ore,
        Core,
        Efficiency,
        DefenseUnlock
    }

    public enum BossKind
    {
        Bulwark,
        BroodCore,
        Leech,
        Splitter,
        Architect
    }

    public static class GameBalance
    {
        public const int StartingGold = 100;
        public const int StartingOre = 60;
        public const int DefenseCost = 60;
        public const int CollectorCost = 100;
        public const int CoreHealth = 10;
        public const float DefaultGameSpeed = 1f;
        public const float CameraZoomStep = 1.5f;
        public const float CameraMinimumZoom = 3f;
        public const float CameraMaximumZoom = 195f;
        public const int PathUnlockInterval = 5;
        public const int BossWaveInterval = 10;
        public const int MajorBossWaveInterval = 50;
        public const int PrestigeUnlockWave = 30;

        private const float DefenseUpgradeGrowth = 1.30f;
        private const float CollectorUpgradeGrowth = 1.33f;
        private const float BuildingCostGrowth = 1.22f;

        public static bool IsCollector(BuildingType type) => type == BuildingType.OreCollector;
        public static bool IsDefense(BuildingType type) => !IsCollector(type);
        public static int Cost(BuildingType type) => type switch
        {
            BuildingType.TriangleDefense => DefenseCost,
            BuildingType.OreCollector => CollectorCost,
            BuildingType.ArcDefense => 110,
            BuildingType.PierceDefense => 140,
            BuildingType.SupportDefense => 125,
            BuildingType.BlastDefense => 160,
            BuildingType.FrostDefense => 130,
            BuildingType.PrismDefense => 150,
            BuildingType.PulseDefense => 170,
            BuildingType.VolleyDefense => 145,
            _ => DefenseCost
        };
        public static string Currency(BuildingType type) => IsCollector(type) ? "gold" : "ore";
        public static string Name(BuildingType type) => type switch
        {
            BuildingType.TriangleDefense => "Triangle Defense",
            BuildingType.OreCollector => "Ore Collector",
            BuildingType.ArcDefense => "Arc Defense",
            BuildingType.PierceDefense => "Pierce Defense",
            BuildingType.SupportDefense => "Support Beacon",
            BuildingType.BlastDefense => "Blast Defense",
            BuildingType.FrostDefense => "Frost Defense",
            BuildingType.PrismDefense => "Prism Defense",
            BuildingType.PulseDefense => "Pulse Defense",
            BuildingType.VolleyDefense => "Volley Defense",
            _ => "Unknown Structure"
        };
        public static string Role(BuildingType type) => type switch
        {
            BuildingType.TriangleDefense => "RAPID SINGLE TARGET",
            BuildingType.OreCollector => "GENERATES ORE",
            BuildingType.ArcDefense => "CHAIN LIGHTNING",
            BuildingType.PierceDefense => "PIERCING RAIL SHOT",
            BuildingType.SupportDefense => "BOOSTS NEARBY DAMAGE",
            BuildingType.BlastDefense => "AREA DAMAGE",
            BuildingType.FrostDefense => "SLOWS ENEMIES",
            BuildingType.PrismDefense => "SPLITS INTO THREE BEAMS",
            BuildingType.PulseDefense => "HITS EVERYTHING NEARBY",
            BuildingType.VolleyDefense => "FIRES AT MANY TARGETS",
            _ => string.Empty
        };

        // Specialized defenses are paired on geometrically opposite path nodes.
        public static int BuildingUnlockPath(BuildingType type) => type switch
        {
            BuildingType.ArcDefense => 4,
            BuildingType.FrostDefense => 5,
            BuildingType.PierceDefense => 6,
            BuildingType.PrismDefense => 7,
            BuildingType.BlastDefense => 8,
            BuildingType.PulseDefense => 9,
            BuildingType.SupportDefense => 10,
            BuildingType.VolleyDefense => 11,
            _ => -1
        };

        public static BuildingType? DefenseUnlockedByPath(int pathIndex) => pathIndex switch
        {
            4 => BuildingType.ArcDefense,
            5 => BuildingType.FrostDefense,
            6 => BuildingType.PierceDefense,
            7 => BuildingType.PrismDefense,
            8 => BuildingType.BlastDefense,
            9 => BuildingType.PulseDefense,
            10 => BuildingType.SupportDefense,
            11 => BuildingType.VolleyDefense,
            _ => null
        };

        public static int DefenseSides(BuildingType type) => type switch
        {
            BuildingType.TriangleDefense => 3,
            BuildingType.ArcDefense => 6,
            BuildingType.PierceDefense => 4,
            BuildingType.SupportDefense => 8,
            BuildingType.BlastDefense => 5,
            BuildingType.FrostDefense => 7,
            BuildingType.PrismDefense => 4,
            BuildingType.PulseDefense => 10,
            BuildingType.VolleyDefense => 9,
            _ => 4
        };

        public static BuildingType? DefenseForPath(int pathIndex)
        {
            if (pathIndex < 4 || pathIndex >= MapLayout.PathParents.Length) return null;
            var current = pathIndex;
            while (current >= 12) current = MapLayout.PathParents[current];
            return DefenseUnlockedByPath(current);
        }

        public static PathRewardKind PathReward(int pathIndex)
        {
            if (DefenseUnlockedByPath(pathIndex).HasValue) return PathRewardKind.DefenseUnlock;
            if (pathIndex >= 12)
            {
                var depth = PathDepth(pathIndex);
                if (depth == 2) return pathIndex % 2 == 0 ? PathRewardKind.Damage : PathRewardKind.Range;
                var supportPath = DefenseForPath(pathIndex) == BuildingType.SupportDefense;
                if (depth == 3) return pathIndex % 2 == 0
                    ? supportPath ? PathRewardKind.Damage : PathRewardKind.Speed
                    : PathRewardKind.Efficiency;
                return pathIndex % 2 == 0
                    ? PathRewardKind.Damage
                    : supportPath ? PathRewardKind.Range : PathRewardKind.Speed;
            }

            return pathIndex switch
            {
                0 => PathRewardKind.Start,
                1 => PathRewardKind.Damage,
                2 => PathRewardKind.Ore,
                3 => PathRewardKind.Range,
                _ => PathRewardKind.Efficiency
            };
        }

        private static int PathDepth(int pathIndex)
        {
            var depth = 0;
            while (MapLayout.PathParents[pathIndex] >= 0)
            {
                pathIndex = MapLayout.PathParents[pathIndex];
                depth++;
            }
            return depth;
        }

        private static string UpgradeName(PathRewardKind reward) => reward switch
        {
            PathRewardKind.Damage => "Power",
            PathRewardKind.Range => "Reach",
            PathRewardKind.Speed => "Tempo",
            _ => "Efficiency"
        };

        private static string UpgradeBonus(BuildingType defense, PathRewardKind reward)
        {
            var name = Name(defense);
            return reward switch
            {
                PathRewardKind.Damage => $"+15% {name} power",
                PathRewardKind.Range => $"+10% {name} range",
                PathRewardKind.Speed => $"+10% {name} attack speed",
                _ => $"5% cheaper {name} and +5% power"
            };
        }

        public static int BuildCost(BuildingType type, int existingCount) => ScaledInt(
            Cost(type), BuildingCostGrowth, Mathf.Max(0, existingCount));

        public static int UpgradeCost(BuildingType type, int level)
        {
            var baseCost = type switch
            {
                BuildingType.TriangleDefense => 45,
                BuildingType.OreCollector => 120,
                BuildingType.ArcDefense => 85,
                BuildingType.PierceDefense => 105,
                BuildingType.SupportDefense => 95,
                BuildingType.BlastDefense => 120,
                BuildingType.FrostDefense => 90,
                BuildingType.PrismDefense => 110,
                BuildingType.PulseDefense => 125,
                BuildingType.VolleyDefense => 105,
                _ => 45
            };
            return ScaledInt(baseCost, IsCollector(type) ? CollectorUpgradeGrowth : DefenseUpgradeGrowth,
                Mathf.Max(0, level - 1));
        }

        public static float DefenseDamage(int level) => Mathf.Min(1e30f,
            20f * Mathf.Pow(1.17f, Mathf.Max(0, level - 1)));

        public static float DefenseRange(int level) => 3.8f + Mathf.Min(3.2f, Mathf.Max(0, level - 1) * .18f);

        public static float DefenseFireInterval(int level) => Mathf.Max(.32f,
            .82f * Mathf.Pow(.965f, Mathf.Max(0, level - 1)));

        public static float BuildingDamage(BuildingType type, int level)
        {
            var baseDamage = type switch
            {
                BuildingType.TriangleDefense => 20f,
                BuildingType.ArcDefense => 14f,
                BuildingType.PierceDefense => 62f,
                BuildingType.BlastDefense => 34f,
                BuildingType.FrostDefense => 18f,
                BuildingType.PrismDefense => 27f,
                BuildingType.PulseDefense => 16f,
                BuildingType.VolleyDefense => 23f,
                _ => 0f
            };
            var growth = type == BuildingType.PierceDefense ? 1.19f : 1.17f;
            return Mathf.Min(1e30f, baseDamage * Mathf.Pow(growth, Mathf.Max(0, level - 1)));
        }

        public static float BuildingRange(BuildingType type, int level)
        {
            var baseRange = type switch
            {
                BuildingType.TriangleDefense => 3.8f,
                BuildingType.ArcDefense => 4.6f,
                BuildingType.PierceDefense => 6.8f,
                BuildingType.SupportDefense => 4.8f,
                BuildingType.BlastDefense => 4.3f,
                BuildingType.FrostDefense => 4.9f,
                BuildingType.PrismDefense => 5.2f,
                BuildingType.PulseDefense => 3.7f,
                BuildingType.VolleyDefense => 5.5f,
                _ => 0f
            };
            return baseRange + Mathf.Min(3.2f, Mathf.Max(0, level - 1) * .16f);
        }

        public static float BuildingFireInterval(BuildingType type, int level)
        {
            var baseInterval = type switch
            {
                BuildingType.TriangleDefense => .82f,
                BuildingType.ArcDefense => 1.08f,
                BuildingType.PierceDefense => 1.85f,
                BuildingType.BlastDefense => 1.48f,
                BuildingType.FrostDefense => 1.12f,
                BuildingType.PrismDefense => 1.32f,
                BuildingType.PulseDefense => 1.55f,
                BuildingType.VolleyDefense => 1.38f,
                _ => 999f
            };
            return Mathf.Max(.24f, baseInterval * Mathf.Pow(.965f, Mathf.Max(0, level - 1)));
        }

        public static float SupportBoost(int level) => .18f + Mathf.Min(.22f, Mathf.Max(0, level - 1) * .015f);

        public static Color BuildingColor(BuildingType type) => type switch
        {
            BuildingType.TriangleDefense => Defense,
            BuildingType.OreCollector => Collector,
            BuildingType.ArcDefense => Ore,
            BuildingType.PierceDefense => new Color(1f, .48f, .24f),
            BuildingType.SupportDefense => GreenBranch,
            BuildingType.BlastDefense => PurpleBranch,
            BuildingType.FrostDefense => new Color(.42f, .9f, 1f),
            BuildingType.PrismDefense => new Color(1f, .45f, .78f),
            BuildingType.PulseDefense => new Color(.72f, 1f, .34f),
            BuildingType.VolleyDefense => new Color(1f, .68f, .22f),
            _ => Text
        };

        public static int CollectorOreAmount(int level) => ScaledInt(4, 1.14f, Mathf.Max(0, level - 1));

        public static float CollectorOreInterval(int level) => Mathf.Max(3f,
            6f * Mathf.Pow(.98f, Mathf.Max(0, level - 1)));

        public static int EnemyCount(int wave) => Mathf.Min(220, 8 + Mathf.CeilToInt(Mathf.Max(1, wave) * 1.25f));

        public static bool IsBossWave(int wave) => wave > 0 && wave % BossWaveInterval == 0;

        public static bool IsMajorBossWave(int wave) => wave > 0 && wave % MajorBossWaveInterval == 0;

        public static BossKind BossForWave(int wave) => (BossKind)(Mathf.Abs(wave / BossWaveInterval - 1) % 5);

        public static string BossName(BossKind kind) => kind switch
        {
            BossKind.Bulwark => "THE BULWARK",
            BossKind.BroodCore => "THE BROOD CORE",
            BossKind.Leech => "THE LEECH",
            BossKind.Splitter => "THE SPLITTER",
            _ => "THE ARCHITECT"
        };

        public static float BossHealth(int wave) => Mathf.Min(1e30f,
            EnemyHealth(wave) * (IsMajorBossWave(wave) ? 90f : 38f));

        public static int BossReward(int wave) => Mathf.Min(2000000000,
            EnemyReward(wave) * (IsMajorBossWave(wave) ? 70 : 28));

        public static int BossShardReward(int wave) => IsMajorBossWave(wave) ? 5 : 1;

        public static int PrestigeShardReward(int clearedWave)
        {
            if (clearedWave < PrestigeUnlockWave) return 0;
            var bosses = clearedWave / BossWaveInterval;
            return Mathf.Max(1, Mathf.FloorToInt(Mathf.Pow(bosses - 1, 1.35f)));
        }

        public static int MasteryRequiredPathNodes(int rank) => rank switch
        {
            1 => 3,
            2 => 7,
            _ => 11
        };

        public static int MasteryRequiredBuildingLevel(int rank) => rank switch
        {
            1 => 5,
            2 => 15,
            _ => 30
        };

        public static int MasteryUpgradeCost(BuildingType type, int rank) =>
            ScaledInt(Cost(type) * 4f, 2.4f, Mathf.Clamp(rank - 1, 0, 2));

        public static string EvolutionName(BuildingType type, int evolution) => (type, evolution) switch
        {
            (BuildingType.TriangleDefense, 1) => "RAZOR ARRAY",
            (BuildingType.TriangleDefense, 2) => "GATLING FRAME",
            (BuildingType.OreCollector, 1) => "DEEP EXTRACTOR",
            (BuildingType.OreCollector, 2) => "QUICK REFINERY",
            (BuildingType.ArcDefense, 1) => "STORM CORE",
            (BuildingType.ArcDefense, 2) => "CHAIN REACTOR",
            (BuildingType.PierceDefense, 1) => "SIEGE RAIL",
            (BuildingType.PierceDefense, 2) => "PHASE LANCE",
            (BuildingType.SupportDefense, 1) => "WAR BEACON",
            (BuildingType.SupportDefense, 2) => "RELAY BEACON",
            (BuildingType.BlastDefense, 1) => "NOVA CANNON",
            (BuildingType.BlastDefense, 2) => "RAPID MORTAR",
            (BuildingType.FrostDefense, 1) => "ABSOLUTE ZERO",
            (BuildingType.FrostDefense, 2) => "WINTER FIELD",
            (BuildingType.PrismDefense, 1) => "SOLAR PRISM",
            (BuildingType.PrismDefense, 2) => "SPECTRAL ARRAY",
            (BuildingType.PulseDefense, 1) => "QUAKE CORE",
            (BuildingType.PulseDefense, 2) => "RESONANCE FIELD",
            (BuildingType.VolleyDefense, 1) => "HEAVY SALVO",
            (BuildingType.VolleyDefense, 2) => "SWARM BATTERY",
            _ => evolution == 1 ? "POWER EVOLUTION" : "TEMPO EVOLUTION"
        };

        public static string EvolutionBonus(BuildingType type, int evolution)
        {
            if (type == BuildingType.OreCollector)
                return evolution == 1 ? "+35% ore per collection" : "25% faster collection";
            if (type == BuildingType.SupportDefense)
                return evolution == 1 ? "+25% support strength" : "+20% support range";
            return evolution == 1 ? "+30% damage" : "+15% range and 20% attack speed";
        }

        public static float EnemyHealth(int wave) => Mathf.Min(1e30f,
            32f * Mathf.Pow(1.065f, Mathf.Max(0, wave - 1)));

        public static float EnemySpeed(int wave) => 1.3f + Mathf.Min(1.7f, Mathf.Max(1, wave) * .012f);

        public static int EnemyReward(int wave) => ScaledInt(5, 1.04f, Mathf.Max(0, wave - 1));

        public static int EnemyCoreDamage(int wave) => Mathf.Min(8, 1 + Mathf.Max(0, wave - 1) / 40);

        public static float EnemySpawnInterval(int wave) => Mathf.Max(.18f, .9f - Mathf.Max(1, wave) * .008f);

        public static int FrontierEnemyCount(int wave, int frontierCount, float averageDepth)
        {
            var extraFrontPressure = 1f + Mathf.Max(0, frontierCount - 1) * .35f;
            var depthPressure = 1f + Mathf.Max(0, averageDepth) * .15f;
            return Mathf.Clamp(Mathf.CeilToInt(EnemyCount(wave) * extraFrontPressure * depthPressure), 1, 600);
        }

        public static float RouteSpeedMultiplier(float routeLength, float startingRouteLength) =>
            Mathf.Sqrt(Mathf.Max(1f, routeLength / Mathf.Max(.01f, startingRouteLength)));

        private static int ScaledInt(float baseValue, float growth, int exponent)
        {
            var value = baseValue * Mathf.Pow(growth, exponent);
            return Mathf.RoundToInt(Mathf.Min(2000000000f, value));
        }

        private static readonly string[] BasePathNames =
        {
            "Foundation", "Sharpened Edges", "Rich Veins", "Long Reach", "Arc Defense Blueprint",
            "Bounty Hunt", "Pierce Defense Blueprint", "Efficient Armory", "Blast Defense Blueprint", "Impact Shield",
            "Support Beacon Blueprint", "Second Wind", "Heavy Tips", "Eagle Eye", "Overclock",
            "Golden Targets", "Crystalline Ore", "Automated Mining", "Fortress Layer", "Reserve Plating",
            "Mass Production", "Prefabrication", "Siege Geometry", "Zero-Lag Triggers", "Prismatic Veins",
            "Golden Current", "Citadel Core", "Phoenix Core"
        };

        private static readonly string[] BasePathBonuses =
        {
            "Starting enemy route", "+20% defense damage", "+25% ore per collection", "+20% defense range",
            "UNLOCKS ARC DEFENSE", "+25% gold from enemies", "UNLOCKS PIERCE DEFENSE",
            "20% cheaper defense costs", "UNLOCKS BLAST DEFENSE", "Enemies deal 1 less core damage",
            "UNLOCKS SUPPORT BEACON", "Survive one lethal core hit per wave", "+15% defense damage",
            "+15% defense range", "+15% defense attack speed", "+20% gold from enemies",
            "+20% ore per collection", "15% faster ore collectors", "+5 maximum core health",
            "+5 maximum core health", "15% cheaper defense costs", "15% cheaper ore collectors",
            "+20% defense damage", "+15% defense attack speed", "+25% ore per collection",
            "+25% gold from enemies", "+10 maximum core health", "+1 Second Wind charge"
        };

        private static readonly string[] EdgeSkillNames =
        {
            "Edge Power", "Edge Vision", "Edge Speed", "Edge Bounty",
            "Edge Mining", "Edge Armor", "Edge Efficiency", "Edge Automation"
        };

        private static readonly string[] EdgeSkillBonuses =
        {
            "+5% defense damage", "+5% defense range", "+5% defense attack speed",
            "+10% gold from enemies", "+10% ore per collection", "+2 maximum core health",
            "3% cheaper defense costs", "5% faster ore collectors"
        };

        public static readonly string[] PathNames = CreatePathNames();
        public static readonly string[] PathBonuses = CreatePathBonuses();

        private static string[] CreatePathNames()
        {
            var names = new List<string>(BasePathNames);
            for (var index = 0; index < 32; index++)
                names.Add($"Outer Leaf {index + 1} - {EdgeSkillNames[index % EdgeSkillNames.Length]}");
            for (var index = 0; index < 64; index++)
                names.Add($"Final Leaf {index + 1} - {EdgeSkillNames[index % EdgeSkillNames.Length]}");
            for (var index = 4; index < names.Count; index++)
            {
                var defense = DefenseForPath(index);
                if (!defense.HasValue) continue;
                names[index] = DefenseUnlockedByPath(index).HasValue
                    ? $"{Name(defense.Value)} Blueprint"
                    : $"{Name(defense.Value)} {UpgradeName(PathReward(index))}";
            }
            return names.ToArray();
        }

        private static string[] CreatePathBonuses()
        {
            var bonuses = new List<string>(BasePathBonuses);
            for (var index = 0; index < 96; index++)
                bonuses.Add(EdgeSkillBonuses[index % EdgeSkillBonuses.Length]);
            for (var index = 4; index < bonuses.Count; index++)
            {
                var defense = DefenseForPath(index);
                if (!defense.HasValue) continue;
                bonuses[index] = DefenseUnlockedByPath(index).HasValue
                    ? $"UNLOCKS {Name(defense.Value).ToUpperInvariant()}"
                    : UpgradeBonus(defense.Value, PathReward(index));
            }
            return bonuses.ToArray();
        }

        // Near-black void and desaturated metal keep the playfield quiet; color is reserved for state.
        public static readonly Color Ground = new(.012f, .016f, .018f);
        public static readonly Color PathLocked = new(.16f, .16f, .15f);
        public static readonly Color PathOpen = new(.55f, .55f, .49f);
        public static readonly Color Defense = new(.32f, .72f, 1f);
        public static readonly Color Collector = new(.72f, .46f, .88f);
        public static readonly Color Enemy = new(1f, .29f, .22f);
        public static readonly Color Gold = new(1f, .82f, .31f);
        public static readonly Color Ore = new(.35f, .86f, .91f);
        public static readonly Color Text = new(.78f, .78f, .72f);
        public static readonly Color Panel = new(.025f, .029f, .030f, .96f);
        public static readonly Color BlueBranch = new(.33f, .69f, 1f);
        public static readonly Color PurpleBranch = new(.73f, .48f, .88f);
        public static readonly Color GreenBranch = new(.58f, .82f, .39f);
    }
}
