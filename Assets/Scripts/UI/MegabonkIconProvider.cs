using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Carga los iconos PNG del Press Kit desde Resources y los reutiliza en la UI.
/// El catálogo conserva un emoji como respaldo para cualquier ítem sin imagen.
/// </summary>
public static class MegabonkIconProvider
{
    private static readonly Dictionary<string, string> ItemFiles = new Dictionary<string, string>
    {
        { "moldy_cheese", "ItemMoldyCheese" }, { "borgar", "ItemBorgor" }, { "wrench", "ItemWrench" },
        { "clover", "itemClover" }, { "time_bracelet", "ItemTimeBracelet" }, { "slippery_ring", "ItemSlipperyRing" },
        { "golden_glove", "ItemGoldenGlove" }, { "gym_sauce", "ItemGymSauce" }, { "oats", "ItemOats" },
        { "medkit", "ItemMedkit" }, { "turbo_socks", "ItemTurboSocks" }, { "tactical_goggles", "ItemTacticalGoggles" },
        { "battery", "ItemBattery" }, { "forbidden_juice", "ItemShadowJuice" }, { "cursed_doll", "ItemCursedDoll" },
        { "boss_buster", "ItemBossBuster" }, { "key", "ItemKey" }, { "ice_crystal", "ItemIceCrystal" },
        { "skuleg", "ItemSkuleg" }, { "ghost", "ItemGhost" }, { "cactus", "ItemCactus" },
        { "credit_card_red", "ItemCreditCardRed" }, { "golden_shield", "ItemGoldenShield" }, { "thunder_mitts", "ItemGlovesLightning" },
        { "feathers", "ItemFeathers" }, { "electric_plug", "ItemElectricPlug" }, { "cowards_cloak", "ItemSpeedyCloak" },
        { "unstable_transfusion", "Unstable Transfusion" }, { "beer", "ItemBeer" }, { "beacon", "Itembeacon" },
        { "backpack", "ItemBackpack" }, { "phantom_shroud", "ItemPhantomShroud" }, { "campfire", "ItemCampfire" },
        { "brass_knuckles", "ItemBrassKnuckle" }, { "demonic_blade", "ItemDemonBlade" }, { "demonic_blood", "ItemDemonBlood" },
        { "echo_shard", "ItemEchoShard" }, { "moldy_gloves", "ItemGlovesPoison" }, { "golden_sneakers", "ItemGoldenSneakers" },
        { "idle_juice", "IdleJuice" }, { "leeching_crystal", "ItemLeechingCrystal" }, { "kevin", "Kevin" },
        { "beefy_ring", "ItemBeefyRing" }, { "credit_card_green", "ItemCreditCardGreen" }, { "spiky_shield", "ItemSpikyShieldf" },
        { "scarf", "ItemScarf" }, { "slurp_gloves", "ItemGlovesBloody" }, { "mirror", "ItemMirror" },
        { "turbo_skates", "ItemRollerblades" }, { "slutty_cannon", "ItemSluttyCannon" }, { "shattered_knowledge", "ItemShatteredWisdom" },
        { "quins_mask", "ItemOldMask" }, { "toxic_barrel", "ToxicBarrel" }, { "gas_mask", "Gasmask" },
        { "gamer_goggles", "GamerLens" }, { "eagle_claw", "ItemEagleClaw" }, { "demonic_soul", "ItemDemonSoul" },
        { "bob_dead", "ItemBobDead" }, { "grandmas_secret_tonic", "GrandmasSecretTonic" }, { "cursed_grabbies", "ItemGlovesCursed" },
        { "op_lamp", "ItemOverpoweredLamp" }, { "za_warudo", "ItemZaWarudo" }, { "big_bonk", "ItemBonker" },
        { "spicy_meatball", "ItemSpicyMeatball" }, { "ice_cube", "ItemIceCube" }, { "giant_fork", "ItemFork" },
        { "power_gloves", "ItemGlovesPower" }, { "lightning_orb", "ItemLightningOrb" }, { "chonkplate", "Chonkplate" },
        { "dragonfire", "ItemDragonfire" }, { "sucky_magnet", "ItemSuckyHoof" }, { "holy_book", "ItemHolyBook" },
        { "speed_boi", "Speed boi" }, { "energy_core", "ItemEnergyCore" }, { "anvil", "ItemAnvil" },
        { "soul_harvester", "ItemSoulHarvester" }, { "joes_dagger", "JoesDagger" }, { "bloody_cleaver", "Bloody Cleaver" }
    };

    private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

    private static readonly Dictionary<string, string> WeaponFiles = new Dictionary<string, string>
    {
        { "Revolver", "Revolver" }, { "Shotgun", "Shotgun" }, { "SMG", "SluttyRocket" }, { "Crossbow", "Bow" }
    };

