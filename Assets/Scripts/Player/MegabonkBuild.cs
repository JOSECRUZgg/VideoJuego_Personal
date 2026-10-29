using System.Collections.Generic;
using UnityEngine;

/// <summary>Inventario de armas automáticas y tomos compartidos de la expedición.</summary>
public partial class MegabonkBuild : MonoBehaviour
{
    public static MegabonkBuild Instance { get; private set; }
    public readonly Dictionary<string, int> Levels = new Dictionary<string, int>();
    readonly Dictionary<string, float> nextAttack = new Dictionary<string, float>();
    public int weaponSlots = 4, tomeSlots = 4;
    public int Quantity => Level("mb_quantity") + Relics("backpack");
    public float XpMultiplier => (1f + Level("mb_xp") * .08f)*(CharacterPassives.Instance?.Xp??1);
    public float CritChance => .05f + Level("mb_precision") * .05f + Relics("forbidden_juice") * .10f+(CharacterPassives.Instance?.Crit??0);
    public float Armor => Level("mb_armor") * 4f+(CharacterPassives.Instance?.Armor??0);
    public int Level(string id) => Levels.TryGetValue(id, out int n) ? n : 0;
    public static int Relics(string id) => MegabonkRelicManager.Instance != null ? MegabonkRelicManager.Instance.GetCount(id) : 0;
    void Awake() { Instance = this; if(GetComponent<BuildModelPresentation>()==null) gameObject.AddComponent<BuildModelPresentation>(); }
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void BeginRun(GameManager.CharacterType hero)
    {
        Levels.Clear(); nextAttack.Clear();
        StopAllCoroutines();
        weaponSlots = 4 + (MetaProgression.Instance != null ? MetaProgression.Instance.weaponSlotRank : 0);
        tomeSlots = 4 + (MetaProgression.Instance != null ? MetaProgression.Instance.tomeSlotRank : 0);
        aegisCharges = 0;
        nextHeal = 0; phantomReady = false; calciumPenaltyUntil = 0;
        Add(StartingWeapon(hero));
        (GetComponent<CharacterPassives>()??gameObject.AddComponent<CharacterPassives>()).BeginRun();
    }
    public static string StartingWeapon(GameManager.CharacterType hero) => hero == GameManager.CharacterType.Original && OriginalContentCatalog.Selected != null
        ? ExtendedWeapons.Resolve(OriginalContentCatalog.Selected.originalWeapon, OriginalContentCatalog.Selected.weaponId) : hero == GameManager.CharacterType.Megachad ? "mb_aura" : hero == GameManager.CharacterType.SirOofie ? "mb_sword" : hero == GameManager.CharacterType.Calcium ? "mb_bone" : "mb_revolver";
    public float calciumPenaltyUntil;

    public bool CanAdd(UpgradeDefinition def)
    {
        if (Level(def.id) > 0) return Level(def.id) < (def.category == "ARMA" ? 10 : 99);
        int count = 0;
        foreach (var pair in Levels) if (UpgradeCatalog.Get(pair.Key)?.category == def.category) count++;
        return count < (def.category == "ARMA" ? weaponSlots : tomeSlots);
    }

    public void Add(string id)
    {
        var def = UpgradeCatalog.Get(id);
        if (def == null || !CanAdd(def)) return;
        Levels[id] = Level(id) + 1;
        ExpeditionProgress.RecordUpgrade(id, Levels[id]);
        XPSystem xp = XPSystem.Instance;
        xp.acquiredUpgradeIds.Add(id);
        switch (id)
        {
            case "mb_damage": xp.damageMultiplier += .08f; break;
            case "mb_cooldown": xp.fireRateMultiplier += .08f; break;
            case "mb_speed": xp.speedMultiplier += .05f; break;
            case "mb_size": xp.bulletSizeMultiplier += .08f; break;
            case "mb_attraction": xp.magnetMultiplier += .12f; break;
            case "mb_luck": if (MegabonkRelicManager.Instance != null) MegabonkRelicManager.Instance.luckPercent += 5f; break;
            case "mb_cursed": if (MegabonkRelicManager.Instance != null) MegabonkRelicManager.Instance.AddTemporaryDifficulty(5f); break;
            case "mb_health": GetComponent<PlayerHealth3D>().IncreaseMaxHealth(10); break;
        }
        if (def.category == "ARMA" && Relics("anvil") > 0) xp.damageMultiplier += .02f * Relics("anvil");
    }

