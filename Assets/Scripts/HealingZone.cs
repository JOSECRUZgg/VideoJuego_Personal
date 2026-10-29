using UnityEngine;

/// <summary>Zona temporal que cura mientras el jugador permanece dentro.</summary>
public class HealingZone : MonoBehaviour
{
    public float life = 16f;
    public float radius = 4f;
    private float tick;
    private GameObject ring;

    public void Setup(float duration, float newRadius = 4.5f)
    {
        life = duration;
        radius = newRadius;
        if (ring != null)
            ring.transform.localScale = new Vector3(radius * 2f, 0.06f, radius * 2f);
    }

    void Start()
    {
        ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.transform.SetParent(transform, false);
        ring.transform.localScale = new Vector3(radius * 2f, 0.06f, radius * 2f);
        Collider col = ring.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        Renderer renderer = ring.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        renderer.sharedMaterial = RuntimeMaterialCache.Get(shader, new Color(0.2f, 1f, 0.45f, 0.45f), new Color(0.1f, 1f, 0.35f));
    }

    void Update()
    {
        life -= Time.deltaTime;
        tick -= Time.deltaTime;
        if (life <= 0f) { Destroy(gameObject); return; }
        if (tick > 0f) return;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && Vector3.Distance(player.transform.position, transform.position) <= radius)
        {
            PlayerHealth3D health = player.GetComponent<PlayerHealth3D>();
            if (health != null) health.HealHearts(1);
        }
        tick = 2f;
    }
}
