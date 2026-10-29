using UnityEngine;

/// <summary>Puente entre los recursos visuales importados y las mecánicas de esta partida.</summary>
public static class ExtractedVisuals
{
    static readonly System.Collections.Generic.Dictionary<string, GameObject> prefabs = new System.Collections.Generic.Dictionary<string, GameObject>();
    static readonly System.Collections.Generic.Dictionary<string, AudioClip> sounds = new System.Collections.Generic.Dictionary<string, AudioClip>();
    static float nextSound;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { prefabs.Clear(); sounds.Clear(); nextSound = 0; }
    static GameObject Load(string name)
    {
        if (!prefabs.TryGetValue(name, out var prefab))
            prefabs[name] = prefab = Resources.Load<GameObject>("ExtractedVisuals/GameObject/" + name);
        return prefab;
    }

    public static GameObject Attach(string name, Transform parent, float height, bool ground = true)
    {
        var prefab = Load(name);
        if (prefab == null) return null;
        var obj = Object.Instantiate(prefab, parent, false);
        obj.name = "Imported_" + name;
        obj.transform.localPosition = Vector3.zero;
        // El prefab puede incluir una rotación correctiva del exportador.
        obj.transform.localRotation = prefab.transform.localRotation;
        foreach (Transform child in obj.GetComponentsInChildren<Transform>(true))
        {
            if (child == obj.transform) continue;
            if (child.name.StartsWith("MinimapIcon") || child.name.StartsWith("AltarIcon") ||
                child.name == "Canvas" || child.name == "Progress") child.gameObject.SetActive(false);
        }
        foreach (var animator in obj.GetComponentsInChildren<Animator>(true))
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        var renderers = obj.GetComponentsInChildren<Renderer>();
        bool found = false;
        Bounds bounds = default;
        foreach (var r in renderers)
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
            if (!found) { bounds = r.bounds; found = true; } else bounds.Encapsulate(r.bounds);
        }
        float extent = ground ? bounds.size.y : Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (found && extent > .001f)
        {
            float scale = height / extent;
            obj.transform.localScale *= scale;
            // Bounds mundiales se recalculan tras escalar: invertir una esquina de
            // un AABB rotado daba una altura falsa y enterraba algunos santuarios.
            found = false;
            foreach (var r in renderers)
            {
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                if (!found) { bounds = r.bounds; found = true; } else bounds.Encapsulate(r.bounds);
            }
            if (found)
            {
                Vector3 target = parent.position;
                obj.transform.position += new Vector3(target.x - bounds.center.x,
                    target.y - (ground ? bounds.min.y : bounds.center.y), target.z - bounds.center.z);
            }
        }
        return obj;
    }

    public static void Burst(string name, Vector3 position, float lifetime = 2f)
    {
        if (Time.timeScale <= 0 || ImportedBurstLifetime.Active >= 40) return;
        var prefab = Load(name);
        if (prefab == null) return;
        var obj = Object.Instantiate(prefab, position, prefab.transform.rotation);
        obj.AddComponent<ImportedBurstLifetime>();
        foreach (var particles in obj.GetComponentsInChildren<ParticleSystem>())
        {
            var main = particles.main;
            main.maxParticles = Mathf.Min(main.maxParticles, 160);
        }
        Object.Destroy(obj, lifetime);
    }

    public static void Sound(string name, Vector3 position)
    {
        if (Time.time < nextSound || Time.timeScale <= 0) return;
        if (!sounds.TryGetValue(name, out var clip))
            sounds[name] = clip = Resources.Load<AudioClip>("ExtractedVisuals/AudioClip/" + name);
        if (clip == null) return;
        nextSound = Time.time + .12f;
        AudioSource.PlayClipAtPoint(clip, position, .35f);
    }
}
