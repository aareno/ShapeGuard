using System.Collections.Generic;
using UnityEngine;

namespace ShapeGuard
{
    public static class VisualFactory
    {
        private static Sprite circle;
        private static Sprite square;
        private static Sprite ring;
        private static readonly Dictionary<int, Sprite> polygons = new();
        private static readonly Dictionary<int, Sprite> polygonOutlines = new();

        public static Sprite Circle => circle != null ? circle : circle = MakeCircle();
        public static Sprite Square => square != null ? square : square = MakeSquare();
        public static Sprite Ring => ring != null ? ring : ring = MakeRing();

        public static Sprite PolygonOutline(int sides)
        {
            sides = Mathf.Max(3, sides);
            if (polygonOutlines.TryGetValue(sides, out var cached)) return cached;
            const int size = 128;
            var texture = NewTexture(size, $"Polygon Outline {sides}");
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var px = (x + .5f) / size * 2f - 1f;
                var py = (y + .5f) / size * 2f - 1f;
                var outer = InsidePolygon(px, py, sides, .92f);
                var inner = InsidePolygon(px, py, sides, .69f);
                pixels[y * size + x] = new Color(1, 1, 1, outer && !inner ? 1f : 0f);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return polygonOutlines[sides] = MakeSprite(texture);
        }

        public static Sprite Polygon(int sides)
        {
            sides = Mathf.Max(3, sides);
            if (polygons.TryGetValue(sides, out var cached)) return cached;
            const int size = 128;
            const int samples = 4;
            var texture = NewTexture(size, $"Polygon {sides}");
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var covered = 0;
                for (var sy = 0; sy < samples; sy++)
                for (var sx = 0; sx < samples; sx++)
                {
                    var px = (x + (sx + .5f) / samples) / size * 2f - 1f;
                    var py = (y + (sy + .5f) / samples) / size * 2f - 1f;
                    if (InsidePolygon(px, py, sides, .9f)) covered++;
                }
                pixels[y * size + x] = new Color(1, 1, 1, covered / 16f);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return polygons[sides] = MakeSprite(texture);
        }

        public static SpriteRenderer Part(Transform parent, string name, Sprite sprite, Color color,
            Vector3 localPosition, Vector3 scale, int order)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = scale;
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        public static SpriteRenderer GlowPart(Transform parent, string name, Sprite sprite, Color color,
            Vector3 localPosition, Vector3 scale, int order, float glowScale = 1.35f)
        {
            var glow = new Color(color.r, color.g, color.b, color.a * .16f);
            Part(parent, $"{name} Glow", sprite, glow, localPosition, scale * glowScale, order - 1);
            return Part(parent, name, sprite, color, localPosition, scale, order);
        }

        private static bool InsidePolygon(float x, float y, int sides, float radius)
        {
            var sector = Mathf.PI * 2f / sides;
            var half = sector * .5f;
            var angle = Mathf.Repeat(Mathf.Atan2(y, x) + Mathf.PI * .5f + half, sector) - half;
            var edge = Mathf.Cos(Mathf.PI / sides) * radius / Mathf.Cos(angle);
            return x * x + y * y <= edge * edge;
        }

        private static Sprite MakeCircle()
        {
            const int size = 128;
            var texture = NewTexture(size, "Circle");
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = (x + .5f) / size * 2f - 1f;
                var dy = (y + .5f) / size * 2f - 1f;
                var alpha = Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) * size * .5f);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return MakeSprite(texture);
        }

        private static Sprite MakeSquare()
        {
            var texture = NewTexture(2, "Square");
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            return MakeSprite(texture);
        }

        private static Sprite MakeRing()
        {
            const int size = 256;
            var texture = NewTexture(size, "Range Ring");
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = (x + .5f) / size * 2f - 1f;
                var dy = (y + .5f) / size * 2f - 1f;
                var distance = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - .95f);
                var blend = Mathf.Clamp01((distance - .012f) / (.008f));
                pixels[y * size + x] = new Color(1, 1, 1, 1f - Mathf.SmoothStep(0, 1, blend));
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return MakeSprite(texture);
        }

        private static Texture2D NewTexture(int size, string name) => new(size, size, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        private static Sprite MakeSprite(Texture2D texture) => Sprite.Create(texture,
            new Rect(0, 0, texture.width, texture.height), Vector2.one * .5f, texture.width);
    }
}
