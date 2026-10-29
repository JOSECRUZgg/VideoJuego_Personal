using UnityEngine;

/// <summary>
/// Comportamientos de las criaturas de la expedición:
/// 1. Lurker (Caminante Estándar): Persigue directamente en masa.
/// 2. Sploder (Explosivo): Se acerca, cuenta atrás de 1s con parpadeo y estalla.
/// 3. Charger (Embestidor): Pausa de 1.5s apuntando y luego embestida rápida.
/// 4. Summoner / Shooter (Disparador): Mantiene distancia y dispara proyectiles lentos.
/// 5. Elite (Mini-Jefe): Gran resistencia, inmune a empuje, ráfagas de proyectiles.
/// 6. Boss (Jefe de Minuto): Ataques en fases (anillos de balas, cargas, invocaciones).
/// </summary>
public class EnemyAI3D : MonoBehaviour
{
    public enum EnemyType
    {
        Lurker,         // Caminante estándar (antiguo Normal)
        Sploder,        // Explosivo suicida
        Charger,        // Embestidor (antiguo Fast)
        Summoner,       // Disparador a distancia
        FlyingShooter,  // Mob volador que dispara proyectiles desde el aire
        Elite,          // Mini-Jefe resistente (antiguo Tank)
        Boss,           // Jefe masivo
        Normal = Lurker,
        Fast = Charger,
        Tank = Elite
    }

    [Header("Tipo de enemigo")]
    public EnemyType enemyType = EnemyType.Lurker;
    [System.NonSerialized] public ExtractedEnemyCatalog.Entry extractedData;

    [Header("Estadísticas de movimiento")]
    public float speed = 3.5f;
    public float stopDistance = 1.5f;
    public int damage = 10;
    public float attackRate = 1.0f;

    [Header("Evasión y empuje")]
    public float avoidDistance = 2f;
    public float avoidForce = 3f;

    private float nextAttack;
    private Transform player;
    private bool statsInitialized;
    private Vector3 knockbackVelocity;
    private float nextTerrainSnapTime;
    private Vector3 lastWalkablePosition;
    private bool hasWalkablePosition;

    // Estados especiales
    // Sploder
    private bool isSploderCountingDown;
    private float sploderTimer = 1.0f;

    // Charger
    private bool isChargerAiming;
    private float chargerAimTimer = 1.5f;
    private bool isChargerDashing;
    private float chargerDashTimer = 1.0f;
    private Vector3 chargerDashDir;

    // Summoner / Shooter
    private float shootTimer = 3.0f;

    // Boss & Elite
    private float specialAttackTimer = 4.0f;

    // Boss Move Set
    private enum BossAttackType
    {
        Chase,
        HornWindup,
        HornCharge,
        FireBreath,
        JumpSmash,
        RingBurst
    }

    private BossAttackType bossAttackMode = BossAttackType.Chase;
    private float bossActionTimer = 0f;
    private float bossSkillCooldown = 3.5f;
    private Vector3 bossSpecialDir;
    private Vector3 bossJumpOrigin;
    private Vector3 bossJumpTarget;
    private int bossMoveIndex = 0;
    private float bossBreathSubTimer = 0f;
    private int bossBreathShotsFired = 0;
    private bool bossSummonedMinions = false;

    private Animator anim;
    private DragonBossVisuals dragonBossVisuals;

