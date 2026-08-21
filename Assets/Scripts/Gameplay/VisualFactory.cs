using UnityEngine;

namespace MeadowGuard
{
    public static class VisualFactory
    {
        private static Sprite square;
        private static Sprite circle;

        public static Sprite Square => square != null ? square : square = MakeSprite(false);
        public static Sprite Circle => circle != null ? circle : circle = MakeSprite(true);

        private static Sprite MakeSprite(bool round)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = round ? "Runtime Circle" : "Runtime Square",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = (x + .5f) / size * 2f - 1f;
                var dy = (y + .5f) / size * 2f - 1f;
                var alpha = !round ? 1f : Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) * 14f);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, size);
        }

        public static SpriteRenderer Part(Transform parent, string name, Sprite sprite, Color color,
            Vector3 localPosition, Vector3 scale, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
