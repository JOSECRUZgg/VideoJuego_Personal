using UnityEngine;

/// <summary>Instancia exclusivamente la presentación, conservando rig, materiales y rotación de autor.</summary>
public static class OriginalModel
{
    public static bool BoundsOf(GameObject model, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            if (renderer.name.StartsWith("Ray") || renderer.name.StartsWith("MinimapIcon")) continue;
            if (!renderer.enabled || renderer.bounds.size.sqrMagnitude < .000001f) continue;
            if (!found) bounds = renderer.bounds; else bounds.Encapsulate(renderer.bounds);
            found = true;
        }
        return found;
    }

    public static GameObject Create(GameObject prefab, Transform parent, float height = 0f, bool preview = false)
    {
        if (prefab == null) return null;
        // Inactive parent prevents gameplay Awake/OnEnable callbacks during cloning.
        var container = new GameObject("Original_" + prefab.name);
        container.SetActive(false);
        container.transform.SetParent(parent, false);
        var model = Object.Instantiate(prefab, container.transform, false);
        model.transform.localPosition = Vector3.zero;
        foreach (var component in model.GetComponentsInChildren<MonoBehaviour>(true))
        { if(component != null) { component.enabled = false; Object.Destroy(component); } }
        foreach (var collider in model.GetComponentsInChildren<Collider>(true))
        { collider.enabled = false; Object.Destroy(collider); }
        foreach (var body in model.GetComponentsInChildren<Rigidbody>(true))
        { body.isKinematic = true; body.detectCollisions = false; Object.Destroy(body); }
        foreach (var audio in model.GetComponentsInChildren<AudioSource>(true))
        { audio.enabled = false; Object.Destroy(audio); }
        foreach (var camera in model.GetComponentsInChildren<Camera>(true))
        { camera.enabled = false; Object.Destroy(camera); }
        foreach (Transform child in model.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.tag = "Untagged";
            if (child.name.StartsWith("MinimapIcon") || child.name.StartsWith("Ray") || child.name == "Canvas" || child.name == "Progress")
                child.gameObject.SetActive(false);
            if (preview) child.gameObject.layer = 31;
        }
        if (preview)
            foreach (var particles in model.GetComponentsInChildren<ParticleSystem>(true))
                particles.gameObject.SetActive(false);
        foreach (var animator in model.GetComponentsInChildren<Animator>(true))
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = preview ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
        }
        container.SetActive(true);
        foreach (var animator in model.GetComponentsInChildren<Animator>())
        {
            if (animator.runtimeAnimatorController == null) continue;
            animator.Rebind();
            foreach (var state in new[] { "Base Layer.Idle", "Base Layer.idle" })
                if (animator.HasState(0, Animator.StringToHash(state))) { animator.Play(state, 0, 0); break; }
            animator.Update(0);
        }
        if (!BoundsOf(model, out var bounds))
        { container.SetActive(false); Object.Destroy(container); return null; }
        if (height > 0 && bounds.size.y > .001f)
        {
            model.transform.localScale *= height / bounds.size.y;
            BoundsOf(model, out bounds);
        }
        model.transform.position += container.transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        return container;
    }
}
