using System;
using UnityEngine;

public class ExtractedStageCatalog : ScriptableObject
{
    [Serializable] public class Stage
    {
        public string id;
        public Material floor;
        public Color ambient, light, sky;
        public float intensity = 1;
    }
    public Stage[] stages;
    public static Stage Current()
    {
        if (GameManager.Instance == null || GameManager.Instance.selectedMap == GameManager.MapType.SnowyMountain) return null;
        var catalog = Resources.Load<ExtractedStageCatalog>("ExtractedStageCatalog");
        if (catalog == null || catalog.stages == null) return null;
        string id = "Stage" + (GameManager.Instance.selectedMap == GameManager.MapType.Desert ? "Desert" : "Forest") +
            (StageDirector.Instance != null ? Mathf.Clamp(StageDirector.Instance.CurrentStage, 1, 3) : 1);
        return Array.Find(catalog.stages, s => s.id == id);
    }
    public static void ApplyAtmosphere()
    {
        var stage = Current(); if (stage == null) return;
        RenderSettings.ambientLight = stage.ambient;
        if (RenderSettings.sun != null) { RenderSettings.sun.color = stage.light; RenderSettings.sun.intensity = stage.intensity; }
        // Keep the user's fog-free presentation.
        RenderSettings.fog = false;
        if (Camera.main != null) Camera.main.backgroundColor = stage.sky;
    }
}
