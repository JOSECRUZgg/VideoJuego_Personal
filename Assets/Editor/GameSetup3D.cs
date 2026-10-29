using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Script de editor que construye toda la escena del FPS 3D Shooter.
/// Acceder vía Tools > Setup 3D Shooter en la barra de menú de Unity.
/// </summary>
public static class GameSetup3D
{
    [MenuItem("Tools/Setup 3D Shooter")]
    public static void Setup()
    {
        // ── Registrar tags personalizados ──
        AddTag("Player");
        AddTag("Enemy");

        // ── Crear materiales ──
        Material groundMat = CreateMaterial("GroundMat", new Color(0.25f, 0.27f, 0.3f));
        Material wallMat   = CreateMaterial("WallMat",   new Color(0.35f, 0.37f, 0.4f));
        Material coverMat  = CreateMaterial("CoverMat",  new Color(0.4f, 0.35f, 0.3f));
        Material playerMat = CreateMaterial("PlayerMat", new Color(0.2f, 0.6f, 1f));
        Material enemyMat  = CreateMaterial("EnemyMat",  new Color(1f, 0.2f, 0.2f));

        // ── Nueva escena vacía ──
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ═══════════════════════════════════════════
        // 1. LUZ DIRECCIONAL
        // ═══════════════════════════════════════════
        GameObject lightObj = new GameObject("Directional Light");
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.shadows = LightShadows.Soft;

        // ═══════════════════════════════════════════
        // 2. SUELO
        // ═══════════════════════════════════════════
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(10f, 1f, 10f); // 100x100 unidades
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;
        ground.isStatic = true;

        // ═══════════════════════════════════════════
        // 3. MUROS DE LA ARENA
        // ═══════════════════════════════════════════
        float arenaHalf = 50f;
        float wallHeight = 4f;
        float wallThickness = 1f;

        CreateWall("Wall_North", new Vector3(0f, wallHeight / 2f, arenaHalf),
            new Vector3(arenaHalf * 2f + wallThickness, wallHeight, wallThickness), wallMat);
        CreateWall("Wall_South", new Vector3(0f, wallHeight / 2f, -arenaHalf),
            new Vector3(arenaHalf * 2f + wallThickness, wallHeight, wallThickness), wallMat);
        CreateWall("Wall_East", new Vector3(arenaHalf, wallHeight / 2f, 0f),
            new Vector3(wallThickness, wallHeight, arenaHalf * 2f + wallThickness), wallMat);
        CreateWall("Wall_West", new Vector3(-arenaHalf, wallHeight / 2f, 0f),
            new Vector3(wallThickness, wallHeight, arenaHalf * 2f + wallThickness), wallMat);

        // ═══════════════════════════════════════════
        // 4. OBJETOS DE COBERTURA
        // ═══════════════════════════════════════════
        CreateCover("Cover_CrateA",      new Vector3( 12f, 1f,    8f),   new Vector3(2f, 2f, 2f), coverMat);
        CreateCover("Cover_CrateB",      new Vector3(-15f, 1f,   12f),   new Vector3(2f, 2f, 2f), coverMat);
        CreateCover("Cover_LongWallA",   new Vector3(  5f, 0.75f,-10f),  new Vector3(3f, 1.5f, 3f), coverMat);
        CreateCover("Cover_LongWallB",   new Vector3(-20f, 0.75f,-18f),  new Vector3(3f, 1.5f, 3f), coverMat);
        CreateCover("Cover_ColumnA",     new Vector3( 25f, 1.5f,  20f),  new Vector3(1f, 3f, 1f), coverMat);
        CreateCover("Cover_ColumnB",     new Vector3(-25f, 1.5f, -22f),  new Vector3(1f, 3f, 1f), coverMat);
        CreateCover("Cover_ColumnC",     new Vector3(  0f, 1.5f,  30f),  new Vector3(1f, 3f, 1f), coverMat);
        CreateCover("Cover_BarricadeA",  new Vector3( 18f, 1f,  -25f),   new Vector3(4f, 2f, 1f), coverMat);
        CreateCover("Cover_BarricadeB",  new Vector3(-10f, 1f,   25f),   new Vector3(1f, 2f, 4f), coverMat);
        CreateCover("Cover_CenterBlock", new Vector3(  0f, 1f,    0f),   new Vector3(3f, 2f, 3f), coverMat);

        // ═══════════════════════════════════════════
        // 5. RAMPAS
        // ═══════════════════════════════════════════
        GameObject ramp1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp1.name = "Ramp_A";
        ramp1.transform.position = new Vector3(30f, 0.8f, -12f);
        ramp1.transform.localScale = new Vector3(4f, 0.4f, 6f);
        ramp1.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
        ramp1.GetComponent<Renderer>().sharedMaterial = coverMat;
        ramp1.isStatic = true;

        GameObject ramp2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp2.name = "Ramp_B";
        ramp2.transform.position = new Vector3(-28f, 0.8f, 15f);
        ramp2.transform.localScale = new Vector3(4f, 0.4f, 6f);
        ramp2.transform.rotation = Quaternion.Euler(-15f, 45f, 0f);
        ramp2.GetComponent<Renderer>().sharedMaterial = coverMat;
        ramp2.isStatic = true;

        // ═══════════════════════════════════════════
        // 6. JUGADOR
        // ═══════════════════════════════════════════
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 0f, -20f);

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0f, 1f, 0f);

        player.AddComponent<PlayerMovement3D>();
        player.AddComponent<PlayerHealth3D>();

        // Cuerpo visual (cápsula sin colisionador, invisible en FPS para no obstruir la vista)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.transform.SetParent(player.transform);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        Renderer bodyRend = body.GetComponent<Renderer>();
        bodyRend.sharedMaterial = playerMat;
        bodyRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        bodyRend.enabled = false;

        // Cámara (hija del jugador)
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        camObj.AddComponent<AudioListener>();
        camObj.transform.SetParent(player.transform);
        camObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        camObj.transform.localRotation = Quaternion.identity;

        MouseLook look = camObj.AddComponent<MouseLook>();
        look.playerBody = player.transform;

        PlayerShooting3D shoot = camObj.AddComponent<PlayerShooting3D>();
        shoot.cam = cam;
        shoot.muzzleFlashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Magic Pig Games (Infinity PBR)/HOVL - Toon Projectiles/Particles/Muzzles/Toon Projectiles - Muzzle 1.prefab");
        shoot.impactEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Magic Pig Games (Infinity PBR)/HOVL - Toon Projectiles/Particles/Impacts/Toon Projectiles - Impact 1.prefab");

        // ═══════════════════════════════════════════
        // 7. PREFAB DE ENEMIGO
        // ═══════════════════════════════════════════
        EnsureFolder("Assets", "Prefabs");

        // Configurar prefabs de esqueleto (oleadas), dragón (jefe) y personajes humanoides
        AssetIntegrationSetup.SetupPrefabs();

        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DragonBoss.prefab");

        // ═══════════════════════════════════════════
        // 8. SPAWNER DE ENEMIGOS
        // ═══════════════════════════════════════════
        GameObject spawner = new GameObject("EnemySpawner");
        EnemySpawner3D es = spawner.AddComponent<EnemySpawner3D>();
        es.enemyPrefab = enemyPrefab;
        es.bossPrefab = bossPrefab;

        // ═══════════════════════════════════════════
        // 9. GAME MANAGER
        // ═══════════════════════════════════════════
        GameObject gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();

        // ═══════════════════════════════════════════
        // 10. SISTEMA DE XP
        // ═══════════════════════════════════════════
        GameObject xpObj = new GameObject("XPSystem");
        xpObj.AddComponent<XPSystem>();

        // ═══════════════════════════════════════════
        // 11. HUD
        // ═══════════════════════════════════════════
        GameObject hudObj = new GameObject("HUD");
        hudObj.AddComponent<GameHUD>();
        hudObj.AddComponent<SkillMenuUI>();

        // ═══════════════════════════════════════════
        // 11. INDICADOR DE DAÑO
        // ═══════════════════════════════════════════
        GameObject dmgObj = new GameObject("DamageIndicator");
        dmgObj.AddComponent<DamageIndicator>();

        // ═══════════════════════════════════════════
        // 12. ENRIQUECEDOR DE ARENA (Santuario, Cielo, Torres, Naturaleza, VFX)
        // ═══════════════════════════════════════════
        GameObject enricherObj = new GameObject("ArenaEnricher");
        enricherObj.AddComponent<ArenaEnricher>();

        // ═══════════════════════════════════════════
        // GUARDAR ESCENA
        // ═══════════════════════════════════════════
        EnsureFolder("Assets", "Scenes");
        string scenePath = "Assets/Scenes/Main.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        // Agregar a Build Settings
        List<EditorBuildSettingsScene> scenes =
            new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        foreach (EditorBuildSettingsScene s in scenes)
        {
            if (s.path == scenePath) { found = true; break; }
        }
        if (!found)
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ 3D SHOOTER CONFIGURADO — Escena guardada en " + scenePath);
    }

    // ── Helpers ──────────────────────────────────────

    static GameObject CreateWall(string name, Vector3 position, Vector3 scale, Material mat)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = mat;
        wall.isStatic = true;
        return wall;
    }

    static GameObject CreateCover(string name, Vector3 position, Vector3 scale, Material mat)
    {
        GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cover.name = name;
        cover.transform.position = position;
        cover.transform.localScale = scale;
        cover.GetComponent<Renderer>().sharedMaterial = mat;
        cover.isStatic = true;
        return cover;
    }

    static Material CreateMaterial(string name, Color color)
    {
        EnsureFolder("Assets", "Materials");
        string path = "Assets/Materials/" + name + ".mat";

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = name;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void EnsureFolder(string parent, string name)
    {
        string fullPath = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(fullPath))
            AssetDatabase.CreateFolder(parent, name);
    }

    static void AddTag(string tag)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;

        SerializedObject so = new SerializedObject(assets[0]);
        SerializedProperty tags = so.FindProperty("tags");
        if (tags == null) return;

        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                return;
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        so.ApplyModifiedProperties();
    }
}
