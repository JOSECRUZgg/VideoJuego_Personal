using UnityEngine;
using System.Collections;

/// <summary>
/// Cofre de objetos: cobro, animación original, recompensa única y estado abierto.
/// </summary>
public class BossChest3D : MonoBehaviour
{
    public float interactionDistance = 3.5f;
    public bool isBossChest = true;
    public int goldCost;

    private Transform player;
    private bool opened;
    private bool keyChecked, keyFree;
    private Vector3 basePos;
    private GameObject chestModel;
    private Vector3 chestVisualBaseLocalPosition;
    private Animator chestAnimator;
    private static BossChest3D openingChest;
    private bool rewardPresented;
    public int CoopId { get; set; }
    public bool RemoteMirror { get; set; }
    public bool IsOpened => opened;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOpening() { openingChest = null; }

    int EffectiveGoldCost
    {
        get
        {
            if (isBossChest || goldCost <= 0 || MegabonkRelicManager.Instance == null) return goldCost;
            return goldCost + MegabonkRelicManager.Instance.chestsOpenedCount * 15;
        }
    }

    public static BossChest3D Spawn(Vector3 position, bool isBoss = true)
    {
        GameObject chestObj = new GameObject(isBoss ? "BossChest_Legendary" : "LootChest_Elite");
        chestObj.transform.position = position;
        BossChest3D chest = chestObj.AddComponent<BossChest3D>();
        chest.isBossChest = isBoss;
        return chest;
    }

    public static BossChest3D Spawn(Vector3 position)
    {
        return Spawn(position, true);
    }

    void Start()
    {
        basePos = transform.position;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        BuildModel();
    }

