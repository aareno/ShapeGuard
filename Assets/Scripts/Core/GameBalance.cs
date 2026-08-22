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

        public static int Cost(BuildingType type) => type == BuildingType.TriangleDefense ? DefenseCost : CollectorCost;
        public static string Currency(BuildingType type) => type == BuildingType.TriangleDefense ? "ore" : "gold";
        public static string Name(BuildingType type) => type == BuildingType.TriangleDefense ? "Triangle Defense" : "Ore Collector";

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
