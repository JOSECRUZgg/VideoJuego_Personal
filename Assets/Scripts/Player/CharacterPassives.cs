using UnityEngine;
using System.Collections.Generic;

// Behavior recovered from passive descriptions/field names. Numerical coefficients are reconstructed.
public class CharacterPassives : MonoBehaviour
{
    public static CharacterPassives Instance {get;private set;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetInstance() {Instance=null;}
    public string Hero {get;private set;}
    public int Rank => Mathf.Max(0,(XPSystem.Instance!=null?XPSystem.Instance.level:1)-1);
    float speedBonus,flexReady,flexDamage,charge,chestTimer;
    float drained;
    int seenRank;
    Vector3 lastPosition;
    EnemyHealth3D marked;
    float markTimer;
    PlayerMovement3D movement;
    PlayerHealth3D health;
    public float Speed => Hero=="Calcium"?1+speedBonus:1;
    public float Crit => Hero=="Clank"?Rank*.01f:0;
    public float Armor => Hero=="Knight"?Rank:0;
    public float Dodge => Hero=="Ninja"?Mathf.Min(.5f,Rank*.005f):0;
    public float Rate => Hero=="Bandit_0"?1+Rank*.01f:1;
    public float Xp => Hero=="Spaceman"?1+Rank*.01f:1;
    public float Gold => Hero=="Robinette"?1+Rank*.01f:1;
    public float LifeSteal => Hero=="Vlad"?Mathf.Min(.2f,Rank*.002f):0;
    public bool CanFloat => Hero=="Birdo" || Hero=="Roberto";
    public bool CanClimb => Hero=="Monke";
    public void BeginRun()
    {
        Instance=this; Hero=OriginalContentCatalog.Selected?.id;
        movement=GetComponent<PlayerMovement3D>(); health=GetComponent<PlayerHealth3D>();
        speedBonus=flexReady=flexDamage=charge=markTimer=drained=0; chestTimer=45; seenRank=0; marked=null; lastPosition=transform.position;
    }
    void OnDestroy(){if(Instance==this) Instance=null;}
    public static string Description(string id)
    {
        switch(id)
        {
            case "Clank": return "Crítico: +1% por nivel.";
            case "Knight": return "Refuerzo: +1 de armadura por nivel.";
            case "Calcium": return "Acumula velocidad; recibir daño reduce a la mitad el bonus. La velocidad aumenta el daño.";
            case "Megachad": return "Flex bloquea un golpe, emite una onda y acumula daño; recarga más rápido al subir de nivel.";
            case "Fox": return "Bendición RNG: +1% de suerte por nivel.";
            case "Bandit_0": return "Flowstate: +1% de velocidad de ataque por nivel.";
            case "Spaceman": return "Quantum: +1% de experiencia por nivel.";
            case "Ogre": return "Guerrero: +1% de daño por nivel.";
            case "Vlad": return "Vampiro: +0,2% de robo de vida por nivel, hasta 20%.";
            case "Ninja": return "Shadowstep: evasión creciente; al esquivar ejecuta un enemigo normal cercano.";
            case "Amog": return "Plaga: los enemigos envenenados propagan veneno al morir.";
            case "Noelle": return "Tamaño creciente y daño adicional por enemigos congelados.";
            case "Birdo": return "Mantén salto al caer para flotar. Daño aéreo creciente por nivel.";
            case "Monke": return "Mantén salto frente a una pared para trepar; +2 de vida máxima por nivel.";
            case "Athena": return "Más daño con poca vida y espinas crecientes por nivel.";
            case "Robinette": return "Oro creciente por nivel y daño adicional según el oro guardado.";
            case "Chadwell": return "Maldición: aumenta la dificultad 1% por nivel.";
            case "Dicehead": return "Gamba: mejora una estadística aleatoria al subir de nivel.";
            case "Roberto": return "Obtiene un cofre gratuito periódicamente; puede flotar ligeramente.";
            case "TonyMcZoom": return "Carga una descarga al desplazarse; aumenta la atracción por nivel.";
            case "Bush_0": return "Marca objetivos; al eliminarlos explotan. Críticos más fuertes por nivel.";
            default:return "Pasiva no recuperada.";
        }
    }
    public float Damage(EnemyHealth3D enemy,bool critical)
    {
        float result=1+flexDamage;
        if(Hero=="Ogre") result+=Rank*.01f;
        if(Hero=="Calcium" && movement!=null) result+=movement.ActualSpeed*.04f;
        if(Hero=="Birdo" && movement!=null && !movement.isGrounded) result+=Rank*.02f;
        if(Hero=="Athena" && health!=null) result+=(1-health.currentHealth/(float)health.maxHealth)*(1+Rank*.01f);
        if(Hero=="Robinette") result+=Mathf.Min(3,GameManager.Instance.gold/1000f*.05f);
        if(Hero=="Bush_0" && critical) result+=Rank*.01f;
        if(Hero=="Noelle") {int frozen=0; foreach(var e in EnemyHealth3D.ActiveEnemies) if(e!=null && !e.IsDying && e.isFrozen) frozen++; result+=Mathf.Min(1,frozen*.02f);}
        return result;
    }
    public bool Block()
    {
        if(Hero!="Megachad" || Time.time<flexReady) return false;
        flexReady=Time.time+Mathf.Max(2,10-Rank*.1f); flexDamage+=.01f;
        CombatVisuals.Ring(transform.position,5,Color.yellow);
        foreach(var e in new List<EnemyHealth3D>(EnemyHealth3D.ActiveEnemies)) if(e!=null && !e.IsDying && Vector3.Distance(e.transform.position,transform.position)<5) MegabonkBuild.Instance?.Hit(e,10,"passive_flex");
        return true;
    }
    public void Damaged() {if(Hero=="Calcium") speedBonus*=.5f;}
    public void Drain(int damage) {drained+=damage*LifeSteal; int amount=Mathf.FloorToInt(drained); if(amount>0){drained-=amount;int before=health.currentHealth;health.Heal(amount);ExpeditionProgress.RecordLifeStealHeal(health.currentHealth-before);}}
    public void Evaded()
    {
        if(Hero!="Ninja") return;
        EnemyHealth3D victim=null; float closest=16f;
        foreach(var e in EnemyHealth3D.ActiveEnemies)
        {
            if(e==null || e.IsDying || e.isBoss || e.isElite) continue;
            float distance=(e.transform.position-transform.position).sqrMagnitude;
            if(distance<closest){closest=distance;victim=e;}
        }
        if(victim!=null) victim.TakeDamage(victim.currentHealth+victim.remainingShield);
    }
    public void EnemyDied(EnemyHealth3D enemy)
    {
        if(Hero=="Amog" && enemy.IsPoisoned)
        {
            ExtractedVisuals.Burst("PlagueFx",enemy.transform.position,1);
            foreach(var e in new List<EnemyHealth3D>(EnemyHealth3D.ActiveEnemies)) if(e!=null && e!=enemy && !e.IsDying && Vector3.Distance(e.transform.position,enemy.transform.position)<5) e.ApplyPoison(4,2+Rank/3);
        }
        if(Hero=="Bush_0" && enemy==marked) {marked=null; enemy.TriggerExplosion(4,20+Rank);}
    }
    public void ApplyLevelBonuses()
    {
        while(seenRank<Rank)
        {
            seenRank++;
            var xp=XPSystem.Instance;
            if(Hero=="Monke") health.IncreaseMaxHealth(2);
            if(Hero=="Fox") MegabonkRelicManager.Instance.luckPercent+=1;
            if(Hero=="Chadwell") MegabonkRelicManager.Instance.AddTemporaryDifficulty(1);
            if(Hero=="Noelle") xp.bulletSizeMultiplier+=.01f;
            if(Hero=="TonyMcZoom") xp.magnetMultiplier+=.01f;
            if(Hero=="Dicehead") switch(Random.Range(0,4)) {case 0:xp.damageMultiplier+=.02f;break;case 1:xp.fireRateMultiplier+=.02f;break;case 2:xp.speedMultiplier+=.02f;break;case 3:xp.bulletSizeMultiplier+=.02f;break;}
        }
    }
    void Update()
    {
        if(GameManager.Instance==null || GameManager.Instance.gameState!=GameManager.GameState.Playing || Time.timeScale<=0) return;
        ApplyLevelBonuses();
        if(Hero=="Calcium") speedBonus=Mathf.Min(1.5f,speedBonus+Time.deltaTime*.01f);
        if(Hero=="Bush_0" && (markTimer-=Time.deltaTime)<=0) {markTimer=5; marked=MegabonkBuild.Nearest(transform.position,40); if(marked!=null) ExtractedVisuals.Burst("Bullseye",marked.transform.position+Vector3.up,3);}
        if(Hero=="Roberto" && (chestTimer-=Time.deltaTime)<=0)
        {
            chestTimer=Mathf.Min(120,45+Rank); var p=transform.position+transform.right*3;
            if(ArenaEnricher.Instance==null || ArenaEnricher.Instance.TryFindWalkablePosition(p,out p)) BossChest3D.Spawn(p,false);
            else chestTimer=3;
        }
        float distance=Vector3.Distance(transform.position,lastPosition); lastPosition=transform.position;
        if(Hero=="TonyMcZoom" && distance<5)
        {
            charge+=distance;
            if(charge>=25) {var e=MegabonkBuild.Nearest(transform.position,25); if(e!=null){charge=0;CombatVisuals.Lightning(transform.position+Vector3.up,e.transform.position+Vector3.up);MegabonkBuild.Instance?.Hit(e,20+Rank,"passive_zap");}}
        }
    }
}
