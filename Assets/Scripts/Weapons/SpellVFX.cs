using System.Collections.Generic;
using UnityEngine;

/// <summary>Visuales Kenney CC0. El daño permanece en los sistemas de combate.</summary>
public class SpellVFX : MonoBehaviour
{
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static int activeEffects;
    const int MaxEffects = 48;
    bool counted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        activeEffects = 0;
        materials.Clear();
    }

    void OnDestroy() { if (counted) activeEffects = Mathf.Max(0, activeEffects - 1); }

    public static void Burst(Vector3 position, string texture, Color color, float size = 1f)
    {
        Spawn(position, texture, color, size, 0f, null);
    }

    public static void Fire(Transform owner, float duration, float size = 1f)
    {
        if (owner == null) return;
        Spawn(owner.position + Vector3.up * 0.35f, "flame_01", new Color(1f, 0.45f, 0.08f), size, duration, owner);
    }

    static void Spawn(Vector3 position, string texture, Color color, float size, float duration, Transform owner)
    {
        if (activeEffects >= MaxEffects || Time.timeScale <= 0f) return;
        if (!materials.TryGetValue(texture, out Material material) || material == null)
        {
            Texture2D image = Resources.Load<Texture2D>("SpellVFX/" + texture);
            Shader shader = Resources.Load<Shader>("SpellVFX/SpellParticle")
                ?? Shader.Find("Game/SpellParticle")
                ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");
            if (shader == null) return;
            material = new Material(shader);
            if (image != null) material.mainTexture = image;
            materials[texture] = material;
        }
        GameObject effect = new GameObject("SpellVFX_" + texture);
        effect.transform.position = position;
        if (owner != null) effect.transform.SetParent(owner, true);
        var lifetime = effect.AddComponent<SpellVFX>();
        lifetime.counted = true;
        activeEffects++;
        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = Mathf.Max(0.1f, duration);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.8f * size);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.35f, size);
        main.startColor = color;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.maxParticles = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        var emission = particles.emission;
        emission.rateOverTime = duration > 0f ? 14f : 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(duration > 0f ? 5 : 12)) });
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = size * 0.25f;
        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.65f, 0.45f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        particles.Play();
        Destroy(effect, Mathf.Max(0.1f, duration) + 1.1f);
    }
}
