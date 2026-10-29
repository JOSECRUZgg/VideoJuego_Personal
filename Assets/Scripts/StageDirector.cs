using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dirige las tres etapas de una expedición: exploración, jefe y portal.
/// Los objetos se crean en tiempo de ejecución para que cada mapa tenga un ciclo completo.
/// </summary>
public class StageDirector : MonoBehaviour
{
    public static StageDirector Instance { get; private set; }
    public int CurrentStage { get; private set; } = 1;
    public bool BossGateActive { get; private set; }
    public bool PortalActive { get; private set; }
    public float StageElapsed { get; private set; }
    public bool FinalEncounterActive { get; private set; }

    private readonly List<EnemyHealth3D> stageBosses = new List<EnemyHealth3D>();
    private readonly List<GameObject> runObjects = new List<GameObject>();
    private float stageStartTime;
    private int bossCursesThisStage;
    private bool firstBossFightDamaged;
    public bool IsTransitioning { get; private set; }
    float transitionProgress;
    float transitionTimeScale = 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void BeginRun()
    {
        ClearStageObjects();
        CurrentStage = 1;
        stageStartTime = GameManager.Instance != null ? GameManager.Instance.gameTime : 0f;
        StageElapsed = 0f;
        BossGateActive = false;
        PortalActive = false;
        FinalEncounterActive = false;
        stageBosses.Clear();
        bossCursesThisStage = 0;
        firstBossFightDamaged = false;
        SpawnStageContents();
    }

    public void ResetStageForNewRun() { CurrentStage = 1; }

    public void SyncRemoteStage(int stage, float elapsed, bool gate, bool portal, bool final)
    {
        if (!CoopRun.IsGuest || stage < 1 || stage > 4) return;
        if (stage != CurrentStage)
        {
            ClearStageObjects();
            CurrentStage = stage;
            Random.InitState(CoopRun.Seed + stage * 7919);
            ArenaEnricher.Instance?.ApplyMap();
            var player = FindPlayer();
            if (player != null && ArenaEnricher.Instance != null &&
                ArenaEnricher.Instance.TryFindWalkablePosition(Vector3.zero, out Vector3 spawn, .2f, 80f))
                player.GetComponent<PlayerMovement3D>()?.ResetMovement(spawn);
        }
        StageElapsed = Mathf.Max(0, elapsed);
        BossGateActive = gate;
        PortalActive = portal;
        FinalEncounterActive = final;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.gameState != GameManager.GameState.Playing) return;
        StageElapsed = gm.gameTime - stageStartTime;

        // El jugador decide cuándo activar el arco; el reloj no invoca al jefe.

