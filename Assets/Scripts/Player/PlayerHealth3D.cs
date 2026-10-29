using UnityEngine;

/// <summary>Vida numérica, armadura, evasión, escudo y curación de la expedición.</summary>
public class PlayerHealth3D : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth = 100;
    public float maxShield = 50f, currentShield = 50f;
    public float ShieldPercent => maxShield > 0 ? Mathf.Clamp01(currentShield / maxShield) * 100f : 0;
    public float invulnerabilityDuration = .6f;
    public bool isDead, isBurning;
    // Adaptadores para prefabs y efectos de objetos anteriores: la vida real es numérica.
    public int maxHearts { get => Mathf.CeilToInt(maxHealth / 25f); set => maxHealth = Mathf.Max(1, value * 25); }
    public int currentHearts { get => Mathf.CeilToInt(currentHealth / 25f); set => currentHealth = Mathf.Clamp(value * 25, 0, maxHealth); }
    int baseHealth = 100;
    float lastDamageTime, immuneUntil, burnRemaining, burnTick;
    public float LastDamageTime => lastDamageTime;
    float nextHealVisual;
    int burnDamage;
    void Awake() { if (GetComponent<PlayerShieldAura>() == null) gameObject.AddComponent<PlayerShieldAura>(); }
    public void SetBaseHearts(int hearts) { baseHealth = hearts * 25; maxHealth = currentHealth = baseHealth; }
    public void ResetHealth()
    {
        maxHealth = currentHealth = baseHealth; maxShield = currentShield = 50;
        isDead = isBurning = false; immuneUntil = 0; burnRemaining = burnTick = 0; lastDamageTime = Time.time;
    }
    public void ReviveCoop()
    {
        if (!CoopRun.Active || !isDead) return;
        isDead = false;
        currentHealth = Mathf.Max(1, Mathf.CeilToInt(maxHealth * .35f));
        immuneUntil = Time.time + 2f;
        lastDamageTime = Time.time;
        CombatVisuals.Ring(transform.position + Vector3.up * .2f, 2f, Color.green);
    }
    public void HealHearts(int count) => Heal(count * 25);
    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;
        int actual = Mathf.Min(amount, maxHealth - currentHealth);
        int excess = amount - actual;
        currentHealth += actual;
        if(actual>0 && Time.time>=nextHealVisual)
        {
            nextHealVisual=Time.time+.75f;
            CombatVisuals.Ring(transform.position+Vector3.up*.15f,1.3f,Color.green);
            SpellVFX.Burst(transform.position+Vector3.up,"spark_01",Color.green,.6f);
        }
        if (excess > 0 && MegabonkBuild.Relics("chonkplate") > 0)
        {
            maxShield = Mathf.Min(50 + maxHealth * .75f, maxShield + excess);
            currentShield = Mathf.Min(maxShield, currentShield + excess);
        }
        if (actual > 0) MegabonkRelicManager.Instance?.OnPlayerHealed(transform.position);
    }
    public void IncreaseMaxHearts(int count) => IncreaseMaxHealth(count * 25);
    public void IncreaseMaxHealth(int amount) { if (amount > 0) { maxHealth += amount; ExpeditionProgress.RecordMaxHealth(maxHealth); Heal(amount); } }
    public void ApplyBurn(float duration, int damagePerSecond)
    {
        isBurning = true; burnRemaining = Mathf.Max(burnRemaining, duration); burnDamage = damagePerSecond;
    }
    void Update()
    {
        if (Time.timeScale <= 0 || GameManager.Instance == null || GameManager.Instance.gameState != GameManager.GameState.Playing || isDead) return;
        if (Time.time - lastDamageTime > 6) currentShield = Mathf.Min(maxShield, currentShield + 8 * Time.deltaTime);
        if (isBurning)
        {
            burnRemaining -= Time.deltaTime; burnTick -= Time.deltaTime;
            if (burnTick <= 0) { burnTick = 1; TakeDamage(burnDamage); }
            if (burnRemaining <= 0) isBurning = false;
        }
    }
    public void TakeHeartDamage(int count) => TakeDamage(count * 25);
    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0 || Time.timeScale <= 0 || Time.time < immuneUntil) return;
        var build = MegabonkBuild.Instance;
        if(CharacterPassives.Instance!=null && CharacterPassives.Instance.Block()) {immuneUntil=Time.time+.2f;return;}
        if (build != null && build.TryBlockAegis()) { immuneUntil = Time.time + .2f; return; }
        float dodge = XPSystem.Instance != null ? XPSystem.Instance.dodgeChance : 0;
        dodge+=CharacterPassives.Instance?.Dodge??0;
        if (Random.value < Mathf.Clamp(dodge, 0, .6f))
        {
            CharacterPassives.Instance?.Evaded();
            if (build != null && MegabonkBuild.Relics("phantom_shroud") > 0) { build.phantomReady = true; XPSystem.Instance.GrantTemporarySpeed(2, .25f); }
            immuneUntil = Time.time + .2f; return;
        }
        float armor = build != null ? build.Armor : 0;
        armor += 5 * MegabonkBuild.Relics("spiky_shield");
        int poisonCount = 0;
        if (MegabonkBuild.Relics("gas_mask") > 0)
            foreach (var e in EnemyHealth3D.ActiveEnemies) if (e != null && !e.IsDying && e.IsPoisoned) poisonCount++;
        armor += Mathf.Min(50, poisonCount * MegabonkBuild.Relics("gas_mask"));
        damage = Mathf.Max(1, Mathf.RoundToInt(damage * 100f / (100f + armor)));
        float absorbed = Mathf.Min(currentShield, damage);
        currentShield -= absorbed;
        currentHealth = Mathf.Max(0, currentHealth - Mathf.CeilToInt(damage - absorbed));
        StageDirector.Instance?.RecordPlayerDamaged();
        lastDamageTime = Time.time; immuneUntil = Time.time + invulnerabilityDuration;
        CharacterPassives.Instance?.Damaged();
        if (MegabonkBuild.Relics("mirror") > 0) immuneUntil += .3f;
        if (build != null) build.calciumPenaltyUntil = Time.time + 5f;
        MegabonkRelicManager.Instance?.OnPlayerDamaged(transform.position);
        int thorns = MegabonkBuild.Relics("cactus") * 12 + MegabonkBuild.Relics("spiky_shield") * (8 + Mathf.RoundToInt(armor * 2));
        if(CharacterPassives.Instance?.Hero=="Athena") thorns+=CharacterPassives.Instance.Rank;
        if(thorns>0) CombatVisuals.Ring(transform.position+Vector3.up*.3f,6,new Color(.7f,1,.25f));
        if(MegabonkBuild.Relics("toxic_barrel")>0) SpellVFX.Burst(transform.position,"smoke_01",Color.green,2);
        foreach (var e in EnemyHealth3D.ActiveEnemies)
        {
            if (e == null || e.IsDying || (e.transform.position - transform.position).sqrMagnitude > 36) continue;
            if (thorns > 0)
            {
                e.TakeDamage(thorns, false, null, false, "thorns");
                if (MegabonkBuild.Relics("quins_mask") > 0 && Random.value < .5f) e.TriggerExplosion(3, thorns);
            }
            if (MegabonkBuild.Relics("electric_plug") > 0)
            {
                CombatVisuals.Lightning(transform.position+Vector3.up,e.transform.position+Vector3.up);
                e.TakeDamage(25 * MegabonkBuild.Relics("electric_plug"));
            }
            if (MegabonkBuild.Relics("toxic_barrel") > 0) e.ApplyPoison(4, 6 * MegabonkBuild.Relics("toxic_barrel"));
            if (MegabonkBuild.Relics("mirror") > 0) e.TakeDamage(damage);
        }
        if (currentHealth > 0) return;
        var relic = MegabonkRelicManager.Instance;
        if (relic != null && relic.HasZaWarudo && relic.zaWarudoReady)
        {
            relic.TriggerZaWarudo(); currentHealth = 1; immuneUntil = Time.time + 5; return;
        }
        isDead = true;
        if (!CoopRun.Active) GameManager.Instance?.OnPlayerDeath();
    }
}
