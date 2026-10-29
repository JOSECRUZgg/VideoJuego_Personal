using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Menú interactivo de Subida de Nivel (3 cartas de mejoras estándar y sinergias)
/// y Menú de Tomos de Jefe (3 tomos de alto impacto con penalización).
/// Compatible con teclado ([ 1 ], [ 2 ], [ 3 ], [ 4 ]) y clic del ratón.
/// </summary>
public class SkillMenuUI : MonoBehaviour
{
    public static SkillMenuUI Instance { get; private set; }

    private List<UpgradeDefinition> offeredUpgrades = new List<UpgradeDefinition>();
    private bool isTomeMode = false;
    private int hoveredIndex = -1;

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle categoryStyle;
    private GUIStyle cardTitleStyle;
    private GUIStyle cardDescStyle;
    private GUIStyle iconStyle;
    private bool stylesReady;

    private Texture2D whiteTex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        whiteTex = Texture2D.whiteTexture;
    }

    public void ResetMenu()
    {
        offeredUpgrades.Clear();
        isTomeMode = false;
        hoveredIndex = -1;
    }

    void Update()
    {
        if (ShrineRewardUI.IsOpen || MicrowaveUI.IsOpen) return;
        XPSystem xp = XPSystem.Instance;
        if (xp == null) return;

        if (xp.bossTomeMenuOpen && !isTomeMode)
        {
            isTomeMode = true;
            offeredUpgrades = xp.GetBossTomeOptions(3);
        }

        // Si se abre el menú de nivel y no hay opciones generadas
        if (xp.skillMenuOpen && (offeredUpgrades.Count == 0 || isTomeMode))
        {
            isTomeMode = false;
            offeredUpgrades = xp.GetLevelUpOptions(3);
        }
    }

    public void OpenBossTomeMenu()
    {
        XPSystem xp = XPSystem.Instance;
        if (xp != null)
        {
            isTomeMode = true;
            offeredUpgrades = xp.GetBossTomeOptions(3);
            xp.OpenBossTomeMenu();
        }
    }

    void InitStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        titleStyle.normal.textColor = Color.white;

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter,
        };
        subtitleStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

        categoryStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };

        cardTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
        };
        cardTitleStyle.normal.textColor = Color.white;

        cardDescStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
        };
        cardDescStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f);

        iconStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 40,
            alignment = TextAnchor.MiddleCenter,
        };
        iconStyle.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        try
        {
            GUI.matrix = Matrix4x4.Scale(Vector3.one * MegabonkUI.Scale);
            DrawSelection();
        }
        finally { GUI.matrix = previousMatrix; GUI.color = previousColor; }
    }

    void DrawSelection()
    {
        if (ShrineRewardUI.IsOpen || MicrowaveUI.IsOpen) return;
        XPSystem xp = XPSystem.Instance;
        if (xp == null) return;

        bool isLevelUpOpen = xp.skillMenuOpen && !isTomeMode;
        bool isTomeOpen = xp.bossTomeMenuOpen || (xp.skillMenuOpen && isTomeMode);

        if (!isLevelUpOpen && !isTomeOpen) return;

        if (offeredUpgrades == null || offeredUpgrades.Count == 0)
        {
            if (isTomeMode)
                offeredUpgrades = xp.GetBossTomeOptions(3);
            else
                offeredUpgrades = xp.GetLevelUpOptions(3);

            if (offeredUpgrades.Count == 0)
            {
                // Si no hay mejoras en el pool, cerrar
                xp.CloseSkillMenu();
                xp.CloseBossTomeMenu();
                return;
            }
        }

        InitStyles();

        float sw = MegabonkUI.Width;
        float sh = MegabonkUI.Height;

        MegabonkUI.Background(true);
        MegabonkUI.Text(new Rect(60, 65, sw-120, 28), isTomeMode ? "RECOMPENSA DE JEFE" : "EVOLUCIÓN DEL SUPERVIVIENTE", 14, MegabonkUI.Cyan, true, TextAnchor.MiddleCenter);
        MegabonkUI.Text(new Rect(60, 115, sw-120, 65), "Elige tu próxima mejora", 42, MegabonkUI.White, true, TextAnchor.MiddleCenter);
        MegabonkUI.Text(new Rect(60, 195, sw-120, 40), $"NIVEL {xp.level}  /  Las armas atacan automáticamente; los tomos mejoran tu build.", 18, MegabonkUI.Muted, false, TextAnchor.MiddleCenter);

        // Distribución de cartas
        int cardCount = offeredUpgrades.Count;
        float cardW = isTomeMode ? Mathf.Clamp(sw * 0.28f, 260f, 320f) : Mathf.Clamp(sw * 0.22f, 220f, 280f);
        float cardH = 420f;
        float spacing = isTomeMode ? 28f : 18f;
        float totalW = cardW * cardCount + spacing * (cardCount - 1);
        float startX = (sw - totalW) / 2f;
        float cardY = (sh - cardH) / 2f + 60f;

        hoveredIndex = -1;
        Vector2 mouse = Event.current.mousePosition;
        Event e = Event.current;
        bool isClick = (e.type == EventType.MouseDown && e.button == 0);

        for (int i = 0; i < cardCount; i++)
        {
            UpgradeDefinition sk = offeredUpgrades[i];
            float cx = startX + i * (cardW + spacing);
            Rect cardRect = new Rect(cx, cardY, cardW, cardH);

            bool hovered = cardRect.Contains(mouse);
            if (hovered) hoveredIndex = i;

            DrawCard(cardRect, sk, hovered, i + 1);

            bool keyPressed = (e.type == EventType.KeyDown &&
                (e.keyCode == KeyCode.Alpha1 + i || e.keyCode == KeyCode.Keypad1 + i));

            // Selección inmediata al hacer clic o presionar el número
            if ((isClick && hovered) || keyPressed)
            {
                e.Use();
                string chosenId = sk.id;
                offeredUpgrades.Clear();
                isTomeMode = false;
                xp.ApplyUpgrade(chosenId);
                GUIUtility.ExitGUI();
                return;
            }
        }

        // Instrucción inferior
        subtitleStyle.fontSize = 14;
        GUI.color = new Color(0.8f, 0.85f, 0.9f);
        string keyGuide = "Haz clic sobre la carta o presiona [ 1 ], [ 2 ] o [ 3 ] en tu teclado";
        GUI.Label(new Rect(0, cardY + cardH + 22f, sw, 25f), keyGuide, subtitleStyle);
        subtitleStyle.fontSize = 15;
        GUI.color = Color.white;
    }

    void DrawCard(Rect r, UpgradeDefinition sk, bool hovered, int slotNum)
    {
        MegabonkUI.Panel(r, sk.color);
        if (hovered) MegabonkUI.Fill(new Rect(r.x, r.y, r.width, 4), sk.color);
        MegabonkUI.Text(new Rect(r.x+24,r.y+24,r.width-48,32), MegabonkUI.Clean(sk.category).ToUpper(), 12, sk.color, true);
        MegabonkIconProvider.DrawUpgradeIcon(new Rect(r.x+24, r.y+68, 64, 64), sk);
        MegabonkUI.Text(new Rect(r.x+24,r.y+166,r.width-48,64), MegabonkUI.Clean(sk.name), 23, MegabonkUI.White, true);
        MegabonkUI.Text(new Rect(r.x+24,r.y+244,r.width-48,115), MegabonkUI.Clean(sk.description), 16, MegabonkUI.Muted);
        MegabonkUI.Fill(new Rect(r.x+24,r.yMax-49,r.width-48,1), sk.color*.6f);
        MegabonkUI.Text(new Rect(r.x+24,r.yMax-38,r.width-48,28), "SELECCIONAR  /  "+slotNum, 13, hovered ? MegabonkUI.White : sk.color, true);
    }
}
