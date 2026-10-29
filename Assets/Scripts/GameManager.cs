using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor principal del juego: puntuación, kills, estados del juego y reinicio limpio hacia el Menú Principal.
/// Singleton accesible vía GameManager.Instance.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState { MainMenu, Playing, Paused, GameOver, Victory }
    public enum CharacterType { CL4NK, Megachad, SirOofie, Calcium, Original }
    public enum PlayStyle { Estandar, FuegoInfernal, RafagaVeloz, TitanBlindado }
    public enum MapType { Forest, Desert, SnowyMountain }
    public enum DifficultyTier { Tier1 = 1, Tier2 = 2, Tier3 = 3 }

    // Singleton
    public static GameManager Instance { get; private set; }

    // Estado público para HUD
    [HideInInspector] public int score;
    [HideInInspector] public int kills;
    [HideInInspector] public GameState gameState = GameState.MainMenu;
    [HideInInspector] public int wavesReached;
    [HideInInspector] public float gameTime;
    public WeaponType selectedWeaponType = WeaponType.Revolver;
    public CharacterType selectedCharacter = CharacterType.CL4NK;
    public string selectedOriginalCharacter = "";
    public PlayStyle selectedPlayStyle = PlayStyle.Estandar;
    public MapType selectedMap = MapType.Forest;
    public DifficultyTier selectedTier = DifficultyTier.Tier1;
    public bool challengeModifier;

    // Meta-progresión: la plata se conserva entre expediciones.
    [HideInInspector] public int silver;
    [HideInInspector] public int gold;
    [HideInInspector] public bool finalSwarmActive;
    [HideInInspector] public float finalSwarmTime;
    float longestSwarmTime;
    private bool runRewardGranted;

    public const float FinalSwarmStartTime = 600f;
    public float TierMultiplier => 1f + ((int)selectedTier - 1) * 0.35f + (challengeModifier ? 0.20f : 0f);
    public float SilverRewardMultiplier => 1f + ((int)selectedTier - 1) * 0.50f + (challengeModifier ? 0.35f : 0f);

    // Estadísticas persistentes y Leaderboards (Megabonk)
    [HideInInspector] public int highestScore = 0;
    [HideInInspector] public int highestKills = 0;
    [HideInInspector] public float bestTimeSurvived = 0f;
    [HideInInspector] public int totalRunsAttempted = 0;
    [HideInInspector] public int bossesSlain = 0;

    // Kill feed (últimas kills para mostrar en HUD)
    [System.Serializable]
    public struct KillEntry
    {
        public string text;
        public float time;
    }
    [HideInInspector] public KillEntry[] killFeed = new KillEntry[5];
    private int killFeedIndex;
    private EnemySpawner3D _cachedSpawner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        EnsureRequiredSystems();
    }

    public static void EnsureRequiredSystems()
    {
        if (Object.FindAnyObjectByType<GameManager>() == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        if (Object.FindAnyObjectByType<GameHUD>() == null)
        {
            GameObject hud = new GameObject("HUD");
            hud.AddComponent<GameHUD>();
        }

        if (Object.FindAnyObjectByType<XPSystem>() == null)
        {
            GameObject xp = new GameObject("XPSystem");
            xp.AddComponent<XPSystem>();
        }

        if (Object.FindAnyObjectByType<SkillMenuUI>() == null)
        {
            GameObject skill = new GameObject("SkillMenuUI");
            skill.AddComponent<SkillMenuUI>();
        }

        if (Object.FindAnyObjectByType<ArenaEnricher>() == null)
        {
            GameObject arena = new GameObject("ArenaEnricher");
            arena.AddComponent<ArenaEnricher>();
        }

        if (Object.FindAnyObjectByType<DamageNumberManager>() == null)
        {
            GameObject dmgNum = new GameObject("DamageNumberManager");
            dmgNum.AddComponent<DamageNumberManager>();
        }

        if (Object.FindAnyObjectByType<MetaProgression>() == null)
        {
            GameObject meta = new GameObject("MetaProgression");
            meta.AddComponent<MetaProgression>();
        }

        if (Object.FindAnyObjectByType<StageDirector>() == null)
        {
            GameObject stages = new GameObject("StageDirector");
            stages.AddComponent<StageDirector>();
        }

        if (Object.FindAnyObjectByType<LanLobby>() == null)
            new GameObject("LanLobby").AddComponent<LanLobby>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        silver = PlayerPrefs.GetInt("MegabonkSilver", 0);
        highestScore = PlayerPrefs.GetInt("Record.Score", 0);
        highestKills = PlayerPrefs.GetInt("Record.Kills", 0);
        bestTimeSurvived = PlayerPrefs.GetFloat("Record.Time", 0);
        totalRunsAttempted = PlayerPrefs.GetInt("Record.Runs", 0);
        bossesSlain = PlayerPrefs.GetInt("Record.Bosses", 0);

        // Auto-crear sistemas requeridos si faltan en la escena
        EnsureRequiredSystems();
    }

    void Start()
    {
        if (gameState == GameState.MainMenu)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void Update()
    {
        if(UiTransition.Busy)return;
        if (gameState == GameState.GameOver || gameState == GameState.Victory)
            return;

        // Pausa y visualización de habilidades con TAB o Escape
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
        {
            if (XPSystem.Instance == null || (!XPSystem.Instance.skillMenuOpen && !XPSystem.Instance.bossTomeMenuOpen && (MegabonkRelicManager.Instance == null || !MegabonkRelicManager.Instance.chestMenuOpen)))
            {
                if (gameState == GameState.Playing)
                    Pause();
                else if (gameState == GameState.Paused)
                {
                    var hud=Object.FindAnyObjectByType<GameHUD>();
                    if(hud==null || !hud.ClosePauseSettings())Resume();
                }
            }
        }

        if (gameState == GameState.Playing)
        {
            gameTime += Time.deltaTime;
            // A los 10 minutos comienza el Final Swarm. La expedición sigue hasta morir;
            // sobrevivir más tiempo sirve para obtener más plata y retos.
            if (!finalSwarmActive && StageDirector.Instance != null && StageDirector.Instance.CurrentStage <= 3 && StageDirector.Instance.StageElapsed >= SourceStageTimeline.StageSeconds(StageDirector.Instance.CurrentStage))
            {
                finalSwarmActive = true;
                finalSwarmTime = 0f;
            }
            if (finalSwarmActive) { finalSwarmTime += Time.deltaTime; longestSwarmTime = Mathf.Max(longestSwarmTime, finalSwarmTime); }
        }

        // Actualizar oleada alcanzada (cacheado)
        if (_cachedSpawner == null) _cachedSpawner = Object.FindAnyObjectByType<EnemySpawner3D>();
        if (_cachedSpawner != null && _cachedSpawner.currentWave > wavesReached)
            wavesReached = _cachedSpawner.currentWave;
    }

    public void AddScore(int points)
    {
        score += points;
    }

    /// <summary>Oro temporal de la expedición: suma puntuación y se muestra durante la partida.</summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        if (CoopRun.IsGuest) return;
        float scaled=amount*(CharacterPassives.Instance?.Gold??1);
        fractionalPassiveGold+=scaled;
        int granted=Mathf.FloorToInt(fractionalPassiveGold); fractionalPassiveGold-=granted;
        gold += granted;
        AddScore(amount);
    }

    float fractionalGold;
    float fractionalPassiveGold;
    public void AddGold(float amount)
    {
        fractionalGold += Mathf.Max(0, amount);
        int whole = Mathf.FloorToInt(fractionalGold);
        fractionalGold -= whole;
        AddGold(whole);
    }

    public bool TrySpendGold(int amount)
    {
        if (CoopRun.IsGuest) return false;
        if (amount < 0 || gold < amount) return false;
        gold -= amount;
        return true;
    }

    /// <summary>Plata persistente obtenida por exploración, vasijas y recompensa final.</summary>
    public void AddSilver(int amount)
    {
        if (amount <= 0) return;
        if (CoopRun.IsGuest && !CoopRun.ReceivingSilver) return;
        CoopRun.ShareSilver(amount);
        silver += amount;
        PlayerPrefs.SetInt("MegabonkSilver", silver);
        PlayerPrefs.Save();
    }

    public void AddKill(int points = 100)
    {
        kills++;

        // Agregar al kill feed
        killFeed[killFeedIndex].text = "Enemigo eliminado +" + points;
        killFeed[killFeedIndex].time = Time.unscaledTime;
        killFeedIndex = (killFeedIndex + 1) % killFeed.Length;
    }

public void OnPlayerDeath()
    {
        if(gameState!=GameState.Playing)return;
        ExpeditionProgress.RecordDeath();
        AwardRunSilver();
        highestScore = Mathf.Max(highestScore, score);
        highestKills = Mathf.Max(highestKills, kills);
        bestTimeSurvived = Mathf.Max(bestTimeSurvived, gameTime);
        SaveRecords();

        gameState = GameState.GameOver;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
    }

    public void TriggerVictory()
    {
        if(gameState!=GameState.Playing)return;
        if (challengeModifier) ExpeditionProgress.RecordChallengeVictory();
        AwardRunSilver();
        highestScore = Mathf.Max(highestScore, score);
        highestKills = Mathf.Max(highestKills, kills);
        bestTimeSurvived = Mathf.Max(bestTimeSurvived, gameTime);
        SaveRecords();

        gameState = GameState.Victory;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
    }

    public void StartGame()
    {
        UiTransition.Begin(this,StartGameImmediate);
    }
    void StartGameImmediate()
    {
        if (CoopRun.Active) Random.InitState(CoopRun.Seed);
        CleanupRunObjects();
        StageDirector.Instance?.ResetStageForNewRun();
        ArenaEnricher arena = Object.FindAnyObjectByType<ArenaEnricher>();
        if (arena != null) arena.ApplyMap();
        totalRunsAttempted++;
        score = 0;
        gold = 0;
        fractionalGold = 0;
        fractionalPassiveGold = 0;
        kills = 0;
        wavesReached = 0;
        gameTime = 0f;
        finalSwarmActive = false;
        finalSwarmTime = 0f;
        runRewardGranted = false;
        longestSwarmTime = 0;

        for (int i = 0; i < killFeed.Length; i++)
        {
            killFeed[i].text = "";
            killFeed[i].time = 0;
        }

        EnemySpawner3D spawner = Object.FindAnyObjectByType<EnemySpawner3D>();
        if (spawner != null) spawner.ResetSpawner();

        XpOrb3D[] orbs = Object.FindObjectsByType<XpOrb3D>();
        foreach (var orb in orbs) if (orb != null) Destroy(orb.gameObject);

        PlayerHealth3D ph = Object.FindAnyObjectByType<PlayerHealth3D>();
        if (ph != null)
        {
            ph.ResetHealth();
            Vector3 playerStart = new Vector3(0f, 1.1f, -15f);
            if (arena != null) playerStart = arena.PlaceOnSnow(playerStart, 1.1f);
            var movement = ph.GetComponent<PlayerMovement3D>();
            if (movement != null) movement.ResetMovement(playerStart);
            else ph.transform.position = playerStart;
        }

        // Equipar arma seleccionada en el menú
        PlayerShooting3D shooting = Object.FindAnyObjectByType<PlayerShooting3D>();
        if (shooting != null)
        {
            shooting.EquipWeapon(selectedWeaponType);
        }

        if (XPSystem.Instance != null) XPSystem.Instance.ResetSkills();
        if (MegabonkRelicManager.Instance == null) new GameObject("MegabonkRelicManager").AddComponent<MegabonkRelicManager>();
        MegabonkRelicManager.Instance.ResetRelics();
        if (ph != null)
        {
            ph.SetBaseHearts(selectedCharacter == CharacterType.Megachad ? 6 : 4);
            ph.transform.localScale = Vector3.one;
            var move = ph.GetComponent<PlayerMovement3D>();
            if (move != null) move.jumpHeight = 1.5f;
            var build = ph.GetComponent<MegabonkBuild>() ?? ph.gameObject.AddComponent<MegabonkBuild>();
            build.BeginRun(selectedCharacter);
            MetaProgression.Instance?.ApplyRunBonuses(XPSystem.Instance, ph);
        }
        var summons = Object.FindAnyObjectByType<PlayerSummons>();
        if (summons != null) summons.ResetSummons();
        Object.FindAnyObjectByType<PlayerCharacterVisual>()?.Refresh();
        SaveRecords();

        gameState = GameState.Playing;
        Time.timeScale = CoopRun.Active ? 0f : 1f;
        Cursor.lockState = CoopRun.Active ? CursorLockMode.None : CursorLockMode.Locked;
        if (StageDirector.Instance != null) StageDirector.Instance.BeginRun();
    }

    public void ReturnToMainMenu()
    {
        LanLobby.Instance?.Stop();
        gameState = GameState.MainMenu;
        CleanupRunObjects();
        if (StageDirector.Instance != null) StageDirector.Instance.ClearStageObjects();
        if (MegabonkRelicManager.Instance != null)
        {
            MegabonkRelicManager.Instance.ResetRelics();
        }
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;

        score = 0;
        gold = 0;
        kills = 0;
        wavesReached = 0;
        gameTime = 0f;
        finalSwarmActive = false;
        finalSwarmTime = 0f;

        for (int i = 0; i < killFeed.Length; i++)
        {
            killFeed[i].text = "";
            killFeed[i].time = 0;
        }

        // 1. Resetear spawner y enemigos
        EnemySpawner3D spawner = Object.FindAnyObjectByType<EnemySpawner3D>();
        if (spawner != null)
        {
            spawner.ResetSpawner();
        }

        // 2. Eliminar esferas de XP residuales
        XpOrb3D[] orbs = Object.FindObjectsByType<XpOrb3D>();
        foreach (var orb in orbs)
        {
            if (orb != null) Destroy(orb.gameObject);
        }

        // 3. Resetear al jugador (vida y posición inicial)
        PlayerHealth3D ph = Object.FindAnyObjectByType<PlayerHealth3D>();
        if (ph != null)
        {
            ph.ResetHealth();
            var movement = ph.GetComponent<PlayerMovement3D>();
            if (movement != null) movement.ResetMovement(new Vector3(0f, 1.1f, -15f));
            else ph.transform.position = new Vector3(0f, 1.1f, -15f);
        }

        // 4. Resetear habilidades de XPSystem
        if (XPSystem.Instance != null)
        {
            XPSystem.Instance.ResetSkills();
        }

        // 5. Resetear invocaciones
        PlayerSummons summons = Object.FindAnyObjectByType<PlayerSummons>();
        if (summons != null)
        {
            summons.ResetSummons();
        }
    }

    public void Pause()
    {
        if (CoopRun.Active) { CoopRun.SetPersonalMenu(!CoopRun.PersonalMenuOpen); return; }
        if(UiTransition.Busy || ShrineRewardUI.IsOpen || MicrowaveUI.IsOpen ||
            (XPSystem.Instance!=null && (XPSystem.Instance.skillMenuOpen || XPSystem.Instance.bossTomeMenuOpen)) ||
            (MegabonkRelicManager.Instance!=null && MegabonkRelicManager.Instance.chestMenuOpen))return;
        if (gameState != GameState.Playing) return;
        gameState = GameState.Paused;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible=true;
    }

    public void Resume()
    {
        if (CoopRun.Active) { CoopRun.SetPersonalMenu(false); return; }
        if(UiTransition.Busy || ShrineRewardUI.IsOpen || MicrowaveUI.IsOpen || (MegabonkRelicManager.Instance!=null && MegabonkRelicManager.Instance.chestMenuOpen))return;
        if (gameState != GameState.Paused) return;
        if (XPSystem.Instance != null && (XPSystem.Instance.skillMenuOpen || XPSystem.Instance.bossTomeMenuOpen)) return;
        gameState = GameState.Playing;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible=false;
    }

    public void RestartGame()
    {
        if (CoopRun.Active) { CoopRun.RequestRestart(); return; }
        StartGame();
    }

    void AwardRunSilver()
    {
        if (runRewardGranted) return;
        runRewardGranted = true;
        ExpeditionProgress.RecordRun(kills, longestSwarmTime);

        // Recompensa clara: bajas, jefes y tiempo extra en el Final Swarm.
        int baseReward = Mathf.FloorToInt(kills * 0.12f) + wavesReached * 20;
        int swarmBonus = Mathf.FloorToInt(longestSwarmTime * 0.5f);
        int earned = Mathf.Max(1, Mathf.RoundToInt((baseReward + swarmBonus) * SilverRewardMultiplier));
        AddSilver(earned);
    }

    void SaveRecords()
    {
        PlayerPrefs.SetInt("Record.Score", highestScore);
        PlayerPrefs.SetInt("Record.Kills", highestKills);
        PlayerPrefs.SetFloat("Record.Time", bestTimeSurvived);
        PlayerPrefs.SetInt("Record.Runs", totalRunsAttempted);
        PlayerPrefs.SetInt("Record.Bosses", bossesSlain);
        PlayerPrefs.Save();
    }

    void CleanupRunObjects()
    {
        ClearRunObjects<MegabonkProjectile>();
        ClearRunObjects<EnemyProjectile3D>();
        ClearRunObjects<BossChest3D>();
        ClearRunObjects<BulletTracer3D>();
        ClearRunObjects<HomingRelicProjectile>();
        ClearRunObjects<FirePuddleBehavior>();
        ClearRunObjects<AfterimageBehavior>();
        DamageNumberManager.Clear();
    }

    static void ClearRunObjects<T>() where T : MonoBehaviour
    {
        foreach (var item in Object.FindObjectsByType<T>())
        {
            item.gameObject.SetActive(false);
            Destroy(item.gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
