using System.Collections.Generic;
using UnityEngine;

/// <summary>Meshes for equipped weapons and acquired objects. Presentation never deals damage.</summary>
public class BuildModelPresentation : MonoBehaviour
{
    readonly Dictionary<string,Transform> equipped=new Dictionary<string,Transform>();
    public static string Model(string id)
    {
        switch(id)
        {
            case "mb_bone": return "B_Bones1";
            case "mb_banana": return "BananaProjectile";
            case "mb_rocket": return "RocketProjectile";
            case "mb_bow": return "BowProjectile";
            case "mb_dagger": return "BluetoothProjectile";
            case "borgar": return "Borgar";
            case "key": return "KeyPickupMonke";
            case "campfire": return "Campfire";
            case "quins_mask": return "QuinsMask";
            default: return ExtendedWeapons.Get(id)?.model;
        }
    }
    public static GameObject Create(string id,Transform parent,float size)
    {
        // These meshes exist independently of the icon-only item prefabs.
        string meshName=id=="key"?"model_key":id=="holy_book"?"Book":null;
        if(meshName!=null)
        {
            var mesh=Resources.Load<Mesh>("ExtractedVisuals/Mesh/"+meshName);
            if(mesh!=null)
            {
                var model=new GameObject("RecoveredMesh_"+meshName); model.transform.SetParent(parent,false);
                model.AddComponent<MeshFilter>().sharedMesh=mesh;
                var color=id=="key"?new Color(1,.72f,.12f):new Color(.45f,.18f,.08f);
                var renderer=model.AddComponent<MeshRenderer>();
                var originalMaterials=FindMeshMaterials(mesh,id=="key"?new[]{"KeyPickupMonke"}:new[]{"FoxWizard","CoffinTile"});
                if(originalMaterials!=null) renderer.sharedMaterials=originalMaterials;
                else renderer.sharedMaterial=RuntimeMaterialCache.Get(Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard"),color,Color.black);
                float extent=Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z));
                model.transform.localScale=Vector3.one*size/Mathf.Max(.001f,extent);
                model.transform.localPosition=-mesh.bounds.center*model.transform.localScale.x;
                return model;
            }
        }
        string resource=Model(id);
        if(resource!=null)
        {
            var prefab=Resources.Load<GameObject>("ExtractedVisuals/GameObject/"+resource);
            if(prefab!=null)
            {
                var model=OriginalModel.Create(prefab,parent);
                if(model!=null && OriginalModel.BoundsOf(model,out var bounds))
                {
                    bool valid=true;
                    foreach(var renderer in model.GetComponentsInChildren<Renderer>())
                        foreach(var mat in renderer.sharedMaterials)
                            if(mat==null || mat.shader==null || !mat.shader.isSupported) valid=false;
                    if(valid)
                    {
                        foreach(var particles in model.GetComponentsInChildren<ParticleSystem>()) particles.gameObject.SetActive(false);
                        float extent=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
                        model.transform.localScale*=size/Mathf.Max(.001f,extent);
                        return model;
                    }
                    model.SetActive(false); Destroy(model);
                }
            }
        }
        return IconVolume.Create(id,parent,size);
    }
    static Material[] FindMeshMaterials(Mesh mesh,string[] prefabs)
    {
        foreach(var name in prefabs)
        {
            var prefab=Resources.Load<GameObject>("ExtractedVisuals/GameObject/"+name);
            if(prefab==null) continue;
            foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter.sharedMesh!=mesh) continue;
                var renderer=filter.GetComponent<MeshRenderer>();
                if(renderer==null) continue;
                var materials=renderer.sharedMaterials;
                bool valid=materials.Length>=mesh.subMeshCount;
                foreach(var material in materials) if(material==null || material.shader==null || !material.shader.isSupported) valid=false;
                if(valid) return materials;
            }
        }
        return null;
    }
    public static void Acquired(string id)
    {
        var player=GameObject.FindGameObjectWithTag("Player");
        if(player==null) return;
        var trophy=new GameObject("Acquired3D_"+id);
        trophy.transform.position=player.transform.position+Vector3.up*2.5f;
        Create(id,trophy.transform,1.1f);
        trophy.AddComponent<AcquiredModelMotion>();
    }
    void Update()
    {
        var gm=GameManager.Instance;
        if(gm==null || gm.gameState==GameManager.GameState.MainMenu || gm.gameState==GameManager.GameState.GameOver || gm.gameState==GameManager.GameState.Victory)
        { Clear(); return; }
        // Inventory icons belong in the HUD. Combat weapons are now presented by
        // WeaponCastMotion when an actual attack is executed, not as orbiting trophies.
        Clear();
    }
    void Clear() { foreach(var model in equipped.Values) if(model!=null) Destroy(model.gameObject); equipped.Clear(); }
    void OnDisable() { Clear(); }
}