    void Update()
    {
        if (Time.timeScale <= 0 || GameManager.Instance == null || GameManager.Instance.gameState != GameManager.GameState.Playing ||
            GetComponent<PlayerHealth3D>()?.isDead == true || CoopRun.PersonalMenuOpen) return;
        foreach (var def in UpgradeCatalog.AllUpgrades)
        {
            if (def.category != "ARMA" || Level(def.id) == 0) continue;
            if (nextAttack.TryGetValue(def.id, out float time) && time > Time.time) continue;
            float range=40f;
            if(def.id=="mb_sword" || def.id=="mb_katana")range=5*Mathf.Max(.5f,XPSystem.Instance.bulletSizeMultiplier);
            if(def.id=="mb_scythe")range=(4+(1+Quantity+(Level(def.id)-1)/3)*.3f)*Mathf.Max(.5f,XPSystem.Instance.bulletSizeMultiplier);
            if(def.id=="mb_dexecutioner")range=6*Mathf.Max(.5f,XPSystem.Instance.bulletSizeMultiplier);
            if(def.id=="mb_sniper")range=999f;
            if(def.id=="mb_blood")range=50f;
            var target = Nearest(transform.position, range);
            if (target == null && def.id != "mb_aegis" && def.id != "mb_frost" && def.id != "mb_aura") continue;
            var movement = GetComponent<PlayerMovement3D>();
            float extraRate = MegabonkRelicManager.Instance != null ? MegabonkRelicManager.Instance.FireRateBonus(movement != null ? movement.ActualSpeed : 0) : 0;
            nextAttack[def.id] = Time.time + ExtendedWeapons.Cooldown(def.id) / (Mathf.Max(.2f, XPSystem.Instance.fireRateMultiplier + extraRate)*(CharacterPassives.Instance?.Rate??1));
            Attack(def.id, target);
        }
        if (Level("mb_regen") > 0 && Time.time >= nextHeal)
        {
            nextHeal = Time.time + 2f;
            GetComponent<PlayerHealth3D>().Heal(Level("mb_regen"));
        }
    }
    float nextHeal;

