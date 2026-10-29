using UnityEngine;

/// <summary>Objeto interactivo de etapa: vasijas, santuarios, arco de jefe y portal.</summary>
public class StageInteractable : MonoBehaviour
{
    public enum Kind { Pot, ChargeShrine, GreedShrine, BossCurseShrine, Microwave, BossGate, Portal, HealingShrine, WindShrine }
    public Kind kind;
    public bool completed;
    public float ChargeProgress { get; private set; }
    public int CoopId { get; set; }
    public bool RemoteMirror { get; private set; }
    public bool IsGolden => golden;
    float remoteChargingUntil;

    private StageDirector director;
    private Transform player;
    private Renderer visual;
    private float useDistance = 3.2f;
    private bool golden;

    public void Initialize(Kind newKind, StageDirector newDirector)
    {
        kind = newKind;
        director = newDirector;
        golden = kind == Kind.ChargeShrine && Random.value < 0.18f;
        BuildVisual();
    }

    public void InitializeRemote(Kind newKind, bool isGolden)
    {
        RemoteMirror = true;
        kind = newKind;
        golden = isGolden;
        director = StageDirector.Instance;
        BuildVisual();
    }

    public void SetRemoteProgress(float progress, bool done)
    { ChargeProgress = Mathf.Clamp01(progress); completed = done; }

    public void ChargeFromRemote()
    {
        if (kind == Kind.ChargeShrine && !completed) remoteChargingUntil = Time.time + .18f;
    }

    public void ActivateFromRemote()
    {
        if (completed || RemoteMirror) return;
        if (kind == Kind.Microwave) { CoopRun.OpenPartnerMicrowave(CoopId); return; }
        Activate();
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
    }

