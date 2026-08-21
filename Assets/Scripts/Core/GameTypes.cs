using UnityEngine;

namespace MeadowGuard
{
    public enum BuildingKind { Cannon, GoldCollector, OreCollector }

    public static class Balance
    {
        public static int PlaceCost(BuildingKind kind) => kind switch
        {
            BuildingKind.Cannon => 80,
            BuildingKind.GoldCollector => 120,
            _ => 100
        };

        public static string Name(BuildingKind kind) => kind switch
        {
            BuildingKind.Cannon => "Cannon",
            BuildingKind.GoldCollector => "Gold Collector",
            _ => "Ore Drill"
        };

        public static Color Color(BuildingKind kind) => kind switch
        {
            BuildingKind.Cannon => new Color(0.23f, 0.31f, 0.39f),
            BuildingKind.GoldCollector => new Color(1f, 0.70f, 0.12f),
            _ => new Color(0.25f, 0.72f, 0.86f)
        };
    }
}
