using System.Collections.Generic;
using UnityEngine;

namespace ShapeGuard
{
    public enum BuildingType { TriangleDefense, OreCollector }

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
        public const float CameraMaximumZoom = 138f;

        public static int Cost(BuildingType type) => type == BuildingType.TriangleDefense ? DefenseCost : CollectorCost;
        public static string Currency(BuildingType type) => type == BuildingType.TriangleDefense ? "ore" : "gold";
        public static string Name(BuildingType type) => type == BuildingType.TriangleDefense ? "Triangle Defense" : "Ore Collector";

        private static readonly string[] BasePathNames =
        {
            "Foundation", "Sharpened Edges", "Rich Veins", "Long Reach", "Quick Triggers",
            "Bounty Hunt", "Reinforced Core", "Efficient Armory", "Deep Mining", "Impact Shield",
            "Collector Blueprints", "Second Wind", "Heavy Tips", "Eagle Eye", "Overclock",
            "Golden Targets", "Crystalline Ore", "Automated Mining", "Fortress Layer", "Reserve Plating",
            "Mass Production", "Prefabrication", "Siege Geometry", "Zero-Lag Triggers", "Prismatic Veins",
            "Golden Current", "Citadel Core", "Phoenix Core"
        };

        private static readonly string[] BasePathBonuses =
        {
            "Starting enemy route", "+20% defense damage", "+25% ore per collection", "+20% defense range",
            "+15% defense attack speed", "+25% gold from enemies", "+5 maximum core health",
            "20% cheaper defense costs", "20% faster ore collectors", "Enemies deal 1 less core damage",
            "20% cheaper ore collectors", "Survive one lethal core hit per wave", "+15% defense damage",
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
            return names.ToArray();
        }

        private static string[] CreatePathBonuses()
        {
            var bonuses = new List<string>(BasePathBonuses);
            for (var index = 0; index < 32; index++)
                bonuses.Add(EdgeSkillBonuses[index % EdgeSkillBonuses.Length]);
            return bonuses.ToArray();
        }

        public static readonly Color Ground = new(.075f, .105f, .095f);
        public static readonly Color PathLocked = new(.16f, .19f, .18f);
        public static readonly Color PathOpen = new(.30f, .40f, .34f);
        public static readonly Color Defense = new(.25f, .78f, .92f);
        public static readonly Color Collector = new(.63f, .47f, .94f);
        public static readonly Color Enemy = new(.94f, .25f, .27f);
        public static readonly Color Gold = new(1f, .78f, .25f);
        public static readonly Color Ore = new(.30f, .85f, .92f);
        public static readonly Color Text = new(.93f, .95f, .92f);
        public static readonly Color Panel = new(.055f, .075f, .068f, .95f);
    }
}
