using UnityEngine;

/// <summary>
/// Salud del enemigo: gestiona daño, estados elementales (quemadura, congelación, maldición),
/// sinergias de muerte (esquirlas, explosiones piro/crio) y recompensas de XP/puntos.
/// </summary>
public class EnemyHealth3D : MonoBehaviour
{
    private static readonly System.Collections.Generic.HashSet<EnemyHealth3D> activeEnemies = new System.Collections.Generic.HashSet<EnemyHealth3D>();
    public static System.Collections.Generic.IReadOnlyCollection<EnemyHealth3D> ActiveEnemies => activeEnemies;
    public static int AliveCount { get; private set; }
    private bool countedAlive;
    private MaterialPropertyBlock colorProperties;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRegistry()
    {
        activeEnemies.Clear();
        AliveCount = 0;
    }

    void OnEnable()
    {
        activeEnemies.Add(this);
        countedAlive = !dying;
        if (countedAlive) AliveCount++;
    }

    void RemoveFromAliveCount()
    {
        if (!countedAlive) return;
        countedAlive = false;
        AliveCount = Mathf.Max(0, AliveCount - 1);
    }

    void OnDisable()
    {
        activeEnemies.Remove(this);
        RemoveFromAliveCount();
    }

    public void SetTint(Color color)
    {
        if (rend == null || !rend.enabled) rend = FindVisibleRenderer();
        if (rend == null) return;
        if (colorProperties == null) colorProperties = new MaterialPropertyBlock();
        rend.GetPropertyBlock(colorProperties);
        colorProperties.SetColor("_BaseColor", color);
        colorProperties.SetColor("_Color", color);
        rend.SetPropertyBlock(colorProperties);
    }

    [Header("Salud")]
    public int maxHealth = 3;
    public int remainingShield { get; private set; }
    public int currentHealth { get; private set; }
    public int CoopId { get; set; }
    public bool RemoteMirror { get; set; }
    public void SetRemoteHealth(int health) { currentHealth = Mathf.Max(0, health); }

    [Header("Estados Elementales")]
    public bool isBurning;
    public bool IsPoisoned => poisonRemaining > 0;
    float poisonRemaining, poisonTick;
    int poisonDamage;
    public void ApplyPoison(float duration, int damagePerSecond)
    {
        if (RemoteMirror && CoopRun.IsGuest) { CoopRun.ReportStatus(CoopId, "poison", duration, damagePerSecond); return; }
        if (!IsPoisoned) SpellVFX.Burst(transform.position+Vector3.up,"smoke_01",new Color(.3f,1,.12f),1.2f);
        poisonRemaining = Mathf.Max(poisonRemaining, duration);
        poisonDamage = Mathf.Max(poisonDamage, damagePerSecond);
    }
    private float burnTimer;
    private float burnTickTimer;
    private int burnDps = 3;
    private string burnSource;

    public bool isFrozen;
    private float freezeTimer;

    public bool isCurse;
    private float curseTimer;
    private int curseDamage = 20;

    private Renderer rend;
    private Color originalColor;
    private float flashTimer;
    private bool dying;
    private float deathTimer;
    private float spawnedAt;
    private string killingWeapon;
    private Vector3 originalScale;

    // XP por tipo de enemigo
    private int xpReward = 20;

    public bool isBoss { get; private set; }
    public bool isElite { get; private set; }
    public bool IsDying => dying;

