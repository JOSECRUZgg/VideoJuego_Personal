using UnityEngine;

public class ShrineRewardUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    bool legendary;
    bool ownsPause;

    public void Open(bool golden)
    {
        if (XPSystem.Instance == null || IsOpen) return;
        legendary = golden;
        ownsPause = true;
        IsOpen = true;
        XPSystem.Instance.OpenSkillMenu();
        Cursor.visible = true;
    }

    public void Close()
    {
        if (!ownsPause) return;
        ownsPause = false;
        IsOpen = false;
        if (XPSystem.Instance != null) XPSystem.Instance.CloseSkillMenu();
    }

    void OnDestroy() { Close(); }

    void OnGUI()
    {
        if (!ownsPause || !IsOpen) return;
        Matrix4x4 previous = GUI.matrix;
        Color oldColor = GUI.color;
        try
        {
            GUI.matrix = Matrix4x4.Scale(Vector3.one * MegabonkUI.Scale);
            MegabonkUI.Background(true);
            MegabonkUI.Text(new Rect(60, 110, MegabonkUI.Width - 120, 60), legendary ? "BENDICIÓN LEGENDARIA" : "SANTUARIO COMPLETADO", 32, MegabonkUI.Gold, true, TextAnchor.MiddleCenter);
            MegabonkUI.Text(new Rect(60, 180, MegabonkUI.Width - 120, 40), "Elige una de las cuatro mejoras para esta expedición", 18, MegabonkUI.White, false, TextAnchor.MiddleCenter);
            float rewardMultiplier = 1f + .075f * MegabonkBuild.Relics("wrench");
            float amount = (legendary ? 0.30f : 0.12f) * rewardMultiplier;
            float attraction = (legendary ? .5f : .2f) * rewardMultiplier;
            string[] titles = { "DAÑO", "VELOCIDAD DE ATAQUE", "MOVIMIENTO", "ATRACCIÓN" };
            string[] details = { $"+{amount:P0} de daño", $"+{amount:P0} de cadencia", $"+{amount:P0} de velocidad", $"+{attraction:P0} de alcance de recogida" };
            float width = (MegabonkUI.Width - 160f) / 4f;
            for (int i = 0; i < 4; i++)
            {
                Rect card = new Rect(50 + i * (width + 20), 290, width, 250);
                MegabonkUI.MetalPanel(card, titles[i]);
                MegabonkUI.Text(new Rect(card.x + 20, card.y + 100, width - 40, 65), details[i], 23, MegabonkUI.White, true, TextAnchor.MiddleCenter);
                bool key = Event.current.type == EventType.KeyDown && (Event.current.keyCode == (KeyCode)((int)KeyCode.Alpha1 + i));
                if (MegabonkUI.PixelButton(new Rect(card.x + 20, card.yMax - 65, width - 40, 45), "ELEGIR " + (i + 1), true) || key)
                {
                    XPSystem xp = XPSystem.Instance;
                    if (xp == null) return;
                    if (i == 0) xp.damageMultiplier += amount;
                    else if (i == 1) xp.fireRateMultiplier += amount;
                    else if (i == 2) xp.speedMultiplier += amount;
                    else xp.magnetMultiplier += attraction;
                    if (key) Event.current.Use();
                    Close();
                    GUIUtility.ExitGUI();
                }
            }
        }
        finally { GUI.matrix = previous; GUI.color = oldColor; }
    }
}
