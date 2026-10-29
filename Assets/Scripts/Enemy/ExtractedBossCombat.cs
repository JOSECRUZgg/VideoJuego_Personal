using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Reconstructed patterns driven by serialized attack values, not recovered method bodies.
public class ExtractedBossCombat : MonoBehaviour
{
    public enum CombatState { Following, Charging, Attacking, Recovering, Dead }
    public CombatState State { get; private set; }
    public bool HasAttacks => attacks != null && attacks.Length > 0;
    public bool IsBusy => State != CombatState.Following;
    ExtractedEnemyCatalog.Attack[] attacks;
    float[] cooldowns;
    int cursor, selected, baseDamage;
    float clock, phaseTime, nextDamage, angle;
    EnemyHealth3D health;
    PlayerHealth3D localHealth;
    Transform target;
    Material warningMaterial;
    readonly List<LineRenderer> warnings = new List<LineRenderer>();
    readonly List<Vector3> centers = new List<Vector3>();
    bool laser, rotating;
    bool darkness;
    public bool DarknessWarning => darkness && (State == CombatState.Charging || State == CombatState.Attacking);

    public void Initialize(ExtractedEnemyCatalog.Entry data)
    {
        attacks = (data.attacks ?? new ExtractedEnemyCatalog.Attack[0]).Where(a =>
            a.name.Contains("Root") || a.name.Contains("Pillar") || a.name.Contains("Laser") ||
            a.name.Contains("Spikes") || a.name.Contains("Darkness")).ToArray();
        cooldowns = attacks.Select(a => Mathf.Max(1, a.initialCooldown)).ToArray();
        baseDamage = Mathf.Max(1, data.damage);
        health = GetComponent<EnemyHealth3D>();
        State = CombatState.Following;
    }
    void Update()
    {
        if (!HasAttacks) return;
        if (health == null || health.IsDying) { State = CombatState.Dead; ClearWarnings(); return; }
        if (Time.timeScale <= 0 || health.isFrozen) return;
        if (localHealth == null) localHealth = FindAnyObjectByType<PlayerHealth3D>();
        Transform own = localHealth != null && !localHealth.isDead ? localHealth.transform : null;
        Transform partner = CoopRun.IsHost && !CoopRun.PartnerDowned ? CoopRun.Partner : null;
        if (own == null) target = partner;
        else if (partner == null) target = own;
        else if (target == partner)
            target = (own.position - transform.position).sqrMagnitude < (partner.position - transform.position).sqrMagnitude * .56f ? own : partner;
        else target = (partner.position - transform.position).sqrMagnitude < (own.position - transform.position).sqrMagnitude * .56f ? partner : own;
        if (target == null) { ClearWarnings(); return; }
        float dt = MegabonkRelicManager.HostileDeltaTime;
        if (dt <= 0) return;
        clock += dt;
        phaseTime -= dt;
        if (State == CombatState.Following)
        {
            for (int n = 0; n < attacks.Length; n++)
            {
                int index = (cursor + n) % attacks.Length;
                if (clock < cooldowns[index] || Vector3.Distance(transform.position, target.position) > attacks[index].range) continue;
                selected = index; cursor = (index + 1) % attacks.Length;
                Prepare(); return;
            }
        }
        else if (State == CombatState.Charging && phaseTime <= 0)
        {
            State = CombatState.Attacking;
            phaseTime = laser || darkness ? Mathf.Max(.5f, attacks[selected].duration) : .25f;
            nextDamage = 0;
            foreach (var line in warnings) line.startColor = line.endColor = Color.red;
            if (darkness) ExtractedVisuals.Burst("DarknessAttack", transform.position, 3f);
            else if (!laser)
                foreach (var p in centers) ExtractedVisuals.Burst(attacks[selected].name.Contains("Root") ? "RootFx" : "SandPillarFx", p, 2);
        }
        else if (State == CombatState.Attacking)
        {
            if (laser) UpdateLasers(dt);
            if (clock >= nextDamage)
            {
                nextDamage = clock + (laser ? .5f : 1000);
                if (PlayerInside())
                {
                    int damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * attacks[selected].damageMultiplier));
                    if (target == CoopRun.Partner) CoopRun.HitPartner(damage);
                    else localHealth.TakeDamage(damage);
                }
            }
            if (phaseTime <= 0)
            {
                ClearWarnings(); State = CombatState.Recovering;
                phaseTime = Mathf.Max(attacks[selected].recovery, attacks[selected].endLag);
                cooldowns[selected] = clock + Mathf.Max(.5f, attacks[selected].cooldown);
            }
        }
        else if (State == CombatState.Recovering && phaseTime <= 0) State = CombatState.Following;
    }
    void Prepare()
    {
        ClearWarnings();
        var attack = attacks[selected];
        State = CombatState.Charging; phaseTime = Mathf.Max(.3f, attack.charge);
        laser = attack.name.Contains("Laser"); rotating = attack.name.Contains("Disco");
        darkness = attack.name.Contains("Darkness");
        if (darkness) return;
        Vector3 direction = target.position - transform.position;
        angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        if (laser)
        {
            for (int i = 0; i < (rotating ? 4 : 1); i++) NewLine(2);
            UpdateLasers(0); return;
        }
        bool lines = attack.name.Contains("Line") || attack.name.Contains("Continous");
        int lanes = attack.name.Contains("Lines3") ? 3 : 1;
        int count = lines ? lanes * 5 : attack.name.Contains("Multiple") || attack.name.Contains("Circles") ? 3 : 1;
        float radius = Mathf.Max(.5f, attack.radius);
        for (int i = 0; i < count; i++)
        {
            Vector3 center = target.position;
            if (lines)
            {
                float yaw = angle + (i / 5 - (lanes - 1) * .5f) * 25;
                center = transform.position + Quaternion.Euler(0, yaw, 0) * Vector3.forward * ((i % 5 + 1) * radius * 1.8f);
            }
            else if (count > 1) center += Quaternion.Euler(0, i * 120, 0) * Vector3.forward * radius * 1.5f;
            if (ArenaEnricher.Instance != null && ArenaEnricher.Instance.TryGetSnowHeight(center, out float y)) center.y = y;
            centers.Add(center);
            var line = NewLine(49);
            for (int j = 0; j <= 48; j++)
            {
                float a = j * Mathf.PI * 2 / 48;
                line.SetPosition(j, center + new Vector3(Mathf.Cos(a) * radius, .12f, Mathf.Sin(a) * radius));
            }
        }
    }
    LineRenderer NewLine(int count)
    {
        if (warningMaterial == null)
            warningMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"));
        var obj = new GameObject("AttackWarning"); obj.transform.SetParent(transform, false);
        var line = obj.AddComponent<LineRenderer>(); line.useWorldSpace = true;
        line.sharedMaterial = warningMaterial; line.positionCount = count;
        line.startWidth = line.endWidth = .18f; line.startColor = line.endColor = Color.yellow;
        warnings.Add(line); return line;
    }
    void UpdateLasers(float dt)
    {
        if (rotating) angle += dt * 30;
        for (int i = 0; i < warnings.Count; i++)
        {
            Vector3 start = transform.position + Vector3.up;
            warnings[i].SetPosition(0, start);
            warnings[i].SetPosition(1, start + Quaternion.Euler(0, angle + i * 90, 0) * Vector3.forward * Mathf.Min(90, attacks[selected].range));
        }
    }
    bool PlayerInside()
    {
        Vector3 point = target.position + Vector3.up;
        if (darkness)
            return StageDirector.Instance == null || !StageDirector.Instance.IsInsideFinalShelter(target.position);
        if (laser)
        {
            foreach (var line in warnings)
            {
                Vector3 a = line.GetPosition(0), d = line.GetPosition(1) - a;
                float t = Mathf.Clamp01(Vector3.Dot(point - a, d) / Mathf.Max(.001f, d.sqrMagnitude));
                if (Vector3.Distance(point, a + d * t) <= .65f) return true;
            }
        }
        else foreach (var center in centers)
        {
            Vector3 d = target.position - center;
            if (Mathf.Abs(d.y) > 3) continue;
            d.y = 0;
            if (d.sqrMagnitude <= attacks[selected].radius * attacks[selected].radius) return true;
        }
        return false;
    }
    void ClearWarnings()
    {
        foreach (var line in warnings) if (line != null) Destroy(line.gameObject);
        warnings.Clear(); centers.Clear();
    }
    void OnDisable() { ClearWarnings(); }
    void OnDestroy() { if (warningMaterial != null) Destroy(warningMaterial); }
}
