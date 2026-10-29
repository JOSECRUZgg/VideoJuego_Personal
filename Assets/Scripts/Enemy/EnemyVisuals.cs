using UnityEngine;

/// <summary>
/// Mejoras visuales procedurales del enemigo:
/// - Ojo brillante que sigue al jugador
/// - Barra de vida flotante
/// - Patrones de color más detallados según tipo
/// - Indicador de advertencia cuando está cerca
/// </summary>
[RequireComponent(typeof(EnemyAI3D))]
[RequireComponent(typeof(EnemyHealth3D))]
public class EnemyVisuals : MonoBehaviour
{
    // Partes visuales generadas en tiempo de ejecución
    private GameObject eyeObj;
    private GameObject eyePupilObj;
    private GameObject bodyDecalObj;  // Detalle extra en el cuerpo
    private Renderer bodyRenderer;
    private Renderer eyeRenderer;
    private Renderer pupilRenderer;
    private GameObject kitVisual;

    private EnemyAI3D ai;
    private EnemyHealth3D health;
    private Transform player;

    // Barra de vida
    private Texture2D whiteTex;
    private Camera mainCam;
    private GUIStyle _cachedTypeLabelStyle;

    // Animación de pulso
    private float pulseTimer;
    private float warningBlink;

    // Color base según tipo (con variación metálica)
    private Color typeColorPrimary;
    private Color typeColorSecondary;
    private Color eyeColor;

    private static Transform s_CachedPlayer;
    private static Material s_PupilMat;
    private static readonly System.Collections.Generic.Dictionary<Color, Material> s_EyeMatCache = new System.Collections.Generic.Dictionary<Color, Material>();
    private static readonly System.Collections.Generic.Dictionary<Color, Material> s_DecalMatCache = new System.Collections.Generic.Dictionary<Color, Material>();
    private MaterialPropertyBlock _propBlock;

    void Start()
    {
        ai = GetComponent<EnemyAI3D>();
        health = GetComponent<EnemyHealth3D>();
        whiteTex = Texture2D.whiteTexture;
        mainCam = Camera.main;

        if (s_CachedPlayer == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) s_CachedPlayer = p.transform;
        }
        player = s_CachedPlayer;
        _propBlock = new MaterialPropertyBlock();

