using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Experiencia, elecciones de nivel y estadísticas compartidas de armas y tomos.
/// Las propiedades antiguas se mantienen inactivas para compatibilidad de escenas.
/// </summary>
public class XPSystem : MonoBehaviour
{
    public static XPSystem Instance { get; private set; }

    [Header("Configuración de XP")]
    public int xpToNextLevel = 100;
    public float xpScalePerLevel = 1.3f;

    // Estado público para HUD
    [HideInInspector] public int currentXP;
    [HideInInspector] public int maxXP;
    [HideInInspector] public int level = 1;
    [HideInInspector] public bool skillMenuOpen;
    [HideInInspector] public bool bossTomeMenuOpen;
    [HideInInspector] public float xpGainFlash;

    // Colección de mejoras adquiridas (por ID)
    [System.NonSerialized]
    public HashSet<string> acquiredUpgradeIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    [System.NonSerialized] private Dictionary<string, int> tomeLevels = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

    // ── Multiplicadores y estadísticas base ───────────────────
    [HideInInspector] public float damageMultiplier = 1f;
    [HideInInspector] public float fireRateMultiplier = 1f;
    [HideInInspector] public float speedMultiplier = 1f;
    [HideInInspector] public float magnetMultiplier = 1f;
    [HideInInspector] public float reloadSpeedMultiplier = 1f;
    [HideInInspector] public float bulletSpeedMultiplier = 1f;
    [HideInInspector] public float bulletSizeMultiplier = 1f;
    [HideInInspector] public float knockbackMultiplier = 1f;
    [HideInInspector] public float spreadModifierDegrees = 0f;
    [HideInInspector] public int bonusPierce = 0;
    [HideInInspector] public int bonusBounce = 0;
    [HideInInspector] public int bonusMaxAmmo = 0;
    [HideInInspector] public float dodgeChance = 0f;

    // ── Estados y Timers dinámicos ───────────────────────────
    [HideInInspector] public float freshClipTimer = 0f;
    [HideInInspector] public int killClipKills = 0;
    [HideInInspector] public float angerPointTimer = 0f;
    [HideInInspector] public float tailwindTimer = 0f;
    [HideInInspector] public int tailwindStacks = 0;
    [HideInInspector] public float flowTimer = 0f;
    [HideInInspector] public float slipperyTimer = 0f;
    [HideInInspector] public float angelicTimer = 0f;
    [HideInInspector] public float smiteTimer = 0f;
    [HideInInspector] public float chaliceTimer = 0f;
    [HideInInspector] public int continuousShots = 0;

    // ── Contadores acumulativos de sinergias ─────────────────
    [HideInInspector] public int ritualKills = 0;
    [HideInInspector] public float ritualDamageBonus = 0f;
    [HideInInspector] public int holyStrikeKills = 0;
    [HideInInspector] public int bloodMagicKills = 0;
    [HideInInspector] public int shotCounter = 0; // Para rayo cada 2 disparos e Ice Blast cada 3

    // ── Escudo Sagrado (Aegis / Stalwart) ────────────────────
    [HideInInspector] public bool holyShieldActive = false;
    [HideInInspector] public float holyShieldCooldown = 0f;
    [HideInInspector] public float holyShieldBaseCooldown = 120f;

    // ── Supervivencia / Recuperación de Vida (4 Niveles) ─────
    [HideInInspector] public int bloodPactKills = 0;
    [HideInInspector] public bool immortalHeartReady = true;
    [HideInInspector] public float immortalHeartCooldown = 0f;
    [HideInInspector] public float regenTimer = 0f;

    public void TriggerImmortalHeart() { }

    // ── Glare / Círculo de Visión ────────────────────────────
    private float glareTimer = 0f;

    // ── Banderas de conveniencia (retrocompatibilidad) ───────
    public bool powerShot => false;
    public bool bigBullets => false;
    public bool splinter => false;
    public bool reaperRounds => false;

    public bool rapidFire => false;
    public bool lightBullets => false;
    public bool rubberBullets => false;
    public bool siege => false;

    public bool doubleShot => false;
    public bool splitfire => false;
    public bool fanFire => false;
    public bool fusillade => false;

    public bool takeAim => false;
    public bool piercing => false;
    public bool sniper => false;
    public bool assassin => false;

    public bool haste => false;
    public bool blazingSpeed => false;
    public bool runAndGun => false;
    public bool inTheWind => false;

    public bool watchAndLearn => false;
    public bool glare => false;
    public bool intenseGlare => false;
    public bool saccade => false;

    public bool pyroMage => false;
    public bool fireStarter => false;
    public bool intenseBurn => false;
    public bool soothingWarmth => false;

    public bool frostMage => false;
    public bool frostbite => false;
    public bool iceShards => false;
    public bool frostShatter => false;

    public bool electroMage => false;
    public bool energized => false;
    public bool electroBug => false;
    public bool electroMastery => false;
    public bool teslaCoil => false;

