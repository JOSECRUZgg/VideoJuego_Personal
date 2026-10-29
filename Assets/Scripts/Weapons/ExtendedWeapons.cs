using UnityEngine;

// Original damage/cooldown values come from Weapon*.asset. Missing C# behavior is reconstructed.
public static class ExtendedWeapons
{
    public sealed class Definition
    {
        public string id, original, name, description, model;
        public float damage, cooldown;
        public string Icon => id=="mb_poison" ? "Poison Flask" : id=="mb_dagger" ? "Bluetooth Knife" : id=="mb_frost" ? "Frostwalker" : original.Substring(6);
        public Definition(string id, string original, string name, string description, string model, float damage, float cooldown)
        { this.id=id; this.original="Weapon"+original; this.name=name; this.description=description; this.model=model; this.damage=damage; this.cooldown=cooldown; }
    }
    public static readonly Definition[] All = {
        new Definition("mb_poison", "PoisonFlask", "Frasco venenoso", "El frasco deja una nube que envenena. Tamaño, daño y cantidad mejoran el ataque.", "PoisonFlaskProjectile", 1, .8f),
        new Definition("mb_dexecutioner", "Dexecutioner", "Dexecutioner", "Corte frontal que ejecuta enemigos normales por debajo del 15% de vida; los jefes reciben daño normal.", "DexecutionerProjectilePrefab", 9, .8f),
        new Definition("mb_tornado", "Tornado", "Tornado", "Remolinos móviles que dañan repetidamente a los enemigos a su paso.", "TornadoProjectile", 8, .8f),
        new Definition("mb_sniper", "Sniper", "Francotirador", "Disparo automático de largo alcance, perforante y con +25% de crítico.", "SniperProjectile", 22, 1),
        new Definition("mb_corrupted", "CorruptedSword", "Espada corrupta", "Ondas de espada perforantes que aplican una maldición de daño retardado.", "CorruptedSwordProjectile", 12, .8f),
        new Definition("mb_dice", "Dice", "Dados", "El resultado de 1 a 6 multiplica el daño; un seis explota en área.", "DiceProjectile", 4, .5f),
        new Definition("mb_aegis", "Aegis", "Égida", "Recarga bloqueos de daño. Cada bloqueo libera una onda de choque cercana.", "AegisAttack", 10, 1.5f),
        new Definition("mb_frost", "FrostWalker", "Paso helado", "Deja zonas de hielo que dañan y congelan a los enemigos.", "IceRing", 9, 1.25f),
        new Definition("mb_axe", "Axe", "Hachas", "Dos hachas giratorias perforantes por ataque, más cantidad al mejorar.", "AxeProjectile", 6, 1.4f),
        new Definition("mb_scythe", "Scythe", "Guadaña", "Barrido circular que golpea a todos los enemigos cercanos.", "ScytheAttackProjectile", 11, .85f),
        new Definition("mb_blackhole", "BlackHole", "Agujero negro", "Crea un vórtice que atrae enemigos normales y causa daño periódico.", "BlackHoleProjectile", 4.5f, 1),
        new Definition("mb_dagger", "WirelessDagger", "Daga inalámbrica", "Dagas teledirigidas que saltan entre varios objetivos.", "BluetoothProjectile", 9, .5f),
        new Definition("mb_blood", "BloodMagic", "Magia de sangre", "Proyectiles de sangre que recuperan un punto de vida al impactar.", "BloodStaffProjectile", 9, 1.5f)
    };
    public static Definition Get(string id) => System.Array.Find(All, w => w.id == id);
    public static string Resolve(string original, string fallback)
    { var w = System.Array.Find(All, d => d.original == original); return w != null ? w.id : fallback; }
    public static float Cooldown(string id) => Get(id)?.cooldown ?? RecoveredWeaponValues.Interval(id,1.3f);
    public static Color Tint(string id) => id == "mb_poison" ? new Color(.3f,1,.1f) : id == "mb_frost" ? Color.cyan :
        id == "mb_blood" ? new Color(1,.08f,.15f) : id == "mb_blackhole" || id == "mb_corrupted" ? new Color(.65f,.2f,1) : new Color(1,.8f,.25f);
}

