using UnityEngine;

/// <summary>Anillo mágico animado para invocaciones: mantiene una lectura clara desde la cámara elevada.</summary>
public class CompanionMagicCircle : MonoBehaviour
{
    LineRenderer line;
    float radius;
    float speed;
    float phase;
    Color color;

    public static void Attach(GameObject owner, Color tint, float ringRadius, float rotationSpeed)
    {
        if (owner == null) return;
        GameObject ring = new GameObject("CompanionMagicCircle");
        ring.transform.SetParent(owner.transform, false);
        ring.transform.localPosition = Vector3.down * 0.28f;
        CompanionMagicCircle effect = ring.AddComponent<CompanionMagicCircle>();
        effect.radius = ringRadius;
        effect.speed = rotationSpeed;
        effect.phase = Random.value * Mathf.PI * 2f;
        effect.color = tint;
        effect.CreateLine();
    }

    void CreateLine()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 40;
        line.widthMultiplier = 0.045f;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        line.sharedMaterial = new Material(shader);
        line.startColor = color;
        line.endColor = color;
    }

    void LateUpdate()
    {
        if (line == null) return;
        float pulse = 1f + Mathf.Sin(Time.time * 3f + phase) * 0.08f;
        float r = radius * pulse;
        for (int i = 0; i < line.positionCount; i++)
        {
            float a = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
        }
        transform.localRotation = Quaternion.Euler(0f, Time.time * speed * 90f + phase * Mathf.Rad2Deg, 0f);
    }
}