    void BuildVisual()
    {
        GameObject lanternPrefab = kind == Kind.ChargeShrine
            ? Resources.Load<GameObject>("Megabonk/Stage/Lantern/SM_Lantern_01") : null;
        GameObject vasePrefab = kind == Kind.Pot
            ? Resources.Load<GameObject>("Megabonk/Stage/Vase/Jarron") : null;
        string importedName = kind == Kind.Pot ? "PotSmall" :
            kind == Kind.ChargeShrine ? "ChargeShrine" :
            kind == Kind.GreedShrine ? "GreedShrine" :
            kind == Kind.BossCurseShrine ? "CursedShrine" :
            kind == Kind.Microwave ? "Microwave" :
            kind == Kind.HealingShrine ? "ShrineMaoi" :
            kind == Kind.WindShrine ? "ShrineBalance" :
            kind == Kind.BossGate ? "BossSpawner" :
            kind == Kind.Portal ? "PlayerSpawnPortal" : null;
        // Prioridad a los modelos que se añadieron expresamente para vasija y
        // santuario; antes el prefab extraído provocaba un retorno prematuro.
        GameObject importedModel = importedName != null && lanternPrefab == null && vasePrefab == null
            ? ExtractedVisuals.Attach(importedName, transform, kind == Kind.Pot ? 1.4f : kind == Kind.Microwave ? 1.8f : 3.8f)
            : null;
        if (importedModel != null)
        {
            visual = importedModel.GetComponentInChildren<Renderer>();
            var hitbox = gameObject.AddComponent<BoxCollider>();
            hitbox.isTrigger = true;
            hitbox.size = new Vector3(1.4f, kind == Kind.Pot ? 1.4f : 3f, 1.4f);
            hitbox.center = Vector3.up * hitbox.size.y * .5f;
            return;
        }
        GameObject mesh = GameObject.CreatePrimitive(kind == Kind.Pot ? PrimitiveType.Cylinder : PrimitiveType.Cube);
        mesh.transform.SetParent(transform, false);
        mesh.transform.localScale = kind == Kind.Pot ? new Vector3(0.9f, 0.8f, 0.9f) : new Vector3(1.6f, 1.8f, 1.6f);
        if (kind == Kind.Portal) mesh.transform.localScale = new Vector3(2.5f, 3.5f, 0.5f);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Color color = GetColor();
        Renderer customVisual = null;
        mesh.transform.localPosition = Vector3.up * (kind == Kind.Pot ? 0.8f : 0.9f);
        if (kind == Kind.BossGate || kind == Kind.Portal)
        {
            mesh.GetComponent<Renderer>().enabled = false;
            mesh.transform.localPosition = Vector3.up * 2f;
            Material stone = RuntimeMaterialCache.Get(shader, new Color(0.28f, 0.32f, 0.36f), Color.black);
            AddPart("LeftPillar", new Vector3(-1.7f, 2f, 0f), new Vector3(0.7f, 4f, 0.9f), stone);
            AddPart("RightPillar", new Vector3(1.7f, 2f, 0f), new Vector3(0.7f, 4f, 0.9f), stone);
            AddPart("Arch", new Vector3(0f, 4f, 0f), new Vector3(4.1f, 0.7f, 1f), stone);
            Material glow = RuntimeMaterialCache.Get(shader, color, color * 2f);
            AddPart("Rune", new Vector3(0f, 4f, -0.55f), new Vector3(0.45f, 0.45f, 0.15f), glow);
            if (kind == Kind.Portal)
                AddPart("PortalEnergy", new Vector3(0f, 1.9f, 0f), new Vector3(2.6f, 3.6f, 0.08f), glow);
        }
        if (kind == Kind.Microwave)
        {
            mesh.transform.localScale = new Vector3(2f, 1.3f, 1.2f);
            Material dark = RuntimeMaterialCache.Get(shader, new Color(0.035f, 0.06f, 0.08f), Color.black);
            AddPart("GlassDoor", new Vector3(-0.18f, 0.9f, -0.62f), new Vector3(1.25f, 0.85f, 0.07f), dark);
            AddPart("ControlPanel", new Vector3(0.73f, 0.9f, -0.63f), new Vector3(0.22f, 0.65f, 0.08f), dark);
        }

        // El Santuario de Carga usa la linterna de arte proporcionada. La base
        // se conserva como altar/collider visible mientras Unity importa el FBX.
        if (kind == Kind.ChargeShrine)
        {
            mesh.name = "ChargeShrineBase";
            mesh.transform.localPosition = Vector3.up * 0.175f;
            mesh.transform.localScale = new Vector3(1.45f, 0.35f, 1.45f);
            if (lanternPrefab != null)
            {
                GameObject lantern = Instantiate(lanternPrefab, transform);
                lantern.name = "ChargeShrineLantern";
                // El mesh original mide ~28 unidades: 0.14 lo deja a una altura
                // de santuario visible sin atravesar el suelo.
                lantern.transform.localPosition = new Vector3(0f, 0.18f, 0f);
                OrientTallModel(lantern);
                // Aumentada para que la linterna se lea claramente como el
                // punto de interés del santuario desde la cámara cenital.
                FitModel(lantern, 4.2f, .35f);

                Texture2D lanternTexture = Resources.Load<Texture2D>("Megabonk/Stage/Lantern/SM_Lantern_01_DefaultMaterial_BaseColor");
                Material lanternMaterial = new Material(shader);
                lanternMaterial.color = Color.white;
                lanternMaterial.SetColor("_BaseColor", Color.white);
                lanternMaterial.SetTexture("_MainTex", lanternTexture);
                lanternMaterial.SetTexture("_BaseMap", lanternTexture);
                foreach (Renderer renderer in lantern.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterial = lanternMaterial;
            }
        }

        // Vasija artística real convertida desde el archivo Rhino proporcionado.
        // El cilindro original queda únicamente como trigger invisible para
        // conservar una detección de impacto estable para el disparo.
        if (kind == Kind.Pot)
        {
            if (vasePrefab != null)
            {
                mesh.name = "PotHitbox";
                Renderer hitboxRenderer = mesh.GetComponent<Renderer>();
                if (hitboxRenderer != null) hitboxRenderer.enabled = false;
                mesh.transform.localPosition = Vector3.up * .7f;
                mesh.transform.localScale = new Vector3(1.15f, .7f, 1.15f);

                GameObject vase = Instantiate(vasePrefab, transform);
                vase.name = "JarronModel";
                vase.transform.localPosition = Vector3.zero;
                OrientTallModel(vase);
                FitModel(vase, 1.4f, 0f);
                Material vaseMaterial = RuntimeMaterialCache.Get(
                    shader, new Color(0.48f, 0.19f, 0.08f), new Color(0.12f, 0.025f, 0.008f));
                foreach (Renderer renderer in vase.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterial = vaseMaterial;
                customVisual = vase.GetComponentInChildren<Renderer>();
            }
        }

        if (kind == Kind.HealingShrine)
        {
            mesh.name = "HealingShrineBase";
            mesh.transform.localScale = new Vector3(1.4f, 1.6f, 1.4f);
            Material emeraldGlow = RuntimeMaterialCache.Get(shader, color, color * 2.8f);
            AddPart("HealingCrystal", new Vector3(0f, 1.8f, 0f), new Vector3(0.55f, 0.75f, 0.55f), emeraldGlow);
        }

        if (kind == Kind.WindShrine)
        {
            mesh.name = "WindShrineBase";
            mesh.transform.localScale = new Vector3(1.5f, 1.3f, 1.5f);
            Material windGlow = RuntimeMaterialCache.Get(shader, color, color * 2.8f);
            AddPart("WindOrb", new Vector3(0f, 1.55f, 0f), new Vector3(0.65f, 0.65f, 0.65f), windGlow);
        }

        Collider collider = mesh.GetComponent<Collider>();
        if (collider == null) collider = mesh.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        visual = customVisual != null ? customVisual : mesh.GetComponentInChildren<Renderer>();
        if (visual != null && customVisual == null)
            visual.sharedMaterial = RuntimeMaterialCache.Get(shader, color, color * 1.4f);
        Light light = mesh.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = kind == Kind.Pot ? 0f : 8.5f;
        light.intensity = kind == Kind.Pot ? 0f : 2.5f;

        if (kind == Kind.ChargeShrine)
        {
            GameObject ring = new GameObject("ChargeCircle");
            ring.name = "ChargeCircle";
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            LineRenderer outline = ring.AddComponent<LineRenderer>();
            outline.useWorldSpace = true;
            outline.loop = true;
            outline.positionCount = 64;
            outline.widthMultiplier = .08f;
            outline.sharedMaterial = RuntimeMaterialCache.Get(shader, color, color * 1.7f);
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2f / 64;
                Vector3 p = transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * useDistance;
                if (ArenaEnricher.Instance != null) p = ArenaEnricher.Instance.PlaceOnGround(p);
                outline.SetPosition(i, p + Vector3.up * .08f);
            }
        }

        // Haces de luz celestiales visibles desde la distancia a lo largo del mapa gigante
        if (kind != Kind.Pot && kind != Kind.Portal && kind != Kind.BossGate)
            CreateBeaconBeam(color);
    }

    // Tamaño real y apoyo por límites del mesh, independiente de las unidades del exportador.
    static void OrientTallModel(GameObject model)
    {
        Quaternion source = model.transform.localRotation;
        Quaternion[] candidates = { source, Quaternion.Euler(-90f, 0f, 0f) * source,
            Quaternion.Euler(90f, 0f, 0f) * source, Quaternion.Euler(0f, 0f, 90f) * source,
            Quaternion.Euler(0f, 0f, -90f) * source };
        float best = -1f;
        Quaternion chosen = source;
        foreach (Quaternion candidate in candidates)
        {
            model.transform.localRotation = candidate;
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) break;
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float score = bounds.size.y / Mathf.Max(.01f, Mathf.Max(bounds.size.x, bounds.size.z));
            if (score > best) { best = score; chosen = candidate; }
        }
        model.transform.localRotation = chosen;
    }

