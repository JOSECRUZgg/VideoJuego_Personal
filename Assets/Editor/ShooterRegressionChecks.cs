using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Runs against the real scene in Play Mode without saving scene changes.
public static class ShooterRegressionChecks
{
    const string Pending = "ShooterRegressionChecks.Pending";
    static int frames;
    static readonly List<string> results = new List<string>();

    public static void RunBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Initialize()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                frames = 0;
                EditorApplication.update += Tick;
            }
        };
    }

    static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        results.Add("PASS: " + description);
    }

    static void Tick()
    {
        if (++frames < 15) return;
        EditorApplication.update -= Tick;
        int exitCode = 0;
        try
        {
            var gm = GameManager.Instance;
            var xp = XPSystem.Instance;
            var health = UnityEngine.Object.FindAnyObjectByType<PlayerHealth3D>();
            Check(gm != null && xp != null && health != null, "Main scene initializes required systems");
            var menu = UnityEngine.Object.FindAnyObjectByType<OriginalMenuBridge>();
            Check(menu != null, "Original menu is instantiated in the main scene");
            bool coopButton = false, blankWhiteImage = false;
            foreach (var action in menu.GetComponentsInChildren<OriginalMenuAction>(true))
                if (action.method == "GoToCoop") coopButton = true;
            foreach (var image in menu.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if (image.sprite == null && image.color == Color.white) blankWhiteImage = true;
            Check(coopButton && !blankWhiteImage, "Original menu retains LAN access without blank white images");
            StartRun(gm);
            int baseHealth = health.maxHealth;
            health.IncreaseMaxHealth(75);
            gm.ReturnToMainMenu();
            StartRun(gm);
            Check(health.maxHealth == baseHealth, "New run restores base health and character passive exactly once");

            xp.ApplyUpgrade("power_shot_1");
            float damage = xp.damageMultiplier;
            xp.ApplyUpgrade("power_shot_1");
            Check(Mathf.Approximately(damage, xp.damageMultiplier), "Duplicate upgrades do not stack twice");
            xp.ApplyUpgrade("nonexistent_upgrade");
            Check(!xp.HasUpgrade("nonexistent_upgrade"), "Unknown upgrade IDs are rejected");

            xp.OpenBossTomeMenu();
            gm.Pause();
            gm.Resume();
            Check(Time.timeScale == 0f, "Resume cannot bypass boss reward menu");
            xp.CloseBossTomeMenu();
            Check(Time.timeScale == 1f, "Closing reward menu resumes an unpaused run");
            gm.Pause();
            Check(Time.timeScale == 0f, "Manual pause stops an active run");
            gm.Resume();
            Check(Time.timeScale == 1f, "Manual pause resumes after the reward menu closes");

            xp.OpenSkillMenu();
            int beforeXP = xp.currentXP;
            xp.AddXP(3);
            Check(xp.currentXP > beforeXP, "XP received during selection is retained after the difficulty multiplier");
            gm.ReturnToMainMenu();
            Check(!xp.skillMenuOpen && !xp.bossTomeMenuOpen, "Restart clears both reward menu flags");
            StartRun(gm);

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m1 = RuntimeMaterialCache.Get(shader, Color.red, Color.red);
            var m2 = RuntimeMaterialCache.Get(shader, Color.red, Color.red);
            var m3 = RuntimeMaterialCache.Get(shader, Color.red, Color.red * 2f);
            Check(m1 == m2 && m1 != m3, "Materials reuse matching keys and preserve distinct emission");

            var colliderProbe = new GameObject("UnreadableMeshColliderProbe");
            var probeMesh = new Mesh();
            probeMesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            probeMesh.triangles = new[] { 0, 1, 2 };
            probeMesh.RecalculateBounds();
            probeMesh.UploadMeshData(true);
            var probeFilter = colliderProbe.AddComponent<MeshFilter>();
            probeFilter.sharedMesh = probeMesh;
            typeof(ArenaEnricher).GetMethod("AddSceneryCollider", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { probeFilter });
            Check(colliderProbe.GetComponent<BoxCollider>() != null && colliderProbe.GetComponent<MeshCollider>() == null,
                "Unreadable scenery meshes use a box collider in player builds");
            UnityEngine.Object.DestroyImmediate(colliderProbe);
            UnityEngine.Object.DestroyImmediate(probeMesh);

            int initialCount = EnemyHealth3D.AliveCount;
            var enemy = new GameObject("RegressionEnemy");
            enemy.AddComponent<EnemyHealth3D>();
            Check(EnemyHealth3D.AliveCount == initialCount + 1, "Enemy registers before its first Update");
            enemy.SetActive(false);
            Check(EnemyHealth3D.AliveCount == initialCount, "Disabled enemies leave alive count");
            enemy.SetActive(true);
            Check(EnemyHealth3D.AliveCount == initialCount + 1, "Re-enabled enemy registers exactly once");
            UnityEngine.Object.DestroyImmediate(enemy);
            Check(EnemyHealth3D.AliveCount == initialCount, "Destroyed enemies leave alive count");

            var sploder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var sploderHealth = sploder.AddComponent<EnemyHealth3D>();
            var neighbor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            neighbor.transform.position = sploder.transform.position + Vector3.right;
            var neighborHealth = neighbor.AddComponent<EnemyHealth3D>();
            var startHealth = typeof(EnemyHealth3D).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            startHealth.Invoke(sploderHealth, null);
            startHealth.Invoke(neighborHealth, null);
            Physics.SyncTransforms();
            int previousKills = gm.kills;
            int previousScore = gm.score;
            int previousOrbs = UnityEngine.Object.FindObjectsByType<XpOrb3D>().Length;
            sploderHealth.DetonateWithoutReward(6.5f);
            Check(sploderHealth.IsDying && !neighborHealth.IsDying && neighborHealth.currentHealth == neighborHealth.maxHealth,
                "Self-detonation removes only the Sploder and leaves neighboring mobs unharmed");
            Check(gm.kills == previousKills && gm.score == previousScore && UnityEngine.Object.FindObjectsByType<XpOrb3D>().Length == previousOrbs,
                "Self-detonation grants no player kills, score or XP orbs");
            UnityEngine.Object.DestroyImmediate(sploder);
            UnityEngine.Object.DestroyImmediate(neighbor);

            var spawner = UnityEngine.Object.FindAnyObjectByType<EnemySpawner3D>();
            Check(spawner != null && spawner.enemyPrefab != null, "Scene has configured enemy prefab");
            spawner.ResetSpawner();
            Set(spawner, "player", health.transform);
            Set(spawner, "enemiesToSpawn", 1);
            Set(spawner, "enemiesSpawnedThisWave", 0);
            Set(spawner, "nextSpawnTime", 0f);
            spawner.currentWave = 1;
            spawner.isResting = false;
            typeof(EnemySpawner3D).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(spawner, null);
            Check(!spawner.isResting && spawner.enemiesRemaining > 0, "Final enemy spawn does not prematurely complete wave");

            EnemyProjectile3D.Create(Vector3.zero, Vector3.forward);
            BossChest3D.Spawn(Vector3.zero);
            gm.ReturnToMainMenu();
            Check(UnityEngine.Object.FindObjectsByType<EnemyProjectile3D>().Length == 0 &&
                  UnityEngine.Object.FindObjectsByType<BossChest3D>().Length == 0,
                  "Returning to menu immediately deactivates projectiles and chests");

            gm.selectedMap = GameManager.MapType.SnowyMountain;
            var arena = UnityEngine.Object.FindAnyObjectByType<ArenaEnricher>();
            arena.ApplyMap();
            var mountain = GameObject.Find("SnowMountainPlayable");
            Check(mountain != null && mountain.GetComponentsInChildren<MeshCollider>().Length > 0,
                "Snow mountain model has walkable mesh colliders");
            Vector3 snowSpawn = new Vector3(0f, 0f, -15f);
            bool importedSurface = false;
            if (arena.TryGetSnowHeight(snowSpawn, out float snowHeight))
                foreach (var collider in mountain.GetComponentsInChildren<MeshCollider>())
                {
                    var bounds = collider.bounds;
                    var ray = new Ray(new Vector3(0f, bounds.max.y + 2f, -15f), Vector3.down);
                    bool hitSurface = collider.Raycast(ray, out RaycastHit hit, bounds.size.y + 4f);
                    if (!hitSurface)
                    {
                        ray = new Ray(new Vector3(0f, bounds.min.y - 2f, -15f), Vector3.up);
                        hitSurface = collider.Raycast(ray, out hit, bounds.size.y + 4f);
                    }
                    if (hitSurface && Mathf.Abs(hit.point.y - snowHeight) < .2f) importedSurface = true;
                }
            Check(importedSurface, "Snow spawn rests on the imported mountain, not the backup floor");
        }
        catch (Exception e)
        {
            results.Add("FAIL: " + e);
            exitCode = 1;
        }
        Directory.CreateDirectory("Review");
        File.WriteAllLines("Review/playmode-results.txt", results);
        foreach (var result in results) Debug.Log(result);
        SessionState.SetBool(Pending, false);
        EditorApplication.Exit(exitCode);
    }

    static void Set(object target, string field, object value)
    {
        if (target == null) return;
        var f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (f != null) f.SetValue(target, value);
    }

    static void StartRun(GameManager manager)
    {
        typeof(GameManager).GetMethod("StartGameImmediate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
    }
}