    public static EnemyHealth3D Nearest(Vector3 origin, float range, HashSet<EnemyHealth3D> exclude = null)
    {
        EnemyHealth3D result = null;
        float best = range * range;
        foreach (var enemy in EnemyHealth3D.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDying || (exclude != null && exclude.Contains(enemy))) continue;
            float d = (enemy.transform.position - origin).sqrMagnitude;
            if (d < best) { best = d; result = enemy; }
        }
        return result;
    }

    void Attack(string id, EnemyHealth3D target)
    {
        int lv = Level(id);
        XPSystem xp = XPSystem.Instance;
        float size = Mathf.Max(.5f, xp.bulletSizeMultiplier);
        Vector3 origin = transform.position + Vector3.up;
        int count = Mathf.Min(128, 1 + Quantity + (lv - 1) / 3);
        CoopRun.ReportCast(id, target != null ? target.transform.position + Vector3.up : origin + transform.forward * 4f);
        if(target!=null)WeaponCastMotion.Play(transform,id,target.transform.position-transform.position);
        if(id=="mb_fire" && target!=null) ExtractedVisuals.Burst("FireballMuzzle",origin,.35f);
        if (AttackExtended(id, target, lv, size, count)) return;
        if (id == "mb_aura" || id == "mb_sword" || id == "mb_katana")
        {
            SpellModelEffects.Burst(id=="mb_aura" ? "AuraAttack" : id=="mb_sword" ? "SwordProjectilePrefab" : "KatanaProjectilePrefab",origin,id=="mb_aura"?3f:1.7f,.35f);
            float radius = (id == "mb_aura" ? 4f : 5f) * size;
            if(id=="mb_aura") CombatVisuals.Ring(transform.position+Vector3.up*.15f,radius,Color.cyan);
            else CombatVisuals.Slash(origin,target.transform.position-transform.position,radius,id=="mb_katana");
            foreach (var enemy in new List<EnemyHealth3D>(EnemyHealth3D.ActiveEnemies))
                if (enemy != null && !enemy.IsDying && (enemy.transform.position - transform.position).sqrMagnitude <= radius * radius)
                    Hit(enemy, RecoveredWeaponValues.Damage(id,lv,16+lv*4), id);
            return;
        }
        if (id == "mb_lightning")
        {
            var visited = new HashSet<EnemyHealth3D>();
            Vector3 from = origin;
            for (int i = 0; i < count + 2 && target != null; i++)
            {
                visited.Add(target);
                Vector3 end = target.transform.position + Vector3.up;
                CombatVisuals.Lightning(from, end);
                Hit(target, 22 + lv * 4, id);
                from = end; target = Nearest(from, 12f * size, visited);
            }
            return;
        }
        if (id == "mb_revolver") {StartCoroutine(RevolverBurst(target,count+5,lv,size));return;}
        for (int i = 0; i < count; i++)
        {
            Vector3 direction = (target.transform.position + Vector3.up - origin).normalized;
            direction = Quaternion.Euler(0, (i - (count - 1) * .5f) * 4f, 0) * direction;
            MegabonkProjectile.Launch(this, origin, direction, target, id, RecoveredWeaponValues.Damage(id,lv,15+lv*5), size,
                id == "mb_revolver" || id == "mb_bone" ? 1 + lv / 4 : 0, id == "mb_bow" ? 2 + lv / 3 : 0);
        }
    }

    public int Hit(EnemyHealth3D enemy, int raw, string weapon)
    {
        if (enemy == null || enemy.IsDying) return 0;
        var xp = XPSystem.Instance;
        var relic = MegabonkRelicManager.Instance;
        float mult = xp.damageMultiplier * (1f + (relic != null ? relic.DamageBonus : 0f));
        var movement = GetComponent<PlayerMovement3D>();
        if (Relics("brass_knuckles") > 0 && (enemy.transform.position - transform.position).sqrMagnitude < 16) mult *= 1 + .2f * Relics("brass_knuckles");
        if (Relics("scarf") > 0 && movement != null && !movement.isGrounded) mult *= 1 + .5f * Relics("scarf");
        if (Relics("idle_juice") > 0 && relic != null) mult *= 1 + Mathf.Min(1, relic.idleTimer * .2f);
        var ai = enemy.GetComponent<EnemyAI3D>();
        if (Relics("eagle_claw") > 0 && ai != null && ai.enemyType == EnemyAI3D.EnemyType.FlyingShooter) mult *= 1 + .66f * Relics("eagle_claw");
        float chance = CritChance + (weapon == "mb_katana" || weapon == "mb_sniper" ? .25f : 0);
        int tiers = Mathf.FloorToInt(chance) + (Random.value < chance % 1f ? 1 : 0);
        if (tiers > 0) ExpeditionProgress.RecordCriticalHit();
        mult*=CharacterPassives.Instance?.Damage(enemy,tiers>0)??1;
        if (tiers > 0) mult *= tiers > 1 && Relics("giant_fork") > 0 ? Mathf.Min(8, 2 << Mathf.Min(tiers - 1, 2)) : 2f;
        if (Relics("big_bonk") > 0 && Random.value < Mathf.Min(1, .02f * Relics("big_bonk"))) mult *= 20f;
        if (Relics("tactical_goggles") > 0 && enemy.currentHealth >= enemy.maxHealth * .9f) mult *= 1 + .2f * Relics("tactical_goggles");
        if (Relics("boss_buster") > 0 && (enemy.isBoss || enemy.isElite)) mult *= 1 + .15f * Relics("boss_buster");
        if (phantomReady) { mult *= 2f; phantomReady = false; }
        int damage = Mathf.Max(1, Mathf.RoundToInt(raw * mult));
        if (Relics("joes_dagger") > 0 && !enemy.isBoss && Random.value < .01f) { damage = enemy.currentHealth; relic.RecordDaggerExecution(); }
        if (ai != null) ai.ApplyKnockback((enemy.transform.position - transform.position).normalized, 3f * xp.knockbackMultiplier);
        if (weapon == "mb_fire") enemy.ApplyBurn(4f, Mathf.Max(2, damage / 5), weapon);
        if (weapon == "mb_corrupted") enemy.ApplyCurse(1.2f, Mathf.Max(1, damage / 2));
        if (weapon == "mb_dexecutioner" && !enemy.IsDying && !enemy.isBoss && !enemy.isElite && enemy.currentHealth <= enemy.maxHealth*.15f)
            damage=Mathf.Max(damage,enemy.currentHealth);
        int lifeStolen = Relics("chonkplate") > 0 ? Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(damage, enemy.currentHealth) * .2f)) : 0;
        relic?.OnBulletHit(enemy, damage, tiers > 0);
        int healthBefore=Mathf.Max(0,enemy.currentHealth);
        enemy.TakeDamage(damage, true, tiers > 0 ? Color.yellow : Color.white, tiers > 0, weapon);
        if (weapon == "mb_blood") ExpeditionProgress.RecordBloodMagicProc();
        int healthLost=Mathf.Max(0,healthBefore-Mathf.Max(0,enemy.currentHealth));
        CharacterPassives.Instance?.Drain(healthLost);
        if (weapon == "mb_blood") GetComponent<PlayerHealth3D>().Heal(1);
        if (lifeStolen > 0)
        {
            GetComponent<PlayerHealth3D>().Heal(lifeStolen);
            if (Relics("bloody_cleaver") > 0 && !enemy.IsDying) enemy.ApplyCurse(1f, damage / 2);
        }
        return damage;
    }
    public bool phantomReady;
    System.Collections.IEnumerator RevolverBurst(EnemyHealth3D target,int count,int level,float size)
    {
        int stage=StageDirector.Instance!=null?StageDirector.Instance.CurrentStage:0;
        float interval=1f/UnityEngine.Mathf.Max(1,count-1)/Mathf.Max(.2f,XPSystem.Instance.fireRateMultiplier*(CharacterPassives.Instance?.Rate??1));
        for(int i=0;i<count;i++)
        {
            if(GameManager.Instance==null || GameManager.Instance.gameState!=GameManager.GameState.Playing || (StageDirector.Instance!=null && stage!=StageDirector.Instance.CurrentStage)) yield break;
            if(target==null || target.IsDying) target=Nearest(transform.position,45);
            if(target==null) yield break;
            Vector3 origin=transform.position+Vector3.up;
            MegabonkProjectile.Launch(this,origin,(target.transform.position+Vector3.up-origin).normalized,target,"mb_revolver",RecoveredWeaponValues.Damage("mb_revolver",level,5),size,(level-1)/4,0);
            ExtractedVisuals.Burst("RevolverMuzzle",origin,.2f);
            if(i<count-1) yield return new WaitForSeconds(interval);
        }
    }
}

