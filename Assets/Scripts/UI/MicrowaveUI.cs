using UnityEngine;

public class MicrowaveUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    StageInteractable source;
    string sacrifice, duplicate;
    Vector2 scroll;
    public static void Open(StageInteractable source)
    {
        if (IsOpen || XPSystem.Instance == null || XPSystem.Instance.skillMenuOpen || MegabonkRelicManager.Instance == null) return;
        IsOpen = true;
        var ui = source.gameObject.AddComponent<MicrowaveUI>();
        ui.source = source;
        XPSystem.Instance.OpenSkillMenu();
    }
    void OnGUI()
    {
        var relics = MegabonkRelicManager.Instance;
        if (relics == null) return;
        Matrix4x4 previous = GUI.matrix;
        bool previousEnabled = GUI.enabled;
        try
        {
        GUI.matrix = Matrix4x4.Scale(Vector3.one * MegabonkUI.Scale);
        MegabonkUI.Background(true);
        Rect panel = new Rect(70, 80, MegabonkUI.Width - 140, MegabonkUI.Height - 160);
        MegabonkUI.MetalPanel(panel, "MICROONDAS");
        MegabonkUI.Text(new Rect(95, 140, panel.width - 50, 50), "Selecciona qué sacrificar y qué duplicar. Deben ser objetos distintos de la misma rareza.", 18, Color.white);
        scroll = GUI.BeginScrollView(new Rect(95, 205, panel.width - 50, panel.height - 220), scroll,
            new Rect(0, 0, panel.width - 75, relics.acquiredRelics.Count * 58));
        int row = 0;
        foreach (var item in relics.acquiredRelics)
        {
            var data = MegabonkRelicManager.GetRelicData(item.Key);
            if (data == null || item.Value <= 0) continue;
            float y = row++ * 58;
            MegabonkIconProvider.DrawItemIcon(new Rect(0, y, 45, 45), item.Key);
            MegabonkUI.Text(new Rect(58, y, 320, 45), data.name + " x" + item.Value, 16, data.bannerColor);
            if (MegabonkUI.PixelButton(new Rect(390, y, 170, 42), sacrifice == item.Key ? "SACRIFICAR ✓" : "SACRIFICAR", sacrifice == item.Key)) sacrifice = item.Key;
            if (MegabonkUI.PixelButton(new Rect(580, y, 170, 42), duplicate == item.Key ? "DUPLICAR ✓" : "DUPLICAR", duplicate == item.Key)) duplicate = item.Key;
        }
        GUI.EndScrollView();
        if (MegabonkUI.PixelButton(new Rect(110, panel.yMax - 70, 200, 45), "CANCELAR", false)) { Close(); GUIUtility.ExitGUI(); }
        bool valid = sacrifice != null && duplicate != null && sacrifice != duplicate && MegabonkRelicManager.GetRelicData(sacrifice).rarity == MegabonkRelicManager.GetRelicData(duplicate).rarity;
        GUI.enabled = valid;
        if (MegabonkUI.PixelButton(new Rect(panel.xMax - 290, panel.yMax - 70, 250, 45), "CONFIRMAR", true) && relics.Transmute(sacrifice, duplicate))
        { source.Complete(); Close(); GUIUtility.ExitGUI(); }
        GUI.enabled = true;
        }
        finally { GUI.matrix = previous; GUI.enabled = previousEnabled; }
    }
    void Close() { IsOpen = false; XPSystem.Instance?.CloseSkillMenu(); Destroy(this); }
    void OnDestroy() { if (IsOpen) { IsOpen = false; XPSystem.Instance?.CloseSkillMenu(); } }
}
