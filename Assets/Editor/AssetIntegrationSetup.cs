using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class AssetIntegrationSetup
{
    [MenuItem("Tools/Upgrade Imported Materials to URP")]
    public static void UpgradeMaterialsToURP()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("No se encontró el shader Universal Render Pipeline/Lit");
            return;
        }

        string[] folders = new string[]
        {
            "Assets/Altar_Ruins_FREE",
            "Assets/FourEvilDragonsPBR",
            "Assets/Polytope Studio",
            "Assets/DungeonCharacters",
            "Assets/Low Poly Guns",
            "Assets/Low Poly Weapons VOL.1",
            "Assets/Mesh",
            "Assets/VFXPACK_FIRE_WALLCOEUR",
            "Assets/Magic Pig Games (Infinity PBR)"
        };

        int upgraded = 0;
        foreach (string folder in folders)
        {
            if (!Directory.Exists(folder)) continue;
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                // Si ya es URP Lit o Skybox, saltar
                if (mat.shader.name.StartsWith("Universal Render Pipeline") || mat.shader.name.StartsWith("Skybox"))
                    continue;

                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                Color col = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                mat.shader = urpLit;
                if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                mat.SetColor("_BaseColor", col);
                if (bumpMap != null)
                {
                    mat.SetTexture("_BumpMap", bumpMap);
                    mat.EnableKeyword("_NORMALMAP");
                }

                EditorUtility.SetDirty(mat);
                upgraded++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Materiales URP actualizados: {upgraded}");
    }

    [MenuItem("Tools/Setup Skeleton and Dragon Prefabs")]
    [InitializeOnLoadMethod]
    public static void SetupPrefabs()
    {
        UpgradeMaterialsToURP();
        SetupMagicVFXTextures();
        SetupSkeletonControllerAndPrefab();
        SetupDragonBossPrefab();
        SetupFlyingShooterPrefab();
        SetupHumanoidPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Prefabs de Esqueleto y Dragón configurados con éxito.");
    }

    [MenuItem("Tools/Setup Magic VFX Textures")]
    public static void SetupMagicVFXTextures()
    {
        string[] folders = new string[] { "Assets/MagicVFX", "Assets/Resources/MagicVFX" };
        int configured = 0;
        foreach (string folder in folders)
        {
            if (!Directory.Exists(folder)) continue;
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    bool modified = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        modified = true;
                    }
                    if (importer.filterMode != FilterMode.Point)
                    {
                        importer.filterMode = FilterMode.Point;
                        modified = true;
                    }
                    if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        modified = true;
                    }
                    if (modified)
                    {
                        importer.SaveAndReimport();
                        configured++;
                    }
                }
            }
        }
        if (configured > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"Magic VFX texturas configuradas como Sprites: {configured}");
        }
    }

    static void SetupSkeletonControllerAndPrefab()
    {
        EnsureFolder("Assets", "Prefabs");

        // 1. Crear Animator Controller para el esqueleto si no existe
        string controllerPath = "Assets/Prefabs/SkeletonController.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;

            // Cargar clips
            AnimationClip idleClip = LoadFirstClip("Assets/DungeonCharacters/Skeletons_demo/animation/DS_onehand_idle_A.FBX");
            AnimationClip walkClip = LoadFirstClip("Assets/DungeonCharacters/Skeletons_demo/animation/DS_onehand_walk.FBX");
            AnimationClip attackClip = LoadFirstClip("Assets/DungeonCharacters/Skeletons_demo/animation/DS_onehand_attack_A.FBX");

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;

            var walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walkClip;

            var attackState = rootStateMachine.AddState("Attack");
            attackState.motion = attackClip;

            // Transición Idle -> Walk
            var toWalk = idleState.AddTransition(walkState);
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            toWalk.duration = 0.15f;

            // Transición Walk -> Idle
            var toIdle = walkState.AddTransition(idleState);
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            toIdle.duration = 0.15f;

            // Transición AnyState -> Attack
            var toAttack = rootStateMachine.AddAnyStateTransition(attackState);
            toAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            toAttack.duration = 0.1f;

            // Transición Attack -> Idle al terminar
            var backToIdle = attackState.AddTransition(idleState);
            backToIdle.hasExitTime = true;
            backToIdle.exitTime = 0.85f;
            backToIdle.duration = 0.15f;
        }

        // 2. Construir Prefab de Esqueleto
        GameObject skeletonModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonCharacters/Skeletons_demo/DungeonSkeleton_demoPrefab.prefab");
        if (skeletonModelPrefab == null)
        {
            Debug.LogError("No se encontró DungeonSkeleton_demoPrefab.prefab");
            return;
        }

        GameObject root = new GameObject("SkeletonEnemy");
        root.tag = "Enemy";

        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.radius = 0.45f;
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);

        root.AddComponent<EnemyAI3D>();
        root.AddComponent<EnemyHealth3D>();
        root.AddComponent<EnemyVisuals>();

        GameObject modelInstance = Object.Instantiate(skeletonModelPrefab, root.transform);
        modelInstance.name = "Model";
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;

        Animator anim = modelInstance.GetComponent<Animator>();
        if (anim == null) anim = modelInstance.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        // Cargar avatar de la fuente FBX si está disponible
        Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/DungeonCharacters/Skeletons_demo/models/DungeonSkeleton_demo.FBX");
        if (avatar != null) anim.avatar = avatar;

        string enemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, enemyPrefabPath);
        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/SkeletonEnemy.prefab");
        Object.DestroyImmediate(root);
        Debug.Log("Prefab de Enemigo Esqueleto guardado en " + enemyPrefabPath);
    }

    static void SetupDragonBossPrefab()
    {
        EnsureFolder("Assets", "Prefabs");

        string controllerPath = "Assets/Prefabs/DragonBossController.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Roar", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        }

        var rootSm = controller.layers[0].stateMachine;

        AnimationClip idleClip        = LoadFirstClip("Assets/FourEvilDragonsPBR/Animations/DragonNightMare/idle01.fbx");
        AnimationClip walkClip        = LoadFirstClip("Assets/FourEvilDragonsPBR/Animations/DragonNightMare/walk.fbx");
        AnimationClip runClip         = LoadFirstClip("Assets/FourEvilDragonsPBR/Animations/DragonNightMare/run.fbx");
        AnimationClip attackClip      = LoadFirstClip("Assets/FourEvilDragonsPBR/Animations/DragonNightMare/Claw Attack.fbx");
        AnimationClip basicAttackClip = LoadFirstClip("Assets/FourEvilDragonsPBR/Animations/DragonNightMare/Basic Attack.fbx");
        AnimationClip hornAttackClip  = LoadFirstClip("Assets/FourEvilDragonsPBR/Animations/DragonNightMare/Horn Attack.fbx");
        AnimationClip jumpClip        = LoadFirstClip("Assets/FourEvilDragonsPBR/Animations/DragonNightMare/Jump.fbx");
        AnimationClip roarClip        = LoadFirstClip("Assets/FourEvilDragonsPBR/Animations/DragonNightMare/scream.fbx");
        AnimationClip hitClip         = LoadFirstClip("Assets/FourEvilDragonsPBR/Animations/DragonNightMare/getHit.fbx");
        AnimationClip dieClip         = LoadFirstClip("Assets/FourEvilDragonsPBR/Animations/DragonNightMare/die.fbx");

        var idleState   = AddOrGetState(rootSm, "Idle", idleClip);
        var walkState   = AddOrGetState(rootSm, "Walk", walkClip);
        var runState    = AddOrGetState(rootSm, "Run", runClip);
        var attackState = AddOrGetState(rootSm, "Attack", attackClip);
        var clawState   = AddOrGetState(rootSm, "Claw Attack", attackClip);
        var biteState   = AddOrGetState(rootSm, "Basic Attack", basicAttackClip);
        var hornState   = AddOrGetState(rootSm, "Horn Attack", hornAttackClip);
        var jumpState   = AddOrGetState(rootSm, "Jump", jumpClip);
        var roarState   = AddOrGetState(rootSm, "Roar", roarClip);
        var screamState = AddOrGetState(rootSm, "Scream", roarClip);
        var hitState    = AddOrGetState(rootSm, "Hit", hitClip);
        var dieState    = AddOrGetState(rootSm, "Die", dieClip);

        EnsureTransition(idleState, walkState, AnimatorConditionMode.Greater, 0.15f, "Speed", 0.2f);
        EnsureTransition(walkState, idleState, AnimatorConditionMode.Less, 0.15f, "Speed", 0.2f);
        EnsureTransition(walkState, runState, AnimatorConditionMode.Greater, 3.5f, "Speed", 0.2f);
        EnsureTransition(runState, walkState, AnimatorConditionMode.Less, 3.5f, "Speed", 0.2f);

        EnsureReturnToIdle(attackState, idleState);
        EnsureReturnToIdle(clawState, idleState);
        EnsureReturnToIdle(biteState, idleState);
        EnsureReturnToIdle(hornState, idleState);
        EnsureReturnToIdle(jumpState, idleState);
        EnsureReturnToIdle(roarState, idleState);
        EnsureReturnToIdle(screamState, idleState);
        EnsureReturnToIdle(hitState, idleState);

        string dragonSourcePath = "Assets/FourEvilDragonsPBR/Prefab/DragonNightmare/Red.prefab";
        GameObject dragonSource = AssetDatabase.LoadAssetAtPath<GameObject>(dragonSourcePath);
        if (dragonSource == null)
        {
            dragonSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FourEvilDragonsPBR/Prefab/DragonUsurper/Black.prefab");
        }

        if (dragonSource == null)
        {
            Debug.LogError("No se encontró ningún prefab de dragón en FourEvilDragonsPBR");
            return;
        }

        GameObject root = new GameObject("DragonBoss");
        root.tag = "Enemy";

        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.radius = 2.2f;
        col.height = 3.2f;
        col.center = new Vector3(0f, 1.6f, 0f);

        EnemyAI3D ai = root.AddComponent<EnemyAI3D>();
        ai.enemyType = EnemyAI3D.EnemyType.Boss;

        EnemyHealth3D health = root.AddComponent<EnemyHealth3D>();
        root.AddComponent<EnemyVisuals>();
        root.AddComponent<DragonBossVisuals>();

        GameObject modelInstance = Object.Instantiate(dragonSource, root.transform);
        modelInstance.name = "DragonModel";
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one * 0.7f; // Tamaño calibrado para la arena

        Animator anim = modelInstance.GetComponent<Animator>();
        if (anim == null) anim = modelInstance.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;

        string bossPrefabPath = "Assets/Prefabs/DragonBoss.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, bossPrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log("Prefab de Jefe Dragón guardado con DragonBossController en " + bossPrefabPath);
    }

    static void SetupFlyingShooterPrefab()
    {
        EnsureFolder("Assets", "Prefabs");

        string flyingModelPath = "Assets/FourEvilDragonsPBR/Prefab/DragonSoulEater/Purple.prefab";
        GameObject flyingSource = AssetDatabase.LoadAssetAtPath<GameObject>(flyingModelPath);
        if (flyingSource == null)
        {
            flyingSource = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FourEvilDragonsPBR/Prefab/DragonSoulEater/Blue.prefab");
        }

        if (flyingSource == null)
        {
            Debug.LogError("No se encontró ningún prefab de DragonSoulEater");
            return;
        }

        GameObject root = new GameObject("FlyingShooterEnemy");
        root.tag = "Enemy";

        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.radius = 1.0f;
        col.height = 2.0f;
        col.center = new Vector3(0f, 1f, 0f);

        EnemyAI3D ai = root.AddComponent<EnemyAI3D>();
        ai.enemyType = EnemyAI3D.EnemyType.FlyingShooter;

        EnemyHealth3D health = root.AddComponent<EnemyHealth3D>();
        health.maxHealth = 14;

        GameObject modelInstance = Object.Instantiate(flyingSource, root.transform);
        modelInstance.name = "DragonModel";
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one * 0.45f;

        RuntimeAnimatorController soulCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/FourEvilDragonsPBR/Animators/SouleaterCTRL.controller");
        Animator anim = modelInstance.GetComponent<Animator>();
        if (anim == null) anim = modelInstance.AddComponent<Animator>();
        if (soulCtrl != null) anim.runtimeAnimatorController = soulCtrl;

        string prefabPath = "Assets/Prefabs/FlyingShooterEnemy.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        Debug.Log("Prefab de Enemigo Volador guardado en " + prefabPath);
    }

    static void SetupHumanoidPrefab()
    {
        EnsureFolder("Assets", "Prefabs");

        string controllerPath = "Assets/Prefabs/HumanoidController.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;

            AnimationClip idleClip = LoadFirstClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx");
            AnimationClip walkClip = LoadFirstClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx");
            AnimationClip runClip = LoadFirstClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx");
            AnimationClip jumpClip = LoadFirstClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Jump/HumanM@Jump01.fbx");

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;

            var walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walkClip;

            var runState = rootStateMachine.AddState("Run");
            runState.motion = runClip;

            var jumpState = rootStateMachine.AddState("Jump");
            jumpState.motion = jumpClip;

            var toWalk = idleState.AddTransition(walkState);
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            toWalk.duration = 0.15f;

            var toRun = walkState.AddTransition(runState);
            toRun.AddCondition(AnimatorConditionMode.Greater, 4.0f, "Speed");
            toRun.duration = 0.15f;

            var runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.Less, 4.0f, "Speed");
            runToWalk.duration = 0.15f;

            var toIdle = walkState.AddTransition(idleState);
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            toIdle.duration = 0.15f;

            var toJump = rootStateMachine.AddAnyStateTransition(jumpState);
            toJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
            toJump.duration = 0.1f;

            var backFromJump = jumpState.AddTransition(idleState);
            backFromJump.hasExitTime = true;
            backFromJump.exitTime = 0.85f;
            backFromJump.duration = 0.15f;
        }

        GameObject modelFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Kevin Iglesias/Human Animations/Models/HumanM_Model.fbx");
        if (modelFbx != null)
        {
            GameObject humanRoot = new GameObject("HumanoidCharacter");
            GameObject modelInst = Object.Instantiate(modelFbx, humanRoot.transform);
            modelInst.name = "Model";

            Animator anim = modelInst.GetComponent<Animator>();
            if (anim == null) anim = modelInst.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;

            Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>("Assets/Kevin Iglesias/Human Animations/Models/HumanM_Model.fbx");
            if (avatar != null) anim.avatar = avatar;

            PrefabUtility.SaveAsPrefabAsset(humanRoot, "Assets/Prefabs/HumanoidCharacter.prefab");
            Object.DestroyImmediate(humanRoot);
            Debug.Log("Prefab humanoide compatible (Kevin Iglesias) guardado en Assets/Prefabs/HumanoidCharacter.prefab");
        }
    }

    static AnimationClip LoadFirstClip(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object a in assets)
        {
            if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        return null;
    }

    static void EnsureFolder(string parent, string name)
    {
        string fullPath = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(fullPath))
            AssetDatabase.CreateFolder(parent, name);
    }

    static AnimatorState AddOrGetState(AnimatorStateMachine sm, string name, AnimationClip clip)
    {
        foreach (var s in sm.states)
        {
            if (s.state.name == name)
            {
                if (s.state.motion == null && clip != null) s.state.motion = clip;
                return s.state;
            }
        }
        var st = sm.AddState(name);
        st.motion = clip;
        return st;
    }

    static void EnsureTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold, string param, float duration)
    {
        foreach (var t in from.transitions)
        {
            if (t.destinationState == to) return;
        }
        var trans = from.AddTransition(to);
        trans.AddCondition(mode, threshold, param);
        trans.duration = duration;
    }

    static void EnsureReturnToIdle(AnimatorState actionState, AnimatorState idleState, float exitTime = 0.85f, float duration = 0.15f)
    {
        foreach (var t in actionState.transitions)
        {
            if (t.destinationState == idleState) return;
        }
        var trans = actionState.AddTransition(idleState);
        trans.hasExitTime = true;
        trans.exitTime = exitTime;
        trans.duration = duration;
    }
}
