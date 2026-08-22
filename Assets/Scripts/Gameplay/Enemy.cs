using UnityEngine;

namespace ShapeGuard
{
    public sealed class Enemy : MonoBehaviour
    {
        private GameController game;
        private Vector3[] path;
        private Transform healthFill;
        private float health;
        private float maxHealth;
        private float speed;
        private int reward;
        private int pathIndex;

        public void Initialize(GameController owner, Vector3[] route, int wave)
        {
            game = owner;
            path = route;
            maxHealth = health = 28f + wave * 11f;
            speed = 1.35f + wave * .018f;
            reward = 7 + Mathf.RoundToInt(wave * 1.4f);
            transform.position = path[0];

            VisualFactory.Part(transform, "Shadow", VisualFactory.Circle, new Color(0, 0, 0, .25f),
                new Vector3(.08f, -.12f, 0), new Vector3(.85f, .5f, 1), 9);
            VisualFactory.Part(transform, "Enemy", VisualFactory.Circle, GameBalance.Enemy,
                Vector3.zero, Vector3.one * .82f, 10);
            VisualFactory.Part(transform, "Center", VisualFactory.Circle, GameBalance.Ground,
                Vector3.zero, Vector3.one * .2f, 11);
            VisualFactory.Part(transform, "Health Back", VisualFactory.Square, new Color(0, 0, 0, .7f),
                new Vector3(0, .58f, 0), new Vector3(.78f, .09f, 1), 12);
            healthFill = VisualFactory.Part(transform, "Health", VisualFactory.Square, GameBalance.Gold,
                new Vector3(0, .58f, 0), new Vector3(.72f, .05f, 1), 13).transform;
        }

        private void Update()
        {
            if (game == null || game.IsTransitioning) return;
            var destination = path[pathIndex + 1];
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            if ((transform.position - destination).sqrMagnitude > .005f) return;
            pathIndex++;
            if (pathIndex >= path.Length - 1)
            {
                game.EnemyReachedCore(this, 2);
                Destroy(gameObject);
            }
        }

        public void TakeDamage(float amount)
        {
            health -= amount;
            var ratio = Mathf.Clamp01(health / maxHealth);
            healthFill.localScale = new Vector3(.72f * ratio, .05f, 1);
            healthFill.localPosition = new Vector3(-.36f * (1f - ratio), .58f, 0);
            if (health > 0) return;
            game.EnemyKilled(this, reward);
            Destroy(gameObject);
        }
    }
}
