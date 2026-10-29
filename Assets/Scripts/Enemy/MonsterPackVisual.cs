using UnityEngine;

/// <summary>Aplica los modelos de Small Hearth Studios a enemigos con IA existente.</summary>
public static class MonsterPackVisual
{
    static Material paletteMaterial;

    public static void Apply(GameObject enemy, EnemyAI3D.EnemyType type, float elapsedSeconds)
    {
        if (enemy == null || type == EnemyAI3D.EnemyType.Boss) return;
        if (ExtractedEnemyMeshVisual.Apply(enemy, type, elapsedSeconds)) return;
        if (elapsedSeconds >= 90f)
        {
            string importedName = type == EnemyAI3D.EnemyType.Elite ? "Ogre" :
                type == EnemyAI3D.EnemyType.Summoner ? "BloodMage" :
                type == EnemyAI3D.EnemyType.FlyingShooter ? "Birdo" :
                type == EnemyAI3D.EnemyType.Charger ? "CorruptedKnight" : "Bandit";
            Renderer[] originalRenderers = enemy.GetComponentsInChildren<Renderer>();
            var imported = ExtractedVisuals.Attach(importedName, enemy.transform, type == EnemyAI3D.EnemyType.Elite ? 3.1f : 1.8f);
            if (imported != null)
            {
                foreach (var renderer in originalRenderers) renderer.enabled = false;
                foreach (var animator in enemy.GetComponentsInChildren<Animator>())
                    if (!animator.transform.IsChildOf(imported.transform)) animator.enabled = false;
                imported.AddComponent<ExtractedMobAnimation>();
                return;
            }
        }
        string modelName = ChooseModel(type, elapsedSeconds);
        if (string.IsNullOrEmpty(modelName)) return;

        GameObject model = Resources.Load<GameObject>("MonstersPack/Models/" + modelName);
        if (model == null)
        {
            Debug.LogWarning("No se pudo cargar el monstruo: " + modelName);
            return;
        }

        // El prefab base aporta IA, animación y colisión. Ocultamos solamente
        // su malla antigua para que no atraviese el nuevo monstruo.
        foreach (Renderer oldRenderer in enemy.GetComponentsInChildren<Renderer>())
            oldRenderer.enabled = false;

        GameObject visual = Object.Instantiate(model, enemy.transform);
        visual.name = "MonsterVisual_" + modelName;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = model.transform.localRotation;

        foreach (Collider collider in visual.GetComponentsInChildren<Collider>())
            collider.enabled = false;

        Material material = GetPaletteMaterial();
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = true;
            if (material != null) renderer.sharedMaterial = material;
        }

        Renderer[] meshes = visual.GetComponentsInChildren<Renderer>();
        if (meshes.Length == 0) return;
        Bounds bounds = CalculateBounds(meshes);
        float targetHeight = type == EnemyAI3D.EnemyType.Elite ? 3.1f :
            type == EnemyAI3D.EnemyType.FlyingShooter ? 1.3f : 1.8f;
        if (bounds.size.y > 0.01f)
            visual.transform.localScale *= targetHeight / bounds.size.y;
        bounds = CalculateBounds(meshes);
        visual.transform.position += new Vector3(
            enemy.transform.position.x - bounds.center.x,
            enemy.transform.position.y - bounds.min.y,
            enemy.transform.position.z - bounds.center.z);
        MonsterPackVisualMotion motion = visual.AddComponent<MonsterPackVisualMotion>();
        motion.hovering = type == EnemyAI3D.EnemyType.FlyingShooter;
    }

    static string ChooseModel(EnemyAI3D.EnemyType type, float elapsedSeconds)
    {
        switch (type)
        {
            case EnemyAI3D.EnemyType.Lurker:
                if (elapsedSeconds < 90f) return null; // primer tramo: esqueleto original animado
                float roll = Random.value;
                return roll < 0.45f ? "Orc" : roll < 0.65f ? "Skeleton" : null;
            case EnemyAI3D.EnemyType.Sploder:
                return Random.value < 0.5f ? "Alien_Undercover" : "Alien_Grunt";
            case EnemyAI3D.EnemyType.Charger:
                return Random.value < 0.5f ? "WereWolf" : "Minotaur";
            case EnemyAI3D.EnemyType.Summoner:
                return Random.value < 0.5f ? "skell_mage" : "Alien_Leader";
            case EnemyAI3D.EnemyType.Elite:
                return Random.value < 0.5f ? "skell_warrior" : "Minotaur";
            case EnemyAI3D.EnemyType.FlyingShooter:
                return "Alien_Leader";
            default:
                return null;
        }
    }

    static Material GetPaletteMaterial()
    {
        if (paletteMaterial != null) return paletteMaterial;
        Texture2D palette = Resources.Load<Texture2D>("MonstersPack/Textures/GradientPalette");
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null) return null;
        paletteMaterial = new Material(shader) { name = "SHS Monsters Palette" };
        paletteMaterial.mainTexture = palette;
        if (palette != null && paletteMaterial.HasProperty("_BaseMap"))
            paletteMaterial.SetTexture("_BaseMap", palette);
        return paletteMaterial;
    }

    static Bounds CalculateBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}

