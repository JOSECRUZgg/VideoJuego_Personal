using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>Host-authoritative LAN run. Only the local process owns its player's build and save.</summary>
public sealed class CoopRun : MonoBehaviour
{
    static CoopRun instance;
    static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
    public static bool Active => instance != null;
    public static bool IsHost => Active && LanLobby.Instance != null && LanLobby.Instance.Role == LanLobby.Seat.Host;
    public static bool IsGuest => Active && LanLobby.Instance != null && LanLobby.Instance.Role == LanLobby.Seat.Guest;
    public static int Seed => instance != null ? instance.seed : 0;
    public static Transform Partner => instance != null && instance.partner != null ? instance.partner.transform : null;
    public static bool PartnerDowned => instance != null && instance.partnerDowned;
    public static bool PersonalMenuOpen => instance != null && instance.personalMenu;
    public static bool IsReady => instance != null && instance.running;

    readonly Dictionary<EnemyHealth3D, int> hostIds = new Dictionary<EnemyHealth3D, int>();
    readonly Dictionary<int, EnemyHealth3D> hostEnemies = new Dictionary<int, EnemyHealth3D>();
    readonly Dictionary<int, EnemyHealth3D> mirrors = new Dictionary<int, EnemyHealth3D>();
    readonly Dictionary<StageInteractable, int> interactableIds = new Dictionary<StageInteractable, int>();
    readonly Dictionary<int, StageInteractable> hostInteractables = new Dictionary<int, StageInteractable>();
    readonly Dictionary<int, StageInteractable> interactableMirrors = new Dictionary<int, StageInteractable>();
    readonly Dictionary<BossChest3D, int> chestIds = new Dictionary<BossChest3D, int>();
    readonly Dictionary<int, BossChest3D> hostChests = new Dictionary<int, BossChest3D>();
    readonly Dictionary<int, BossChest3D> chestMirrors = new Dictionary<int, BossChest3D>();
    readonly Dictionary<XpOrb3D, int> orbIds = new Dictionary<XpOrb3D, int>();
    readonly Dictionary<int, XpOrb3D> hostOrbs = new Dictionary<int, XpOrb3D>();
    readonly Dictionary<int, XpOrb3D> orbMirrors = new Dictionary<int, XpOrb3D>();
    readonly HashSet<int> seen = new HashSet<int>();
    readonly List<int> stale = new List<int>();
    readonly StringBuilder snapshot = new StringBuilder(8192);
    int seed, nextEnemyId = 1, nextObjectId = 1;
    bool localLoaded, peerLoaded, running, partnerDowned, personalMenu, peerChoosing, localChoosing, announcedChoosing, defeatSent;
    bool localRestart, peerRestart;
    float nextPosition, nextWorld, nextEnemies, nextObjects, reviveProgress, reviveDamageTime, nextChargeRequest;
    int partnerHealth, partnerMaxHealth;
    GameObject partner;
    Vector3 partnerTarget;
    Quaternion partnerRotation;
    PlayerHealth3D localHealth;
    Transform localPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic() { instance = null; }

    public static void Begin(int sharedSeed)
    {
        if (Active || LanLobby.Instance == null || !LanLobby.Instance.Connected ||
            GameManager.Instance == null || GameManager.Instance.gameState != GameManager.GameState.MainMenu) return;
        var go = new GameObject("CoopRun");
        instance = go.AddComponent<CoopRun>();
        instance.seed = sharedSeed;
        LanLobby.Instance.RunMessage += instance.Receive;
        GameManager.Instance.StartGame();
    }

    void OnDestroy()
    {
        if (LanLobby.Instance != null) LanLobby.Instance.RunMessage -= Receive;
        if (partner != null) Destroy(partner);
        foreach (var mirror in mirrors.Values) if (mirror != null) Destroy(mirror.gameObject);
        foreach (var mirror in interactableMirrors.Values) if (mirror != null) Destroy(mirror.gameObject);
        foreach (var mirror in chestMirrors.Values) if (mirror != null) Destroy(mirror.gameObject);
        foreach (var mirror in orbMirrors.Values) if (mirror != null) Destroy(mirror.gameObject);
        mirrors.Clear();
        if (instance == this) instance = null;
    }

