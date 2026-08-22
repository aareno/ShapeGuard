using System.Collections.Generic;
using UnityEngine;

namespace ShapeGuard
{
    public static class MapLayout
    {
        public static readonly Vector3 CorePosition = Vector3.zero;
        public static readonly Rect Bounds = Rect.MinMaxRect(-27f, -18f, 27f, 18f);

        public static IReadOnlyList<Vector3[]> CreatePaths() => new List<Vector3[]>
        {
            // West entrances merge into one west trunk.
            Route(new(-25, 10, 0), new(-19, 10, 0), new(-14, 5, 0), new(-10, 0, 0), new(-5, 0, 0)),
            Route(new(-25, -10, 0), new(-19, -10, 0), new(-14, -5, 0), new(-10, 0, 0), new(-5, 0, 0)),

            // North entrances merge into one north trunk.
            Route(new(-16, 17, 0), new(-12, 13, 0), new(-6, 11, 0), new(0, 8, 0), new(0, 4, 0)),
            Route(new(0, 17, 0), new(0, 12, 0), new(0, 8, 0), new(0, 4, 0)),
            Route(new(16, 17, 0), new(12, 13, 0), new(6, 11, 0), new(0, 8, 0), new(0, 4, 0)),

            // East entrances merge into one east trunk.
            Route(new(25, 10, 0), new(19, 10, 0), new(14, 5, 0), new(10, 0, 0), new(5, 0, 0)),
            Route(new(25, -10, 0), new(19, -10, 0), new(14, -5, 0), new(10, 0, 0), new(5, 0, 0)),

            // South entrances merge into one south trunk.
            Route(new(16, -17, 0), new(12, -13, 0), new(6, -11, 0), new(0, -8, 0), new(0, -4, 0)),
            Route(new(0, -17, 0), new(0, -12, 0), new(0, -8, 0), new(0, -4, 0)),
            Route(new(-16, -17, 0), new(-12, -13, 0), new(-6, -11, 0), new(0, -8, 0), new(0, -4, 0))
        };

        private static Vector3[] Route(params Vector3[] points)
        {
            var route = new Vector3[points.Length + 1];
            for (var index = 0; index < points.Length; index++) route[index] = points[index];
            route[^1] = CorePosition;
            return route;
        }
    }
}
