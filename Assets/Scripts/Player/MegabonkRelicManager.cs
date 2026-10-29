using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema Oficial de Reliquias y Objetos de Megabonk:
/// - 4 Niveles de Rareza: 🟢 Común (21), 🔵 Poco común (21), 🟣 Raro (18), 🟡 Legendario (4).
/// - Sistema de probabilidad dinámico gobernado por la estadística de Suerte (Luck).
/// - Interfaz de cofre interactivo (Loot Chest Modal) con 3 opciones a elegir.
/// - Gestión de efectos de combate activos y pasivos.
/// </summary>
public class MegabonkRelicManager : MonoBehaviour
{
    public static MegabonkRelicManager Instance { get; private set; }
    PlayerHealth3D cachedHealth;
    float hostileFreezeUntil;
    public static float HostileTimeScale
    {
        get
        {
            var manager = Instance;
            if (manager == null) return 1;
            if (Time.time < manager.hostileFreezeUntil) return 0;
            if (manager.GetCount("speed_boi") == 0) return 1;
            if (manager.cachedHealth == null) manager.cachedHealth = UnityEngine.Object.FindAnyObjectByType<PlayerHealth3D>();
            return manager.cachedHealth != null && manager.cachedHealth.currentHealth * 2 < manager.cachedHealth.maxHealth ? .5f : 1;
        }
    }
    public static float HostileDeltaTime => Time.deltaTime * HostileTimeScale;

    public enum RelicRarity
    {
        Common,     // 🟢 Verde
        Uncommon,   // 🔵 Azul
        Rare,       // 🟣 Magenta
        Legendary   // 🟡 Dorado / Legendario
    }

    [System.Serializable]
    public class RelicData
    {
        public string id;
        public string name;
        public RelicRarity rarity;
        public string icon;
        public string description;
        public string unlockCondition;
        public Color bannerColor;

        public RelicData(string id, string name, RelicRarity rarity, string icon, string description, string unlock, Color bannerColor)
        {
            this.id = id;
            this.name = name;
            this.rarity = rarity;
            this.icon = icon;
            this.description = description;
            this.unlockCondition = unlock;
            this.bannerColor = bannerColor;
        }
    }

    // ── Catálogo Maestro de Reliquias ──────────────────────────
    private static readonly List<RelicData> MasterCatalog = new List<RelicData>();
    public static IReadOnlyList<RelicData> Catalog { get { InitializeCatalog(); return MasterCatalog; } }

    // ── Inventario de la partida actual ───────────────────────
    [System.NonSerialized]
    public Dictionary<string, int> acquiredRelics = new Dictionary<string, int>();

    // ── Estadísticas gobernadas por reliquias ─────────────────
    [Header("Estadísticas de Reliquias")]
    public float luckPercent = 0f; // 0% base
    private float temporaryDifficultyPercent;
    public int chestsOpenedCount = 0;
    public float idleTimer = 0f;
    private Vector3 lastPlayerPos;
    private float distanceWalkedAccumulator = 0f;
    private float campfireTimer = 0f;
    private float thunderMittsReadyAt;
    private float slurpGlovesReadyAt;
    private float energyCoreTimer;
    private float beaconTimer;
    private float magnetTimer;
    private int demonicBloodKills;
    private int daggerExecutions;
    readonly List<int> beerHealthCosts = new List<int>();
    private float sneakerGoldDistance;

    // Reliquias Legendarias de un uso o estado
    public bool zaWarudoReady = true;
    public bool HasZaWarudo => GetCount("za_warudo") > 0;
    public bool HasBigBonk => GetCount("big_bonk") > 0;
    public bool HasSpicyMeatball => GetCount("spicy_meatball") > 0;
    public bool HasOpLamp => GetCount("op_lamp") > 0;

    // Estado del Menú de Cofre / Ruleta de Galería
    public enum RouletteState { Spinning, Revealed }
    [HideInInspector] public bool chestMenuOpen = false;
    [HideInInspector] public RouletteState rouletteState = RouletteState.Spinning;
    private List<RelicData> rouletteReel = new List<RelicData>();
    private RelicData wonRelic;
    private int winningIndex = 32;
    private float spinTimer = 0f;
    private float spinDuration = 3.6f;
    private Action onChestClosedCallback;

    // Colores de rareza oficiales
    public static readonly Color ColorCommon = new Color(0.2f, 0.9f, 0.35f);       // 🟢 Verde
    public static readonly Color ColorUncommon = new Color(0.2f, 0.65f, 1f);       // 🔵 Azul
    public static readonly Color ColorRare = new Color(0.9f, 0.2f, 0.85f);         // 🟣 Magenta
    public static readonly Color ColorLegendary = new Color(1f, 0.85f, 0.15f);     // 🟡 Amarillo

    public static RelicData GetRelicData(string id)
    {
        return MasterCatalog.Find(r => r.id == id);
    }

