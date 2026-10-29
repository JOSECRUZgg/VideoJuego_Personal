using UnityEngine;

/// <summary>Dos anillos defensivos visibles mientras el jugador conserve escudo.</summary>
public class PlayerShieldAura : MonoBehaviour
{
    PlayerHealth3D health;
    LineRenderer[] rings;

    void Start()
    {
        health = GetComponent<PlayerHealth3D>();
        rings = new[] { CreateRing(.68f) };
    }

    LineRenderer CreateRing(float radius)
    {
        GameObject ring = new GameObject("ShieldCircle");
        ring.transform.SetParent(transform, false);
        ring.transform.localPosition = Vector3.up * 0.08f;
        LineRenderer line = ring.AddComponent<LineRenderer>();
        line.loop = true; line.useWorldSpace = false; line.positionCount = 36;
        line.widthMultiplier = 0.028f;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.sharedMaterial = RuntimeMaterialCache.Get(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"),new Color(.1f,.38f,.5f),Color.black);
        for (int i = 0; i < line.positionCount; i++)
        {
            float a = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }
        return line;
    }

    void Update()
    {
        if (health == null || rings == null) return;
        float alpha = health.ShieldPercent > 0f ? Mathf.Lerp(0.12f, 0.7f, health.ShieldPercent / 100f) : 0f;
        Color color = new Color(0.15f, 0.7f, 1f, alpha);
        for (int i = 0; i < rings.Length; i++)
        {
            if (rings[i] == null) continue;
            rings[i].enabled=health.currentShield>0 && !health.isDead && GameManager.Instance!=null && GameManager.Instance.gameState!=GameManager.GameState.MainMenu;
            rings[i].startColor = color; rings[i].endColor = color;
            rings[i].transform.localRotation = Quaternion.Euler(0f, Time.time * (i == 0 ? 55f : -36f), 0f);
        }
    }
}