    void BuildModel()
    {
        var original = OriginalContentCatalog.Instance;
        if (original != null && original.animatedChest != null)
        {
            chestModel = OriginalModel.Create(original.animatedChest, transform, 1.3f);
            if (chestModel != null)
            {
                chestAnimator = chestModel.GetComponentInChildren<Animator>();
                if (chestAnimator != null && original.chestController != null)
                {
                    chestAnimator.runtimeAnimatorController = original.chestController;
                    chestAnimator.Rebind(); chestAnimator.Play("Base Layer.Idle",0,0); chestAnimator.Update(0);
                    // Encajar el estado cerrado; el prefab OpenChest trae la pose de abierto.
                    if (OriginalModel.BoundsOf(chestModel,out var bounds))
                    {
                        if(bounds.size.y > .001f) chestModel.transform.localScale *= 1.3f / bounds.size.y;
                        OriginalModel.BoundsOf(chestModel,out bounds);
                        chestModel.transform.position += Vector3.up * (transform.position.y - bounds.min.y);
                    }
                }
                chestVisualBaseLocalPosition = chestModel.transform.localPosition;
                return;
            }
        }
        chestModel = ExtractedVisuals.Attach(isBossChest ? "ChestFree" : "Chest", transform, 1.3f);
        if (chestModel != null)
        {
            chestVisualBaseLocalPosition = chestModel.transform.localPosition;
            return;
        }
        chestModel = new GameObject("VisualChest");
        chestModel.transform.SetParent(transform);
        chestModel.transform.localPosition = Vector3.zero;
        chestVisualBaseLocalPosition = chestModel.transform.localPosition;

        // Base del cofre
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Box";
        box.transform.SetParent(chestModel.transform);
        box.transform.localPosition = Vector3.zero;
        box.transform.localScale = isBossChest ? new Vector3(1.3f, 0.75f, 0.9f) : new Vector3(1.1f, 0.65f, 0.75f);
        Destroy(box.GetComponent<Collider>());

        Renderer rendBox = box.GetComponent<Renderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Color baseCol = isBossChest ? new Color(0.85f, 0.08f, 0.12f) : new Color(0.12f, 0.35f, 0.85f);
        Material matBox = RuntimeMaterialCache.Get(s, baseCol, baseCol * 1.5f);
        rendBox.sharedMaterial = matBox;

        // Tapa
        GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lid.name = "Lid";
        lid.transform.SetParent(chestModel.transform);
        lid.transform.localPosition = new Vector3(0f, isBossChest ? 0.44f : 0.38f, 0f);
        lid.transform.localScale = isBossChest ? new Vector3(1.4f, 0.28f, 1.0f) : new Vector3(1.2f, 0.22f, 0.85f);
        Destroy(lid.GetComponent<Collider>());

        Renderer rendLid = lid.GetComponent<Renderer>();
        Color lidCol = isBossChest ? new Color(1f, 0.85f, 0.2f) : new Color(0.2f, 0.9f, 0.5f);
        Material matLid = RuntimeMaterialCache.Get(s, lidCol, lidCol * 2f);
        rendLid.sharedMaterial = matLid;

        // Herrajes y cerradura: silueta reconocible desde la cámara elevada.
        float size = isBossChest ? 1.15f : 1f;
        AddDetail("LeftBand", new Vector3(-0.38f, 0.08f, 0f) * size, new Vector3(0.10f, 0.85f, 0.86f) * size, matLid);
        AddDetail("RightBand", new Vector3(0.38f, 0.08f, 0f) * size, new Vector3(0.10f, 0.85f, 0.86f) * size, matLid);
        AddDetail("Lock", new Vector3(0f, 0.18f, -0.44f) * size, new Vector3(0.22f, 0.26f, 0.10f) * size, matLid);

        // Luz de aura del cofre
        GameObject lightObj = new GameObject("ChestLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.up * 0.8f;
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = isBossChest ? new Color(1f, 0.85f, 0.2f) : new Color(0.2f, 0.8f, 1f);
        l.range = isBossChest ? 7.5f : 5.0f;
        l.intensity = isBossChest ? 3.5f : 2.2f;
    }

    void Update()
    {
        if (opened || Time.timeScale <= 0f) return;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            return;
        }

        // Mantener la base en el suelo; solo la tapa se mueve mediante su rig.
        if (chestModel != null)
        {
            chestModel.transform.localPosition = chestVisualBaseLocalPosition;
        }

        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= interactionDistance)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (RemoteMirror && CoopRun.IsGuest) CoopRun.RequestChest(CoopId);
                else OpenChest();
            }
        }
    }

    void OpenChest()
    {
        if (opened || openingChest != null || MegabonkRelicManager.Instance == null || MegabonkRelicManager.Instance.chestMenuOpen) return;
        int cost = EffectiveGoldCost;
        if (!keyChecked && MegabonkBuild.Relics("key") > 0)
        {
            keyChecked = true;
            keyFree = Random.value < Mathf.Min(1f, .1f * MegabonkBuild.Relics("key"));
        }
        if (keyFree) cost = 0;
        if (GameManager.Instance == null || !GameManager.Instance.TrySpendGold(cost)) return;
        opened = true;
        openingChest = this;
        StartCoroutine(OpenSequence());
    }

    public void OpenFromRemote() { if (CoopRun.IsHost) OpenChest(); }
    public void SetRemoteOpened(bool value)
    {
        if (RemoteMirror && value && !opened)
        {
            opened = true;
            if (chestAnimator != null && chestAnimator.HasState(0, Animator.StringToHash("Base Layer.Opened")))
                chestAnimator.Play("Base Layer.Opened", 0, 0);
        }
    }
    public void PresentRemoteReward()
    {
        if (!RemoteMirror || rewardPresented) return;
        rewardPresented = true;
        opened = true;
        MegabonkRelicManager.Instance?.OpenChest(isBossChest);
    }

    IEnumerator OpenSequence()
    {
        var originals = OriginalContentCatalog.Instance;
        if (originals != null && originals.chestOpenSound != null)
            AudioSource.PlayClipAtPoint(originals.chestOpenSound,transform.position,.65f);
        if (chestAnimator != null && chestAnimator.HasState(0,Animator.StringToHash("Base Layer.Open")))
        {
            chestAnimator.Play("Base Layer.Open",0,0); chestAnimator.Update(0);
            // Conservar velocidad y transición Open -> Opened del controller original.
            yield return null;
            while (chestAnimator != null && (chestAnimator.GetCurrentAnimatorStateInfo(0).IsName("Open") || chestAnimator.IsInTransition(0)))
                yield return null;
        }
        if (!rewardPresented && MegabonkRelicManager.Instance != null)
        {
            while (MegabonkRelicManager.Instance.chestMenuOpen || Time.timeScale <= 0) yield return null;
            rewardPresented = true;
            ExtractedVisuals.Burst("GoldBurstSmall",transform.position);
            CoopRun.ShareChestReward(CoopId, isBossChest);
            MegabonkRelicManager.Instance.OpenChest(isBossChest, () =>
            {
                if (originals != null && originals.chestTakeSound != null)
                    AudioSource.PlayClipAtPoint(originals.chestTakeSound,transform.position,.65f);
                // El cofre permanece abierto y ya no puede cobrar ni recompensar de nuevo.
            });
        }
        if (openingChest == this) openingChest = null;
    }
    void OnDestroy() { if (openingChest == this) openingChest = null; }

    void AddDetail(string label, Vector3 position, Vector3 scale, Material material)
    {
        GameObject detail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        detail.name = label;
        detail.transform.SetParent(chestModel.transform, false);
        detail.transform.localPosition = position;
        detail.transform.localScale = scale;
        detail.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(detail.GetComponent<Collider>());
    }

    private GUIStyle cachedPromptStyle;

    void OnGUI()
    {
        if (opened || player == null || Time.timeScale <= 0f) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= interactionDistance)
        {
            // Prompt interactivo en pantalla
            float sw = Screen.width;
            float sh = Screen.height;
            float w = 460f;
            float h = 42f;
            float x = (sw - w) / 2f;
            float y = sh * 0.65f;

            GUI.color = new Color(0.06f, 0.07f, 0.12f, 0.92f);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

            Color borderCol = isBossChest ? new Color(1f, 0.85f, 0.2f) : new Color(0.2f, 0.85f, 0.5f);
            GUI.color = borderCol;
            GUI.DrawTexture(new Rect(x, y, w, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y + h - 2f, w, 2f), Texture2D.whiteTexture);

            if (cachedPromptStyle == null)
            {
                cachedPromptStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }
            cachedPromptStyle.normal.textColor = borderCol;

            GUI.color = Color.white;
            int cost = EffectiveGoldCost;
            bool affordable = GameManager.Instance != null && GameManager.Instance.gold >= cost;
            string prompt = cost > 0
                ? $"[ E ] COFRE · {cost} ORO" + (affordable ? " · ABRIR" : " · ORO INSUFICIENTE")
                : "[ E ] COFRE DE RECOMPENSA · GRATIS";
            GUI.Label(new Rect(x, y, w, h), prompt, cachedPromptStyle);
        }
    }
}