public class MegabonkProjectile : MonoBehaviour
{
    MegabonkBuild owner;
    EnemyHealth3D target;
    string weapon;
    Vector3 direction;
    int damage, bounces, pierces;
    float age, radius;
    bool returning;
    bool diceSix;
    readonly HashSet<EnemyHealth3D> hit = new HashSet<EnemyHealth3D>();
    public static void Launch(MegabonkBuild owner, Vector3 position, Vector3 direction, EnemyHealth3D target, string weapon, int damage, float size, int bounces, int pierces)
    {
        GameObject go = new GameObject();
        go.name = weapon; go.transform.position = position;
        go.transform.rotation = Quaternion.LookRotation(direction);
        var p = go.AddComponent<MegabonkProjectile>();
        p.owner = owner; p.direction = direction; p.target = target; p.weapon = weapon; p.damage = damage;
        p.radius = .22f * size; p.bounces = bounces; p.pierces = pierces;
        if (weapon == "mb_dice") { int roll=Random.Range(1,7); p.damage*=roll; p.diceSix=roll==6; }
        // An incomplete extracted VFX must not prevent the damaging projectile from spawning.
        try { CombatVisuals.Projectile(go.transform,weapon,size,ExtendedWeapons.Get(weapon)!=null ? ExtendedWeapons.Tint(weapon) : weapon=="mb_fire"?new Color(1,.35f,.05f):Color.yellow); }
        catch(System.Exception error)
        {
            Debug.LogException(error);
            SpellVFX.Fire(go.transform,4f,.5f);
        }
    }
    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.gameState == GameManager.GameState.MainMenu ||
            GameManager.Instance.gameState == GameManager.GameState.GameOver || GameManager.Instance.gameState == GameManager.GameState.Victory) { Destroy(gameObject); return; }
        if (Time.timeScale <= 0) return;
        float lifetime = weapon == "mb_dexecutioner" ? .5f : weapon == "mb_sniper" ? 12f : 4f;
        if (owner == null || (age += Time.deltaTime) > lifetime) { Destroy(gameObject); return; }
        if (weapon == "mb_banana")
        {
            if (!returning && age >= .65f) { returning = true; hit.Clear(); }
            if (returning)
            {
                Vector3 toOwner = owner.transform.position + Vector3.up - transform.position;
                if (toOwner.magnitude < Mathf.Max(.7f, 28f * Mathf.Max(.5f, XPSystem.Instance.bulletSpeedMultiplier) * Time.deltaTime))
                { Destroy(gameObject); return; }
                direction = toOwner.normalized;
            }
        }
        if ((weapon == "mb_rocket" || weapon == "mb_dagger" || weapon == "mb_blood") && target != null && !target.IsDying) direction = (target.transform.position + Vector3.up - transform.position).normalized;
        if(direction.sqrMagnitude>.001f) transform.rotation=Quaternion.LookRotation(direction);
        if ((weapon == "mb_banana" || weapon == "mb_bone" || weapon == "mb_axe" || weapon == "mb_dice" || weapon == "mb_blackhole") && transform.childCount > 0)
            transform.GetChild(0).Rotate(Vector3.up, 720f * Time.deltaTime, Space.Self);
        float step = (weapon == "mb_sniper" ? 90f : weapon == "mb_blackhole" ? 20f : 28f) *
            Mathf.Max(.5f, XPSystem.Instance.bulletSpeedMultiplier) * Time.deltaTime;
        var hits = Physics.SphereCastAll(transform.position, radius, direction, step, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
        foreach (var contact in hits)
        {
            if (contact.collider.GetComponentInParent<PlayerHealth3D>() != null) continue;
            var pot = contact.collider.GetComponentInParent<StageInteractable>();
            if (pot != null) { if (pot.kind == StageInteractable.Kind.Pot) pot.BreakPot(); continue; }
            var enemy = contact.collider.GetComponentInParent<EnemyHealth3D>();
            if (enemy == null) { if (!contact.collider.isTrigger) { ImpactArea(contact.point); Destroy(gameObject); return; } continue; }
            if (enemy.IsDying || !hit.Add(enemy)) continue;
            if (weapon == "mb_blackhole") { ImpactArea(contact.point); Destroy(gameObject); return; }
            int actualDamage = owner.Hit(enemy, damage, weapon);
            ImpactArea(contact.point);
            if (weapon == "mb_fire")
            {
                ExtractedVisuals.Burst("FireballExplosion",contact.point,.8f);
                SpellVFX.Burst(contact.point, "flame_01", new Color(1f, .38f, .04f), 1.1f);
                CombatVisuals.Ring(contact.point, .8f, new Color(1f, .42f, .06f));
            }
            else if (weapon == "mb_bone")
                SpellVFX.Burst(contact.point, "spark_01", new Color(1f, .92f, .68f), .6f);
            else
                ExtractedVisuals.Burst(weapon == "mb_rocket" ? "RocketExplosion" : "BulletHit", contact.point, 1.2f);
            if (weapon == "mb_rocket" || weapon == "mb_fire") enemy.TriggerExplosion(3f * Mathf.Max(.5f, XPSystem.Instance.bulletSizeMultiplier), actualDamage / 2);
            if (weapon == "mb_banana") continue;
            if (bounces-- > 0)
            {
                var next = MegabonkBuild.Nearest(contact.point, 16f, hit);
                if (next != null) { target = next; transform.position = contact.point; direction = (next.transform.position + Vector3.up - contact.point).normalized; return; }
            }
            if (pierces-- <= 0) { Destroy(gameObject); return; }
        }
        transform.position += direction * step;
    }
    void ImpactArea(Vector3 position)
    {
        float size = Mathf.Max(.5f, XPSystem.Instance.bulletSizeMultiplier);
        if (weapon == "mb_poison") WeaponArea.Spawn(owner, weapon, position, Vector3.zero, 3*size, 3f, damage);
        if (weapon == "mb_blackhole") WeaponArea.Spawn(owner, weapon, position, Vector3.zero,
            3*size, Mathf.Min(7f,2f+.5f*(owner.Level(weapon)-1)), damage);
        if (weapon == "mb_dice" && diceSix)
        {
            CombatVisuals.Ring(position, 3*size, Color.yellow);
            foreach(var enemy in new List<EnemyHealth3D>(EnemyHealth3D.ActiveEnemies))
                if(enemy!=null && !enemy.IsDying && !hit.Contains(enemy) && Vector3.Distance(enemy.transform.position,position)<3*size)
                    owner.Hit(enemy, damage, weapon);
        }
    }
}
