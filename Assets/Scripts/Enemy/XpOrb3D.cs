using UnityEngine;

/// <summary>
/// Gemas de experiencia recogibles durante la expedición:
/// - Pequeño (Verde): 1 XP (Lurkers, Sploders)
/// - Mediano (Azul): 5 XP (Chargers, Summoners)
/// - Grande (Morado/Dorado): 25 a 50 XP (Elites, Jefes)
/// </summary>
public class XpOrb3D : MonoBehaviour
{
    public enum OrbTier { Small, Medium, Large }

    public int xpAmount = 1;
    public bool RemoteMirror { get; set; }
    public OrbTier tier = OrbTier.Small;
    public float floatSpeed = 4f;
    public float floatAmplitude = 0.15f;
    public float rotationSpeed = 120f;
    public float pickupDistance = 2.0f;
    public float baseMagnetDistance = 9.0f;
    public float magnetSpeed = 16f;

    private Transform player;
    private Vector3 basePosition;
    private float lifeTimer;
    private bool isFlyingToPlayer;
    readonly System.Collections.Generic.HashSet<EnemyHealth3D> damagedEnemies = new System.Collections.Generic.HashSet<EnemyHealth3D>();

    public void Attract() { isFlyingToPlayer = true; }

    public static XpOrb3D Spawn(Vector3 position, int xpValue)
    {
        OrbTier t = OrbTier.Small;
        if (xpValue >= 20) t = OrbTier.Large;
        else if (xpValue >= 4) t = OrbTier.Medium;

        return Spawn(position, xpValue, t);
    }

    public static XpOrb3D Spawn(Vector3 position, int xpValue, OrbTier orbTier)
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "XpOrb_" + orbTier;
        Vector3 spawnPos = position;
        if (ArenaEnricher.Instance != null) spawnPos = ArenaEnricher.Instance.PlaceOnSnow(spawnPos);
        orb.transform.position = spawnPos + Vector3.up * 0.5f;

        // Tamaño y color según el tipo de orbe
        float scale = 0.35f;
        Color col = new Color(0.2f, 1f, 0.4f); // Verde (Pequeño)

        if (orbTier == OrbTier.Medium)
        {
            scale = 0.52f;
            col = new Color(0.2f, 0.75f, 1f); // Azul (Mediano)
        }
        else if (orbTier == OrbTier.Large)
        {
            scale = 0.85f;
            col = new Color(0.9f, 0.25f, 1f); // Morado/Dorado (Grande)
        }

        orb.transform.localScale = Vector3.one * scale;

        Collider colObj = orb.GetComponent<Collider>();
        if (colObj != null) Destroy(colObj);

