using System.Collections.Generic;
using UnityEngine;

public partial class ArenaEnricher
{
    static float OriginalDesertHeight(float x, float z, OriginalContentCatalog.DesertStage stage)
    {
        float value = 0, amplitude = 1, frequency = 1, total = 0;
        for (int i = 0; i < stage.octaves; i++)
        {
            float nx = (x / stage.uniformScale + stage.offset.x + stage.seed * 13.37f) / stage.noiseScale * frequency;
            float nz = (z / stage.uniformScale + stage.offset.y - stage.seed * 7.91f) / stage.noiseScale * frequency;
            value += Mathf.PerlinNoise(nx, nz) * amplitude; total += amplitude;
            amplitude *= stage.persistence; frequency *= stage.lacunarity;
        }
        return stage.heightCurve.Evaluate(value / Mathf.Max(.001f,total)) * stage.heightMultiplier * stage.uniformScale;
    }

    void BuildOriginalDesert()
    {
        var stage = OriginalContentCatalog.Desert;
        isProceduralDesert = true;
        BuildProceduralTerrain();
        var ground = transform.Find("DesertTerrain");
        if (ground != null && stage.terrainMaterial != null)
        {
            var renderer = ground.GetComponent<MeshRenderer>();
            var generated = renderer.sharedMaterial;
            renderer.sharedMaterial = stage.terrainMaterial;
            if (generated != stage.terrainMaterial) Destroy(generated);
        }
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = stage.ambient;
        foreach (var light in Object.FindObjectsByType<Light>())
        {
            if (light.type != LightType.Directional) continue;
            light.color = stage.sunlight; light.intensity = stage.lightIntensity;
        }
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = stage.sky;
            Camera.main.farClipPlane = stage.farClip;
        }
        var root = new GameObject("OriginalDesertDecorations"); root.transform.SetParent(transform,false);
        var occupied = new List<Vector3>(); var radii = new List<float>();
        var random = new System.Random(stage.seed + (StageDirector.Instance != null ? StageDirector.Instance.CurrentStage : 1));
        foreach (var group in stage.decorations)
        {
            if (group.prefabs == null || group.prefabs.Length == 0) continue;
            for (int i = 0; i < group.amount; i++)
            {
                float scale = Mathf.Lerp(group.scaleMin,group.scaleMax,(float)random.NextDouble());
                var prefab = group.prefabs[random.Next(group.prefabs.Length)];
                Vector3 point = Vector3.zero, normal = Vector3.up;
                bool placed = false;
                float spacing = Mathf.Max(.6f,group.checkRadius * scale);
                if (OriginalModel.BoundsOf(prefab,out var prefabBounds))
                    spacing = Mathf.Max(spacing, Mathf.Max(prefabBounds.extents.x,prefabBounds.extents.z) * scale);
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2;
                    float radius = Mathf.Sqrt((float)random.NextDouble()) * 215;
                    point = new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                    // Reservar recorridos y zonas de los interactuables actuales.
                    if (Mathf.Abs(point.x) < 7+spacing || Mathf.Abs(point.z) < 7+spacing || point.magnitude < 32+spacing || point.magnitude+spacing>224) continue;
                    if (Vector3.Distance(point,new Vector3(0,0,110))<28+spacing || Vector3.Distance(point,new Vector3(0,0,-110))<28+spacing ||
                        Vector3.Distance(point,new Vector3(115,0,0))<28+spacing || Vector3.Distance(point,new Vector3(-115,0,0))<28+spacing) continue;
                    bool blocked = false;
                    for(int n=0;n<occupied.Count;n++) if ((point-occupied[n]).sqrMagnitude < Mathf.Pow(spacing+radii[n],2)) { blocked=true; break; }
                    if (blocked || !TryGetSnowHeight(point,out float y)) continue;
                    float dx=OriginalDesertHeight(point.x+1,point.z,stage)-OriginalDesertHeight(point.x-1,point.z,stage);
                    float dz=OriginalDesertHeight(point.x,point.z+1,stage)-OriginalDesertHeight(point.x,point.z-1,stage);
                    normal=new Vector3(-dx,2,-dz).normalized;
                    if(Vector3.Angle(normal,Vector3.up)>Mathf.Min(45,group.maxSlopeAngle)) continue;
                    occupied.Add(point); radii.Add(spacing);
                    point.y=y+group.upOffset; placed=true; break;
                }
                if (!placed) continue;
                var decoration = OriginalModel.Create(prefab,root.transform);
                if (decoration == null) continue;
                decoration.transform.localScale = Vector3.one * scale;
                decoration.transform.rotation = (group.alignWithNormal ? Quaternion.FromToRotation(Vector3.up,normal) : Quaternion.identity) *
                    Quaternion.Euler(0,(float)random.NextDouble()*360,0);
                decoration.transform.position = point;
                if(OriginalModel.BoundsOf(decoration,out var bounds)) decoration.transform.position += Vector3.up*(point.y-bounds.min.y);
                bool solid = prefab.name.Contains("Rock") || prefab.name.Contains("Cactus") || prefab.name.Contains("Tree") || prefab.name.Contains("Grave") ||
                    prefab.name.Contains("House") || prefab.name.Contains("Landmark") || prefab.name.Contains("Ladnmark") || prefab.name.Contains("Log");
                foreach (var filter in decoration.GetComponentsInChildren<MeshFilter>())
                {
                    filter.gameObject.isStatic = true;
                    if (solid && filter.sharedMesh != null && filter.GetComponent<MeshRenderer>() != null)
                        filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                }
            }
        }
        Physics.SyncTransforms();
    }
}