    public bool curse => false;
    public bool doom => false;
    public bool wither => false;
    public bool ritual => false;

    public bool holyStrike => false;
    public bool justice => false;
    public bool angelic => false;

    public bool ghostFriend => false;
    public bool energeticFriends => false;
    public bool inSync => false;
    public bool vengefulGhost => false;

    public bool magicLens => false;
    public bool ignitingLens => false;
    public bool refraction => false;
    public bool focalPoint => false;

    public bool dragonEgg => false;
    public bool agedDragon => false;
    public bool trainedDragon => false;
    public bool dragonBond => false;

    public bool magicScythe => false;
    public bool shadowBlade => false;
    public bool windCutter => false;
    public bool scytheMastery => false;
    public bool magicSpear => false;

    // Sinergias
    public bool overload => false;
    public bool frostFire => false;
    public bool iceBlast => false;
    public bool cryoLight => false;
    public bool bloodMagic => false;
    public bool blight => false;

    public bool miniClip => false;
    public bool gunMastery => false;
    public bool deadeye => false;
    public bool chalice => false;
    public bool barrage => false;
    public bool shrapnel => false;

    public bool colossus => false;
    public bool standYourGround => false;
    public bool holyZeal => false;
    public bool smite => false;

    public bool momentum => false;
    public bool afterimage => false;
    public bool zephyr => false;
    public bool battlefield => false;
    public bool deathStare => false;
    public bool soulDrain => false;

    public bool necromancy => false;
    public bool sacrifice => false;
    public bool elderDragon => false;
    public bool prismaticLens => false;
    public bool reaperScythe => false;
    public bool commander => false;

    // Tomos
    public bool tomePower => false;
    public bool tomeSpeed => false;
    public bool tomeGlass => false;
    public bool tomeSummoning => false;
    public bool tomeElements => false;
    public bool tomeRage => false;
    public bool tomeElasticity => false;


