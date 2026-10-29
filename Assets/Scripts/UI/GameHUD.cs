using UnityEngine;

/// <summary>
/// HUD completo con IMGUI: barra de vida, barra de XP, barra de jefe, munición infinita,
/// oleadas, puntuación, crosshair, minimapa, kill feed, píldoras de habilidades activas,
/// pantalla de pausa, pantalla de game over y menú principal interactivo.
/// </summary>
public partial class GameHUD : MonoBehaviour
{
    // Referencias
    private PlayerHealth3D playerHealth;
    private PlayerShooting3D playerShooting;
    private PlayerMovement3D playerMovement;
    private EnemySpawner3D spawner;
    private Camera mainCam;

    // Texturas
    private Texture2D whiteTex;
    private Texture2D minimapBgTex;

    // Sub-pantallas del Menú Principal estilo Megabonk
    public enum MenuSubScreen { Main, StartRun, Characters, MapSelect, Unlocks, Settings, Leaderboards, Shop, Toggler, Quests, Credits, LanLobby }
    private MenuSubScreen currentMenuScreen = MenuSubScreen.Main;

    // Configuración interactiva (Settings)
    private float settingFOV = 75f;
    private float settingSensitivity = 2.0f;
    private bool settingAutoAim = false;
    private bool settingInvertY = false;
    private float settingVolume = 1.0f;
    private int settingQualityLevel = 1;
    private Vector2 synergiesScrollPos = Vector2.zero;
    private Vector2 skillsOverviewScrollPos = Vector2.zero;

    // Estructura de personajes para selección
    private struct CharacterInfo
    {
        public GameManager.CharacterType type;
        public string name;
        public string title;
        public string icon;
        public string passive;
        public Color color;
    }

    private static readonly CharacterInfo[] Characters = new CharacterInfo[]
    {
        new CharacterInfo {
            type = GameManager.CharacterType.CL4NK,
            name = "CL4NK",
            title = "Robot pistolero · 100 HP",
            icon = "🏹",
            passive = "Revólver inicial. +10% de probabilidad de esquivar.",
            color = new Color(0.2f, 0.9f, 1f)
        },
        new CharacterInfo {
            type = GameManager.CharacterType.Megachad,
            name = "MEGACHAD",
            title = "Tanque · 150 HP",
            icon = "🛡",
            passive = "Aura inicial. 150 puntos de vida.",
            color = new Color(1f, 0.65f, 0.2f)
        },
        new CharacterInfo {
            type = GameManager.CharacterType.SirOofie,
            name = "SIR OOFIE",
            title = "Caballero · 100 HP",
            icon = "🔥",
            passive = "Espada inicial. +15% de daño.",
            color = new Color(1f, 0.25f, 0.25f)
        },
        new CharacterInfo {
            type = GameManager.CharacterType.Calcium,
            name = "CALCIUM",
            title = "Esqueleto veloz · 100 HP",
            icon = "⚡",
            passive = "Hueso inicial. El daño aumenta con la velocidad; recibir daño reduce temporalmente el bono.",
            color = new Color(1f, 0.95f, 0.2f)
        },
        new CharacterInfo {
            type = GameManager.CharacterType.Original, name = "PERSONAJE ORIGINAL", title = "Modelo original",
            icon = "", passive = "Armas automáticas, tomos y objetos de la partida.", color = Color.cyan
        }
    };

    // Estilos
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle labelStyle;
    private GUIStyle smallStyle;
    private bool stylesInitialized;

    void Start()
    {
        FindReferences();
        CreateTextures();
        InitSettings();
    }

    void InitSettings()
    {
        if (Camera.main != null) settingFOV = Camera.main.fieldOfView;
        MouseLook ml = Object.FindAnyObjectByType<MouseLook>();
        if (ml != null)
        {
            settingSensitivity = ml.sensitivity;
            settingInvertY = ml.invertY;
        }
        PlayerShooting3D ps = Object.FindAnyObjectByType<PlayerShooting3D>();
        if (ps != null) settingAutoAim = ps.autoAim;
        settingVolume = AudioListener.volume;
        settingQualityLevel = QualitySettings.GetQualityLevel();
        RestorePreferences();
    }

    void ApplySettings()
    {
        if (Camera.main != null) Camera.main.fieldOfView = settingFOV;
        MouseLook ml = Object.FindAnyObjectByType<MouseLook>();
        if (ml != null)
        {
            ml.sensitivity = settingSensitivity;
            ml.normalFOV = settingFOV;
            ml.invertY = settingInvertY;
        }
        PlayerShooting3D ps = Object.FindAnyObjectByType<PlayerShooting3D>();
        if (ps != null) ps.autoAim = settingAutoAim;
        AudioListener.volume = settingVolume;
        QualitySettings.SetQualityLevel(settingQualityLevel);
        SavePreferences();
    }

    void FindReferences()
    {
        playerHealth = Object.FindAnyObjectByType<PlayerHealth3D>();
        playerShooting = Object.FindAnyObjectByType<PlayerShooting3D>();
        playerMovement = Object.FindAnyObjectByType<PlayerMovement3D>();
        spawner = Object.FindAnyObjectByType<EnemySpawner3D>();
        mainCam = Camera.main;
    }

    void CreateTextures()
    {
        whiteTex = Texture2D.whiteTexture;

        minimapBgTex = new Texture2D(1, 1);
        minimapBgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
        minimapBgTex.Apply();
    }

