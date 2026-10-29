using UnityEngine;

/// <summary>Hitos persistentes usados por el catálogo y las recompensas.</summary>
public static class ExpeditionProgress
{
    public static int Kills => PlayerPrefs.GetInt("Progress.Kills", 0);
    public static float Swarm => PlayerPrefs.GetFloat("Progress.Swarm", 0);
    public static int Chests => PlayerPrefs.GetInt("Progress.Chests", 0);
    public static int Pots => PlayerPrefs.GetInt("Progress.Pots", 0);
    public static int FinalBosses => PlayerPrefs.GetInt("Progress.FinalBosses", 0);
    public static int CriticalHits => PlayerPrefs.GetInt("Progress.CriticalHits", 0);
    public static int Deaths => PlayerPrefs.GetInt("Progress.Deaths", 0);
    public static int CheeseDuplications => PlayerPrefs.GetInt("Progress.CheeseDuplications", 0);
    public static int WeaponKills(string id) => PlayerPrefs.GetInt("Progress.WeaponKills." + id, 0);
    public static int PoisonKills => PlayerPrefs.GetInt("Progress.PoisonKills", 0);
    public static int ThornKills => PlayerPrefs.GetInt("Progress.ThornKills", 0);
    public static int IdleKills => PlayerPrefs.GetInt("Progress.IdleKills", 0);
    public static int LifeStealHealed => PlayerPrefs.GetInt("Progress.LifeStealHealed", 0);
    public static int ChallengeVictories => PlayerPrefs.GetInt("Progress.ChallengeVictories", 0);
    public static int BloodMagicProcs => PlayerPrefs.GetInt("Progress.BloodMagicProcs", 0);
    static bool HasMilestone(string id) => PlayerPrefs.GetInt("Progress.Milestone." + id, 0) != 0;

