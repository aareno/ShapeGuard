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
        private Transform alertFrame;
        private Transform visualRoot;
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer frameRenderer;
        private SpriteRenderer coreRenderer;
        private float pulseOffset;
        private Camera gameCamera;
        private float hitTimer;
        private Vector3 hitDirection;
        private Color hitColor;
        private float slowTimer;
        private float slowMultiplier = 1f;
        private bool isBoss;
        private BossKind bossKind;
        private float bossAbilityTimer;
        private int bossPhase;
        private int coreDamage;
        private float healthBarWidth = .94f;

        public bool IsBoss => isBoss;
        public BossKind BossKind => bossKind;
        public float HealthRatio => maxHealth <= 0 ? 0 : Mathf.Clamp01(health / maxHealth);

        public void Initialize(GameController owner, Vector3[] route, int wave, float speedMultiplier = 1f,
            bool boss = false)
        {
            game = owner;
            path = route;
            isBoss = boss;
            bossKind = GameBalance.BossForWave(wave);
            maxHealth = health = boss ? GameBalance.BossHealth(wave) : GameBalance.EnemyHealth(wave);
            speed = GameBalance.EnemySpeed(wave) * Mathf.Max(1f, speedMultiplier) * (boss ? .58f : 1f);
            reward = boss ? GameBalance.BossReward(wave) : GameBalance.EnemyReward(wave);
            coreDamage = boss ? Mathf.Min(GameBalance.CoreHealth, 4 + wave / 20) : GameBalance.EnemyCoreDamage(wave);
            bossAbilityTimer = 5f;
            transform.position = path[0];
            gameCamera = Camera.main;

            pulseOffset = Random.value * Mathf.PI * 2f;
            visualRoot = new GameObject("Visuals").transform;
            visualRoot.SetParent(transform, false);
            VisualFactory.Part(visualRoot, "Shadow", VisualFactory.Circle, new Color(0, 0, 0, .7f),
                new Vector3(.1f, -.14f, 0), new Vector3(1.28f, .72f, 1), 9);
            bodyRenderer = VisualFactory.Part(visualRoot, "Red Body", VisualFactory.Circle,
                new Color(GameBalance.Enemy.r * .45f, GameBalance.Enemy.g * .25f, GameBalance.Enemy.b * .25f, 1f),
                Vector3.zero, Vector3.one * 1.02f, 10);
            frameRenderer = VisualFactory.GlowPart(visualRoot, "Enemy Frame", VisualFactory.PolygonOutline(6),
                GameBalance.Enemy, Vector3.zero, Vector3.one * 1.24f, 12, 1.5f);
            alertFrame = frameRenderer.transform;
            coreRenderer = VisualFactory.Part(visualRoot, "Bright Core", VisualFactory.Polygon(4), new Color(1f, .84f, .55f),
                Vector3.zero, Vector3.one * .25f, 13);
            var healthBack = VisualFactory.Part(visualRoot, "Health Back", VisualFactory.Square, new Color(0, 0, 0, .7f),
                new Vector3(0, .78f, 0), new Vector3(1.02f, .12f, 1), 14);
            healthFill = VisualFactory.Part(visualRoot, "Health", VisualFactory.Square, GameBalance.Gold,
                new Vector3(0, .78f, 0), new Vector3(.94f, .07f, 1), 15).transform;

            if (isBoss)
            {
                name = GameBalance.BossName(bossKind);
                bodyRenderer.color = new Color(.34f, .08f, .07f, 1f);
                frameRenderer.color = GameBalance.Gold;
                bodyRenderer.transform.localScale = Vector3.one * 1.55f;
                alertFrame.localScale = Vector3.one * 1.8f;
                coreRenderer.transform.localScale = Vector3.one * .42f;
                healthBarWidth = 2.2f;
                healthBack.transform.localScale = new Vector3(2.35f, .16f, 1f);
                healthFill.localScale = new Vector3(healthBarWidth, .1f, 1f);
                VisualFactory.GlowPart(visualRoot, "Boss Crown", VisualFactory.PolygonOutline(8), GameBalance.Gold,
                    Vector3.zero, Vector3.one * 1.72f, 11, 1.65f);
            }
        }

        private void Update()
        {
            if (game == null || game.IsTransitioning) return;
            var visibilityScale = gameCamera == null ? 1f : Mathf.Clamp(gameCamera.orthographicSize / 52f, 1f, 4f);
            transform.localScale = Vector3.one * visibilityScale;
            UpdateHitFeedback();
            slowTimer = Mathf.Max(0f, slowTimer - Time.deltaTime);
            if (slowTimer <= 0f) slowMultiplier = 1f;
            if (isBoss) UpdateBossAbility();
            if (alertFrame != null)
            {
                var pulse = 1f + Mathf.Sin(Time.unscaledTime * 5f + pulseOffset) * .08f;
                alertFrame.localScale = Vector3.one * ((isBoss ? 1.8f : 1.24f) * pulse);
            }
            var destination = path[pathIndex + 1];
            transform.position = Vector3.MoveTowards(transform.position, destination,
                speed * slowMultiplier * Time.deltaTime);
            if ((transform.position - destination).sqrMagnitude > .005f) return;
            pathIndex++;
            if (pathIndex >= path.Length - 1)
            {
                game.EnemyReachedCore(this, coreDamage);
                Destroy(gameObject);
            }
        }

        private void UpdateBossAbility()
        {
            bossAbilityTimer -= Time.deltaTime;
            if (bossKind == BossKind.Splitter)
            {
                var targetPhase = HealthRatio < .34f ? 2 : HealthRatio < .67f ? 1 : 0;
                if (targetPhase > bossPhase)
                {
                    speed *= Mathf.Pow(1.3f, targetPhase - bossPhase);
                    bossPhase = targetPhase;
                    game.SpawnBossMinions(transform.position, path, pathIndex, 2 + bossPhase);
                    game.ShowImpact(transform.position, GameBalance.Gold, 2.2f, true);
                }
            }
            if (bossAbilityTimer > 0) return;
            bossAbilityTimer = bossKind == BossKind.BroodCore ? 6f : 5f;
            switch (bossKind)
            {
                case BossKind.BroodCore:
                    game.SpawnBossMinions(transform.position, path, pathIndex, 3 + game.ActiveWave / 50);
                    break;
                case BossKind.Leech:
                    var drained = game.DrainOreForBoss(Mathf.Max(5, game.ActiveWave * 2));
                    health = Mathf.Min(maxHealth, health + drained * maxHealth * .0008f);
                    break;
                case BossKind.Architect:
                    bossPhase = 1 - bossPhase;
                    game.ShowImpact(transform.position, bossPhase == 1 ? GameBalance.Ore : GameBalance.Gold,
                        2f, true);
                    break;
            }
        }

        private void UpdateHitFeedback()
        {
            hitTimer = Mathf.Max(0, hitTimer - Time.deltaTime);
            var strength = hitTimer <= 0 ? 0f : Mathf.Sin(hitTimer / .14f * Mathf.PI);
            visualRoot.localPosition = hitDirection * (.16f * strength);
            visualRoot.localScale = new Vector3(1f + .14f * strength, 1f - .1f * strength, 1f);
            var flash = Color.Lerp(hitColor, Color.white, .78f);
            var bodyColor = isBoss ? new Color(.34f, .08f, .07f, 1f) :
                new Color(GameBalance.Enemy.r * .45f, GameBalance.Enemy.g * .25f, GameBalance.Enemy.b * .25f, 1f);
            var frameColor = isBoss ? GameBalance.Gold : GameBalance.Enemy;
            bodyRenderer.color = Color.Lerp(bodyColor, flash, strength);
            frameRenderer.color = Color.Lerp(frameColor, Color.white, strength);
            coreRenderer.color = Color.Lerp(new Color(1f, .84f, .55f), Color.white, strength);
        }

        public void TakeDamage(float amount, Vector3? sourcePosition = null, Color? impactColor = null)
        {
            hitTimer = .14f;
            hitDirection = sourcePosition.HasValue
                ? (transform.position - sourcePosition.Value).normalized
                : Vector3.zero;
            hitColor = impactColor ?? GameBalance.Enemy;
            if (isBoss && bossKind == BossKind.Bulwark && HealthRatio > .5f) amount *= .45f;
            if (isBoss && bossKind == BossKind.Architect && bossPhase == 1) amount *= .6f;
            health -= amount;
            var ratio = Mathf.Clamp01(health / maxHealth);
            healthFill.localScale = new Vector3(healthBarWidth * ratio, isBoss ? .1f : .07f, 1);
            healthFill.localPosition = new Vector3(-healthBarWidth * .5f * (1f - ratio), .78f, 0);
            if (health > 0) return;
            game.ShowEnemyDeath(transform.position, hitColor);
            game.EnemyKilled(this, reward);
            Destroy(gameObject);
        }

        public void ApplySlow(float speedMultiplier, float duration)
        {
            slowMultiplier = Mathf.Min(slowMultiplier, Mathf.Clamp(speedMultiplier, .25f, 1f));
            slowTimer = Mathf.Max(slowTimer, duration);
        }
    }
}