    static void FitModel(GameObject model, float height, float baseHeight)
    {
        foreach (Collider col in model.GetComponentsInChildren<Collider>()) col.enabled = false;
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
        if (bounds.size.y > .001f) model.transform.localScale *= height / bounds.size.y;
        bounds = renderers[0].bounds;
        foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
        Vector3 ground = model.transform.parent.TransformPoint(Vector3.up * baseHeight);
        model.transform.position += new Vector3(ground.x - bounds.center.x, ground.y - bounds.min.y, ground.z - bounds.center.z);
    }

    void CreateBeaconBeam(Color color)
    {
        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.name = "BeaconBeam";
        beam.transform.SetParent(transform, false);
        beam.transform.localPosition = new Vector3(0f, 25f, 0f);
        beam.transform.localScale = new Vector3(0.32f, 25f, 0.32f);
        Collider beamCol = beam.GetComponent<Collider>();
        if (beamCol != null) Destroy(beamCol);
        Renderer beamRend = beam.GetComponent<Renderer>();
        if (beamRend != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            beamRend.sharedMaterial = RuntimeMaterialCache.Get(shader, new Color(color.r, color.g, color.b, 0.30f), color * 3.2f);
        }
    }

    void Update()
    {
        if (completed || Time.timeScale <= 0f) return;
        if (player == null) { GameObject p = GameObject.FindGameObjectWithTag("Player"); if (p != null) player = p.transform; return; }
        float distance = Vector3.Distance(player.position, transform.position);

        if (RemoteMirror && CoopRun.IsGuest)
        {
            if (distance <= useDistance)
            {
                if (kind == Kind.ChargeShrine) CoopRun.RequestCharge(CoopId);
                else if (Input.GetKeyDown(KeyCode.E)) CoopRun.RequestInteract(CoopId);
            }
            return;
        }

        if (kind == Kind.ChargeShrine && (distance <= useDistance ||
            CoopRun.IsHost && remoteChargingUntil > Time.time))
        {
            int wrenchCount = MegabonkRelicManager.Instance != null
                ? MegabonkRelicManager.Instance.GetCount("wrench") : 0;
            ChargeProgress = Mathf.Clamp01(ChargeProgress + Time.deltaTime * (1f + 0.04f * wrenchCount) / (golden ? 2f : 3f));
            if (ChargeProgress >= 1f)
            {
                completed = true;
                ExtractedVisuals.Burst("ChargeShrineFinishFx", transform.position);
                director.GrantChargeBlessing(golden);
                CoopRun.ShareShrineBlessing(golden);
                // Keep the completed altar as a landmark. The final encounter
                // also uses its lantern as shelter from the darkness blast.
            }
        }
        else if (distance <= useDistance && Input.GetKeyDown(KeyCode.E))
        {
            Activate();
        }
        else if (kind == Kind.ChargeShrine) ChargeProgress = 0f;
    }