        Renderer rend = orb.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.sharedMaterial = GetOrCreateMaterial(orbTier, col);
            // Real faceted crystal mesh; retain the existing magnet/pickup logic.
            var filter=orb.GetComponent<MeshFilter>();
            if(filter!=null) filter.sharedMesh=CrystalMesh;
        }

        // Luz puntual solo para gemas Medianas y Grandes para no sobrecargar el renderizador
        if (orbTier != OrbTier.Small)
        {
            GameObject lightObj = new GameObject("OrbLight");
            lightObj.transform.SetParent(orb.transform);
            lightObj.transform.localPosition = Vector3.zero;
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = col;
            l.range = orbTier == OrbTier.Large ? 4.5f : 2.5f;
            l.intensity = orbTier == OrbTier.Large ? 2.0f : 1.2f;
        }

        XpOrb3D script = orb.AddComponent<XpOrb3D>();
        script.xpAmount = xpValue;
        script.tier = orbTier;
        return script;
    }

    static Mesh crystalMesh;
    static Mesh CrystalMesh
    {
        get
        {
            if(crystalMesh!=null) return crystalMesh;
            Vector3[] points={Vector3.up*.8f,Vector3.down*.8f,Vector3.left*.35f,Vector3.forward*.35f,Vector3.right*.35f,Vector3.back*.35f};
            int[] faces={0,3,2,0,4,3,0,5,4,0,2,5,1,2,3,1,3,4,1,4,5,1,5,2};
            var vertices=new Vector3[faces.Length]; var triangles=new int[faces.Length];
            for(int i=0;i<faces.Length;i++) { vertices[i]=points[faces[i]]; triangles[i]=i; }
            crystalMesh=new Mesh {name="XP_Crystal_Reconstructed",vertices=vertices,triangles=triangles}; crystalMesh.RecalculateNormals(); crystalMesh.RecalculateBounds(); return crystalMesh;
        }
    }

    private static Material s_MatSmall;
    private static Material s_MatMed;
    private static Material s_MatLarge;
    private static Transform s_CachedPlayer;

    private static Material GetOrCreateMaterial(OrbTier tier, Color col)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        switch (tier)
        {
            case OrbTier.Small:
                if (s_MatSmall == null)
                {
                    s_MatSmall = RuntimeMaterialCache.Get(shader, col, col * 2.8f);
                }
                return s_MatSmall;

            case OrbTier.Medium:
                if (s_MatMed == null)
                {
                    s_MatMed = RuntimeMaterialCache.Get(shader, col, col * 2.8f);
                }
                return s_MatMed;

            case OrbTier.Large:
            default:
                if (s_MatLarge == null)
                {
                    s_MatLarge = RuntimeMaterialCache.Get(shader, col, col * 2.8f);
                }
                return s_MatLarge;
        }
    }

    void Start()
    {
        basePosition = transform.position;
        if (s_CachedPlayer == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) s_CachedPlayer = p.transform;
        }
        player = s_CachedPlayer;
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (RemoteMirror) { transform.Rotate(Vector3.up * (rotationSpeed * Time.deltaTime)); return; }
        lifeTimer += Time.deltaTime;

        if (player == null)
        {
            if (s_CachedPlayer == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) s_CachedPlayer = p.transform;
            }
            player = s_CachedPlayer;
            if (player == null) return;
        }

        Transform magnetTarget = player;
        if (CoopRun.IsHost && !CoopRun.PartnerDowned && CoopRun.Partner != null &&
            (CoopRun.Partner.position - transform.position).sqrMagnitude < (player.position - transform.position).sqrMagnitude)
            magnetTarget = CoopRun.Partner;
        Vector3 targetPos = magnetTarget.position + Vector3.up * 1f;
        float dist = Vector3.Distance(transform.position, targetPos);

        // Imán dinámico
        float magnetMult = XPSystem.Instance != null ? XPSystem.Instance.magnetMultiplier : 1f;
        float actualMagnetDist = baseMagnetDistance * magnetMult;

        // Sinergia Soul Drain: atracción instantánea
        if (XPSystem.Instance != null && XPSystem.Instance.soulDrain)
        {
            actualMagnetDist *= 2.5f;
        }

        if (dist <= actualMagnetDist)
        {
            isFlyingToPlayer = true;
        }

        if (isFlyingToPlayer)
        {
            Vector3 start = transform.position;
            float currentMagSpeed = (XPSystem.Instance != null && XPSystem.Instance.soulDrain) ? magnetSpeed * 1.6f : magnetSpeed;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, currentMagSpeed * Time.deltaTime);
            int knowledge = MegabonkBuild.Relics("shattered_knowledge");
            if (knowledge > 0)
            {
                Vector3 segment = transform.position - start;
                foreach (var enemy in EnemyHealth3D.ActiveEnemies)
                {
                    if (enemy == null || enemy.IsDying || damagedEnemies.Contains(enemy)) continue;
                    Vector3 center = enemy.transform.position + Vector3.up;
                    float t = segment.sqrMagnitude > 0 ? Mathf.Clamp01(Vector3.Dot(center - start, segment) / segment.sqrMagnitude) : 0;
                    if ((center - start - segment * t).sqrMagnitude > 2.25f) continue;
                    damagedEnemies.Add(enemy);
                    enemy.TakeDamage(8 * knowledge, true, Color.cyan);
                }
            }

            if (dist <= pickupDistance && !RemoteMirror)
            {
                Collect();
            }
        }
        else
        {
            // Animación flotante
            float newY = basePosition.y + Mathf.Sin(lifeTimer * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(basePosition.x, newY, basePosition.z);
            transform.Rotate(Vector3.up * (rotationSpeed * Time.deltaTime));
        }
    }

    void Collect()
    {
        if (RemoteMirror) return;
        if (XPSystem.Instance != null)
        {
            XPSystem.Instance.AddXP(xpAmount);
        }
        Destroy(gameObject);
    }
}