    void InitStyles()
    {
        if (stylesInitialized) return;
        stylesInitialized = true;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        titleStyle.normal.textColor = Color.white;

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
        };
        subtitleStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
        };
        labelStyle.normal.textColor = Color.white;

        smallStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
        };
        smallStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
    }

    void OnGUI()
    {
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        try
        {
            GUI.matrix = Matrix4x4.Scale(Vector3.one * MegabonkUI.Scale);
            RenderHUD();
        }
        finally { GUI.matrix = previousMatrix; GUI.color = previousColor; }
    }

    void RenderHUD()
    {
        if(UiTransition.Busy)return;
        InitStyles();

        if (playerHealth == null || playerShooting == null || spawner == null)
        {
            FindReferences();
            if (playerHealth == null) return;
        }

        GameManager gm = GameManager.Instance;

        // ── Menú Principal ──
        if (gm != null && gm.gameState == GameManager.GameState.MainMenu)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape && currentMenuScreen != MenuSubScreen.Main)
            {
                if (currentMenuScreen == MenuSubScreen.LanLobby) LanLobby.Instance?.Stop();
                currentMenuScreen = MenuSubScreen.Main;
                Event.current.Use();
            }
            DrawMainMenu(gm);
            return;
        }

        // ── Pantalla de Game Over ──
        if (gm != null && gm.gameState == GameManager.GameState.GameOver)
        {
            DrawModernEnd(gm, false);
            return;
        }

        // ── Pantalla de Victoria (20 minutos) ──
        if (gm != null && gm.gameState == GameManager.GameState.Victory)
        {
            DrawModernEnd(gm, true);
            return;
        }

        // ── Pantalla de Pausa / Libro de Habilidades ──
        if (gm != null && (gm.gameState == GameManager.GameState.Paused || CoopRun.PersonalMenuOpen))
        {
            if(pauseSettings) DrawModernSettings(gm); else DrawModernPause(gm);
            return;
        }

        if (gm != null) DrawExpeditionHUD(gm);
    }

    // ═══════════════════════════════════════════════════════
    // BARRA DE JEFE (BOSS) - Arriba centro (cuando hay un Boss activo)
    // ═══════════════════════════════════════════════════════
    void DrawBossBar()
    {
        if (spawner == null || spawner.currentBoss == null || spawner.currentBoss.IsDying) return;

        EnemyHealth3D boss = spawner.currentBoss;
        float sw = MegabonkUI.Width;
        float barW = Mathf.Clamp(sw * 0.52f, 400f, 650f);
        float barH = 24f;
        float x = (sw - barW) / 2f;
        float y = 134f;

        // Fondo oscuro
        GUI.color = new Color(0.08f, 0.03f, 0.04f, 0.95f);
        GUI.DrawTexture(new Rect(x, y, barW, barH), whiteTex);

        // Barra de vida carmesí/fuego con detección de Furia
        float hpPercent = Mathf.Clamp01((float)boss.currentHealth / boss.maxHealth);
        bool isEnraged = boss.currentHealth < boss.maxHealth * 0.50f;
        Color bossBarColor = isEnraged ? new Color(1f, 0.08f, 0.05f, 0.98f) : new Color(1f, 0.15f, 0.22f, 0.98f);
        GUI.color = bossBarColor;
        GUI.DrawTexture(new Rect(x, y, barW * hpPercent, barH), whiteTex);

        // Borde dorado / ardiente
        GUI.color = isEnraged ? new Color(1f, 0.35f, 0.05f, 0.95f) : new Color(1f, 0.8f, 0.2f, 0.9f);
        GUI.DrawTexture(new Rect(x, y, barW, 2.5f), whiteTex);
        GUI.DrawTexture(new Rect(x, y + barH - 2.5f, barW, 2.5f), whiteTex);
        GUI.DrawTexture(new Rect(x, y, 2.5f, barH), whiteTex);
        GUI.DrawTexture(new Rect(x + barW - 2.5f, y, 2.5f, barH), whiteTex);

        // Texto centrado
        GUIStyle bossStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        bossStyle.normal.textColor = Color.white;
        GUI.color = Color.white;
        var bossData=boss.GetComponent<EnemyAI3D>()?.extractedData;
        string bossName=bossData!=null ? bossData.id == "GhostKingData" ? "REY FANTASMA" : bossData.id.StartsWith("Pharaoh") ? "FARAÓN" : bossData.id.StartsWith("EntStage") ? "ENT DEL BOSQUE" : bossData.id : "JEFE";
        string stageLabel=StageDirector.Instance!=null ? StageDirector.Instance.FinalEncounterActive ? "JEFE FINAL" : "ETAPA "+StageDirector.Instance.CurrentStage : "OLEADA "+spawner.currentWave;
        bool darknessWarning=boss.GetComponent<ExtractedBossCombat>()?.DarknessWarning == true;
        string bossText=darknessWarning ? "☠ OSCURIDAD: REFÚGIATE JUNTO A UN SANTUARIO ☠"
            : $"☠ {bossName} · {stageLabel} [{boss.currentHealth} / {boss.maxHealth} HP] ☠";
        GUI.Label(new Rect(x, y, barW, barH), bossText, bossStyle);
    }

    // ═══════════════════════════════════════════════════════
    // ═══════════════════════════════════════════════════════
    // CONTENEDORES DE CORAZONES - Abajo izquierda
    // ═══════════════════════════════════════════════════════
    void DrawHealthBar()
    {
        if (playerHealth == null) return;

        int maxH = playerHealth.maxHearts;
        int curH = playerHealth.currentHearts;
        float heartSize = 26f;
        float gap = 3f;
        float totalHeartsWidth = maxH * (heartSize + gap);
        float containerWidth = Mathf.Max(260f, totalHeartsWidth + 14f);
        float containerHeight = 32f;
        float x = 20f;
        float y = MegabonkUI.Height - 50f;

        // Fondo oscuro estilizado para los contenedores de corazón
        GUI.color = new Color(0.08f, 0.09f, 0.13f, 0.92f);
        GUI.DrawTexture(new Rect(x, y, containerWidth, containerHeight), whiteTex);

        // Borde elegante
        GUI.color = new Color(0.35f, 0.4f, 0.5f, 0.75f);
        GUI.DrawTexture(new Rect(x, y, containerWidth, 1f), whiteTex);
        GUI.DrawTexture(new Rect(x, y + containerHeight - 1f, containerWidth, 1f), whiteTex);
        GUI.DrawTexture(new Rect(x, y, 1f, containerHeight), whiteTex);
        GUI.DrawTexture(new Rect(x + containerWidth - 1f, y, 1f, containerHeight), whiteTex);

        // Estilo de los corazones
        GUIStyle heartStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            alignment = TextAnchor.MiddleCenter
        };

        for (int i = 0; i < maxH; i++)
        {
            Rect hRect = new Rect(x + 6f + i * (heartSize + gap), y + 3f, heartSize, heartSize);
            bool filled = (i < curH);

            if (filled)
            {
                // Corazón lleno rojo brillante
                heartStyle.normal.textColor = new Color(1f, 0.22f, 0.28f);
                GUI.Label(hRect, "❤", heartStyle);
            }
            else
            {
                // Contenedor de corazón vacío (gris oscuro)
                heartStyle.normal.textColor = new Color(0.35f, 0.38f, 0.44f, 0.85f);
                GUI.Label(hRect, "🖤", heartStyle);
            }
        }

        // Texto informativo numérico al lado
        GUI.color = Color.white;
        smallStyle.alignment = TextAnchor.MiddleLeft;
        smallStyle.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(x + totalHeartsWidth + 16f, y + 6f, 120f, 20f), $"{curH} / {maxH} ❤", smallStyle);
        smallStyle.fontStyle = FontStyle.Normal;

        // Indicador de estado superior: Escudo Sagrado / Corazón Inmortal / Quemadura
        if (XPSystem.Instance != null && XPSystem.Instance.holyShieldActive)
        {
            GUI.color = new Color(1f, 0.85f, 0.2f, 0.95f);
            smallStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(x, y - 20f, containerWidth, 18f), "🛡 ESCUDO SAGRADO ACTIVO", smallStyle);
        }
        else if (XPSystem.Instance != null && XPSystem.Instance.HasUpgrade("vitality_4") && XPSystem.Instance.immortalHeartReady)
        {
            GUI.color = new Color(0.4f, 1f, 0.6f, 0.95f);
            smallStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(x, y - 20f, containerWidth, 18f), "💖 CORAZÓN INMORTAL ACTIVO", smallStyle);
        }
        else if (playerHealth.isBurning)
        {
            GUI.color = new Color(1f, 0.4f, 0.1f, 0.95f);
            smallStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(x, y - 20f, containerWidth, 18f), "🔥 ¡QUEMADURA DE DRAGÓN!", smallStyle);
        }
    }

    // ═══════════════════════════════════════════════════════
    // MUNICIÓN INFINITA - Abajo derecha
    // ═══════════════════════════════════════════════════════
    void DrawAmmo()
    {
        if (playerShooting == null) return;

        float x = MegabonkUI.Width - 240f;
        float y = MegabonkUI.Height - 62f;

        labelStyle.alignment = TextAnchor.MiddleRight;
        string wName = playerShooting.currentWeapon != null ? playerShooting.currentWeapon.name.ToUpper() : "ARMA";

        if (playerShooting.isReloading)
        {
            GUI.color = new Color(1f, 0.85f, 0.2f);
            labelStyle.fontSize = 20;
            GUI.Label(new Rect(x, y, 220f, 30f), "⟳ RECARGANDO...", labelStyle);
            labelStyle.fontSize = 16;
        }
        else
        {
            labelStyle.fontSize = 26;
            GUI.color = playerShooting.currentAmmo <= 2 ? new Color(1f, 0.5f, 0.2f) : new Color(0.2f, 0.95f, 1f);
            string ammoText = $"{playerShooting.currentAmmo} / {playerShooting.magazineSize}  [∞]";
            GUI.Label(new Rect(x, y, 220f, 32f), ammoText, labelStyle);
            labelStyle.fontSize = 16;
        }

        GUI.color = Color.white;
        smallStyle.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(x, y - 20f, 220f, 20f), $"🔫 {wName} (INFINITO)", smallStyle);
    }

    // ═══════════════════════════════════════════════════════
    // INFO DE OLEADA - Arriba centro (Reloj grande)
    // ═══════════════════════════════════════════════════════
    void DrawWaveInfo()
    {
        if (spawner == null) return;
        GameManager gm = GameManager.Instance;

        float cx = MegabonkUI.Width / 2f;
        subtitleStyle.alignment = TextAnchor.MiddleCenter;

        // Reloj grande de 10:00 estilo Megabonk
        int totalSec = Mathf.FloorToInt(gm != null ? gm.gameTime : 0f);
        int mins = totalSec / 60;
        int secs = totalSec % 60;
        string timerStr = $"⏱ {mins:00}:{secs:00} / 10:00";

        GUIStyle timerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        timerStyle.normal.textColor = new Color(0.25f, 0.92f, 1f);
        GUI.Label(new Rect(cx - 200f, 22f, 400f, 28f), timerStr, timerStyle);

        if (spawner.isResting)
        {
            GUI.color = new Color(1f, 0.85f, 0.2f);
            string nextWave = "OLEADA " + (spawner.currentWave + 1) + " EN " +
                              Mathf.CeilToInt(spawner.restTimeLeft) + "s";
            GUI.Label(new Rect(cx - 200f, 48f, 400f, 22f), nextWave, subtitleStyle);
        }
        else
        {
            GUI.color = Color.white;
            string waveText = (spawner.currentBoss != null) 
                ? "⚠ ALERTA DE JEFE: OLEADA " + spawner.currentWave 
                : "OLEADA " + spawner.currentWave + " • " + spawner.enemiesRemaining + " enemigos";
            GUI.Label(new Rect(cx - 200f, 48f, 400f, 22f), waveText, subtitleStyle);
        }

        GUI.color = Color.white;
    }

    // ═══════════════════════════════════════════════════════
    // PUNTUACIÓN - Abajo centro
    // ═══════════════════════════════════════════════════════
    void DrawScore()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        float cx = MegabonkUI.Width / 2f;
        float y = MegabonkUI.Height - 40f;

        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.fontSize = 18;
        GUI.color = new Color(1f, 0.85f, 0.3f);
        GUI.Label(new Rect(cx - 150f, y, 300f, 25f), "PUNTOS: " + gm.score, labelStyle);
        labelStyle.fontSize = 16;
        GUI.color = Color.white;
    }

    // ═══════════════════════════════════════════════════════
    // CROSSHAIR Y HIT MARKER
    // ═══════════════════════════════════════════════════════
    void DrawCrosshair()
    {
        float cx = MegabonkUI.Width / 2f;
        float cy = MegabonkUI.Height / 2f;

        // === Calcular gap dinámico basado en spread actual ===
        float baseGap = 5f;
        float spreadGap = 0f;
        bool isSprinting = playerMovement != null && playerMovement.isSprinting;
        bool isMoving = playerMovement != null && (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f);

        if (playerShooting != null && XPSystem.Instance != null)
        {
            float totalSpread = playerShooting.currentWeapon != null
                ? playerShooting.currentWeapon.spread + XPSystem.Instance.spreadModifierDegrees
                : 5f;
            totalSpread = Mathf.Max(0f, totalSpread);
            spreadGap = totalSpread * 1.4f;
        }
        if (isMoving) spreadGap += 4f;
        if (isSprinting) spreadGap += 8f;

        // Interpolar suavemente el gap
        _crosshairGapSmoothed = Mathf.Lerp(_crosshairGapSmoothed, baseGap + spreadGap, Time.deltaTime * 10f);
        float gap = _crosshairGapSmoothed;
        float lineLen = 9f;
        float thickness = 2f;

        // === Color del crosshair ===
        bool isHit = playerShooting != null && playerShooting.hitMarkerActive;
        bool isReloading = playerShooting != null && playerShooting.isReloading;

        Color chColor;
        if (isHit)
            chColor = new Color(1f, 0.15f, 0.15f, 1f);
        else if (isReloading)
            chColor = new Color(1f, 0.8f, 0.1f, 0.9f);
        else
            chColor = new Color(0.9f, 0.95f, 1f, 0.85f);

        // === Sombra oscura para legibilidad ===
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(new Rect(cx - lineLen - gap + 1f, cy, lineLen, thickness), whiteTex);
        GUI.DrawTexture(new Rect(cx + gap + 1f, cy, lineLen, thickness), whiteTex);
        GUI.DrawTexture(new Rect(cx, cy - lineLen - gap + 1f, thickness, lineLen), whiteTex);
        GUI.DrawTexture(new Rect(cx, cy + gap + 1f, thickness, lineLen), whiteTex);

        // === Líneas principales ===
        GUI.color = chColor;
        GUI.DrawTexture(new Rect(cx - lineLen - gap, cy - thickness * 0.5f, lineLen, thickness), whiteTex);
        GUI.DrawTexture(new Rect(cx + gap, cy - thickness * 0.5f, lineLen, thickness), whiteTex);
        GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy - lineLen - gap, thickness, lineLen), whiteTex);
        GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy + gap, thickness, lineLen), whiteTex);

        // === Punto central ===
        GUI.color = new Color(chColor.r, chColor.g, chColor.b, isHit ? 1f : 0.6f);
        GUI.DrawTexture(new Rect(cx - 1.5f, cy - 1.5f, 3f, 3f), whiteTex);

        // === Hit marker (X roja pulsante) ===
        if (isHit)
        {
            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 30f);
            GUI.color = new Color(1f, 0.1f, 0.1f, pulse);
            float hm = 7f;
            float hmGap = gap + lineLen * 0.6f;
            // Diagonal lines forming X
            GUI.matrix = Matrix4x4.TRS(new Vector3(cx, cy, 0f), Quaternion.Euler(0f, 0f, 45f), Vector3.one);
            GUI.DrawTexture(new Rect(-hm * 0.5f - hmGap, -1f, hm, 2f), whiteTex);
            GUI.DrawTexture(new Rect(hmGap, -1f, hm, 2f), whiteTex);
            GUI.DrawTexture(new Rect(-1f, -hm * 0.5f - hmGap, 2f, hm), whiteTex);
            GUI.DrawTexture(new Rect(-1f, hmGap, 2f, hm), whiteTex);
            GUI.matrix = Matrix4x4.identity;
        }

        GUI.color = Color.white;
    }

    private float _crosshairGapSmoothed = 5f;


    // ═══════════════════════════════════════════════════════
    // MINIMAPA - Arriba derecha
    // ═══════════════════════════════════════════════════════
    BossChest3D[] minimapChests = new BossChest3D[0];
    StageInteractable[] minimapObjects = new StageInteractable[0];
    float nextMinimapScan;
    void DrawMinimap()
    {
        if (Time.unscaledTime >= nextMinimapScan)
        {
            nextMinimapScan = Time.unscaledTime + .25f;
            minimapChests = Object.FindObjectsByType<BossChest3D>();
            minimapObjects = Object.FindObjectsByType<StageInteractable>();
        }
        float mapSize = 135f;
        float margin = 20f;
        float mapX = MegabonkUI.Width - mapSize - margin;
        float mapY = 32f;
        float worldRadius = 60f;

        // Fondo
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(mapX, mapY, mapSize, mapSize), minimapBgTex);

        // Borde
        GUI.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        GUI.DrawTexture(new Rect(mapX, mapY, mapSize, 1.5f), whiteTex);
        GUI.DrawTexture(new Rect(mapX, mapY + mapSize - 1.5f, mapSize, 1.5f), whiteTex);
        GUI.DrawTexture(new Rect(mapX, mapY, 1.5f, mapSize), whiteTex);
        GUI.DrawTexture(new Rect(mapX + mapSize - 1.5f, mapY, 1.5f, mapSize), whiteTex);

        float centerMapX = mapX + mapSize / 2f;
        float centerMapY = mapY + mapSize / 2f;

        // Jugador
        GUI.color = new Color(0.2f, 0.9f, 0.3f);
        GUI.DrawTexture(new Rect(centerMapX - 3.5f, centerMapY - 3.5f, 7f, 7f), whiteTex);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Vector3 pPos = playerObj.transform.position;

            // Cofres de Jefe (Boss Chests) - Diamantes dorados pulsantes
            BossChest3D[] chests = minimapChests;
            foreach (var chest in chests)
            {
                if (chest == null) continue;
                Vector3 cPos = chest.transform.position;
                float dx = cPos.x - pPos.x;
                float dz = cPos.z - pPos.z;

                float mx = centerMapX + (dx / worldRadius) * (mapSize / 2f);
                float my = centerMapY - (dz / worldRadius) * (mapSize / 2f);

                if (mx >= mapX + 3 && mx <= mapX + mapSize - 3 &&
                    my >= mapY + 3 && my <= mapY + mapSize - 3)
                {
                    float pulse = 0.75f + Mathf.PingPong(Time.unscaledTime * 3.5f, 0.25f);
                    GUI.color = new Color(1f, 0.85f, 0.2f, pulse);
                    GUI.DrawTexture(new Rect(mx - 4f, my - 4f, 8f, 8f), whiteTex);
                }
            }

            // Objetivos de etapa: verde = santuarios, blanco = vasijas,
            // rojo = arco de jefe y azul = portal de transición.
            StageInteractable[] stageObjects = minimapObjects;
            foreach (StageInteractable stageObject in stageObjects)
            {
                if (stageObject == null) continue;
                bool finalShelter = stageObject.kind == StageInteractable.Kind.ChargeShrine &&
                    StageDirector.Instance != null && StageDirector.Instance.FinalEncounterActive;
                if (stageObject.completed && !finalShelter) continue;
                Vector3 sPos = stageObject.transform.position;
                float mx = centerMapX + ((sPos.x - pPos.x) / worldRadius) * (mapSize / 2f);
                float my = centerMapY - ((sPos.z - pPos.z) / worldRadius) * (mapSize / 2f);
                if (mx < mapX + 3 || mx > mapX + mapSize - 3 || my < mapY + 3 || my > mapY + mapSize - 3) continue;

                Color marker = Color.white;
                float size = 5f;
                switch (stageObject.kind)
                {
                    case StageInteractable.Kind.ChargeShrine:
                        marker = finalShelter && stageObject.completed ? new Color(0.2f, 0.85f, 1f) : new Color(0.2f, 1f, 0.4f);
                        size = finalShelter ? 9f : 7f;
                        break;
                    case StageInteractable.Kind.GreedShrine:
                    case StageInteractable.Kind.BossCurseShrine:
                    case StageInteractable.Kind.Microwave: marker = new Color(0.2f, 1f, 0.4f); size = 7f; break;
                    case StageInteractable.Kind.BossGate: marker = new Color(1f, 0.15f, 0.2f); size = 9f; break;
                    case StageInteractable.Kind.Portal: marker = new Color(0.25f, 0.65f, 1f); size = 9f; break;
                }
                GUI.color = marker;
                GUI.DrawTexture(new Rect(mx - size * 0.5f, my - size * 0.5f, size, size), whiteTex);
            }

            // Enemigos
            foreach (EnemyHealth3D e in EnemyHealth3D.ActiveEnemies)
            {
                if (e == null || e.IsDying) continue;
                Vector3 ePos = e.transform.position;
                float dx = ePos.x - pPos.x;
                float dz = ePos.z - pPos.z;

                float mx = centerMapX + (dx / worldRadius) * (mapSize / 2f);
                float my = centerMapY - (dz / worldRadius) * (mapSize / 2f);

                if (mx >= mapX + 3 && mx <= mapX + mapSize - 3 &&
                    my >= mapY + 3 && my <= mapY + mapSize - 3)
                {
                    EnemyAI3D ai = e.GetComponent<EnemyAI3D>();
                    if (ai != null && ai.enemyType == EnemyAI3D.EnemyType.Boss)
                    {
                        // Jefe: Marcador grande carmesí
                        GUI.color = new Color(1f, 0.1f, 0.25f);
                        GUI.DrawTexture(new Rect(mx - 5f, my - 5f, 10f, 10f), whiteTex);
                    }
                    else if (ai != null && (ai.enemyType == EnemyAI3D.EnemyType.Charger || ai.enemyType == EnemyAI3D.EnemyType.Sploder))
                    {
                        // Élite / Rápido / Explosivo: Naranja
                        GUI.color = new Color(1f, 0.6f, 0.15f);
                        GUI.DrawTexture(new Rect(mx - 3.5f, my - 3.5f, 7f, 7f), whiteTex);
                    }
                    else
                    {
                        // Común: Rojo
                        GUI.color = new Color(1f, 0.35f, 0.35f);
                        GUI.DrawTexture(new Rect(mx - 2.5f, my - 2.5f, 5f, 5f), whiteTex);
                    }
                }
            }
        }

        GUI.color = Color.white;
    }

    // ═══════════════════════════════════════════════════════
    // KILL FEED
    // ═══════════════════════════════════════════════════════
    void DrawKillFeed()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        float x = MegabonkUI.Width - 240f;
        float startY = 175f;
        float lineHeight = 20f;
        float now = Time.unscaledTime;

        smallStyle.alignment = TextAnchor.MiddleRight;

        for (int i = 0; i < gm.killFeed.Length; i++)
        {
            var entry = gm.killFeed[i];
            if (string.IsNullOrEmpty(entry.text)) continue;

            float age = now - entry.time;
            if (age > 4f) continue;

            float alpha = age > 3f ? (4f - age) : 1f;
            GUI.color = new Color(1f, 0.85f, 0.3f, alpha);
            GUI.Label(new Rect(x, startY + i * lineHeight, 220f, lineHeight), entry.text, smallStyle);
        }

        GUI.color = Color.white;
    }

    // ═══════════════════════════════════════════════════════
    // INDICADOR DE SPRINT
    // ═══════════════════════════════════════════════════════
    void DrawSprintIndicator()
    {
        if (playerMovement == null || !playerMovement.isSprinting) return;

        float x = 20f;
        float y = MegabonkUI.Height - 72f;

        smallStyle.alignment = TextAnchor.MiddleLeft;
        GUI.color = new Color(0.2f, 0.95f, 1f, 0.9f);
        GUI.Label(new Rect(x, y, 160f, 20f), "🏃 SPRINT ACTIVO", smallStyle);
        GUI.color = Color.white;
    }

    // ═══════════════════════════════════════════════════════
    // MENÚ PRINCIPAL MULTI-PANTALLA ESTILO MEGABONK
    // ═══════════════════════════════════════════════════════
    void DrawMainMenu(GameManager gm)
    {
        switch (currentMenuScreen)
        {
            case MenuSubScreen.Main:
                if(!OriginalMenuBridge.Visible)DrawModernHub(gm);
                break;
            case MenuSubScreen.StartRun:
                DrawModernLoadout(gm);
                break;
            case MenuSubScreen.Characters:
                DrawModernLoadout(gm);
                break;
            case MenuSubScreen.MapSelect:
                DrawMapSelection(gm);
                break;
            case MenuSubScreen.Unlocks:
                DrawUnlocks(gm);
                break;
            case MenuSubScreen.Settings:
                DrawModernSettings(gm);
                break;
            case MenuSubScreen.Leaderboards:
                DrawModernStatistics(gm);
                break;
            case MenuSubScreen.Shop:
                DrawMetaShop(gm);
                break;
            case MenuSubScreen.Toggler:
                DrawToggler(gm);
                break;
            case MenuSubScreen.Quests: DrawQuestPanel(); break;
            case MenuSubScreen.Credits: DrawCreditsPanel(); break;
            case MenuSubScreen.LanLobby: DrawLanLobby(gm); break;
        }
    }

    void DrawMenuBackground(float sw, float sh)
    {
        MegabonkUI.Background();
    }

    // ── 1. HUB PRINCIPAL (MAIN MENU) ──
    void DrawMenuMain_Hub(GameManager gm)
    {
        float sw = MegabonkUI.Width;
        float sh = MegabonkUI.Height;
        float cx = sw / 2f;
        float cy = sh / 2f;

        DrawMenuBackground(sw, sh);

        // LOGO / TÍTULO DE MEGABONK
        float pulse = Mathf.PingPong(Time.unscaledTime * 1.5f, 1f);
        titleStyle.fontSize = 46;
        GUI.color = new Color(0.18f, 0.88f, 1f);
        GUI.Label(new Rect(cx - 450f, cy - 240f, 900f, 54f), "⚡ MEGABONK ⚡", titleStyle);

        subtitleStyle.fontSize = 14;
        GUI.color = new Color(0.7f + pulse * 0.25f, 0.85f, 1f);
        GUI.Label(new Rect(cx - 450f, cy - 188f, 900f, 22f),
            "SURVIVORS 3D • SESIÓN DE 20 MINUTOS • HORDA PROCEDURAL Y SINERGIAS", subtitleStyle);

        // Disposición en 2 Columnas
        float colSpacing = 24f;
        float navW = 330f;
        float infoW = 510f;
        float totalW = navW + colSpacing + infoW;
        float startX = cx - totalW / 2f;
        float startY = cy - 145f;

        // ── COLUMNA IZQUIERDA: BOTONES DE NAVEGACIÓN ──
        float btnH = 54f;
        float btnGap = 10f;

        DrawMenuNavButton(new Rect(startX, startY + 0 * (btnH + btnGap), navW, btnH),
            "▶ JUGAR / START RUN", "Configurar expedición y comenzar", new Color(0.2f, 0.9f, 1f), true,
            () => currentMenuScreen = MenuSubScreen.StartRun);

        DrawMenuNavButton(new Rect(startX, startY + 1 * (btnH + btnGap), navW, btnH),
            "👤 CHARACTERS & UNLOCKS", "Supervivientes y 18 sinergias secretas", new Color(1f, 0.85f, 0.25f), false,
            () => currentMenuScreen = MenuSubScreen.Characters);

        DrawMenuNavButton(new Rect(startX, startY + 2 * (btnH + btnGap), navW, btnH),
            "⚙ SETTINGS (AJUSTES)", "FOV, gráficos, controles, auto-aim y audio", new Color(0.4f, 0.8f, 1f), false,
            () => currentMenuScreen = MenuSubScreen.Settings);

        DrawMenuNavButton(new Rect(startX, startY + 3 * (btnH + btnGap), navW, btnH),
            "🏆 LEADERBOARDS & STATS", "Récords personales y ranking arcade", new Color(1f, 0.6f, 0.2f), false,
            () => currentMenuScreen = MenuSubScreen.Leaderboards);

        DrawMenuNavButton(new Rect(startX, startY + 4 * (btnH + btnGap), navW, btnH),
            "🚪 QUIT (SALIR)", "Cerrar el juego y salir al escritorio", new Color(1f, 0.3f, 0.35f), false,
            () => QuitGame());

        // ── COLUMNA DERECHA: TARJETA DE RESUMEN Y PARTIDA RÁPIDA ──
        float infoX = startX + navW + colSpacing;
        float infoH = (btnH + btnGap) * 5f - btnGap;

        // Fondo del panel
        GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);
        GUI.DrawTexture(new Rect(infoX, startY, infoW, infoH), whiteTex);

        // Borde
        GUI.color = new Color(0.2f, 0.8f, 1f, 0.7f);
        GUI.DrawTexture(new Rect(infoX, startY, infoW, 2f), whiteTex);
        GUI.DrawTexture(new Rect(infoX, startY + infoH - 2f, infoW, 2f), whiteTex);
        GUI.DrawTexture(new Rect(infoX, startY, 2f, infoH), whiteTex);
        GUI.DrawTexture(new Rect(infoX + infoW - 2f, startY, 2f, infoH), whiteTex);

        // Título del panel
        GUIStyle cardHeaderSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        cardHeaderSt.normal.textColor = new Color(0.2f, 0.92f, 1f);
        GUI.color = Color.white;
        GUI.Label(new Rect(infoX, startY + 8f, infoW, 20f), "⚡ DESPLIEGUE RÁPIDO & HISTORIAL", cardHeaderSt);

        // Información del héroe y arma seleccionados
        CharacterInfo currentHero = Characters[(int)gm.selectedCharacter];
        GUIStyle subSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };
        subSt.normal.textColor = Color.white;

        // Card Héroe rápido
        float previewY = startY + 34f;
        GUI.color = new Color(currentHero.color.r * 0.15f, currentHero.color.g * 0.15f, currentHero.color.b * 0.15f, 0.9f);
        GUI.DrawTexture(new Rect(infoX + 16f, previewY, infoW - 32f, 44f), whiteTex);
        GUI.color = currentHero.color;
        GUI.DrawTexture(new Rect(infoX + 16f, previewY, 4f, 44f), whiteTex);
        GUI.color = Color.white;
        GUI.Label(new Rect(infoX + 26f, previewY + 4f, infoW - 46f, 18f),
            $"{currentHero.icon} HÉROE ACTIVO: {currentHero.name} - {currentHero.title}", cardHeaderSt);
        subSt.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
        GUI.Label(new Rect(infoX + 26f, previewY + 22f, infoW - 46f, 18f), currentHero.passive, subSt);

        // Card Arma rápida
        float weaponPreviewY = previewY + 50f;
        GUI.color = new Color(0.09f, 0.12f, 0.18f, 0.9f);
        GUI.DrawTexture(new Rect(infoX + 16f, weaponPreviewY, infoW - 32f, 44f), whiteTex);
        GUI.color = new Color(0.2f, 0.85f, 1f);
        GUI.DrawTexture(new Rect(infoX + 16f, weaponPreviewY, 4f, 44f), whiteTex);
        GUI.color = Color.white;
        string wTitle = gm.selectedWeaponType.ToString().ToUpper();
        GUI.Label(new Rect(infoX + 26f, weaponPreviewY + 4f, infoW - 46f, 18f),
            $"🔫 ARMA INICIAL: {wTitle}", cardHeaderSt);
        string wStatDesc = (gm.selectedWeaponType == WeaponType.Revolver) ? "Daño: 35 | Cadencia: 2.0/s | Balas: 6 | Recarga: 0.8s" :
                           (gm.selectedWeaponType == WeaponType.Shotgun) ? "Daño: 18x5 | Cadencia: 1.2/s | Balas: 3 | Recarga: 1.2s" :
                           (gm.selectedWeaponType == WeaponType.SMG) ? "Daño: 11 | Cadencia: 8.0/s | Balas: 25 | Recarga: 0.6s" :
                           "Daño: 80 (Explosivo) | Cadencia: 0.9/s | Balas: 1 | Recarga: 0.9s";
        GUI.Label(new Rect(infoX + 26f, weaponPreviewY + 22f, infoW - 46f, 18f), wStatDesc, subSt);

        // Tabla de estadísticas históricas
        float statsY = weaponPreviewY + 52f;
        int bestSec = Mathf.FloorToInt(gm.bestTimeSurvived);
        string bestTimeStr = $"{bestSec / 60:00}:{bestSec % 60:00}";
        GUIStyle statLabelSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft
        };
        statLabelSt.normal.textColor = new Color(0.8f, 0.85f, 0.9f);

        GUI.Label(new Rect(infoX + 24f, statsY + 0f, 230f, 18f), $"⏱ Mejor Supervivencia: {bestTimeStr} / 10:00", statLabelSt);
        GUI.Label(new Rect(infoX + 260f, statsY + 0f, 230f, 18f), $"🏆 Mayor Puntuación: {gm.highestScore}", statLabelSt);
        GUI.Label(new Rect(infoX + 24f, statsY + 18f, 230f, 18f), $"💀 Kills Récord: {gm.highestKills}", statLabelSt);
        GUI.Label(new Rect(infoX + 260f, statsY + 18f, 230f, 18f), $"☠ Jefes Goliat Eliminados: {gm.bossesSlain}", statLabelSt);
        GUI.Label(new Rect(infoX + 24f, statsY + 36f, 230f, 18f), $"🚀 Intentos Totales: {gm.totalRunsAttempted}", statLabelSt);
        GUI.Label(new Rect(infoX + 260f, statsY + 36f, 230f, 18f), $"🎯 Auto-Aim: {(settingAutoAim ? "Activado" : "Desactivado")}", statLabelSt);

        // Botón grande de combate directo
        Rect playBtnRect = new Rect(infoX + 16f, startY + infoH - 56f, infoW - 32f, 44f);
        bool hoverDirect = playBtnRect.Contains(Event.current.mousePosition);
        GUI.color = hoverDirect ? new Color(0.2f, 0.85f, 0.4f, 0.98f) : new Color(0.12f, 0.6f, 0.3f, 0.9f);
        GUI.DrawTexture(playBtnRect, whiteTex);

        GUIStyle directSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        directSt.normal.textColor = Color.white;
        GUI.Label(new Rect(playBtnRect.x, playBtnRect.y + 4f, playBtnRect.width, 20f), "▶ ENTRAR AL COMBATE DIRECTO", directSt);
        GUIStyle hotkeySt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter
        };
        hotkeySt.normal.textColor = new Color(0.9f, 1f, 0.9f, 0.85f);
        GUI.Label(new Rect(playBtnRect.x, playBtnRect.y + 24f, playBtnRect.width, 16f), "[ Haz clic o presiona ESPACIO / ENTER ]", hotkeySt);

        if ((Event.current.type == EventType.MouseDown && hoverDirect) ||
            Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            gm.StartGame();
            Event.current.Use();
        }

        // Footer informativo
        subtitleStyle.fontSize = 12;
        GUI.color = new Color(0.6f, 0.7f, 0.8f);
        GUI.Label(new Rect(0, startY + infoH + 16f, sw, 20f),
            "Usa WASD para moverte • Click para disparar • R para recargar • 1-4 para mejoras al subir de nivel", subtitleStyle);

        titleStyle.fontSize = 36;
        GUI.color = Color.white;
    }

    // ── 2. SELECCIÓN DE PERSONAJE Y ARMAS (START RUN) ──
    void DrawMenu_StartRun(GameManager gm)
    {
        float sw = MegabonkUI.Width;
        float sh = MegabonkUI.Height;
        float cx = sw / 2f;
        float cy = sh / 2f;

        DrawMenuBackground(sw, sh);

        // Header
        titleStyle.fontSize = 34;
        GUI.color = new Color(0.2f, 0.9f, 1f);
        GUI.Label(new Rect(cx - 450f, cy - 240f, 900f, 40f), "⚡ CONFIGURACIÓN DE LA PARTIDA (START RUN) ⚡", titleStyle);

        subtitleStyle.fontSize = 13;
        GUI.color = new Color(0.85f, 0.9f, 1f);
        GUI.Label(new Rect(cx - 450f, cy - 198f, 900f, 20f),
            "SELECCIONA TU HÉROE Y TU ARMA PRINCIPAL ANTES DE DESPLEGARTE EN LA NIEBLA", subtitleStyle);

        float blockW = 880f;
        float blockStartX = cx - blockW / 2f;

        // ── 1. PERSONAJES ──
        float charSectionY = cy - 168f;
        smallStyle.alignment = TextAnchor.MiddleLeft;
        smallStyle.fontStyle = FontStyle.Bold;
        GUI.color = new Color(1f, 0.85f, 0.25f);
        GUI.Label(new Rect(blockStartX, charSectionY, blockW, 20f), "1. SELECCIONA TU SUPERVIVIENTE:", smallStyle);
        smallStyle.fontStyle = FontStyle.Normal;

        float charCardW = 211f;
        float charCardH = 120f;
        float charSpacing = 12f;
        float charCardsY = charSectionY + 24f;

        for (int i = 0; i < Characters.Length; i++)
        {
            var ch = Characters[i];
            bool selected = (gm.selectedCharacter == ch.type);
            Rect r = new Rect(blockStartX + i * (charCardW + charSpacing), charCardsY, charCardW, charCardH);
            DrawCharacterSelectCard(r, ch, selected, () => { gm.selectedOriginalCharacter = ""; gm.selectedCharacter = ch.type; });
        }

        // ── 2. ARMAS ──
        float weaponSectionY = charCardsY + charCardH + 16f;
        smallStyle.fontStyle = FontStyle.Bold;
        GUI.color = new Color(0.3f, 0.9f, 1f);
        GUI.Label(new Rect(blockStartX, weaponSectionY, blockW, 20f), "2. SELECCIONA TU ARMA INICIAL:", smallStyle);
        smallStyle.fontStyle = FontStyle.Normal;

        float weaponCardsY = weaponSectionY + 24f;
        float weaponCardW = 211f;
        float weaponCardH = 75f;

        DrawWeaponButton(new Rect(blockStartX + 0 * (weaponCardW + charSpacing), weaponCardsY, weaponCardW, weaponCardH),
            "🔫 REVÓLVER", "Daño: 35 | Cad: 2.0/s\nBalas: 6 | Rec: 0.8s", WeaponType.Revolver, gm);

        DrawWeaponButton(new Rect(blockStartX + 1 * (weaponCardW + charSpacing), weaponCardsY, weaponCardW, weaponCardH),
            "💥 ESCOPETA", "Daño: 18x5 | Cad: 1.2/s\nBalas: 3 | Rec: 1.2s", WeaponType.Shotgun, gm);

        DrawWeaponButton(new Rect(blockStartX + 2 * (weaponCardW + charSpacing), weaponCardsY, weaponCardW, weaponCardH),
            "⚡ SUBFUSIL", "Daño: 11 | Cad: 8.0/s\nBalas: 25 | Rec: 0.6s", WeaponType.SMG, gm);

        DrawWeaponButton(new Rect(blockStartX + 3 * (weaponCardW + charSpacing), weaponCardsY, weaponCardW, weaponCardH),
            "🏹 BALLESTA", "Daño: 80 (Explosivo)\nBalas: 1 | Rec: 0.9s", WeaponType.Crossbow, gm);

        // ── 3. ESTILOS DE JUEGO / MODOS DE FUEGO ──
        float styleSectionY = weaponCardsY + weaponCardH + 12f;
        smallStyle.fontStyle = FontStyle.Bold;
        GUI.color = new Color(1f, 0.65f, 0.2f);
        GUI.Label(new Rect(blockStartX, styleSectionY, blockW, 18f), "3. SELECCIONA TU ESTILO DE JUEGO / MODO DE FUEGO:", smallStyle);
        smallStyle.fontStyle = FontStyle.Normal;

        float styleCardsY = styleSectionY + 20f;
        float styleCardW = 211f;
        float styleCardH = 48f;

        DrawPlayStyleButton(new Rect(blockStartX + 0 * (styleCardW + charSpacing), styleCardsY, styleCardW, styleCardH),
            "🔥 FUEGO INFERNAL", "Balas incendiarias con daño de ignición", GameManager.PlayStyle.FuegoInfernal, gm);

        DrawPlayStyleButton(new Rect(blockStartX + 1 * (styleCardW + charSpacing), styleCardsY, styleCardW, styleCardH),
            "⚡ RÁFAGA VELOZ", "+35% Cadencia y perforación", GameManager.PlayStyle.RafagaVeloz, gm);

        DrawPlayStyleButton(new Rect(blockStartX + 2 * (styleCardW + charSpacing), styleCardsY, styleCardW, styleCardH),
            "🛡️ TITÁN BLINDADO", "+80 Salud y gran retroceso", GameManager.PlayStyle.TitanBlindado, gm);

        DrawPlayStyleButton(new Rect(blockStartX + 3 * (styleCardW + charSpacing), styleCardsY, styleCardW, styleCardH),
            "🎯 ESTÁNDAR", "Combate puro y equilibrado", GameManager.PlayStyle.Estandar, gm);

        // ── BARRA INFERIOR DE ACCIÓN ──
        float bottomBarY = styleCardsY + styleCardH + 16f;

        // Botón Volver
        Rect backRect = new Rect(blockStartX, bottomBarY, 260f, 48f);
        bool hoverBack = backRect.Contains(Event.current.mousePosition);
        GUI.color = hoverBack ? new Color(0.2f, 0.25f, 0.35f, 0.95f) : new Color(0.1f, 0.12f, 0.18f, 0.9f);
        GUI.DrawTexture(backRect, whiteTex);
        GUI.color = Color.white;
        smallStyle.alignment = TextAnchor.MiddleCenter;
        smallStyle.fontStyle = FontStyle.Bold;
        GUI.Label(backRect, "⮌ VOLVER AL MENÚ [ ESC ]", smallStyle);
        smallStyle.fontStyle = FontStyle.Normal;

        if (Event.current.type == EventType.MouseDown && hoverBack)
        {
            currentMenuScreen = MenuSubScreen.Main;
            Event.current.Use();
        }

        // Botón Iniciar Combate
        Rect startBtnRect = new Rect(blockStartX + blockW - 360f, bottomBarY, 360f, 48f);
        bool hoverStart = startBtnRect.Contains(Event.current.mousePosition);
        GUI.color = hoverStart ? new Color(0.2f, 0.85f, 0.4f, 0.98f) : new Color(0.12f, 0.6f, 0.3f, 0.9f);
        GUI.DrawTexture(startBtnRect, whiteTex);

        // Borde
        GUI.color = hoverStart ? Color.white : new Color(0.3f, 1f, 0.5f);
        GUI.DrawTexture(new Rect(startBtnRect.x, startBtnRect.y, startBtnRect.width, 2f), whiteTex);
        GUI.DrawTexture(new Rect(startBtnRect.x, startBtnRect.yMax - 2f, startBtnRect.width, 2f), whiteTex);
        GUI.DrawTexture(new Rect(startBtnRect.x, startBtnRect.y, 2f, startBtnRect.height), whiteTex);
        GUI.DrawTexture(new Rect(startBtnRect.xMax - 2f, startBtnRect.y, 2f, startBtnRect.height), whiteTex);

        GUIStyle goSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        goSt.normal.textColor = Color.white;
        GUI.Label(new Rect(startBtnRect.x, startBtnRect.y + 4f, startBtnRect.width, 22f), "▶ ENTRAR AL COMBATE", goSt);
        GUIStyle hotSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter
        };
        hotSt.normal.textColor = new Color(0.9f, 1f, 0.9f, 0.85f);
        GUI.Label(new Rect(startBtnRect.x, startBtnRect.y + 26f, startBtnRect.width, 16f), "[ Presiona ESPACIO o ENTER ]", hotSt);

        if ((Event.current.type == EventType.MouseDown && hoverStart) ||
            Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            gm.StartGame();
            Event.current.Use();
        }

        titleStyle.fontSize = 36;
        GUI.color = Color.white;
    }

    // ── 3. PERSONAJES Y CÓDICE DE DESBLOQUEOS (CHARACTERS & UNLOCKS) ──
    void DrawMenu_Characters(GameManager gm)
    {
        float sw = MegabonkUI.Width;
        float sh = MegabonkUI.Height;
        float cx = sw / 2f;
        float cy = sh / 2f;

        DrawMenuBackground(sw, sh);

        // Header
        titleStyle.fontSize = 32;
        GUI.color = new Color(1f, 0.85f, 0.25f);
        GUI.Label(new Rect(cx - 450f, cy - 240f, 900f, 40f), "👤 ELENCO DE SUPERVIVIENTES Y CÓDICE DE SINERGIAS", titleStyle);

        subtitleStyle.fontSize = 13;
        GUI.color = new Color(0.85f, 0.9f, 1f);
        GUI.Label(new Rect(cx - 450f, cy - 200f, 900f, 20f),
            "CONOCE LAS PASIVAS DE CADA HÉROE Y CONSULTA LAS COMBINACIONES DE SINERGIAS SECRETAS", subtitleStyle);

        float totalW = 900f;
        float startX = cx - totalW / 2f;
        float startY = cy - 170f;
        float colH = 345f;

        // Panel Izquierdo: 4 Héroes en vertical (width = 360f)
        float heroColW = 360f;
        float heroCardH = 78f;
        float heroGap = 10f;

        smallStyle.alignment = TextAnchor.MiddleLeft;
        smallStyle.fontStyle = FontStyle.Bold;
        GUI.color = new Color(0.3f, 0.9f, 1f);
        GUI.Label(new Rect(startX, startY - 20f, heroColW, 18f), "ROSTER DE SUPERVIVIENTES:", smallStyle);
        smallStyle.fontStyle = FontStyle.Normal;

        for (int i = 0; i < Characters.Length; i++)
        {
            var ch = Characters[i];
            bool isSelected = (gm.selectedCharacter == ch.type);
            Rect r = new Rect(startX, startY + i * (heroCardH + heroGap), heroColW, heroCardH);
            DrawCharacterSelectCard(r, ch, isSelected, () => { gm.selectedOriginalCharacter = ""; gm.selectedCharacter = ch.type; });
        }

        // Panel Derecho: Códice de Sinergias (width = 516f)
        float codexX = startX + heroColW + 24f;
        float codexW = totalW - heroColW - 24f;

        smallStyle.fontStyle = FontStyle.Bold;
        GUI.color = new Color(1f, 0.75f, 0.2f);
        GUI.Label(new Rect(codexX, startY - 20f, codexW, 18f), "CÓDICE DE LAS 18 SINERGIAS ELEMENTALES Y SECRETAS:", smallStyle);
        smallStyle.fontStyle = FontStyle.Normal;

        // Caja contenedora de sinergias
        GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
        GUI.DrawTexture(new Rect(codexX, startY, codexW, colH), whiteTex);
        GUI.color = new Color(0.3f, 0.5f, 0.7f, 0.5f);
        GUI.DrawTexture(new Rect(codexX, startY, codexW, 1f), whiteTex);
        GUI.DrawTexture(new Rect(codexX, startY + colH - 1f, codexW, 1f), whiteTex);
        GUI.DrawTexture(new Rect(codexX, startY, 1f, colH), whiteTex);
        GUI.DrawTexture(new Rect(codexX + codexW - 1f, startY, 1f, colH), whiteTex);

        // Obtenemos las sinergias registradas
        var allSyns = UpgradeCatalog.AllUpgrades.FindAll(u => u.type == UpgradeType.Synergy);

        // Scrollview para las sinergias
        float viewH = allSyns.Count * 56f + 10f;
        Rect scrollRect = new Rect(codexX + 2f, startY + 2f, codexW - 4f, colH - 4f);
        Rect contentRect = new Rect(0, 0, codexW - 22f, viewH);

        synergiesScrollPos = GUI.BeginScrollView(scrollRect, synergiesScrollPos, contentRect);

        GUIStyle synTitleSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        GUIStyle synDescSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };
        synDescSt.normal.textColor = new Color(0.85f, 0.88f, 0.95f);

        for (int i = 0; i < allSyns.Count; i++)
        {
            var syn = allSyns[i];
            float itemY = i * 56f + 4f;

            // Fondo de fila
            GUI.color = (i % 2 == 0) ? new Color(0.09f, 0.12f, 0.18f, 0.8f) : new Color(0.07f, 0.09f, 0.14f, 0.8f);
            GUI.DrawTexture(new Rect(4f, itemY, codexW - 30f, 52f), whiteTex);

            // Borde izquierdo de color
            GUI.color = syn.color;
            GUI.DrawTexture(new Rect(4f, itemY, 3.5f, 52f), whiteTex);

            // Título de la sinergia
            synTitleSt.normal.textColor = syn.color;
            string synReqs = syn.synergyRequirements != null && syn.synergyRequirements.Length >= 2
                ? $"[Req: {syn.synergyRequirements[0]} + {syn.synergyRequirements[1]}]"
                : "";
            GUI.Label(new Rect(14f, itemY + 3f, codexW - 50f, 18f), $"{syn.icon} {syn.name}  {synReqs}", synTitleSt);

            // Descripción
            GUI.color = Color.white;
            GUI.Label(new Rect(14f, itemY + 20f, codexW - 55f, 30f), syn.description, synDescSt);
        }

        GUI.EndScrollView();

        // Botón Volver
        Rect backBtn = new Rect(startX, startY + colH + 16f, 280f, 44f);
        bool hoverBack = backBtn.Contains(Event.current.mousePosition);
        GUI.color = hoverBack ? new Color(0.2f, 0.25f, 0.35f, 0.95f) : new Color(0.1f, 0.12f, 0.18f, 0.9f);
        GUI.DrawTexture(backBtn, whiteTex);
        GUI.color = Color.white;
        smallStyle.alignment = TextAnchor.MiddleCenter;
        smallStyle.fontStyle = FontStyle.Bold;
        GUI.Label(backBtn, "⮌ VOLVER AL MENÚ [ ESC ]", smallStyle);
        smallStyle.fontStyle = FontStyle.Normal;

        if (Event.current.type == EventType.MouseDown && hoverBack)
        {
            currentMenuScreen = MenuSubScreen.Main;
            Event.current.Use();
        }

        titleStyle.fontSize = 36;
        GUI.color = Color.white;
    }

    // ── 4. AJUSTES Y CONFIGURACIÓN (SETTINGS) ──
    void DrawMenu_Settings(GameManager gm)
    {
        float sw = MegabonkUI.Width;
        float sh = MegabonkUI.Height;
        float cx = sw / 2f;
        float cy = sh / 2f;

        DrawMenuBackground(sw, sh);

        // Header
        titleStyle.fontSize = 34;
        GUI.color = new Color(0.2f, 0.9f, 1f);
        GUI.Label(new Rect(cx - 450f, cy - 240f, 900f, 40f), "⚙ AJUSTES Y CONFIGURACIÓN (SETTINGS) ⚙", titleStyle);

        subtitleStyle.fontSize = 13;
        GUI.color = new Color(0.85f, 0.9f, 1f);
        GUI.Label(new Rect(cx - 450f, cy - 198f, 900f, 20f),
            "CONFIGURA RENDIMIENTO GRÁFICO, FOV, CONTROLES, TARGETING (AUTO-AIM) Y AUDIO", subtitleStyle);

        float totalW = 900f;
        float cardW = 280f;
        float cardH = 330f;
        float cardGap = 30f;
        float startX = cx - totalW / 2f;
        float startY = cy - 165f;

        GUIStyle catHeaderSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        GUIStyle optSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft
        };
        optSt.normal.textColor = new Color(0.85f, 0.9f, 0.95f);

        // ── PANEL 1: VÍDEO & FOV ──
        float p1X = startX;
        DrawMenuPanelBox(p1X, startY, cardW, cardH, new Color(0.2f, 0.85f, 1f));
        catHeaderSt.normal.textColor = new Color(0.2f, 0.85f, 1f);
        GUI.Label(new Rect(p1X, startY + 10f, cardW, 20f), "🖥 VÍDEO & FOV", catHeaderSt);

        // FOV Slider
        GUI.Label(new Rect(p1X + 16f, startY + 45f, cardW - 32f, 18f), $"Campo de Visión (FOV): {Mathf.RoundToInt(settingFOV)}°", optSt);
        float newFOV = GUI.HorizontalSlider(new Rect(p1X + 16f, startY + 68f, cardW - 32f, 20f), settingFOV, 60f, 105f);
        if (Mathf.Abs(newFOV - settingFOV) > 0.1f)
        {
            settingFOV = newFOV;
            if (Camera.main != null) Camera.main.fieldOfView = settingFOV;
        }

        // Gráficos Low-Poly / Presets
        GUI.Label(new Rect(p1X + 16f, startY + 105f, cardW - 32f, 18f), "Preajuste Gráfico:", optSt);
        float qBtnW = (cardW - 32f - 8f) / 3f;
        float qBtnH = 28f;
        float qBtnY = startY + 128f;

        string[] qNames = new string[] { "Rápido", "Medio", "Ultra" };
        for (int q = 0; q < 3; q++)
        {
            Rect qr = new Rect(p1X + 16f + q * (qBtnW + 4f), qBtnY, qBtnW, qBtnH);
            bool isQ = (settingQualityLevel == q);
            bool hoverQ = qr.Contains(Event.current.mousePosition);
            GUI.color = isQ ? new Color(0.2f, 0.7f, 0.95f, 0.95f) : (hoverQ ? new Color(0.15f, 0.2f, 0.3f) : new Color(0.08f, 0.1f, 0.15f));
            GUI.DrawTexture(qr, whiteTex);
            GUI.color = isQ ? Color.white : new Color(0.7f, 0.8f, 0.9f);
            GUIStyle qSt = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter, fontStyle = isQ ? FontStyle.Bold : FontStyle.Normal };
            GUI.Label(qr, qNames[q], qSt);
            if (Event.current.type == EventType.MouseDown && hoverQ)
            {
                settingQualityLevel = q;
                QualitySettings.SetQualityLevel(q);
                Event.current.Use();
            }
        }

        GUI.color = new Color(0.7f, 0.75f, 0.85f);
        smallStyle.alignment = TextAnchor.UpperLeft;
        smallStyle.wordWrap = true;
        GUI.Label(new Rect(p1X + 16f, startY + 175f, cardW - 32f, 130f),
            "• Rápido (Low-Poly): Sombras ligeras y máxima tasa de cuadros.\n\n" +
            "• FOV amplio (80°-95°) recomendado para anticipar oleadas de la horda.\n\n" +
            "• El cambio de FOV se aplica en tiempo real.", smallStyle);

        // ── PANEL 2: CONTROLES & TARGETING ──
        float p2X = startX + cardW + cardGap;
        DrawMenuPanelBox(p2X, startY, cardW, cardH, new Color(1f, 0.85f, 0.25f));
        catHeaderSt.normal.textColor = new Color(1f, 0.85f, 0.25f);
        GUI.Label(new Rect(p2X, startY + 10f, cardW, 20f), "🎮 CONTROLES & TARGETING", catHeaderSt);

        // Sensibilidad Slider
        GUI.Label(new Rect(p2X + 16f, startY + 45f, cardW - 32f, 18f), $"Sensibilidad Mouse: {settingSensitivity:0.0}x", optSt);
        settingSensitivity = GUI.HorizontalSlider(new Rect(p2X + 16f, startY + 68f, cardW - 32f, 20f), settingSensitivity, 0.5f, 5.0f);

        // Invertir Eje Y Toggle
        GUI.Label(new Rect(p2X + 16f, startY + 105f, 150f, 26f), "Invertir Eje Y:", optSt);
        Rect invRect = new Rect(p2X + cardW - 80f, startY + 102f, 64f, 24f);
        bool hoverInv = invRect.Contains(Event.current.mousePosition);
        GUI.color = settingInvertY ? new Color(0.2f, 0.85f, 0.4f) : new Color(0.2f, 0.22f, 0.28f);
        GUI.DrawTexture(invRect, whiteTex);
        GUI.color = Color.white;
        smallStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(invRect, settingInvertY ? "SÍ" : "NO", smallStyle);
        if (Event.current.type == EventType.MouseDown && hoverInv)
        {
            settingInvertY = !settingInvertY;
            Event.current.Use();
        }

        // AUTO-AIM / APUNTADO ASISTIDO TOGGLE
        GUI.Label(new Rect(p2X + 16f, startY + 145f, 150f, 26f), "Auto-Aim (Targeting):", optSt);
        Rect aimRect = new Rect(p2X + cardW - 100f, startY + 142f, 84f, 24f);
        bool hoverAim = aimRect.Contains(Event.current.mousePosition);
        GUI.color = settingAutoAim ? new Color(0.2f, 0.85f, 0.4f) : new Color(0.25f, 0.28f, 0.35f);
        GUI.DrawTexture(aimRect, whiteTex);
        GUI.color = Color.white;
        GUI.Label(aimRect, settingAutoAim ? "ACTIVADO" : "DESACT.", smallStyle);
        if (Event.current.type == EventType.MouseDown && hoverAim)
        {
            settingAutoAim = !settingAutoAim;
            Event.current.Use();
        }

        GUI.color = new Color(0.7f, 0.75f, 0.85f);
        GUI.Label(new Rect(p2X + 16f, startY + 180f, cardW - 32f, 130f),
            "• Auto-Aim: Las balas priorizan automáticamente a los enemigos dentro de un cono frontal de 40°.\n\n" +
            "• Ideal si juegas con mando o prefieres enfoque de evasión rápida al estilo Survivors.", smallStyle);

        // ── PANEL 3: AUDIO & ATAJOS ──
        float p3X = startX + (cardW + cardGap) * 2f;
        DrawMenuPanelBox(p3X, startY, cardW, cardH, new Color(1f, 0.5f, 0.2f));
        catHeaderSt.normal.textColor = new Color(1f, 0.5f, 0.2f);
        GUI.Label(new Rect(p3X, startY + 10f, cardW, 20f), "🔊 AUDIO & ATAJOS", catHeaderSt);

        // Volumen Maestro Slider
        int volPercent = Mathf.RoundToInt(settingVolume * 100f);
        GUI.Label(new Rect(p3X + 16f, startY + 45f, cardW - 32f, 18f), $"Volumen Maestro: {volPercent}%", optSt);
        float newVol = GUI.HorizontalSlider(new Rect(p3X + 16f, startY + 68f, cardW - 32f, 20f), settingVolume, 0f, 1f);
        if (Mathf.Abs(newVol - settingVolume) > 0.01f)
        {
            settingVolume = newVol;
            AudioListener.volume = settingVolume;
        }

        GUI.Label(new Rect(p3X + 16f, startY + 105f, cardW - 32f, 18f), "Atajos de Combate:", optSt);
        GUI.color = new Color(0.8f, 0.85f, 0.9f);
        GUI.Label(new Rect(p3X + 16f, startY + 128f, cardW - 32f, 180f),
            "• [ WASD ]: Desplazamiento\n" +
            "• [ Shift ]: Sprint / Correr\n" +
            "• [ Espacio ]: Salto\n" +
            "• [ Click Izq ]: Disparo continuo\n" +
            "• [ R ]: Recargar cargador\n" +
            "• [ 1, 2, 3, 4 ]: Elegir habilidad\n" +
            "• [ ESC ]: Pausar partida", smallStyle);

        // ── BARRA INFERIOR DE ACCIÓN ──
        float bBarY = startY + cardH + 18f;

        // Botón Volver
        Rect backBtn = new Rect(startX, bBarY, 260f, 44f);
        bool hoverBack = backBtn.Contains(Event.current.mousePosition);
        GUI.color = hoverBack ? new Color(0.2f, 0.25f, 0.35f, 0.95f) : new Color(0.1f, 0.12f, 0.18f, 0.9f);
        GUI.DrawTexture(backBtn, whiteTex);
        GUI.color = Color.white;
        smallStyle.alignment = TextAnchor.MiddleCenter;
        smallStyle.fontStyle = FontStyle.Bold;
        GUI.Label(backBtn, "⮌ VOLVER AL MENÚ [ ESC ]", smallStyle);
        smallStyle.fontStyle = FontStyle.Normal;

        if (Event.current.type == EventType.MouseDown && hoverBack)
        {
            currentMenuScreen = MenuSubScreen.Main;
            Event.current.Use();
        }

        // Botón Guardar y Aplicar
        Rect saveBtn = new Rect(startX + totalW - 320f, bBarY, 320f, 44f);
        bool hoverSave = saveBtn.Contains(Event.current.mousePosition);
        GUI.color = hoverSave ? new Color(0.2f, 0.85f, 0.4f, 0.98f) : new Color(0.12f, 0.55f, 0.28f, 0.9f);
        GUI.DrawTexture(saveBtn, whiteTex);
        GUI.color = Color.white;
        GUIStyle saveSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        saveSt.normal.textColor = Color.white;
        GUI.Label(saveBtn, "✔ GUARDAR Y APLICAR CAMBIOS", saveSt);

        if (Event.current.type == EventType.MouseDown && hoverSave)
        {
            ApplySettings();
            Event.current.Use();
        }

        titleStyle.fontSize = 36;
        GUI.color = Color.white;
    }

    // ── 5. SALÓN DE LA FAMA Y ESTADÍSTICAS (LEADERBOARDS & STATS) ──
    void DrawMenu_Leaderboards(GameManager gm)
    {
        float sw = MegabonkUI.Width;
        float sh = MegabonkUI.Height;
        float cx = sw / 2f;
        float cy = sh / 2f;

        DrawMenuBackground(sw, sh);

        // Header
        titleStyle.fontSize = 34;
        GUI.color = new Color(1f, 0.85f, 0.25f);
        GUI.Label(new Rect(cx - 450f, cy - 240f, 900f, 40f), "🏆 SALÓN DE LA FAMA Y ESTADÍSTICAS (LEADERBOARDS) 🏆", titleStyle);

        subtitleStyle.fontSize = 13;
        GUI.color = new Color(0.85f, 0.9f, 1f);
        GUI.Label(new Rect(cx - 450f, cy - 198f, 900f, 20f),
            "REGISTRO HISTÓRICO DE TUS EXPEDICIONES DE 20 MINUTOS Y TABLA DE RÉCORDS MUNDIALES", subtitleStyle);

        float totalW = 900f;
        float startX = cx - totalW / 2f;
        float startY = cy - 165f;
        float colH = 330f;

        // Panel Izquierdo: Estadísticas Personales (w = 340f)
        float statColW = 340f;
        DrawMenuPanelBox(startX, startY, statColW, colH, new Color(0.2f, 0.85f, 1f));

        GUIStyle cardHeaderSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        cardHeaderSt.normal.textColor = new Color(0.2f, 0.9f, 1f);
        GUI.Label(new Rect(startX, startY + 10f, statColW, 20f), "📊 TUS RÉCORDS GLOBALES", cardHeaderSt);

        int bestSec = Mathf.FloorToInt(gm.bestTimeSurvived);
        string bestTimeStr = $"{bestSec / 60:00}:{bestSec % 60:00} / 10:00";
        string rankTitle = (gm.bestTimeSurvived >= 1200f) ? "🏆 LEYENDA VIVIENTE" :
                           (gm.bestTimeSurvived >= 600f) ? "⭐ VETERANO DE HORDA" :
                           (gm.highestKills >= 100) ? "🗡 ASESINO DE ÉLITE" : "🔰 RECLUTA DE NIEBLA";

        GUIStyle statEntrySt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft
        };
        statEntrySt.normal.textColor = Color.white;

        float statItemY = startY + 45f;
        float itemH = 38f;

        DrawStatEntry(startX + 16f, statItemY + 0 * itemH, statColW - 32f, "⏱ Mejor Supervivencia:", bestTimeStr, new Color(0.3f, 0.95f, 1f));
        DrawStatEntry(startX + 16f, statItemY + 1 * itemH, statColW - 32f, "🏆 Puntuación Máxima:", gm.highestScore.ToString("N0"), new Color(1f, 0.85f, 0.2f));
        DrawStatEntry(startX + 16f, statItemY + 2 * itemH, statColW - 32f, "💀 Mayor Número de Bajas:", gm.highestKills.ToString("N0"), new Color(1f, 0.35f, 0.35f));
        DrawStatEntry(startX + 16f, statItemY + 3 * itemH, statColW - 32f, "☠ Jefes Goliat Eliminados:", gm.bossesSlain.ToString(), new Color(0.95f, 0.3f, 0.9f));
        DrawStatEntry(startX + 16f, statItemY + 4 * itemH, statColW - 32f, "🚀 Expediciones Intentadas:", gm.totalRunsAttempted.ToString(), new Color(0.4f, 0.8f, 1f));
        DrawStatEntry(startX + 16f, statItemY + 5 * itemH, statColW - 32f, "🎖 Rango de Superviviente:", rankTitle, new Color(1f, 0.9f, 0.4f));

        // Panel Derecho: Tabla de Clasificación Mundial (w = 536f)
        float boardX = startX + statColW + 24f;
        float boardW = totalW - statColW - 24f;
        DrawMenuPanelBox(boardX, startY, boardW, colH, new Color(1f, 0.85f, 0.25f));

        cardHeaderSt.normal.textColor = new Color(1f, 0.85f, 0.25f);
        GUI.Label(new Rect(boardX, startY + 10f, boardW, 20f), "🌐 TABLA GLOBAL SURVIVOR (ARCADE RANKING)", cardHeaderSt);

        // Cabecera de la tabla
        float tableY = startY + 40f;
        GUI.color = new Color(0.12f, 0.16f, 0.24f, 0.9f);
        GUI.DrawTexture(new Rect(boardX + 10f, tableY, boardW - 20f, 24f), whiteTex);
        GUI.color = Color.white;

        GUIStyle thSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        thSt.normal.textColor = new Color(0.3f, 0.9f, 1f);

        GUI.Label(new Rect(boardX + 12f, tableY + 2f, 40f, 20f), "#", thSt);
        GUI.Label(new Rect(boardX + 54f, tableY + 2f, 75f, 20f), "TIEMPO", thSt);
        GUI.Label(new Rect(boardX + 130f, tableY + 2f, 75f, 20f), "PUNTOS", thSt);
        GUI.Label(new Rect(boardX + 210f, tableY + 2f, 95f, 20f), "HÉROE", thSt);
        GUI.Label(new Rect(boardX + 310f, tableY + 2f, 105f, 20f), "ARMA", thSt);
        GUI.Label(new Rect(boardX + 420f, tableY + 2f, 85f, 20f), "ESTADO", thSt);

        // Filas arcade
        string pTime = $"{Mathf.FloorToInt(gm.bestTimeSurvived) / 60:00}:{Mathf.FloorToInt(gm.bestTimeSurvived) % 60:00}";
        string pScore = gm.highestScore.ToString("N0");
        string pHero = gm.selectedCharacter.ToString().ToUpper();
        string pWeapon = gm.selectedWeaponType.ToString().ToUpper();
        string pStatus = gm.bestTimeSurvived >= 1200f ? "VICTORIA" : (gm.highestScore > 0 ? "REGISTRO" : "--");

        string[,] boardEntries = new string[,]
        {
            { "1", "10:00", "42,850", "CALCIUM", "BALLESTA", "VICTORIA 🏆" },
            { "2", "10:00", "38,400", "CL4NK", "SUBFUSIL", "VICTORIA 🏆" },
            { "3", "10:00", "34,120", "MEGACHAD", "ESCOPETA", "VICTORIA 🏆" },
            { "4", (gm.highestScore > 0 ? pTime : "17:40"), (gm.highestScore > 0 ? pScore : "26,500"), (gm.highestScore > 0 ? pHero : "SIR OOFIE"), (gm.highestScore > 0 ? pWeapon : "REVÓLVER"), (gm.highestScore > 0 ? $"[ TÚ: {pStatus} ]" : "SOBREVIVIENTE") },
            { "5", "14:15", "18,920", "SIR OOFIE", "REVÓLVER", "DERROTA ☠" }
        };

        GUIStyle tdSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter
        };

        for (int r = 0; r < 5; r++)
        {
            float rowY = tableY + 28f + r * 42f;
            bool isPlayerRow = (r == 3 && gm.highestScore > 0);

            // Fondo de fila
            GUI.color = isPlayerRow ? new Color(0.15f, 0.35f, 0.55f, 0.95f) : ((r % 2 == 0) ? new Color(0.08f, 0.1f, 0.15f, 0.85f) : new Color(0.06f, 0.08f, 0.12f, 0.85f));
            GUI.DrawTexture(new Rect(boardX + 10f, rowY, boardW - 20f, 38f), whiteTex);

            Color textColor = isPlayerRow ? new Color(1f, 0.9f, 0.3f) : Color.white;
            tdSt.normal.textColor = textColor;

            GUI.Label(new Rect(boardX + 12f, rowY + 9f, 40f, 20f), boardEntries[r, 0], tdSt);
            GUI.Label(new Rect(boardX + 54f, rowY + 9f, 75f, 20f), boardEntries[r, 1], tdSt);
            GUI.Label(new Rect(boardX + 130f, rowY + 9f, 75f, 20f), boardEntries[r, 2], tdSt);
            GUI.Label(new Rect(boardX + 210f, rowY + 9f, 95f, 20f), boardEntries[r, 3], tdSt);
            GUI.Label(new Rect(boardX + 310f, rowY + 9f, 105f, 20f), boardEntries[r, 4], tdSt);

            Color statusCol = boardEntries[r, 5].Contains("VICTORIA") ? new Color(0.3f, 1f, 0.5f) :
                              (boardEntries[r, 5].Contains("TÚ") ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.4f, 0.4f));
            tdSt.normal.textColor = statusCol;
            GUI.Label(new Rect(boardX + 420f, rowY + 9f, 85f, 20f), boardEntries[r, 5], tdSt);
        }

        // Botón Volver
        Rect backBtn = new Rect(startX, startY + colH + 18f, 280f, 44f);
        bool hoverBack = backBtn.Contains(Event.current.mousePosition);
        GUI.color = hoverBack ? new Color(0.2f, 0.25f, 0.35f, 0.95f) : new Color(0.1f, 0.12f, 0.18f, 0.9f);
        GUI.DrawTexture(backBtn, whiteTex);
        GUI.color = Color.white;
        smallStyle.alignment = TextAnchor.MiddleCenter;
        smallStyle.fontStyle = FontStyle.Bold;
        GUI.Label(backBtn, "⮌ VOLVER AL MENÚ [ ESC ]", smallStyle);
        smallStyle.fontStyle = FontStyle.Normal;

        if (Event.current.type == EventType.MouseDown && hoverBack)
        {
            currentMenuScreen = MenuSubScreen.Main;
            Event.current.Use();
        }

        titleStyle.fontSize = 36;
        GUI.color = Color.white;
    }

    void DrawStatEntry(float x, float y, float w, string label, string val, Color valColor)
    {
        GUIStyle lSt = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleLeft };
        lSt.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
        GUI.Label(new Rect(x, y, w - 120f, 20f), label, lSt);

        GUIStyle vSt = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
        vSt.normal.textColor = valColor;
        GUI.Label(new Rect(x + w - 130f, y, 130f, 20f), val, vSt);
    }

    void DrawMenuPanelBox(float x, float y, float w, float h, Color accent)
    {
        MegabonkUI.Panel(new Rect(x, y, w, h), accent);
    }

    void DrawMenuNavButton(Rect r, string title, string sub, Color accent, bool highlighted, System.Action onClick)
    {
        bool hover = r.Contains(Event.current.mousePosition);

        // Fondo
        GUI.color = hover
            ? new Color(0.12f, 0.18f, 0.28f, 0.98f)
            : (highlighted ? new Color(0.09f, 0.14f, 0.22f, 0.94f) : new Color(0.06f, 0.08f, 0.12f, 0.9f));
        GUI.DrawTexture(r, whiteTex);

        // Barra indicadora lateral izquierda
        GUI.color = hover ? accent : (highlighted ? new Color(accent.r, accent.g, accent.b, 0.8f) : new Color(0.2f, 0.3f, 0.4f, 0.4f));
        GUI.DrawTexture(new Rect(r.x, r.y, 4f, r.height), whiteTex);

        // Bordes
        Color bCol = hover ? accent : (highlighted ? new Color(accent.r * 0.7f, accent.g * 0.7f, accent.b * 0.7f, 0.6f) : new Color(0.15f, 0.2f, 0.28f, 0.5f));
        GUI.color = bCol;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1f), whiteTex);
        GUI.DrawTexture(new Rect(r.x, r.yMax - 1f, r.width, 1f), whiteTex);
        GUI.DrawTexture(new Rect(r.xMax - 1f, r.y, 1f, r.height), whiteTex);

        // Títulos
        GUIStyle tSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        tSt.normal.textColor = hover ? Color.white : (highlighted ? new Color(0.35f, 0.92f, 1f) : new Color(0.9f, 0.92f, 0.96f));
        GUI.color = Color.white;
        GUI.Label(new Rect(r.x + 14f, r.y + 7f, r.width - 24f, 20f), title, tSt);

        GUIStyle sSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleLeft
        };
        sSt.normal.textColor = hover ? new Color(0.85f, 0.95f, 1f) : new Color(0.65f, 0.72f, 0.82f);
        GUI.Label(new Rect(r.x + 14f, r.y + 27f, r.width - 24f, 18f), sub, sSt);

        if (Event.current.type == EventType.MouseDown && hover)
        {
            onClick?.Invoke();
            Event.current.Use();
        }
    }

    void DrawCharacterSelectCard(Rect r, CharacterInfo info, bool selected, System.Action onClick)
    {
        bool hover = r.Contains(Event.current.mousePosition);

        // Fondo
        if (selected)
            GUI.color = new Color(info.color.r * 0.16f, info.color.g * 0.16f, info.color.b * 0.16f, 0.96f);
        else if (hover)
            GUI.color = new Color(0.1f, 0.14f, 0.2f, 0.95f);
        else
            GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.9f);
        GUI.DrawTexture(r, whiteTex);

        // Borde
        Color bCol = selected ? info.color : (hover ? new Color(0.4f, 0.85f, 1f) : new Color(0.18f, 0.22f, 0.3f));
        GUI.color = bCol;
        float bThick = selected ? 2.5f : 1f;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, bThick), whiteTex);
        GUI.DrawTexture(new Rect(r.x, r.yMax - bThick, r.width, bThick), whiteTex);
        GUI.DrawTexture(new Rect(r.x, r.y, bThick, r.height), whiteTex);
        GUI.DrawTexture(new Rect(r.xMax - bThick, r.y, bThick, r.height), whiteTex);

        // Badge superior si está seleccionado
        if (selected)
        {
            GUI.color = info.color;
            GUI.DrawTexture(new Rect(r.xMax - 70f, r.y, 70f, 16f), whiteTex);
            GUIStyle selSt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            selSt.normal.textColor = Color.black;
            GUI.Label(new Rect(r.xMax - 70f, r.y, 70f, 16f), "ELEGIDO", selSt);
        }

        // Título del personaje
        GUIStyle nameSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        nameSt.normal.textColor = selected ? info.color : Color.white;
        GUI.color = Color.white;
        GUI.Label(new Rect(r.x + 8f, r.y + 6f, r.width - 16f, 20f), $"{info.icon} {info.name}", nameSt);

        GUIStyle titleSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleLeft
        };
        titleSt.normal.textColor = new Color(0.7f, 0.8f, 0.9f);
        GUI.Label(new Rect(r.x + 8f, r.y + 24f, r.width - 16f, 16f), info.title, titleSt);

        GUIStyle passSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        passSt.normal.textColor = new Color(0.85f, 0.88f, 0.94f);
        GUI.Label(new Rect(r.x + 8f, r.y + 42f, r.width - 16f, r.height - 46f), info.passive, passSt);

        if (Event.current.type == EventType.MouseDown && hover)
        {
            onClick?.Invoke();
            Event.current.Use();
        }
    }

    void DrawWeaponButton(Rect r, string name, string stats, WeaponType type, GameManager gm)
    {
        bool isSelected = (gm.selectedWeaponType == type);
        bool hover = r.Contains(Event.current.mousePosition);

        // Fondo
        if (isSelected)
            GUI.color = new Color(0.12f, 0.28f, 0.45f, 0.95f);
        else if (hover)
            GUI.color = new Color(0.1f, 0.15f, 0.22f, 0.95f);
        else
            GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.88f);
        GUI.DrawTexture(r, whiteTex);

        // Borde
        Color bCol = isSelected ? new Color(1f, 0.8f, 0.2f) : (hover ? new Color(0.3f, 0.85f, 1f) : new Color(0.2f, 0.25f, 0.35f));
        GUI.color = bCol;
        float bThick = isSelected ? 2.5f : 1f;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, bThick), whiteTex);
        GUI.DrawTexture(new Rect(r.x, r.yMax - bThick, r.width, bThick), whiteTex);
        GUI.DrawTexture(new Rect(r.x, r.y, bThick, r.height), whiteTex);
        GUI.DrawTexture(new Rect(r.xMax - bThick, r.y, bThick, r.height), whiteTex);

        // Badge si está equipada
        if (isSelected)
        {
            GUI.color = new Color(1f, 0.8f, 0.2f);
            GUI.DrawTexture(new Rect(r.xMax - 66f, r.y, 66f, 15f), whiteTex);
            GUIStyle eqSt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            eqSt.normal.textColor = Color.black;
            GUI.Label(new Rect(r.xMax - 66f, r.y, 66f, 15f), "EQUIPADA", eqSt);
        }

        // Texto
        GUIStyle titleSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        titleSt.normal.textColor = isSelected ? new Color(1f, 0.85f, 0.2f) : Color.white;
        GUI.color = Color.white;
        GUI.Label(new Rect(r.x + 8f, r.y + 5f, r.width - 16f, 20f), name, titleSt);

        GUIStyle statSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        statSt.normal.textColor = new Color(0.8f, 0.88f, 0.95f);
        GUI.Label(new Rect(r.x + 8f, r.y + 26f, r.width - 16f, r.height - 30f), stats, statSt);

        if (Event.current.type == EventType.MouseDown && hover)
        {
            gm.selectedWeaponType = type;
            Event.current.Use();
        }
    }

    void DrawPlayStyleButton(Rect r, string name, string desc, GameManager.PlayStyle style, GameManager gm)
    {
        bool isSelected = (gm.selectedPlayStyle == style);
        bool hover = r.Contains(Event.current.mousePosition);

        Color accentColor = style == GameManager.PlayStyle.FuegoInfernal ? new Color(1f, 0.45f, 0.1f) :
                            style == GameManager.PlayStyle.RafagaVeloz ? new Color(0.2f, 0.85f, 1f) :
                            style == GameManager.PlayStyle.TitanBlindado ? new Color(0.3f, 1f, 0.4f) :
                            new Color(1f, 0.85f, 0.25f);

        if (isSelected)
            GUI.color = new Color(accentColor.r * 0.25f, accentColor.g * 0.25f, accentColor.b * 0.25f, 0.95f);
        else if (hover)
            GUI.color = new Color(0.1f, 0.14f, 0.2f, 0.95f);
        else
            GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.9f);
        GUI.DrawTexture(r, whiteTex);

        // Borde
        float bThick = isSelected ? 2f : 1f;
        GUI.color = isSelected ? accentColor : (hover ? Color.white : new Color(0.25f, 0.35f, 0.45f, 0.5f));
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, bThick), whiteTex);
        GUI.DrawTexture(new Rect(r.x, r.yMax - bThick, r.width, bThick), whiteTex);
        GUI.DrawTexture(new Rect(r.x, r.y, bThick, r.height), whiteTex);
        GUI.DrawTexture(new Rect(r.xMax - bThick, r.y, bThick, r.height), whiteTex);

        // Badge si está activo
        if (isSelected)
        {
            GUI.color = accentColor;
            GUI.DrawTexture(new Rect(r.xMax - 54f, r.y, 54f, 14f), whiteTex);
            GUIStyle actSt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 8,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            actSt.normal.textColor = Color.black;
            GUI.Label(new Rect(r.xMax - 54f, r.y, 54f, 14f), "ACTIVO", actSt);
        }

        // Título
        GUIStyle titleSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        titleSt.normal.textColor = isSelected ? accentColor : (hover ? Color.white : new Color(0.9f, 0.94f, 0.98f));
        GUI.Label(new Rect(r.x + 8f, r.y + 4f, r.width - 64f, 18f), name, titleSt);

        // Descripción
        GUIStyle descSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        descSt.normal.textColor = new Color(0.8f, 0.86f, 0.92f);
        GUI.Label(new Rect(r.x + 8f, r.y + 22f, r.width - 16f, r.height - 24f), desc, descSt);

        if (Event.current.type == EventType.MouseDown && hover)
        {
            gm.selectedPlayStyle = style;
            Event.current.Use();
        }
    }

    void DrawMenuCard(float x, float y, float w, float h, string title, Color accentColor, string body)
    {
        // Fondo de tarjeta
        GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
        GUI.DrawTexture(new Rect(x, y, w, h), whiteTex);

        // Borde superior acentuado
        GUI.color = accentColor;
        GUI.DrawTexture(new Rect(x, y, w, 3f), whiteTex);

        // Bordes finos
        GUI.color = new Color(accentColor.r * 0.4f, accentColor.g * 0.4f, accentColor.b * 0.4f, 0.6f);
        GUI.DrawTexture(new Rect(x, y, 1f, h), whiteTex);
        GUI.DrawTexture(new Rect(x + w - 1f, y, 1f, h), whiteTex);
        GUI.DrawTexture(new Rect(x, y + h - 1f, w, 1f), whiteTex);

        // Título de la tarjeta
        GUIStyle titleSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleSt.normal.textColor = accentColor;
        GUI.color = Color.white;
        GUI.Label(new Rect(x, y + 8f, w, 20f), title, titleSt);

        // Línea divisoria
        GUI.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.3f);
        GUI.DrawTexture(new Rect(x + 15f, y + 28f, w - 30f, 1f), whiteTex);

        // Texto del cuerpo
        GUIStyle bodySt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        bodySt.normal.textColor = new Color(0.85f, 0.88f, 0.92f);
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 12f, y + 34f, w - 24f, h - 38f), body, bodySt);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ═══════════════════════════════════════════════════════
    // PANTALLA DE GAME OVER
    // ═══════════════════════════════════════════════════════
    void DrawGameOver(GameManager gm)
    {
        float cx = MegabonkUI.Width / 2f;
        float cy = MegabonkUI.Height / 2f;

        // Overlay oscuro
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(new Rect(0, 0, MegabonkUI.Width, MegabonkUI.Height), whiteTex);

        // Título
        GUI.color = new Color(1f, 0.2f, 0.25f);
        titleStyle.fontSize = 48;
        GUI.Label(new Rect(cx - 300f, cy - 135f, 600f, 60f), "☠ GAME OVER ☠", titleStyle);
        titleStyle.fontSize = 36;

        // Estadísticas
        GUI.color = Color.white;
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(cx - 200f, cy - 55f, 400f, 28f), "Puntuación: " + gm.score, subtitleStyle);
        GUI.Label(new Rect(cx - 200f, cy - 22f, 400f, 28f), "Eliminaciones: " + gm.kills, subtitleStyle);
        GUI.Label(new Rect(cx - 200f, cy + 12f, 400f, 28f), "Oleadas alcanzadas: " + gm.wavesReached, subtitleStyle);

        // Botón interactivo para volver al Menú Principal
        Rect restartBtn = new Rect(cx - 150f, cy + 65f, 300f, 50f);
        bool hoverRestart = restartBtn.Contains(Event.current.mousePosition);
        GUI.color = hoverRestart ? new Color(0.2f, 0.7f, 0.95f, 0.98f) : new Color(0.12f, 0.45f, 0.75f, 0.9f);
        GUI.DrawTexture(restartBtn, whiteTex);

        GUIStyle rStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        rStyle.normal.textColor = Color.white;
        GUI.Label(restartBtn, "⮌ VOLVER AL MENÚ [ R ]", rStyle);

        GUI.color = new Color(0.85f, 0.85f, 0.85f, Mathf.PingPong(Time.unscaledTime * 2f, 1f));
        GUI.Label(new Rect(cx - 200f, cy + 125f, 400f, 25f),
            "Presiona [ R ] o haz clic para volver al Menú", subtitleStyle);

        if ((Event.current.type == EventType.MouseDown && hoverRestart) || Input.GetKeyDown(KeyCode.R))
        {
            gm.ReturnToMainMenu();
            Event.current.Use();
        }

        GUI.color = Color.white;
    }

    // ═══════════════════════════════════════════════════════
    // PANTALLA DE VICTORIA (20 MINUTOS COMPLETADOS)
    // ═══════════════════════════════════════════════════════
    void DrawVictory(GameManager gm)
    {
        float cx = MegabonkUI.Width / 2f;
        float cy = MegabonkUI.Height / 2f;

        // Overlay oscuro con tinte dorado/victoria
        GUI.color = new Color(0.04f, 0.05f, 0.03f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, MegabonkUI.Width, MegabonkUI.Height), whiteTex);

        // Título de Victoria
        GUI.color = new Color(1f, 0.85f, 0.2f);
        titleStyle.fontSize = 46;
        GUI.Label(new Rect(cx - 350f, cy - 140f, 700f, 60f), "🏆 ¡VICTORIA! 20 MINUTOS 🏆", titleStyle);
        titleStyle.fontSize = 36;

        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        GUI.color = new Color(0.9f, 0.95f, 1f);
        GUI.Label(new Rect(cx - 300f, cy - 75f, 600f, 26f), "¡HAS SOBREVIVIDO A TODAS LAS OLEADAS DE LA NOCHE!", subtitleStyle);

        GUI.color = Color.white;
        XPSystem xp = XPSystem.Instance ?? Object.FindAnyObjectByType<XPSystem>();
        int lvl = xp != null ? xp.level : 1;
        GUI.Label(new Rect(cx - 200f, cy - 35f, 400f, 26f), "Tiempo Sobrevivido: 10:00 (Completo)", subtitleStyle);
        GUI.Label(new Rect(cx - 200f, cy - 8f, 400f, 26f), $"Puntuación Final: {gm.score}", subtitleStyle);
        GUI.Label(new Rect(cx - 250f, cy + 18f, 500f, 26f), $"Eliminaciones: {gm.kills}  |  Nivel Alcanzado: {lvl}  |  Arma: {gm.selectedWeaponType}", subtitleStyle);

        // Botón interactivo para volver al Menú Principal
        Rect restartBtn = new Rect(cx - 150f, cy + 65f, 300f, 50f);
        bool hoverRestart = restartBtn.Contains(Event.current.mousePosition);
        GUI.color = hoverRestart ? new Color(0.2f, 0.85f, 0.4f, 0.98f) : new Color(0.12f, 0.55f, 0.25f, 0.9f);
        GUI.DrawTexture(restartBtn, whiteTex);

        GUIStyle rStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        rStyle.normal.textColor = Color.white;
        GUI.Label(restartBtn, "⮌ VOLVER AL MENÚ [ R ]", rStyle);

        GUI.color = new Color(0.85f, 0.85f, 0.85f, Mathf.PingPong(Time.unscaledTime * 2f, 1f));
        GUI.Label(new Rect(cx - 200f, cy + 125f, 400f, 25f),
            "Presiona [ R ] o haz clic para volver al Menú", subtitleStyle);

        if ((Event.current.type == EventType.MouseDown && hoverRestart) || Input.GetKeyDown(KeyCode.R))
        {
            gm.ReturnToMainMenu();
            Event.current.Use();
        }

        GUI.color = Color.white;
    }

    // ═══════════════════════════════════════════════════════
    // PANTALLA DE HABILIDADES Y PAUSA (TAB / ESC)
    // ═══════════════════════════════════════════════════════
    void DrawPause(GameManager gm)
    {
        float sw = MegabonkUI.Width;
        float sh = MegabonkUI.Height;
        float cx = sw / 2f;
        float cy = sh / 2f;

        // Fondo oscuro traslúcido para ver el juego congelado detrás
        GUI.color = new Color(0.02f, 0.03f, 0.06f, 0.90f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), whiteTex);

        // Rejilla cibernética sutil de fondo
        GUI.color = new Color(0.1f, 0.8f, 1f, 0.04f);
        for (float ly = 0; ly < sh; ly += 36f)
        {
            GUI.DrawTexture(new Rect(0, ly, sw, 1f), whiteTex);
        }
        GUI.color = Color.white;

        // ── ENCABEZADO ──
        float pulse = Mathf.PingPong(Time.unscaledTime * 2f, 1f);
        titleStyle.fontSize = 32;
        GUI.color = new Color(0.2f, 0.9f, 1f);
        GUI.Label(new Rect(cx - 450f, cy - 248f, 900f, 38f), "📜 LIBRO DE HABILIDADES Y ESTADO (PAUSA)", titleStyle);

        subtitleStyle.fontSize = 13;
        GUI.color = new Color(0.8f + pulse * 0.2f, 0.9f, 1f);
        GUI.Label(new Rect(cx - 450f, cy - 212f, 900f, 20f),
            "[ JUEGO EN PAUSA ] • Presiona [ TAB ] o [ ESC ] para reanudar el combate", subtitleStyle);

        // Botón rápido cerrar [ ✕ REANUDAR ] en la esquina superior derecha
        float totalW = Mathf.Clamp(sw * 0.9f, 850f, 980f);
        float startX = cx - totalW / 2f;

        Rect closeBtnRect = new Rect(startX + totalW - 170f, cy - 245f, 170f, 32f);
        bool hoverClose = closeBtnRect.Contains(Event.current.mousePosition);
        GUI.color = hoverClose ? new Color(0.2f, 0.85f, 0.4f, 0.98f) : new Color(0.12f, 0.5f, 0.25f, 0.9f);
        GUI.DrawTexture(closeBtnRect, whiteTex);
        GUI.color = Color.white;
        smallStyle.alignment = TextAnchor.MiddleCenter;
        smallStyle.fontStyle = FontStyle.Bold;
        GUI.Label(closeBtnRect, "✕ REANUDAR [TAB]", smallStyle);
        smallStyle.fontStyle = FontStyle.Normal;

        if (Event.current.type == EventType.MouseDown && hoverClose)
        {
            gm.Resume();
            Event.current.Use();
        }

        // ── BANNER RESUMEN DE LA EXPEDICIÓN (SUPERVIVIENTE, ARMA Y TIEMPO) ──
        float ribbonY = cy - 188f;
        float ribbonH = 46f;
        GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
        GUI.DrawTexture(new Rect(startX, ribbonY, totalW, ribbonH), whiteTex);

        // Borde dorado/cian del banner
        GUI.color = new Color(0.2f, 0.85f, 1f, 0.7f);
        GUI.DrawTexture(new Rect(startX, ribbonY, totalW, 1.5f), whiteTex);
        GUI.DrawTexture(new Rect(startX, ribbonY + ribbonH - 1.5f, totalW, 1.5f), whiteTex);
        GUI.DrawTexture(new Rect(startX, ribbonY, 1.5f, ribbonH), whiteTex);
        GUI.DrawTexture(new Rect(startX + totalW - 1.5f, ribbonY, 1.5f, ribbonH), whiteTex);

        CharacterInfo hero = Characters[(int)gm.selectedCharacter];
        XPSystem xp = XPSystem.Instance ?? Object.FindAnyObjectByType<XPSystem>();
        int lvl = xp != null ? xp.level : 1;
        int totalSec = Mathf.FloorToInt(gm.gameTime);
        string timerStr = $"{totalSec / 60:00}:{totalSec % 60:00}";
        int hp = playerHealth != null ? playerHealth.currentHealth : 100;
        int maxHp = playerHealth != null ? playerHealth.maxHealth : 100;
        int wave = spawner != null ? spawner.currentWave : 1;
        string wName = playerShooting != null && playerShooting.currentWeapon != null ? playerShooting.currentWeapon.name.ToUpper() : gm.selectedWeaponType.ToString().ToUpper();

        GUIStyle ribbonSt = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleLeft };
        ribbonSt.normal.textColor = Color.white;

        GUI.color = hero.color;
        GUI.DrawTexture(new Rect(startX + 12f, ribbonY + 8f, 4f, ribbonH - 16f), whiteTex);
        GUI.color = Color.white;

        // Fila 1 del ribbon
        GUIStyle bldSt = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        bldSt.normal.textColor = hero.color;
        GUI.Label(new Rect(startX + 22f, ribbonY + 4f, 320f, 18f), $"{hero.icon} {hero.name} ({hero.title})", bldSt);

        bldSt.normal.textColor = new Color(0.35f, 0.92f, 1f);
        GUI.Label(new Rect(startX + 350f, ribbonY + 4f, 250f, 18f), $"🔫 {wName}", bldSt);

        bldSt.normal.textColor = new Color(1f, 0.85f, 0.25f);
        GUI.Label(new Rect(startX + 600f, ribbonY + 4f, 150f, 18f), $"⏱ {timerStr} / 10:00", bldSt);

        bldSt.normal.textColor = (hp > maxHp * 0.4f) ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);
        GUI.Label(new Rect(startX + 750f, ribbonY + 4f, 180f, 18f), $"❤ {hp} / {maxHp} HP", bldSt);

        // Fila 2 del ribbon
        ribbonSt.normal.textColor = new Color(0.8f, 0.85f, 0.9f);
        GUI.Label(new Rect(startX + 22f, ribbonY + 24f, 320f, 16f), hero.passive, ribbonSt);
        GUI.Label(new Rect(startX + 350f, ribbonY + 24f, 240f, 16f), $"★ Nivel: {lvl}  |  🌊 Oleada: {wave}", ribbonSt);
        GUI.Label(new Rect(startX + 600f, ribbonY + 24f, 150f, 16f), $"💀 Bajas: {gm.kills}", ribbonSt);
        GUI.Label(new Rect(startX + 750f, ribbonY + 24f, 180f, 16f), $"🏆 Puntos: {gm.score}", ribbonSt);

        // ── DOS COLUMNAS PRINCIPALES (HABILIDADES A LA IZQUIERDA, ATRIBUTOS A LA DERECHA) ──
        float bodyY = ribbonY + ribbonH + 12f;
        float bodyH = 340f;
        float colSpacing = 16f;
        float leftColW = totalW * 0.58f;
        float rightColW = totalW - leftColW - colSpacing;

        // ═════════════════════════════════════════════════════════════
        // COLUMNA IZQUIERDA: HABILIDADES Y MEJORAS ADQUIRIDAS (INVENTARIO)
        // ═════════════════════════════════════════════════════════════
        float leftX = startX;
        DrawMenuPanelBox(leftX, bodyY, leftColW, bodyH, new Color(0.2f, 0.85f, 1f));

        int upgradeCount = (xp != null && xp.acquiredUpgradeIds != null) ? xp.acquiredUpgradeIds.Count : 0;
        GUIStyle colHeaderSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        colHeaderSt.normal.textColor = new Color(0.2f, 0.9f, 1f);
        GUI.Label(new Rect(leftX + 16f, bodyY + 8f, leftColW - 32f, 20f),
            $"🎒 HABILIDADES Y MEJORAS ACTIVAS ({upgradeCount})", colHeaderSt);

        // Lista deslizable de habilidades
        Rect scrollArea = new Rect(leftX + 4f, bodyY + 34f, leftColW - 8f, bodyH - 40f);

        System.Collections.Generic.List<string> upgradeList = new System.Collections.Generic.List<string>();
        if (xp != null && xp.acquiredUpgradeIds != null)
        {
            foreach (var id in xp.acquiredUpgradeIds) upgradeList.Add(id);
        }

        if (upgradeList.Count == 0)
        {
            // Mensaje informativo si no tiene mejoras todavía
            GUIStyle emptySt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            emptySt.normal.textColor = new Color(0.7f, 0.8f, 0.9f);
            GUI.Label(new Rect(leftX + 30f, bodyY + 110f, leftColW - 60f, 100f),
                "🌱 Aún no has adquirido mejoras adicionales en esta expedición.\n\n" +
                "¡Elimina enemigos, absorbe las esmeraldas de experiencia (XP) para subir de nivel y desbloquear cartas de habilidad!", emptySt);
        }
        else
        {
            float itemH = 62f;
            float itemGap = 6f;
            float totalScrollH = upgradeList.Count * (itemH + itemGap) + 10f;
            Rect contentArea = new Rect(0, 0, leftColW - 28f, totalScrollH);

            skillsOverviewScrollPos = GUI.BeginScrollView(scrollArea, skillsOverviewScrollPos, contentArea);

            GUIStyle upTitleSt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            GUIStyle upCatSt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft
            };
            upCatSt.normal.textColor = new Color(0.7f, 0.8f, 0.9f);

            GUIStyle upDescSt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };
            upDescSt.normal.textColor = new Color(0.85f, 0.88f, 0.95f);

            for (int i = 0; i < upgradeList.Count; i++)
            {
                string id = upgradeList[i];
                var def = UpgradeCatalog.Get(id);
                if (def == null) continue;

                float iy = i * (itemH + itemGap) + 4f;
                float iw = contentArea.width;

                // Fondo de la carta
                GUI.color = new Color(0.08f, 0.11f, 0.16f, 0.92f);
                GUI.DrawTexture(new Rect(4f, iy, iw, itemH), whiteTex);

                // Borde izquierdo con color temático
                GUI.color = def.color;
                GUI.DrawTexture(new Rect(4f, iy, 4f, itemH), whiteTex);

                // Bordes tenues
                GUI.color = new Color(def.color.r * 0.4f, def.color.g * 0.4f, def.color.b * 0.4f, 0.4f);
                GUI.DrawTexture(new Rect(4f, iy, iw, 1f), whiteTex);
                GUI.DrawTexture(new Rect(4f, iy + itemH - 1f, iw, 1f), whiteTex);
                GUI.DrawTexture(new Rect(4f + iw - 1f, iy, 1f, itemH), whiteTex);

                // Título e Icono
                upTitleSt.normal.textColor = def.color;
                GUI.color = Color.white;
                string typeBadge = def.type == UpgradeType.BossTome ? "[TOMO DE JEFE]" :
                                   (def.type == UpgradeType.Synergy ? "[SINERGIA]" : $"[Nivel {def.tier}]");
                GUI.Label(new Rect(14f, iy + 4f, iw - 120f, 18f), $"{def.icon} {def.name.ToUpper()}  {typeBadge}", upTitleSt);

                // Categoría
                GUI.Label(new Rect(14f, iy + 20f, iw - 120f, 14f), $"Categoría: {def.category}", upCatSt);

                // Temporizador / Cooldown en tiempo real (si aplica)
                string timerText = xp != null ? xp.GetAbilityTimerText(id) : null;
                if (!string.IsNullOrEmpty(timerText))
                {
                    GUIStyle timerBadgeSt = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 11,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleRight
                    };
                    timerBadgeSt.normal.textColor = timerText.Contains("⏱") || timerText.Contains("⟳") ? new Color(1f, 0.85f, 0.25f) : new Color(0.35f, 0.95f, 1f);
                    GUI.Label(new Rect(iw - 110f, iy + 4f, 100f, 18f), timerText, timerBadgeSt);
                }

                // Descripción
                GUI.Label(new Rect(14f, iy + 34f, iw - 20f, 26f), def.description, upDescSt);
            }

            GUI.EndScrollView();
        }

        // ═════════════════════════════════════════════════════════════
        // COLUMNA DERECHA: ATRIBUTOS TOTALES Y SINERGIAS ACTIVAS
        // ═════════════════════════════════════════════════════════════
        float rightX = leftX + leftColW + colSpacing;
        DrawMenuPanelBox(rightX, bodyY, rightColW, bodyH, new Color(1f, 0.85f, 0.25f));

        colHeaderSt.normal.textColor = new Color(1f, 0.85f, 0.25f);
        GUI.Label(new Rect(rightX + 16f, bodyY + 8f, rightColW - 32f, 20f), "📊 ATRIBUTOS TOTALES EN COMBATE", colHeaderSt);

        float statY = bodyY + 34f;
        float sRowH = 26f;

        // Cálculos reales de atributos
        float dmgMult = xp != null ? xp.damageMultiplier : 1f;
        float fireMult = xp != null ? xp.fireRateMultiplier : 1f;
        float spdMult = xp != null ? xp.speedMultiplier : 1f;
        int pierce = xp != null ? xp.bonusPierce : 0;
        int bounce = xp != null ? xp.bonusBounce : 0;
        float dodge = xp != null ? xp.dodgeChance * 100f : 0f;
        float magnet = xp != null ? xp.magnetMultiplier * 100f : 100f;
        bool shieldReady = xp != null && xp.holyShieldActive;
        float shieldCd = xp != null ? xp.holyShieldCooldown : 0f;

        DrawStatEntry(rightX + 16f, statY + 0 * sRowH, rightColW - 32f, "• Multiplicador de Daño:", $"{Mathf.RoundToInt(dmgMult * 100f)}%", new Color(1f, 0.45f, 0.35f));
        DrawStatEntry(rightX + 16f, statY + 1 * sRowH, rightColW - 32f, "• Cadencia de Fuego:", $"{Mathf.RoundToInt(fireMult * 100f)}%", new Color(1f, 0.75f, 0.2f));
        DrawStatEntry(rightX + 16f, statY + 2 * sRowH, rightColW - 32f, "• Velocidad de Movimiento:", $"{Mathf.RoundToInt(spdMult * 100f)}%", new Color(0.3f, 1f, 0.5f));
        DrawStatEntry(rightX + 16f, statY + 3 * sRowH, rightColW - 32f, "• Perforación (Pierce):", $"+{pierce} enemigos", new Color(0.35f, 0.9f, 1f));
        DrawStatEntry(rightX + 16f, statY + 4 * sRowH, rightColW - 32f, "• Rebote de Balas:", $"+{bounce} rebotes", new Color(0.6f, 0.85f, 1f));
        DrawStatEntry(rightX + 16f, statY + 5 * sRowH, rightColW - 32f, "• Probabilidad de Esquive:", $"{Mathf.RoundToInt(dodge)}%", new Color(0.4f, 1f, 0.8f));
        DrawStatEntry(rightX + 16f, statY + 6 * sRowH, rightColW - 32f, "• Radio de Imán de XP:", $"{Mathf.RoundToInt(magnet)}%", new Color(0.4f, 0.85f, 1f));

        string shieldStatus = shieldReady ? "🛡 ACTIVO (1 Golpe)" : (shieldCd > 0f ? $"⏳ Regenerando ({Mathf.CeilToInt(shieldCd)}s)" : "Inactivo");
        Color shieldCol = shieldReady ? new Color(0.3f, 1f, 0.5f) : (shieldCd > 0f ? new Color(1f, 0.85f, 0.2f) : new Color(0.6f, 0.7f, 0.8f));
        DrawStatEntry(rightX + 16f, statY + 7 * sRowH, rightColW - 32f, "• Escudo Sagrado:", shieldStatus, shieldCol);

        // Sub-caja de Sinergias Activas abajo a la derecha
        float synBoxY = statY + 8 * sRowH + 6f;
        float synBoxH = bodyY + bodyH - synBoxY - 10f;
        GUI.color = new Color(0.08f, 0.1f, 0.15f, 0.9f);
        GUI.DrawTexture(new Rect(rightX + 12f, synBoxY, rightColW - 24f, synBoxH), whiteTex);
        GUI.color = new Color(1f, 0.8f, 0.2f, 0.6f);
        GUI.DrawTexture(new Rect(rightX + 12f, synBoxY, rightColW - 24f, 1f), whiteTex);
        GUI.color = Color.white;

        GUIStyle synHeadSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        synHeadSt.normal.textColor = new Color(1f, 0.85f, 0.25f);
        GUI.Label(new Rect(rightX + 20f, synBoxY + 4f, rightColW - 40f, 16f), "⚡ SINERGIAS SECRETAS DESATADAS:", synHeadSt);

        // Listar sinergias activas
        System.Collections.Generic.List<UpgradeDefinition> activeSyns = new System.Collections.Generic.List<UpgradeDefinition>();
        if (xp != null && xp.acquiredUpgradeIds != null)
        {
            foreach (var id in xp.acquiredUpgradeIds)
            {
                var def = UpgradeCatalog.Get(id);
                if (def != null && def.type == UpgradeType.Synergy)
                    activeSyns.Add(def);
            }
        }

        GUIStyle synTextSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };

        if (activeSyns.Count == 0)
        {
            synTextSt.normal.textColor = new Color(0.7f, 0.75f, 0.85f);
            GUI.Label(new Rect(rightX + 20f, synBoxY + 22f, rightColW - 40f, synBoxH - 26f),
                "Ninguna sinergia completada aún.\n¡Combina 2 ramas afines (ej. Criomancia + Piromancia, o Fuego Rápido + Rayo) para forjar sinergias!", synTextSt);
        }
        else
        {
            string synSummary = "";
            foreach (var syn in activeSyns)
            {
                synSummary += $"• {syn.icon} {syn.name}: {syn.description}\n";
            }
            synTextSt.normal.textColor = new Color(0.9f, 0.95f, 1f);
            GUI.Label(new Rect(rightX + 20f, synBoxY + 22f, rightColW - 40f, synBoxH - 26f), synSummary, synTextSt);
        }

        // ── BARRA INFERIOR DE ACCIÓN ──
        float bottomBarY = bodyY + bodyH + 14f;

        // Botón Reanudar Combate
        Rect resumeBtnRect = new Rect(startX, bottomBarY, 340f, 44f);
        bool hoverResume = resumeBtnRect.Contains(Event.current.mousePosition);
        GUI.color = hoverResume ? new Color(0.2f, 0.85f, 0.4f, 0.98f) : new Color(0.12f, 0.6f, 0.3f, 0.9f);
        GUI.DrawTexture(resumeBtnRect, whiteTex);

        // Borde
        GUI.color = hoverResume ? Color.white : new Color(0.3f, 1f, 0.5f);
        GUI.DrawTexture(new Rect(resumeBtnRect.x, resumeBtnRect.y, resumeBtnRect.width, 2f), whiteTex);
        GUI.DrawTexture(new Rect(resumeBtnRect.x, resumeBtnRect.yMax - 2f, resumeBtnRect.width, 2f), whiteTex);
        GUI.DrawTexture(new Rect(resumeBtnRect.x, resumeBtnRect.y, 2f, resumeBtnRect.height), whiteTex);
        GUI.DrawTexture(new Rect(resumeBtnRect.xMax - 2f, resumeBtnRect.y, 2f, resumeBtnRect.height), whiteTex);

        GUIStyle resSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        resSt.normal.textColor = Color.white;
        GUI.Label(new Rect(resumeBtnRect.x, resumeBtnRect.y + 4f, resumeBtnRect.width, 20f), "▶ REANUDAR COMBATE", resSt);
        GUIStyle hotSt = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
        hotSt.normal.textColor = new Color(0.9f, 1f, 0.9f, 0.85f);
        GUI.Label(new Rect(resumeBtnRect.x, resumeBtnRect.y + 24f, resumeBtnRect.width, 16f), "[ Presiona TAB o ESC ]", hotSt);

        if (Event.current.type == EventType.MouseDown && hoverResume)
        {
            gm.Resume();
            Event.current.Use();
        }

        // Botón Salir al Menú Principal
        Rect quitBtnRect = new Rect(startX + totalW - 280f, bottomBarY, 280f, 44f);
        bool hoverQuit = quitBtnRect.Contains(Event.current.mousePosition);
        GUI.color = hoverQuit ? new Color(0.85f, 0.2f, 0.25f, 0.98f) : new Color(0.45f, 0.12f, 0.15f, 0.9f);
        GUI.DrawTexture(quitBtnRect, whiteTex);
        GUI.color = Color.white;
        smallStyle.alignment = TextAnchor.MiddleCenter;
        smallStyle.fontStyle = FontStyle.Bold;
        GUI.Label(quitBtnRect, "🚪 SALIR AL MENÚ PRINCIPAL", smallStyle);
        smallStyle.fontStyle = FontStyle.Normal;

        if (Event.current.type == EventType.MouseDown && hoverQuit)
        {
            gm.ReturnToMainMenu();
            Event.current.Use();
        }

        titleStyle.fontSize = 36;
        GUI.color = Color.white;
    }

    // ═══════════════════════════════════════════════════════
    // BARRA DE XP — Parte superior
    // ═══════════════════════════════════════════════════════
    void DrawXPBar()
    {
        XPSystem xp = XPSystem.Instance ?? Object.FindAnyObjectByType<XPSystem>();
        if (xp == null) return;

        float barWidth = MegabonkUI.Width - 40f;
        float barHeight = 16f;
        float x = 20f;
        float y = 8f;

        GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
        GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), whiteTex);

        float xpPercent = xp.maxXP > 0 ? Mathf.Clamp01((float)xp.currentXP / xp.maxXP) : 0f;
        float flash = xp.xpGainFlash > 0f ? xp.xpGainFlash * 0.4f : 0f;
        GUI.color = new Color(0.7f + flash, 0.2f, 1f, 1f);
        GUI.DrawTexture(new Rect(x, y, barWidth * xpPercent, barHeight), whiteTex);

        GUI.color = new Color(0.8f, 0.6f, 1f, 0.9f);
        GUI.DrawTexture(new Rect(x, y, barWidth, 1.5f), whiteTex);
        GUI.DrawTexture(new Rect(x, y + barHeight - 1.5f, barWidth, 1.5f), whiteTex);
        GUI.DrawTexture(new Rect(x, y, 1.5f, barHeight), whiteTex);
        GUI.DrawTexture(new Rect(x + barWidth - 1.5f, y, 1.5f, barHeight), whiteTex);

        smallStyle.alignment = TextAnchor.MiddleCenter;
        smallStyle.fontStyle = FontStyle.Bold;
        GUI.color = Color.white;
        string xpText = "★ NIVEL " + xp.level + "   [" + xp.currentXP + " / " + xp.maxXP + " XP]";
        GUI.Label(new Rect(x, y - 2f, barWidth, barHeight), xpText, smallStyle);
        smallStyle.fontStyle = FontStyle.Normal;

        GUI.color = Color.white;
    }

    // ═══════════════════════════════════════════════════════
    // HABILIDADES OBTENIDAS — Columnas de arriba hacia abajo
    // ═══════════════════════════════════════════════════════
    private struct ActiveSkillBadge
    {
        public string label;
        public string timerText;
        public Color color;
    }

    void DrawActiveBuffs()
    {
        XPSystem xp = XPSystem.Instance ?? Object.FindAnyObjectByType<XPSystem>();
        if (xp == null) return;

        System.Collections.Generic.List<ActiveSkillBadge> activeList = new System.Collections.Generic.List<ActiveSkillBadge>();

        // Recorrer todas las mejoras adquiridas desde el catálogo dinámico
        if (xp.acquiredUpgradeIds != null && xp.acquiredUpgradeIds.Count > 0)
        {
            foreach (string id in xp.acquiredUpgradeIds)
            {
                var def = UpgradeCatalog.Get(id);
                if (def != null)
                {
                    string timer = xp.GetAbilityTimerText(id);
                    activeList.Add(new ActiveSkillBadge
                    {
                        label = $"{def.icon} {def.name.ToUpper()}",
                        timerText = timer,
                        color = def.color
                    });
                }
            }
        }
        else
        {
            // Respaldo para compatibilidad
            if (xp.powerShot) activeList.Add(new ActiveSkillBadge { label = "💥 PODER TIRO", timerText = null, color = new Color(1f, 0.4f, 0.2f) });
            if (xp.rapidFire) activeList.Add(new ActiveSkillBadge { label = "🔥 FUEGO RÁPIDO", timerText = null, color = new Color(1f, 0.6f, 0.1f) });
            if (xp.doubleShot) activeList.Add(new ActiveSkillBadge { label = "🔫 DISPARO DOBLE", timerText = null, color = new Color(1f, 0.35f, 0.35f) });
            if (xp.takeAim) activeList.Add(new ActiveSkillBadge { label = "🎯 PUNTERÍA", timerText = null, color = new Color(0.3f, 0.9f, 0.5f) });
            if (xp.haste) activeList.Add(new ActiveSkillBadge { label = "👟 PRISA", timerText = null, color = new Color(0.2f, 0.9f, 0.4f) });
            if (xp.pyroMage) activeList.Add(new ActiveSkillBadge { label = "🌋 PIROMANCIA", timerText = null, color = new Color(1f, 0.35f, 0.05f) });
            if (xp.frostMage) activeList.Add(new ActiveSkillBadge { label = "❄ CRIOMANCIA", timerText = null, color = new Color(0.4f, 0.85f, 1f) });
            if (xp.electroMage) activeList.Add(new ActiveSkillBadge { label = "⚡ ELECTROMANCIA", timerText = null, color = new Color(1f, 0.95f, 0.2f) });
        }

        // Mostrar Suerte y Reliquias Megabonk activas
        if (MegabonkRelicManager.Instance != null)
        {
            float luck = MegabonkRelicManager.Instance.luckPercent;
            if (luck > 0f)
            {
                activeList.Add(new ActiveSkillBadge
                {
                    label = $"🍀 SUERTE +{luck:0.#}%",
                    timerText = null,
                    color = new Color(0.25f, 1f, 0.45f)
                });
            }

            foreach (var kvp in MegabonkRelicManager.Instance.acquiredRelics)
            {
                if (kvp.Value <= 0) continue;
                activeList.Add(new ActiveSkillBadge
                {
                    label = $"🎁 {kvp.Key.Replace('_', ' ').ToUpper()} x{kvp.Value}",
                    timerText = null,
                    color = new Color(1f, 0.85f, 0.2f)
                });
            }
        }

        if (activeList.Count == 0) return;

        // Distribución en columnas de arriba hacia abajo
        float startX = 20f;
        float startY = 32f; // Justo debajo de la barra de XP
        float pillW = 200f;
        float pillH = 22f;
        float spacingY = 4f;
        float spacingX = 8f;

        // Límite inferior para no chocar con la barra de vida abajo
        float bottomLimit = MegabonkUI.Height - 75f;
        int maxRowsPerCol = Mathf.Max(4, Mathf.FloorToInt((bottomLimit - startY) / (pillH + spacingY)));

        for (int i = 0; i < activeList.Count; i++)
        {
            int col = i / maxRowsPerCol; // Al llenarse la columna, se crea otra a la derecha
            int row = i % maxRowsPerCol; // Se van acomodando de arriba hacia abajo

            float x = startX + col * (pillW + spacingX);
            float y = startY + row * (pillH + spacingY);

            DrawBuffBadge(x, y, pillW, pillH, activeList[i].label, activeList[i].timerText, activeList[i].color);
        }
    }

    void DrawBuffBadge(float x, float y, float w, float h, string label, string timerText, Color col)
    {
        // Fondo traslúcido
        GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.88f);
        GUI.DrawTexture(new Rect(x, y, w, h), whiteTex);

        // Barra lateral izquierda del color del elemento
        GUI.color = col;
        GUI.DrawTexture(new Rect(x, y, 4f, h), whiteTex);

        // Borde exterior fino
        GUI.color = new Color(col.r * 0.6f, col.g * 0.6f, col.b * 0.6f, 0.5f);
        GUI.DrawTexture(new Rect(x + 4f, y, w - 4f, 1f), whiteTex);
        GUI.DrawTexture(new Rect(x + 4f, y + h - 1f, w - 4f, 1f), whiteTex);
        GUI.DrawTexture(new Rect(x + w - 1f, y, 1f, h), whiteTex);

        // Texto con icono
        GUIStyle bStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold
        };
        bStyle.normal.textColor = Color.white;
        GUI.color = Color.white;

        if (string.IsNullOrEmpty(timerText))
        {
            GUI.Label(new Rect(x + 8f, y, w - 12f, h), label, bStyle);
        }
        else
        {
            float timerW = 75f;
            float labelW = w - timerW - 10f;
            GUI.Label(new Rect(x + 8f, y, labelW, h), label, bStyle);

            GUIStyle tStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold
            };
            bool isCountdown = timerText.Contains("⏱") || timerText.Contains("⟳") || timerText.Contains("🥚");
            tStyle.normal.textColor = isCountdown ? new Color(1f, 0.9f, 0.25f) : new Color(0.35f, 0.95f, 1f);
            GUI.Label(new Rect(x + w - timerW - 6f, y, timerW, h), timerText, tStyle);
        }
    }
}
