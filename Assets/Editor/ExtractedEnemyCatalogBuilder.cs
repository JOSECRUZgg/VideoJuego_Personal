using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Only translates serialized visual data; extracted gameplay scripts are not executable AI.
[InitializeOnLoad]
public static class ExtractedEnemyCatalogBuilder
{
    const string Source = @"C:\Users\JOSEC\Desktop\MATERIAS UT\10MO CUATRIMESTRE\JUAN\MEGABONK\ExportedProject\Assets\MonoBehaviour";
    const string Target = "Assets/Resources/ExtractedEnemyCatalog.asset";
    static ExtractedEnemyCatalogBuilder() { EditorApplication.delayCall += Ensure; }
    static void Ensure()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += Ensure; return; }
        var existing = AssetDatabase.LoadAssetAtPath<ExtractedEnemyCatalog>(Target);
        if (existing == null || existing.schemaVersion < 2) Build();
        if (!File.Exists("Assets/Resources/ExtractedStageCatalog.asset")) BuildStages();
    }

    [MenuItem("Tools/Megabonk/Rebuild extracted enemy catalog")]
    public static void Build()
    {
        if (!Directory.Exists(Source)) return;
        var originals = new Dictionary<string, string>();
        foreach (string path in Directory.GetFiles(Source, "*.asset.meta"))
        {
            string guid = Regex.Match(File.ReadAllText(path), @"guid: ([a-f0-9]{32})").Groups[1].Value;
            if (guid.Length == 32) originals[guid] = path.Substring(0, path.Length - 5);
        }
        var entries = new List<ExtractedEnemyCatalog.Entry>();
        foreach (string path in Directory.GetFiles(Source, "*.asset"))
        {
            string yaml = File.ReadAllText(path);
            if (!yaml.Contains("guid: 3c9ec722e4a6d81b8a737e16ff4eaf7c")) continue;
            string animGuid = Reference(yaml, "animation");
            if (!originals.TryGetValue(animGuid, out string animPath)) continue;
            string animation = File.ReadAllText(animPath);
            // Keep the first locomotion sequence, not unrelated attack/death sequences.
            int next = animation.IndexOf("  - Name:", animation.IndexOf("  - Name:") + 1, StringComparison.Ordinal);
            if (next > 0) animation = animation.Substring(0, next);
            Mesh[] frames = Regex.Matches(animation, @"guid: ([a-f0-9]{32})")
                .Cast<Match>().Select(m => Load<Mesh>(m.Groups[1].Value)).Where(m => m != null).ToArray();
            var material = Load<Material>(Reference(yaml, "material"));
            if (frames.Length == 0 || material == null) continue;
            var rotation = Regex.Match(yaml, @"rendererRotationOffset: \{x: ([-\d.]+), y: ([-\d.]+), z: ([-\d.]+)\}");
            entries.Add(new ExtractedEnemyCatalog.Entry {
                id = Path.GetFileNameWithoutExtension(path), frames = frames, material = material,
                fps = Number(animation, "AnimationFPS", 8), speed = Number(yaml, "speed", 3.5f),
                spawnAfter = Number(yaml, "canSpawnAfterTime", 0), flying = Number(yaml, "isFlying", 0) != 0,
                hp = (int)Number(yaml, "hp", 24), damage = (int)Number(yaml, "damage", 8),
                shield = (int)Number(yaml, "shield", 0), xp = (int)Number(yaml, "xp", 1),
                minDistance = Number(yaml, "minStayAtDistance", 0), maxDistance = Number(yaml, "maxStayAtDistance", 0),
                attacks = ReadAttacks(yaml),
                rotation = rotation.Success ? new Vector3(Parse(rotation.Groups[1].Value), Parse(rotation.Groups[2].Value), Parse(rotation.Groups[3].Value)) : Vector3.zero
            });
        }
        if (entries.Count == 0) { Debug.LogWarning("Enemy catalog: imported meshes/materials are not available yet."); return; }
        var catalog = AssetDatabase.LoadAssetAtPath<ExtractedEnemyCatalog>(Target);
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<ExtractedEnemyCatalog>(); AssetDatabase.CreateAsset(catalog, Target); }
        catalog.entries = entries.ToArray();
        catalog.schemaVersion = 2;
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        BuildStages();
        Debug.Log("Extracted enemy visual catalog: " + entries.Count + " entries.");
    }
    static string Reference(string text, string field) => Regex.Match(text, @"(?m)^  " + field + @": .*guid: ([a-f0-9]{32})").Groups[1].Value;
    static void BuildStages()
    {
        if (!Directory.Exists(Source)) return;
        var stages = new List<ExtractedStageCatalog.Stage>();
        foreach (string biome in new[] { "Forest", "Desert" })
        for (int i = 1; i <= 3; i++)
        {
            string id = "Stage" + biome + i, path = Path.Combine(Source, id + ".asset");
            if (!File.Exists(path)) continue;
            string yaml = File.ReadAllText(path);
            var floor = Regex.Match(yaml, @"flatMaterials:\s*- .*guid: ([a-f0-9]{32})");
            stages.Add(new ExtractedStageCatalog.Stage { id = id,
                floor = Load<Material>(floor.Groups[1].Value), ambient = ReadColor(yaml, "ambienceColor"),
                light = ReadColor(yaml, "lightColor"), sky = ReadColor(yaml, "fogColor"), intensity = Number(yaml, "lightIntensity", 1) });
        }
        if (stages.Count == 0) return;
        const string destination = "Assets/Resources/ExtractedStageCatalog.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<ExtractedStageCatalog>(destination);
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<ExtractedStageCatalog>(); AssetDatabase.CreateAsset(catalog, destination); }
        catalog.stages = stages.ToArray(); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
    }
    static Color ReadColor(string yaml, string field)
    {
        var match = Regex.Match(yaml, field + @": \{r: ([-\d.]+), g: ([-\d.]+), b: ([-\d.]+), a: ([-\d.]+)\}");
        return match.Success ? new Color(Parse(match.Groups[1].Value), Parse(match.Groups[2].Value), Parse(match.Groups[3].Value), Parse(match.Groups[4].Value)) : Color.white;
    }
    static float Parse(string value) => float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    static ExtractedEnemyCatalog.Attack[] ReadAttacks(string yaml)
    {
        var result = new List<ExtractedEnemyCatalog.Attack>();
        foreach (Match match in Regex.Matches(yaml, @"(?ms)^  - isEnabled: 1\r?\n(.*?)(?=^  - |^  [a-zA-Z]|\z)"))
        {
            string block = match.Groups[1].Value;
            string name = Regex.Match(block, @"attackName: ([^\r\n]+)").Groups[1].Value;
            if (name.Length == 0) continue;
            result.Add(new ExtractedEnemyCatalog.Attack { name = name,
                charge = Number(block, "attackChargeTime", 1), radius = Number(block, "attackRadius", 3),
                cooldown = Number(block, "attackCooldown", 8), initialCooldown = Number(block, "initialCooldown", 0),
                range = Number(block, "triggerDistance", 60), recovery = Number(block, "nextSpecialAttackCooldown", 3),
                damageMultiplier = Number(block, "damageMultiplier", 1), duration = Number(block, "duration", 0),
                endLag = Number(block, "endLag", 0) });
        }
        return result.ToArray();
    }
    static float Number(string text, string field, float fallback)
    {
        var m = Regex.Match(text, @"(?m)^ +" + field + @": ([-\d.]+)");
        return m.Success ? Parse(m.Groups[1].Value) : fallback;
    }
    static T Load<T>(string guid) where T : UnityEngine.Object
    {
        if (guid.Length != 32) return null;
        using (var hash = MD5.Create())
        {
            string mapped = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes("UnityShooter.Visuals." + guid))).Replace("-", "").ToLowerInvariant();
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(mapped));
        }
    }
}

public class ExtractedEnemyCatalogBuildHook : IPreprocessBuildWithReport
{
    public int callbackOrder => 50;
    public void OnPreprocessBuild(BuildReport report) { ExtractedEnemyCatalogBuilder.Build(); }
}