        if (BossGateActive && !PortalActive)
        {
            stageBosses.RemoveAll(boss => boss == null || boss.IsDying);
            if (stageBosses.Count == 0)
            {
                if (FinalEncounterActive)
                {
                    FinalEncounterActive = false;
                    ExpeditionProgress.RecordFinalBossVictory();
                    if (GameManager.Instance != null) GameManager.Instance.TriggerVictory();
                }
                else
                {
                    if (CurrentStage == 1 && !firstBossFightDamaged)
                        ExpeditionProgress.RecordFirstBossPerfect();
                    SpawnPortal();
                }
            }
        }
    }

    void SpawnStageContents()
    {
        Transform player = FindPlayer();
        Vector3 center = player != null ? player.position : Vector3.zero;

        // 1. Sector Central (Santuario Principal)
        SpawnInteractable(StageInteractable.Kind.ChargeShrine, center + new Vector3(18f, 0f, 10f));
        SpawnInteractable(StageInteractable.Kind.GreedShrine, center + new Vector3(-20f, 0f, 14f));
        SpawnInteractable(StageInteractable.Kind.HealingShrine, center + new Vector3(12f, 0f, -22f));
        SpawnChest(center + new Vector3(-14f, 0f, -14f), 30);
        SpawnChest(center + new Vector3(22f, 0f, -6f), 45);
        SpawnPotsCircle(center, 8, 14f, 26f);
        if (GameManager.Instance != null && GameManager.Instance.selectedMap == GameManager.MapType.Forest)
        {
            for (int sector = 0; sector < 8; sector++)
            {
                float angle = sector * Mathf.PI / 4f;
                Vector3 camp = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 240f;
                SpawnInteractable(sector % 2 == 0 ? StageInteractable.Kind.ChargeShrine : StageInteractable.Kind.GreedShrine, camp);
                SpawnChest(camp + new Vector3(6, 0, 4), 100);
                SpawnPotsCircle(camp, 5, 4f, 10f);
            }
        }

        // 2. Sector Norte: Bastión Antiguo / Outpost de Vigilancia (0, 0, 110)
        Vector3 northOutpost = new Vector3(0f, 0f, 110f);
        SpawnInteractable(StageInteractable.Kind.BossGate, new Vector3(0f, 0f, 75f));
        SpawnInteractable(StageInteractable.Kind.ChargeShrine, northOutpost + new Vector3(12f, 0f, 8f));
        SpawnInteractable(StageInteractable.Kind.BossCurseShrine, northOutpost + new Vector3(-14f, 0f, 10f));
        SpawnInteractable(StageInteractable.Kind.WindShrine, northOutpost + new Vector3(0f, 0f, -14f));
        SpawnChest(northOutpost + new Vector3(-8f, 0f, -8f), 60);
        SpawnChest(northOutpost + new Vector3(14f, 0f, -10f), 75);
        SpawnChest(northOutpost + new Vector3(0f, 0f, 18f), 90);
        SpawnPotsCircle(northOutpost, 8, 8f, 22f);

        // 3. Sector Sur: Coliseo Hundido (0, 0, -110)
        Vector3 southColosseum = new Vector3(0f, 0f, -110f);
        SpawnInteractable(StageInteractable.Kind.ChargeShrine, southColosseum + new Vector3(-10f, 0f, -8f));
        SpawnInteractable(StageInteractable.Kind.GreedShrine, southColosseum + new Vector3(14f, 0f, -12f));
        SpawnInteractable(StageInteractable.Kind.BossCurseShrine, southColosseum + new Vector3(0f, 0f, 16f));
        SpawnChest(southColosseum + new Vector3(-12f, 0f, 10f), 65);
        SpawnChest(southColosseum + new Vector3(12f, 0f, 10f), 80);
        SpawnChest(southColosseum + new Vector3(0f, 0f, -18f), 95);
        SpawnPotsCircle(southColosseum, 8, 8f, 22f);

        // 4. Sector Este: Círculo de Menhires Místicos (115, 0, 0)
        Vector3 eastMonoliths = new Vector3(115f, 0f, 0f);
        SpawnInteractable(StageInteractable.Kind.ChargeShrine, eastMonoliths + new Vector3(8f, 0f, 12f));
        SpawnInteractable(StageInteractable.Kind.HealingShrine, eastMonoliths + new Vector3(-10f, 0f, -10f));
        SpawnInteractable(StageInteractable.Kind.GreedShrine, eastMonoliths + new Vector3(12f, 0f, -8f));
        SpawnChest(eastMonoliths + new Vector3(-12f, 0f, 8f), 70);
        SpawnChest(eastMonoliths + new Vector3(14f, 0f, 10f), 85);
        SpawnChest(eastMonoliths + new Vector3(0f, 0f, -16f), 100);
        SpawnPotsCircle(eastMonoliths, 8, 8f, 22f);

        // 5. Sector Oeste: Necrópolis y Forja Fortificada (-115, 0, 0)
        Vector3 westFort = new Vector3(-115f, 0f, 0f);
        SpawnInteractable(StageInteractable.Kind.ChargeShrine, westFort + new Vector3(-12f, 0f, 10f));
        SpawnInteractable(StageInteractable.Kind.Microwave, westFort + new Vector3(8f, 0f, 12f));
        SpawnInteractable(StageInteractable.Kind.WindShrine, westFort + new Vector3(-6f, 0f, -14f));
        SpawnChest(westFort + new Vector3(12f, 0f, -8f), 70);
        SpawnChest(westFort + new Vector3(-14f, 0f, -10f), 85);
        SpawnChest(westFort + new Vector3(0f, 0f, 16f), 110);
        SpawnPotsCircle(westFort, 8, 8f, 22f);

        // 6. Dunas Salvajes y Rutas Intermedias (radios 45m a 160m)
        SpawnInteractable(StageInteractable.Kind.ChargeShrine, new Vector3(65f, 0f, 65f));
        SpawnInteractable(StageInteractable.Kind.GreedShrine, new Vector3(-65f, 0f, 65f));
        SpawnInteractable(StageInteractable.Kind.BossCurseShrine, new Vector3(70f, 0f, -70f));
        SpawnInteractable(StageInteractable.Kind.HealingShrine, new Vector3(-70f, 0f, -70f));
        SpawnInteractable(StageInteractable.Kind.WindShrine, new Vector3(85f, 0f, 50f));
        SpawnInteractable(StageInteractable.Kind.Microwave, new Vector3(-85f, 0f, -50f));

        // Cofres secretos en las dunas
        SpawnChest(new Vector3(80f, 0f, -40f), 90);
        SpawnChest(new Vector3(-80f, 0f, 40f), 90);
        SpawnChest(new Vector3(50f, 0f, 85f), 105);
        SpawnChest(new Vector3(-50f, 0f, -85f), 120);

        // Vasijas salvajes a lo largo de las dunas
        for (int i = 0; i < 15; i++)
        {
            float ang = i * (360f / 15f) * Mathf.Deg2Rad;
            float r = Random.Range(45f, 155f);
            Vector3 pos = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
            SpawnInteractable(StageInteractable.Kind.Pot, pos);
        }
    }

    void SpawnChest(Vector3 position, int baseCost)
    {
        position.y = 0f;
        if (ArenaEnricher.Instance != null && !ArenaEnricher.Instance.TryFindWalkablePosition(position, out position)) return;
        BossChest3D chest = BossChest3D.Spawn(position, false);
        chest.goldCost = baseCost + (CurrentStage - 1) * 30;
        runObjects.Add(chest.gameObject);
    }

    void SpawnPotsCircle(Vector3 center, int count, float minR, float maxR)
    {
        for (int i = 0; i < count; i++)
        {
            float ang = (i * (360f / count) + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
            float dist = Random.Range(minR, maxR);
            Vector3 pos = center + new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist);
            SpawnInteractable(StageInteractable.Kind.Pot, pos);
        }
    }

    void SpawnPortal()
    {
        Transform player = FindPlayer();
        if (player == null) return;
        PortalActive = true;
        SpawnInteractable(StageInteractable.Kind.Portal, player.position + new Vector3(0f, 0f, 18f));
    }

    void SpawnInteractable(StageInteractable.Kind kind, Vector3 position)
    {
        position.y = 0f;
        if (ArenaEnricher.Instance != null && !ArenaEnricher.Instance.TryFindWalkablePosition(position, out position)) return;
        GameObject obj = new GameObject(kind.ToString());
        obj.transform.position = position;
        obj.AddComponent<StageInteractable>().Initialize(kind, this);
        runObjects.Add(obj);
    }

    public bool ActivateBossGate()
    {
        if (BossGateActive || PortalActive) return false;
        EnemySpawner3D spawner = Object.FindAnyObjectByType<EnemySpawner3D>();
        if (spawner == null) return false;
        EnemyHealth3D boss = spawner.SpawnStageBoss(CurrentStage);
        if (boss == null) return false;
        BossGateActive = true;
        stageBosses.Add(boss);
        return true;
    }

    public bool ActivateBossCurse()
    {
        EnemySpawner3D spawner = Object.FindAnyObjectByType<EnemySpawner3D>();
        if (spawner == null) return false;
        if (!BossGateActive) ActivateBossGate();
        if (!BossGateActive) return false;
        EnemyHealth3D cursedBoss = spawner.SpawnStageBoss(CurrentStage);
        if (cursedBoss == null) return false;
        stageBosses.Add(cursedBoss);
        bossCursesThisStage++; // El segundo jefe deja su propio cofre al caer.
        return true;
    }

    public void AdvanceStage()
    {
        if (!PortalActive || IsTransitioning) return;
        if (bossCursesThisStage >= 2) ExpeditionProgress.RecordDoubleCurseStage();
        bool anyChargeShrine = false, allChargeShrinesComplete = true;
        foreach (GameObject obj in runObjects)
        {
            if (obj == null) continue;
            StageInteractable interactable = obj.GetComponent<StageInteractable>();
            if (interactable == null || interactable.kind != StageInteractable.Kind.ChargeShrine) continue;
            anyChargeShrine = true;
            if (!interactable.completed) allChargeShrinesComplete = false;
        }
        if (anyChargeShrine && allChargeShrinesComplete) ExpeditionProgress.RecordAllChargeShrines();
        Transform traveler = FindPlayer();
        if (traveler != null) ExtractedVisuals.Sound("PortalPass", traveler.position);
        StartCoroutine(LoadNextStage());
    }

    System.Collections.IEnumerator LoadNextStage()
    {
        IsTransitioning = true;
        transitionTimeScale = Time.timeScale;
        Time.timeScale = 0;
        transitionProgress = .1f;
        try
        {
            yield return null;
            CompleteStageChange();
            transitionProgress = 1;
            yield return null;
        }
        finally
        {
            if (GameManager.Instance == null || GameManager.Instance.gameState == GameManager.GameState.Playing)
                Time.timeScale = transitionTimeScale;
            IsTransitioning = false;
        }
    }

    void CompleteStageChange()
    {
        PortalActive = false;
        ClearStageObjects();
        if (CurrentStage >= 4) return;
        CurrentStage++;
        transitionProgress = .3f;
        var spawner = Object.FindAnyObjectByType<EnemySpawner3D>();
        if (spawner != null) spawner.ResetSpawner();
        foreach (var shot in Object.FindObjectsByType<EnemyProjectile3D>()) Destroy(shot.gameObject);
        foreach (var shot in Object.FindObjectsByType<MegabonkProjectile>()) Destroy(shot.gameObject);
        foreach (var orb in Object.FindObjectsByType<XpOrb3D>()) Destroy(orb.gameObject);
        foreach (var chest in Object.FindObjectsByType<BossChest3D>()) Destroy(chest.gameObject);
        foreach (var zone in Object.FindObjectsByType<HealingZone>()) Destroy(zone.gameObject);
        if (CoopRun.Active) Random.InitState(CoopRun.Seed + CurrentStage * 7919);
        if (ArenaEnricher.Instance != null) ArenaEnricher.Instance.ApplyMap();
        Transform player = FindPlayer();
        if (player != null && ArenaEnricher.Instance != null &&
            ArenaEnricher.Instance.TryFindWalkablePosition(Vector3.zero, out Vector3 spawn, .2f, 80f))
        {
            var controller = player.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (wasEnabled) controller.enabled = false;
            player.position = spawn;
            if (wasEnabled) controller.enabled = true;
        }
        if (GameManager.Instance != null) { GameManager.Instance.finalSwarmActive = false; GameManager.Instance.finalSwarmTime = 0; }
        stageStartTime = GameManager.Instance != null ? GameManager.Instance.gameTime : 0f;
        StageElapsed = 0f;
        BossGateActive = false;
        stageBosses.Clear();
        bossCursesThisStage = 0;
        firstBossFightDamaged = false;
        if (CurrentStage == 4) StartFinalEncounter(spawner);
        else SpawnStageContents();
    }

    public void RecordPlayerDamaged()
    {
        if (CurrentStage == 1 && BossGateActive) firstBossFightDamaged = true;
    }
    void StartFinalEncounter(EnemySpawner3D spawner)
    {
        // The exported FinalBossMap controller has no executable body. Keep the
        // existing walkable arena, but use the extracted Ghost King mesh and data.
        FinalEncounterActive = true;
        BossGateActive = true;
        Transform player = FindPlayer();
        Vector3 shelterCenter = player != null ? player.position : Vector3.zero;
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * .5f;
            Vector3 shelter = shelterCenter + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 16f;
            SpawnInteractable(StageInteractable.Kind.ChargeShrine, shelter);
        }
        EnemyHealth3D ghostKing = spawner != null ? spawner.SpawnStageBoss(4, true) : null;
        if (ghostKing != null) stageBosses.Add(ghostKing);
        else
        {
            FinalEncounterActive = false;
            BossGateActive = false;
            Debug.LogError("Ghost King source model could not be loaded; final encounter was not started.");
        }
    }

    public bool IsInsideFinalShelter(Vector3 position)
    {
        if (!FinalEncounterActive) return false;
        bool hasShelter = false;
        foreach (GameObject obj in runObjects)
        {
            if (obj == null) continue;
            StageInteractable interactable = obj.GetComponent<StageInteractable>();
            if (interactable == null || interactable.kind != StageInteractable.Kind.ChargeShrine) continue;
            hasShelter = true;
            Vector3 delta = position - obj.transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 4.2f * 4.2f) return true;
        }
        return !hasShelter;
    }

    void OnGUI()
    {
        if (!IsTransitioning) return;
        int oldDepth = GUI.depth;
        Color oldColor = GUI.color;
        GUI.depth = -10000;
        GUI.color = new Color(.04f, .07f, .06f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        // LoadingScreen.unity in the extracted project uses the same framed
        // Megabonk window and logo; reuse those art assets without replacing
        // the current scene transition logic.
        var logo = Resources.Load<Texture2D>("ExtractedVisuals/Texture2D/MegabonkLogo");
        GUI.color = Color.white;
        MegabonkUI.MetalPanel(new Rect(Screen.width * .18f, Screen.height * .24f, Screen.width * .64f, Screen.height * .48f));
        if (logo != null)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(Screen.width * .30f, Screen.height * .29f, Screen.width * .40f, Screen.height * .14f), logo, ScaleMode.ScaleToFit, true);
        }
        GUI.color = Color.white;
        GUI.Label(new Rect(0, Screen.height * .42f, Screen.width, 60), "CARGANDO ETAPA…", MegabonkUI.Style(32, true, TextAnchor.MiddleCenter));
        GUI.color = new Color(.25f, .8f, .45f);
        GUI.DrawTexture(new Rect(Screen.width * .25f, Screen.height * .58f, Screen.width * .5f * transitionProgress, 18), Texture2D.whiteTexture);
        GUI.color = oldColor;
        GUI.depth = oldDepth;
    }

    public void ApplyGreed()
    {
        if (MegabonkRelicManager.Instance != null)
            MegabonkRelicManager.Instance.AddTemporaryDifficulty(5f);
        if (GameManager.Instance != null) GameManager.Instance.AddGold(35);
    }

    public void ApplyHealingShrine(Vector3 shrinePos)
    {
        PlayerHealth3D playerHealth = Object.FindAnyObjectByType<PlayerHealth3D>();
        if (playerHealth != null) playerHealth.HealHearts(2);
        CreateHealingZone(shrinePos, 25f, 5.5f);
        MagicVFX.SpawnImpact(shrinePos + Vector3.up * 1.5f, 2.5f, new Color(0.2f, 1f, 0.45f));
    }

    public void ApplyWindShrine(Vector3 shrinePos)
    {
        Transform player = FindPlayer();
        Vector3 center = player != null ? player.position : shrinePos;

        // Ráfaga octogonal de torbellinos cortantes en 8 direcciones
        for (int i = 0; i < 8; i++)
        {
            float ang = i * 45f * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            MagicVFX.LaunchWhirlwind(center + Vector3.up * 0.6f + dir * 1.2f, dir, speed: 16f, duration: 3.5f, damage: 35, scale: 2.4f, tint: new Color(0.35f, 0.95f, 1f));
        }

        if (XPSystem.Instance != null)
        {
            XPSystem.Instance.GrantTemporarySpeed(20f, 0.40f);
            XPSystem.Instance.tailwindStacks = 4;
            MagicVFX.SpawnWhirlwind(center, duration: 2.0f, scale: 3.2f, tint: new Color(0.3f, 1f, 0.9f));
        }
    }

    public void GrantChargeBlessing(bool legendary)
    {
        ShrineRewardUI rewards = GetComponent<ShrineRewardUI>();
        if (rewards == null) rewards = gameObject.AddComponent<ShrineRewardUI>();
        rewards.Open(legendary);

        PlayerHealth3D playerHealth = Object.FindAnyObjectByType<PlayerHealth3D>();
        if (playerHealth != null) playerHealth.HealHearts(legendary ? 2 : 1);
        CreateHealingZone(null, 16f, 4f);
    }

    void CreateHealingZone(Vector3? customPos = null, float duration = 16f, float radius = 4f)
    {
        Transform player = FindPlayer();
        Vector3 position = customPos ?? (player != null ? player.position : Vector3.zero);
        position.y = 0.06f;
        if (ArenaEnricher.Instance != null) position = ArenaEnricher.Instance.PlaceOnSnow(position, 0.06f);
        GameObject zone = new GameObject("HealingZone");
        zone.transform.position = position;
        HealingZone hz = zone.AddComponent<HealingZone>();
        hz.Setup(duration, radius);
        runObjects.Add(zone);
    }

    public void DuplicateRelic()
    {
        MegabonkRelicManager relics = MegabonkRelicManager.Instance;
        if (relics == null) return;
        relics.TryMicrowaveTransmute();
    }

    public void ClearStageObjects()
    {
        ShrineRewardUI rewards = GetComponent<ShrineRewardUI>();
        if (rewards != null) rewards.Close();
        foreach (GameObject obj in runObjects)
            if (obj != null) Destroy(obj);
        runObjects.Clear();
    }

    Transform FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform : null;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}
