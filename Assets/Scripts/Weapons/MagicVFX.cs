using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor de efectos mágicos animados (Magic VFX Pack 1).
/// Controla la animación y renderizado de 'Whirlwind' (19 frames) e 'Impact' (4 frames)
/// como billboards 3D de alta visibilidad, proyectiles de viento y ráfagas de impacto.
/// </summary>
public static class MagicVFX
{
    private static Sprite[] s_WhirlwindFrames;
    private static Sprite[] s_ImpactFrames;
    private static Material s_SpriteMaterial;
    private static bool s_Initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_WhirlwindFrames = null;
        s_ImpactFrames = null;
        s_SpriteMaterial = null;
        s_Initialized = false;
    }

    public static void Initialize()
    {
        if (s_Initialized) return;
        s_Initialized = true;

        EnsureMaterial();
        LoadImpactFrames();
        LoadWhirlwindFrames();
    }

    static void EnsureMaterial()
    {
        if (s_SpriteMaterial != null) return;
        Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit")
            ?? Shader.Find("Sprites/Default")
            ?? Shader.Find("Game/SpellParticle")
            ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh != null)
        {
            s_SpriteMaterial = new Material(sh);
        }
    }

    static void LoadImpactFrames()
    {
        var list = new List<Sprite>();
        for (int i = 1; i <= 4; i++)
        {
            string num = i.ToString("D3");
            Sprite sp = Resources.Load<Sprite>($"MagicVFX/Sprites/Impact/Impact-{num}");
            if (sp == null)
            {
                Texture2D tex = Resources.Load<Texture2D>($"MagicVFX/Sprites/Impact/Impact-{num}");
                if (tex != null)
                {
                    tex.filterMode = FilterMode.Point;
                    sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
                }
            }
            if (sp != null) list.Add(sp);
        }

        // Si falló por carpetas de Resources, intentar cortar la hoja de spritesheet
        if (list.Count == 0)
        {
            Texture2D sheet = Resources.Load<Texture2D>("MagicVFX/Spritesheets/Impact");
#if UNITY_EDITOR
            if (sheet == null)
                sheet = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MagicVFX/Spritesheets/Impact.png");
#endif
            if (sheet != null)
            {
                sheet.filterMode = FilterMode.Point;
                float frameW = sheet.width / 4f;
                for (int i = 0; i < 4; i++)
                {
                    Sprite sp = Sprite.Create(sheet, new Rect(i * frameW, 0, frameW, sheet.height), new Vector2(0.5f, 0.5f), 32f);
                    list.Add(sp);
                }
            }
        }

        s_ImpactFrames = list.ToArray();
    }

    static void LoadWhirlwindFrames()
    {
        var list = new List<Sprite>();
        for (int i = 1; i <= 19; i++)
        {
            string num = i.ToString("D3");
            Sprite sp = Resources.Load<Sprite>($"MagicVFX/Sprites/Whirlwind/Whirlwind-{num}");
            if (sp == null)
            {
                Texture2D tex = Resources.Load<Texture2D>($"MagicVFX/Sprites/Whirlwind/Whirlwind-{num}");
                if (tex != null)
                {
                    tex.filterMode = FilterMode.Point;
                    sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
                }
            }
            if (sp != null) list.Add(sp);
        }

        // Si falló por carpetas de Resources, intentar cortar la hoja de spritesheet
        if (list.Count == 0)
        {
            Texture2D sheet = Resources.Load<Texture2D>("MagicVFX/Spritesheets/Whirlwind");
#if UNITY_EDITOR
            if (sheet == null)
                sheet = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MagicVFX/Spritesheets/Whirlwind.png");
#endif
            if (sheet != null)
            {
                sheet.filterMode = FilterMode.Point;
                float frameW = sheet.width / 19f;
                for (int i = 0; i < 19; i++)
                {
                    Sprite sp = Sprite.Create(sheet, new Rect(i * frameW, 0, frameW, sheet.height), new Vector2(0.5f, 0.5f), 32f);
                    list.Add(sp);
                }
            }
        }

        s_WhirlwindFrames = list.ToArray();
    }

    /// <summary>
    /// Genera una ráfaga de impacto mágico animado (4 frames) en la posición indicada.
    /// </summary>
    public static GameObject SpawnImpact(Vector3 position, float scale = 1.6f, Color? tint = null, float fps = 18f)
    {
        Initialize();
        if (s_ImpactFrames == null || s_ImpactFrames.Length == 0) return null;

        Color finalColor = tint ?? new Color(1f, 0.9f, 0.4f, 1f);
        GameObject go = new GameObject("MagicImpactVFX");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * scale;

        var anim = go.AddComponent<MagicAnimatedVFX>();
        anim.Setup(s_ImpactFrames, fps, false, finalColor, s_SpriteMaterial, billboardYOnly: false);
        anim.AddLight(finalColor, scale * 2.2f, 2.5f, 0.18f);

        return go;
    }

    /// <summary>
    /// Genera un torbellino de viento mágico (19 frames) que permanece en el lugar o sigue a un objetivo.
    /// </summary>
    public static GameObject SpawnWhirlwind(Vector3 position, float duration = 1.2f, float scale = 2.4f, Color? tint = null, Transform followTarget = null)
    {
        Initialize();
        if (s_WhirlwindFrames == null || s_WhirlwindFrames.Length == 0) return null;

        Color finalColor = tint ?? new Color(0.6f, 0.95f, 1f, 0.95f);
        GameObject go = new GameObject("MagicWhirlwindVFX");
        go.transform.position = position;
        go.transform.localScale = Vector3.one * scale;

        if (followTarget != null)
            go.transform.SetParent(followTarget, true);

        float calculatedFps = s_WhirlwindFrames.Length / Mathf.Max(0.1f, duration);
        var anim = go.AddComponent<MagicAnimatedVFX>();
        anim.Setup(s_WhirlwindFrames, calculatedFps, false, finalColor, s_SpriteMaterial, billboardYOnly: true);
        anim.AddLight(finalColor, scale * 2.5f, 1.8f, duration);

        return go;
    }

    /// <summary>
    /// Lanza un proyectil torbellino viajero que avanza cortando y atrayendo enemigos a su paso.
    /// </summary>
    public static GameObject LaunchWhirlwind(Vector3 origin, Vector3 direction, float speed = 14f, float duration = 2.2f, int damage = 22, float scale = 2.5f, Color? tint = null)
    {
        Initialize();
        if (s_WhirlwindFrames == null || s_WhirlwindFrames.Length == 0) return null;

        Color finalColor = tint ?? new Color(0.4f, 0.95f, 0.85f, 0.95f);
        GameObject go = new GameObject("WhirlwindCutterProjectile");
        go.transform.position = origin;
        go.transform.localScale = Vector3.one * scale;

        var anim = go.AddComponent<MagicAnimatedVFX>();
        anim.Setup(s_WhirlwindFrames, 24f, true, finalColor, s_SpriteMaterial, billboardYOnly: true);
        anim.AddLight(finalColor, scale * 2.8f, 2.2f, duration);

        var projectile = go.AddComponent<WhirlwindProjectile>();
        projectile.Setup(direction.normalized, speed, duration, damage, scale);

        return go;
    }
}

