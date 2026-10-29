using UnityEngine;

/// <summary>
/// Proyectil trazador ultra-visible con colores según tipo de arma y chispazo de impacto mejorado.
/// </summary>
public class BulletTracer3D : MonoBehaviour
{
    private Vector3 targetPoint;
    private float speed = 65f;
    private float lifeTime = 1.5f;
    private Color tracerColor;

    // Colores canónicos por arma
    public static readonly Color ColorRevolver  = new Color(1.00f, 0.82f, 0.15f);  // Dorado cálido
    public static readonly Color ColorSMG       = new Color(0.20f, 0.92f, 1.00f);  // Cyan eléctrico
    public static readonly Color ColorShotgun   = new Color(1.00f, 0.40f, 0.10f);  // Naranja ardiente
    public static readonly Color ColorCrossbow  = new Color(0.80f, 0.20f, 1.00f);  // Violeta mágico
    public static readonly Color ColorDefault   = new Color(1.00f, 0.85f, 0.15f);  // Amarillo genérico

    private static PlayerShooting3D s_CachedShooting;
    private static readonly System.Collections.Generic.Dictionary<Color, Material> s_TracerMatCache = new System.Collections.Generic.Dictionary<Color, Material>();
    private static readonly System.Collections.Generic.Dictionary<Color, Material> s_TrailMatCache = new System.Collections.Generic.Dictionary<Color, Material>();
    private static readonly System.Collections.Generic.Dictionary<(Color, float), Material> s_SparkMatCache = new System.Collections.Generic.Dictionary<(Color, float), Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_CachedShooting = null;
        s_TracerMatCache.Clear();
        s_TrailMatCache.Clear();
        s_SparkMatCache.Clear();
    }

    static Material GetTracerMaterial(Color bulletColor)
    {
        if (!s_TracerMatCache.TryGetValue(bulletColor, out Material mat) || mat == null)
        {
#if UNITY_EDITOR
            Material baseMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BulletTracerMat.mat");
            if (baseMat != null)
            {
                mat = new Material(baseMat);
                mat.SetColor("_BaseColor", bulletColor);
                mat.SetColor("_EmissionColor", bulletColor * 3.5f);
                s_TracerMatCache[bulletColor] = mat;
                return mat;
            }
#endif
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = RuntimeMaterialCache.Get(litShader, bulletColor, bulletColor * 4f);
            s_TracerMatCache[bulletColor] = mat;
        }
        return mat;
    }

    static Material GetTrailMaterial(Color bulletColor)
    {
        if (!s_TrailMatCache.TryGetValue(bulletColor, out Material mat) || mat == null)
        {
#if UNITY_EDITOR
            Material baseMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BulletTrailMat.mat");
            if (baseMat != null)
            {
                mat = new Material(baseMat);
                Color trailCol = new Color(bulletColor.r, bulletColor.g, bulletColor.b, 0.85f);
                mat.SetColor("_BaseColor", trailCol);
                mat.SetColor("_EmissionColor", bulletColor * 2.5f);
                s_TrailMatCache[bulletColor] = mat;
                return mat;
            }
#endif
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = RuntimeMaterialCache.Get(litShader, bulletColor, bulletColor * 2.5f);
            s_TrailMatCache[bulletColor] = mat;
        }
        return mat;
    }

    public static Material GetSparkMaterial(Color col, float emissionMultiplier = 3.5f)
    {
#if UNITY_EDITOR
        var key = (col, emissionMultiplier);
        if (s_SparkMatCache.TryGetValue(key, out Material sm) && sm != null) return sm;
        Material baseMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BulletTracerMat.mat");
        if (baseMat != null)
        {
            sm = new Material(baseMat);
            sm.SetColor("_BaseColor", col);
            sm.SetColor("_EmissionColor", col * emissionMultiplier);
            s_SparkMatCache[key] = sm;
            return sm;
        }
#endif
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        return RuntimeMaterialCache.Get(sh, col, col * emissionMultiplier);
    }

    /// <summary>
    /// Crea un trazador de bala con color basado en el arma activa del jugador.
    /// </summary>
    public static void Create(Vector3 startPos, Vector3 endPos, bool bigBullets = false, Color? customColor = null)
    {
        // Detectar color por tipo de arma si no hay un color personalizado
        Color bulletColor = customColor ?? GetWeaponColor();

        GameObject tracer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tracer.name = "BulletTracer";
        tracer.transform.position = startPos;

        float width  = bigBullets ? 0.38f : 0.20f;
        float length = bigBullets ? 1.5f  : 0.90f;
        tracer.transform.localScale = new Vector3(width, width, length);
        tracer.transform.LookAt(endPos);

        Collider col = tracer.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Material brillante compartido con fuerte emisión
        Renderer rend = tracer.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.sharedMaterial = GetTracerMaterial(bulletColor);
        }

        // Luz dinámica que ilumina el entorno mientras viaja
        GameObject lightObj = new GameObject("BulletLight");
        lightObj.transform.SetParent(tracer.transform);
        lightObj.transform.localPosition = Vector3.zero;
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = bulletColor;
        l.range = bigBullets ? 5f : 3.5f;
        l.intensity = bigBullets ? 2.5f : 1.8f;

        // Estela — color degradado del tracer
        TrailRenderer trail = tracer.AddComponent<TrailRenderer>();
        trail.time = bigBullets ? 0.25f : 0.16f;
        trail.startWidth = bigBullets ? 0.28f : 0.15f;
        trail.endWidth = 0.01f;
        trail.sharedMaterial = GetTrailMaterial(bulletColor);

        BulletTracer3D script = tracer.AddComponent<BulletTracer3D>();
        script.targetPoint = endPos;
        script.tracerColor = bulletColor;
    }

    static Color GetWeaponColor()
    {
        if (s_CachedShooting == null) s_CachedShooting = Object.FindAnyObjectByType<PlayerShooting3D>();
        if (s_CachedShooting == null || s_CachedShooting.currentWeapon == null) return ColorDefault;

        switch (s_CachedShooting.currentWeapon.type)
        {
            case WeaponType.Revolver:  return ColorRevolver;
            case WeaponType.SMG:       return ColorSMG;
            case WeaponType.Shotgun:   return ColorShotgun;
            case WeaponType.Crossbow:  return ColorCrossbow;
            default:                   return ColorDefault;
        }
    }

    void Update()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f) { Destroy(gameObject); return; }

        transform.position = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPoint) < 0.3f)
        {
            CreateHitSpark(transform.position, tracerColor);
            Destroy(gameObject);
        }
    }

    static void CreateHitSpark(Vector3 pos, Color col)
    {
        SpellVFX.Burst(pos, "spark_01", col, 0.5f);
        MagicVFX.SpawnImpact(pos, 1.25f, col);
    }
}


public static class RuntimeMaterialCache
{
    private static readonly System.Collections.Generic.Dictionary<(Shader, Color, Color), Material> materials =
        new System.Collections.Generic.Dictionary<(Shader, Color, Color), Material>();

    public static Material Get(Shader shader, Color color, Color emission = default)
    {
        var key = (shader, color, emission);
        if (materials.TryGetValue(key, out Material material) && material != null) return material;
        material = new Material(shader) { color = color };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (emission != Color.clear && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
        materials[key] = material;
        return material;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Initialize()
    {
        Release();
        Application.quitting -= Release;
        Application.quitting += Release;
    }

    static void Release()
    {
        foreach (var material in materials.Values)
            if (material != null) Object.Destroy(material);
        materials.Clear();
    }
}
