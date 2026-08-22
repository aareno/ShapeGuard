using System.Collections.Generic;
using UnityEngine;

namespace ShapeGuard
{
    public static class MapLayout
    {
        public static readonly Vector3 CorePosition = Vector3.zero;
        public static readonly Rect Bounds = Rect.MinMaxRect(-135f, -135f, 135f, 135f);

        private static readonly int[] BasePathParents =
        {
            -1, -1, -1, -1,
            0, 0, 1, 1, 2, 2, 3, 3,
            4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11
        };

        private static readonly Vector3[] BasePathNodes =
        {
            // Four primary branches: north, east, south, and west.
            new(0, 16, 0), new(16, 0, 0), new(0, -16, 0), new(-16, 0, 0),

            // Each primary branch splits into two outer branches.
            new(-14, 34, 0), new(14, 34, 0), new(34, 14, 0), new(34, -14, 0),
            new(14, -34, 0), new(-14, -34, 0), new(-34, -14, 0), new(-34, 14, 0),

            // Each outer branch currently ends in two leaves.
            new(-40, 60, 0), new(-14, 71, 0), new(14, 71, 0), new(40, 60, 0),
            new(60, 40, 0), new(71, 14, 0), new(71, -14, 0), new(60, -40, 0),
            new(40, -60, 0), new(14, -71, 0), new(-14, -71, 0), new(-40, -60, 0),
            new(-60, -40, 0), new(-71, -14, 0), new(-71, 14, 0), new(-60, 40, 0)
        };

        public static readonly int[] PathParents = CreatePathParents();
        private static readonly Vector3[] PathNodes = CreatePathNodes();

        private static int[] CreatePathParents()
        {
            var parents = new List<int>(BasePathParents);
            for (var parent = 12; parent < BasePathNodes.Length; parent++)
            {
                parents.Add(parent);
                parents.Add(parent);
            }
            return parents.ToArray();
        }

        private static Vector3[] CreatePathNodes()
        {
            var nodes = new List<Vector3>(BasePathNodes);
            const float leafRadius = 120f;
            const float splitAngle = 2.8f * Mathf.Deg2Rad;
            for (var parent = 12; parent < BasePathNodes.Length; parent++)
            {
                var parentNode = BasePathNodes[parent];
                var angle = Mathf.Atan2(parentNode.y, parentNode.x);
                nodes.Add(Polar(leafRadius, angle - splitAngle));
                nodes.Add(Polar(leafRadius, angle + splitAngle));
            }
            return nodes.ToArray();
        }

        private static Vector3 Polar(float radius, float angle) =>
            new(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);

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