/// <summary>
/// Componente de animación flipbook para Sprites en 3D con billboarding hacia la cámara.
/// </summary>
public class MagicAnimatedVFX : MonoBehaviour
{
    private Sprite[] frames;
    private SpriteRenderer rend;
    private float fps = 16f;
    private bool loop = false;
    private bool billboardYOnly = true;
    private float timer = 0f;
    private Camera cachedCam;

    public void Setup(Sprite[] animFrames, float framesPerSec, bool isLooping, Color color, Material mat, bool billboardYOnly)
    {
        frames = animFrames;
        fps = Mathf.Max(1f, framesPerSec);
        loop = isLooping;
        this.billboardYOnly = billboardYOnly;

        rend = gameObject.AddComponent<SpriteRenderer>();
        if (mat != null) rend.sharedMaterial = mat;
        rend.color = color;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        if (frames != null && frames.Length > 0)
        {
            rend.sprite = frames[0];
        }

        cachedCam = Camera.main;
    }

    public void AddLight(Color lightColor, float range, float intensity, float fadeDuration)
    {
        GameObject lObj = new GameObject("VFXLight");
        lObj.transform.SetParent(transform, false);
        lObj.transform.localPosition = Vector3.up * 0.3f;
        Light l = lObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = lightColor;
        l.range = range;
        l.intensity = intensity;
        var fader = lObj.AddComponent<LightFader>();
        fader.Setup(intensity, fadeDuration);
    }

