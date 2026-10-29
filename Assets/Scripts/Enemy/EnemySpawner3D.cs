using UnityEngine;

/// <summary>
/// Sistema de horda continua (modo supervivencia / horde survival):
/// Los enemigos (esqueletos y variantes) aparecen continuamente sin pausas entre oleadas.
/// La densidad de la horda y la agresividad escalan con el tiempo transcurrido.
/// Cada 60 segundos de supervivencia se genera un temible JEFE (Dragon Boss).
/// </summary>
public class EnemySpawner3D : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject enemyPrefab;
    public GameObject flyingEnemyPrefab;
    public GameObject bossPrefab;

    [Header("Configuración de spawn")]
    public float spawnRadius = 36f;
    public float spawnRadiusMax = 52f;
    public float timeBetweenSpawns = 0.35f;

    // Estado público para HUD
    [HideInInspector] public int currentWave = 1;
    [HideInInspector] public int enemiesRemaining;
    [HideInInspector] public bool isResting = false;
    [HideInInspector] public float restTimeLeft = 0f;
    [HideInInspector] public EnemyHealth3D currentBoss;

    private float survivalTimer;
    private float nextSpawnTime;
    private float nextBossMinute;
    private Transform player;
    private int enemiesToSpawn = 0;
    private int enemiesSpawnedThisWave = 0;
    private float nextHordeRefresh;
    private bool triedRuntimeLibrary;
    private int nextTimelineEvent;
    private float swarmUntil;

    void Start()
    {
        isResting = false;
        restTimeLeft = 0f;
        currentWave = 1;
        survivalTimer = 0f;
        nextBossMinute = 60f;
        EnsurePrefabsLoaded();
    }

    void EnsurePrefabsLoaded()
    {
        if (!triedRuntimeLibrary)
        {
            triedRuntimeLibrary = true;
            var library = Resources.Load<ForestAssetLibrary>("Forest/ForestAssetLibrary");
            if (library != null && library.prefabs != null)
                foreach (var prefab in library.prefabs)
                {
                    if (prefab == null) continue;
                    if (enemyPrefab == null && (prefab.name == "Enemy" || prefab.name == "SkeletonEnemy")) enemyPrefab = prefab;
                    if (flyingEnemyPrefab == null && prefab.name == "FlyingShooterEnemy") flyingEnemyPrefab = prefab;
                    if (bossPrefab == null && prefab.name == "DragonBoss") bossPrefab = prefab;
                }
        }
#if UNITY_EDITOR
        if (enemyPrefab == null)
        {
            enemyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
            if (enemyPrefab == null)
                enemyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SkeletonEnemy.prefab");
        }

        if (flyingEnemyPrefab == null)
        {
            flyingEnemyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FlyingShooterEnemy.prefab");
            if (flyingEnemyPrefab == null)
            {
                flyingEnemyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FourEvilDragonsPBR/Prefab/DragonSoulEater/Purple.prefab");
                if (flyingEnemyPrefab == null)
                    flyingEnemyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FourEvilDragonsPBR/Prefab/DragonSoulEater/Blue.prefab");
            }
        }

        if (bossPrefab == null)
        {
            bossPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DragonBoss.prefab");
        }
#endif
    }

    public void ResetSpawner()
    {
        currentWave = 1;
        survivalTimer = 0f;
        nextBossMinute = 60f;
        isResting = false;
        restTimeLeft = 0f;
        currentBoss = null;
        nextSpawnTime = Time.time;
        enemiesSpawnedThisWave = 0;
        nextTimelineEvent = 0;
        swarmUntil = 0f;
        nextHordeRefresh = 0f;

        // Limpiar enemigos residuales
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject e in enemies)
        {
            if (e != null)
            {
                e.SetActive(false);
                Destroy(e);
            }
        }
    }

    void Update()
    {
        if (Time.timeScale <= 0f || (GameManager.Instance != null && GameManager.Instance.gameState != GameManager.GameState.Playing)) return;

        if (CoopRun.IsHost)
        {
            GameObject own = GameObject.FindGameObjectWithTag("Player");
            Transform partner = CoopRun.PartnerDowned ? null : CoopRun.Partner;
            bool ownAlive = own != null && own.GetComponent<PlayerHealth3D>()?.isDead != true;
            if (partner != null && (!ownAlive || Time.frameCount / 90 % 2 == 0)) player = partner;
            else if (ownAlive) player = own.transform;
        }

        // Buscar jugador
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            return;
        }

        PlayerHealth3D ph = player.GetComponent<PlayerHealth3D>();
        if (ph != null && ph.isDead) return;

        EnsurePrefabsLoaded();

        survivalTimer += Time.deltaTime;
        currentWave = Mathf.FloorToInt(survivalTimer / 60f) + 1;
        var director = StageDirector.Instance;
        if (director != null && director.CurrentStage <= 3)
        {
            bool desert = GameManager.Instance != null && GameManager.Instance.selectedMap == GameManager.MapType.Desert;
            var events = SourceStageTimeline.Events(director.CurrentStage, desert);
            while (nextTimelineEvent < events.Length && director.StageElapsed >= events[nextTimelineEvent].start)
            {
                var stageEvent = events[nextTimelineEvent++];
                if (stageEvent.miniboss) SpawnEnemy(EnemyAI3D.EnemyType.Elite);
                else swarmUntil = Mathf.Max(swarmUntil, stageEvent.start + stageEvent.duration);
            }
        }

        // Comprobar estado de boss activo
        if (currentBoss != null && currentBoss.IsDying)
        {
            currentBoss = null;
        }
        if (currentBoss == null && StageDirector.Instance != null && StageDirector.Instance.BossGateActive)
        {
            foreach (var candidate in EnemyHealth3D.ActiveEnemies)
            {
                if (candidate == null || candidate.IsDying || !candidate.isBoss) continue;
                currentBoss = candidate;
                break;
            }
        }

        // Spawn periódico de jefe cada minuto
        if (StageDirector.Instance == null && survivalTimer >= nextBossMinute && currentBoss == null)
        {
            nextBossMinute += 60f;
            SpawnBoss();
        }

        enemiesRemaining = EnemyHealth3D.AliveCount;
        if (Time.time >= nextHordeRefresh && (director == null || director.CurrentStage <= 3))
        {
            nextHordeRefresh = Time.time + 2f;
            int recycled = 0;
            foreach (var enemy in EnemyHealth3D.ActiveEnemies)
            {
                if (enemy == null || enemy.isBoss || enemy.IsDying ||
                    (enemy.transform.position - player.position).sqrMagnitude < 130f * 130f) continue;
                var ai = enemy.GetComponent<EnemyAI3D>();
                if (ai != null && ai.enemyType == EnemyAI3D.EnemyType.Boss) continue;
                Vector3 p = GetForwardSpawnPosition(spawnRadius, spawnRadiusMax, 0);
                float offset = ai != null && ai.enemyType == EnemyAI3D.EnemyType.FlyingShooter ? 4.5f : .1f;
                if (ArenaEnricher.Instance != null && !ArenaEnricher.Instance.TryFindWalkablePosition(p, out p, offset, 60f)) continue;
                enemy.transform.position = p;
                if (++recycled >= 6) break;
            }
        }

        // Capacidad dinámica de la horda: aumenta con el tiempo de supervivencia
        float difficulty = MegabonkRelicManager.Instance != null ? MegabonkRelicManager.Instance.DifficultyMultiplier : 1f;
        float tierMultiplier = GameManager.Instance != null ? GameManager.Instance.TierMultiplier : 1f;
        bool finalSwarm = GameManager.Instance != null && GameManager.Instance.finalSwarmActive;
        bool scheduledSwarm = director != null && director.StageElapsed < swarmUntil;
        float swarmMultiplier = finalSwarm ? 2.4f : scheduledSwarm ? 1.5f : 1f;
        int targetCapacity = Mathf.Clamp(Mathf.RoundToInt((20 + Mathf.FloorToInt(survivalTimer / 12f) * 3) * Mathf.Lerp(1f, difficulty, 0.45f) * tierMultiplier * swarmMultiplier), 20, finalSwarm ? 220 : 120);
        float currentInterval = Mathf.Clamp(timeBetweenSpawns - (survivalTimer / 360f) * 0.18f, 0.15f, 0.45f);
        currentInterval /= Mathf.Lerp(1f, difficulty * tierMultiplier * swarmMultiplier, 0.20f);

        // Generación continua sin pausas
        if ((director == null || director.CurrentStage <= 3) &&
            (enemiesRemaining < targetCapacity || enemiesSpawnedThisWave < enemiesToSpawn) && Time.time >= nextSpawnTime)
        {
            nextSpawnTime = Time.time + currentInterval;
            SpawnEnemy();
            enemiesSpawnedThisWave++;
            enemiesRemaining = EnemyHealth3D.AliveCount;

            // Ocasionalmente spawnear una pequeña patrulla de 2 enemigos adicionales
            if (Random.value < 0.25f && enemiesRemaining + 2 < targetCapacity)
            {
                SpawnEnemy();
                enemiesSpawnedThisWave++;
                enemiesRemaining = EnemyHealth3D.AliveCount;
            }
        }
    }

    public EnemyHealth3D SpawnStageBoss(int stage = 0, bool finalBoss = false)
    {
        GameObject prefabToUse = bossPrefab;
        if (prefabToUse == null)
        {
            prefabToUse = Resources.Load<GameObject>("Prefabs/DragonBoss");
#if UNITY_EDITOR
            if (prefabToUse == null)
            {
                prefabToUse = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DragonBoss.prefab");
            }
#endif
        }
        if (prefabToUse == null) prefabToUse = enemyPrefab;
        if (prefabToUse == null || player == null) return null;

        // Jefe aparece imponente en el arco frontal
        Vector3 pos = GetForwardSpawnPosition(spawnRadius + 4f, spawnRadius + 8f, 0.1f);
        if (ArenaEnricher.Instance != null &&
            !ArenaEnricher.Instance.TryFindWalkablePosition(pos, out pos, 0.1f, 28f))
        {
            if (!finalBoss) return null;
            // The final portal must not soft-lock a run when the generated edge
            // has no walkable point. Fall back near the player on the arena floor.
            Vector3 fallback = player.position + player.forward * 12f;
            if (!ArenaEnricher.Instance.TryFindWalkablePosition(fallback, out pos, 0.1f, 20f))
                pos = ArenaEnricher.Instance.PlaceOnGround(fallback, 0.1f);
        }

        GameObject boss = Instantiate(prefabToUse, pos, Quaternion.identity);
        boss.name = finalBoss ? "BOSS_GhostKing" : stage > 0 ? "BOSS_Stage_" + stage : "BOSS_Dragon_Min_" + currentWave;

        EnemyAI3D ai = boss.GetComponent<EnemyAI3D>();
        if (ai != null)
        {
            ai.enemyType = EnemyAI3D.EnemyType.Boss;
            ai.ApplyTypeStats();
        }

        bool sourceVisual = ExtractedEnemyMeshVisual.Apply(boss, EnemyAI3D.EnemyType.Boss,
            survivalTimer, Mathf.Max(1, stage), finalBoss ? "GhostKingData" : null);
        if (finalBoss && !sourceVisual)
            Debug.LogWarning("Ghost King visual was not available; using the boss prefab so the run can still finish.");

        // Luz amenazante sobre el boss
        GameObject bossLight = new GameObject("BossAura");
        bossLight.transform.SetParent(boss.transform);
        bossLight.transform.localPosition = new Vector3(0f, finalBoss ? 6.6f : 3.5f, 0f);
        Light bl = bossLight.AddComponent<Light>();
        bl.type = LightType.Point;
        bl.color = new Color(1f, 0.15f, 0.15f);
        bl.range = 16f;
        bl.intensity = 4.0f;

        currentBoss = boss.GetComponent<EnemyHealth3D>();
        return currentBoss;
    }

    void SpawnBoss()
    {
        SpawnStageBoss();
    }

    /// <summary>
    /// Genera una posición de spawn en un arco frontal amplio (250°),
    /// abarcando casi todo el círculo frontal y lateral, pero excluyendo 100% la espalda del jugador.
    /// </summary>
    Vector3 GetForwardSpawnPosition(float minRadius, float maxRadius, float heightY)
    {
        if (player == null) return Vector3.zero;

        // Dirección frontal en la que mira el jugador (cámara / orientación)
        Vector3 fwd = player.forward;
        if (Camera.main != null)
        {
            Vector3 camFwd = Camera.main.transform.forward;
            camFwd.y = 0f;
            if (camFwd.sqrMagnitude > 0.01f) fwd = camFwd.normalized;
        }
        else
        {
            fwd.y = 0f;
            fwd.Normalize();
        }

        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;

        // Ángulo de -125° a +125°: Cobertura de 250° (frente, diagonales y laterales).
        // El sector de 110° directamente atrás del jugador queda completamente protegido.
        float angleDeg = Random.Range(-125f, 125f);
        Vector3 spawnDir = Quaternion.Euler(0f, angleDeg, 0f) * fwd;

        float dist = Random.Range(minRadius, maxRadius);
        Vector3 pos = player.position + spawnDir * dist;
        pos.y = heightY;
        return pos;
    }

    void SpawnEnemy(EnemyAI3D.EnemyType? forcedType = null)
    {
        if (enemyPrefab == null || player == null) return;

        EnemyAI3D.EnemyType type = forcedType ?? GetEnemyType();
        GameObject prefabToSpawn = enemyPrefab;
        Vector3 pos;

        if (type == EnemyAI3D.EnemyType.FlyingShooter)
        {
            if (flyingEnemyPrefab != null)
                prefabToSpawn = flyingEnemyPrefab;

            pos = GetForwardSpawnPosition(spawnRadius, spawnRadiusMax, player.position.y + 4.5f);
        }
        else
        {
            pos = GetForwardSpawnPosition(spawnRadius, spawnRadiusMax, 0f); // Plantado al suelo
        }
        if (ArenaEnricher.Instance != null)
        {
            float offset = type == EnemyAI3D.EnemyType.FlyingShooter ? 4.5f : 0.1f;
            if (!ArenaEnricher.Instance.TryFindWalkablePosition(pos, out pos, offset, 60f)) return;
        }

        GameObject enemy = Instantiate(prefabToSpawn, pos, Quaternion.identity);
        enemy.tag = "Enemy";

        // Asegurar colisionador para registrar disparos
        Collider col = enemy.GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider cc = enemy.AddComponent<CapsuleCollider>();
            cc.radius = 1.0f;
            cc.height = 2.0f;
            cc.center = new Vector3(0f, 1f, 0f);
        }

        // Asegurar componente de salud
        EnemyHealth3D hp = enemy.GetComponent<EnemyHealth3D>();
        if (hp == null)
        {
            hp = enemy.AddComponent<EnemyHealth3D>();
            hp.maxHealth = (type == EnemyAI3D.EnemyType.FlyingShooter) ? 14 : 10;
        }

        EnemyAI3D ai = enemy.GetComponent<EnemyAI3D>();
        if (ai == null)
        {
            ai = enemy.AddComponent<EnemyAI3D>();
        }
        ai.enemyType = type;
        ai.ApplyTypeStats();
        int stage = StageDirector.Instance != null ? StageDirector.Instance.CurrentStage : 1;
        if (!ExtractedEnemyMeshVisual.Apply(enemy, type, survivalTimer, stage))
            MonsterPackVisual.Apply(enemy, type, survivalTimer);
    }

    EnemyAI3D.EnemyType GetEnemyType()
    {
        float roll = Random.value;

        // Introducción escalonada: primero esqueletos originales animados,
        // luego orcos y después se desbloquean las variantes del pack.
        if (survivalTimer < 90f)
            return EnemyAI3D.EnemyType.Lurker;
        if (survivalTimer < 150f)
        {
            if (roll < 0.72f) return EnemyAI3D.EnemyType.Lurker;
            if (roll < 0.92f) return EnemyAI3D.EnemyType.Sploder;
            return EnemyAI3D.EnemyType.Charger;
        }
        else if (survivalTimer < 240f)
        {
            if (roll < 0.42f) return EnemyAI3D.EnemyType.Lurker;
            if (roll < 0.65f) return EnemyAI3D.EnemyType.Sploder;
            if (roll < 0.84f) return EnemyAI3D.EnemyType.Charger;
            return EnemyAI3D.EnemyType.Summoner;
        }
        else
        {
            if (roll < 0.24f) return EnemyAI3D.EnemyType.Lurker;
            if (roll < 0.40f) return EnemyAI3D.EnemyType.Sploder;
            if (roll < 0.56f) return EnemyAI3D.EnemyType.FlyingShooter;
            if (roll < 0.72f) return EnemyAI3D.EnemyType.Charger;
            if (roll < 0.88f) return EnemyAI3D.EnemyType.Summoner;
            return EnemyAI3D.EnemyType.Elite;
        }
    }
}