    public static void RecordChestOpened()
    {
        PlayerPrefs.SetInt("Progress.Chests", Chests + 1);
        PlayerPrefs.Save();
    }
    public static void RecordPotBroken()
    {
        PlayerPrefs.SetInt("Progress.Pots", Pots + 1);
        PlayerPrefs.Save();
    }
    public static void RecordCriticalHit()
    {
        int value = CriticalHits + 1;
        PlayerPrefs.SetInt("Progress.CriticalHits", value);
        if (value % 10 == 0 || value == 100) PlayerPrefs.Save();
    }
    public static void RecordDeath()
    {
        PlayerPrefs.SetInt("Progress.Deaths", Deaths + 1);
        PlayerPrefs.Save();
    }
    public static void RecordMicrowaveDuplicate(string id)
    {
        if (id != "moldy_cheese") return;
        PlayerPrefs.SetInt("Progress.CheeseDuplications", CheeseDuplications + 1);
        PlayerPrefs.Save();
    }
    public static void RecordWeaponKill(string id)
    {
        int goal = id == "mb_frost" ? 50 : id == "mb_sword" || id == "mb_rocket" ? 5000 :
            id == "mb_sniper" || id == "mb_lightning" ? 3000 :
            id == "mb_fire" ? 4000 : id == "mb_dexecutioner" ? 10000 : 0;
        if (goal == 0) return;
        int count = WeaponKills(id) + 1;
        PlayerPrefs.SetInt("Progress.WeaponKills." + id, count);
        if (count == goal || count % 25 == 0) PlayerPrefs.Save();
    }
    public static void RecordSpecialKill(string cause)
    {
        string key = cause == "poison" ? "Progress.PoisonKills" : cause == "thorns" ? "Progress.ThornKills" : null;
        if (key == null) return;
        int value = PlayerPrefs.GetInt(key, 0) + 1;
        PlayerPrefs.SetInt(key, value);
        if (value % 25 == 0 || cause == "thorns" && value == 100 || cause == "poison" && value == 2000) PlayerPrefs.Save();
    }
    public static void RecordIdleKill()
    {
        int count = IdleKills + 1;
        PlayerPrefs.SetInt("Progress.IdleKills", count);
        if (count % 25 == 0 || count == 500) PlayerPrefs.Save();
    }
    public static void RecordLifeStealHeal(int actual)
    {
        if (actual <= 0) return;
        int amount = LifeStealHealed + actual;
        PlayerPrefs.SetInt("Progress.LifeStealHealed", amount);
        if (amount >= 350 || amount % 25 < actual) PlayerPrefs.Save();
    }
    public static void RecordDoubleCurseStage()
    {
        PlayerPrefs.SetInt("Progress.Milestone.speed_boi", 1);
        PlayerPrefs.Save();
    }
    public static void RecordFirstBossPerfect()
    {
        PlayerPrefs.SetInt("Progress.Milestone.holy_book", 1);
        PlayerPrefs.Save();
    }
    public static void RecordAllChargeShrines()
    {
        PlayerPrefs.SetInt("Progress.Milestone.sucky_magnet", 1);
        PlayerPrefs.Save();
    }
    public static void RecordChallengeVictory()
    {
        PlayerPrefs.SetInt("Progress.ChallengeVictories", ChallengeVictories + 1);
        PlayerPrefs.Save();
    }
    public static void RecordBloodMagicProc()
    {
        int count = BloodMagicProcs + 1;
        PlayerPrefs.SetInt("Progress.BloodMagicProcs", count);
        if (count % 25 == 0 || count == 400) PlayerPrefs.Save();
    }
    public static void RecordFinalBossVictory()
    {
        PlayerPrefs.SetInt("Progress.FinalBosses", FinalBosses + 1);
        PlayerPrefs.Save();
    }
    public static void RecordBossOrEliteKill(bool boss, bool elite, float lifetime)
    {
        bool changed = false;
        if (elite && lifetime <= 25f && !HasMilestone("tactical_goggles"))
        {
            PlayerPrefs.SetInt("Progress.Milestone.tactical_goggles", 1);
            changed = true;
        }
        if (boss)
        {
            if (lifetime <= 30f && !HasMilestone("boss_buster"))
            {
                PlayerPrefs.SetInt("Progress.Milestone.boss_buster", 1);
                changed = true;
            }
            var gm = GameManager.Instance;
            if (gm != null && gm.selectedOriginalCharacter == "Athena" && !HasMilestone("quins_mask"))
            {
                PlayerPrefs.SetInt("Progress.Milestone.quins_mask", 1);
                changed = true;
            }
            var health = Object.FindAnyObjectByType<PlayerHealth3D>();
            if (health != null && health.currentHealth > 0 && health.currentHealth <= health.maxHealth * .1f && !HasMilestone("gamer_goggles"))
            {
                PlayerPrefs.SetInt("Progress.Milestone.gamer_goggles", 1);
                changed = true;
            }
        }
        if (changed) PlayerPrefs.Save();
    }
    public static void RecordMaxHealth(int amount)
    {
        if (amount < 500 || HasMilestone("chonkplate")) return;
        PlayerPrefs.SetInt("Progress.Milestone.chonkplate", 1);
        PlayerPrefs.Save();
    }
    public static void RecordUpgrade(string id, int level)
    {
        string milestone = id == "mb_speed" && level >= 5 ? "turbo_socks" :
            id == "mb_cooldown" && level >= 5 ? "battery" :
            id == "mb_xp" && level >= 8 ? "echo_shard" :
            id == "mb_regen" && level >= 8 ? "leeching_crystal" :
            id == "mb_attraction" && level >= 8 ? "shattered_knowledge" :
            id == "mb_cursed" && level >= 10 ? "cursed_grabbies" : null;
        if (milestone != null && !HasMilestone(milestone))
            PlayerPrefs.SetInt("Progress.Milestone." + milestone, 1);
        var build = MegabonkBuild.Instance;
        if (build != null && build.Level("mb_cooldown") >= 5 && build.Level("mb_speed") >= 5)
            PlayerPrefs.SetInt("Progress.Milestone.turbo_skates", 1);
        if (milestone != null || id == "mb_cooldown" || id == "mb_speed") PlayerPrefs.Save();
    }
    public static void RecordPlayerLevel(int level)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (level >= 30 && (gm.selectedCharacter == GameManager.CharacterType.Calcium || gm.selectedOriginalCharacter == "Calcium") && !HasMilestone("demonic_soul"))
        {
            PlayerPrefs.SetInt("Progress.Milestone.demonic_soul", 1);
            PlayerPrefs.Save();
        }
        if (level >= 50 && gm.selectedOriginalCharacter == "Vlad" && !HasMilestone("bloody_cleaver"))
        {
            PlayerPrefs.SetInt("Progress.Milestone.bloody_cleaver", 1);
            PlayerPrefs.Save();
        }
    }
    public static void RecordRelicAndDifficulty(string relicId, int copies, float difficulty)
    {
        bool changed = false;
        if (relicId == "leeching_crystal" && copies >= 3 && !HasMilestone("kevin"))
        {
            PlayerPrefs.SetInt("Progress.Milestone.kevin", 1);
            changed = true;
        }
        if (difficulty >= 40f && !HasMilestone("skuleg"))
        {
            PlayerPrefs.SetInt("Progress.Milestone.skuleg", 1);
            changed = true;
        }
        if (changed) PlayerPrefs.Save();
    }
    public static void RecordRun(int kills, float swarm)
    {
        PlayerPrefs.SetInt("Progress.Kills", Kills + kills);
        PlayerPrefs.SetFloat("Progress.Swarm", Mathf.Max(Swarm, swarm));
        var gm = GameManager.Instance;
        if (gm != null && (gm.selectedOriginalCharacter == "Birdo" || gm.selectedOriginalCharacter == "Amog"))
        {
            string id = gm.selectedOriginalCharacter;
            int total = PlayerPrefs.GetInt("Progress.CharacterKills." + id, 0) + kills;
            PlayerPrefs.SetInt("Progress.CharacterKills." + id, total);
        }
        PlayerPrefs.Save();
    }
    public static bool IsUnlocked(MegabonkRelicManager.RelicData item)
    {
        if (item.id == "ghost") return Swarm >= 60;
        if (item.id == "soul_harvester") return Swarm >= 360;
        if (item.id == "bob_dead") return Swarm >= 120;
        if (item.id == "key") return Chests >= 25;
        if (item.id == "golden_sneakers") return Pots >= 150;
        if (item.id == "energy_core") return FinalBosses >= 2;
        if (item.id == "forbidden_juice") return CriticalHits >= 100;
        if (item.id == "cursed_doll") return Deaths >= 10;
        if (item.id == "moldy_gloves") return CheeseDuplications >= 3;
        if (item.id == "eagle_claw") return PlayerPrefs.GetInt("Progress.CharacterKills.Birdo", 0) >= 10000;
        if (item.id == "gas_mask") return PlayerPrefs.GetInt("Progress.CharacterKills.Amog", 0) >= 5000;
        if (item.id == "ice_crystal") return WeaponKills("mb_frost") >= 50;
        if (item.id == "brass_knuckles") return WeaponKills("mb_sword") >= 5000;
        if (item.id == "slutty_cannon") return WeaponKills("mb_rocket") >= 5000;
        if (item.id == "grandmas_secret_tonic") return WeaponKills("mb_sniper") >= 3000;
        if (item.id == "lightning_orb") return WeaponKills("mb_lightning") >= 3000;
        if (item.id == "dragonfire") return WeaponKills("mb_fire") >= 4000;
        if (item.id == "joes_dagger") return WeaponKills("mb_dexecutioner") >= 10000;
        if (item.id == "cactus") return ThornKills >= 100;
        if (item.id == "toxic_barrel") return PoisonKills >= 2000;
        if (item.id == "idle_juice") return IdleKills >= 500;
        if (item.id == "demonic_blade") return LifeStealHealed >= 350;
        if (item.id == "anvil") return ChallengeVictories >= 3;
        if (item.id == "demonic_blood") return BloodMagicProcs >= 400;
        if (item.id == "kevin" || item.id == "skuleg" || item.id == "demonic_soul" || item.id == "bloody_cleaver" ||
            item.id == "speed_boi" || item.id == "holy_book" || item.id == "sucky_magnet" ||
            item.id == "tactical_goggles" || item.id == "boss_buster" ||
            item.id == "quins_mask" || item.id == "gamer_goggles" ||
            item.id == "turbo_socks" || item.id == "battery" || item.id == "echo_shard" ||
            item.id == "leeching_crystal" || item.id == "turbo_skates" ||
            item.id == "shattered_knowledge" || item.id == "cursed_grabbies" || item.id == "chonkplate")
            return HasMilestone(item.id);
        if (item.unlockCondition == "Desbloqueado por defecto") return true;
        return false;
    }
    public static string Requirement(MegabonkRelicManager.RelicData item)
    {
        if (item.id == "ghost") return $"Enjambre final: {Swarm:0}/60 s";
        if (item.id == "soul_harvester") return $"Enjambre final: {Swarm:0}/360 s";
        if (item.id == "bob_dead") return $"Enjambre final: {Swarm:0}/120 s";
        if (item.id == "key") return $"Cofres abiertos: {Chests}/25";
        if (item.id == "golden_sneakers") return $"Vasijas rotas: {Pots}/150";
        if (item.id == "energy_core") return $"Rey Fantasma derrotado: {FinalBosses}/2";
        if (item.id == "forbidden_juice") return $"Golpes críticos: {CriticalHits}/100";
        if (item.id == "cursed_doll") return $"Muertes: {Deaths}/10";
        if (item.id == "moldy_gloves") return $"Moldy Cheese duplicados: {CheeseDuplications}/3";
        if (item.id == "eagle_claw") return $"Bajas con Birdo: {PlayerPrefs.GetInt("Progress.CharacterKills.Birdo", 0)}/10000";
        if (item.id == "gas_mask") return $"Bajas con Amog: {PlayerPrefs.GetInt("Progress.CharacterKills.Amog", 0)}/5000";
        if (item.id == "ice_crystal") return $"Bajas con Frostwalker: {WeaponKills("mb_frost")}/50";
        if (item.id == "brass_knuckles") return $"Bajas con espada: {WeaponKills("mb_sword")}/5000";
        if (item.id == "slutty_cannon") return $"Bajas con misil: {WeaponKills("mb_rocket")}/5000";
        if (item.id == "grandmas_secret_tonic") return $"Bajas con francotirador: {WeaponKills("mb_sniper")}/3000";
        if (item.id == "lightning_orb") return $"Bajas con rayos: {WeaponKills("mb_lightning")}/3000";
        if (item.id == "dragonfire") return $"Bajas con fuego: {WeaponKills("mb_fire")}/4000";
        if (item.id == "joes_dagger") return $"Bajas con Dexecutioner: {WeaponKills("mb_dexecutioner")}/10000";
        if (item.id == "cactus") return $"Bajas por espinas: {ThornKills}/100";
        if (item.id == "toxic_barrel") return $"Bajas por veneno: {PoisonKills}/2000";
        if (item.id == "idle_juice") return $"Bajas estando quieto: {IdleKills}/500";
        if (item.id == "demonic_blade") return $"Vida recuperada por robo: {LifeStealHealed}/350";
        if (item.id == "anvil") return $"Expediciones con desafío completadas: {ChallengeVictories}/3";
        if (item.id == "demonic_blood") return $"Activaciones de Magia de Sangre: {BloodMagicProcs}/400";
        if (item.id == "kevin" || item.id == "skuleg" || item.id == "demonic_soul" || item.id == "bloody_cleaver" ||
            item.id == "speed_boi" || item.id == "holy_book" || item.id == "sucky_magnet" ||
            item.id == "tactical_goggles" || item.id == "boss_buster" ||
            item.id == "quins_mask" || item.id == "gamer_goggles" ||
            item.id == "turbo_socks" || item.id == "battery" || item.id == "echo_shard" ||
            item.id == "leeching_crystal" || item.id == "turbo_skates" ||
            item.id == "shattered_knowledge" || item.id == "cursed_grabbies" || item.id == "chonkplate")
            return item.unlockCondition + (HasMilestone(item.id) ? " (cumplido)" : " (pendiente)");
        if (item.unlockCondition == "Desbloqueado por defecto") return "Disponible desde el inicio";
        return item.unlockCondition + " (seguimiento pendiente)";
    }
}
