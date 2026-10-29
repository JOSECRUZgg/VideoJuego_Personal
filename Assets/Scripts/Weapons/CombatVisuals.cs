using UnityEngine;

/// <summary>Presentación exclusivamente visual; nunca aplica daño ni cambia las colisiones de combate.</summary>
public class CombatVisuals : MonoBehaviour
{
    float lifetime, elapsed;
    LineRenderer line;
    bool expand;
    static int activeBursts;
    static readonly System.Collections.Generic.HashSet<string> unavailableProjectileMeshes =
        new System.Collections.Generic.HashSet<string>();
    bool counted;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCount() { activeBursts = 0; unavailableProjectileMeshes.Clear(); }
    static Material Material(Color color) => RuntimeMaterialCache.Get(
        Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"), color, color);

    static Transform Part(Transform parent, PrimitiveType shape, Vector3 position, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(shape);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position; go.transform.localScale = scale;
        go.GetComponent<Collider>().enabled = false;
        go.GetComponent<Renderer>().sharedMaterial = Material(color);
        go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go.transform;
    }

    static bool TryMeshProjectile(string prefabName, Transform parent, float size)
    {
        if (unavailableProjectileMeshes.Contains(prefabName)) return false;
        var imported = ExtractedVisuals.Attach(prefabName, parent, size, false);
        if (imported == null) { unavailableProjectileMeshes.Add(prefabName); return false; }
        bool valid = false;
        foreach (var renderer in imported.GetComponentsInChildren<MeshRenderer>(true))
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (renderer.enabled && renderer.gameObject.activeInHierarchy && filter != null && filter.sharedMesh != null &&
                filter.sharedMesh.vertexCount > 0 && filter.sharedMesh.bounds.size.sqrMagnitude > .000001f && renderer.sharedMaterial != null &&
                renderer.sharedMaterial.shader != null && renderer.sharedMaterial.shader.isSupported)
            { valid = true; break; }
        }
        if (!valid)
        {
            foreach (var renderer in imported.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.sharedMesh != null && renderer.sharedMesh.vertexCount > 0 &&
                    renderer.sharedMesh.bounds.size.sqrMagnitude > .000001f && renderer.sharedMaterial != null &&
                    renderer.sharedMaterial.shader != null && renderer.sharedMaterial.shader.isSupported)
                { valid = true; break; }
        }
        if (valid) return true;
        imported.SetActive(false);
        Object.Destroy(imported);
        unavailableProjectileMeshes.Add(prefabName);
        return false;
    }