public partial class MegabonkBuild
{
    int aegisCharges;
    public bool TryBlockAegis()
    {
        if (Level("mb_aegis") == 0 || aegisCharges <= 0) return false;
        aegisCharges--;
        ExtractedVisuals.Sound("AegisUse",transform.position);
        float radius = 4 * Mathf.Max(.5f, XPSystem.Instance.bulletSizeMultiplier);
        SpellModelEffects.Burst("AegisAttack", transform.position + Vector3.up, radius * 2f, .6f);
        CombatVisuals.Ring(transform.position + Vector3.up, radius, Color.cyan);
        foreach (var enemy in new System.Collections.Generic.List<EnemyHealth3D>(EnemyHealth3D.ActiveEnemies))
            if (enemy != null && !enemy.IsDying && Vector3.Distance(enemy.transform.position, transform.position) <= radius)
                Hit(enemy, 10 + Level("mb_aegis") * 3, "mb_aegis");
        return true;
    }
    bool AttackExtended(string id, EnemyHealth3D target, int level, float size, int count)
    {
        var def = ExtendedWeapons.Get(id);
        if (def == null) return false;
        int damage = Mathf.Max(1, Mathf.RoundToInt(def.damage * (1 + .25f * (level - 1))));
        Vector3 origin = transform.position + Vector3.up;
        if(id != "mb_aegis") ExtractedVisuals.Sound(id=="mb_dice" ? "DiceThrow" : id=="mb_axe" ? "AxeThrow" : id=="mb_scythe" ? "Scythe1" : def.original.Substring(6), origin);
        if (id == "mb_aegis")
        {
            int capacity = 2 + Quantity + (level - 1) / 3;
            if (aegisCharges < capacity) { aegisCharges++; SpellModelEffects.Burst("AegisAttack",origin,2f*size,.45f); CombatVisuals.Ring(origin, 1.5f * size, Color.cyan); ExtractedVisuals.Sound("AegisRecharge",origin); }
            return true;
        }
        if (id == "mb_frost")
        {
            WeaponArea.Spawn(this, id, transform.position, Vector3.zero, (2 + count * .2f) * size, 3f, damage);
            return true;
        }
        if (target == null) return true;
        if (id == "mb_sniper") ExtractedVisuals.Burst("SniperMuzzle",origin,.35f);
        else if (id == "mb_dagger") ExtractedVisuals.Burst("BluetoothMuzzle",origin,.35f);
        Vector3 direction = (target.transform.position + Vector3.up - origin).normalized;
        if (id == "mb_dexecutioner")
        {
            // The exported attack prefab contains no renderers; its visible part is
            // DexecutionerProjectilePrefab. Release it forward, as the source asset specifies.
            for(int i=0;i<count;i++)
            {
                Vector3 aim=Quaternion.Euler(0,(i-(count-1)*.5f)*8,0)*direction;
                MegabonkProjectile.Launch(this,origin+aim*.6f,aim,target,id,damage,size,0,2+level/3);
            }
            return true;
        }
        if (id == "mb_scythe")
        {
            float radius = (4 + count * .3f) * size;
            CombatVisuals.Slash(origin, direction, radius, true);
            WeaponArea.Visual(id, origin, radius * .3f, .5f);
            foreach (var enemy in new System.Collections.Generic.List<EnemyHealth3D>(EnemyHealth3D.ActiveEnemies))
            {
                if (enemy == null || enemy.IsDying) continue;
                Vector3 delta = enemy.transform.position - transform.position;
                if (delta.sqrMagnitude > radius * radius) continue;
                Hit(enemy, damage, id);
            }
            return true;
        }
        count = Mathf.Min(32, count + (id == "mb_axe" ? 1 : 0));
        for (int i=0; i<count; i++)
        {
            var aim = Quaternion.Euler(0, (i-(count-1)*.5f)*7, 0) * direction;
            // shortcut: source attack scripts are empty; refine travel and impact if their executable logic becomes available.
            if (id == "mb_tornado")
            {
                WeaponArea.Spawn(this,id,origin,aim*20f,3*size,1.2f,damage);
            }
            else if (id == "mb_blackhole")
                MegabonkProjectile.Launch(this,origin,aim,target,id,damage,size,0,0);
            else MegabonkProjectile.Launch(this, origin, aim, target, id, damage, size,
                id == "mb_dagger" ? 2 + level / 3 : 0,
                id == "mb_sniper" ? 5 + level : id == "mb_corrupted" || id == "mb_axe" ? 2 + level / 3 : 0);
        }
        return true;
    }
}