    float speedBonus, speedUntil;
    float fractionalXP;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }
    void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; ResetSkills(); }
    void OnDestroy() { if (Instance == this) Instance = null; }
    public bool HasUpgrade(string id) => !string.IsNullOrEmpty(id) && acquiredUpgradeIds.Contains(id);
    public int GetTomeLevel(string id) => MegabonkBuild.Instance != null ? MegabonkBuild.Instance.Level(id) : 0;
    public string GetAbilityTimerText(string id) => UpgradeCatalog.Get(id)?.category == "ARMA" ? "AUTOMÁTICA" : "PASIVA GLOBAL";
    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        if (CoopRun.IsGuest && !applyingSharedXP) return;
        if (CoopRun.IsHost) CoopRun.AwardSharedXP(amount);
        float mult = 1f + .08f * MegabonkBuild.Relics("time_bracelet");
        if (MegabonkBuild.Instance != null) mult *= MegabonkBuild.Instance.XpMultiplier + MegabonkBuild.Instance.Level("mb_cursed") * .05f;
        fractionalXP += amount * mult * .7f;
        int earned = Mathf.FloorToInt(fractionalXP);
        fractionalXP -= earned;
        currentXP += earned;
        xpGainFlash = .5f;
        TryLevelUp();
    }
    bool applyingSharedXP;
    public void AddSharedXP(int amount)
    {
        applyingSharedXP = true;
        try { AddXP(amount); }
        finally { applyingSharedXP = false; }
    }
    void Update() { xpGainFlash = Mathf.Max(0, xpGainFlash - Time.unscaledDeltaTime); TryLevelUp(); }
    void TryLevelUp()
    {
        if (CoopRun.Active && Object.FindAnyObjectByType<PlayerHealth3D>()?.isDead == true) return;
        if (GameManager.Instance == null || GameManager.Instance.gameState != GameManager.GameState.Playing || Time.timeScale <= 0 || skillMenuOpen || bossTomeMenuOpen || maxXP <= 0 || currentXP < maxXP) return;
        currentXP -= maxXP; level++; maxXP = 10 + level * 15;
        ExpeditionProgress.RecordPlayerLevel(level);
        CharacterPassives.Instance?.ApplyLevelBonuses();
        OpenSkillMenu();
    }
    public void ResetSkills()
    {
        acquiredUpgradeIds.Clear(); tomeLevels.Clear();
        level = 1; currentXP = 0; maxXP = 25; xpGainFlash = 0;
        fractionalXP = 0;
        skillMenuOpen = bossTomeMenuOpen = false;
        damageMultiplier = fireRateMultiplier = speedMultiplier = magnetMultiplier = reloadSpeedMultiplier = bulletSpeedMultiplier = bulletSizeMultiplier = knockbackMultiplier = 1;
        dodgeChance = speedBonus = speedUntil = 0;
        bonusPierce = bonusBounce = bonusMaxAmmo = tailwindStacks = 0;
        holyShieldActive = false; immortalHeartReady = false;
        SkillMenuUI.Instance?.ResetMenu();
    }
    public List<UpgradeDefinition> GetLevelUpOptions(int count = 3)
    {
        var options = new List<UpgradeDefinition>();
        var build = MegabonkBuild.Instance;
        foreach (var def in UpgradeCatalog.AllUpgrades)
        {
            if (build == null || !build.CanAdd(def)) continue;
            if (MetaProgression.Instance != null && MetaProgression.Instance.IsDisabled(def.id)) continue;
            int level = build.Level(def.id);
            options.Add(new UpgradeDefinition(def.id, def.name, def.category, def.treeId, level + 1, null, null,
                def.type, def.icon, (level == 0 ? "NUEVO · " : "NIVEL " + (level + 1) + " · ") + def.description, def.color));
        }
        for (int i = options.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); var t = options[i]; options[i] = options[j]; options[j] = t; }
        if (options.Count > count) options.RemoveRange(count, options.Count - count);
        string[] ids = { "repeat_damage", "repeat_speed", "repeat_cadence" };
        string[] names = { "Daño +5%", "Velocidad +5%", "Cadencia +5%" };
        for (int i = 0; options.Count < count && i < ids.Length; i++)
            options.Add(new UpgradeDefinition(ids[i], names[i], "ESTADÍSTICA", ids[i], 1, null, null, UpgradeType.Standard, "+", names[i], Color.cyan));
        return options;
    }

    public List<UpgradeDefinition> GetBossTomeOptions(int count = 3) => GetLevelUpOptions(count);


    public void OpenSkillMenu() { SkillMenuUI.Instance?.ResetMenu(); skillMenuOpen = true; SyncPause(); }
    public void CloseSkillMenu() { skillMenuOpen = false; SyncPause(); }
    public void OpenBossTomeMenu() { bossTomeMenuOpen = true; SyncPause(); }
    public void CloseBossTomeMenu() { bossTomeMenuOpen = false; SyncPause(); }
    void SyncPause()
    {
        bool playing = GameManager.Instance != null && GameManager.Instance.gameState == GameManager.GameState.Playing;
        bool resume = playing && !skillMenuOpen && !bossTomeMenuOpen && (MegabonkRelicManager.Instance == null || !MegabonkRelicManager.Instance.chestMenuOpen);
        Time.timeScale = resume ? 1 : 0; Cursor.lockState = resume ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !resume;
        CoopRun.SyncChoicePause();
    }
    public void ApplyUpgrade(string upgradeId)
    {
        if (upgradeId == "repeat_damage") damageMultiplier += .05f;
        else if (upgradeId == "repeat_speed") speedMultiplier += .05f;
        else if (upgradeId == "repeat_cadence") fireRateMultiplier += .05f;
        else MegabonkBuild.Instance?.Add(upgradeId);
        CloseSkillMenu();
        CloseBossTomeMenu();
    }


    public void GrantTemporarySpeed(float seconds, float bonus) { speedUntil = Time.time + seconds; speedBonus = bonus; }
    public float GetCurrentSpeedMultiplier() => Mathf.Max(.2f, speedMultiplier + (Time.time < speedUntil ? speedBonus : 0));
    public float GetCurrentDamageMultiplier() => Mathf.Max(.1f, damageMultiplier);
    public float GetCurrentFireRateMultiplier() => Mathf.Max(.2f, fireRateMultiplier);
    public float GetCurrentReloadSpeedMultiplier() => 1;
    public int GetCurrentMagazineSize() => 1;
    public int GetActiveSummonCount() => 0;
    public void OnEnemyKill(EnemyHealth3D enemy) { }
    public void OnHeal() { }
    public void OnBulletHit(EnemyHealth3D enemy, ref int damage, float distance) { }
    public void OnDodgeSuccess() { }
    public bool OnPlayerTakeDamage(ref int damage) => true;
    public void OnXpPickup() { }
    public void OnShoot(PlayerShooting3D shooting, ref bool consumeAmmo) { consumeAmmo = false; }
    public void OnReloadStart(PlayerShooting3D shooting) { }
    public void OnReloadFinish(PlayerShooting3D shooting) { }
    public void UpdateVisionFog() { RenderSettings.fog = false; }
    public void EnsureSummonsComponent()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.GetComponent<PlayerSummons>() == null) player.AddComponent<PlayerSummons>();
    }
}
public class FirePuddleBehavior : MonoBehaviour
{
    float nextTick;
    void Update()
    {
        if (Time.timeScale <= 0 || Time.time < nextTick) return;
        nextTick = Time.time + .5f;
        foreach (var enemy in EnemyHealth3D.ActiveEnemies)
            if (enemy != null && !enemy.IsDying && (enemy.transform.position - transform.position).sqrMagnitude < 4) enemy.ApplyBurn(3, 4);
    }
}
public class AfterimageBehavior : MonoBehaviour { }