    public static void Projectile(Transform parent, string kind, float size, Color tint)
    {
        if(kind=="mb_fire") SpellModelEffects.Attach("FireballProjectile",parent,size);
        if(kind=="mb_blood") SpellModelEffects.Attach("BloodStaffProjectile",parent,size);
        if(kind=="energy") SpellModelEffects.Attach("HomingOrb",parent,size);
        // ProjectileBone, FireballProjectile, RevolverProjectile y HomingOrb son
        // sistemas de partículas sin malla: en runtime se veían como bolas blancas.
        string model = kind == "mb_rocket" ? "RocketProjectile" : kind == "mb_bow" ? "BowProjectile" :
            kind == "mb_bone" ? "B_Bones1" : kind == "mb_banana" ? "BananaProjectile" : ExtendedWeapons.Get(kind)?.model;
        if(model!=null && (kind=="mb_blackhole" || kind=="mb_dexecutioner" || kind=="mb_sniper" || kind=="mb_corrupted"))
            SpellModelEffects.Attach(model,parent,size);
        if (model != null && TryMeshProjectile(model, parent,
            Mathf.Max(kind == "mb_bone" ? .8f : .2f, size * (kind == "mb_bone" ? .9f : .6f))))
        {
            AddProjectileTrail(parent, kind, size, tint);
            return;
        }
        var root = new GameObject("Visual_" + kind).transform;
        root.SetParent(parent, false);
        root.localScale = Vector3.one * Mathf.Clamp(size, .5f, 4f);
        if(kind=="mb_blackhole")
        {
            Part(root,PrimitiveType.Sphere,Vector3.zero,Vector3.one*.48f,new Color(.035f,.015f,.09f));
            for(int i=0;i<3;i++)
            {
                float angle=i*2.094f;
                Part(root,PrimitiveType.Sphere,new Vector3(Mathf.Cos(angle)*.34f,Mathf.Sin(angle)*.34f,0),
                    Vector3.one*.12f,new Color(.72f,.25f,1f));
            }
        }
        else if(kind=="mb_dexecutioner")
        {
            // Readable advancing blade wave if extracted particle materials are unavailable.
            Part(root,PrimitiveType.Cube,Vector3.zero,new Vector3(1.4f,.12f,.16f),new Color(.58f,.12f,.75f));
            Part(root,PrimitiveType.Cube,Vector3.forward*.1f,new Vector3(.85f,.05f,.20f),new Color(1f,.48f,.9f));
        }
        else if (kind == "mb_corrupted")
        {
            // Chadwell emits a traveling sword wave, not a stationary dagger.
            var violet = new Color(.58f,.15f,.88f);
            for (int i=-2;i<=2;i++)
                Part(root,PrimitiveType.Cube,new Vector3(i*.28f,0,.12f*Mathf.Abs(i)),
                    new Vector3(.31f,.12f,.16f),violet);
            Part(root,PrimitiveType.Cube,Vector3.forward*.12f,new Vector3(.65f,.05f,.18f),new Color(1f,.48f,1f));
        }
        else if (kind == "mb_dagger" || kind == "mb_sniper")
        {
            Part(root,PrimitiveType.Cube,Vector3.zero,new Vector3(.12f,.05f,1.2f),tint);
            Part(root,PrimitiveType.Cube,Vector3.back*.35f,new Vector3(.4f,.08f,.10f),Color.gray);
        }
        else if (kind == "mb_dice")
        {
            Part(root,PrimitiveType.Cube,Vector3.zero,Vector3.one*.45f,Color.white);
            for(int i=-1;i<=1;i++) Part(root,PrimitiveType.Sphere,new Vector3(i*.13f,.235f,i*.13f),Vector3.one*.065f,Color.black);
        }
        else if (kind == "mb_poison")
        {
            Part(root,PrimitiveType.Sphere,Vector3.zero,Vector3.one*.4f,tint);
            Part(root,PrimitiveType.Cylinder,Vector3.up*.25f,new Vector3(.14f,.12f,.14f),new Color(.45f,.25f,.1f));
        }
        else if (kind == "mb_axe")
        {
            Part(root,PrimitiveType.Cube,Vector3.zero,new Vector3(.08f,.65f,.08f),new Color(.5f,.25f,.1f));
            Part(root,PrimitiveType.Cube,Vector3.up*.2f,new Vector3(.5f,.28f,.08f),Color.gray);
        }
        else if (kind == "mb_blood")
        {
            Part(root,PrimitiveType.Capsule,Vector3.zero,new Vector3(.18f,.32f,.18f),tint).localRotation=Quaternion.Euler(90,0,0);
        }
        else if (kind == "mb_rocket")
        {
            Part(root, PrimitiveType.Capsule, Vector3.zero, new Vector3(.23f,.44f,.23f), new Color(.8f,.85f,.9f)).localRotation = Quaternion.Euler(90,0,0);
            Part(root, PrimitiveType.Sphere, Vector3.forward*.4f, new Vector3(.24f,.24f,.32f), Color.red);
            for(int i=0;i<4;i++)
            {
                var fin = Part(root,PrimitiveType.Cube,new Vector3(0,0,-.28f),new Vector3(.08f,.65f,.24f),new Color(.2f,.25f,.3f));
                fin.localRotation=Quaternion.Euler(0,0,i*90);
            }
            var exhaust=new GameObject("Exhaust").transform; exhaust.SetParent(root,false); exhaust.localPosition=Vector3.back*.5f;
            SpellVFX.Fire(exhaust,4,.35f);
        }
        else if(kind=="mb_bow")
        {
            Part(root,PrimitiveType.Cube,Vector3.zero,new Vector3(.06f,.06f,1.1f),new Color(.55f,.3f,.12f));
            Part(root,PrimitiveType.Sphere,Vector3.forward*.55f,new Vector3(.18f,.10f,.3f),Color.white);
            Part(root,PrimitiveType.Cube,Vector3.back*.4f,new Vector3(.35f,.035f,.22f),Color.cyan);
        }
        else if(kind=="mb_bone")
        {
            Part(root,PrimitiveType.Capsule,Vector3.zero,new Vector3(.12f,.35f,.12f),new Color(1,.94f,.75f)).localRotation=Quaternion.Euler(90,0,0);
            for(int end=-1;end<=1;end+=2)
                for(int side=-1;side<=1;side+=2)
                    Part(root,PrimitiveType.Sphere,new Vector3(side*.08f,0,end*.3f),Vector3.one*.22f,new Color(1,.94f,.75f));
        }
        else if (kind == "mb_fire")
        {
            var ember = new Color(1f, .25f, .025f);
            Part(root, PrimitiveType.Sphere, Vector3.zero, Vector3.one * .42f, ember);
            Part(root, PrimitiveType.Sphere, Vector3.forward * .16f, Vector3.one * .23f, new Color(1f, .9f, .22f));
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f * Mathf.Deg2Rad;
                var flame = Part(root, PrimitiveType.Capsule,
                    new Vector3(Mathf.Cos(angle) * .14f, Mathf.Sin(angle) * .14f, -.24f),
                    new Vector3(.10f, .26f, .10f), new Color(1f, .48f, .035f));
                flame.localRotation = Quaternion.Euler(75f, 0f, i * 120f);
            }
            SpellVFX.Fire(root, 4f, .55f);
        }
        else if (kind == "energy")
        {
            Part(root, PrimitiveType.Sphere, Vector3.zero, Vector3.one * .34f, tint);
            Part(root, PrimitiveType.Sphere, Vector3.forward * .12f, Vector3.one * .16f, Color.white);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f * Mathf.Deg2Rad;
                Part(root, PrimitiveType.Sphere,
                    new Vector3(Mathf.Cos(angle) * .24f, Mathf.Sin(angle) * .24f, -.10f),
                    Vector3.one * .10f, tint);
            }
        }
        else
        {
            Part(root,PrimitiveType.Sphere,Vector3.zero,kind=="mb_revolver"?new Vector3(.1f,.1f,.38f):Vector3.one*.4f,tint);
            if(kind=="meatball") SpellVFX.Fire(root,4,.45f);
        }
        AddProjectileTrail(parent, kind, size, tint);
    }

    static void AddProjectileTrail(Transform parent, string kind, float size, Color tint)
    {
        var trail=parent.gameObject.AddComponent<TrailRenderer>();
        trail.time=kind=="mb_revolver"?.08f:.24f;
        trail.startWidth=.12f*size; trail.endWidth=0;
        trail.sharedMaterial=Material(tint);
        trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    public static void Ghost(Transform parent)
    {
        // GhostProjectile también es solo un sistema de partículas sin malla.
        Part(parent,PrimitiveType.Capsule,Vector3.zero,new Vector3(.6f,.45f,.5f),new Color(.4f,.85f,1));
        for(int side=-1;side<=1;side+=2)
        {
            Part(parent,PrimitiveType.Sphere,new Vector3(side*.13f,.13f,.24f),Vector3.one*.12f,new Color(.015f,.03f,.08f));
            Part(parent,PrimitiveType.Sphere,new Vector3(side*.38f,-.05f,0),new Vector3(.3f,.12f,.18f),Color.cyan);
        }
        for(int i=0;i<3;i++) Part(parent,PrimitiveType.Sphere,new Vector3((i-1)*.18f,-.38f,0),new Vector3(.2f,.25f,.3f),Color.cyan);
    }

    static CombatVisuals Effect(Vector3 position, Color color, float duration)
    {
        if(activeBursts>=80 || Time.timeScale<=0) return null;
        var go=new GameObject("CombatPulse"); go.transform.position=position;
        var effect=go.AddComponent<CombatVisuals>();
        effect.counted=true; activeBursts++; effect.lifetime=duration;
        effect.line=go.AddComponent<LineRenderer>();
        effect.line.useWorldSpace=false; effect.line.widthMultiplier=.10f;
        effect.line.sharedMaterial=Material(color);
        effect.line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        return effect;
    }
    public static void Ring(Vector3 position,float radius,Color color)
    {
        var fx=Effect(position,color,.45f); if(fx==null) return;
        fx.expand=true; fx.line.loop=true; fx.line.positionCount=48;
        for(int i=0;i<48;i++) { float a=i*Mathf.PI*2/48; fx.line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius)); }
    }
    public static void Slash(Vector3 position,Vector3 direction,float radius,bool katana)
    {
        var fx=Effect(position,katana?new Color(1,.35f,.65f):new Color(.8f,.95f,1),.22f); if(fx==null) return;
        fx.transform.rotation=Quaternion.LookRotation(direction.sqrMagnitude>.01f?direction:Vector3.forward);
        fx.line.positionCount=25;
        for(int i=0;i<25;i++) { float a=Mathf.Lerp(-2.3f,2.3f,i/24f); fx.line.SetPosition(i,new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius)); }
    }
    public static void Lightning(Vector3 start,Vector3 end)
    {
        ExtractedVisuals.Burst("LightningStrikeFx",end,.45f);
        var fx=Effect(start,Color.cyan,.16f); if(fx==null) return;
        fx.line.positionCount=12; Vector3 delta=end-start;
        Vector3 side=Vector3.Cross(delta.normalized,Vector3.up).normalized;
        for(int i=0;i<12;i++) fx.line.SetPosition(i,delta*(i/11f)+(i==0||i==11?Vector3.zero:side*Mathf.Sin(i*8.1f)*.35f));
    }
    void Update()
    {
        if(Time.timeScale<=0) return;
        elapsed+=Time.deltaTime;
        float t=Mathf.Clamp01(elapsed/lifetime);
        line.widthMultiplier=.1f*(1-t);
        if(expand) transform.localScale=Vector3.one*Mathf.Lerp(.85f,1,t);
        if(elapsed>=lifetime) Destroy(gameObject);
    }
    void OnDestroy() { if(counted) activeBursts=Mathf.Max(0,activeBursts-1); }
}