    void Update()
    {
        var lobby = LanLobby.Instance;
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (lobby == null || !lobby.Connected)
        {
            if (gm.gameState != GameManager.GameState.MainMenu) gm.ReturnToMainMenu();
            Destroy(gameObject);
            return;
        }
        if (!localLoaded)
        {
            if (gm.gameState != GameManager.GameState.Playing || UiTransition.Busy) return;
            localHealth = FindAnyObjectByType<PlayerHealth3D>();
            localPlayer = localHealth != null ? localHealth.transform : null;
            if (localPlayer == null) return;
            if (IsGuest)
            {
                var spawner = FindAnyObjectByType<EnemySpawner3D>();
                if (spawner != null) spawner.enabled = false;
                StageDirector.Instance?.ClearStageObjects();
                if (StageDirector.Instance != null) StageDirector.Instance.enabled = false;
            }
            CreatePartner();
            localLoaded = true;
            lobby.SendRun("RUN_LOADED");
            BeginIfReady();
        }
        if (!running) return;
        if (gm.gameState == GameManager.GameState.MainMenu) { Destroy(gameObject); return; }
        if (Input.GetKeyDown(KeyCode.X)) lobby.SendRun("RUN_PING", "HELP");
        if (Time.unscaledTime >= nextPosition)
        {
            nextPosition = Time.unscaledTime + .05f;
            SendPosition();
        }
        if (partner != null)
        {
            partner.transform.position = Vector3.Lerp(partner.transform.position, partnerTarget,
                1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
            partner.transform.rotation = Quaternion.Slerp(partner.transform.rotation, partnerRotation,
                1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
        }
        if (IsHost)
        {
            if (Time.unscaledTime >= nextWorld)
            { nextWorld = Time.unscaledTime + .2f; SendWorld(); }
            if (Time.unscaledTime >= nextEnemies)
            { nextEnemies = Time.unscaledTime + .12f; SendEnemies(); }
            if (Time.unscaledTime >= nextObjects)
            { nextObjects = Time.unscaledTime + .4f; SendObjects(); SendOrbs(); }
            if (!defeatSent && localHealth != null && localHealth.isDead && partnerDowned)
            {
                defeatSent = true;
                lobby.SendRun("RUN_DEFEAT");
                gm.OnPlayerDeath();
            }
        }
        HandleRevive();
        bool choosing = XPSystem.Instance != null && (XPSystem.Instance.skillMenuOpen || XPSystem.Instance.bossTomeMenuOpen);
        choosing |= ShrineRewardUI.IsOpen || MicrowaveUI.IsOpen ||
            (MegabonkRelicManager.Instance != null && MegabonkRelicManager.Instance.chestMenuOpen);
        localChoosing = choosing;
        if (choosing != announcedChoosing)
        {
            announcedChoosing = choosing;
            lobby.SendRun("RUN_CHOICE", choosing ? "1" : "0");
        }
        SyncChoicePause();
    }

    void BeginIfReady()
    {
        if (!IsHost || !localLoaded || !peerLoaded || running) return;
        LanLobby.Instance.SendRun("RUN_GO");
        Go();
    }

    void Go()
    {
        if (!localLoaded) return;
        running = true;
        SyncChoicePause();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void CreatePartner()
    {
        partner = new GameObject("LAN Companion");
        partnerTarget = localPlayer.position + Vector3.right * 3f;
        partner.transform.position = partnerTarget;
        partnerRotation = Quaternion.identity;
        var catalog = OriginalContentCatalog.Instance;
        if (catalog != null && catalog.characters != null)
            foreach (var hero in catalog.characters)
                if (hero != null && hero.displayName == LanLobby.Instance.PeerCharacter && hero.prefab != null)
                {
                    OriginalModel.Create(hero.prefab, partner.transform, 1.8f);
                    return;
                }
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "CompanionVisual";
        body.transform.SetParent(partner.transform, false);
        body.transform.localPosition = Vector3.up;
        Destroy(body.GetComponent<Collider>());
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        body.GetComponent<Renderer>().sharedMaterial = RuntimeMaterialCache.Get(shader, Color.cyan, Color.black);
    }

    void SendPosition()
    {
        if (localPlayer == null || localHealth == null) return;
        Vector3 p = localPlayer.position;
        var visual = localPlayer.GetComponent<PlayerCharacterVisual>();
        LanLobby.Instance.SendRun("RUN_POS", F(p.x), F(p.y), F(p.z), F(visual != null ? visual.FacingYaw : localPlayer.eulerAngles.y),
            localHealth.currentHealth.ToString(), localHealth.maxHealth.ToString(), localHealth.isDead ? "1" : "0");
    }

    void SendWorld()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        LanLobby.Instance.SendRun("RUN_WORLD", F(gm.gameTime), gm.gold.ToString(), gm.score.ToString(),
            gm.kills.ToString(), gm.finalSwarmActive ? "1" : "0", ((int)gm.gameState).ToString(),
            (StageDirector.Instance?.CurrentStage ?? 1).ToString(),
            F(StageDirector.Instance?.StageElapsed ?? 0), StageDirector.Instance?.BossGateActive == true ? "1" : "0",
            StageDirector.Instance?.PortalActive == true ? "1" : "0",
            StageDirector.Instance?.FinalEncounterActive == true ? "1" : "0");
    }

    void SendEnemies()
    {
        snapshot.Length = 0;
        seen.Clear();
        foreach (var enemy in EnemyHealth3D.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDying || !enemy.gameObject.activeInHierarchy) continue;
            if (!hostIds.TryGetValue(enemy, out int id))
            { id = nextEnemyId++; hostIds[enemy] = id; hostEnemies[id] = enemy; }
            seen.Add(id);
            var ai = enemy.GetComponent<EnemyAI3D>();
            var visual = enemy.GetComponentInChildren<ExtractedEnemyMeshVisual>();
            Vector3 p = enemy.transform.position;
            if (snapshot.Length > 0) snapshot.Append(';');
            snapshot.Append(id).Append(',').Append(ai != null ? (int)ai.enemyType : 0).Append(',')
                .Append(F(p.x)).Append(',').Append(F(p.y)).Append(',').Append(F(p.z)).Append(',')
                .Append(F(enemy.transform.eulerAngles.y)).Append(',').Append(enemy.currentHealth)
                .Append(',').Append(visual != null ? visual.ModelId : "");
        }
        stale.Clear();
        foreach (var pair in hostEnemies) if (!seen.Contains(pair.Key)) stale.Add(pair.Key);
        foreach (int id in stale)
        { var enemy = hostEnemies[id]; hostEnemies.Remove(id); if (enemy != null) hostIds.Remove(enemy); }
        LanLobby.Instance.SendRun("RUN_ENEMIES", snapshot.ToString());
    }

    void SendObjects()
    {
        snapshot.Length = 0;
        seen.Clear();
        foreach (var obj in FindObjectsByType<StageInteractable>(FindObjectsSortMode.None))
        {
            if (obj == null || obj.RemoteMirror) continue;
            if (!interactableIds.TryGetValue(obj, out int id))
            { id = nextObjectId++; interactableIds[obj] = id; hostInteractables[id] = obj; obj.CoopId = id; }
            seen.Add(id);
            var p = obj.transform.position;
            if (snapshot.Length > 0) snapshot.Append(';');
            snapshot.Append(id).Append(',').Append((int)obj.kind).Append(',').Append(F(p.x)).Append(',')
                .Append(F(p.y)).Append(',').Append(F(p.z)).Append(',')
                .Append(obj.completed ? 1 : 0).Append(',').Append(F(obj.ChargeProgress)).Append(',')
                .Append(obj.IsGolden ? 1 : 0);
        }
        stale.Clear();
        foreach (var pair in hostInteractables) if (!seen.Contains(pair.Key)) stale.Add(pair.Key);
        foreach (int id in stale)
        { var obj = hostInteractables[id]; hostInteractables.Remove(id); if (obj != null) interactableIds.Remove(obj); }
        LanLobby.Instance.SendRun("RUN_OBJECTS", snapshot.ToString());

        snapshot.Length = 0;
        seen.Clear();
        foreach (var chest in FindObjectsByType<BossChest3D>(FindObjectsSortMode.None))
        {
            if (chest == null || chest.RemoteMirror) continue;
            if (!chestIds.TryGetValue(chest, out int id))
            { id = nextObjectId++; chestIds[chest] = id; hostChests[id] = chest; chest.CoopId = id; }
            seen.Add(id);
            var p = chest.transform.position;
            if (snapshot.Length > 0) snapshot.Append(';');
            snapshot.Append(id).Append(',').Append(chest.isBossChest ? 1 : 0).Append(',')
                .Append(chest.goldCost).Append(',').Append(F(p.x)).Append(',').Append(F(p.y))
                .Append(',').Append(F(p.z)).Append(',').Append(chest.IsOpened ? 1 : 0);
        }
        stale.Clear();
        foreach (var pair in hostChests) if (!seen.Contains(pair.Key)) stale.Add(pair.Key);
        foreach (int id in stale)
        { var chest = hostChests[id]; hostChests.Remove(id); if (chest != null) chestIds.Remove(chest); }
        LanLobby.Instance.SendRun("RUN_CHESTS", snapshot.ToString());
    }

    void SendOrbs()
    {
        snapshot.Length = 0;
        seen.Clear();
        foreach (var orb in FindObjectsByType<XpOrb3D>(FindObjectsSortMode.None))
        {
            if (orb == null || orb.RemoteMirror) continue;
            if (!orbIds.TryGetValue(orb, out int id))
            { id = nextObjectId++; orbIds[orb] = id; hostOrbs[id] = orb; }
            seen.Add(id);
            var p = orb.transform.position;
            if (snapshot.Length > 0) snapshot.Append(';');
            snapshot.Append(id).Append(',').Append(orb.xpAmount).Append(',').Append((int)orb.tier)
                .Append(',').Append(F(p.x)).Append(',').Append(F(p.y)).Append(',').Append(F(p.z));
        }
        stale.Clear();
        foreach (var pair in hostOrbs) if (!seen.Contains(pair.Key)) stale.Add(pair.Key);
        foreach (int id in stale)
        { var orb = hostOrbs[id]; hostOrbs.Remove(id); if (orb != null) orbIds.Remove(orb); }
        LanLobby.Instance.SendRun("RUN_ORBS", snapshot.ToString());
    }

    void Receive(string[] message)
    {
        if (message.Length == 0) return;
        switch (message[0])
        {
            case "RUN_LOADED": peerLoaded = true; BeginIfReady(); break;
            case "RUN_GO": if (IsGuest) Go(); break;
            case "RUN_POS": ReceivePosition(message); break;
            case "RUN_CAST": if (partner != null) ReceiveCast(message); break;
            case "RUN_WORLD": if (IsGuest) ReceiveWorld(message); break;
            case "RUN_ENEMIES": if (IsGuest) ReceiveEnemies(message); break;
            case "RUN_OBJECTS": if (IsGuest) ReceiveObjects(message); break;
            case "RUN_CHESTS": if (IsGuest) ReceiveChests(message); break;
            case "RUN_ORBS": if (IsGuest) ReceiveOrbs(message); break;
            case "RUN_INTERACT": if (IsHost) ReceiveInteraction(message); break;
            case "RUN_INTERACT_COMPLETE": if (IsHost && message.Length == 2 && int.TryParse(message[1], out int completedId) &&
                hostInteractables.TryGetValue(completedId, out var completedObj) && completedObj != null &&
                completedObj.kind == StageInteractable.Kind.Microwave) completedObj.Complete(); break;
            case "RUN_CHARGE": if (IsHost) ReceiveCharge(message); break;
            case "RUN_CHEST_OPEN": if (IsHost) ReceiveChestOpen(message); break;
            case "RUN_CHEST_REWARD": if (IsGuest && message.Length == 3 && int.TryParse(message[1], out int chestId) &&
                chestMirrors.TryGetValue(chestId, out var rewardChest) && rewardChest != null)
                rewardChest.PresentRemoteReward(); break;
            case "RUN_SHRINE_REWARD": if (IsGuest && message.Length == 2)
                StageDirector.Instance?.GrantChargeBlessing(message[1] == "1"); break;
            case "RUN_MICROWAVE": if (IsGuest && message.Length == 2 && int.TryParse(message[1], out int microwaveId) &&
                interactableMirrors.TryGetValue(microwaveId, out var microwave) && microwave != null) MicrowaveUI.Open(microwave); break;
            case "RUN_HEAL": if (IsGuest && message.Length == 4 &&
                TryFloat(message[1], out float healX) && TryFloat(message[2], out float healY) && TryFloat(message[3], out float healZ))
                StageDirector.Instance?.ApplyHealingShrine(new Vector3(healX, healY, healZ)); break;
            case "RUN_WIND": if (IsGuest) XPSystem.Instance?.GrantTemporarySpeed(20f, .4f); break;
            case "RUN_GREED": if (IsGuest) MegabonkRelicManager.Instance?.AddTemporaryDifficulty(5f); break;
            case "RUN_SILVER": if (IsGuest && message.Length == 2 && int.TryParse(message[1], out int silver))
            { applyingSilver = true; GameManager.Instance?.AddSilver(Mathf.Clamp(silver, 0, 100000)); applyingSilver = false; } break;
            case "RUN_HIT": if (IsHost) ReceiveHit(message); break;
            case "RUN_STATUS": if (IsHost) ReceiveStatus(message); break;
            case "RUN_DAMAGE": if (IsGuest && message.Length == 2 && int.TryParse(message[1], out int damage)) localHealth?.TakeDamage(Mathf.Clamp(damage, 1, 100000)); break;
            case "RUN_XP": if (IsGuest && message.Length == 2 && int.TryParse(message[1], out int xp)) XPSystem.Instance?.AddSharedXP(Mathf.Clamp(xp, 0, 100000)); break;
            case "RUN_CHOICE": peerChoosing = message.Length == 2 && message[1] == "1"; SyncChoicePause(); break;
            case "RUN_REVIVE": if (message.Length == 1 && localHealth != null && localHealth.isDead &&
                !partnerDowned && localPlayer != null && Vector3.Distance(localPlayer.position, partnerTarget) <= 4f)
                localHealth.ReviveCoop(); break;
            case "RUN_DEFEAT": GameManager.Instance?.OnPlayerDeath(); break;
            case "RUN_RESTART_REQUEST": peerRestart = true; BeginRestartIfReady(); break;
            case "RUN_RESTART_GO": if (IsGuest) RestartTogether(); break;
            case "RUN_PING": if (message.Length == 2) pingUntil = Time.unscaledTime + 4f; break;
        }
    }

    void ReceivePosition(string[] fields)
    {
        if (fields.Length != 8 || !TryFloat(fields[1], out float x) || !TryFloat(fields[2], out float y) ||
            !TryFloat(fields[3], out float z) || !TryFloat(fields[4], out float yaw) ||
            !int.TryParse(fields[5], out int hp) || !int.TryParse(fields[6], out int maxHp)) return;
        Vector3 p = new Vector3(x, y, z);
        if (!Finite(x) || !Finite(y) || !Finite(z) || p.sqrMagnitude > 100000000f) return;
        partnerTarget = p;
        partnerRotation = Quaternion.Euler(0, yaw, 0);
        partnerMaxHealth = Mathf.Clamp(maxHp, 1, 100000);
        partnerHealth = Mathf.Clamp(hp, 0, partnerMaxHealth);
        partnerDowned = fields[7] == "1";
    }

    void ReceiveCast(string[] fields)
    {
        if (fields.Length != 5 || UpgradeCatalog.Get(fields[1]) == null ||
            !TryFloat(fields[2], out float x) || !TryFloat(fields[3], out float y) ||
            !TryFloat(fields[4], out float z) || !Finite(x) || !Finite(y) || !Finite(z)) return;
        Vector3 target = new Vector3(x, y, z);
        Vector3 start = partner.transform.position + Vector3.up;
        if ((target - start).sqrMagnitude > 1000000f) return;
        string id = fields[1];
        WeaponCastMotion.Play(partner.transform, id, target - start);
        if (id == "mb_aura" || id == "mb_aegis" || id == "mb_frost" || id == "mb_scythe")
            CombatVisuals.Ring(partner.transform.position + Vector3.up * .15f, 4f, ExtendedWeapons.Tint(id));
        else if (id == "mb_sword" || id == "mb_katana" || id == "mb_dexecutioner")
            CombatVisuals.Slash(start, target - start, 4f, id == "mb_katana");
        else if (id == "mb_lightning") CombatVisuals.Lightning(start, target);
        else StartCoroutine(AnimateRemoteCast(id, start, target));
    }

    IEnumerator AnimateRemoteCast(string id, Vector3 start, Vector3 target)
    {
        var shot = new GameObject("LAN Cast " + id);
        shot.transform.position = start;
        CombatVisuals.Projectile(shot.transform, id, .8f, ExtendedWeapons.Tint(id));
        float elapsed = 0f;
        while (shot != null && elapsed < .45f)
        {
            elapsed += Time.deltaTime;
            shot.transform.position = Vector3.Lerp(start, target, Mathf.Clamp01(elapsed / .45f));
            yield return null;
        }
        if (shot != null) Destroy(shot);
    }

    void ReceiveWorld(string[] fields)
    {
        if (fields.Length != 12 || !TryFloat(fields[1], out float time) ||
            !int.TryParse(fields[2], out int gold) || !int.TryParse(fields[3], out int score) ||
            !int.TryParse(fields[4], out int kills) || !int.TryParse(fields[7], out int stage) ||
            !TryFloat(fields[8], out float elapsed)) return;
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.gameTime = Mathf.Max(0, time);
        gm.gold = Mathf.Max(0, gold);
        gm.score = Mathf.Max(0, score);
        gm.kills = Mathf.Max(0, kills);
        gm.finalSwarmActive = fields[5] == "1";
        StageDirector.Instance?.SyncRemoteStage(stage, elapsed, fields[9] == "1", fields[10] == "1", fields[11] == "1");
        if (fields[6] == ((int)GameManager.GameState.Victory).ToString() && gm.gameState == GameManager.GameState.Playing)
            gm.TriggerVictory();
    }

    void ReceiveEnemies(string[] fields)
    {
        if (fields.Length != 2) return;
        seen.Clear();
        if (fields[1].Length > 0)
            foreach (string entry in fields[1].Split(';'))
            {
                string[] p = entry.Split(',');
                if (p.Length != 8 || !int.TryParse(p[0], out int id) || id <= 0 ||
                    !int.TryParse(p[1], out int type) || type < 0 || type > 6 ||
                    !TryFloat(p[2], out float x) || !TryFloat(p[3], out float y) ||
                    !TryFloat(p[4], out float z) || !TryFloat(p[5], out float yaw) ||
                    !int.TryParse(p[6], out int health)) continue;
                if (!Finite(x) || !Finite(y) || !Finite(z)) continue;
                seen.Add(id);
                if (!mirrors.TryGetValue(id, out var enemy) || enemy == null)
                {
                    enemy = CreateMirror(id, (EnemyAI3D.EnemyType)type, new Vector3(x, y, z), p[7]);
                    if (enemy == null) continue;
                    mirrors[id] = enemy;
                }
                enemy.transform.position = new Vector3(x, y, z);
                enemy.transform.rotation = Quaternion.Euler(0, yaw, 0);
                enemy.SetRemoteHealth(health);
            }
        stale.Clear();
        foreach (var pair in mirrors) if (!seen.Contains(pair.Key)) stale.Add(pair.Key);
        foreach (int id in stale)
        { if (mirrors[id] != null) Destroy(mirrors[id].gameObject); mirrors.Remove(id); }
    }

    void ReceiveObjects(string[] fields)
    {
        if (fields.Length != 2) return;
        seen.Clear();
        if (fields[1].Length > 0) foreach (string entry in fields[1].Split(';'))
        {
            var p = entry.Split(',');
            if (p.Length != 8 || !int.TryParse(p[0], out int id) || id <= 0 ||
                !int.TryParse(p[1], out int kind) || kind < 0 || kind > (int)StageInteractable.Kind.WindShrine ||
                !TryFloat(p[2], out float x) || !TryFloat(p[3], out float y) || !TryFloat(p[4], out float z) ||
                !TryFloat(p[6], out float progress) || !Finite(x) || !Finite(y) || !Finite(z)) continue;
            seen.Add(id);
            if (!interactableMirrors.TryGetValue(id, out var obj) || obj == null)
            {
                var go = new GameObject("LAN Interactable " + id);
                go.transform.position = new Vector3(x, y, z);
                obj = go.AddComponent<StageInteractable>();
                obj.CoopId = id;
                obj.InitializeRemote((StageInteractable.Kind)kind, p[7] == "1");
                interactableMirrors[id] = obj;
            }
            obj.SetRemoteProgress(progress, p[5] == "1");
        }
        stale.Clear();
        foreach (var pair in interactableMirrors) if (!seen.Contains(pair.Key)) stale.Add(pair.Key);
        foreach (int id in stale)
        { if (interactableMirrors[id] != null) Destroy(interactableMirrors[id].gameObject); interactableMirrors.Remove(id); }
    }

    void ReceiveChests(string[] fields)
    {
        if (fields.Length != 2) return;
        seen.Clear();
        if (fields[1].Length > 0) foreach (string entry in fields[1].Split(';'))
        {
            var p = entry.Split(',');
            if (p.Length != 7 || !int.TryParse(p[0], out int id) || id <= 0 ||
                !int.TryParse(p[2], out int cost) || !TryFloat(p[3], out float x) ||
                !TryFloat(p[4], out float y) || !TryFloat(p[5], out float z) ||
                !Finite(x) || !Finite(y) || !Finite(z)) continue;
            seen.Add(id);
            if (!chestMirrors.TryGetValue(id, out var chest) || chest == null)
            {
                chest = BossChest3D.Spawn(new Vector3(x, y, z), p[1] == "1");
                chest.goldCost = Mathf.Max(0, cost);
                chest.CoopId = id;
                chest.RemoteMirror = true;
                chestMirrors[id] = chest;
            }
            chest.SetRemoteOpened(p[6] == "1");
        }
        stale.Clear();
        foreach (var pair in chestMirrors) if (!seen.Contains(pair.Key)) stale.Add(pair.Key);
        foreach (int id in stale)
        { if (chestMirrors[id] != null) Destroy(chestMirrors[id].gameObject); chestMirrors.Remove(id); }
    }

    void ReceiveOrbs(string[] fields)
    {
        if (fields.Length != 2) return;
        seen.Clear();
        if (fields[1].Length > 0) foreach (string entry in fields[1].Split(';'))
        {
            var p = entry.Split(',');
            if (p.Length != 6 || !int.TryParse(p[0], out int id) || id <= 0 ||
                !int.TryParse(p[1], out int xp) || !int.TryParse(p[2], out int tier) || tier < 0 || tier > 2 ||
                !TryFloat(p[3], out float x) || !TryFloat(p[4], out float y) || !TryFloat(p[5], out float z) ||
                !Finite(x) || !Finite(y) || !Finite(z)) continue;
            seen.Add(id);
            if (!orbMirrors.TryGetValue(id, out var orb) || orb == null)
            {
                orb = XpOrb3D.Spawn(new Vector3(x, y, z), xp, (XpOrb3D.OrbTier)tier);
                orb.RemoteMirror = true;
                orbMirrors[id] = orb;
            }
            orb.transform.position = new Vector3(x, y, z);
        }
        stale.Clear();
        foreach (var pair in orbMirrors) if (!seen.Contains(pair.Key)) stale.Add(pair.Key);
        foreach (int id in stale)
        { if (orbMirrors[id] != null) Destroy(orbMirrors[id].gameObject); orbMirrors.Remove(id); }
    }

    void ReceiveInteraction(string[] fields)
    {
        if (fields.Length != 2 || !int.TryParse(fields[1], out int id) ||
            !hostInteractables.TryGetValue(id, out var obj) || obj == null || obj.completed ||
            Vector3.Distance(partnerTarget, obj.transform.position) > 4.5f) return;
        obj.ActivateFromRemote();
    }

    void ReceiveCharge(string[] fields)
    {
        if (fields.Length != 2 || !int.TryParse(fields[1], out int id) ||
            !hostInteractables.TryGetValue(id, out var obj) || obj == null ||
            Vector3.Distance(partnerTarget, obj.transform.position) > 4.5f) return;
        obj.ChargeFromRemote();
    }

    void ReceiveChestOpen(string[] fields)
    {
        if (fields.Length != 2 || !int.TryParse(fields[1], out int id) ||
            !hostChests.TryGetValue(id, out var chest) || chest == null || chest.IsOpened ||
            Vector3.Distance(partnerTarget, chest.transform.position) > chest.interactionDistance + 1f) return;
        chest.OpenFromRemote();
    }

    EnemyHealth3D CreateMirror(int id, EnemyAI3D.EnemyType type, Vector3 position, string modelId)
    {
        var spawner = FindAnyObjectByType<EnemySpawner3D>();
        GameObject prefab = spawner == null ? null : type == EnemyAI3D.EnemyType.Boss ? spawner.bossPrefab :
            type == EnemyAI3D.EnemyType.FlyingShooter && spawner.flyingEnemyPrefab != null ? spawner.flyingEnemyPrefab : spawner.enemyPrefab;
        GameObject go = prefab != null ? Instantiate(prefab, position, Quaternion.identity) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "LAN Enemy " + id;
        go.transform.position = position;
        var health = go.GetComponent<EnemyHealth3D>() ?? go.AddComponent<EnemyHealth3D>();
        health.CoopId = id;
        health.RemoteMirror = true;
        var ai = go.GetComponent<EnemyAI3D>();
        if (ai != null) { ai.enemyType = type; ai.enabled = false; }
        float elapsed = GameManager.Instance != null ? GameManager.Instance.gameTime : 0f;
        int stage = StageDirector.Instance != null ? StageDirector.Instance.CurrentStage : 1;
        if (!ExtractedEnemyMeshVisual.Apply(go, type, elapsed, stage, modelId))
            MonsterPackVisual.Apply(go, type, elapsed);
        var boss = go.GetComponent<ExtractedBossCombat>();
        if (boss != null) boss.enabled = false;
        return health;
    }

    void ReceiveHit(string[] fields)
    {
        if (fields.Length != 4 || !int.TryParse(fields[1], out int id) ||
            !int.TryParse(fields[2], out int damage) || !hostEnemies.TryGetValue(id, out var enemy) ||
            enemy == null || enemy.IsDying) return;
        enemy.TakeDamage(Mathf.Clamp(damage, 1, 100000), true, null, false, fields[3]);
    }

    void ReceiveStatus(string[] fields)
    {
        if (fields.Length != 5 || !int.TryParse(fields[1], out int id) ||
            !hostEnemies.TryGetValue(id, out var enemy) || enemy == null || enemy.IsDying ||
            !TryFloat(fields[3], out float duration) || !int.TryParse(fields[4], out int value)) return;
        duration = Mathf.Clamp(duration, 0, 30);
        value = Mathf.Clamp(value, 0, 100000);
        switch (fields[2])
        {
            case "poison": enemy.ApplyPoison(duration, value); break;
            case "burn": enemy.ApplyBurn(duration, value); break;
            case "freeze": enemy.ApplyFreeze(duration); break;
            case "curse": enemy.ApplyCurse(duration, value); break;
        }
    }

    public static void ReportHit(int id, int damage, string source)
    {
        if (!IsGuest || id <= 0 || damage <= 0) return;
        LanLobby.Instance.SendRun("RUN_HIT", id.ToString(), Mathf.Min(damage, 100000).ToString(), source ?? "");
    }

    public static void ReportCast(string id, Vector3 target)
    {
        if (!IsReady || string.IsNullOrEmpty(id)) return;
        LanLobby.Instance.SendRun("RUN_CAST", id, F(target.x), F(target.y), F(target.z));
    }

    public static void ReportStatus(int id, string kind, float duration, int value)
    {
        if (!IsGuest || id <= 0) return;
        LanLobby.Instance.SendRun("RUN_STATUS", id.ToString(), kind, F(duration), value.ToString());
    }

    public static void AwardSharedXP(int amount)
    {
        if (IsHost && amount > 0) LanLobby.Instance.SendRun("RUN_XP", amount.ToString());
    }

    public static void HitPartner(int damage)
    {
        if (IsHost && !PartnerDowned && damage > 0)
            LanLobby.Instance.SendRun("RUN_DAMAGE", damage.ToString());
    }

    static bool applyingSilver;
    public static bool ReceivingSilver => applyingSilver;
    public static void ShareSilver(int amount)
    { if (IsHost && amount > 0) LanLobby.Instance.SendRun("RUN_SILVER", amount.ToString()); }
    public static void ShareShrineBlessing(bool golden)
    { if (IsHost) LanLobby.Instance.SendRun("RUN_SHRINE_REWARD", golden ? "1" : "0"); }
    public static void OpenPartnerMicrowave(int id)
    { if (IsHost) LanLobby.Instance.SendRun("RUN_MICROWAVE", id.ToString()); }
    public static void ShareHealingShrine(Vector3 position)
    { if (IsHost) LanLobby.Instance.SendRun("RUN_HEAL", F(position.x), F(position.y), F(position.z)); }
    public static void WindPartner()
    { if (IsHost) LanLobby.Instance.SendRun("RUN_WIND"); }
    public static void ShareGreed()
    { if (IsHost) LanLobby.Instance.SendRun("RUN_GREED"); }
    public static void RequestCharge(int id)
    {
        if (!IsGuest || id <= 0 || Time.unscaledTime < instance.nextChargeRequest) return;
        instance.nextChargeRequest = Time.unscaledTime + .08f;
        LanLobby.Instance.SendRun("RUN_CHARGE", id.ToString());
    }
    public static void RequestInteract(int id)
    { if (IsGuest && id > 0) LanLobby.Instance.SendRun("RUN_INTERACT", id.ToString()); }
    public static void RequestCompleteInteract(int id)
    { if (IsGuest && id > 0) LanLobby.Instance.SendRun("RUN_INTERACT_COMPLETE", id.ToString()); }
    public static void RequestChest(int id)
    { if (IsGuest && id > 0) LanLobby.Instance.SendRun("RUN_CHEST_OPEN", id.ToString()); }
    public static void ShareChestReward(int id, bool boss)
    { if (IsHost && id > 0) LanLobby.Instance.SendRun("RUN_CHEST_REWARD", id.ToString(), boss ? "1" : "0"); }
    public static bool CanUsePortal(Vector3 position)
    { return !Active || IsHost && !PartnerDowned && instance.localHealth != null && !instance.localHealth.isDead &&
        Vector3.Distance(instance.localPlayer.position, position) <= 5f && Vector3.Distance(instance.partnerTarget, position) <= 5f; }

    public static void RequestRestart()
    {
        if (!Active || instance.localRestart) return;
        instance.localRestart = true;
        LanLobby.Instance.SendRun("RUN_RESTART_REQUEST");
        instance.BeginRestartIfReady();
    }

    void BeginRestartIfReady()
    {
        if (!IsHost || !localRestart || !peerRestart) return;
        LanLobby.Instance.SendRun("RUN_RESTART_GO");
        RestartTogether();
    }

    void RestartTogether()
    {
        localRestart = peerRestart = false;
        localLoaded = peerLoaded = running = defeatSent = false;
        personalMenu = peerChoosing = localChoosing = announcedChoosing = partnerDowned = false;
        reviveProgress = 0;
        foreach (var mirror in mirrors.Values) if (mirror != null) Destroy(mirror.gameObject);
        foreach (var mirror in interactableMirrors.Values) if (mirror != null) Destroy(mirror.gameObject);
        foreach (var mirror in chestMirrors.Values) if (mirror != null) Destroy(mirror.gameObject);
        foreach (var mirror in orbMirrors.Values) if (mirror != null) Destroy(mirror.gameObject);
        mirrors.Clear(); interactableMirrors.Clear(); chestMirrors.Clear(); orbMirrors.Clear();
        hostIds.Clear(); hostEnemies.Clear(); interactableIds.Clear(); hostInteractables.Clear(); chestIds.Clear(); hostChests.Clear(); orbIds.Clear(); hostOrbs.Clear();
        nextEnemyId = nextObjectId = 1;
        if (partner != null) Destroy(partner);
        partner = null;
        if (IsGuest)
        {
            var spawner = FindAnyObjectByType<EnemySpawner3D>();
            if (spawner != null) spawner.enabled = false;
        }
        GameManager.Instance?.StartGame();
    }

    void HandleRevive()
    {
        if (localHealth == null || partner == null || localHealth.isDead || !partnerDowned || Time.timeScale <= 0)
        { reviveProgress = 0; return; }
        bool near = Vector3.Distance(localPlayer.position, partner.transform.position) <= 3f;
        if (!near || !Input.GetKey(KeyCode.E) || localHealth.LastDamageTime > reviveDamageTime)
        { reviveProgress = 0; reviveDamageTime = localHealth.LastDamageTime; return; }
        reviveProgress += Time.deltaTime;
        if (reviveProgress >= 4f)
        {
            reviveProgress = 0;
            LanLobby.Instance.SendRun("RUN_REVIVE");
        }
    }

    public static void SyncChoicePause()
    {
        if (!Active || !instance.running) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.gameState != GameManager.GameState.Playing) return;
        if (StageDirector.Instance != null && StageDirector.Instance.IsTransitioning) return;
        instance.localChoosing = (XPSystem.Instance != null &&
            (XPSystem.Instance.skillMenuOpen || XPSystem.Instance.bossTomeMenuOpen)) ||
            ShrineRewardUI.IsOpen || MicrowaveUI.IsOpen ||
            (MegabonkRelicManager.Instance != null && MegabonkRelicManager.Instance.chestMenuOpen);
        bool paused = instance.localChoosing || instance.peerChoosing;
        if (UiTransition.Busy) return;
        Time.timeScale = paused ? 0 : 1;
        Cursor.lockState = paused || instance.personalMenu ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused || instance.personalMenu;
    }