    private static Transform s_CachedPlayer;
    private static readonly System.Collections.Generic.Dictionary<Color, Material> s_MaterialCache = new System.Collections.Generic.Dictionary<Color, Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_CachedPlayer = null;
        s_MaterialCache.Clear();
    }

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim != null && !anim.enabled) anim = null;
        dragonBossVisuals = GetComponent<DragonBossVisuals>();
        if (dragonBossVisuals != null && !dragonBossVisuals.enabled) dragonBossVisuals = null;
        if (s_CachedPlayer == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) s_CachedPlayer = p.transform;
        }
        player = s_CachedPlayer;
        if (enemyType != EnemyType.FlyingShooter && ArenaEnricher.Instance != null &&
            ArenaEnricher.Instance.TryGetSnowHeight(transform.position, out _))
        {
            lastWalkablePosition = transform.position;
            hasWalkablePosition = true;
        }
        ApplyTypeStats();
    }

    public void ApplyTypeStats(bool force = false)
    {
        if (statsInitialized && !force) return;
        statsInitialized = true;

        switch (enemyType)
        {
            case EnemyType.Lurker:
                speed = 3.6f;
                stopDistance = 1.2f;
                damage = 8;
                attackRate = 1.0f;
                transform.localScale = Vector3.one * 0.75f;
                SetVisualColor(new Color(1f, 0.2f, 0.2f));
                break;

            case EnemyType.Sploder:
                speed = 4.2f;
                stopDistance = 3.5f;
                damage = 15;
                attackRate = 1.0f;
                transform.localScale = Vector3.one * 0.85f;
                SetVisualColor(new Color(1f, 0.45f, 0.1f));
                break;

            case EnemyType.Charger:
                speed = 2.8f;
                stopDistance = 1.5f;
                damage = 12;
                attackRate = 1.2f;
                transform.localScale = new Vector3(0.75f, 1.2f, 0.75f);
                SetVisualColor(new Color(1f, 0.8f, 0.1f));
                break;

            case EnemyType.Summoner:
                speed = 2.5f;
                stopDistance = 14f; // Mantiene distancia
                damage = 10;
                attackRate = 3.0f;
                transform.localScale = Vector3.one * 1.0f;
                SetVisualColor(new Color(0.6f, 0.1f, 0.9f));
                break;

            case EnemyType.FlyingShooter:
                speed = 4.2f;
                stopDistance = 12f;
                damage = 9;
                attackRate = 2.4f;
                transform.localScale = Vector3.one * 0.45f;
                SetVisualColor(new Color(0.6f, 0.2f, 0.95f));
                break;

            case EnemyType.Elite:
                speed = 2.8f;
                stopDistance = 2.5f;
                damage = 16;
                attackRate = 1.5f;
                transform.localScale = Vector3.one * 1.8f;
                SetVisualColor(new Color(0.85f, 0.05f, 0.25f));
                break;

            case EnemyType.Boss:
                speed = 2.2f;
                stopDistance = 3.0f;
                damage = 25;
                attackRate = 1.5f;
                transform.localScale = Vector3.one * 3.4f;
                SetVisualColor(new Color(0.9f, 0.05f, 0.1f));
                break;
        }
    }

    private static MaterialPropertyBlock s_ColorBlock;

    void SetVisualColor(Color c)
    {
        if (enemyType == EnemyType.Boss) return; // Mantener texturas PBR completas del dragón

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            if (s_ColorBlock == null) s_ColorBlock = new MaterialPropertyBlock();
            foreach (Renderer r in renderers)
            {
                if (r == null || r is ParticleSystemRenderer) continue;
                r.GetPropertyBlock(s_ColorBlock);
                Color finalColor = (enemyType == EnemyType.Lurker) ? Color.white : Color.Lerp(Color.white, c, 0.45f);
                s_ColorBlock.SetColor("_BaseColor", finalColor);
                s_ColorBlock.SetColor("_Color", finalColor);
                r.SetPropertyBlock(s_ColorBlock);
            }
        }
    }

    public void ApplyKnockback(Vector3 dir, float force)
    {
        // Los Elites y Jefes son inmunes o casi inmunes al empuje
        if (enemyType == EnemyType.Elite || enemyType == EnemyType.Boss)
            return;

        dir.y = 0f;
        knockbackVelocity = dir.normalized * force;
    }

    void Update()
    {
        if (Time.timeScale <= 0f || MegabonkRelicManager.HostileTimeScale <= 0) return;
        if (player == null)
        {
            if (s_CachedPlayer == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) s_CachedPlayer = p.transform;
            }
            player = s_CachedPlayer;
            if (player == null) return;
        }

        if (CoopRun.IsHost && CoopRun.Partner != null)
        {
            Transform own = s_CachedPlayer;
            Transform other = CoopRun.PartnerDowned ? null : CoopRun.Partner;
            bool ownAlive = own != null && own.GetComponent<PlayerHealth3D>()?.isDead != true;
            if (!ownAlive) player = other;
            else if (other == null) player = own;
            else
            {
                float ownDistance = (own.position - transform.position).sqrMagnitude;
                float otherDistance = (other.position - transform.position).sqrMagnitude;
                player = player == other ? (ownDistance < otherDistance * .56f ? own : other)
                    : (otherDistance < ownDistance * .56f ? other : own);
            }
            if (player == null) return;
        }

        EnemyHealth3D health = GetComponent<EnemyHealth3D>();
        if (health != null && health.IsDying) return;
        Vector3 movementOrigin = transform.position;

        // Desvanecer empuje
        if (knockbackVelocity.sqrMagnitude > 0.01f)
        {
            transform.position += knockbackVelocity * MegabonkRelicManager.HostileDeltaTime;
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 10f * MegabonkRelicManager.HostileDeltaTime);
        }

        // Si está congelado, la velocidad es 0
        if (health != null && health.isFrozen)
        {
            if (anim != null) anim.SetFloat("Speed", 0f);
            return;
        }

        Vector3 toPlayer = (player.position - transform.position);
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;
        Vector3 dir = toPlayer.normalized;

        // Mirar hacia el jugador
        if (dir.sqrMagnitude > 0.001f && !isChargerDashing && (enemyType != EnemyType.Boss || (bossAttackMode != BossAttackType.HornCharge && bossAttackMode != BossAttackType.JumpSmash)))
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * MegabonkRelicManager.HostileDeltaTime);
        }

        if (anim != null)
        {
            bool isMoving = dist > stopDistance && (health == null || (!health.isFrozen && !health.IsDying));
            anim.SetFloat("Speed", isMoving ? speed : 0f);
        }

        // ── COMPORTAMIENTO POR TIPO ──────────────────────────────
        switch (enemyType)
        {
            case EnemyType.Lurker:
                UpdateLurker(dist, dir);
                break;

            case EnemyType.Sploder:
                UpdateSploder(dist, dir, health);
                break;

            case EnemyType.Charger:
                UpdateCharger(dist, dir);
                break;

            case EnemyType.Summoner:
                UpdateSummoner(dist, dir);
                break;

            case EnemyType.FlyingShooter:
                UpdateFlyingShooter(dist, dir);
                break;

            case EnemyType.Elite:
                UpdateElite(dist, dir);
                break;

            case EnemyType.Boss:
                var extractedCombat = GetComponent<ExtractedBossCombat>();
                if (extractedCombat != null && extractedCombat.HasAttacks)
                {
                    if (!extractedCombat.IsBusy) UpdateLurker(dist, dir);
                }
                else UpdateBoss(dist, dir);
                break;
        }

        if (enemyType != EnemyType.FlyingShooter &&
            !(enemyType == EnemyType.Boss && bossAttackMode == BossAttackType.JumpSmash))
        {
            bool charging = isChargerDashing || (enemyType == EnemyType.Boss && bossAttackMode == BossAttackType.HornCharge);
            Vector3 desired = transform.position;
            transform.position = EnemyGroundMotion.Resolve(transform, movementOrigin, desired, !charging);
            if (charging && (desired - transform.position).sqrMagnitude > .04f)
            {
                chargerDashTimer = 0;
                if (enemyType == EnemyType.Boss) bossAttackMode = BossAttackType.Chase;
            }
        }

        if (enemyType != EnemyType.FlyingShooter && Time.time >= nextTerrainSnapTime &&
            !(enemyType == EnemyType.Boss && bossAttackMode == BossAttackType.JumpSmash) &&
            ArenaEnricher.Instance != null)
        {
            nextTerrainSnapTime = Time.time + 0.12f;
            if (ArenaEnricher.Instance.TryGetSnowHeight(transform.position, out float groundHeight))
            {
                Vector3 groundedPosition = transform.position;
                groundedPosition.y = groundHeight;
                transform.position = groundedPosition;
                lastWalkablePosition = groundedPosition;
                hasWalkablePosition = true;
            }
            else if (hasWalkablePosition)
                transform.position = lastWalkablePosition;
        }
    }

    // 1. LURKER: Persecución en línea recta
    void UpdateLurker(float dist, Vector3 dir)
    {
        if (dist > stopDistance)
        {
            transform.position += dir * speed * MegabonkRelicManager.HostileDeltaTime;
        }
        else if (Time.time >= nextAttack)
        {
            AttackPlayer(damage);
        }
    }

    // 2. SPLODER: Cuenta atrás y explosión
    void UpdateSploder(float dist, Vector3 dir, EnemyHealth3D health)
    {
        if (!isSploderCountingDown)
        {
            if (dist > 3.8f)
            {
                transform.position += dir * speed * MegabonkRelicManager.HostileDeltaTime;
            }
            else
            {
                // Iniciar cuenta atrás de detonación
                isSploderCountingDown = true;
                sploderTimer = 1.0f;
            }
        }
        else
        {
            // Parpadear rápidamente
            sploderTimer -= MegabonkRelicManager.HostileDeltaTime;
            Renderer r = GetComponentInChildren<Renderer>();
            if (r != null)
            {
                bool flash = Mathf.Sin(Time.time * 25f) > 0f;
                if (health != null) health.SetTint(flash ? Color.white : new Color(1f, 0.2f, 0.05f));
            }

            if (sploderTimer <= 0f)
            {
                // Detonar
                if (health != null)
                {
                    if (dist <= 6.5f) AttackPlayer(damage);
                    health.DetonateWithoutReward(6.5f);
                }
            }
        }
    }

    // 3. CHARGER: Pausa de 1.5s y deslizamiento rápido
    void UpdateCharger(float dist, Vector3 dir)
    {
        if (isChargerDashing)
        {
            chargerDashTimer -= MegabonkRelicManager.HostileDeltaTime;
            transform.position += chargerDashDir * (speed * 4.5f) * MegabonkRelicManager.HostileDeltaTime;

            if (dist <= 1.8f && Time.time >= nextAttack)
            {
                AttackPlayer(damage);
            }

            if (chargerDashTimer <= 0f)
            {
                isChargerDashing = false;
                isChargerAiming = true;
                chargerAimTimer = 1.5f;
            }
        }
        else if (isChargerAiming)
        {
            // Se detiene por 1.5s apuntando
            chargerAimTimer -= MegabonkRelicManager.HostileDeltaTime;
            if (chargerAimTimer <= 0f)
            {
                isChargerAiming = false;
                isChargerDashing = true;
                chargerDashTimer = 0.9f;
                chargerDashDir = dir;
            }
        }
        else
        {
            // Acercarse normalmente hasta distancia de carga
            if (dist > 12f)
            {
                transform.position += dir * speed * MegabonkRelicManager.HostileDeltaTime;
            }
            else
            {
                isChargerAiming = true;
                chargerAimTimer = 1.5f;
            }
        }
    }

    // 4. SUMMONER: Mantiene distancia prudente y dispara proyectiles lentos
    void UpdateSummoner(float dist, Vector3 dir)
    {
        float targetDist = extractedData != null && extractedData.minDistance > 0 ? extractedData.minDistance : 14f;
        if (dist < targetDist - 2f)
        {
            // Alejarse si el jugador está muy cerca
            transform.position -= dir * (speed * 1.2f) * MegabonkRelicManager.HostileDeltaTime;
        }
        else if (dist > (extractedData != null && extractedData.maxDistance > targetDist ? extractedData.maxDistance : targetDist + 2f))
        {
            // Acercarse si está muy lejos
            transform.position += dir * speed * MegabonkRelicManager.HostileDeltaTime;
        }

        // Disparo de proyectil cada 3s
        shootTimer -= MegabonkRelicManager.HostileDeltaTime;
        if (shootTimer <= 0f)
        {
            shootTimer = 3.0f;
            Vector3 startP = transform.position + Vector3.up * 1f;
            Vector3 targetP = player.position + Vector3.up * 1f;
            EnemyProjectile3D.Create(startP, targetP, damage, new Color(0.7f, 0.2f, 1f));
        }
    }

    private float flightTimer;

    // 4b. FLYING SHOOTER: Vuela por el aire (altitud 4m a 6m) y dispara proyectiles hacia el jugador
    void UpdateFlyingShooter(float dist, Vector3 dir)
    {
        flightTimer += MegabonkRelicManager.HostileDeltaTime;

        // Altura de vuelo flotante con movimiento senoidal sobre el jugador
        float targetY = player.position.y + 4.5f + Mathf.Sin(flightTimer * 2.2f) * 0.8f;
        float currentY = Mathf.Lerp(transform.position.y, targetY, 4f * MegabonkRelicManager.HostileDeltaTime);

        // Movimiento aéreo orbital / persecución
        float desiredDistance = 13f;
        Vector3 horizontalVelocity = Vector3.zero;

        if (dist > desiredDistance + 2.5f)
        {
            horizontalVelocity = dir * speed;
        }
        else if (dist < desiredDistance - 2.5f)
        {
            horizontalVelocity = -dir * (speed * 1.1f);
        }
        else
        {
            Vector3 strafe = Vector3.Cross(Vector3.up, dir);
            horizontalVelocity = strafe * (speed * 0.75f);
        }

        Vector3 nextPos = transform.position + horizontalVelocity * MegabonkRelicManager.HostileDeltaTime;
        nextPos.y = currentY;
        if (ArenaEnricher.Instance != null && ArenaEnricher.Instance.TryGetSnowHeight(nextPos, out float terrainY))
            nextPos.y = Mathf.Max(nextPos.y, terrainY + 3f);
        transform.position = nextPos;

        // Apuntar suavemente hacia el jugador abajo
        Vector3 aimVector = (player.position + Vector3.up * 1f) - transform.position;
        if (aimVector.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(aimVector.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 6f * MegabonkRelicManager.HostileDeltaTime);
        }

        if (anim != null)
        {
            anim.SetFloat("Speed", speed);
            if (anim.HasState(0, Animator.StringToHash("Fly Forward")))
            {
                var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsName("Fly Forward") && !stateInfo.IsName("Fly Fireball Shoot") && !stateInfo.IsName("Fly Float") && !stateInfo.IsName("Get Hit") && !stateInfo.IsName("Die"))
                {
                    anim.CrossFade("Fly Forward", 0.2f);
                }
            }
        }

        // Disparo de proyectil desde el aire cada 2.5s
        shootTimer -= MegabonkRelicManager.HostileDeltaTime;
        if (shootTimer <= 0f)
        {
            shootTimer = Random.Range(2.2f, 3.0f);
            Vector3 startP = transform.position + transform.forward * 0.9f;
            Vector3 targetP = player.position + Vector3.up * 0.9f;

            // Proyectil de fuego místico disparado desde las alturas
            EnemyProjectile3D.Create(startP, targetP, damage, new Color(0.85f, 0.25f, 1f));

            if (anim != null && anim.HasState(0, Animator.StringToHash("Fly Fireball Shoot")))
            {
                anim.CrossFade("Fly Fireball Shoot", 0.15f);
            }
        }
    }

    // 5. ELITE: Persecución constante con ráfagas de proyectiles
    void UpdateElite(float dist, Vector3 dir)
    {
        if (dist > stopDistance)
        {
            transform.position += dir * speed * MegabonkRelicManager.HostileDeltaTime;
        }
        else if (Time.time >= nextAttack)
        {
            AttackPlayer(damage);
        }

        // Ráfaga de 3 proyectiles cada 3.5s
        specialAttackTimer -= MegabonkRelicManager.HostileDeltaTime;
        if (specialAttackTimer <= 0f)
        {
            specialAttackTimer = 3.5f;
            Vector3 startP = transform.position + Vector3.up * 1.2f;
            for (int i = -1; i <= 1; i++)
            {
                Vector3 spreadTarget = player.position + player.right * (i * 2.5f) + Vector3.up * 1f;
                EnemyProjectile3D.Create(startP, spreadTarget, damage, Color.red);
            }
        }
    }

    // 6. BOSS: Set de movimientos completo y temible (Furia, Minions, Embestida con Cuernos, Torrente Ígneo, Salto Sísmico y Anillo 360°)
    void UpdateBoss(float dist, Vector3 dir)
    {
        EnemyHealth3D health = GetComponent<EnemyHealth3D>();
        bool isEnraged = health != null && health.currentHealth < health.maxHealth * 0.50f;
        float actualSpeed = isEnraged ? speed * 1.5f : speed * 1.15f;
        float minCooldown = isEnraged ? 1.8f : 2.8f;

        // Furia: Al llegar al 50% de HP ruge fuertemente y convoca dragones esbirros
        if (isEnraged && !bossSummonedMinions)
        {
            bossSummonedMinions = true;
            if (dragonBossVisuals != null) dragonBossVisuals.OnSpecialRoar();
            EnemySpawner3D spawner = Object.FindAnyObjectByType<EnemySpawner3D>();
            if (spawner != null && spawner.flyingEnemyPrefab != null)
            {
                for (int s = 0; s < 2; s++)
                {
                    Vector3 minionPos = transform.position + (s == 0 ? transform.right : -transform.right) * 5f + Vector3.up * 3.5f;
                    GameObject minion = Instantiate(spawner.flyingEnemyPrefab, minionPos, Quaternion.identity);
                    minion.name = "DragonMinion_" + s;
                    minion.tag = "Enemy";
                    EnemyAI3D ai = minion.GetComponent<EnemyAI3D>() ?? minion.AddComponent<EnemyAI3D>();
                    ai.enemyType = EnemyType.FlyingShooter;
                    ai.ApplyTypeStats();
                }
            }
        }

        switch (bossAttackMode)
        {
            case BossAttackType.Chase:
                if (dist > stopDistance)
                {
                    transform.position += dir * actualSpeed * MegabonkRelicManager.HostileDeltaTime;
                }
                else if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + (isEnraged ? attackRate * 0.75f : attackRate) / Mathf.Max(.1f, MegabonkRelicManager.HostileTimeScale);
                    bool useBite = Random.value < 0.5f;
                    if (dragonBossVisuals != null)
                    {
                        if (useBite) dragonBossVisuals.OnBasicBite();
                        else dragonBossVisuals.OnClawAttack();
                    }
                    AttackPlayer(useBite ? 32 : 26);
                }

                // Temporizador para seleccionar el próximo ataque especial
                bossSkillCooldown -= MegabonkRelicManager.HostileDeltaTime;
                if (bossSkillCooldown <= 0f)
                {
                    bossSkillCooldown = Random.Range(minCooldown, minCooldown + 1.0f);
                    bossMoveIndex++;

                    // Selección dinámica de ataque
                    if (isEnraged && (bossMoveIndex % 3 == 0))
                    {
                        // 1. Salto Sísmico / Terremoto (Slam)
                        bossAttackMode = BossAttackType.JumpSmash;
                        bossActionTimer = 1.15f;
                        bossJumpOrigin = transform.position;
                        bossJumpTarget = player.position;
                        if (dragonBossVisuals != null) dragonBossVisuals.OnJumpSlam();
                    }
                    else if (dist > 6.0f && (bossMoveIndex % 2 == 0))
                    {
                        // 2. Embestida con Cuernos (Horn Charge)
                        bossAttackMode = BossAttackType.HornWindup;
                        bossActionTimer = 0.5f;
                        bossSpecialDir = dir;
                        if (dragonBossVisuals != null) dragonBossVisuals.OnSpecialRoar();
                    }
                    else if (dist <= 18f && (bossMoveIndex % 2 == 1))
                    {
                        // 3. Aliento Ígneo / Cono de Fuego (Fire Breath)
                        bossAttackMode = BossAttackType.FireBreath;
                        bossActionTimer = 1.4f;
                        bossBreathSubTimer = 0f;
                        bossBreathShotsFired = 0;
                        bossSpecialDir = dir;
                        if (dragonBossVisuals != null) dragonBossVisuals.OnFireBreath();
                    }
                    else
                    {
                        // 4. Anillo de Fuego 360° (Ring Burst)
                        bossAttackMode = BossAttackType.RingBurst;
                        bossActionTimer = 0.85f;
                        if (dragonBossVisuals != null) dragonBossVisuals.OnSpecialRoar();

                        Vector3 startP = transform.position + Vector3.up * 1.5f;
                        int projectileCount = isEnraged ? 16 : 12;
                        for (int i = 0; i < projectileCount; i++)
                        {
                            float angle = i * (360f / projectileCount) * Mathf.Deg2Rad;
                            Vector3 ringDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                            EnemyProjectile3D.Create(startP, startP + ringDir * 14f, 20, new Color(1f, 0.2f, 0.1f), burn: true);
                        }
                    }
                }
                break;

            case BossAttackType.HornWindup:
                bossActionTimer -= MegabonkRelicManager.HostileDeltaTime;
                if (bossSpecialDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(bossSpecialDir), 14f * MegabonkRelicManager.HostileDeltaTime);
                }
                if (bossActionTimer <= 0f)
                {
                    bossAttackMode = BossAttackType.HornCharge;
                    bossActionTimer = 1.3f;
                    if (dragonBossVisuals != null) dragonBossVisuals.OnHornCharge();
                }
                break;

            case BossAttackType.HornCharge:
                transform.position += bossSpecialDir * (actualSpeed * 3.8f) * MegabonkRelicManager.HostileDeltaTime;
                if (bossSpecialDir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(bossSpecialDir);

                if (Vector3.Distance(transform.position, player.position) < 3.5f)
                {
                    MagicVFX.SpawnImpact(player.position + Vector3.up * 1f, 3.2f, new Color(1f, 0.3f, 0.1f));
                    AttackPlayer(40); // 2 corazones completos de impacto directo
                    bossAttackMode = BossAttackType.Chase;
                }
                else
                {
                    bossActionTimer -= MegabonkRelicManager.HostileDeltaTime;
                    if (bossActionTimer <= 0f)
                    {
                        bossAttackMode = BossAttackType.Chase;
                    }
                }
                break;

            case BossAttackType.FireBreath:
                bossActionTimer -= MegabonkRelicManager.HostileDeltaTime;
                bossBreathSubTimer -= MegabonkRelicManager.HostileDeltaTime;

                if (dir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * MegabonkRelicManager.HostileDeltaTime);
                }

                if (bossBreathSubTimer <= 0f && bossBreathShotsFired < 8)
                {
                    bossBreathSubTimer = 0.14f;
                    bossBreathShotsFired++;
                    Vector3 mouthPos = transform.position + transform.forward * 2.4f + Vector3.up * 1.8f;
                    Vector3 spread = player.right * Random.Range(-2.0f, 2.0f) + Vector3.up * Random.Range(-0.2f, 0.6f);
                    EnemyProjectile3D.Create(mouthPos, player.position + spread + Vector3.up * 0.9f, 18, new Color(1f, 0.35f, 0.05f), burn: true);
                }

                if (bossActionTimer <= 0f)
                {
                    bossAttackMode = BossAttackType.Chase;
                }
                break;

            case BossAttackType.JumpSmash:
                bossActionTimer -= MegabonkRelicManager.HostileDeltaTime;
                float totalAirTime = 1.15f;
                float progress = 1f - Mathf.Clamp01(bossActionTimer / totalAirTime);

                Vector3 landPlane = Vector3.Lerp(bossJumpOrigin, bossJumpTarget, progress);
                float height = Mathf.Sin(progress * Mathf.PI) * 6.5f;
                landPlane.y += height;
                transform.position = landPlane;

                if (bossActionTimer <= 0f)
                {
                    Vector3 landing = new Vector3(bossJumpTarget.x, 0.1f, bossJumpTarget.z);
                    if (ArenaEnricher.Instance != null) landing = ArenaEnricher.Instance.PlaceOnSnow(landing, 0.1f);
                    transform.position = landing;

                    // Impacto sísmico visual
                    Vector3 impactPos = transform.position + Vector3.up * 0.6f;
                    MagicVFX.SpawnImpact(impactPos, 3.8f, new Color(1f, 0.4f, 0.1f));
                    MagicVFX.SpawnWhirlwind(landing, 1.3f, 4.8f, new Color(1f, 0.5f, 0.15f));

                    if (Vector3.Distance(transform.position, player.position) <= 6.5f)
                    {
                        AttackPlayer(45); // 2 corazones
                    }

                    // Anillo de fuego sísmico de 16 meteoros en expansión
                    int meteorCount = isEnraged ? 16 : 12;
                    for (int i = 0; i < meteorCount; i++)
                    {
                        float angle = i * (360f / meteorCount) * Mathf.Deg2Rad;
                        Vector3 waveDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                        EnemyProjectile3D.Create(impactPos, impactPos + waveDir * 14f, 18, new Color(1f, 0.4f, 0.05f), burn: true);
                    }

                    bossAttackMode = BossAttackType.Chase;
                }
                break;

            case BossAttackType.RingBurst:
                bossActionTimer -= MegabonkRelicManager.HostileDeltaTime;
                if (bossActionTimer <= 0f)
                {
                    bossAttackMode = BossAttackType.Chase;
                }
                break;
        }
    }

    void AttackPlayer(int dmg)
    {
        nextAttack = Time.time + attackRate / Mathf.Max(.1f, MegabonkRelicManager.HostileTimeScale);

        if (anim != null)
        {
            anim.SetTrigger("Attack");
        }
        if (dragonBossVisuals != null)
        {
            dragonBossVisuals.OnAttack();
        }

        PlayerHealth3D ph = player.GetComponent<PlayerHealth3D>();
        if (ph != null && !ph.isDead)
        {
            ph.TakeDamage(dmg);
        }
        else if (CoopRun.IsHost && player == CoopRun.Partner) CoopRun.HitPartner(dmg);
    }
}