// Finite, world-space damage zones. A stage/run change disposes them; pause freezes their lifetime.
public class WeaponArea : MonoBehaviour
{
    MegabonkBuild owner;
    string weapon;
    Vector3 velocity;
    float radius, remaining, tick;
    int damage, stage;
    Transform readableVisual;
    static int active;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetCount() { active=0; }
    void OnDestroy() { active = Mathf.Max(0,active-1); }
    public static void Visual(string id, Vector3 position, float size, float duration)
    {
        var def = ExtendedWeapons.Get(id);
        if (def?.model == null) return;
        var holder = new GameObject("WeaponFx_"+id); holder.transform.position=position;
        SpellModelEffects.Attach(def.model,holder.transform,Mathf.Clamp(size,.3f,1.8f));
        Destroy(holder,duration);
    }
    public static void Spawn(MegabonkBuild owner,string id,Vector3 position,Vector3 velocity,float radius,float duration,int damage)
    {
        if (active >= 96) return;
        var go = new GameObject("Area_"+id); go.transform.position=position;
        var field=go.AddComponent<WeaponArea>(); active++;
        field.owner=owner; field.weapon=id; field.velocity=velocity; field.radius=radius;
        field.remaining=duration; field.damage=damage; field.stage=StageDirector.Instance != null ? StageDirector.Instance.CurrentStage : 0;
        var model=id=="mb_poison" ? "PoisonProjectileFx" : ExtendedWeapons.Get(id)?.model;
        if(model!=null) SpellModelEffects.Attach(model,go.transform,Mathf.Max(.4f,radius*2));
        field.readableVisual=CreateReadableArea(id,go.transform,radius);
        // A readable world-space outline remains even when an imported particle
        // material renders transparent or its emission depends on removed scripts.
        CombatVisuals.Ring(position + Vector3.up * .12f, radius, ExtendedWeapons.Tint(id));
    }
    static Transform CreateReadableArea(string id,Transform parent,float radius)
    {
        // Particle-only exports can be accepted by Unity but still disappear
        // because their controller/emission logic was absent from the dump.
        var root=new GameObject("ReadableArea_"+id).transform;
        root.SetParent(parent,false);
        Color color=ExtendedWeapons.Tint(id);
        var material=RuntimeMaterialCache.Get(
            Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Sprites/Default"),color,color*.45f);
        void Part(PrimitiveType shape,Vector3 position,Vector3 scale,Material mat)
        {
            var primitive=GameObject.CreatePrimitive(shape);
            primitive.transform.SetParent(root,false);
            primitive.transform.localPosition=position;
            primitive.transform.localScale=scale;
            var collider=primitive.GetComponent<Collider>();
            if(collider!=null) { collider.enabled=false; Object.Destroy(collider); }
            primitive.GetComponent<Renderer>().sharedMaterial=mat;
        }
        if(id=="mb_tornado")
        {
            for(int i=0;i<3;i++)
            {
                float angle=i*2.094f;
                Part(PrimitiveType.Capsule,
                    new Vector3(Mathf.Cos(angle)*radius*.28f,.55f+i*.37f,Mathf.Sin(angle)*radius*.28f),
                    new Vector3(.16f,.55f,.16f),material);
            }
        }
        else if(id=="mb_blackhole")
        {
            var dark=RuntimeMaterialCache.Get(
                Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Sprites/Default"),
                new Color(.035f,.015f,.09f),new Color(.22f,.02f,.42f));
            Part(PrimitiveType.Sphere,Vector3.up*.45f,Vector3.one*Mathf.Min(1.3f,radius*.45f),dark);
            Part(PrimitiveType.Cylinder,Vector3.up*.12f,new Vector3(radius*1.1f,.025f,radius*1.1f),material);
        }
        else if(id=="mb_poison")
        {
            for(int i=0;i<3;i++)
                Part(PrimitiveType.Sphere,new Vector3((i-1)*radius*.22f,.22f+i*.13f,(i%2)*radius*.15f),
                    Vector3.one*(.22f+i*.08f),material);
        }
        else if(id=="mb_frost")
            Part(PrimitiveType.Cylinder,Vector3.up*.08f,new Vector3(radius*1.25f,.035f,radius*1.25f),material);
        return root;
    }
    void Update()
    {
        if(owner==null || GameManager.Instance==null || GameManager.Instance.gameState==GameManager.GameState.MainMenu ||
           GameManager.Instance.gameState==GameManager.GameState.GameOver || GameManager.Instance.gameState==GameManager.GameState.Victory ||
           (StageDirector.Instance!=null && stage!=StageDirector.Instance.CurrentStage)) { Destroy(gameObject); return; }
        if(Time.timeScale<=0 || owner.GetComponent<PlayerHealth3D>()?.isDead == true) return;
        remaining-=Time.deltaTime;
        if(remaining<=0) { Destroy(gameObject); return; }
        transform.position+=velocity*Time.deltaTime;
        if(readableVisual!=null) readableVisual.Rotate(Vector3.up,(weapon=="mb_tornado"?220f:70f)*Time.deltaTime,Space.World);
        tick-=Time.deltaTime;
        if(tick>0) return;
        tick=.5f;
        // The attached effect shows this zone; avoid stacking a second ring every damage tick.
        CombatVisuals.Ring(transform.position+Vector3.up*.1f,radius,ExtendedWeapons.Tint(weapon));
        foreach(var enemy in new System.Collections.Generic.List<EnemyHealth3D>(EnemyHealth3D.ActiveEnemies))
        {
            if(enemy==null || enemy.IsDying || Vector3.Distance(enemy.transform.position,transform.position)>radius) continue;
            int actual=owner.Hit(enemy,damage,weapon);
            if(weapon=="mb_poison") enemy.ApplyPoison(3,Mathf.Max(1,actual));
            if(weapon=="mb_frost") enemy.ApplyFreeze(.65f);
            if(weapon=="mb_blackhole" && !enemy.isBoss)
                enemy.GetComponent<EnemyAI3D>()?.ApplyKnockback((transform.position-enemy.transform.position).normalized,7);
        }
    }
}
