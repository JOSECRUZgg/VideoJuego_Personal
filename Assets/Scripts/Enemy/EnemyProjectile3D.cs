using UnityEngine;

/// <summary>
/// Proyectil de fuego/magia disparado por Summoners, Elites y Bosses.
/// Visuales propios disponibles también en la compilación, no solo en el editor.
/// Vuela a velocidad constante y daña al jugador al impactar.
/// </summary>
public class EnemyProjectile3D : MonoBehaviour
{
    public float speed = 7.5f;
    public int damage = 10;
    public float lifeTime = 5.0f;
    public bool appliesBurn = false;
    private bool fireVisual;
    private Color visualColor;

    private Vector3 direction;
    private Transform cachedPlayer;
    readonly RaycastHit[] obstacleHits = new RaycastHit[32];

    public static void Create(Vector3 startPos, Vector3 targetPos, int dmg = 10, Color? col = null, bool burn = false)
    {
        Color bulletColor = col ?? new Color(1f, .35f, .1f);
        bool fire = burn || (bulletColor.r > .65f && bulletColor.r > bulletColor.b * 1.4f);
        GameObject proj = new GameObject(fire ? "EnemyProjectile_Fireball" : "EnemyProjectile_Energy");
        proj.transform.position = startPos;
        CombatVisuals.Projectile(proj.transform, fire ? "mb_fire" : "energy", .9f, bulletColor);

        // Añadir luz dinámica que ilumina la arena en su trayectoria
        GameObject lightObj = new GameObject("Light");
        lightObj.transform.SetParent(proj.transform, false);
        Light lt = lightObj.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.range = 6f;
        lt.intensity = 2.5f;
        lt.color = col ?? new Color(1f, 0.45f, 0.15f);

        EnemyProjectile3D script = proj.AddComponent<EnemyProjectile3D>();
        script.damage = dmg;
        script.appliesBurn = burn;
        script.fireVisual = fire;
        script.visualColor = bulletColor;

        Vector3 dir = (targetPos - startPos);
        script.direction = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward;
        proj.transform.rotation = Quaternion.LookRotation(script.direction);
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) cachedPlayer = p.transform;
    }

    void Update()
    {
        if (Time.timeScale <= 0f || MegabonkRelicManager.HostileTimeScale <= 0) return;
        Vector3 previousPosition = transform.position;
        transform.position += direction * speed * MegabonkRelicManager.HostileDeltaTime;
        lifeTime -= MegabonkRelicManager.HostileDeltaTime;
        if (lifeTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // Detección de colisión con el jugador
        if (cachedPlayer == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) cachedPlayer = p.transform;
            return;
        }

        Vector3 target = cachedPlayer.position + Vector3.up;
        Vector3 segment = transform.position - previousPosition;
        float length = segment.magnitude;
        float wallDistance = float.PositiveInfinity;
        if (length > .0001f)
        {
            int count = Physics.SphereCastNonAlloc(previousPosition, .15f, segment / length,
                obstacleHits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == obstacleHits.Length) { Destroy(gameObject); return; }
            for (int i = 0; i < count; i++)
            {
                var obstacle = obstacleHits[i].collider;
                if (obstacle.GetComponentInParent<EnemyAI3D>() != null ||
                    obstacle.GetComponentInParent<PlayerHealth3D>() != null || obstacle.transform.IsChildOf(transform)) continue;
                wallDistance = Mathf.Min(wallDistance, obstacleHits[i].distance);
            }
        }
        float t = segment.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(target - previousPosition, segment) / segment.sqrMagnitude) : 0f;
        float dist = Vector3.Distance(previousPosition + segment * t, target);
        Transform partner = CoopRun.IsHost && !CoopRun.PartnerDowned ? CoopRun.Partner : null;
        float partnerT = partner != null && segment.sqrMagnitude > 0f
            ? Mathf.Clamp01(Vector3.Dot(partner.position + Vector3.up - previousPosition, segment) / segment.sqrMagnitude) : 0f;
        bool partnerHit = partner != null &&
            Vector3.Distance(previousPosition + segment * partnerT, partner.position + Vector3.up) <= 1.25f &&
            partnerT * length <= wallDistance;
        bool localHit = dist <= 1.25f && t * length <= wallDistance &&
            cachedPlayer.GetComponent<PlayerHealth3D>()?.isDead != true;
        if (partnerHit && (!localHit || partnerT < t))
        {
            CoopRun.HitPartner(damage);
            Impact(transform.position);
            Destroy(gameObject);
        }
        else if (localHit)
        {
            PlayerHealth3D ph = cachedPlayer.GetComponent<PlayerHealth3D>();
            if (ph != null && !ph.isDead)
            {
                ph.TakeDamage(damage);
                if (appliesBurn)
                {
                    ph.ApplyBurn(3.0f, 3);
                }
            }
            Impact(transform.position);
            Destroy(gameObject);
        }
        else if (!float.IsPositiveInfinity(wallDistance))
        {
            Impact(previousPosition + segment.normalized * wallDistance);
            Destroy(gameObject);
        }
    }

    void Impact(Vector3 position)
    {
        SpellVFX.Burst(position, fireVisual ? "flame_01" : "spark_01", visualColor, .65f);
    }
}
