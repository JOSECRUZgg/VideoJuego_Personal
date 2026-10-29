using UnityEngine;

/// <summary>Proyectil de reliquia que persigue un enemigo y explota al alcanzarlo.</summary>
public class HomingRelicProjectile : MonoBehaviour
{
    public EnemyHealth3D target;
    public int damage = 35;
    public float speed = 18f;
    public float radius = 3f;
    public Color trailColor = new Color(1f, 0.45f, 0.1f);
    private float remainingLife = 5f;

    void Start()
    {
        CombatVisuals.Projectile(transform, visualKind, 1f, trailColor);
    }
    string visualKind;

    public static void Launch(Vector3 origin, EnemyHealth3D enemy, int hitDamage, Color color, float blastRadius = 3f, string visual = null)
    {
        if (enemy == null || enemy.IsDying) return;
        GameObject missile = new GameObject();
        missile.name = "RelicHomingProjectile";
        missile.transform.position = origin;
        var projectile = missile.AddComponent<HomingRelicProjectile>();
        projectile.target = enemy;
        projectile.damage = hitDamage;
        projectile.radius = blastRadius;
        projectile.trailColor = color;
        projectile.visualKind = visual ?? (blastRadius > 0 ? "mb_rocket" : "energy");
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        remainingLife -= Time.deltaTime;
        if (target == null || target.IsDying) target = MegabonkBuild.Nearest(transform.position, 50);
        if (remainingLife <= 0f || target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 start = transform.position;
        Vector3 destination = target.transform.position + Vector3.up;
        if((destination-start).sqrMagnitude>.001f) transform.rotation=Quaternion.LookRotation(destination-start);
        transform.position = Vector3.MoveTowards(start, destination, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, destination) <= 0.65f)
        {
            MagicVFX.SpawnImpact(destination, 2.2f, trailColor);
            ExtractedVisuals.Burst(visualKind == "mb_rocket" ? "RocketExplosion" : "GeneralProjectileHit", destination, 1.2f);
            SpellVFX.Burst(destination, "spark_01", trailColor, 1.1f);
            target.TakeDamage(damage, true, trailColor);
            if (radius > 0f) target.TriggerExplosion(radius, Mathf.Max(1, damage / 2));
            Destroy(gameObject);
        }
    }
}