        SetupColors();
        BuildVisuals();

    }

    void SetupColors()
    {
        switch (ai.enemyType)
        {
            case EnemyAI3D.EnemyType.Lurker:
                typeColorPrimary   = new Color(0.85f, 0.12f, 0.12f);   // Rojo
                typeColorSecondary = new Color(0.5f,  0.05f, 0.05f);   // Rojo profundo
                eyeColor           = new Color(1f,    0.9f,  0.1f);    // Ojo amarillo
                break;
            case EnemyAI3D.EnemyType.Sploder:
                typeColorPrimary   = new Color(1f,    0.45f, 0.1f);    // Naranja explosivo
                typeColorSecondary = new Color(0.8f,  0.15f, 0.05f);   // Rojo fuego
                eyeColor           = new Color(1f,    0.2f,  0.1f);    // Ojo rojo peligro
                break;
            case EnemyAI3D.EnemyType.Charger:
                typeColorPrimary   = new Color(0.95f, 0.75f, 0.05f);   // Dorado rápido
                typeColorSecondary = new Color(0.7f,  0.4f,  0.0f);    // Dorado oscuro
                eyeColor           = new Color(0.1f,  1f,    1f);      // Ojo cyan
                break;
            case EnemyAI3D.EnemyType.Summoner:
                typeColorPrimary   = new Color(0.55f, 0.15f, 0.75f);   // Púrpura arcano
                typeColorSecondary = new Color(0.3f,  0.05f, 0.45f);   // Violeta oscuro
                eyeColor           = new Color(0.2f,  1f,    0.3f);    // Ojo verde místico
                break;
            case EnemyAI3D.EnemyType.Elite:
                typeColorPrimary   = new Color(0.45f, 0.05f, 0.55f);   // Morado oscuro
                typeColorSecondary = new Color(0.2f,  0.0f,  0.3f);    // Obsidiana
                eyeColor           = new Color(1f,    0.0f,  0.5f);    // Ojo magenta
                break;
            case EnemyAI3D.EnemyType.Boss:
                typeColorPrimary   = new Color(0.9f,  0.05f, 0.12f);   // Carmesí amenazante
                typeColorSecondary = new Color(0.3f,  0.0f,  0.05f);   // Obsidiana oscura
                eyeColor           = new Color(1f,    0.3f,  0.0f);    // Ojo ardiente
                break;
        }
    }

    void BuildVisuals()
    {
        if (ai == null || ai.enemyType == EnemyAI3D.EnemyType.Boss) return;

        // Los prefabs de esqueletos ya traen su malla animada. No superponer
        // el cuerpo procedimental (cápsula/cilindro) sobre el modelo 3D.
        if (GetComponentInChildren<SkinnedMeshRenderer>() != null) return;

        BuildProceduralMobModel();
        Renderer prototypeRenderer = GetComponent<Renderer>();
        if (prototypeRenderer != null) prototypeRenderer.enabled = false;
    }

    void BuildProceduralMobModel()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Color primary = typeColorPrimary;
        Color secondary = typeColorSecondary;
        if (ai.enemyType == EnemyAI3D.EnemyType.Elite) primary = new Color(0.42f, 0.06f, 0.55f);

        CreateMobPart("MobBody", PrimitiveType.Capsule, new Vector3(0f, 0.78f, 0f), new Vector3(0.72f, 0.82f, 0.62f), primary, shader);
        CreateMobPart("MobHead", PrimitiveType.Sphere, new Vector3(0f, 1.62f, 0f), new Vector3(0.58f, 0.55f, 0.58f), secondary, shader);
        CreateMobPart("ArmL", PrimitiveType.Cube, new Vector3(-0.55f, 0.78f, 0f), new Vector3(0.18f, 0.62f, 0.22f), secondary, shader, new Vector3(0f, 0f, -16f));
        CreateMobPart("ArmR", PrimitiveType.Cube, new Vector3(0.55f, 0.78f, 0f), new Vector3(0.18f, 0.62f, 0.22f), secondary, shader, new Vector3(0f, 0f, 16f));

        switch (ai.enemyType)
        {
            case EnemyAI3D.EnemyType.Sploder:
                CreateMobPart("ExplosiveBelly", PrimitiveType.Sphere, new Vector3(0f, 0.68f, 0.32f), new Vector3(0.82f, 0.72f, 0.52f), new Color(1f, 0.2f, 0.04f), shader);
                CreateMobPart("Fuse", PrimitiveType.Cylinder, new Vector3(0f, 1.85f, 0f), new Vector3(0.08f, 0.18f, 0.08f), new Color(1f, 0.7f, 0.05f), shader);
                break;
            case EnemyAI3D.EnemyType.Charger:
                CreateMobPart("HornL", PrimitiveType.Cylinder, new Vector3(-0.27f, 1.95f, 0.16f), new Vector3(0.12f, 0.42f, 0.12f), new Color(0.95f, 0.75f, 0.12f), shader, new Vector3(-24f, 0f, -20f));
                CreateMobPart("HornR", PrimitiveType.Cylinder, new Vector3(0.27f, 1.95f, 0.16f), new Vector3(0.12f, 0.42f, 0.12f), new Color(0.95f, 0.75f, 0.12f), shader, new Vector3(-24f, 0f, 20f));
                CreateMobPart("ArmorPlate", PrimitiveType.Cube, new Vector3(0f, 0.9f, 0.39f), new Vector3(0.68f, 0.38f, 0.08f), new Color(1f, 0.65f, 0.05f), shader);
                break;
            case EnemyAI3D.EnemyType.Summoner:
                CreateMobPart("HatBrim", PrimitiveType.Cylinder, new Vector3(0f, 1.92f, 0f), new Vector3(0.55f, 0.08f, 0.55f), new Color(0.1f, 0.06f, 0.2f), shader);
                CreateMobPart("HatTip", PrimitiveType.Cylinder, new Vector3(0f, 2.25f, 0f), new Vector3(0.30f, 0.45f, 0.30f), secondary, shader, new Vector3(0f, 0f, 5f));
                CreateMobPart("MagicOrb", PrimitiveType.Sphere, new Vector3(0.65f, 1.35f, 0f), new Vector3(0.2f, 0.2f, 0.2f), new Color(0.2f, 1f, 0.45f), shader);
                break;
            case EnemyAI3D.EnemyType.Elite:
                CreateMobPart("ShoulderL", PrimitiveType.Sphere, new Vector3(-0.68f, 1.12f, 0f), new Vector3(0.34f, 0.25f, 0.34f), secondary, shader);
                CreateMobPart("ShoulderR", PrimitiveType.Sphere, new Vector3(0.68f, 1.12f, 0f), new Vector3(0.34f, 0.25f, 0.34f), secondary, shader);
                CreateMobPart("Crown", PrimitiveType.Cylinder, new Vector3(0f, 2.02f, 0f), new Vector3(0.46f, 0.12f, 0.46f), new Color(1f, 0.8f, 0.12f), shader);
                break;
            default:
                CreateMobPart("BackSpikes", PrimitiveType.Cylinder, new Vector3(0f, 1.3f, -0.35f), new Vector3(0.12f, 0.55f, 0.12f), secondary, shader, new Vector3(90f, 0f, 0f));
                break;
        }
    }

    GameObject CreateMobPart(string name, PrimitiveType primitive, Vector3 pos, Vector3 scale, Color color, Shader shader, Vector3 euler = default(Vector3))
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = pos;
        part.transform.localScale = scale;
        part.transform.localEulerAngles = euler;
        Collider col = part.GetComponent<Collider>();
        if (col != null) Destroy(col);
        Renderer renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = RuntimeMaterialCache.Get(shader, color, color * 0.35f);
        return part;
    }

    void BuildBodyDecal()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        switch (ai.enemyType)
        {
            case EnemyAI3D.EnemyType.Lurker:
                // Placa de hombros delgada
                AddDecalPart("Shoulder", sh, typeColorSecondary, new Vector3(0f, 0.8f, 0f), new Vector3(1.0f, 0.12f, 0.8f), Vector3.zero, false);
                // Dos pinchos laterales
                AddDecalPart("SpikeL", sh, typeColorSecondary, new Vector3(-0.55f, 0.95f, 0f), new Vector3(0.12f, 0.55f, 0.12f), new Vector3(0f, 0f, 25f), true);
                AddDecalPart("SpikeR", sh, typeColorSecondary, new Vector3(0.55f, 0.95f, 0f), new Vector3(0.12f, 0.55f, 0.12f), new Vector3(0f, 0f, -25f), true);
                break;

            case EnemyAI3D.EnemyType.Sploder:
                // Barriga abultada (esfera más grande)
                AddDecalPart("Belly", sh, new Color(1f, 0.2f, 0.05f), new Vector3(0f, 0.5f, 0.25f), new Vector3(0.75f, 0.75f, 0.6f), Vector3.zero, true);
                // Luz de advertencia pulsante
                AddDecalPartWithLight("WarningLight", sh, new Color(1f, 0.4f, 0.0f), new Vector3(0f, 0.9f, 0.4f), new Vector3(0.2f, 0.2f, 0.2f), new Color(1f, 0.3f, 0f), 2.5f, 1.5f);
                break;

            case EnemyAI3D.EnemyType.Charger:
                // Cuernos frontales (el Charger embiste)
                AddDecalPart("HornL", sh, typeColorSecondary, new Vector3(-0.25f, 1.1f, 0.35f), new Vector3(0.12f, 0.5f, 0.12f), new Vector3(20f, 0f, -15f), false);
                AddDecalPart("HornR", sh, typeColorSecondary, new Vector3(0.25f, 1.1f, 0.35f), new Vector3(0.12f, 0.5f, 0.12f), new Vector3(20f, 0f, 15f), false);
                // Placa frontal blindada
                AddDecalPart("FrontPlate", sh, typeColorPrimary, new Vector3(0f, 0.7f, 0.38f), new Vector3(0.7f, 0.5f, 0.06f), Vector3.zero, false);
                break;

            case EnemyAI3D.EnemyType.Summoner:
                // Sombrero de mago (cono)
                AddDecalPart("HatBase", sh, typeColorPrimary, new Vector3(0f, 1.65f, 0f), new Vector3(0.65f, 0.1f, 0.65f), Vector3.zero, false);
                AddDecalPart("HatTop", sh, typeColorSecondary, new Vector3(0f, 1.9f, 0f), new Vector3(0.35f, 0.55f, 0.35f), Vector3.zero, false);
                // Orbe mágico flotante
                AddDecalPartWithLight("MagicOrb", sh, new Color(0.3f, 1f, 0.4f), new Vector3(0.6f, 1.2f, 0f), new Vector3(0.25f, 0.25f, 0.25f), new Color(0.2f, 1f, 0.3f), 3f, 1.8f);
                break;

            case EnemyAI3D.EnemyType.Elite:
                // Hombreras de élite (anchas)
                AddDecalPart("ShoulderL", sh, typeColorSecondary, new Vector3(-0.65f, 1.0f, 0f), new Vector3(0.38f, 0.38f, 0.38f), Vector3.zero, true);
                AddDecalPart("ShoulderR", sh, typeColorSecondary, new Vector3(0.65f, 1.0f, 0f), new Vector3(0.38f, 0.38f, 0.38f), Vector3.zero, true);
                // Corona
                AddDecalPart("Crown", sh, new Color(1f, 0.8f, 0.1f), new Vector3(0f, 1.7f, 0f), new Vector3(0.5f, 0.12f, 0.5f), Vector3.zero, false);
                // Núcleo brillante
                AddDecalPartWithLight("CoreGlow", sh, new Color(1f, 0.0f, 0.6f), new Vector3(0f, 0.8f, 0.3f), new Vector3(0.18f, 0.18f, 0.18f), new Color(1f, 0f, 0.5f), 4f, 2.5f);
                break;

            case EnemyAI3D.EnemyType.Boss:
                // Alas laterales masivas
                AddDecalPart("WingL", sh, typeColorSecondary, new Vector3(-1.0f, 0.8f, -0.2f), new Vector3(0.8f, 0.15f, 0.9f), new Vector3(0f, 25f, 10f), false);
                AddDecalPart("WingR", sh, typeColorSecondary, new Vector3(1.0f, 0.8f, -0.2f), new Vector3(0.8f, 0.15f, 0.9f), new Vector3(0f, -25f, -10f), false);
                // Casco
                AddDecalPart("Helm", sh, new Color(0.4f, 0f, 0f), new Vector3(0f, 1.8f, 0f), new Vector3(0.75f, 0.4f, 0.75f), Vector3.zero, true);
                // Núcleo de fuego
                AddDecalPartWithLight("BossCore", sh, new Color(1f, 0.15f, 0f), new Vector3(0f, 0.9f, 0.35f), new Vector3(0.28f, 0.28f, 0.28f), new Color(1f, 0.2f, 0f), 6f, 3.5f);
                // Store first part as bodyDecalObj
                bodyDecalObj = null;
                break;
        }
    }

    /// <summary>Crea una parte decorativa del cuerpo del enemigo.</summary>
    GameObject AddDecalPart(string partName, Shader sh, Color col, Vector3 localPos, Vector3 localScale, Vector3 eulerRot, bool isSphere)
    {
        GameObject part = isSphere
            ? GameObject.CreatePrimitive(PrimitiveType.Sphere)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = partName;
        part.transform.SetParent(transform);
        part.transform.localPosition = localPos;
        part.transform.localScale = localScale;
        part.transform.localEulerAngles = eulerRot;
        Destroy(part.GetComponent<Collider>());

        Renderer r = part.GetComponent<Renderer>();
        if (!s_DecalMatCache.TryGetValue(col, out Material mat) || mat == null)
        {
            mat = RuntimeMaterialCache.Get(sh, col, col * 0.6f);
            s_DecalMatCache[col] = mat;
        }
        r.sharedMaterial = mat;

        if (bodyDecalObj == null) bodyDecalObj = part;
        return part;
    }

    /// <summary>Crea una parte decorativa que también emite luz puntual.</summary>
    void AddDecalPartWithLight(string partName, Shader sh, Color col, Vector3 localPos, Vector3 localScale, Color lightColor, float lightRange, float lightIntensity)
    {
        GameObject part = AddDecalPart(partName, sh, col, localPos, localScale, Vector3.zero, true);

        Light lt = part.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.color = lightColor;
        lt.range = lightRange;
        lt.intensity = lightIntensity;
    }

    void Update()
    {
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

        pulseTimer += Time.deltaTime;

        // El ojo sigue al jugador
        if (eyeObj != null)
        {
            eyeObj.transform.LookAt(player.position + Vector3.up * 1f);
        }

        // Parpadeo del ojo según porcentaje de vida (más rápido = menos vida) usando MaterialPropertyBlock
        if (eyeRenderer != null && health != null)
        {
            float hpPercent = (float)health.currentHealth / health.maxHealth;
            float blinkSpeed = Mathf.Lerp(6f, 1.5f, hpPercent);
            float brightness = 0.6f + Mathf.Abs(Mathf.Sin(pulseTimer * blinkSpeed)) * 1.4f;

            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            eyeRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_EmissionColor", eyeColor * brightness);
            eyeRenderer.SetPropertyBlock(_propBlock);
        }

        // Animación de "alerta" cuando el Elite está cerca del jugador
        if (ai.enemyType == EnemyAI3D.EnemyType.Elite)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist < 6f && bodyDecalObj != null)
            {
                warningBlink += Time.deltaTime * 8f;
                Renderer r = bodyDecalObj.GetComponent<Renderer>();
                if (r != null)
                {
                    float t = Mathf.Abs(Mathf.Sin(warningBlink));
                    if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
                    r.GetPropertyBlock(_propBlock);
                    _propBlock.SetColor("_Color", Color.Lerp(typeColorSecondary, new Color(1f, 0.2f, 0.2f), t));
                    _propBlock.SetColor("_BaseColor", Color.Lerp(typeColorSecondary, new Color(1f, 0.2f, 0.2f), t));
                    r.SetPropertyBlock(_propBlock);
                }
            }
        }
    }

    void OnGUI()
    {
        // Barra de vida flotante sobre los mobs desactivada (vista limpia de modelos 3D)
    }
}
