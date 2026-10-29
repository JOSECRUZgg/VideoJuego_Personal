using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Hierba y flores agrupadas por celdas: sin colliders, luces ni miles de Updates.</summary>
public class ForestGroundCover : MonoBehaviour
{
    class Part { public Mesh mesh; public Material material; public int submesh; public Matrix4x4 local; }
    class Batch { public int type; public Bounds bounds; public Matrix4x4[] matrices; public List<Matrix4x4> pending = new List<Matrix4x4>(); }
    readonly List<Part>[] parts = { new List<Part>(), new List<Part>() };
    readonly Dictionary<string,Batch> cells = new Dictionary<string,Batch>();
    readonly List<Material> ownedMaterials = new List<Material>();
    readonly List<Batch> batches = new List<Batch>();
    Camera viewCamera;
    readonly Plane[] planes = new Plane[6];

    public void Configure(GameObject grass, GameObject flowers)
    {
        Prepare(grass,0); Prepare(flowers!=null?flowers:grass,1);
    }
    void Prepare(GameObject prefab,int type)
    {
        if(prefab==null) return;
        Renderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>();
        var lod = prefab.GetComponentInChildren<LODGroup>();
        if(lod!=null && lod.GetLODs().Length>0) renderers=lod.GetLODs()[0].renderers;
        var materials = new Dictionary<Material,Material>();
        foreach(var renderer in renderers)
        {
            if(renderer==null) continue;
            var filter=renderer.GetComponent<MeshFilter>();
            if(filter==null || filter.sharedMesh==null) continue;
            for(int sub=0;sub<filter.sharedMesh.subMeshCount && sub<renderer.sharedMaterials.Length;sub++)
            {
                Material source=renderer.sharedMaterials[sub];
                if(source==null) continue;
                if(!materials.TryGetValue(source,out var material))
                {
                    material=new Material(source); material.enableInstancing=true;
                    materials[source]=material; ownedMaterials.Add(material);
                }
                // El render por instancias no crea la raíz del prefab: incluir aquí
                // su rotación original evita que hierbas y flores queden acostadas.
                Matrix4x4 root=Matrix4x4.TRS(Vector3.zero,prefab.transform.localRotation,prefab.transform.localScale);
                parts[type].Add(new Part {mesh=filter.sharedMesh, material=material, submesh=sub,
                    local=root*prefab.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix});
            }
        }
    }
    public void Add(Vector3 position,float angle,float scale,bool flower)
    {
        int type=flower?1:0;
        int x=Mathf.FloorToInt(position.x/24), z=Mathf.FloorToInt(position.z/24);
        string key=type+"_"+x+"_"+z;
        if(!cells.TryGetValue(key,out var cell))
        {
            cell=new Batch {type=type,bounds=new Bounds(position,Vector3.one*8)};
            cells[key]=cell;
        }
        cell.bounds.Encapsulate(new Bounds(position,Vector3.one*8));
        cell.pending.Add(Matrix4x4.TRS(position,Quaternion.Euler(0,angle,0),Vector3.one*scale));
    }
    public void Finish()
    {
        foreach(var cell in cells.Values)
            for(int start=0;start<cell.pending.Count;start+=1023)
            {
                int count=Mathf.Min(1023,cell.pending.Count-start);
                // Cada submalla conserva su transformación local del prefab.
                for(int part=0;part<parts[cell.type].Count;part++)
                {
                    var batch=new Batch {type=cell.type*100+part,bounds=cell.bounds,matrices=new Matrix4x4[count]};
                    for(int i=0;i<count;i++) batch.matrices[i]=cell.pending[start+i]*parts[cell.type][part].local;
                    batch.pending=null; batches.Add(batch);
                }
            }
        cells.Clear();
    }
    void LateUpdate()
    {
        if(viewCamera==null) viewCamera=Camera.main;
        if(viewCamera==null) return;
        GeometryUtility.CalculateFrustumPlanes(viewCamera,planes);
        Vector3 cameraPosition=viewCamera.transform.position;
        foreach(var batch in batches)
        {
            if((batch.bounds.center-cameraPosition).sqrMagnitude>155*155 || !GeometryUtility.TestPlanesAABB(planes,batch.bounds)) continue;
            Part part=parts[batch.type/100][batch.type%100];
            if(SystemInfo.supportsInstancing)
                Graphics.DrawMeshInstanced(part.mesh,part.submesh,part.material,batch.matrices,batch.matrices.Length,null,ShadowCastingMode.Off,true,gameObject.layer,viewCamera,LightProbeUsage.Off);
            else
                foreach(var matrix in batch.matrices) Graphics.DrawMesh(part.mesh,matrix,part.material,gameObject.layer,viewCamera,part.submesh,null,false,true,false);
        }
    }
    void OnDestroy() { foreach(var material in ownedMaterials) if(material!=null) Destroy(material); }
}
