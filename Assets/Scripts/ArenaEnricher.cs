using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Transforma el escenario en un desierto vivo de día, con ruinas, arena y cactus:
/// - Reemplaza los cubos/barricadas primitivas por ruinas, columnas y muros auténticos (Altar_Ruins_FREE).
/// - Altar central ceremonial con antorchas de fuego místico (VFXPACK_FIRE_WALLCOEUR).
/// - Anillo perimetral de acantilados rocosos y árboles secos (Ultimate Nature / Polytope Studio).
/// - Terreno con texturas de piedra envejecida y mapa de normales.
/// - Cielo nocturno estrellado y con luna (AllSkyFree).
/// </summary>
public partial class ArenaEnricher : MonoBehaviour
{
    public static ArenaEnricher Instance { get; private set; }
    MeshCollider[] snowColliders;

    [Header("Skybox & Atmósfera")]
    public Material skyboxMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    void Awake() { Instance = this; }
    void OnDestroy() { if (forestFloorTexture != null) Destroy(forestFloorTexture); if (Instance == this) Instance = null; }

    void Start()
    {
        ApplyMap();
    }

    private bool isProceduralDesert = true;
    private bool isForestMap;
    private bool isSnowMap;

    public void ApplyMap()
    {
        snowColliders = null;
        isSnowMap = GameManager.Instance != null && GameManager.Instance.selectedMap == GameManager.MapType.SnowyMountain;
        GameObject baseGround = GameObject.Find("Ground");
        if (baseGround != null) { var c = baseGround.GetComponent<Collider>(); if (c != null) c.enabled = false; var r = baseGround.GetComponent<Renderer>(); if (r != null) r.enabled = false; }
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject oldScenery = transform.GetChild(i).gameObject;
            oldScenery.SetActive(false);
            Destroy(oldScenery);
        }
        HidePrimitivePrototypes();
        isForestMap = GameManager.Instance != null && GameManager.Instance.selectedMap == GameManager.MapType.Forest;
        if (!isForestMap && !isSnowMap && OriginalContentCatalog.Desert != null)
        { BuildOriginalDesert(); return; }
        if (GameManager.Instance != null && GameManager.Instance.selectedMap == GameManager.MapType.SnowyMountain)
        {
            isProceduralDesert = false;
            ConfigureSnowAtmosphere();
            BuildProceduralTerrain();
            BuildSnowyMountain();
            BuildAbandonedSanctuary();
            return;
        }
        isProceduralDesert = true;
        ConfigureAtmosphere();
        BuildProceduralTerrain();
        BuildAbandonedSanctuary();
        if (isForestMap) BuildForestDetails();
        else BuildDesertDetails();
        if (!isForestMap) BuildNatureBorders();
        CreateAtmosphericParticles();
        ExtractedStageCatalog.ApplyAtmosphere();
    }

    void ConfigureSnowAtmosphere()
    {
        RenderSettings.fog = false;
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0.48f, 0.70f, 0.88f);
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.68f, 0.82f, 0.96f);
        RenderSettings.ambientEquatorColor = new Color(0.78f, 0.87f, 0.94f);
        RenderSettings.ambientGroundColor = new Color(0.40f, 0.54f, 0.67f);
        foreach (Light light in Object.FindObjectsByType<Light>())
        {
            if (light.type != LightType.Directional) continue;
            light.color = new Color(0.84f, 0.92f, 1f);
            light.intensity = 1.35f;
            light.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        }

        GameObject ground = GameObject.Find("Ground");
        Renderer groundRenderer = ground != null ? ground.GetComponent<Renderer>() : null;
        if (ground != null) ground.transform.position = new Vector3(0f, -3f, 0f);
        if (groundRenderer != null) groundRenderer.enabled = false;
    }

    public bool TryGetSnowHeight(Vector3 position, out float height)
    {
        height = 0f;
        if (isProceduralDesert)
        {
            float distSq = position.x * position.x + position.z * position.z;
            if (distSq > 228f * 228f) return false;
            height = GetProceduralHeight(position.x, position.z);
            return true;
        }

        float playableRadius = isForestMap ? 348f : 228f;
        if (position.x * position.x + position.z * position.z > playableRadius * playableRadius) return false;
        if (snowColliders == null || snowColliders.Length == 0) return false;
        float highest = float.NegativeInfinity;
        foreach (MeshCollider collider in snowColliders)
        {
            if (collider == null || !collider.enabled) continue;
            Bounds bounds = collider.bounds;
            if (position.x < bounds.min.x || position.x > bounds.max.x ||
                position.z < bounds.min.z || position.z > bounds.max.z) continue;
            Ray ray = new Ray(new Vector3(position.x, bounds.max.y + 2f, position.z), Vector3.down);
            bool found = collider.Raycast(ray, out RaycastHit hit, bounds.size.y + 4f);
            if (!found)
            {
                // Algunos triángulos del FBX miran hacia abajo: consultar su reverso desde abajo.
                ray = new Ray(new Vector3(position.x, bounds.min.y - 2f, position.z), Vector3.up);
                found = collider.Raycast(ray, out hit, bounds.size.y + 4f);
            }
            if (found && Mathf.Abs(hit.normal.y) >= 0.35f && hit.point.y > highest)
                highest = hit.point.y;
        }
        if (float.IsNegativeInfinity(highest)) return false;
        height = highest;
        return true;
    }

    public Vector3 PlaceOnSnow(Vector3 position, float heightOffset = 0f)
    {
        if (TryGetSnowHeight(position, out float height)) position.y = height + heightOffset;
        return position;
    }

    public Vector3 PlaceOnGround(Vector3 position, float heightOffset = 0f)
    {
        return PlaceOnSnow(position, heightOffset);
    }

    public bool TryFindWalkablePosition(Vector3 desired, out Vector3 result, float heightOffset = 0f, float searchRadius = 18f)
    {
        desired.y = 0f;
        if (TryGetSnowHeight(desired, out float directHeight))
        {
            result = new Vector3(desired.x, directHeight + heightOffset, desired.z);
            return true;
        }

        const int directions = 12;
        for (int ring = 1; ring <= 3; ring++)
        {
            float radius = searchRadius * ring / 3f;
            for (int i = 0; i < directions; i++)
            {
                float angle = i * Mathf.PI * 2f / directions;
                Vector3 candidate = desired + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                if (!TryGetSnowHeight(candidate, out float height)) continue;
                result = new Vector3(candidate.x, height + heightOffset, candidate.z);
                return true;
            }
        }
        result = desired;
        return false;
    }

    void BuildSnowyMountain()
    {
        GameObject mountain = Resources.Load<GameObject>("SnowyMountain/SnowyMountain");
        if (mountain == null) return;
        GameObject backdrop = Instantiate(mountain, transform);
        backdrop.name = "SnowMountainPlayable";
        Bounds bounds = CalculateRenderBounds(backdrop);
        float width = Mathf.Max(bounds.size.x, bounds.size.z);
        if (width > .01f) backdrop.transform.localScale *= 430f / width;
        bounds = CalculateRenderBounds(backdrop);
        backdrop.transform.position += new Vector3(-bounds.center.x, -bounds.min.y - 18f, -bounds.center.z);
        var walkable = new List<MeshCollider>();
        foreach (var oldCollider in backdrop.GetComponentsInChildren<Collider>()) oldCollider.enabled = false;
        foreach (var filter in backdrop.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null) continue;
            var collider = filter.GetComponent<MeshCollider>();
            if (collider == null) collider = filter.gameObject.AddComponent<MeshCollider>();
            if (collider == null) continue;
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
            collider.enabled = true;
            walkable.Add(collider);
        }
        if (walkable.Count > 0)
        {
            snowColliders[0].transform.localPosition = Vector3.down * 20f;
            walkable.AddRange(snowColliders);
            snowColliders = walkable.ToArray();
        }
        else Debug.LogWarning("La montaña importada no tiene mallas transitables; se conserva el terreno nevado generado.");
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader);
        Texture2D tex = Resources.Load<Texture2D>("SnowyMountain/Textures/snowy_mountain_texture_00");
        mat.mainTexture = tex;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        foreach (var renderer in backdrop.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = mat;
        Physics.SyncTransforms();
        if (Camera.main != null) Camera.main.farClipPlane = 1600f;
    }

    static Bounds CalculateRenderBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.one);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    /// <summary>
    /// Desactiva los cubos y rampas grises prototipo para que el jugador solo vea modelos 3D detallados.
    /// </summary>
    void HidePrimitivePrototypes()
    {
        string[] primitiveNames = new string[] {
            "Cover_ColumnC", "Cover_BarricadeA", "Cover_LongWallB", "Cover_LongWallA",
            "Cover_CenterBlock", "Cover_CrateB", "Cover_BarricadeB", "Cover_ColumnB",
            "Cover_CrateA", "Cover_ColumnA", "Wall_North", "Wall_South", "Wall_East", "Wall_West",
            "Ramp_A", "Ramp_B"
        };

        foreach (string name in primitiveNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    void ConfigureAtmosphere()
    {
        GameObject existingGround = GameObject.Find("Ground");
        if (existingGround != null)
        {
            Renderer existingGroundRenderer = existingGround.GetComponent<Renderer>();
            if (existingGroundRenderer != null) existingGroundRenderer.enabled = false;
            Collider existingGroundCollider = existingGround.GetComponent<Collider>();
            if (existingGroundCollider != null) existingGroundCollider.enabled = false;
        }

        // 1. Cielo diurno claro, cálido y sin niebla.
        Material desertSky = skyboxMaterial;
        if (desertSky == null)
        {
#if UNITY_EDITOR
            desertSky = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/AllSkyFree/Cartoon Base BlueSky/Day_BlueSky_Nothing.mat");
            if (desertSky == null)
                desertSky = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/AllSkyFree/Epic_BlueSunset/Epic_BlueSunset.mat");
#endif
        }

        if (desertSky != null)
        {
            RenderSettings.skybox = desertSky;
        }

        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.Skybox;
        }

        // 2. Sol alto y cálido.
        foreach (Light light in Object.FindObjectsByType<Light>())
        {
            if (light.type != LightType.Directional) continue;
            light.color = isForestMap ? new Color(0.86f, 0.96f, 0.82f) : new Color(1f, 0.82f, 0.56f);
            light.intensity = isForestMap ? 1.25f : 1.55f;
            light.shadowStrength = 0.62f;
            light.transform.rotation = Quaternion.Euler(58f, -28f, 0f);
        }

        // 3. Iluminación ambiental saturada de mediodía.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = isForestMap ? new Color(0.46f, 0.70f, 0.78f) : new Color(0.50f, 0.76f, 0.95f);
        RenderSettings.ambientEquatorColor = isForestMap ? new Color(0.35f, 0.55f, 0.30f) : new Color(0.92f, 0.60f, 0.32f);
        RenderSettings.ambientGroundColor = isForestMap ? new Color(0.12f, 0.25f, 0.10f) : new Color(0.48f, 0.22f, 0.08f);
        RenderSettings.fog = false;
    }

    public static float GetProceduralHeight(float x, float z)
    {
        if (Instance != null && Instance.isForestMap) return ForestHeight(x, z);
        if (Instance != null && !Instance.isSnowMap && OriginalContentCatalog.Desert != null)
            return OriginalDesertHeight(x, z, OriginalContentCatalog.Desert);
        float distSq = x * x + z * z;
        float dist = Mathf.Sqrt(distSq);

        // Borde exterior: eleva suavemente hacia acantilados montañosos perimetrales
        float rimHeight = 0f;
        if (dist > 185f)
        {
            float rimFactor = Mathf.Clamp01((dist - 185f) / 40f);
            rimHeight = rimFactor * rimFactor * 14f;
        }

        // Altar y santuario central: zona plana ceremonial para combate limpio
        if (dist < 32f) return 0f;

        // Suavizado entre el centro plano y el relieve natural
        float centerBlend = Mathf.Clamp01((dist - 32f) / 20f);
        centerBlend = centerBlend * centerBlend * (3f - 2f * centerBlend); // Smoothstep

        // Outposts en los 4 puntos cardinales
        // Norte (0, 110)
        float dNorth = Mathf.Sqrt(x * x + (z - 110f) * (z - 110f));
        float fNorth = Mathf.Clamp01(dNorth / 24f);
        fNorth = fNorth * fNorth * (3f - 2f * fNorth);

        // Sur (0, -110)
        float dSouth = Mathf.Sqrt(x * x + (z + 110f) * (z + 110f));
        float fSouth = Mathf.Clamp01(dSouth / 24f);
        fSouth = fSouth * fSouth * (3f - 2f * fSouth);

        // Este (115, 0)
        float dEast = Mathf.Sqrt((x - 115f) * (x - 115f) + z * z);
        float fEast = Mathf.Clamp01(dEast / 24f);
        fEast = fEast * fEast * (3f - 2f * fEast);

        // Oeste (-115, 0)
        float dWest = Mathf.Sqrt((x + 115f) * (x + 115f) + z * z);
        float fWest = Mathf.Clamp01(dWest / 24f);
        fWest = fWest * fWest * (3f - 2f * fWest);

        // Múltiples octavas de ruido Perlin para dunas y colinas onduladas vivas
        float octave1 = (Mathf.PerlinNoise((x + 400f) * 0.012f, (z + 400f) * 0.012f) - 0.5f) * 7.5f;
        float octave2 = (Mathf.PerlinNoise((x + 850f) * 0.028f, (z + 180f) * 0.028f) - 0.5f) * 3.2f;
        float octave3 = (Mathf.PerlinNoise((x + 150f) * 0.065f, (z + 700f) * 0.065f) - 0.5f) * 1.0f;

        float dunes = (octave1 + octave2 + octave3) * centerBlend;
        dunes *= fNorth * fSouth * fEast * fWest;

        return dunes + rimHeight;
    }

    void BuildProceduralTerrain()
    {
        GameObject terrainObj = new GameObject(isSnowMap ? "SnowWalkableTerrain" : isForestMap ? "ForestTerrain" : "DesertTerrain");
        terrainObj.transform.SetParent(transform, false);

        int resolution = isForestMap ? 175 : 115;
        float size = isForestMap ? 700f : 460f;
        float halfSize = size * 0.5f;
        float step = size / resolution;

        int vertCount = (resolution + 1) * (resolution + 1);
        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] triangles = new int[resolution * resolution * 6];

        for (int z = 0; z <= resolution; z++)
        {
            float posZ = -halfSize + z * step;
            for (int x = 0; x <= resolution; x++)
            {
                float posX = -halfSize + x * step;
                int idx = z * (resolution + 1) + x;
                float posY = GetProceduralHeight(posX, posZ);
                vertices[idx] = new Vector3(posX, posY, posZ);
                uvs[idx] = isForestMap ? new Vector2(posX / size + .5f, posZ / size + .5f) : new Vector2(posX * 0.08f, posZ * 0.08f);
            }
        }

        int triIdx = 0;
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int bottomLeft = z * (resolution + 1) + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = (z + 1) * (resolution + 1) + x;
                int topRight = topLeft + 1;

                triangles[triIdx++] = bottomLeft;
                triangles[triIdx++] = topLeft;
                triangles[triIdx++] = bottomRight;

                triangles[triIdx++] = bottomRight;
                triangles[triIdx++] = topLeft;
                triangles[triIdx++] = topRight;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "GiantDesertMesh";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter mf = terrainObj.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = terrainObj.AddComponent<MeshRenderer>();
        Material desertMat = null;
#if UNITY_EDITOR
        desertMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Altar_Ruins_FREE/Art/Textures/Old/Material/Plane001Mat.mat");
#endif
        if (desertMat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            desertMat = new Material(shader);
            desertMat.color = new Color(0.92f, 0.62f, 0.28f);
        }
        else
        {
            desertMat = new Material(desertMat);
        }
        Color groundColor = isSnowMap ? new Color(.84f, .91f, .97f) : isForestMap ? new Color(0.27f, 0.48f, 0.22f) : new Color(0.95f, 0.85f, 0.72f);
        desertMat.color = groundColor;
        if (desertMat.HasProperty("_BaseColor")) desertMat.SetColor("_BaseColor", groundColor);
        Texture2D groundTexture = Resources.Load<Texture2D>(isForestMap ? "Terrain/ForestGround" : "Terrain/DesertGround");
        if (isSnowMap) groundTexture = null;
        if (groundTexture != null || isSnowMap)
        {
            desertMat.mainTexture = groundTexture;
            if (desertMat.HasProperty("_BaseMap")) desertMat.SetTexture("_BaseMap", groundTexture);
        }
        desertMat.mainTextureScale = Vector2.one;
        if (desertMat.HasProperty("_BaseMap")) desertMat.SetTextureScale("_BaseMap", Vector2.one);
        if (desertMat.HasProperty("_BumpMap")) desertMat.SetTextureScale("_BumpMap", Vector2.one);
        mr.sharedMaterial = desertMat;
        if (isForestMap) ConfigureForestFloor(desertMat);
        var stageStyle = ExtractedStageCatalog.Current();
        if (stageStyle != null && stageStyle.floor != null)
        {
            var extractedFloor = new Material(stageStyle.floor);
            extractedFloor.mainTextureScale = new Vector2(32, 32);
            mr.sharedMaterial = extractedFloor;
            Destroy(desertMat);
        }

        MeshCollider mc = terrainObj.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = false;
        terrainObj.isStatic = true;
        snowColliders = new[] { mc };
        isProceduralDesert = false;
        Physics.SyncTransforms();
    }

    void BuildAbandonedSanctuary()
    {
        GameObject sanctuaryRoot = new GameObject("AbandonedSanctuary");
        sanctuaryRoot.transform.SetParent(transform, false);

        // ════════════ 1. SANTUARIO CENTRAL (R: 0 - 38m) ════════════
        GameObject platformCirclePrefab = FindPrefabByName("Platform_circle");
        GameObject altarPrefab = FindPrefabByName("Altar_2") ?? FindPrefabByName("Altar_1");

        if (platformCirclePrefab != null)
            SpawnScenery(platformCirclePrefab, Vector3.zero, Quaternion.identity, new Vector3(1.6f, 1.0f, 1.6f), sanctuaryRoot.transform);

        if (altarPrefab != null)
        {
            GameObject altar = SpawnScenery(altarPrefab, new Vector3(0f, 0.45f, 0f), Quaternion.identity, Vector3.one * 1.35f, sanctuaryRoot.transform);
            GameObject torchPrefab = FindPrefabByName("VFX_TorchLight_Green") ?? FindPrefabByName("VFX_TorchLight");
            if (torchPrefab != null)
            {
                GameObject torch = Instantiate(torchPrefab, altar.transform);
                torch.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            }

            GameObject flameLight = new GameObject("AltarLight");
            flameLight.transform.SetParent(altar.transform, false);
            flameLight.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            Light fl = flameLight.AddComponent<Light>();
            fl.type = LightType.Point;
            fl.color = new Color(0.25f, 0.95f, 0.5f);
            fl.range = 28f;
            fl.intensity = 4.2f;
        }

        GameObject brokenColumnPrefab = FindPrefabByName("Broken_column");
        GameObject smallColumnPrefab = FindPrefabByName("Column_broken_small");
        GameObject columnPrefab = FindPrefabByName("Column");
        GameObject stonePostPrefab = FindPrefabByName("Stone_post_2");

        Vector3[] centralPillars = new Vector3[] {
            new Vector3( 10f, 0f,  10f), new Vector3(-10f, 0f,  10f),
            new Vector3( 10f, 0f, -10f), new Vector3(-10f, 0f, -10f),
            new Vector3( 20f, 0f,   0f), new Vector3(-20f, 0f,   0f),
            new Vector3(  0f, 0f,  20f), new Vector3(  0f, 0f, -20f)
        };
        for (int i = 0; i < centralPillars.Length; i++)
        {
            GameObject p = (i % 2 == 0 && brokenColumnPrefab != null) ? brokenColumnPrefab : smallColumnPrefab;
            if (p == null) p = stonePostPrefab;
            if (p != null)
                SpawnScenery(p, centralPillars[i], Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * 1.15f, sanctuaryRoot.transform);
        }

        GameObject wallBroken1 = FindPrefabByName("Wall_broken_1");
        GameObject wallBroken2 = FindPrefabByName("Wall_broken_2");
        GameObject wallRound2 = FindPrefabByName("Wall_round_2");
        GameObject stoneStruct = FindPrefabByName("Stone_structure");

        Vector3[] centralCovers = new Vector3[] {
            new Vector3( 18f, 0f,  18f), new Vector3(-18f, 0f,  18f),
            new Vector3( 18f, 0f, -18f), new Vector3(-18f, 0f, -18f),
            new Vector3( 28f, 0f,   8f), new Vector3(-28f, 0f,  -8f),
            new Vector3(  8f, 0f,  28f), new Vector3( -8f, 0f, -28f)
        };
        for (int i = 0; i < centralCovers.Length; i++)
        {
            GameObject wp = (i % 3 == 0 && wallRound2 != null) ? wallRound2 : ((i % 3 == 1 && stoneStruct != null) ? stoneStruct : wallBroken1);
            if (wp != null)
                SpawnScenery(wp, centralCovers[i], Quaternion.Euler(0f, (i * 45f), 0f), Vector3.one * 1.05f, sanctuaryRoot.transform);
        }

        GameObject gatePrefab = FindPrefabByName("Gate");
        GameObject brokenGatePrefab = FindPrefabByName("Gate_broken");
        GameObject archPrefab = FindPrefabByName("Arch_1");

        if (gatePrefab != null)
            SpawnScenery(gatePrefab, new Vector3(0f, 0f, 38f), Quaternion.identity, Vector3.one * 1.15f, sanctuaryRoot.transform);
        if (brokenGatePrefab != null)
            SpawnScenery(brokenGatePrefab, new Vector3(0f, 0f, -38f), Quaternion.identity, Vector3.one * 1.15f, sanctuaryRoot.transform);
        if (archPrefab != null)
        {
            SpawnScenery(archPrefab, new Vector3(38f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.15f, sanctuaryRoot.transform);
            SpawnScenery(archPrefab, new Vector3(-38f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.15f, sanctuaryRoot.transform);
        }

        // ════════════ 2. SECTOR NORTE: BASTIÓN DE VIGILANCIA (0, 0, 110) ════════════
        GameObject towerA = FindPrefabByName("Tower_A");
        if (towerA != null)
            SpawnScenery(towerA, new Vector3(0f, 0f, 122f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.35f, sanctuaryRoot.transform);
        if (archPrefab != null)
            SpawnScenery(archPrefab, new Vector3(0f, 0f, 96f), Quaternion.identity, Vector3.one * 1.2f, sanctuaryRoot.transform);
        if (wallBroken1 != null)
            SpawnScenery(wallBroken1, new Vector3(-14f, 0f, 110f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.1f, sanctuaryRoot.transform);
        if (wallBroken2 != null)
            SpawnScenery(wallBroken2, new Vector3(14f, 0f, 110f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.1f, sanctuaryRoot.transform);
        if (columnPrefab != null)
        {
            SpawnScenery(columnPrefab, new Vector3(-8f, 0f, 102f), Quaternion.identity, Vector3.one * 1.1f, sanctuaryRoot.transform);
            SpawnScenery(columnPrefab, new Vector3(8f, 0f, 102f), Quaternion.identity, Vector3.one * 1.1f, sanctuaryRoot.transform);
        }
        GameObject rubble1 = FindPrefabByName("Rubble_1") ?? FindPrefabByName("Rubble_2");
        if (rubble1 != null)
            SpawnScenery(rubble1, new Vector3(-6f, 0f, 116f), Quaternion.identity, Vector3.one * 1.2f, sanctuaryRoot.transform);

        // ════════════ 3. SECTOR SUR: COLISEO HUNDIDO (0, 0, -110) ════════════
        GameObject wallRound1 = FindPrefabByName("Wall_round_1") ?? wallRound2;
        GameObject wallRound3 = FindPrefabByName("Wall_round_3") ?? wallRound2;
        GameObject stoneSlab1 = FindPrefabByName("Stone_slab_1");
        if (wallRound1 != null)
        {
            SpawnScenery(wallRound1, new Vector3(-16f, 0f, -104f), Quaternion.Euler(0f, 45f, 0f), Vector3.one * 1.15f, sanctuaryRoot.transform);
            SpawnScenery(wallRound1, new Vector3(16f, 0f, -104f), Quaternion.Euler(0f, -45f, 0f), Vector3.one * 1.15f, sanctuaryRoot.transform);
        }
        if (wallRound3 != null)
        {
            SpawnScenery(wallRound3, new Vector3(-14f, 0f, -118f), Quaternion.Euler(0f, 135f, 0f), Vector3.one * 1.15f, sanctuaryRoot.transform);
            SpawnScenery(wallRound3, new Vector3(14f, 0f, -118f), Quaternion.Euler(0f, -135f, 0f), Vector3.one * 1.15f, sanctuaryRoot.transform);
        }
        if (brokenColumnPrefab != null)
        {
            SpawnScenery(brokenColumnPrefab, new Vector3(-10f, 0f, -110f), Quaternion.identity, Vector3.one * 1.15f, sanctuaryRoot.transform);
            SpawnScenery(brokenColumnPrefab, new Vector3(10f, 0f, -110f), Quaternion.identity, Vector3.one * 1.15f, sanctuaryRoot.transform);
        }
        if (stoneSlab1 != null)
            SpawnScenery(stoneSlab1, new Vector3(0f, 0f, -110f), Quaternion.identity, Vector3.one * 1.3f, sanctuaryRoot.transform);

        // ════════════ 4. SECTOR ESTE: CÍRCULO DE MENHIRES MÍSTICOS (115, 0, 0) ════════════
        GameObject menhirPrefab = FindPrefabByName("PT_Menhir_Rock_02") ?? stonePostPrefab;
        if (menhirPrefab != null)
        {
            for (int i = 0; i < 6; i++)
            {
                float ang = i * 60f * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(115f + Mathf.Cos(ang) * 14f, 0f, Mathf.Sin(ang) * 14f);
                SpawnScenery(menhirPrefab, pos, Quaternion.Euler(0f, i * 60f + 90f, 0f), Vector3.one * 1.35f, sanctuaryRoot.transform);
            }
        }
        GameObject stoneSlab2 = FindPrefabByName("Stone_slab_2") ?? stoneSlab1;
        if (stoneSlab2 != null)
            SpawnScenery(stoneSlab2, new Vector3(115f, 0f, 0f), Quaternion.identity, Vector3.one * 1.25f, sanctuaryRoot.transform);
        GameObject deadPineTree = FindPrefabByName("PT_Pine_Tree_03_dead");
        if (deadPineTree != null)
        {
            SpawnScenery(deadPineTree, new Vector3(126f, 0f, 12f), Quaternion.identity, Vector3.one * 1.2f, sanctuaryRoot.transform);
            SpawnScenery(deadPineTree, new Vector3(105f, 0f, -12f), Quaternion.identity, Vector3.one * 1.1f, sanctuaryRoot.transform);
        }

        // ════════════ 5. SECTOR OESTE: NECRÓPOLIS Y FORJA FORTIFICADA (-115, 0, 0) ════════════
        GameObject towerB = FindPrefabByName("Tower_B");
        if (towerB != null)
            SpawnScenery(towerB, new Vector3(-124f, 0f, -6f), Quaternion.Euler(0f, 75f, 0f), Vector3.one * 1.35f, sanctuaryRoot.transform);
        if (stoneStruct != null)
            SpawnScenery(stoneStruct, new Vector3(-110f, 0f, 8f), Quaternion.identity, Vector3.one * 1.2f, sanctuaryRoot.transform);
        if (brokenGatePrefab != null)
            SpawnScenery(brokenGatePrefab, new Vector3(-98f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.15f, sanctuaryRoot.transform);
        if (wallBroken1 != null)
            SpawnScenery(wallBroken1, new Vector3(-115f, 0f, 18f), Quaternion.identity, Vector3.one * 1.1f, sanctuaryRoot.transform);
    }

    void BuildDesertDetails()
    {
        GameObject desertRoot = new GameObject("VividDesertDetails");
        desertRoot.transform.SetParent(transform, false);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material cactus = RuntimeMaterialCache.Get(shader, new Color(0.12f, 0.42f, 0.16f), new Color(0.01f, 0.05f, 0.01f));

        // Prefabs de rocas y árboles naturales
        GameObject rockStd = FindPrefabByName("UNS_Standard_Rock_01") ?? FindPrefabByName("UNS_Standard_Rock_02");
        GameObject rockStd2 = FindPrefabByName("UNS_Standard_Rock_03");
        GameObject genericRock = FindPrefabByName("PT_Generic_Rock_01");
        GameObject deadPine = FindPrefabByName("PT_Pine_Tree_03_dead");
        GameObject spruce = FindPrefabByName("UNS_Spruce_01");

        // 1. Cactus a lo largo y ancho de las dunas gigantes (65 agrupaciones)
        for (int i = 0; i < 65; i++)
        {
            float angle = (i * (360f / 65f) + Random.Range(-4f, 4f)) * Mathf.Deg2Rad;
            float radius = Random.Range(36f, 185f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            CreateCactus(pos, cactus, desertRoot.transform);
        }

        // 2. Rocas y formaciones tácticas naturales por las dunas (55 agrupaciones)
        for (int i = 0; i < 55; i++)
        {
            float angle = (i * (360f / 55f) + Random.Range(-6f, 6f)) * Mathf.Deg2Rad;
            float radius = Random.Range(38f, 185f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            GameObject rockPrefab = (i % 3 == 0 && genericRock != null) ? genericRock : ((i % 3 == 1 && rockStd2 != null) ? rockStd2 : rockStd);
            if (rockPrefab != null)
                SpawnScenery(rockPrefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * Random.Range(1.2f, 2.2f), desertRoot.transform);
        }

        // 3. Arboles secos dispersos en crestas y oasis (28 árboles)
        for (int i = 0; i < 28; i++)
        {
            float angle = (i * (360f / 28f) + Random.Range(-8f, 8f)) * Mathf.Deg2Rad;
            float radius = Random.Range(45f, 180f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            GameObject treePrefab = (i % 2 == 0 && deadPine != null) ? deadPine : spruce;
            if (treePrefab != null)
                SpawnScenery(treePrefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * Random.Range(1.1f, 1.6f), desertRoot.transform);
        }
    }

    void BuildForestDetails() { BuildDesignedForest(); }

    void SpawnVisualDecoration(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale, Transform parent)
    {
        position = PlaceOnGround(position);
        GameObject decoration = Instantiate(prefab, position, rotation * prefab.transform.rotation, parent);
        decoration.transform.localScale = Vector3.Scale(prefab.transform.localScale, scale);
        Bounds decorationBounds = CalculateRenderBounds(decoration);
        decoration.transform.position += Vector3.up * (position.y - decorationBounds.min.y);
        foreach (Collider collider in decoration.GetComponentsInChildren<Collider>()) collider.enabled = false;
        decoration.isStatic = true;
    }

    void CreateCactus(Vector3 position, Material material, Transform parent)
    {
        position = PlaceOnSnow(position);
        CreateDesertPrimitive("Cactus", PrimitiveType.Cylinder, position + Vector3.up * 1.5f, new Vector3(0.35f, 1.5f, 0.35f), material, parent);
        // Los brazos comparten raíz con el tronco; si son hijos del cilindro
        // heredan su escala no uniforme y terminan desplazados o aplastados.
        CreateDesertPrimitive("CactusArm1", PrimitiveType.Cylinder, position + new Vector3(0.52f, 1.75f, 0f), new Vector3(0.20f, 0.65f, 0.20f), material, parent).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        CreateDesertPrimitive("CactusArm2", PrimitiveType.Cylinder, position + new Vector3(-0.48f, 1.45f, 0f), new Vector3(0.18f, 0.55f, 0.18f), material, parent).transform.rotation = Quaternion.Euler(0f, 0f, -90f);
    }

    GameObject CreateDesertPrimitive(string objectName, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(primitive);
        obj.name = objectName;
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.transform.localScale = scale;
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        return obj;
    }

    void BuildNatureBorders()
    {
        GameObject borderRoot = new GameObject("NatureBorders");
        borderRoot.transform.SetParent(transform, false);

        GameObject cliff1 = FindPrefabByName("UNS_Rock_Cliff_01");
        GameObject cliff2 = FindPrefabByName("UNS_Rock_Cliff_02");
        GameObject cliff3 = FindPrefabByName("UNS_Rock_Cliff_03");
        GameObject cliff4 = FindPrefabByName("UNS_Rock_Cliff_04");
        GameObject mountain = FindPrefabByName("UNS_Mountain");
        GameObject deadPine = FindPrefabByName("PT_Pine_Tree_03_dead");
        GameObject spruce = FindPrefabByName("UNS_Spruce_01");

        // Anillo perimetral exterior completo (radio 195m - 225m) con 36 acantilados colosales
        int segments = 36;
        for (int i = 0; i < segments; i++)
        {
            float angle = (i * (360f / segments)) * Mathf.Deg2Rad;
            float dist = Random.Range(195f, 222f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            int choice = i % 5;
            switch (choice)
            {
                case 0:
                    GameObject c0 = cliff1 ?? cliff2 ?? cliff3;
                    if (c0 != null)
                        SpawnScenery(c0, pos, rot, Vector3.one * Random.Range(3.0f, 4.5f), borderRoot.transform);
                    break;
                case 1:
                    GameObject c1 = mountain ?? cliff4 ?? cliff3;
                    if (c1 != null)
                        SpawnScenery(c1, pos, rot, Vector3.one * Random.Range(2.8f, 4.2f), borderRoot.transform);
                    break;
                case 2:
                    GameObject c2 = cliff2 ?? cliff1;
                    if (c2 != null)
                        SpawnScenery(c2, pos, rot, Vector3.one * Random.Range(3.0f, 4.5f), borderRoot.transform);
                    break;
                case 3:
                    GameObject c3 = cliff3 ?? cliff4;
                    if (c3 != null)
                        SpawnScenery(c3, pos, rot, Vector3.one * Random.Range(2.8f, 4.0f), borderRoot.transform);
                    break;
                case 4:
                    if (deadPine != null)
                        SpawnScenery(deadPine, pos, rot, Vector3.one * Random.Range(2.0f, 2.8f), borderRoot.transform);
                    else if (spruce != null)
                        SpawnScenery(spruce, pos, rot, Vector3.one * Random.Range(1.8f, 2.5f), borderRoot.transform);
                    break;
            }
        }
    }

    void CreateAtmosphericParticles()
    {
        GameObject starsPrefab = FindPrefabByName("Stars");
        if (starsPrefab != null)
        {
            GameObject stars = Instantiate(starsPrefab, new Vector3(0f, 2.5f, 0f), Quaternion.identity, transform);
            stars.name = "RuinsStarsParticles";
        }
    }

    GameObject SpawnScenery(GameObject prefab, Vector3 pos, Quaternion rot, Vector3 scale, Transform parent)
    {
        if (prefab == null) return null;
        pos = PlaceOnSnow(pos);
        GameObject obj = Instantiate(prefab, pos, rot * prefab.transform.rotation, parent);
        obj.transform.localScale = Vector3.Scale(prefab.transform.localScale, scale);
        Bounds visualBounds = CalculateRenderBounds(obj);
        obj.transform.position += Vector3.up * (pos.y - visualBounds.min.y);
        obj.isStatic = true;

        // Asegurar colisionador para cobertura física
        if (obj.GetComponent<Collider>() == null && obj.GetComponentInChildren<Collider>() == null)
        {
            MeshFilter mf = obj.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
                AddSceneryCollider(mf);
            else
            {
                var bc = obj.AddComponent<BoxCollider>();
                bc.size = new Vector3(2.5f, 3.5f, 2.5f);
                bc.center = new Vector3(0f, 1.75f, 0f);
            }
        }

        return obj;
    }

    static void AddSceneryCollider(MeshFilter filter)
    {
        Mesh mesh = filter.sharedMesh;
        if (mesh.isReadable)
            filter.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        else
        {
            // Imported visual meshes may drop CPU data in a player build.
            var box = filter.gameObject.AddComponent<BoxCollider>();
            box.center = mesh.bounds.center;
            box.size = Vector3.Max(mesh.bounds.size, Vector3.one * .05f);
        }
    }

    private static readonly Dictionary<string, GameObject> s_PrefabCache = new Dictionary<string, GameObject>();

    private static GameObject FindPrefabByName(string name)
    {
        string importedName = name == "PT_Pine_Tree_03_green" ? "Tree0" :
            name == "UNS_Spruce_02" ? "Tree2" :
            name == "PT_Fruit_Tree_01_green" ? "Tree0Forest2" :
            name == "PT_Fruit_Tree_01_apples" ? "Tree1Forest2" :
            name == "UNS_Bush" ? "Bush0" :
            name == "PT_Pine_Tree_03_logs" ? "Log0" :
            name == "PT_Pine_Tree_03_dead" ? "DeadTree0" : null;
        if (importedName != null)
        {
            var imported = Resources.Load<GameObject>("ExtractedVisuals/GameObject/" + importedName);
            if (imported != null) return imported;
        }
        if (s_PrefabCache.TryGetValue(name, out GameObject cached) && cached != null)
            return cached;

        var library = Resources.Load<ForestAssetLibrary>("Forest/ForestAssetLibrary");
        if (library != null && library.prefabs != null)
            foreach (var prefab in library.prefabs)
                if (prefab != null && prefab.name == name) { s_PrefabCache[name] = prefab; return prefab; }

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets(name + " t:Prefab");
        if (guids != null && guids.Length > 0)
        {
            string p = null;
            foreach (string guid in guids)
            {
                string candidate = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(candidate) == name) { p = candidate; break; }
            }
            GameObject loaded = p != null ? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p) : null;
            if (loaded != null)
            {
                s_PrefabCache[name] = loaded;
                return loaded;
            }
        }
#endif
        GameObject runtimePrefab = Resources.Load<GameObject>("Megabonk/WorldAssets/Prefabs/" + name);
        if (runtimePrefab != null)
        {
            s_PrefabCache[name] = runtimePrefab;
            return runtimePrefab;
        }
        return null;
    }
}