    void Start()
    {
        spawnedAt = Time.time;
        EnemyAI3D ai = GetComponent<EnemyAI3D>();
        if (ai != null)
        {
            // Fórmula de escalado por tiempo: VidaBase * (1 + (minutos * 0.35) + (minutos^2 * 0.05)) * BonosDificultad
            float m = GameManager.Instance != null ? GameManager.Instance.gameTime / 60f : 0f;
            float difficultyMod = 1f;
            if (MegabonkRelicManager.Instance != null)
            {
                difficultyMod = MegabonkRelicManager.Instance.DifficultyMultiplier;
            }
            if (GameManager.Instance != null)
            {
                difficultyMod *= GameManager.Instance.TierMultiplier;
                if (GameManager.Instance.finalSwarmActive) difficultyMod *= 1.35f;
            }
            float healthScaling = (1f + (m * 0.35f) + (m * m * 0.05f)) * difficultyMod;

            switch (ai.enemyType)
            {
                case EnemyAI3D.EnemyType.Lurker:
                    maxHealth = Mathf.RoundToInt(24 * healthScaling);
                    xpReward = 2;
                    break;
                case EnemyAI3D.EnemyType.Sploder:
                    maxHealth = Mathf.RoundToInt(18 * healthScaling);
                    xpReward = 2;
                    break;
                case EnemyAI3D.EnemyType.Charger:
                    maxHealth = Mathf.RoundToInt(55 * healthScaling);
                    xpReward = 6;
                    break;
                case EnemyAI3D.EnemyType.Summoner:
                    maxHealth = Mathf.RoundToInt(45 * healthScaling);
                    xpReward = 8;
                    break;
                case EnemyAI3D.EnemyType.FlyingShooter:
                    maxHealth = Mathf.RoundToInt(30 * healthScaling);
                    xpReward = 5;
                    break;
                case EnemyAI3D.EnemyType.Elite:
                    isElite = true;
                    maxHealth = Mathf.RoundToInt(280 * healthScaling);
                    xpReward = 35;
                    break;
                case EnemyAI3D.EnemyType.Boss:
                    isBoss = true;
                    maxHealth = Mathf.RoundToInt(1500 * healthScaling);
                    xpReward = 100;
                    break;
            }
        }

        if (ai != null && ai.extractedData != null && ai.extractedData.hp > 0)
        {
            float tier = GameManager.Instance != null ? GameManager.Instance.TierMultiplier : 1;
            float difficulty = MegabonkRelicManager.Instance != null ? MegabonkRelicManager.Instance.DifficultyMultiplier : 1;
            maxHealth = Mathf.Max(1, Mathf.RoundToInt(ai.extractedData.hp * tier * difficulty));
            xpReward = Mathf.Max(1, ai.extractedData.xp);
            remainingShield = Mathf.Max(0, ai.extractedData.shield);
        }
        currentHealth = maxHealth;
        rend = FindVisibleRenderer();
        if (rend != null)
            originalColor = rend.sharedMaterial != null ? rend.sharedMaterial.color : Color.white;
        originalScale = transform.localScale;
    }

    Renderer FindVisibleRenderer()
    {
        foreach (Renderer candidate in GetComponentsInChildren<Renderer>())
            if (candidate != null && candidate.enabled) return candidate;
        return null;
    }

    public void ApplyBurn(float duration, int dps, string sourceWeapon = null)
    {
        if (RemoteMirror && CoopRun.IsGuest) { CoopRun.ReportStatus(CoopId, "burn", duration, dps); return; }
        if (dying) return;
        if (!isBurning) SpellVFX.Fire(transform, Mathf.Min(duration, 4f), 1.1f);
        bool stronger = !isBurning || dps > burnDps;
        isBurning = true;
        burnTimer = Mathf.Max(burnTimer, duration);
        if (stronger)
        {
            burnDps = dps;
            burnSource = sourceWeapon;
        }
        else if (dps == burnDps && !string.IsNullOrEmpty(sourceWeapon))
            burnSource = sourceWeapon;
    }

    public void ApplyFreeze(float duration)
    {
        if (RemoteMirror && CoopRun.IsGuest) { CoopRun.ReportStatus(CoopId, "freeze", duration, 0); return; }
        if (dying) return;
        if (!isFrozen) SpellVFX.Burst(transform.position + Vector3.up, "star_04", new Color(0.3f, 0.85f, 1f), 1.4f);
        isFrozen = true;
        freezeTimer = Mathf.Max(freezeTimer, duration);
        if (rend != null) SetTint(new Color(0.4f, 0.8f, 1f));
    }

