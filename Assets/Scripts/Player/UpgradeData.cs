using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Catálogo jugable de armas y tomos. Los tipos antiguos se conservan por compatibilidad.
/// </summary>
public enum UpgradeType
{
    Standard,
    Synergy,
    BossTome
}

[System.Serializable]
public class UpgradeDefinition
{
    public string id;
    public string name;
    public string category;
    public string treeId;
    public int tier; // 1 to 4
    public string prerequisiteId; // ID del nivel anterior
    public string[] synergyRequirements; // Exactamente 2 IDs requeridos
    public UpgradeType type;
    public string icon;
    public string description;
    public Color color;

    public UpgradeDefinition(
        string id, string name, string category, string treeId, int tier,
        string prerequisiteId, string[] synergyRequirements, UpgradeType type,
        string icon, string description, Color color)
    {
        this.id = id;
        this.name = name;
        this.category = category;
        this.treeId = treeId;
        this.tier = tier;
        this.prerequisiteId = prerequisiteId;
        this.synergyRequirements = synergyRequirements;
        this.type = type;
        this.icon = icon;
        this.description = description;
        this.color = color;
    }
}

public static class UpgradeCatalog
{
    private static List<UpgradeDefinition> allUpgrades;
    private static Dictionary<string, UpgradeDefinition> upgradeDict;

    public static List<UpgradeDefinition> AllUpgrades
    {
        get
        {
            if (allUpgrades == null) Initialize();
            return allUpgrades;
        }
    }

    public static UpgradeDefinition Get(string id)
    {
        if (upgradeDict == null) Initialize();
        return upgradeDict.TryGetValue(id, out var def) ? def : null;
    }

    private static void Initialize()
    {
        allUpgrades = new List<UpgradeDefinition>();
        upgradeDict = new Dictionary<string, UpgradeDefinition>(StringComparer.OrdinalIgnoreCase);
        AddBuild("mb_revolver", "Revólver", "ARMA", "Revolver", "Seis balas con rebote. Más daño y rebotes al mejorar.");
        AddBuild("mb_bone", "Hueso", "ARMA", "Bone", "Huesos que rebotan entre enemigos. Escala con daño y cantidad.");
        AddBuild("mb_banana", "Banana", "ARMA", "Bananarang", "Lanza bananas giratorias que atraviesan enemigos y regresan al personaje.");
        AddBuild("mb_sword", "Espada", "ARMA", "Sword", "Cortes automáticos contra enemigos cercanos.");
        AddBuild("mb_katana", "Katana", "ARMA", "Katana", "Cortes de gran daño con +25% de probabilidad crítica.");
        AddBuild("mb_bow", "Arco", "ARMA", "Bow", "Flechas perforantes que atraviesan varios enemigos.");
        AddBuild("mb_lightning", "Bastón de rayos", "ARMA", "LightningStaff", "Rayos encadenados entre enemigos cercanos.");
        AddBuild("mb_fire", "Bastón de fuego", "ARMA", "Firestaff", "Proyectiles incendiarios con explosión y quemadura.");
        AddBuild("mb_aura", "Aura", "ARMA", "Aura", "Daño periódico en un círculo alrededor del personaje.");
        AddBuild("mb_rocket", "Misiles", "ARMA", "SluttyRocket", "Misiles teledirigidos con daño explosivo.");
        foreach (var weapon in ExtendedWeapons.All)
            AddBuild(weapon.id, weapon.name, "ARMA", weapon.Icon, weapon.description);
        AddBuild("mb_damage", "Tomo de Daño", "TOMO", "DamageTome", "+8% de daño global.");
        AddBuild("mb_cooldown", "Tomo de Enfriamiento", "TOMO", "CooldownTome", "+8% de velocidad de ataque global.");
        AddBuild("mb_quantity", "Tomo de Cantidad", "TOMO", "QuantityTome", "+1 proyectil o salto de rayo para todas las armas compatibles.");
        AddBuild("mb_speed", "Tomo de Agilidad", "TOMO", "SpeedTome", "+5% de velocidad de movimiento.");
        AddBuild("mb_size", "Tomo de Tamaño", "TOMO", "SizeTome", "+8% de tamaño de proyectiles y áreas.");
        AddBuild("mb_attraction", "Tomo de Atracción", "TOMO", "AttractionTome", "+12% de radio de recogida.");
        AddBuild("mb_xp", "Tomo de Experiencia", "TOMO", "XpTome", "+8% de experiencia.");
        AddBuild("mb_luck", "Tomo de Suerte", "TOMO", "LuckTome", "+5% de suerte en cofres.");
        AddBuild("mb_cursed", "Tomo de Maldito", "TOMO", "CursedTome", "+5% de dificultad y +5% de experiencia.");
        AddBuild("mb_health", "Tomo de Vida", "TOMO", "HealthTome", "+10 de vida máxima.");
        AddBuild("mb_armor", "Tomo de Armadura", "TOMO", "ArmorTome", "+4 de armadura.");
        AddBuild("mb_regen", "Tomo de Regeneración", "TOMO", "RegenerationTome", "+1 de vida cada 2 segundos.");
        AddBuild("mb_precision", "Tomo de Precisión", "TOMO", "PrecisionTome", "+5% de probabilidad de crítico. Puede superar el 100%.");
    }

    private static void AddBuild(string id, string name, string category, string icon, string description)
    {
        var def = new UpgradeDefinition(id, name, category, id, 1, null, null, UpgradeType.Standard, icon, description, category == "ARMA" ? Color.yellow : Color.cyan);
        allUpgrades.Add(def);
        upgradeDict[id] = def;
    }

    private static void AddStd(string id, string name, string category, string treeId, int tier,
        string prereq, string icon, string desc, Color col)
    {
        var u = new UpgradeDefinition(id, name, category, treeId, tier, prereq, null, UpgradeType.Standard, icon, desc, col);
        allUpgrades.Add(u);
        upgradeDict[id] = u;
    }

    private static void AddSyn(string id, string name, string category, string req1, string req2,
        string icon, string desc, Color col)
    {
        var u = new UpgradeDefinition(id, name, category, null, 0, null, new string[] { req1, req2 }, UpgradeType.Synergy, icon, desc, col);
        allUpgrades.Add(u);
        upgradeDict[id] = u;
    }

    private static void AddTome(string id, string name, string category, string icon, string desc, Color col)
    {
        var u = new UpgradeDefinition(id, name, category, null, 0, null, null, UpgradeType.BossTome, icon, desc, col);
        allUpgrades.Add(u);
        upgradeDict[id] = u;
    }
}