    private static readonly Dictionary<string, string> CharacterFiles = new Dictionary<string, string>
    {
        { "CL4NK", "_0019_Clank" }, { "MEGACHAD", "_0001_Megachad" },
        { "SIR OOFIE", "_0004_Knight" }, { "CALCIUM", "_0003_Calcium" }
    };

    // Orden del Press Kit para la cuadrícula de Tomos del menú Desbloqueos.
    private static readonly string[] TomeFiles =
    {
        "ArmorTome", "AttractionTome", "BloodTOme", "ChaosTome", "CooldownTome", "CursedTome",
        "DamageTome", "DurationTome", "EvasionTome", "GoldenTome", "HealthTome", "KnockbackTome",
        "LuckTome", "PrecisionTome", "ProjectileSpeedTOme", "QuantityTome", "RegenerationTome",
        "Retaliation", "ShieldTome", "SilverTome", "SizeTome", "SpeedTome", "XpTome"
    };

    private static Texture2D Load(string path)
    {
        if (!Cache.TryGetValue(path, out Texture2D texture))
        {
            texture = Resources.Load<Texture2D>(path);
            Cache[path] = texture;
        }
        return texture;
    }

    public static bool DrawItemIcon(Rect rect, string id)
    {
        if (string.IsNullOrEmpty(id) || !ItemFiles.TryGetValue(id, out string file)) return false;
        Texture2D texture = Load("Megabonk/Game Icons/Items/" + file);
        if (texture == null) return false;
        GUI.color = Color.white;
        GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
        return true;
    }

    public static Texture2D ItemTexture(string id)
    {
        if(!ItemFiles.TryGetValue(id,out var file)) return null;
        return Load("Megabonk/Game Icons/Items/"+file) ?? Load("ExtractedVisuals/Texture2D/"+file);
    }
    public static Texture2D UpgradeTexture(string id)
    {
        var def=UpgradeCatalog.Get(id);
        if(def==null) return null;
        return Load("Megabonk/Game Icons/"+(def.category=="ARMA"?"Weapons/":"Tomes/")+def.icon)
            ?? Load("ExtractedVisuals/Texture2D/"+def.icon);
    }

    public static void DrawUpgradeIcon(Rect rect, UpgradeDefinition upgrade)
    {
        if (upgrade.id.StartsWith("mb_"))
        {
            Texture2D asset = Load("Megabonk/Game Icons/" + (upgrade.category == "ARMA" ? "Weapons/" : "Tomes/") + upgrade.icon);
            if(asset==null) asset=Load("ExtractedVisuals/Texture2D/"+upgrade.icon);
            if (asset != null) { GUI.color = Color.white; GUI.DrawTexture(rect, asset, ScaleMode.ScaleToFit, true); }
            return;
        }
        string id = upgrade.id;
        string weapon = id.StartsWith("magic_scythe") ? "Scythe"
            : id.StartsWith("dragon_egg") ? "DragonsBreath"
            : id.StartsWith("ghost_friend") ? "Space Noodle"
            : id.StartsWith("pyro") ? "Firestaff"
            : id.StartsWith("electro") ? "LightningStaff"
            : id.StartsWith("magic_lens") ? "Aura" : null;
        if (weapon != null)
        {
            Texture2D icon = Load("Megabonk/Game Icons/Weapons/" + weapon);
            if (icon != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
                return;
            }
        }
        int tome = id.Contains("speed") ? 21 : id.Contains("cadence") || id.Contains("rapid") ? 4
            : id.Contains("vitality") ? 10 : id.Contains("magnet") ? 1
            : id.Contains("shield") ? 18 : id.Contains("reload") ? 4 : 6;
        DrawTomeIcon(rect, tome);
    }

    public static bool DrawWeaponIcon(Rect rect, string weaponName)
    {
        if (string.IsNullOrEmpty(weaponName)) return false;
        string key = weaponName.Contains("Revolver") ? "Revolver" : weaponName.Contains("Shotgun") ? "Shotgun" : weaponName.Contains("SMG") ? "SMG" : "Crossbow";
        if (!WeaponFiles.TryGetValue(key, out string file)) return false;
        Texture2D texture = Load("Megabonk/Game Icons/Weapons/" + file);
        if (texture == null) return false;
        GUI.color = Color.white;
        GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
        return true;
    }

    public static bool DrawCharacterIcon(Rect rect, string characterName)
    {
        if (string.IsNullOrEmpty(characterName) || !CharacterFiles.TryGetValue(characterName.ToUpperInvariant(), out string file)) return false;
        Texture2D texture = Load("Megabonk/Characters/" + file);
        if (texture == null) return false;
        GUI.color = Color.white;
        GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
        return true;
    }

    public static bool DrawTomeIcon(Rect rect, int index)
    {
        if (index < 0 || index >= TomeFiles.Length) return false;
        Texture2D texture = Load("Megabonk/Game Icons/Tomes/" + TomeFiles[index]);
        if (texture == null) return false;
        GUI.color = Color.white;
        GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
        return true;
    }
}