    public void ApplyCurse(float delay, int dmg)
    {
        if (RemoteMirror && CoopRun.IsGuest) { CoopRun.ReportStatus(CoopId, "curse", delay, dmg); return; }
        if (dying) return;
        if (!isCurse) SpellVFX.Burst(transform.position + Vector3.up, "magic_01", new Color(0.75f, 0.25f, 1f), 1.5f);
        curseTimer = isCurse ? Mathf.Min(curseTimer, delay) : delay;
        curseDamage = isCurse ? Mathf.Max(curseDamage, dmg) : dmg;
        isCurse = true;
        if (rend != null) SetTint(new Color(0.6f, 0.1f, 0.8f));
    }

    private static Material s_ExplosionMat;

    private static Material GetExplosionMaterial()
    {
        if (s_ExplosionMat == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            s_ExplosionMat = RuntimeMaterialCache.Get(s, new Color(1f, 0.6f, 0.1f, 0.6f), new Color(1f, 0.5f, 0.1f) * 3f);
        }
        return s_ExplosionMat;
    }

    public void TakeDamage(int damage, bool showNumber = false, Color? numColor = null, bool critical = false, string sourceWeapon = null)
    {
        if (RemoteMirror && CoopRun.IsGuest) { CoopRun.ReportHit(CoopId, damage, sourceWeapon); return; }
        if (dying || damage <= 0) return;
        killingWeapon = sourceWeapon;

        // Bonificaciones de daño contra enemigos malditos (Doom +10%, Wither +30%)
        if (isCurse && XPSystem.Instance != null)
        {
            if (XPSystem.Instance.doom) damage = Mathf.RoundToInt(damage * 1.10f);
            if (XPSystem.Instance.wither) damage = Mathf.RoundToInt(damage * 1.30f);
        }

        int absorbed = Mathf.Min(remainingShield, damage);
        remainingShield -= absorbed;
        damage -= absorbed;
        currentHealth -= damage;
        flashTimer = 0.08f;

        if (showNumber)
        {
            DamageNumberManager.Register(transform.position, damage, numColor, critical);
        }

        if (rend != null)
            SetTint(Color.white);

        DragonBossVisuals dragon = GetComponent<DragonBossVisuals>();
        if (dragon != null) dragon.OnTakeHit();

        if (currentHealth <= 0)
            Die();
    }