public class AcquiredModelMotion : MonoBehaviour
{
    float life=2.5f;
    void Update()
    {
        if(GameManager.Instance==null || GameManager.Instance.gameState==GameManager.GameState.MainMenu) { Destroy(gameObject); return; }
        if(Time.timeScale<=0) return;
        transform.Rotate(0,90*Time.deltaTime,0,Space.World);
        transform.position+=Vector3.up*(.25f*Time.deltaTime);
        if((life-=Time.deltaTime)<=0) Destroy(gameObject);
    }
}

/// <summary>Explicit reconstructed fallback: extruded pixel silhouette, not an original mesh.</summary>
public static class IconVolume
{
    static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
    static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        foreach(var mesh in meshes.Values) if(mesh!=null) Object.Destroy(mesh);
        foreach(var mat in materials.Values) if(mat!=null) Object.Destroy(mat);
        meshes.Clear(); materials.Clear();
    }
    public static GameObject Create(string id,Transform parent,float size)
    {
        var texture=id.StartsWith("mb_")?MegabonkIconProvider.UpgradeTexture(id):MegabonkIconProvider.ItemTexture(id);
        if(texture==null) { Debug.LogWarning("No hay modelo ni icono para "+id); return null; }
        if(!meshes.TryGetValue(id,out var mesh))
        {
            const int n=32;
            var rt=RenderTexture.GetTemporary(n,n,0,RenderTextureFormat.ARGB32);
            var old=RenderTexture.active;
            var sample=new Texture2D(n,n,TextureFormat.RGBA32,false);
            Color32[] pixels;
            try { Graphics.Blit(texture,rt); RenderTexture.active=rt; sample.ReadPixels(new Rect(0,0,n,n),0,0); sample.Apply(); pixels=sample.GetPixels32(); }
            finally { RenderTexture.active=old; RenderTexture.ReleaseTemporary(rt); Object.Destroy(sample); }
            var vertices=new List<Vector3>(); var uv=new List<Vector2>(); var indices=new List<int>();
            var corners=new[]{new Vector3(0,0,-.09f),new Vector3(1,0,-.09f),new Vector3(1,1,-.09f),new Vector3(0,1,-.09f),new Vector3(0,0,.09f),new Vector3(1,0,.09f),new Vector3(1,1,.09f),new Vector3(0,1,.09f)};
            int[] faces={0,3,2,1,4,5,6,7,0,4,7,3,1,2,6,5,3,7,6,2,0,1,5,4};
            for(int y=0;y<n;y++) for(int x=0;x<n;x++)
            {
                if(pixels[y*n+x].a<96) continue;
                for(int f=0;f<6;f++)
                {
                    // Omit internal voxel walls; the silhouette stays solid with fewer triangles.
                    if(f==2 && x>0 && pixels[y*n+x-1].a>=96) continue;
                    if(f==3 && x<n-1 && pixels[y*n+x+1].a>=96) continue;
                    if(f==4 && y<n-1 && pixels[(y+1)*n+x].a>=96) continue;
                    if(f==5 && y>0 && pixels[(y-1)*n+x].a>=96) continue;
                    int start=vertices.Count;
                    for(int k=0;k<4;k++) { var c=corners[faces[f*4+k]]; vertices.Add(new Vector3((x+c.x)/n-.5f,(y+c.y)/n,c.z)); uv.Add(new Vector2((x+.5f)/n,(y+.5f)/n)); }
                    indices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
                }
            }
            mesh=new Mesh {name="ReconstructedIconVolume_"+id}; mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(indices,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes[id]=mesh;
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.SetTexture("_BaseMap",texture); material.SetTexture("_MainTex",texture); material.color=Color.white;
            materials[id]=material;
        }
        var go=new GameObject("ReconstructedVolume_"+id); go.transform.SetParent(parent,false); go.transform.localScale=Vector3.one*size;
        go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=materials[id];
        return go;
    }
}
