using UnityEngine;

namespace ShapeGuard
{
    public static class CombatFx
    {
        public static void Tracer(Vector3 start, Vector3 end, Color color, float width, float duration,
            bool jagged = false)
        {
            var effect = new GameObject(jagged ? "Arc Lightning" : "Defense Shot");
            var glow = CreateLine(effect.transform, "Glow", new Color(color.r, color.g, color.b, .22f),
                width * 3.8f, 29);
            var core = CreateLine(effect.transform, "Core", Color.Lerp(color, Color.white, .58f), width, 30);
            SetPath(glow, start, end, jagged);
            SetPath(core, start, end, jagged);
            effect.AddComponent<TracerEffect>().Initialize(glow, core, color, width, duration);
        }

        public static void Impact(Vector3 position, Color color, float size = 1f, bool heavy = false)
        {
            var effect = new GameObject(heavy ? "Heavy Impact" : "Impact");
            effect.transform.position = position;
            var ring = VisualFactory.Part(effect.transform, "Shock Ring", VisualFactory.Ring,
                new Color(color.r, color.g, color.b, .9f), Vector3.zero, Vector3.one * .18f, 33);
            var flash = VisualFactory.Part(effect.transform, "Impact Flash", VisualFactory.Polygon(4),
                Color.Lerp(color, Color.white, .72f), Vector3.zero, Vector3.one * (heavy ? .72f : .42f), 34);
            var sparkCount = heavy ? 9 : 5;
            var sparks = new SpriteRenderer[sparkCount];
            var velocities = new Vector3[sparkCount];
            for (var index = 0; index < sparkCount; index++)
            {
                var angle = Mathf.PI * 2f * index / sparkCount + (heavy ? .18f : .42f);
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                sparks[index] = VisualFactory.Part(effect.transform, $"Spark {index + 1}", VisualFactory.Square,
                    color, direction * .12f, new Vector3(.06f, heavy ? .34f : .24f, 1), 35);
                sparks[index].transform.up = direction;
                velocities[index] = direction * (heavy ? 4.8f : 3.2f) * size;
            }
            effect.AddComponent<ImpactEffect>().Initialize(ring, flash, sparks, velocities, color, size, heavy);
        }

        public static void EnemyBurst(Vector3 position, Color color)
        {
            Impact(position, color, 1.45f, true);
            Impact(position, GameBalance.Gold, .72f, false);
        }

        private static LineRenderer CreateLine(Transform parent, string name, Color color, float width, int order)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            var line = child.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = line.endColor = color;
            line.startWidth = width;
            line.endWidth = width * .42f;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.sortingOrder = order;
            return line;
        }

        private static void SetPath(LineRenderer line, Vector3 start, Vector3 end, bool jagged)
        {
            if (!jagged)
            {
                line.positionCount = 2;
                line.SetPosition(0, start);
                line.SetPosition(1, end);
                return;
            }

            const int points = 7;
            line.positionCount = points;
            var delta = end - start;
            var perpendicular = new Vector3(-delta.y, delta.x, 0).normalized;
            for (var index = 0; index < points; index++)
            {
                var progress = index / (points - 1f);
                var offset = index == 0 || index == points - 1 ? 0f : (index % 2 == 0 ? -.14f : .14f);
                line.SetPosition(index, Vector3.Lerp(start, end, progress) + perpendicular * offset);
            }
        }

        private sealed class TracerEffect : MonoBehaviour
        {
            private LineRenderer glow;
            private LineRenderer core;
            private Color color;
            private float width;
            private float duration;
            private float age;

            public void Initialize(LineRenderer glowLine, LineRenderer coreLine, Color shotColor,
                float shotWidth, float life)
            {
                glow = glowLine;
                core = coreLine;
                color = shotColor;
                width = shotWidth;
                duration = Mathf.Max(.04f, life);
            }

            private void Update()
            {
                age += Time.deltaTime;
                var progress = Mathf.Clamp01(age / duration);
                var fade = 1f - progress * progress;
                glow.startColor = glow.endColor = new Color(color.r, color.g, color.b, .24f * fade);
                core.startColor = core.endColor = new Color(1f, 1f, 1f, fade);
                glow.startWidth = width * Mathf.Lerp(4.6f, 2.4f, progress);
                glow.endWidth = glow.startWidth * .42f;
                core.startWidth = width * Mathf.Lerp(1.35f, .35f, progress);
                core.endWidth = core.startWidth * .42f;
                if (age < duration) return;
                Destroy(glow.material);
                Destroy(core.material);
                Destroy(gameObject);
            }
        }

        private sealed class ImpactEffect : MonoBehaviour
        {
            private SpriteRenderer ring;
            private SpriteRenderer flash;
            private SpriteRenderer[] sparks;
            private Vector3[] velocities;
            private Color color;
            private float size;
            private float duration;
            private float age;

            public void Initialize(SpriteRenderer shockRing, SpriteRenderer impactFlash, SpriteRenderer[] particles,
                Vector3[] particleVelocities, Color impactColor, float impactSize, bool heavy)
            {
                ring = shockRing;
                flash = impactFlash;
                sparks = particles;
                velocities = particleVelocities;
                color = impactColor;
                size = impactSize;
                duration = heavy ? .34f : .22f;
            }

            private void Update()
            {
                age += Time.deltaTime;
                var progress = Mathf.Clamp01(age / duration);
                var fade = 1f - progress;
                ring.transform.localScale = Vector3.one * Mathf.Lerp(.18f, 1.65f * size, progress);
                ring.color = new Color(color.r, color.g, color.b, fade * .82f);
                flash.transform.localScale *= Mathf.Lerp(1f, .82f, Time.deltaTime * 25f);
                flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, fade * fade);
                for (var index = 0; index < sparks.Length; index++)
                {
                    sparks[index].transform.localPosition += velocities[index] * Time.deltaTime;
                    sparks[index].transform.localScale = new Vector3(
                        sparks[index].transform.localScale.x, Mathf.Lerp(.34f, .02f, progress), 1);
                    sparks[index].color = new Color(color.r, color.g, color.b, fade);
                }
                if (age >= duration) Destroy(gameObject);
            }
        }
    }
}