    void Update()
    {
        if (frames == null || frames.Length == 0 || Time.timeScale <= 0f) return;

        timer += Time.deltaTime;
        int frameIndex = Mathf.FloorToInt(timer * fps);

        if (frameIndex >= frames.Length)
        {
            if (loop)
            {
                frameIndex %= frames.Length;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        if (rend != null && frameIndex < frames.Length)
        {
            rend.sprite = frames[frameIndex];
        }
    }

    void LateUpdate()
    {
        if (cachedCam == null) cachedCam = Camera.main;
        if (cachedCam == null) return;

        if (billboardYOnly)
        {
            Vector3 forward = cachedCam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.001f)
                transform.forward = forward;
        }
        else
        {
            transform.forward = cachedCam.transform.forward;
        }
    }
}

/// <summary>
/// Desvanece suavemente una luz dinámica de efecto visual.
/// </summary>
public class LightFader : MonoBehaviour
{
    private Light targetLight;
    private float startIntensity;
    private float duration;
    private float timer;

    public void Setup(float intensity, float lifeDuration)
    {
        targetLight = GetComponent<Light>();
        startIntensity = intensity;
        duration = Mathf.Max(0.05f, lifeDuration);
        timer = 0f;
    }

    void Update()
    {
        if (targetLight == null) return;
        timer += Time.deltaTime;
        float progress = Mathf.Clamp01(timer / duration);
        targetLight.intensity = Mathf.Lerp(startIntensity, 0f, progress);
        if (progress >= 1f)
        {
            targetLight.enabled = false;
        }
    }
}

/// <summary>
/// Torbellino viajero cortante con daño continuo y leve atracción de enemigos en área.
/// </summary>
public class WhirlwindProjectile : MonoBehaviour
{
    private Vector3 moveDir;
    private float moveSpeed;
    private float remainingLife;
    private int hitDamage;
    private float radius;
    private float damageTickTimer;

    public void Setup(Vector3 direction, float speed, float lifetime, int damage, float scale)
    {
        moveDir = direction;
        moveSpeed = speed;
        remainingLife = lifetime;
        hitDamage = damage;
        radius = scale * 0.9f;
        damageTickTimer = 0f;
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;

        remainingLife -= Time.deltaTime;
        if (remainingLife <= 0f)
        {
            MagicVFX.SpawnImpact(transform.position + Vector3.up * 0.5f, radius * 1.2f, new Color(0.4f, 1f, 0.8f));
            Destroy(gameObject);
            return;
        }

        transform.position += moveDir * moveSpeed * Time.deltaTime;

        // Comprobación de daño y atracción periódica
        damageTickTimer -= Time.deltaTime;
        if (damageTickTimer <= 0f)
        {
            damageTickTimer = 0.22f;
            ApplyWhirlwindContact();
        }
    }

    void ApplyWhirlwindContact()
    {
        Vector3 center = transform.position;
        foreach (var eh in EnemyHealth3D.ActiveEnemies)
        {
            if (eh == null || eh.IsDying) continue;
            float dist = Vector3.Distance(center, eh.transform.position);
            if (dist <= radius)
            {
                eh.TakeDamage(hitDamage, false, new Color(0.5f, 1f, 0.85f));
                MagicVFX.SpawnImpact(eh.transform.position + Vector3.up * 0.7f, 1.2f, new Color(0.5f, 1f, 0.9f));

                // Leve vórtice que atrae al enemigo hacia el centro del torbellino
                EnemyAI3D ai = eh.GetComponent<EnemyAI3D>();
                if (ai != null)
                {
                    Vector3 pullDir = (center - eh.transform.position);
                    pullDir.y = 0f;
                    ai.ApplyKnockback(pullDir.normalized, 2.8f);
                }
            }
        }
    }
}
