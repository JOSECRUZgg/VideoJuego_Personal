using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class OriginalContentBuilder
{
    const string Source = @"C:\Users\JOSEC\Desktop\MATERIAS UT\10MO CUATRIMESTRE\JUAN\MEGABONK\ExportedProject\Assets";
    const string Target = "Assets/Resources/OriginalContentCatalog.asset";
    static readonly Dictionary<string, string> originals = new Dictionary<string, string>();
    static readonly List<string> report = new List<string>();
    static readonly HashSet<string> missing = new HashSet<string>();
    static OriginalContentBuilder() { EditorApplication.delayCall += Ensure; }
    static void Ensure()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Ensure; return; }
        var catalog = AssetDatabase.LoadAssetAtPath<OriginalContentCatalog>(Target);
        if (catalog == null || catalog.schemaVersion < 3) BuildAndAudit();
    }
    static string Read(string path) => File.Exists(path) ? File.ReadAllText(path) : "";
    static string Ref(string text, string field) => Regex.Match(text, @"(?m)^\s+" + field + @":\s*\{[^\r\n]*guid: ([a-f0-9]{32})").Groups[1].Value;
    static float N(string text, string field, float fallback = 0)
    {
        var match = Regex.Match(text, @"(?m)^\s+(?:- )?" + field + @": ([-\d.Ee+]+)");
        return match.Success ? float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : fallback;
    }
    static string Remapped(string guid)
    {
        using (var hash = MD5.Create())
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes("UnityShooter.Visuals." + guid))).Replace("-", "").ToLowerInvariant();
    }
    static T Load<T>(string guid) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(guid)) return null;
        string path = AssetDatabase.GUIDToAssetPath(Remapped(guid));
        if (string.IsNullOrEmpty(path)) path = AssetDatabase.GUIDToAssetPath(guid);
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null && originals.TryGetValue(guid, out var original))
        {
            string imported = "Assets/Resources/ExtractedVisuals/" + original.Substring(Source.Length + 1).Replace('\\', '/');
            asset = AssetDatabase.LoadAssetAtPath<T>(imported);
        }
        if (asset == null) missing.Add(typeof(T).Name + ": " + (originals.TryGetValue(guid, out var file) ? file : "GUID " + guid));
        return asset;
    }
    static string SourceText(string guid) => originals.TryGetValue(guid, out var path) ? Read(path) : "";
    static string SourceName(string guid) => originals.TryGetValue(guid, out var path) ? Path.GetFileNameWithoutExtension(path) : "";
    static Color ColorField(string text, string field)
    {
        var m = Regex.Match(text, field + @": \{r: ([\d.Ee+-]+), g: ([\d.Ee+-]+), b: ([\d.Ee+-]+), a: ([\d.Ee+-]+)\}");
        if (!m.Success) return Color.white;
        return new Color(float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
            float.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture), float.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture));
    }

    [MenuItem("Tools/Megabonk/Integrar desierto, cofres y personajes originales")]
    public static void BuildAndAudit()
    {
        if (!Directory.Exists(Source)) throw new DirectoryNotFoundException(Source);
        originals.Clear(); report.Clear(); missing.Clear();
        foreach (string meta in Directory.EnumerateFiles(Source, "*.meta", SearchOption.AllDirectories))
        {
            var m = Regex.Match(Read(meta), @"(?m)^guid: ([a-f0-9]{32})");
            if (m.Success) originals[m.Groups[1].Value] = meta.Substring(0, meta.Length - 5);
        }
        var catalog = AssetDatabase.LoadAssetAtPath<OriginalContentCatalog>(Target);
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<OriginalContentCatalog>(); AssetDatabase.CreateAsset(catalog, Target); }
        var characters = new List<OriginalContentCatalog.Character>();
        string localization = Read(Path.Combine(Source, "Localization/String Tables/Characters_es.asset"));
        foreach (string file in Directory.EnumerateFiles(Path.Combine(Source, "MonoBehaviour"), "*.asset"))
        {
            string yaml = Read(file);
            if (!Regex.IsMatch(yaml, @"(?m)^  eCharacter:")) continue;
            var prefab = Load<GameObject>(Ref(yaml, "prefab"));
            string id = Path.GetFileNameWithoutExtension(file);
            if (prefab == null || !ValidateModel(prefab, "Character " + id)) continue;
            string name = id.Replace("_0", "");
            var key = Regex.Match(yaml, @"localizedName:[\s\S]*?m_KeyId: (\d+)");
            if (key.Success)
            {
                var label = Regex.Match(localization, @"m_Id: " + key.Groups[1].Value + @"\r?\n\s+m_Localized: ([^\r\n]+)");
                if (label.Success) name = label.Groups[1].Value.Trim('"');
            }
            var legacy = id == "Clank" ? GameManager.CharacterType.CL4NK : id == "Calcium" ? GameManager.CharacterType.Calcium :
                id == "Knight" ? GameManager.CharacterType.SirOofie : id == "Megachad" ? GameManager.CharacterType.Megachad : GameManager.CharacterType.Original;
            string weapon = SourceName(Ref(yaml, "weapon"));
            var weapons = new Dictionary<string,string> { {"WeaponRevolver","mb_revolver"}, {"WeaponBone","mb_bone"}, {"WeaponSword","mb_sword"},
                {"WeaponAura","mb_aura"}, {"WeaponFireball","mb_fire"}, {"WeaponLightningStaff","mb_lightning"}, {"WeaponBow","mb_bow"},
                {"WeaponBanana","mb_banana"}, {"WeaponBananarang","mb_banana"}, {"WeaponFireStaff","mb_fire"},
                {"WeaponKatana","mb_katana"}, {"WeaponRocket","mb_rocket"} };
            string weaponId = ExtendedWeapons.Resolve(weapon, weapons.TryGetValue(weapon, out var mapped) ? mapped : "mb_revolver");
            report.Add("Arma: " + id + " / " + weapon + " -> " + weaponId);
            characters.Add(new OriginalContentCatalog.Character { id = id, displayName = name, prefab = prefab, icon = Load<Texture2D>(Ref(yaml, "icon")),
                legacy = legacy, weaponId = weaponId, originalWeapon = weapon });
            report.Add("Personaje: " + id + " -> " + AssetDatabase.GetAssetPath(prefab));
            foreach (var animator in prefab.GetComponentsInChildren<Animator>(true))
            {
                if (animator.runtimeAnimatorController == null) { report.Add(id + ": sin controller original; conserva pose de autor."); continue; }
                report.Add("  Animación: " + animator.runtimeAnimatorController.name + " / " + animator.runtimeAnimatorController.animationClips.Length + " clips.");
            }
        }
        catalog.characters = characters.OrderBy(c => (int)c.legacy).ThenBy(c => c.displayName).ToArray();
        var stages = new List<OriginalContentCatalog.DesertStage>();
        for (int index = 1; index <= 3; index++)
        {
            string id = "StageDesert" + index;
            string yaml = Read(Path.Combine(Source, "MonoBehaviour/" + id + ".asset"));
            if (yaml.Length == 0) { missing.Add(id + ".asset"); continue; }
            string noise = SourceText(Ref(yaml, "proceduralNoiseData"));
            string terrain = SourceText(Ref(yaml, "proceduralTerrainData"));
            var stage = new OriginalContentCatalog.DesertStage { id = id,
                noiseScale = N(noise,"noiseScale",100), persistence = N(noise,"persistance",.4f), lacunarity = N(noise,"lacunarity",3),
                octaves = (int)N(noise,"octaves",2), seed = (int)N(noise,"seed",257), heightMultiplier = N(terrain,"heightMultiplier",11),
                uniformScale = N(terrain,"uniformScale",1) * N(yaml,"proceduralMapScale",1),
                ambient = ColorField(yaml,"ambienceColor"), sunlight = ColorField(yaml,"lightColor"), sky = ColorField(yaml,"fogColor"),
                lightIntensity = N(yaml,"lightIntensity",1), farClip = N(yaml,"farClipPlane",800) };
            var offset = Regex.Match(noise, @"offset: \{x: ([-\d.]+), y: ([-\d.]+)\}");
            if (offset.Success) stage.offset = new Vector2(float.Parse(offset.Groups[1].Value,CultureInfo.InvariantCulture), float.Parse(offset.Groups[2].Value,CultureInfo.InvariantCulture));
            var keys = new List<Keyframe>();
            foreach (Match m in Regex.Matches(terrain, @"time: ([-\d.Ee+]+)\s+value: ([-\d.Ee+]+)\s+inSlope: ([-\d.Ee+]+)\s+outSlope: ([-\d.Ee+]+)"))
                keys.Add(new Keyframe(float.Parse(m.Groups[1].Value,CultureInfo.InvariantCulture),float.Parse(m.Groups[2].Value,CultureInfo.InvariantCulture),
                    float.Parse(m.Groups[3].Value,CultureInfo.InvariantCulture),float.Parse(m.Groups[4].Value,CultureInfo.InvariantCulture)));
            stage.heightCurve = new AnimationCurve(keys.ToArray());
            string objects = Regex.Match(yaml, @"(?ms)^  randomMapObjects:\s*\r?\n(.*?)^  stageTilePrefabs:").Groups[1].Value;
            var groups = new List<OriginalContentCatalog.Decoration>();
            foreach (string group in Regex.Split(objects, @"(?m)(?=^  - amount:)"))
            {
                if (!group.Contains("amount:")) continue;
                var prefabs = Regex.Matches(group,@"guid: ([a-f0-9]{32})").Cast<Match>().Select(m => Load<GameObject>(m.Groups[1].Value))
                    .Where(p => p != null && ValidateModel(p, id)).ToArray();
                groups.Add(new OriginalContentCatalog.Decoration { prefabs = prefabs, amount = (int)N(group,"amount"), scaleMin = N(group,"scaleMin",1),
                    scaleMax = N(group,"scaleMax",1), checkRadius = N(group,"checkRadius",1), maxSlopeAngle = N(group,"maxSlopeAngle",90),
                    upOffset = N(group,"upOffset"), alignWithNormal = N(group,"alignWithNormal") != 0 });
                report.Add(id + ": " + N(group,"amount") + " / " + string.Join(", ", prefabs.Select(p => p.name)));
            }
            stage.decorations = groups.ToArray();
            stage.terrainMaterial = BuildTerrainMaterial(Ref(yaml,"triplanarMaterial"), id);
            stages.Add(stage);
        }
        catalog.desertStages = stages.ToArray();
        catalog.animatedChest = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ExtractedVisuals/GameObject/OpenChest.prefab");
        catalog.chestController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Resources/ExtractedVisuals/AnimatorController/Animator_Chest.controller");
        ValidateModel(catalog.animatedChest, "Animated chest");
        if (catalog.chestController != null && catalog.animatedChest != null)
        {
            foreach (var clip in catalog.chestController.animationClips.Distinct())
            {
                report.Add("Cofre: " + clip.name + ", duración " + clip.length.ToString("0.000",CultureInfo.InvariantCulture) + " s");
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    if (!string.IsNullOrEmpty(binding.path) && catalog.animatedChest.transform.Find(binding.path) == null)
                        missing.Add("Cofre " + clip.name + ": ruta de animación " + binding.path);
            }
        }
        else missing.Add("Animator_Chest.controller o OpenChest.prefab ausente");
        string chest = Read(Path.Combine(Source,"GameObject/OpenChest.prefab"));
        var sounds = Regex.Matches(chest,@"m_audioClip: .*guid: ([a-f0-9]{32})").Cast<Match>().Select(m => Load<AudioClip>(m.Groups[1].Value)).Where(a => a != null).ToArray();
        catalog.chestOpenSound = sounds.FirstOrDefault(); catalog.chestTakeSound = sounds.Skip(1).FirstOrDefault();
        catalog.schemaVersion = 3;
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        report.Insert(0, "Personajes completos: " + characters.Count + ". Etapas: " + stages.Count + ".");
        report.Add("Se reutilizan los GUID ya importados; no se regeneran metas. Los GUID remapeados proceden de la importación previa.");
        report.Add("Los cuerpos C# exportados de generación son stubs. Se reconstruye la distribución con parámetros serializados, no una copia exacta del algoritmo perdido.");
        report.Add("El shader compilado original no tiene fuente utilizable; se adapta a URP con sus texturas de arena/pared, escalas y colores.");
        report.Add("Dependencias faltantes o inválidas:\n" + (missing.Count == 0 ? "Ninguna detectada en los recursos integrados." : string.Join("\n",missing)));
        Directory.CreateDirectory("Docs"); File.WriteAllLines("Docs/OriginalContentAudit.txt",report);
        Debug.Log("Original integration: " + characters.Count + " characters; " + stages.Count + " desert stages; issues: " + missing.Count);
    }

    static bool ValidateModel(GameObject prefab, string context)
    {
        if (prefab == null) { missing.Add(context + ": prefab ausente"); return false; }
        bool mesh = false, valid = true;
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            if (renderer.name.StartsWith("MinimapIcon") || renderer.name.StartsWith("Ray")) continue;
            var filter = renderer.GetComponent<MeshFilter>();
            var geometry = renderer is SkinnedMeshRenderer skin ? skin.sharedMesh : filter != null ? filter.sharedMesh : null;
            if (geometry == null) { missing.Add(context + "/" + renderer.name + ": mesh"); valid = false; continue; }
            mesh = true;
            if (renderer.sharedMaterials.Length == 0 || renderer.sharedMaterials.Any(m => m == null))
            { missing.Add(context + "/" + renderer.name + ": material"); valid = false; }
        }
        if (!mesh) missing.Add(context + ": sin geometría");
        return mesh && valid;
    }
    static Material BuildTerrainMaterial(string guid, string id)
    {
        string text = SourceText(guid);
        var shader = Shader.Find("Game/OriginalDesertSurface");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/" + id + "Surface.mat");
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material,"Assets/Resources/" + id + "Surface.mat"); }
        material.shader = shader;
        foreach (var property in new[] { "_MainTex", "_MainTexSide" })
        {
            var reference = Regex.Match(text, property + @":\s+m_Texture: .*guid: ([a-f0-9]{32})");
            material.SetTexture(property,Load<Texture2D>(reference.Groups[1].Value));
        }
        material.SetFloat("_BaseScale",N(text,"_BaseScale",.04f)); material.SetFloat("_SideScale",N(text,"_SideScale",.04f));
        material.SetColor("_Color",ColorField(text,"_Color")); material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }
}
