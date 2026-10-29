using UnityEngine;

/// <summary>Cámara aislada para inspeccionar el modelo completo, sin físicas ni scripts de partida.</summary>
public class CharacterPreview3D : MonoBehaviour
{
    GameObject studio, model;
    Transform pivot;
    Camera previewCamera;
    RenderTexture texture;
    string characterId;
    float yaw = 180f, targetYaw = 180f, yawVelocity, radius;
    Vector3 center;
    bool dragging;
    int lastDrawFrame;
    readonly System.Collections.Generic.Dictionary<Camera, int> cameraMasks = new System.Collections.Generic.Dictionary<Camera, int>();
    int dragControl;

    public void Draw(Rect rect, OriginalContentCatalog.Character character)
    {
        lastDrawFrame = Time.frameCount;
        if (studio == null) CreateStudio();
        if (characterId != character.id) Show(character);
        if (model == null) { GUI.Label(rect, "Modelo no disponible"); return; }
        previewCamera.aspect = rect.width / Mathf.Max(1, rect.height);
        float halfFov = previewCamera.fieldOfView * .5f * Mathf.Deg2Rad;
        float limitingFov = Mathf.Min(halfFov, Mathf.Atan(Mathf.Tan(halfFov) * previewCamera.aspect));
        float distance = radius / Mathf.Sin(limitingFov) * 1.15f;
        previewCamera.transform.position = center + new Vector3(0, radius * .12f, distance);
        previewCamera.transform.LookAt(center);
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
        var ev = Event.current;
        int control = GUIUtility.GetControlID(FocusType.Passive);
        if (ev.type == EventType.MouseDown && ev.button == 0 && rect.Contains(ev.mousePosition))
        { dragging = true; dragControl=control; GUIUtility.hotControl=control; ev.Use(); }
        if (ev.type == EventType.MouseDrag && dragging)
        { targetYaw -= ev.delta.x * .6f; ev.Use(); }
        if (ev.rawType == EventType.MouseUp && dragging)
        { dragging = false; if(GUIUtility.hotControl==dragControl) GUIUtility.hotControl=0; ev.Use(); }
    }
    void CreateStudio()
    {
        studio = new GameObject("CharacterPreviewStudio");
        studio.transform.position = new Vector3(10000, 10000, 10000);
        pivot = new GameObject("VerticalRotationPivot").transform;
        pivot.SetParent(studio.transform, false);
        previewCamera = new GameObject("CharacterPreviewCamera").AddComponent<Camera>();
        previewCamera.transform.SetParent(studio.transform, false);
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = Color.clear;
        previewCamera.cullingMask = 1 << 31;
        previewCamera.fieldOfView = 32;
        previewCamera.nearClipPlane = .01f; previewCamera.farClipPlane = 50;
        previewCamera.allowHDR = false;
        texture = new RenderTexture(768, 896, 24, RenderTextureFormat.ARGB32) { name = "CharacterPreview", antiAliasing = 2 };
        texture.Create(); previewCamera.targetTexture = texture;
        foreach (var camera in Camera.allCameras) if (camera != previewCamera)
        { cameraMasks[camera]=camera.cullingMask; camera.cullingMask &= ~(1 << 31); }
        AddLight(new Vector3(2, 3, 4), 2.8f, new Color(1f,.94f,.82f));
        AddLight(new Vector3(-3, 2, -2), 2f, new Color(.6f,.8f,1f));
    }
    void AddLight(Vector3 position, float intensity, Color color)
    {
        var light = new GameObject("PreviewLight").AddComponent<Light>();
        light.transform.SetParent(studio.transform, false); light.transform.localPosition = position;
        light.type = LightType.Point; light.range = 10; light.intensity = intensity; light.color = color;
        light.cullingMask = 1 << 31; light.shadows = LightShadows.None;
    }
    void Show(OriginalContentCatalog.Character character)
    {
        if (model != null) { model.SetActive(false); Destroy(model); }
        pivot.localRotation = Quaternion.identity;
        model = OriginalModel.Create(character.prefab, pivot, 2.4f, true);
        characterId = character.id;
        targetYaw = yaw = 180f; yawVelocity = 0; dragging = false;
        if (model == null || !OriginalModel.BoundsOf(model, out var bounds)) return;
        radius = Mathf.Max(.2f, bounds.extents.magnitude);
        center = bounds.center;
    }
    void LateUpdate()
    {
        if (studio == null) return;
        if (Time.frameCount - lastDrawFrame > 2 || GameManager.Instance == null || GameManager.Instance.gameState != GameManager.GameState.MainMenu)
        { Release(); return; }
        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, .065f, Mathf.Infinity, Time.unscaledDeltaTime);
        pivot.localRotation = Quaternion.Euler(0, yaw, 0);
    }
    void Release()
    {
        if(dragging && GUIUtility.hotControl==dragControl) GUIUtility.hotControl=0;
        foreach(var pair in cameraMasks) if(pair.Key!=null) pair.Key.cullingMask=pair.Value;
        cameraMasks.Clear();
        if (previewCamera != null) previewCamera.targetTexture = null;
        if (texture != null) { texture.Release(); Destroy(texture); }
        if (studio != null) { studio.SetActive(false); Destroy(studio); }
        studio = null; model = null; characterId = null; dragging = false;
    }
    void OnDisable() { Release(); }
    void OnDestroy() { Release(); }
}
