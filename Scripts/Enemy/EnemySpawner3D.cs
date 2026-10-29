using UnityEngine;

/// <summary>
/// Sistema de oleadas (waves) que genera enemigos con dificultad progresiva.
/// Cada 3 oleadas (Oleada 3, 6, 9...) genera un temible JEFE (Boss) gigante.
/// </summary>
public class EnemySpawner3D : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject enemyPrefab;

    [Header("Configuración de spawn")]
    public float spawnRadius = 22f;
    public float spawnRadiusMax = 32f;
    public float timeBetweenSpawns = 0.45f;

    [Header("Descanso entre oleadas")]
    public float restDuration = 8f;

    // Estado público para HUD
    [HideInInspector] public int currentWave = 0;
    [HideInInspector] public int enemiesRemaining;
    [HideInInspector] public bool isResting = true;
    [HideInInspector] public float restTimeLeft;
    [HideInInspector] public EnemyHealth3D currentBoss;

    private int enemiesToSpawn;
    private int enemiesSpawnedThisWave;
    private int totalEnemiesThisWave;
    private float nextSpawnTime;
    private Transform player;
    private bool bossSpawnedThisWave;

    void Start()
    {
        restTimeLeft = 4f;
        isResting = true;
    }

    public void ResetSpawner()
    {
        currentWave = 0;
        enemiesRemaining = 0;
        enemiesToSpawn = 0;
        enemiesSpawnedThisWave = 0;
        isResting = true;
        restTimeLeft = 4f;
        currentBoss = null;
        bossSpawnedThisWave = false;

        // Limpiar enemigos residuales
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject e in enemies)
        {
            if (e != null) Destroy(e);
        }
    }

    void Update()
    {
        // Buscar jugador
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            return;
        }

        // Verificar si el jugador está muerto
        PlayerHealth3D ph = player.GetComponent<PlayerHealth3D>();
        if (ph != null && ph.isDead) return;

        // Comprobar estado de boss activo
        if (currentBoss != null && currentBoss.IsDying)
        {
            currentBoss = null;
        }

        if (isResting)
        {
            restTimeLeft -= Time.deltaTime;
            if (restTimeLeft <= 0f)
                StartNextWave();
            return;
        }

        // Contar enemigos vivos (más eficiente con conteo)
        enemiesRemaining = CountEnemiesAlive();

        // Spawn de jefe si toca y aún no se ha generado en esta oleada
        if (!bossSpawnedThisWave && currentWave > 0 && currentWave % 3 == 0)
        {
            bossSpawnedThisWave = true;
            SpawnBoss();
        }

        // Spawn de enemigos comunes restantes
        if (enemiesToSpawn > 0 && Time.time >= nextSpawnTime)
        {
            nextSpawnTime = Time.time + timeBetweenSpawns;
            SpawnEnemy();
            enemiesToSpawn--;
            enemiesSpawnedThisWave++;
        }

        // Oleada completada cuando todos los enemigos y el boss mueren
        if (enemiesRemaining == 0 && enemiesToSpawn == 0 && enemiesSpawnedThisWave > 0 && currentBoss == null)
        {
            isResting = true;
            restTimeLeft = restDuration;
        }
    }

    void StartNextWave()
    {
        currentWave++;
        isResting = false;
        bossSpawnedThisWave = false;
        totalEnemiesThisWave = 5 + currentWave * 3;
        enemiesToSpawn = totalEnemiesThisWave;
        enemiesSpawnedThisWave = 0;
    }

    void SpawnBoss()
    {
        if (enemyPrefab == null || player == null) return;

        Vector2 circle = Random.insideUnitCircle.normalized * (spawnRadius + 4f);
        Vector3 pos = new Vector3(player.position.x + circle.x, 1.5f, player.position.z + circle.y);

        GameObject boss = Instantiate(enemyPrefab, pos, Quaternion.identity);
        boss.name = "BOSS_Goliath_Wave_" + currentWave;

        EnemyAI3D ai = boss.GetComponent<EnemyAI3D>();
        if (ai != null)
        {
            ai.enemyType = EnemyAI3D.EnemyType.Boss;
            ai.ApplyTypeStats();
        }

        // Estilo visual del Boss: Carmesí oscuro imponente con luz de advertencia
        Renderer rend = boss.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material bossMat = new Material(litShader) { color = new Color(0.9f, 0.05f, 0.1f) };
            if (bossMat.HasProperty("_EmissionColor"))
            {
                bossMat.EnableKeyword("_EMISSION");
                bossMat.SetColor("_EmissionColor", new Color(1f, 0.15f, 0.05f) * 2.5f);
            }
            rend.material = bossMat;
        }

        // Luz amenazante sobre el boss
        GameObject bossLight = new GameObject("BossAura");
        bossLight.transform.SetParent(boss.transform);
        bossLight.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        Light bl = bossLight.AddComponent<Light>();
        bl.type = LightType.Point;
        bl.color = new Color(1f, 0.1f, 0.1f);
        bl.range = 14f;
        bl.intensity = 3.5f;

        if (boss.GetComponent<EnemyVisuals>() == null)
            boss.AddComponent<EnemyVisuals>();

        currentBoss = boss.GetComponent<EnemyHealth3D>();
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null || player == null) return;

        float radius = Random.Range(spawnRadius, spawnRadiusMax);
        Vector2 circle = Random.insideUnitCircle.normalized * radius;
        Vector3 pos = new Vector3(
            player.position.x + circle.x,
            1f,
            player.position.z + circle.y
        );

        GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);

        EnemyAI3D.EnemyType type = GetEnemyType();
        EnemyAI3D ai = enemy.GetComponent<EnemyAI3D>();
        if (ai != null)
        {
            ai.enemyType = type;
            ai.ApplyTypeStats();
        }

        Renderer rend = enemy.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            switch (type)
            {
                case EnemyAI3D.EnemyType.Lurker:
                    rend.material.color = new Color(1f, 0.2f, 0.2f); // Rojo
                    break;
                case EnemyAI3D.EnemyType.Sploder:
                    rend.material.color = new Color(1f, 0.45f, 0.1f); // Naranja
                    break;
                case EnemyAI3D.EnemyType.Charger:
                    rend.material.color = new Color(1f, 0.85f, 0.15f); // Amarillo
                    break;
                case EnemyAI3D.EnemyType.Summoner:
                    rend.material.color = new Color(0.6f, 0.15f, 0.9f); // Morado
                    break;
                case EnemyAI3D.EnemyType.Elite:
                    rend.material.color = new Color(0.85f, 0.05f, 0.2f); // Carmesí grande
                    break;
            }
        }

        if (enemy.GetComponent<EnemyVisuals>() == null)
            enemy.AddComponent<EnemyVisuals>();
    }

    EnemyAI3D.EnemyType GetEnemyType()
    {
        float roll = Random.value;

        if (currentWave <= 2)
        {
            return roll < 0.75f ? EnemyAI3D.EnemyType.Lurker : EnemyAI3D.EnemyType.Sploder;
        }
        else if (currentWave <= 5)
        {
            if (roll < 0.45f) return EnemyAI3D.EnemyType.Lurker;
            if (roll < 0.70f) return EnemyAI3D.EnemyType.Sploder;
            if (roll < 0.88f) return EnemyAI3D.EnemyType.Charger;
            return EnemyAI3D.EnemyType.Summoner;
        }
        else
        {
            if (roll < 0.35f) return EnemyAI3D.EnemyType.Lurker;
            if (roll < 0.55f) return EnemyAI3D.EnemyType.Sploder;
            if (roll < 0.75f) return EnemyAI3D.EnemyType.Charger;
            if (roll < 0.90f) return EnemyAI3D.EnemyType.Summoner;
            return EnemyAI3D.EnemyType.Elite;
        }
    }

    int CountEnemiesAlive()
    {
        // Más eficiente que FindGameObjectsWithTag en cada frame
        // cuando hay muchos enemigos activos
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int alive = 0;
        foreach (var e in enemies)
        {
            if (e == null) continue;
            EnemyHealth3D eh = e.GetComponent<EnemyHealth3D>();
            if (eh == null || !eh.IsDying) alive++;
        }
        return alive;
    }
}