    public static void SetPersonalMenu(bool open)
    {
        if (!Active) return;
        instance.personalMenu = open;
        SyncChoicePause();
    }

    float pingUntil;
    void OnGUI()
    {
        if (!running && localLoaded && GameManager.Instance != null &&
            GameManager.Instance.gameState == GameManager.GameState.Playing)
        {
            MegabonkUI.Background(true);
            MegabonkUI.Text(new Rect(60, Screen.height * .4f, Screen.width - 120, 80),
                "ESPERANDO A QUE AMBOS TERMINEN DE CARGAR…", 24, MegabonkUI.White, true, TextAnchor.MiddleCenter);
            return;
        }
        if (!running || partner == null || localPlayer == null) return;
        var old = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(Vector3.one * MegabonkUI.Scale);
        try
        {
            Vector3 d = partner.transform.position - localPlayer.position;
            string status = partnerDowned ? "DERRIBADO · MANTÉN E CERCA PARA REVIVIR" :
                "VIDA " + partnerHealth + "/" + partnerMaxHealth;
            MegabonkUI.MetalPanel(new Rect(MegabonkUI.Width - 325, 166, 295, 85), "COMPAÑERO");
            MegabonkUI.Text(new Rect(MegabonkUI.Width - 315, 205, 275, 26),
                Mathf.RoundToInt(d.magnitude) + " m  ·  " + status, 14,
                partnerDowned ? MegabonkUI.Gold : MegabonkUI.White, true);
            if (localHealth != null && localHealth.isDead)
                MegabonkUI.Text(new Rect(MegabonkUI.Width * .5f - 350, MegabonkUI.Height * .64f, 700, 55),
                    "DERRIBADO · ESPERA A TU COMPAÑERO · X PARA PEDIR AYUDA", 28, MegabonkUI.Gold, true, TextAnchor.MiddleCenter);
            if (localRestart)
                MegabonkUI.Text(new Rect(MegabonkUI.Width * .5f - 320, MegabonkUI.Height * .70f, 640, 44),
                    "ESPERANDO QUE EL COMPAÑERO TAMBIÉN REINICIE", 18, MegabonkUI.Gold, true, TextAnchor.MiddleCenter);
            else if (reviveProgress > 0)
                MegabonkUI.Meter(new Rect(MegabonkUI.Width * .5f - 120, MegabonkUI.Height * .63f, 240, 12),
                    reviveProgress / 4f, MegabonkUI.Cyan);
            if (Time.unscaledTime < pingUntil)
                MegabonkUI.Text(new Rect(MegabonkUI.Width * .5f - 150, 210, 300, 30),
                    "¡EL COMPAÑERO PIDE AYUDA!", 18, MegabonkUI.Gold, true, TextAnchor.MiddleCenter);
        }
        finally { GUI.matrix = old; }
    }

    static string F(float value) => value.ToString("R", Culture);
    static bool TryFloat(string text, out float value) => float.TryParse(text, NumberStyles.Float, Culture, out value);
    static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
