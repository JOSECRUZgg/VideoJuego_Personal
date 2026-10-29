using UnityEngine;

/// <summary>Original effect prefabs, without gameplay components or collision. Owned by the attack.</summary>
public static class SpellModelEffects
{
    public static GameObject Attach(string name,Transform parent,float size)
    {
        var prefab=Resources.Load<GameObject>("ExtractedVisuals/GameObject/"+name);
        if(prefab==null) return null;
        var holder=new GameObject("OriginalSpell_"+name); holder.SetActive(false); holder.transform.SetParent(parent,false);
        var fx=Object.Instantiate(prefab,holder.transform,false);
        foreach(var script in fx.GetComponentsInChildren<MonoBehaviour>(true)) if(script!=null) { script.enabled=false; Object.Destroy(script); }
        foreach(var collider in fx.GetComponentsInChildren<Collider>(true)) { collider.enabled=false; Object.Destroy(collider); }
        foreach(var body in fx.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic=true; body.detectCollisions=false; Object.Destroy(body); }
        foreach(var audio in fx.GetComponentsInChildren<AudioSource>(true)) { audio.enabled=false; Object.Destroy(audio); }
        foreach(var camera in fx.GetComponentsInChildren<Camera>(true)) { camera.enabled=false; Object.Destroy(camera); }
        bool usable=false;
        foreach(var renderer in fx.GetComponentsInChildren<Renderer>(true))
        {
            bool valid=renderer.sharedMaterials.Length>0;
            foreach(var material in renderer.sharedMaterials) if(material==null || material.shader==null || !material.shader.isSupported) valid=false;
            renderer.enabled &= valid;
            if(valid) usable=true;
        }
        if(!usable) { Object.Destroy(holder); return null; }
        // size means world-space diameter, not a multiplier of arbitrary export units.
        float extent=.1f;
        foreach(var filter in fx.GetComponentsInChildren<MeshFilter>(true))
        {
            if(filter.sharedMesh==null) continue;
            var matrix=holder.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
            var bounds=filter.sharedMesh.bounds;
            extent=Mathf.Max(extent,2*(matrix.MultiplyPoint3x4(bounds.center).magnitude+
                matrix.MultiplyVector(bounds.extents).magnitude));
        }
        foreach(var particle in fx.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main=particle.main; var shape=particle.shape;
            float reach=Mathf.Abs(main.startSize.constantMax)+2*Mathf.Abs(main.startSpeed.constantMax)*Mathf.Min(2,main.startLifetime.constantMax);
            if(shape.enabled) reach+=2*Mathf.Abs(shape.radius);
            extent=Mathf.Max(extent,reach*Mathf.Max(.01f,particle.transform.lossyScale.magnitude/Mathf.Max(.01f,holder.transform.lossyScale.magnitude)));
        }
        holder.transform.localScale=Vector3.one*Mathf.Clamp(size,.2f,6f)/extent;
        foreach(var particle in fx.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main=particle.main; main.maxParticles=Mathf.Min(main.maxParticles,100); main.scalingMode=ParticleSystemScalingMode.Hierarchy;
        }
        holder.SetActive(true);
        foreach(var particle in fx.GetComponentsInChildren<ParticleSystem>()) particle.Play();
        return holder;
    }
    public static void Burst(string name,Vector3 position,float diameter,float duration)
    {
        var root=new GameObject("SizedSpell_"+name); root.transform.position=position;
        Attach(name,root.transform,diameter); Object.Destroy(root,duration);
    }
}
