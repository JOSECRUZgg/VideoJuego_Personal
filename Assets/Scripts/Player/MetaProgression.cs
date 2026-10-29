using System.Collections.Generic;
using UnityEngine;

/// <summary>Progreso permanente del perfil: compras de plata y preferencias del Toggler.</summary>
public class MetaProgression : MonoBehaviour
{
    public static MetaProgression Instance { get; private set; }

    public int damageRank { get; private set; }
    public int speedRank { get; private set; }
    public int vitalityRank { get; private set; }
    public int luckRank { get; private set; }
    public int weaponSlotRank { get; private set; }
    public int tomeSlotRank { get; private set; }
    private readonly HashSet<string> disabledUpgrades = new HashSet<string>();

    const string Prefix = "MegabonkMeta.";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Load();
    }

    void Load()
    {
        damageRank = PlayerPrefs.GetInt(Prefix + "Damage", 0);
        speedRank = PlayerPrefs.GetInt(Prefix + "Speed", 0);
        vitalityRank = PlayerPrefs.GetInt(Prefix + "Vitality", 0);
        luckRank = PlayerPrefs.GetInt(Prefix + "Luck", 0);
        weaponSlotRank = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "WeaponSlot", 0), 0, 1);
        tomeSlotRank = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "TomeSlot", 0), 0, 1);
        disabledUpgrades.Clear();
        foreach (string id in PlayerPrefs.GetString(Prefix + "Disabled", "").Split('|'))
            if (!string.IsNullOrEmpty(id)) disabledUpgrades.Add(id);
    }

    public int GetRank(string id)
    {
        switch (id) { case "damage": return damageRank; case "speed": return speedRank; case "vitality": return vitalityRank; case "luck": return luckRank; case "weapon_slot": return weaponSlotRank; case "tome_slot": return tomeSlotRank; }
        return 0;
    }

    public int GetMaxRank(string id) => id == "weapon_slot" || id == "tome_slot" ? 1 : 5;
    public int GetCost(string id) => id == "weapon_slot" ? 400 : id == "tome_slot" ? 400 : 50 + GetRank(id) * 75;

    public bool Purchase(string id, GameManager gm)
    {
        if (id != "damage" && id != "speed" && id != "vitality" && id != "luck" &&
            id != "weapon_slot" && id != "tome_slot") return false;
        if (gm == null || GetRank(id) >= GetMaxRank(id) || gm.silver < GetCost(id)) return false;
        gm.silver -= GetCost(id);
        switch (id)
        {
            case "damage": damageRank++; break;
            case "speed": speedRank++; break;
            case "vitality": vitalityRank++; break;
            case "luck": luckRank++; break;
            case "weapon_slot": weaponSlotRank++; break;
            case "tome_slot": tomeSlotRank++; break;
        }
        PlayerPrefs.SetInt("MegabonkSilver", gm.silver);
        Save();
        return true;
    }

    public void ApplyRunBonuses(XPSystem xp, PlayerHealth3D health)
    {
        if (xp != null)
        {
            xp.damageMultiplier += damageRank * 0.05f;
            xp.speedMultiplier += speedRank * 0.04f;
        }
        if (health != null && vitalityRank > 0) health.IncreaseMaxHearts(vitalityRank);
        if (MegabonkRelicManager.Instance != null) MegabonkRelicManager.Instance.luckPercent += luckRank * 5f;
    }

    public bool IsDisabled(string upgradeId) => disabledUpgrades.Contains(upgradeId);

    public void ToggleUpgrade(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return;
        if (!disabledUpgrades.Add(upgradeId)) disabledUpgrades.Remove(upgradeId);
        Save();
    }

    void Save()
    {
        PlayerPrefs.SetInt(Prefix + "Damage", damageRank);
        PlayerPrefs.SetInt(Prefix + "Speed", speedRank);
        PlayerPrefs.SetInt(Prefix + "Vitality", vitalityRank);
        PlayerPrefs.SetInt(Prefix + "Luck", luckRank);
        PlayerPrefs.SetInt(Prefix + "WeaponSlot", weaponSlotRank);
        PlayerPrefs.SetInt(Prefix + "TomeSlot", tomeSlotRank);
        PlayerPrefs.SetString(Prefix + "Disabled", string.Join("|", disabledUpgrades));
        PlayerPrefs.Save();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}