    void Update()
    {
        if (RemoteMirror && CoopRun.IsGuest) return;
        if (Time.timeScale <= 0f) return;
        if (dying)
        {
            deathTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(deathTimer / 0.3f);
            transform.localScale = originalScale * t;
            if (deathTimer <= 0f)
                Destroy(gameObject);
            return;
        }

        if (poisonRemaining > 0)
        {
            poisonRemaining -= Time.deltaTime;
            poisonTick -= Time.deltaTime;
            if (poisonTick <= 0) { poisonTick = .5f; TakeDamage(Mathf.Max(1, poisonDamage / 2), true, Color.green, false, "poison"); }
            if (dying) return;
            if (poisonRemaining <= 0) poisonDamage = 0;
        }
        // Estado: Quemadura
        if (isBurning)
        {
            burnTimer -= Time.deltaTime;
            burnTickTimer -= Time.deltaTime;
            if (burnTickTimer <= 0f)
            {
                // Sinergia Blight: ticks un 50% más rápido si tiene tanto quemadura como maldición
                bool fastTicks = (XPSystem.Instance != null && XPSystem.Instance.blight && isCurse);
                burnTickTimer = fastTicks ? 0.25f : 0.5f;
                TakeDamage(Mathf.Max(1, burnDps / 2), true, new Color(1f, 0.45f, 0.1f), false, burnSource);
            }
            if (burnTimer <= 0f) isBurning = false;
        }

        // Estado: Congelación
        if (isFrozen)
        {
            freezeTimer -= Time.deltaTime;
            if (freezeTimer <= 0f)
            {
                isFrozen = false;
                if (rend != null) SetTint(originalColor);
            }
        }

        // Estado: Maldición
        if (isCurse)
        {
            curseTimer -= Time.deltaTime;
            if (curseTimer <= 0f)
            {
                isCurse = false;
                TakeDamage(curseDamage, true, new Color(0.85f, 0.25f, 1f), true);
                TriggerExplosion(3f, 15);
            }
        }

        // Flash de daño
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && rend != null && !dying)
            {
                if (isFrozen)
                    SetTint(new Color(0.4f, 0.8f, 1f));
                else if (isBurning)
                    SetTint(new Color(1f, 0.45f, 0.1f));
                else
                    SetTint(originalColor);
            }
        }
    }

    public void TriggerExplosion(float radius, int dmg, bool damageEnemies = true)
    {
        if (damageEnemies)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
            var damagedTargets = new System.Collections.Generic.HashSet<EnemyHealth3D>();
            foreach (var col in colliders)
            {
                if (col.gameObject == gameObject) continue;
                EnemyHealth3D target = col.GetComponentInParent<EnemyHealth3D>();
                if (target != null && target != this && !target.IsDying && damagedTargets.Add(target))
                {
                    target.TakeDamage(dmg, true, new Color(1f, 0.35f, 0.1f));
                }
            }
        }

        Vector3 blastPosition = transform.position + Vector3.up * 0.5f;
        SpellVFX.Burst(blastPosition, "flame_01", new Color(1f, 0.45f, 0.08f), Mathf.Clamp(radius * 0.6f, 0.6f, 3f));
        SpellVFX.Burst(blastPosition, "smoke_01", new Color(0.4f, 0.38f, 0.42f), Mathf.Clamp(radius * 0.4f, 0.5f, 2f));
    }

    // Enemy self-detonation is not a player kill and cannot trigger kill perks.
    public void DetonateWithoutReward(float radius)
    {
        if (dying) return;
        TriggerExplosion(radius, 0, false);
        Die(false);
    }

    void Die(bool grantRewards = true)
    {
        dying = true;
        RemoveFromAliveCount();
        deathTimer = 0.3f;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        EnemyAI3D ai = GetComponent<EnemyAI3D>();
        if (ai != null) ai.enabled = false;

        if (!grantRewards) return;
        if (!string.IsNullOrEmpty(killingWeapon)) ExpeditionProgress.RecordWeaponKill(killingWeapon);
        ExpeditionProgress.RecordSpecialKill(killingWeapon);
        var playerMovement = Object.FindAnyObjectByType<PlayerMovement3D>();
        if (playerMovement != null && playerMovement.ActualSpeed < .1f) ExpeditionProgress.RecordIdleKill();
        if (isBoss || isElite) ExpeditionProgress.RecordBossOrEliteKill(isBoss, isElite, Time.time - spawnedAt);
        CharacterPassives.Instance?.EnemyDied(this);

        // Comprobar sinergias de muerte desde XPSystem
        XPSystem xp = XPSystem.Instance;
        if (xp != null)
        {
            xp.OnEnemyKill(this);

            // Balas Perforantes / Esquirlas (Splinter): suelta 3 esquirlas al morir
            if (xp.splinter)
            {
                SpawnSplinterShards();
            }

            // Iniciador de Fuego (FireStarter): explosión al morir si está quemado
            if (xp.fireStarter && isBurning)
            {
                TriggerExplosion(4.5f, 25);
            }

            // Hielo Cortante (FrostShatter): estalla al morir si está congelado (20% vida máxima en área)
            if (xp.frostShatter && isFrozen)
            {
                int shatterDmg = Mathf.Max(20, Mathf.RoundToInt(maxHealth * 0.20f));
                TriggerExplosion(4.5f, shatterDmg);
            }
        }

        // Puntuación y recompensa de oro equilibrada
        int score = isBoss ? 2000 : 100;
        int goldReward = isBoss ? 25 : 1;
        if (ai != null && !isBoss)
        {
            switch (ai.enemyType)
            {
                case EnemyAI3D.EnemyType.Lurker:        score = 50;  goldReward = 2; break;
                case EnemyAI3D.EnemyType.Sploder:       score = 75;  goldReward = 2; break;
                case EnemyAI3D.EnemyType.Charger:       score = 120; goldReward = 3; break;
                case EnemyAI3D.EnemyType.Summoner:      score = 150; goldReward = 3; break;
                case EnemyAI3D.EnemyType.FlyingShooter: score = 180; goldReward = 3; break;
                case EnemyAI3D.EnemyType.Elite:         score = 400; goldReward = 8; break;
                default:                                score = 100; goldReward = 1; break;
            }
        }

        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            MegabonkRelicManager relics = MegabonkRelicManager.Instance;
            float goldBonus = relics != null ? 1f + 0.15f * relics.GetCount("golden_glove") : 1f;
            gm.AddGold(goldReward * goldBonus);
            float scoreBonus = goldBonus + (relics != null ? .07f * relics.GetCount("skuleg") : 0);
            gm.AddKill(Mathf.RoundToInt(score * scoreBonus));
            if (isBoss) gm.bossesSlain++;
        }
        if (MegabonkRelicManager.Instance != null)
            MegabonkRelicManager.Instance.OnEnemyKill(this);

        // Soltar XP física y Cofre de Jefe
        if (isBoss)
        {
            // Los jefes sueltan 6 esferas grandes de experiencia
            for (int i = 0; i < 6; i++)
            {
                Vector3 offset = Random.insideUnitSphere * 2.5f;
                offset.y = 0.5f;
                XpOrb3D.Spawn(transform.position + offset, 50, XpOrb3D.OrbTier.Large);
            }

            // Soltar Gran Cofre Dorado de Jefe con Reliquias Legendarias
            BossChest3D.Spawn(transform.position, true);
        }
        else
        {
            XpOrb3D.OrbTier tier = XpOrb3D.OrbTier.Small;
            if (ai != null)
            {
                if (ai.enemyType == EnemyAI3D.EnemyType.Elite)
                {
                    tier = XpOrb3D.OrbTier.Large;
                    if (Random.value < .35f) BossChest3D.Spawn(transform.position, false);
                }
                else
                {
                    if (ai.enemyType == EnemyAI3D.EnemyType.Charger || ai.enemyType == EnemyAI3D.EnemyType.Summoner) tier = XpOrb3D.OrbTier.Medium;

                    // La suerte conserva su efecto, pero los cofres normales son más raros.
                    float luck = MegabonkRelicManager.Instance != null ? MegabonkRelicManager.Instance.luckPercent : 0f;
                    float dropChance = 0.01f * (1f + (luck / 100f));
                    if (Random.value < dropChance)
                    {
                        BossChest3D.Spawn(transform.position, false);
                    }
                }
            }
            XpOrb3D.Spawn(transform.position, xpReward, tier);
        }
    }

    void SpawnSplinterShards()
    {
        bool shrapnelBonus = (XPSystem.Instance != null && XPSystem.Instance.shrapnel);
        int shardDmg = shrapnelBonus ? 16 : 12;

        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 startPos = transform.position + Vector3.up * 0.5f;
            Vector3 endPos = startPos + dir * 14f;

            if (shrapnelBonus)
            {
                // Con metralla: perfora objetivos
                RaycastHit[] hits = Physics.RaycastAll(startPos, dir, 14f);
                foreach (var h in hits)
                {
                    EnemyHealth3D target = h.collider.GetComponentInParent<EnemyHealth3D>();
                    if (target != null && target != this && !target.IsDying)
                    {
                        target.TakeDamage(shardDmg);
                    }
                }
            }
            else
            {
                RaycastHit hit;
                if (Physics.Raycast(startPos, dir, out hit, 12f))
                {
                    endPos = hit.point;
                    EnemyHealth3D target = hit.collider.GetComponentInParent<EnemyHealth3D>();
                    if (target != null && target != this && !target.IsDying)
                    {
                        target.TakeDamage(shardDmg);
                    }
                }
            }
            BulletTracer3D.Create(startPos, endPos);
        }
    }
}