    static MegabonkRelicManager()
    {
        InitializeCatalog();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ResetRelics();
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) lastPlayerPos = p.transform.position;
    }

    public static void InitializeCatalog()
    {
        if (MasterCatalog.Count > 0) return;

        // ═════════════════════════════════════════════════════════
        // 🟢 OBJETOS COMUNES (21 OBJETOS)
        // ═════════════════════════════════════════════════════════
        AddC("moldy_cheese", "Moldy Cheese", "🧀", "+40% de probabilidad de envenenar al golpear.", "Desbloqueado por defecto");
        AddC("borgar", "Borgar", "🍔", "+2% de probabilidad de generar un Borgar al matar a un enemigo que cura 1 corazón.", "Desbloqueado por defecto");
        AddC("wrench", "Wrench", "🔧", "Altar de Carga: Carga un 4% más rápido y da +7.5% de recompensas.", "Desbloqueado por defecto");
        AddC("clover", "Clover", "🍀", "Incrementa la Suerte en un +7.5%.", "Desbloqueado por defecto");
        AddC("time_bracelet", "Time Bracelet", "⌚", "+8% de ganancia de Experiencia (XP).", "Desbloqueado por defecto");
        AddC("slippery_ring", "Slippery Ring", "💍", "Incrementa la Evasión en un +15%.", "Desbloqueado por defecto");
        AddC("golden_glove", "Golden Glove", "🥊", "Ganas +15% más de Oro / Puntuación al matar enemigos.", "Desbloqueado por defecto");
        AddC("gym_sauce", "Gym Sauce", "🥫", "Incrementa el Daño en un +10%.", "Desbloqueado por defecto");
        AddC("oats", "Oats", "🥣", "Incrementa la Vida Máxima en +1 Contenedor de Corazón (+25 HP).", "Desbloqueado por defecto");
        AddC("medkit", "MedKit", "🩹", "Recupera 2 HP por segundo y por copia.", "Desbloqueado por defecto");
        AddC("turbo_socks", "Turbo Socks", "🧦", "Incrementa la Velocidad de Movimiento en un +15%.", "Sube el Tomo de Agilidad a Nivel 5");
        AddC("tactical_goggles", "Tactical Goggles", "🥽", "+20% de daño a enemigos que tengan más del 90% de su HP.", "Derrota a un Miniboss en 25s o menos");
        AddC("battery", "Battery", "🔋", "Incrementa la Velocidad de Ataque / Cadencia en un +8%.", "Sube el Tomo de Cooldown a Nivel 5");
        AddC("forbidden_juice", "Forbidden Juice", "🧃", "Incrementa la Probabilidad de Crítico en un +10%.", "Logra 100 golpes críticos");
        AddC("cursed_doll", "Cursed Doll", "🎎", "Maldice cada segundo a un enemigo: 30% de vida máxima (1% contra jefes).", "Muere 10 veces");
        AddC("boss_buster", "Boss Buster", "🎯", "+15% de daño contra Elites y Jefes.", "Derrota a un Jefe en menos de 30 segundos");
        AddC("key", "Key", "🔑", "+10% de probabilidad de abrir cofres de forma gratuita / recompensa extra.", "Compra 25 cofres");
        AddC("ice_crystal", "Ice Crystal", "🧊", "+7.5% de probabilidad de congelar enemigos al golpearlos.", "Consigue 50 bajas con Frostwalker");
        AddC("skuleg", "Skuleg", "💀", "Incrementa la Dificultad del juego en +7% pero otorga más puntos.", "Obtén un 40% de Dificultad");
        AddC("ghost", "Ghost", "👻", "Abrir o interactuar con un cofre invoca fantasmas aliados.", "Sobrevive al Enjambre Final por 60 segundos");
        AddC("cactus", "Cactus", "🌵", "Dispara espinas en círculo al recibir daño. El daño escala con Espinas.", "Mata 100 enemigos usando daño de Espinas");

        // ═════════════════════════════════════════════════════════
        // 🔵 OBJETOS POCO COMUNES (21 OBJETOS)
        // ═════════════════════════════════════════════════════════
        AddU("credit_card_red", "Credit Card (Red)", "💳", "Incrementa el Daño en un +2.5% cada vez que abres un cofre.", "Desbloqueado por defecto");
        AddU("golden_shield", "Golden Shield", "🛡", "Te otorga oro y puntos extra cada vez que recibes daño.", "Desbloqueado por defecto");
        AddU("thunder_mitts", "Thunder Mitts", "⚡", "Al golpear, inflige un aplauso de trueno que genera un rayo en área (10s CD).", "Desbloqueado por defecto");
        AddU("feathers", "Feathers", "🪶", "Te permite saltar más alto y más ágilmente.", "Desbloqueado por defecto");
        AddU("electric_plug", "Electric Plug", "🔌", "Al recibir daño, lanza una descarga eléctrica a los enemigos cercanos.", "Desbloqueado por defecto");
        AddU("cowards_cloak", "Coward's Cloak", "🧥", "Aumenta la Velocidad de Movimiento al recibir daño y te da +5% de base.", "Desbloqueado por defecto");
        AddU("unstable_transfusion", "Unstable Transfusion", "🩸", "+27% de probabilidad de aplicar una carga de Bloodmark al golpear.", "Desbloqueado por defecto");
        AddU("beer", "Beer", "🍺", "+20% de Daño a cambio de perder 1 Corazón Máximo.", "Desbloqueado por defecto");
        AddU("beacon", "Beacon", "🗼", "Genera una zona de curación constante a tu alrededor.", "Desbloqueado por defecto");
        AddU("backpack", "Backpack", "🎒", "+1 proyectil o salto de rayo para armas compatibles.", "Desbloqueado por defecto");
        AddU("phantom_shroud", "Phantom Shroud", "👤", "Tras evadir, haces 2x de daño en el próximo ataque y subes velocidad temporalmente.", "Desbloqueado por defecto");
        AddU("campfire", "Campfire", "🏕", "Quedarte completamente quieto restaura 1 corazón cada 6 segundos.", "Desbloqueado por defecto");
        AddU("brass_knuckles", "Brass Knuckles", "🥊", "Inflige +20% más de daño a los enemigos que estén muy cerca.", "Mata 5,000 enemigos usando Espada");
        AddU("demonic_blade", "Demonic Blade", "🗡", "Los golpes críticos tienen un +25% de probabilidad de restaurar 1 corazón.", "Cúrate 350 HP usando Robo de Vida");
        AddU("demonic_blood", "Demonic Blood", "🩸", "Aumenta la vida máxima por bajas acumuladas.", "Incrementa tu HP con Magia de Sangre");
        AddU("echo_shard", "Echo Shard", "💎", "+12% de probabilidad de generar una partícula de XP extra al matar un enemigo.", "Sube el Tomo de XP a Nivel 8");
        AddU("moldy_gloves", "Moldy Gloves", "🧤", "Crea una nube de veneno mohosa al golpear.", "Duplica Moldy Cheese 3 veces");
        AddU("golden_sneakers", "Golden Sneakers", "👟", "Ganas puntos y oro de forma pasiva simplemente corriendo por el mapa.", "Rompe 150 vasijas");
        AddU("idle_juice", "Idle Juice", "🧋", "Entre más tiempo pases quieto, más daño haces (hasta un máximo de +100%).", "Mata 500 enemigos estando quieto");
        AddU("leeching_crystal", "Leeching Crystal", "💠", "+2 Contenedores de Corazón Máximos.", "Sube el Tomo de Regeneración a Nivel 8");
        AddU("kevin", "Kevin", "😈", "Tiene un 25% de probabilidad de darte un empujón cómico no letal.", "Consigue 3 Leeching Crystals en una partida");

        // ═════════════════════════════════════════════════════════
        // 🟣 OBJETOS RAROS (18 OBJETOS)
        // ═════════════════════════════════════════════════════════
        AddR("beefy_ring", "Beefy Ring", "💍", "Incrementa tu daño en un +20% por cada Contenedor de Vida que tengas.", "Desbloqueado por defecto");
        AddR("credit_card_green", "Credit Card (Green)", "💳", "Incrementa la Suerte en un +2% cada vez que abres un cofre.", "Desbloqueado por defecto");
        AddR("spiky_shield", "Spiky Shield", "🛡", "+5 de armadura; al recibir daño inflige 8 + 2 por punto de armadura a enemigos cercanos.", "Desbloqueado por defecto");
        AddR("scarf", "Scarf", "🧣", "Inflige un +50% más de Daño mientras te encuentres en el aire (saltando).", "Desbloqueado por defecto");
        AddR("slurp_gloves", "Slurp Gloves", "🧤", "Lanza magia de sangre al golpear, dañando el área y curándote (9s CD).", "Desbloqueado por defecto");
        AddR("mirror", "Mirror", "🪞", "Refleja el daño a enemigos a 6 metros y añade 0.3 s de invulnerabilidad.", "Desbloqueado por defecto");
        AddR("turbo_skates", "Turbo Skates", "⛸", "Entre más rápido te muevas, más Velocidad de Ataque ganas (hasta +40%).", "Sube Cooldown y Agilidad a Nivel 5");
        AddR("slutty_cannon", "Slutty Cannon", "🚀", "20% de probabilidad de lanzar misiles teledirigidos al matar a un enemigo.", "Mata 5,000 enemigos con Slutty Rocket");
        AddR("shattered_knowledge", "Shattered Knowledge", "📜", "Las partículas de experiencia atraviesan y dañan a los enemigos al recogerlas.", "Sube Atracción a Nivel 8");
        AddR("quins_mask", "Quin's Mask", "🎭", "El daño de tus Espinas (Thorns) tiene un 50% de probabilidad de explotar en área.", "Derrota un Jefe con Athena");
        AddR("toxic_barrel", "Toxic Barrel", "🛢", "Libera desechos tóxicos al recibir daño, envenenando en área a los enemigos.", "Mata 2,000 enemigos con Veneno");
        AddR("gas_mask", "Gas Mask", "🎭", "Ganas armadura y protección por cada enemigo infectado con veneno.", "Mata 5,000 enemigos con Amog");
        AddR("gamer_goggles", "Gamer Goggles", "👓", "Tu daño aumenta exponencialmente entre menor sea tu cantidad de corazones actuales.", "Derrota a un Jefe con menos del 10% HP");
        AddR("eagle_claw", "Eagle Claw", "🦅", "+66% de daño contra enemigos aéreos (dragones y voladores).", "Mata 10,000 enemigos jugando con Birdo");
        AddR("demonic_soul", "Demonic Soul", "🔮", "Incrementa el Daño en un +0.1% por cada baja realizada (Máximo hasta +100%).", "Llega a Nivel 30 con Calcium");
        AddR("bob_dead", "Bob (Dead)", "🧟", "Por cada 14 unidades de distancia que camines, invocas al cadáver aliado de Bob.", "Sobrevive al enjambre 2 minutos");
        AddR("grandmas_secret_tonic", "Grandma's Secret Tonic", "🧪", "Los impactos críticos tienen un 50% de probabilidad de explotar en área.", "Mata 3,000 con Sniper Rifle");
        AddR("cursed_grabbies", "Cursed Grabbies", "🖐", "+10% dificultad, pero 5% de probabilidad de lanzar magia maldita masiva.", "Sube Tomo Maldito a Nivel 10");

        // ═════════════════════════════════════════════════════════
        // 🟡 OBJETOS LEGENDARIOS (4 OBJETOS)
        // ═════════════════════════════════════════════════════════
        AddL("op_lamp", "Overpowered Lamp", "🪔", "Te otorga un +100% de probabilidad de duplicar cualquier efecto de golpe (on-hit effects).", "Desbloqueado por defecto");
        AddL("za_warudo", "Za Warudo", "⏳", "Evita un golpe letal: quedas con 1 HP, inmune y con enemigos y proyectiles congelados durante 5 s. Un uso por partida.", "Desbloqueado por defecto");
        AddL("big_bonk", "Big Bonk", "🔨", "Te otorga un 2% de probabilidad de aplicar un 'BONK' fulminante infligiendo 20x de daño.", "Desbloqueado por defecto");
        AddL("spicy_meatball", "Spicy Meatball", "🧆", "Tus ataques obtienen un 25% de probabilidad de lanzar una albóndiga picante gigante explosiva.", "Desbloqueado por defecto");
        // Legendarios adicionales del catálogo de MegaBonk (Items wiki).
        AddL("ice_cube", "Ice Cube", "🧊", "20% de probabilidad de aplicar daño de hielo al golpear; el hielo puede congelar.", "Desbloqueado por defecto");
        AddL("giant_fork", "Giant Fork", "🍴", "Los críticos pueden convertirse en Megacríticos de daño superior.", "Desbloqueado por defecto");
        AddL("power_gloves", "Power Gloves", "🥊", "8% de probabilidad de crear una explosión que daña y empuja a los enemigos.", "Desbloqueado por defecto");
        AddL("lightning_orb", "Lightning Orb", "⚡", "25% de probabilidad de invocar un rayo al golpear; los rayos pueden aturdir.", "Mata 3,000 enemigos con daño eléctrico");
        AddL("chonkplate", "Chonkplate", "🛡", "Convierte curación sobrante en escudo, hasta 75% de vida adicional; roba 20% de vida al atacar.", "Alcanza 500 de vida máxima");
        AddL("dragonfire", "Dragonfire", "🐉", "15% de probabilidad de invocar fuego de dragón; el fuego puede aplicar quemadura.", "Mata 4,000 enemigos con daño de fuego");
        AddL("sucky_magnet", "Sucky Magnet", "🧲", "Atrae periódicamente todos los orbes de experiencia cercanos.", "Completa todos los santuarios de carga de un mapa");
        AddL("holy_book", "Holy Book", "📖", "+100 de vida máxima y +50 de regeneración; curarte daña a los enemigos cercanos.", "Derrota al primer jefe sin recibir daño");
        AddL("speed_boi", "Speed Boi", "💨", "Con menos de 50% de vida ralentiza enemigos y proyectiles al 50% y añade +100% de daño.", "Completa una fase con 2 maldiciones de jefe");
        AddL("energy_core", "Energy Core", "🔋", "Dispara orbes de energía que buscan a los enemigos.", "Derrota al jefe final 2 veces");
        AddL("anvil", "Anvil", "⚒", "Cada nueva arma o nivel de arma otorga +2% de daño global por copia.", "Completa 3 desafíos");
        AddL("soul_harvester", "Soul Harvester", "☠", "Cada enemigo eliminado invoca dos almas perseguidoras.", "Sobrevive al enjambre final durante 6 minutos");
        AddL("joes_dagger", "Joe's Dagger", "🗡", "1% de probabilidad de ejecutar; cada ejecución aumenta el daño 1%.", "Mata 10,000 enemigos con Dexecutioner");
        AddL("bloody_cleaver", "Bloody Cleaver", "🩸", "El robo de vida activa Bloodmark y añade 50% de probabilidad de aplicarlo.", "Alcanza nivel 50 con Vlad");
    }

    private static void AddC(string id, string name, string icon, string desc, string unlock) =>
        MasterCatalog.Add(new RelicData(id, name, RelicRarity.Common, icon, desc, unlock, ColorCommon));

    private static void AddU(string id, string name, string icon, string desc, string unlock) =>
        MasterCatalog.Add(new RelicData(id, name, RelicRarity.Uncommon, icon, desc, unlock, ColorUncommon));

    private static void AddR(string id, string name, string icon, string desc, string unlock) =>
        MasterCatalog.Add(new RelicData(id, name, RelicRarity.Rare, icon, desc, unlock, ColorRare));

    private static void AddL(string id, string name, string icon, string desc, string unlock) =>
        MasterCatalog.Add(new RelicData(id, name, RelicRarity.Legendary, icon, desc, unlock, ColorLegendary));

    public void ResetRelics()
    {
        acquiredRelics.Clear();
        beerHealthCosts.Clear();
        hostileFreezeUntil = 0;
        extraTimer = 0;
        luckPercent = 0f;
        temporaryDifficultyPercent = 0f;
        chestsOpenedCount = 0;
        idleTimer = 0f;
        campfireTimer = 0f;
        thunderMittsReadyAt = 0f;
        slurpGlovesReadyAt = 0f;
        energyCoreTimer = 0f;
        beaconTimer = 0f;
        magnetTimer = 0f;
        demonicBloodKills = 0;
        daggerExecutions = 0;
        sneakerGoldDistance = 0f;
        distanceWalkedAccumulator = 0f;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        lastPlayerPos = player != null ? player.transform.position : Vector3.zero;
        zaWarudoReady = true;
        chestMenuOpen = false;
        rouletteReel.Clear();
        wonRelic = null;
    }

    public int GetCount(string id)
    {
        return acquiredRelics.TryGetValue(id, out int count) ? count : 0;
    }

    // Dificultad tipo MegaBonk: más presión de oleadas a cambio de más riesgo.
    public float DifficultyPercent => GetCount("skuleg") * 7f + GetCount("cursed_grabbies") * 10f + temporaryDifficultyPercent;
    public float DifficultyMultiplier => 1f + DifficultyPercent / 100f;

    /// <summary>
    /// Calcula las probabilidades oficiales de rareza según la Suerte actual del jugador.
    /// </summary>
    public (float common, float uncommon, float rare, float legendary) GetRarityChances()
    {
        // Tasas base oficiales (Con 0% de Suerte):
        float c = 70.0f;
        float u = 23.0f;
        float r = 6.5f;
        float l = 0.5f;

        // Escalado matemático dinámico de Suerte:
        if (luckPercent >= 2000f)
        {
            // Umbral > 2000%: Legendario se dispara masivamente
            l = 28.0f;
            r = 50.0f;
            u = 20.0f;
            c = 2.0f;
        }
        else if (luckPercent >= 300f)
        {
            // Umbral 300%-400%: Raro (Magenta) es el estándar (~45-50%)
            float t = Mathf.Clamp01((luckPercent - 300f) / 100f);
            r = Mathf.Lerp(45f, 52f, t);
            u = 32f;
            l = Mathf.Lerp(3f, 8f, t);
            c = Mathf.Max(1f, 100f - (r + u + l));
        }
        else if (luckPercent >= 100f)
        {
            // Umbral > 100%: Azules y Raros se duplican, Comunes bajan
            float t = Mathf.Clamp01((luckPercent - 100f) / 200f);
            u = Mathf.Lerp(38f, 42f, t);
            r = Mathf.Lerp(15f, 35f, t);
            l = Mathf.Lerp(1.2f, 3.5f, t);
            c = Mathf.Max(5f, 100f - (u + r + l));
        }
        else if (luckPercent > 0f)
        {
            // Incremento progresivo de 0 a 100% de Suerte
            float t = luckPercent / 100f;
            u = Mathf.Lerp(23f, 36f, t);
            r = Mathf.Lerp(6.5f, 16f, t);
            l = Mathf.Lerp(0.5f, 1.5f, t);
            c = 100f - (u + r + l);
        }

        return (c, u, r, l);
    }

    public RelicRarity RollRarity()
    {
        var (c, u, r, l) = GetRarityChances();
        float roll = UnityEngine.Random.Range(0f, 100f);

        if (roll < l) return RelicRarity.Legendary;
        if (roll < (l + r)) return RelicRarity.Rare;
        if (roll < (l + r + u)) return RelicRarity.Uncommon;
        return RelicRarity.Common;
    }

    public List<RelicData> RollChestOptions(int count = 3, bool forceLegendary = false)
    {
        count = Mathf.Clamp(count, 1, MasterCatalog.Count);
        List<RelicData> chosen = new List<RelicData>();
        HashSet<string> pickedIds = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            RelicRarity targetRarity = (forceLegendary && i == 0) ? RelicRarity.Legendary : RollRarity();
            List<RelicData> pool = MasterCatalog.FindAll(item => item.rarity == targetRarity && !pickedIds.Contains(item.id) && ExpeditionProgress.IsUnlocked(item));

            if (pool.Count == 0)
            {
                // Fallback a cualquier reliquia no elegida aún
                pool = MasterCatalog.FindAll(item => !pickedIds.Contains(item.id) && ExpeditionProgress.IsUnlocked(item));
            }

            if (pool.Count > 0)
            {
                RelicData picked = pool[UnityEngine.Random.Range(0, pool.Count)];
                chosen.Add(picked);
                pickedIds.Add(picked.id);
            }
        }

        return chosen;
    }

    /// <summary>
    /// Abre la interfaz interactiva de Cofre (Megabonk Chest Modal) con 3 reliquias.
    /// </summary>
    public void OpenChest(bool isBossChest = false, Action onClosed = null)
    {
        // Evita duplicar recompensas si dos eventos intentan abrir el mismo
        // cofre durante el mismo frame o mientras la ruleta ya está visible.
        if (chestMenuOpen) return;

        if (!isBossChest) { chestsOpenedCount++; ExpeditionProgress.RecordChestOpened(); }
        onChestClosedCallback = onClosed;

        // Credit Card (Red): +2.5% Daño por cofre abierto
        if (GetCount("credit_card_red") > 0)
        {
            if (XPSystem.Instance != null) XPSystem.Instance.damageMultiplier += 0.025f * GetCount("credit_card_red");
        }

        // Credit Card (Green): +2% Suerte por cofre abierto
        if (GetCount("credit_card_green") > 0)
        {
            luckPercent += 2.0f * GetCount("credit_card_green");
        }

        // Ghost: Usar interactuable / cofre invoca fantasmas
        if (GetCount("ghost") > 0)
        {
            PlayerSummons summons = UnityEngine.Object.FindAnyObjectByType<PlayerSummons>();
            if (summons == null) { var host = GameObject.FindGameObjectWithTag("Player"); if (host != null) summons = host.AddComponent<PlayerSummons>(); }
            if (summons != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) for (int copy = 0; copy < GetCount("ghost"); copy++) summons.SpawnSkeleton(player.transform.position);
            }
        }

        // Determinar la reliquia ganada según suerte / rareza (o legendaria para jefes)
        RelicRarity targetRarity = isBossChest ? RelicRarity.Legendary : RollRarity();
        List<RelicData> pool = MasterCatalog.FindAll(r => r.rarity == targetRarity && ExpeditionProgress.IsUnlocked(r));
        if (pool.Count == 0) pool = MasterCatalog.FindAll(ExpeditionProgress.IsUnlocked);
        wonRelic = pool[UnityEngine.Random.Range(0, pool.Count)];

        // Construir la cinta de la galería rápida (42 ítems deslizantes)
        rouletteReel.Clear();
        winningIndex = 32;
        for (int i = 0; i < 42; i++)
        {
            if (i == winningIndex)
            {
                rouletteReel.Add(wonRelic);
            }
            else
            {
                rouletteReel.Add(MasterCatalog[UnityEngine.Random.Range(0, MasterCatalog.Count)]);
            }
        }

        spinTimer = 0f;
        spinDuration = 3.6f;
        rouletteState = RouletteState.Spinning;
        chestMenuOpen = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseChest()
    {
        chestMenuOpen = false;
        rouletteReel.Clear();
        wonRelic = null;

        bool playing = (GameManager.Instance == null || GameManager.Instance.gameState == GameManager.GameState.Playing) &&
            (XPSystem.Instance == null || !XPSystem.Instance.skillMenuOpen && !XPSystem.Instance.bossTomeMenuOpen);
        Time.timeScale = playing ? 1f : 0f;
        Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !playing;

        onChestClosedCallback?.Invoke();
        onChestClosedCallback = null;
    }

    public void ClaimWonRelic()
    {
        if (wonRelic != null)
        {
            SelectRelic(wonRelic);
        }
        else
        {
            CloseChest();
        }
    }

    public void SelectRelic(RelicData relic)
    {
        if (relic == null || !chestMenuOpen || rouletteState != RouletteState.Revealed) return;

        if (acquiredRelics.ContainsKey(relic.id))
            acquiredRelics[relic.id]++;
        else
            acquiredRelics[relic.id] = 1;

        ApplyRelicImmediateEffect(relic.id);
        ExpeditionProgress.RecordRelicAndDifficulty(relic.id, GetCount(relic.id), DifficultyPercent);
        BuildModelPresentation.Acquired(relic.id);
        CloseChest();
    }

    public void AddRelicCopy(string relicId)
    {
        if (string.IsNullOrEmpty(relicId) || GetRelicData(relicId) == null) return;
        if (acquiredRelics.ContainsKey(relicId)) acquiredRelics[relicId]++;
        else acquiredRelics[relicId] = 1;
        ApplyRelicImmediateEffect(relicId);
        ExpeditionProgress.RecordRelicAndDifficulty(relicId, GetCount(relicId), DifficultyPercent);
        BuildModelPresentation.Acquired(relicId);
    }

    public void AddTemporaryDifficulty(float amount)
    {
        temporaryDifficultyPercent += Mathf.Max(0f, amount);
        ExpeditionProgress.RecordRelicAndDifficulty(null, 0, DifficultyPercent);
    }

    public float DamageBonus
    {
        get
        {
            float bonus = Mathf.Min(1f, GameManager.Instance != null ? GameManager.Instance.kills * 0.001f * GetCount("demonic_soul") : 0f);
            bonus += daggerExecutions * 0.01f;
            PlayerHealth3D health = UnityEngine.Object.FindAnyObjectByType<PlayerHealth3D>();
            if (health != null)
            {
                bonus += health.maxHearts * 0.20f * GetCount("beefy_ring");
                if (health.maxHearts > 0)
                    bonus += Mathf.Pow(1f - (float)health.currentHealth / health.maxHealth, 2f) * GetCount("gamer_goggles");
                if (GetCount("speed_boi") > 0 && health.currentHealth * 2 < health.maxHealth)
                    bonus += GetCount("speed_boi");
            }
            return bonus;
        }
    }

    public float FireRateBonus(float movementSpeed)
    {
        return Mathf.Min(0.4f * GetCount("turbo_skates"), Mathf.Max(0f, movementSpeed) * 0.04f * GetCount("turbo_skates"));
    }

    public void RecordDaggerExecution()
    {
        daggerExecutions++;
    }

    public void OnBulletHit(EnemyHealth3D enemy, int damage, bool critical = false)
    {
        if (enemy == null || enemy.IsDying) return;
        bool thunderReady = Time.time >= thunderMittsReadyAt;
        bool slurpReady = Time.time >= slurpGlovesReadyAt;
        int extraRolls = 1 + GetCount("op_lamp");
        for (int i = 0; i < extraRolls; i++)
        {
            if (GetCount("ice_cube") > 0 && UnityEngine.Random.value < Mathf.Min(1f, 0.20f * GetCount("ice_cube")))
            {
                enemy.TakeDamage(Mathf.Max(1, damage / 4), true, Color.cyan);
                enemy.ApplyFreeze(2f);
            }
            if (GetCount("dragonfire") > 0 && UnityEngine.Random.value < Mathf.Min(1f, 0.15f * GetCount("dragonfire")))
                enemy.ApplyBurn(4f, Mathf.Max(4, damage / 5));
            if (GetCount("lightning_orb") > 0 && UnityEngine.Random.value < Mathf.Min(1f, 0.25f * GetCount("lightning_orb")))
            {
                enemy.TakeDamage(Mathf.Max(5, damage / 3), true, Color.yellow);
                enemy.ApplyFreeze(0.5f);
                CombatVisuals.Lightning(enemy.transform.position + Vector3.up * 7f, enemy.transform.position + Vector3.up);
            }
            if (GetCount("power_gloves") > 0 && UnityEngine.Random.value < Mathf.Min(1f, 0.08f * GetCount("power_gloves")))
            {
                enemy.TriggerExplosion(3.5f, Mathf.Max(6, damage / 2));
                foreach (var nearby in EnemyHealth3D.ActiveEnemies)
                    if (nearby != null && !nearby.IsDying && (nearby.transform.position - enemy.transform.position).sqrMagnitude <= 12.25f)
                        nearby.GetComponent<EnemyAI3D>()?.ApplyKnockback((nearby.transform.position - enemy.transform.position + Vector3.up * .1f).normalized, 8);
            }
            if (GetCount("thunder_mitts") > 0 && thunderReady)
            {
                thunderMittsReadyAt = Time.time + 10f;
                enemy.TriggerExplosion(4f, Mathf.Max(8, damage / 2));
                enemy.TakeDamage(Mathf.Max(8, damage / 2), true, Color.yellow);
                CombatVisuals.Lightning(enemy.transform.position + Vector3.up * 8, enemy.transform.position + Vector3.up);
            }
            if (GetCount("slurp_gloves") > 0 && slurpReady)
            {
                slurpGlovesReadyAt = Time.time + 9f;
                enemy.TriggerExplosion(3.5f, Mathf.Max(8, damage / 3));
                enemy.TakeDamage(Mathf.Max(8, damage / 3), true, Color.red);
                PlayerHealth3D health = UnityEngine.Object.FindAnyObjectByType<PlayerHealth3D>();
                if (health != null) health.HealHearts(1);
                if (GetCount("bloody_cleaver") > 0) enemy.ApplyCurse(1, damage / 2);
            }
            if (GetCount("unstable_transfusion") > 0 && UnityEngine.Random.value < Mathf.Min(1f, 0.27f * GetCount("unstable_transfusion")))
                enemy.ApplyCurse(1.5f, Mathf.Max(6, damage / 2));
            if (GetCount("cursed_grabbies") > 0 && UnityEngine.Random.value < Mathf.Min(1f, 0.05f * GetCount("cursed_grabbies")))
                enemy.ApplyCurse(0.8f, damage * 2);
            if (GetCount("moldy_gloves") > 0 && UnityEngine.Random.value < Mathf.Min(1, .2f * GetCount("moldy_gloves")))
                foreach (var nearby in EnemyHealth3D.ActiveEnemies)
                    if (nearby != null && !nearby.IsDying && (nearby.transform.position - enemy.transform.position).sqrMagnitude <= 16)
                        nearby.ApplyPoison(4f, Mathf.Max(4, damage / 6));
            if (UnityEngine.Random.value < Mathf.Min(1, .4f * GetCount("moldy_cheese"))) enemy.ApplyPoison(4, Mathf.Max(2, damage / 5));
            if (UnityEngine.Random.value < Mathf.Min(1, .075f * GetCount("ice_crystal"))) enemy.ApplyFreeze(2);
            if (UnityEngine.Random.value < Mathf.Min(1, .5f * GetCount("bloody_cleaver"))) enemy.ApplyCurse(1, damage / 2);
            if (critical && UnityEngine.Random.value < Mathf.Min(1, .5f * GetCount("grandmas_secret_tonic"))) enemy.TriggerExplosion(4, damage / 2);
            if (critical && UnityEngine.Random.value < Mathf.Min(1, .25f * GetCount("demonic_blade")))
                UnityEngine.Object.FindAnyObjectByType<PlayerHealth3D>()?.Heal(25);
            if (UnityEngine.Random.value < Mathf.Min(1, .25f * GetCount("spicy_meatball")))
                HomingRelicProjectile.Launch(enemy.transform.position + Vector3.up * 3, enemy, damage, new Color(1,.3f,.1f), 4, "meatball");
        }
    }

    public void OnEnemyKill(EnemyHealth3D enemy)
    {
        if (enemy == null) return;
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null) return;
        PlayerHealth3D health = player.GetComponent<PlayerHealth3D>();
        if (GetCount("borgar") > 0 && UnityEngine.Random.value < Mathf.Min(1f, 0.02f * GetCount("borgar")))
            if (health != null) health.HealHearts(1);
        if (GetCount("echo_shard") > 0 && UnityEngine.Random.value < Mathf.Min(1f, 0.12f * GetCount("echo_shard")))
            XpOrb3D.Spawn(enemy.transform.position, 5);
        if (GetCount("demonic_blood") > 0 && ++demonicBloodKills % 40 == 0 && health != null)
            health.IncreaseMaxHearts(1);
        if (GetCount("slutty_cannon") > 0 && UnityEngine.Random.value < Mathf.Min(1f, 0.20f * GetCount("slutty_cannon")))
            LaunchAtNearest(player.position + Vector3.up * 1.4f, 36, new Color(1f, 0.42f, 0.1f), 4f);
        if (GetCount("soul_harvester") > 0)
            for (int i = 0; i < 2 * GetCount("soul_harvester"); i++)
                LaunchAtNearest(enemy.transform.position + Vector3.up, 18, new Color(0.55f, 0.9f, 1f), 0f);
    }

    public void OnPlayerHealed(Vector3 position)
    {
        if (GetCount("holy_book") <= 0) return;
        foreach (EnemyHealth3D enemy in EnemyHealth3D.ActiveEnemies)
            if (enemy != null && !enemy.IsDying && Vector3.Distance(position, enemy.transform.position) <= 5f)
                enemy.TakeDamage(12 * GetCount("holy_book"), true, Color.yellow);
    }

    public void OnPlayerDamaged(Vector3 position)
    {
        int shield = GetCount("golden_shield");
        if (shield > 0 && GameManager.Instance != null) GameManager.Instance.AddGold(3 * shield);
        if (GetCount("cowards_cloak") > 0)
        {
            XPSystem xp = XPSystem.Instance;
            if (xp != null) xp.GrantTemporarySpeed(2f, 0.25f * GetCount("cowards_cloak"));
        }
    }

    void LaunchAtNearest(Vector3 origin, int damage, Color color, float blastRadius)
    {
        EnemyHealth3D nearest = null;
        float nearestDistance = 50f;
        foreach (EnemyHealth3D enemy in EnemyHealth3D.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDying) continue;
            float distance = Vector3.Distance(origin, enemy.transform.position);
            if (distance < nearestDistance) { nearest = enemy; nearestDistance = distance; }
        }
        float multiplier = XPSystem.Instance != null ? XPSystem.Instance.GetCurrentDamageMultiplier() : 1;
        float size = XPSystem.Instance != null ? XPSystem.Instance.bulletSizeMultiplier : 1;
        if (nearest != null) HomingRelicProjectile.Launch(origin, nearest, Mathf.RoundToInt(damage * multiplier * (1 + DamageBonus)), color, blastRadius * size);
    }

    /// <summary>
    /// Microondas: consume una reliquia y crea una copia de otra de la misma rareza.
    /// Elige automáticamente dos reliquias elegibles para no abrir un menú adicional
    /// durante el combate.
    /// </summary>
    public bool TryMicrowaveTransmute()
    {
        string sacrificedId = null;
        string duplicateId = null;

        foreach (KeyValuePair<string, int> entry in acquiredRelics)
        {
            if (entry.Value <= 0) continue;
            RelicData candidate = GetRelicData(entry.Key);
            if (candidate == null) continue;

            foreach (KeyValuePair<string, int> other in acquiredRelics)
            {
                if (other.Value <= 0 || other.Key == entry.Key) continue;
                RelicData target = GetRelicData(other.Key);
                if (target != null && target.rarity == candidate.rarity)
                {
                    sacrificedId = entry.Key;
                    duplicateId = other.Key;
                    break;
                }
            }
            if (sacrificedId != null) break;
        }

        if (sacrificedId == null || duplicateId == null) return false;

        RemoveRelicCopy(sacrificedId);
        AddRelicCopy(duplicateId);
        ExpeditionProgress.RecordMicrowaveDuplicate(duplicateId);
        return true;
    }

    public bool Transmute(string sacrifice, string duplicate)
    {
        if (sacrifice == duplicate || GetCount(sacrifice) <= 0 || GetCount(duplicate) <= 0) return false;
        var a = GetRelicData(sacrifice); var b = GetRelicData(duplicate);
        if (a == null || b == null || a.rarity != b.rarity) return false;
        RemoveRelicCopy(sacrifice); AddRelicCopy(duplicate); ExpeditionProgress.RecordMicrowaveDuplicate(duplicate); return true;
    }

    void RemoveRelicCopy(string relicId)
    {
        if (!acquiredRelics.TryGetValue(relicId, out int count) || count <= 0) return;
        if (count == 1) acquiredRelics.Remove(relicId);
        else acquiredRelics[relicId] = count - 1;

        // Revertir los efectos que se aplican inmediatamente al recoger la reliquia.
        XPSystem xp = XPSystem.Instance;
        PlayerHealth3D ph = UnityEngine.Object.FindAnyObjectByType<PlayerHealth3D>();
        PlayerMovement3D pm = UnityEngine.Object.FindAnyObjectByType<PlayerMovement3D>();
        
        switch (relicId)
        {
            case "holy_book": if (ph != null) { ph.maxHealth = Mathf.Max(1, ph.maxHealth - 100); ph.currentHealth = Mathf.Min(ph.currentHealth, ph.maxHealth); } break;
            case "clover": luckPercent = Mathf.Max(0f, luckPercent - 7.5f); break;
            case "gym_sauce": if (xp != null) xp.damageMultiplier -= 0.10f; break;
            case "oats": RemoveHeartContainers(ph, 1); break;
            case "turbo_socks": if (xp != null) xp.speedMultiplier -= 0.15f; break;
            case "battery": if (xp != null) xp.fireRateMultiplier -= 0.08f; break;
            case "slippery_ring": if (xp != null) xp.dodgeChance = Mathf.Max(0f, xp.dodgeChance - 0.15f); break;
            case "beer":
                if (xp != null) xp.damageMultiplier -= 0.20f;
                if (beerHealthCosts.Count > 0)
                {
                    int last = beerHealthCosts.Count - 1;
                    if (ph != null) ph.IncreaseMaxHealth(beerHealthCosts[last]);
                    beerHealthCosts.RemoveAt(last);
                }
                break;

            case "leeching_crystal": RemoveHeartContainers(ph, 2); break;
            case "feathers": if (pm != null) pm.jumpHeight /= 1.35f; break;
            case "cowards_cloak": if (xp != null) xp.speedMultiplier -= 0.05f; break;
        }
    }

    static void RemoveHeartContainers(PlayerHealth3D health, int amount)
    {
        if (health == null) return;
        health.maxHealth = Mathf.Max(1, health.maxHealth - amount * 25);
        health.currentHealth = Mathf.Min(health.currentHealth, health.maxHealth);
    }

    private void ApplyRelicImmediateEffect(string id)
    {
        XPSystem xp = XPSystem.Instance;
        PlayerHealth3D ph = UnityEngine.Object.FindAnyObjectByType<PlayerHealth3D>();
        PlayerMovement3D pm = UnityEngine.Object.FindAnyObjectByType<PlayerMovement3D>();

        switch (id)
        {
            case "clover":
                luckPercent += 7.5f;
                break;
            case "gym_sauce":
                if (xp != null) xp.damageMultiplier += 0.10f;
                break;
            case "oats":
                if (ph != null) ph.IncreaseMaxHearts(1);
                break;
            case "turbo_socks":
                if (xp != null) xp.speedMultiplier += 0.15f;
                break;
            case "battery":
                if (xp != null) xp.fireRateMultiplier += 0.08f;
                break;
            case "slippery_ring":
                if (xp != null) xp.dodgeChance += 0.15f;
                break;
            case "beer":
                if (xp != null) xp.damageMultiplier += 0.20f;
                beerHealthCosts.Add(ph != null ? Mathf.Min(25, Mathf.Max(0, ph.maxHealth - 1)) : 0);
                RemoveHeartContainers(ph, 1);
                break;

            case "leeching_crystal":
                if (ph != null) ph.IncreaseMaxHearts(2);
                break;
            case "feathers":
                if (pm != null) pm.jumpHeight *= 1.35f;
                break;
            case "cowards_cloak":
                if (xp != null) xp.speedMultiplier += 0.05f;
                break;
            case "holy_book": if (ph != null) ph.IncreaseMaxHealth(100); break;
            case "za_warudo":
                // La carga se reinicia al empezar la partida, no al duplicar el objeto.
                break;
        }
    }

    public void TriggerZaWarudo()
    {
        zaWarudoReady = false;
        hostileFreezeUntil = Time.time + 5f;
        // Congela enemigos durante 5 segundos
        foreach (var e in EnemyHealth3D.ActiveEnemies)
        {
            if (e != null && !e.IsDying) e.ApplyFreeze(5.0f);
        }
    }

    float extraTimer;
    void Update()
    {
        if (chestMenuOpen)
        {
            if (rouletteState == RouletteState.Spinning)
            {
                spinTimer += Time.unscaledDeltaTime;

                // Tecla para saltar inmediatamente al final de la ruleta
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
                {
                    spinTimer = spinDuration;
                }

                if (spinTimer >= spinDuration)
                {
                    spinTimer = spinDuration;
                    rouletteState = RouletteState.Revealed;
                }
            }
            else if (rouletteState == RouletteState.Revealed)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.E))
                {
                    ClaimWonRelic();
                }
            }
            return;
        }

        if (Time.timeScale <= 0f || GameManager.Instance == null || GameManager.Instance.gameState != GameManager.GameState.Playing) return;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;

        float dt = Time.deltaTime;
        extraTimer -= dt;
        if (extraTimer <= 0)
        {
            extraTimer = 1f;
            var health = p.GetComponent<PlayerHealth3D>();
            if (health != null && GetCount("medkit") + GetCount("holy_book") > 0) health.Heal(GetCount("medkit") * 2 + GetCount("holy_book") * 50);
            if (GetCount("cursed_doll") > 0) { var target = MegabonkBuild.Nearest(p.transform.position, 25); if (target != null) target.ApplyCurse(1f, Mathf.RoundToInt(target.maxHealth * (target.isBoss ? .01f : .30f))); }
            if (GetCount("kevin") > 0 && UnityEngine.Random.value < .25f) p.GetComponent<PlayerMovement3D>()?.ApplyKnockback(UnityEngine.Random.onUnitSphere, 3);
        }
        float moved = Vector3.Distance(p.transform.position, lastPlayerPos);
        lastPlayerPos = p.transform.position;
        if (GetCount("beacon") > 0 && (beaconTimer -= dt) <= 0f)
        {
            beaconTimer = Mathf.Max(1f, 4f / GetCount("beacon"));
            PlayerHealth3D health = p.GetComponent<PlayerHealth3D>();
            if (health != null) health.HealHearts(1);
        }
        if (GetCount("energy_core") > 0 && (energyCoreTimer -= dt) <= 0f)
        {
            energyCoreTimer = Mathf.Max(0.5f, 2.5f / GetCount("energy_core"));
            LaunchAtNearest(p.transform.position + Vector3.up * 1.6f, 24, Color.cyan, 0f);
        }
        if (GetCount("sucky_magnet") > 0 && (magnetTimer -= dt) <= 0f)
        {
            magnetTimer = Mathf.Max(2f, 8f / GetCount("sucky_magnet"));
            foreach (XpOrb3D orb in UnityEngine.Object.FindObjectsByType<XpOrb3D>())
                if (orb != null && Vector3.Distance(orb.transform.position, p.transform.position) < 35f)
                    orb.Attract();
        }

        // Detección de estar quieto (Campfire & Idle Juice)
        if (moved < dt * .15f)
        {
            idleTimer += dt;
            if (GetCount("campfire") > 0)
            {
                campfireTimer += dt;
                if (campfireTimer >= 6.0f)
                {
                    campfireTimer = 0f;
                    PlayerHealth3D ph = UnityEngine.Object.FindAnyObjectByType<PlayerHealth3D>();
                    if (ph != null) ph.HealHearts(GetCount("campfire"));
                }
            }
        }
        else
        {
            idleTimer = 0f;
            campfireTimer = 0f;

            // Golden Sneakers: acumula puntos al correr (1 de oro cada 10 unidades)
            if (GetCount("golden_sneakers") > 0)
            {
                sneakerGoldDistance += moved * 0.10f * GetCount("golden_sneakers");
                int earned = Mathf.FloorToInt(sneakerGoldDistance);
                if (earned > 0 && GameManager.Instance != null)
                {
                    GameManager.Instance.AddGold(earned);
                    sneakerGoldDistance -= earned;
                }
            }

            // Bob (Dead): cada 14 unidades invoca a Bob aliado
            if (GetCount("bob_dead") > 0)
            {
                distanceWalkedAccumulator += moved;
                if (distanceWalkedAccumulator >= 14f)
                {
                    distanceWalkedAccumulator -= 14f;
                    PlayerSummons summons = UnityEngine.Object.FindAnyObjectByType<PlayerSummons>();
                    if (summons == null) summons = p.AddComponent<PlayerSummons>();
                    for (int copy = 0; copy < GetCount("bob_dead"); copy++) summons.SpawnSkeleton(p.transform.position - p.transform.forward * 1.5f);
                }
            }
        }
    }

    // ── RENDERIZADO DEL MODAL DE COFRE / RULETA DE GALERÍA ─────
    void OnGUI()
    {
        if (!chestMenuOpen || rouletteReel == null || rouletteReel.Count == 0) return;

        float sw = Screen.width;
        float sh = Screen.height;
        float cx = sw / 2f;
        float cy = sh / 2f - 30f; // Ligeramente subido para dejar espacio abajo

        // 1. Fondo oscurecido con viñeta
        GUI.color = new Color(0.03f, 0.04f, 0.07f, 0.96f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);

        // Resplandor ambiental de color de rareza si ya se reveló
        if (rouletteState == RouletteState.Revealed && wonRelic != null)
        {
            Color glow = wonRelic.bannerColor;
            GUI.color = new Color(glow.r, glow.g, glow.b, 0.12f);
            GUI.DrawTexture(new Rect(cx - 380f, cy - 200f, 760f, 400f), Texture2D.whiteTexture);
        }

        // 2. Encabezados y títulos
        GUIStyle titleSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleSt.normal.textColor = (rouletteState == RouletteState.Revealed && wonRelic != null)
            ? wonRelic.bannerColor
            : new Color(1f, 0.85f, 0.2f);
        GUI.color = Color.white;

        string titleMsg = (rouletteState == RouletteState.Revealed && wonRelic != null)
            ? "⭐ ¡HAS OBTENIDO UN OBJETO MEGABONK! ⭐"
            : "🎰 RULETA DE OBJETOS MEGABONK 🎰";
        GUI.Label(new Rect(cx - 450f, cy - 205f, 900f, 38f), titleMsg, titleSt);

        GUIStyle subSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter
        };
        subSt.normal.textColor = new Color(0.85f, 0.92f, 1f, 0.9f);
        string subMsg = (rouletteState == RouletteState.Revealed && wonRelic != null)
            ? $"¡{wonRelic.name.ToUpper()} DESBLOQUEADO Y AÑADIDO AL ARSENAL!"
            : $"🍀 Suerte Actual: +{luckPercent:0.#}%  •  Girando galería de reliquias y deteniéndose...";
        GUI.Label(new Rect(cx - 450f, cy - 165f, 900f, 24f), subMsg, subSt);

        // 3. Geometría y cálculo de la Ruleta
        float cardW = 160f;
        float cardH = 220f;
        float cardGap = 16f;
        float slotStep = cardW + cardGap; // 176px por ranura
        float targetScroll = winningIndex * slotStep;

        float p = Mathf.Clamp01(spinTimer / spinDuration);
        // Desaceleración cuártica: Comienza vertiginosa y va frenando suavemente
        float ease = 1f - Mathf.Pow(1f - p, 4f);
        float currentScroll = ease * targetScroll;

        // Franja de fondo de la ruleta
        float trackY = cy - cardH / 2f - 18f;
        float trackH = cardH + 36f;
        GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
        GUI.DrawTexture(new Rect(0, trackY, sw, trackH), Texture2D.whiteTexture);

        // Líneas doradas superior e inferior de la pista
        GUI.color = new Color(0.35f, 0.42f, 0.52f, 0.7f);
        GUI.DrawTexture(new Rect(0, trackY, sw, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, trackY + trackH - 2f, sw, 2f), Texture2D.whiteTexture);

        // 4. Dibujar las tarjetas deslizantes de la ruleta
        float startItemX = cx - (cardW / 2f);
        for (int k = 0; k < rouletteReel.Count; k++)
        {
            float ix = startItemX + (k * slotStep) - currentScroll;
            if (ix + cardW < -60 || ix > sw + 60) continue; // Culling fuera de pantalla

            RelicData r = rouletteReel[k];
            bool isCenterWinner = (rouletteState == RouletteState.Revealed && k == winningIndex);
            DrawReelCard(new Rect(ix, cy - cardH / 2f, cardW, cardH), r, isCenterWinner);
        }

        // 5. Agujas indicadoras doradas centrales (Pointer)
        float needlePulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 8f);
        Color needleColor = (rouletteState == RouletteState.Revealed && wonRelic != null)
            ? wonRelic.bannerColor
            : new Color(1f, 0.85f, 0.2f, needlePulse);

        GUIStyle needleSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        needleSt.normal.textColor = needleColor;

        // Flecha superior ▼
        GUI.Label(new Rect(cx - 20f, cy - cardH / 2f - 38f, 40f, 32f), "▼", needleSt);
        // Flecha inferior ▲
        GUI.Label(new Rect(cx - 20f, cy + cardH / 2f + 6f, 40f, 32f), "▲", needleSt);

        // Guías verticales doradas enmarcando la ranura central ganadora
        GUI.color = needleColor;
        GUI.DrawTexture(new Rect(cx - cardW / 2f - 2f, cy - cardH / 2f - 8f, 3f, cardH + 16f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx + cardW / 2f - 1f, cy - cardH / 2f - 8f, 3f, cardH + 16f), Texture2D.whiteTexture);

        // 6. Sección inferior: Detalle del objeto o consejo
        if (rouletteState == RouletteState.Spinning)
        {
            GUIStyle hintSt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            hintSt.normal.textColor = new Color(0.65f, 0.72f, 0.82f);
            GUI.Label(new Rect(cx - 400f, cy + cardH / 2f + 42f, 800f, 24f),
                "La ruleta se está deteniendo gradualmente... Presiona [ ESPACIO ] para saltar al resultado", hintSt);
        }
        else if (rouletteState == RouletteState.Revealed && wonRelic != null)
        {
            DrawWinningShowcase(cx, cy + cardH / 2f + 35f, wonRelic);
        }
    }

    private void DrawReelCard(Rect r, RelicData relic, bool isWinner)
    {
        // Fondo con tinte sutil de su rareza
        Color bgCol = isWinner
            ? new Color(relic.bannerColor.r * 0.35f, relic.bannerColor.g * 0.35f, relic.bannerColor.b * 0.35f, 0.98f)
            : new Color(0.08f, 0.10f, 0.14f, 0.94f);
        GUI.color = bgCol;
        GUI.DrawTexture(r, Texture2D.whiteTexture);

        // Borde superior de rareza
        GUI.color = relic.bannerColor;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 4f), Texture2D.whiteTexture);

        // Bordes de la tarjeta
        float bThick = isWinner ? 2.5f : 1f;
        GUI.color = isWinner ? Color.white : new Color(relic.bannerColor.r * 0.5f, relic.bannerColor.g * 0.5f, relic.bannerColor.b * 0.5f, 0.55f);
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, bThick), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.yMax - bThick, r.width, bThick), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.y, bThick, r.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.xMax - bThick, r.y, bThick, r.height), Texture2D.whiteTexture);

        // Badge de rareza
        string rarityLabel = relic.rarity == RelicRarity.Common ? "🟢 COMÚN" :
                             relic.rarity == RelicRarity.Uncommon ? "🔵 POCO COMÚN" :
                             relic.rarity == RelicRarity.Rare ? "🟣 RARO" : "🟡 LEGENDARIO";
        GUI.color = relic.bannerColor;
        GUI.DrawTexture(new Rect(r.x + 8f, r.y + 10f, r.width - 16f, 18f), Texture2D.whiteTexture);
        GUIStyle rarSt = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        rarSt.normal.textColor = Color.black;
        GUI.Label(new Rect(r.x + 8f, r.y + 10f, r.width - 16f, 18f), rarityLabel, rarSt);

        // Icono gigante
        GUIStyle iconSt = new GUIStyle(GUI.skin.label) { fontSize = 42, alignment = TextAnchor.MiddleCenter };
        GUI.color = Color.white;
        if (!MegabonkIconProvider.DrawItemIcon(new Rect(r.x + r.width * 0.25f, r.y + 34f, r.width * 0.5f, 62f), relic.id))
            GUI.Label(new Rect(r.x, r.y + 36f, r.width, 56f), relic.icon, iconSt);

        // Nombre
        GUIStyle nameSt = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        nameSt.normal.textColor = relic.bannerColor;
        GUI.Label(new Rect(r.x + 6f, r.y + 98f, r.width - 12f, 40f), relic.name, nameSt);

        // Efecto corto / resumen
        GUIStyle descSt = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        descSt.normal.textColor = new Color(0.85f, 0.9f, 0.95f, 0.85f);
        string shortDesc = relic.description.Length > 65 ? (relic.description.Substring(0, 62) + "...") : relic.description;
        GUI.Label(new Rect(r.x + 8f, r.y + 144f, r.width - 16f, 66f), shortDesc, descSt);
    }

    private void DrawWinningShowcase(float cx, float startY, RelicData relic)
    {
        float panelW = 580f;
        float panelH = 135f;
        Rect pRect = new Rect(cx - panelW / 2f, startY, panelW, panelH);

        // Fondo del panel de detalle
        GUI.color = new Color(0.07f, 0.09f, 0.14f, 0.95f);
        GUI.DrawTexture(pRect, Texture2D.whiteTexture);

        // Borde superior coloreado por rareza
        GUI.color = relic.bannerColor;
        GUI.DrawTexture(new Rect(pRect.x, pRect.y, pRect.width, 3f), Texture2D.whiteTexture);

        // Borde general
        GUI.color = new Color(relic.bannerColor.r * 0.6f, relic.bannerColor.g * 0.6f, relic.bannerColor.b * 0.6f, 0.5f);
        GUI.DrawTexture(new Rect(pRect.x, pRect.yMax - 1f, pRect.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(pRect.x, pRect.y, 1f, pRect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(pRect.xMax - 1f, pRect.y, 1f, pRect.height), Texture2D.whiteTexture);

        // Icono + Nombre + Rareza
        GUIStyle hSt = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        hSt.normal.textColor = relic.bannerColor;
        if (!MegabonkIconProvider.DrawItemIcon(new Rect(pRect.x + 18f, pRect.y + 8f, 34f, 34f), relic.id))
            GUI.Label(new Rect(pRect.x + 18f, pRect.y + 10f, 34f, 26f), relic.icon, hSt);
        GUI.Label(new Rect(pRect.x + 58f, pRect.y + 10f, panelW - 76f, 26f), relic.name.ToUpper(), hSt);

        // Descripción completa de Efecto
        GUIStyle dSt = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.UpperLeft, wordWrap = true };
        dSt.normal.textColor = new Color(0.92f, 0.95f, 0.98f);
        GUI.Label(new Rect(pRect.x + 18f, pRect.y + 40f, panelW - 36f, 52f), relic.description, dSt);

        // Lore / desbloqueo
        GUIStyle lSt = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleLeft };
        lSt.normal.textColor = new Color(0.6f, 0.65f, 0.75f);
        GUI.Label(new Rect(pRect.x + 18f, pRect.y + 96f, panelW - 36f, 22f), "Desbloqueo: " + ExpeditionProgress.Requirement(relic), lSt);

        // Botón grande interactivo para reclamar
        Rect btnRect = new Rect(cx - 160f, startY + panelH + 16f, 320f, 44f);
        bool btnHover = btnRect.Contains(Event.current.mousePosition);

        GUI.color = btnHover ? relic.bannerColor : new Color(relic.bannerColor.r * 0.45f, relic.bannerColor.g * 0.45f, relic.bannerColor.b * 0.45f, 0.9f);
        GUI.DrawTexture(btnRect, Texture2D.whiteTexture);

        GUIStyle btnSt = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        btnSt.normal.textColor = btnHover ? Color.black : Color.white;
        GUI.Label(btnRect, "[ ESPACIO / ENTER ] RECLAMAR OBJETO", btnSt);

        if (Event.current.type == EventType.MouseDown && btnHover)
        {
            ClaimWonRelic();
            Event.current.Use();
        }
    }
}
