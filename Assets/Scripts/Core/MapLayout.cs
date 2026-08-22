using System.Collections.Generic;
using UnityEngine;

namespace ShapeGuard
{
    public static class MapLayout
    {
        public static readonly Vector3 CorePosition = Vector3.zero;
        public static readonly Rect Bounds = Rect.MinMaxRect(-85f, -85f, 85f, 85f);

        public static readonly int[] PathParents =
        {
            -1, -1, -1, -1,
            0, 0, 1, 1, 2, 2, 3, 3,
            4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11
        };

        private static readonly Vector3[] PathNodes =
        {
            // Four primary branches: north, east, south, and west.
            new(0, 16, 0),
            new(16, 0, 0),
            new(0, -16, 0),
            new(-16, 0, 0),

            // Each primary branch splits into two widely spaced outer branches.
            new(-14, 34, 0),
            new(14, 34, 0),
            new(34, 14, 0),
            new(34, -14, 0),
            new(14, -34, 0),
            new(-14, -34, 0),
            new(-34, -14, 0),
            new(-34, 14, 0),

            // Every outer branch splits once more into two final endpoints.
            new(-40, 60, 0),
            new(-14, 71, 0),
            new(14, 71, 0),
            new(40, 60, 0),
            new(60, 40, 0),
            new(71, 14, 0),
            new(71, -14, 0),
            new(60, -40, 0),
            new(40, -60, 0),
            new(14, -71, 0),
            new(-14, -71, 0),
            new(-40, -60, 0),
            new(-60, -40, 0),
            new(-71, -14, 0),
            new(-71, 14, 0),
            new(-60, 40, 0)
        };

        public static IReadOnlyList<Vector3[]> CreatePaths()
        {
            var routes = new List<Vector3[]>();
            for (var index = 0; index < PathNodes.Length; index++)
            {
                var route = new List<Vector3> { PathNodes[index] };
                var parent = PathParents[index];
                while (parent >= 0)
                {
                    route.Add(PathNodes[parent]);
                    parent = PathParents[parent];
                }
                route.Add(CorePosition);
                routes.Add(route.ToArray());
            }
            return routes;
        }
    }
}
