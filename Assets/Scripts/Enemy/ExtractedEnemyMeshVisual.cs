using System.Linq;
using UnityEngine;

public class ExtractedEnemyMeshVisual : MonoBehaviour
{
    MeshFilter mesh;
    ExtractedEnemyCatalog.Entry entry;
    EnemyHealth3D health;
    float elapsed;
    Vector3 previous;
    bool animateWhileStationary;
    static ExtractedEnemyCatalog catalog;
    public string ModelId => entry != null ? entry.id : null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache() { catalog = null; }

    public static bool Apply(GameObject enemy, EnemyAI3D.EnemyType type, float time, int stage = 1, string overrideId = null)
    {
        if (catalog == null) catalog = Resources.Load<ExtractedEnemyCatalog>("ExtractedEnemyCatalog");
        if (catalog == null || catalog.entries == null) return false;
        bool desert = GameManager.Instance != null && GameManager.Instance.selectedMap == GameManager.MapType.Desert;
        string id;
        if (!string.IsNullOrEmpty(overrideId)) id = overrideId;
        else if (type == EnemyAI3D.EnemyType.Boss) id = (desert ? "Pharaoh" : "EntStage") + Mathf.Clamp(stage, 1, 3);
        else if (type == EnemyAI3D.EnemyType.Elite) id = desert ? "Miniboss_Scorpion" : "Miniboss_Golem";
        else if (type == EnemyAI3D.EnemyType.Summoner) id = desert ? "CactusShooter_0" : "Wisp_0";
        else if (type == EnemyAI3D.EnemyType.FlyingShooter) id = "Bee_0";
        else if (type == EnemyAI3D.EnemyType.Sploder) id = "BoomerSpider";
        else if (type == EnemyAI3D.EnemyType.Charger) id = desert ? "Scorpion_0" : "GoblinStrong_0";
        else
        {
            string[] pool = desert ? new[] { "SkeletonDusty", "Mummy", "MummyTank_0" } :
                new[] { "Skeleton", "SkeletonArmored", "Golem_0" };
            var eligible = catalog.entries.Where(e => pool.Contains(e.id) && e.spawnAfter <= time &&
                (time >= 90 || e.id == pool[0])).ToArray();
            id = eligible.Length > 0 ? eligible[Random.Range(0, eligible.Length)].id : pool[0];
        }
        var data = catalog.entries.FirstOrDefault(e => e.id == id);
        if (data == null || data.frames == null || data.frames.Length == 0 || data.material == null) return false;
        if (string.IsNullOrEmpty(overrideId) && type == EnemyAI3D.EnemyType.Lurker && time < data.spawnAfter) return false;
        var old = enemy.GetComponentsInChildren<Renderer>();
        var visual = new GameObject("Megabonk_" + id);
        visual.transform.SetParent(enemy.transform, false);
        visual.transform.localRotation = Quaternion.Euler(data.rotation);
        var filter = visual.AddComponent<MeshFilter>();
        filter.sharedMesh = data.frames[0];
        var renderer = visual.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = data.material;
        float height = id == "GhostKingData" ? 13.25f :
            type == EnemyAI3D.EnemyType.Boss ? 7 : type == EnemyAI3D.EnemyType.Elite ? 3.5f : data.flying ? 1.3f : 1.8f;
        visual.transform.localScale *= height / Mathf.Max(.01f, renderer.bounds.size.y);
        var bounds = renderer.bounds;
        visual.transform.position += new Vector3(enemy.transform.position.x - bounds.center.x, enemy.transform.position.y - bounds.min.y, enemy.transform.position.z - bounds.center.z);
        var capsule = enemy.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            Vector3 scale = enemy.transform.lossyScale;
            capsule.direction = 1;
            capsule.height = height / Mathf.Max(.01f, Mathf.Abs(scale.y));
            capsule.radius = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.z) * .25f, .3f, height * .4f) /
                Mathf.Max(.01f, Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)));
            capsule.center = Vector3.up * capsule.height * .5f;
        }
        foreach (var r in old) r.enabled = false;
        foreach (var a in enemy.GetComponentsInChildren<Animator>()) a.enabled = false;
        var dragon = enemy.GetComponent<DragonBossVisuals>();
        if (dragon != null) dragon.enabled = false;
        var motion = visual.AddComponent<ExtractedEnemyMeshVisual>();
        motion.mesh = filter; motion.entry = data; motion.health = enemy.GetComponent<EnemyHealth3D>();
        motion.previous = visual.transform.position;
        motion.animateWhileStationary = type == EnemyAI3D.EnemyType.Boss || type == EnemyAI3D.EnemyType.Elite;
        var ai = enemy.GetComponent<EnemyAI3D>();
        if (ai != null)
        {
            ai.extractedData = data;
            ai.speed = Mathf.Max(0, data.speed);
            if (data.damage > 0) ai.damage = data.damage;
            if (data.minDistance > 0) ai.stopDistance = data.minDistance;
            if (type == EnemyAI3D.EnemyType.Boss)
                enemy.AddComponent<ExtractedBossCombat>().Initialize(data);
        }
        return true;
    }
    void Update()
    {
        Vector3 delta = transform.position - previous;
        previous = transform.position;
        if (Time.deltaTime <= 0 || health != null && (health.isFrozen || health.IsDying)) return;
        if (!animateWhileStationary && delta.sqrMagnitude < .000001f) return;
        elapsed += Time.deltaTime * Mathf.Max(.1f, MegabonkRelicManager.HostileTimeScale);
        mesh.sharedMesh = entry.frames[(int)(elapsed * Mathf.Max(1, entry.fps)) % entry.frames.Length];
    }
}
