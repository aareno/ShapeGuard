using System.Collections.Generic;
using UnityEngine;

namespace ShapeGuard
{
    public static class MapLayout
    {
        private const int BaseNodeCount = 28;
        private const float MapRadius = 188f;
        public static readonly Vector3 CorePosition = Vector3.zero;
        public static readonly Rect Bounds = Rect.MinMaxRect(-200f, -200f, 200f, 200f);
        public static readonly float[] TierFractions = { .12f, .27f, .49f, .73f, .97f };

        // Binary angle splits distribute the final leaves evenly around the whole map.
        private static readonly float[] TierSplitAngles = { 0f, 22.5f, 11.25f, 5.625f, 2.8125f };

        private static readonly int[] BasePathParents =
        {
            -1, -1, -1, -1,
            0, 0, 1, 1, 2, 2, 3, 3,
            4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11
        };

        public static readonly int[] PathParents = CreatePathParents();
        private static readonly Vector3[] PathNodes = CreatePathNodes();

        private static int[] CreatePathParents()
        {
            var parents = new List<int>(BasePathParents);
            for (var parent = 12; parent < BaseNodeCount; parent++)
            {
                parents.Add(parent);
                parents.Add(parent);
            }
            var endBranchCount = parents.Count;
            for (var parent = BaseNodeCount; parent < endBranchCount; parent++)
            {
                parents.Add(parent);
                parents.Add(parent);
            }
            return parents.ToArray();
        }

        private static Vector3[] CreatePathNodes()
        {
            var nodes = new Vector3[PathParents.Length];
            var angles = new float[PathParents.Length];
            var depths = new int[PathParents.Length];
            var rootAngles = new[] { 90f, 0f, -90f, 180f };
            for (var index = 0; index < rootAngles.Length; index++)
            {
                angles[index] = rootAngles[index];
                nodes[index] = PointOnTier(TierFractions[0], angles[index] * Mathf.Deg2Rad);
            }

            for (var index = rootAngles.Length; index < nodes.Length; index++)
            {
                var parent = PathParents[index];
                var depth = depths[parent] + 1;
                depths[index] = depth;
                angles[index] = angles[parent] + TierSplitAngles[depth] * (index % 2 == 0 ? 1f : -1f);
                nodes[index] = PointOnTier(TierFractions[depth], angles[index] * Mathf.Deg2Rad);
            }
            return nodes;
        }

        public static Vector3 PointOnTier(float fraction, float angle)
        {
            var radius = MapRadius * fraction;
            return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        }

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
