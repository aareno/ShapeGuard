using UnityEngine;

namespace MeadowGuard
{
    public sealed class Enemy : MonoBehaviour
    {
        public float Health { get; private set; }
        private float maxHealth;
        private float speed;
        private int reward;
        private int pathIndex;
        private GameController game;
        private Transform healthFill;

        public void Initialize(GameController owner, int wave, bool elite)
        {
            game = owner;
            maxHealth = Health = (34f + wave * 10f) * (elite ? 2.4f : 1f);
            speed = (1.25f + wave * .025f) * (elite ? .78f : 1f);
            reward = Mathf.RoundToInt((7 + wave * 1.5f) * (elite ? 2.5f : 1f));
            transform.position = game.Path[0];
            transform.localScale = Vector3.one * (elite ? 1.25f : 1f);

            VisualFactory.Part(transform, "Shadow", VisualFactory.Circle, new Color(.08f, .08f, .08f, .3f),
                new Vector3(.1f, -.16f, 0), new Vector3(.95f, .55f), 10);
            VisualFactory.Part(transform, "Body", VisualFactory.Circle,
                elite ? new Color(.62f, .18f, .68f) : new Color(.72f, .24f, .22f), Vector3.zero, new Vector3(.82f, .92f), 11);
            VisualFactory.Part(transform, "Eye L", VisualFactory.Circle, Color.white, new Vector3(-.17f, .12f), Vector3.one * .17f, 12);
            VisualFactory.Part(transform, "Eye R", VisualFactory.Circle, Color.white, new Vector3(.17f, .12f), Vector3.one * .17f, 12);
            VisualFactory.Part(transform, "Pupil L", VisualFactory.Circle, new Color(.1f, .08f, .08f), new Vector3(-.17f, .12f), Vector3.one * .07f, 13);
            VisualFactory.Part(transform, "Pupil R", VisualFactory.Circle, new Color(.1f, .08f, .08f), new Vector3(.17f, .12f), Vector3.one * .07f, 13);
            VisualFactory.Part(transform, "Health Back", VisualFactory.Square, new Color(.1f, .1f, .1f, .75f), new Vector3(0, .66f), new Vector3(.82f, .10f), 14);
            healthFill = VisualFactory.Part(transform, "Health", VisualFactory.Square, new Color(.35f, .9f, .25f),
                new Vector3(0, .66f), new Vector3(.76f, .06f), 15).transform;
        }

        private void Update()
        {
            if (game == null || game.IsGameOver) return;
            var target = game.Path[pathIndex + 1];
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            if ((transform.position - target).sqrMagnitude > .01f) return;
            pathIndex++;
            if (pathIndex >= game.Path.Length - 1) ReachCore();
        }

        public void TakeDamage(float damage)
        {
            Health -= damage;
            var ratio = Mathf.Clamp01(Health / maxHealth);
            healthFill.localScale = new Vector3(.76f * ratio, .06f, 1);
            healthFill.localPosition = new Vector3(-.38f * (1f - ratio), .66f, 0);
            if (Health > 0) return;
            game.EnemyDefeated(this, reward);
            Destroy(gameObject);
        }

        private void ReachCore()
        {
            game.EnemyReachedCore(this);
            Destroy(gameObject);
        }
    }
}
