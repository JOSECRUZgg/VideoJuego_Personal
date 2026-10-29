using UnityEngine;

public partial class GameHUD
{
    private Vector2 expeditionItemsScroll;
    private Vector2 expeditionWeaponsScroll;

    void DrawExpeditionHUD(GameManager gm)
    {
        float w = MegabonkUI.Width, h = MegabonkUI.Height;
        XPSystem xp = XPSystem.Instance;
        MegabonkUI.MetalPanel(new Rect(20, 24, 270, 140), "ESTADO");
        MegabonkUI.Text(new Rect(32, 62, 245, 24), $"ORO {gm.gold}    BAJAS {gm.kills}", 16, MegabonkUI.Gold, true);
        if (playerHealth != null)
        {
            DrawExpeditionBar(new Rect(32, 94, 245, 24), (float)playerHealth.currentHealth / Mathf.Max(1, playerHealth.maxHealth),
                new Color(0.8f, 0.12f, 0.16f), $"VIDA {playerHealth.currentHealth}/{playerHealth.maxHealth}");
            DrawExpeditionBar(new Rect(32, 124, 245, 24), playerHealth.ShieldPercent / 100f,
                new Color(0.08f, 0.55f, 0.9f), $"ESCUDO {playerHealth.ShieldPercent:0}%");
        }

        StageDirector stage = StageDirector.Instance;
        float duration = stage != null ? SourceStageTimeline.StageSeconds(stage.CurrentStage) : 600f;
        float remaining = Mathf.Max(0f, duration - (stage != null ? stage.StageElapsed : gm.gameTime));
        string clockText = stage != null && stage.FinalEncounterActive ? "JEFE FINAL"
            : $"{Mathf.FloorToInt(remaining / 60):00}:{Mathf.FloorToInt(remaining % 60):00}";
        MegabonkUI.Text(new Rect(w / 2 - 160, 25, 320, 48), clockText, 34, MegabonkUI.White, true, TextAnchor.MiddleCenter);
        string status = gm.finalSwarmActive ? $"ENJAMBRE FINAL +{gm.finalSwarmTime:0}s"
            : stage != null && stage.FinalEncounterActive ? "REY FANTASMA"
            : stage != null ? $"ETAPA {stage.CurrentStage}/3" + (stage.PortalActive ? " · PORTAL ABIERTO" : "") : "EXPLORA EL MAPA";
        MegabonkUI.Text(new Rect(w / 2 - 210, 78, 420, 30), status, 17, MegabonkUI.Gold, true, TextAnchor.MiddleCenter);

        float panelHeight = Mathf.Min(340f, h - 310f);
        MegabonkUI.MetalPanel(new Rect(20, 178, 270, panelHeight), "ARMAS Y TOMOS");
        int rows = (xp != null ? xp.acquiredUpgradeIds.Count : 0);
        expeditionWeaponsScroll = GUI.BeginScrollView(new Rect(28, 218, 254, panelHeight - 48), expeditionWeaponsScroll,
            new Rect(0, 0, 230, rows * 52));
        int row = 0;
        if (xp != null)
        {
            foreach (var def in UpgradeCatalog.AllUpgrades)
            {
                if (!xp.HasUpgrade(def.id)) continue;
                float y = row++ * 52;
                MegabonkIconProvider.DrawUpgradeIcon(new Rect(4, y + 2, 44, 44), def);
                MegabonkUI.Text(new Rect(54, y, 173, 26), MegabonkUI.Clean(def.name), 13, MegabonkUI.White, true);
                int shownLevel = xp.GetTomeLevel(def.id);
                MegabonkUI.Text(new Rect(54, y + 25, 173, 22), "NV " + shownLevel + "  " + xp.GetAbilityTimerText(def.id), 11, def.color);
            }
        }
        GUI.EndScrollView();

        if (xp != null)
        {
            DrawExpeditionBar(new Rect(20, h - 134, w - 40, 19), (float)xp.currentXP / Mathf.Max(1, xp.maxXP),
                new Color(0.26f, 0.65f, 0.12f), $"NIVEL {xp.level} · {xp.currentXP}/{xp.maxXP} XP");
        }
        MegabonkUI.MetalPanel(new Rect(20, h - 108, w - 40, 96));
        MegabonkUI.Text(new Rect(32, h - 103, 130, 20), "OBJETOS", 13, MegabonkUI.Gold, true);
        var relics = MegabonkRelicManager.Instance;
        int itemCount = relics != null ? relics.acquiredRelics.Count : 0;
        expeditionItemsScroll = GUI.BeginScrollView(new Rect(30, h - 80, w - 60, 65), expeditionItemsScroll,
            new Rect(0, 0, Mathf.Max(w - 80, itemCount * 66), 48));
        int index = 0;
        if (relics != null)
            foreach (var item in relics.acquiredRelics)
            {
                if (item.Value <= 0) continue;
                Rect slot = new Rect(index++ * 66, 0, 58, 48);
                var data = MegabonkRelicManager.GetRelicData(item.Key);
                MegabonkUI.Fill(slot, data != null ? data.bannerColor * 0.45f : Color.gray);
                MegabonkIconProvider.DrawItemIcon(new Rect(slot.x + 4, 0, 44, 44), item.Key);
                MegabonkUI.Text(new Rect(slot.x + 24, 28, 32, 20), "x" + item.Value, 13, MegabonkUI.White, true, TextAnchor.MiddleRight);
                GUI.Label(slot, new GUIContent("", data != null ? data.name : item.Key));
            }
        if (index == 0) MegabonkUI.Text(new Rect(4, 6, w - 90, 25), "Abre cofres para conseguir objetos", 14, MegabonkUI.Muted);
        GUI.EndScrollView();
        if (!string.IsNullOrEmpty(GUI.tooltip))
            MegabonkUI.Text(new Rect(310, h - 170, w - 620, 28), GUI.tooltip, 18, MegabonkUI.Gold, true, TextAnchor.MiddleCenter);
        DrawBossBar();
        DrawMinimap();
    }

    void DrawExpeditionBar(Rect rect, float fraction, Color color, string text)
    {
        MegabonkUI.Fill(rect, new Color(0.035f, 0.05f, 0.05f, 0.95f));
        MegabonkUI.Fill(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), color);
        MegabonkUI.Text(rect, text, 13, MegabonkUI.White, true, TextAnchor.MiddleCenter);
    }
}