    public void BreakPot()
    {
        if (kind != Kind.Pot || completed) return;
        if (RemoteMirror && CoopRun.IsGuest) { CoopRun.RequestInteract(CoopId); return; }
        completed = true;
        ExpeditionProgress.RecordPotBroken();
        ExtractedVisuals.Burst("PotBreakSmall", transform.position);
        ExtractedVisuals.Sound("PotBreak", transform.position);
        // Cada vasija entrega los tres recursos de exploración: plata persistente,
        // oro de la expedición y experiencia que alimenta las elecciones de nivel.
        int silver = Random.Range(1, 2);
        int gold = Random.Range(2, 5);
        int xp = Random.Range(3, 6);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddSilver(silver);
            GameManager.Instance.AddGold(gold);
        }
        if (XPSystem.Instance != null) XPSystem.Instance.AddXP(xp);
        Destroy(gameObject);
    }

    void AddPart(string label, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = label;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(part.GetComponent<Collider>());
    }

    void Activate()
    {
        switch (kind)
        {
            case Kind.Pot: BreakPot(); break;
            case Kind.GreedShrine: director.ApplyGreed(); CoopRun.ShareGreed(); Complete(); break;
            case Kind.BossCurseShrine: if (director.ActivateBossCurse()) Complete(); break;
            case Kind.Microwave:
                MicrowaveUI.Open(this);
                break;
            case Kind.BossGate: if (director.ActivateBossGate()) Complete(); break;
            case Kind.Portal: if (!CoopRun.Active || CoopRun.CanUsePortal(transform.position)) director.AdvanceStage(); break;
            case Kind.HealingShrine: director.ApplyHealingShrine(transform.position); CoopRun.ShareHealingShrine(transform.position); Complete(); break;
            case Kind.WindShrine: director.ApplyWindShrine(transform.position); CoopRun.WindPartner(); Complete(); break;
        }
    }

    public void Complete()
    {
        if (RemoteMirror && CoopRun.IsGuest) CoopRun.RequestCompleteInteract(CoopId);
        completed = true;
        Destroy(gameObject, 0.3f);
    }

    Color GetColor()
    {
        switch (kind)
        {
            case Kind.ChargeShrine: return golden ? new Color(1f, 0.75f, 0.12f) : new Color(0.2f, 1f, 0.45f);
            case Kind.GreedShrine: return new Color(0.95f, 0.75f, 0.12f);
            case Kind.BossCurseShrine: return new Color(0.8f, 0.15f, 0.85f);
            case Kind.HealingShrine: return new Color(0.18f, 0.96f, 0.42f);
            case Kind.WindShrine: return new Color(0.25f, 0.88f, 1f);
            case Kind.Microwave: return new Color(0.25f, 0.7f, 1f);
            case Kind.BossGate: return new Color(1f, 0.12f, 0.15f);
            case Kind.Portal: return new Color(0.25f, 0.55f, 1f);
            default: return new Color(0.75f, 0.35f, 0.12f);
        }
    }

    void OnGUI()
    {
        if (completed || player == null || Time.timeScale <= 0f) return;
        float distance = Vector3.Distance(player.position, transform.position);
        if (distance > useDistance) return;
        string label = kind == Kind.ChargeShrine
            ? $"CARGANDO SANTUARIO {Mathf.RoundToInt(ChargeProgress * 100f)}%"
            : "[ E ] " + GetInteractionName();
        GUI.Label(new Rect(Screen.width * 0.5f - 160f, Screen.height * 0.68f, 320f, 28f), label, GUI.skin.box);
    }

    string GetInteractionName()
    {
        switch (kind)
        {
            case Kind.GreedShrine: return "SANTUARIO DE CODICIA  •  +35 ORO / +5% DIFICULTAD";
            case Kind.BossCurseShrine: return "MALDICIÓN DE JEFE  •  JEFE EXTRA / COFRE EXTRA";
            case Kind.HealingShrine: return "SANTUARIO DE CURACIÓN  •  RESTAURAR 2 CORAZONES Y AURA VITAL";
            case Kind.WindShrine: return "SANTUARIO DEL VIENTO  •  TORBELLINOS, +40% VEL Y 4 TAILWIND";
            case Kind.Microwave: return "MICROONDAS  •  SACRIFICAR Y DUPLICAR OBJETO";
            case Kind.BossGate: return "ARCO DEL JEFE  •  INVOCAR";
            case Kind.Portal: return director != null && director.CurrentStage >= 3
                ? "PORTAL  •  JEFE FINAL" : "PORTAL  •  SIGUIENTE ETAPA";
            case Kind.Pot: return "VASija  •  ROMPER";
            default: return kind.ToString().ToUpper();
        }
    }
}
