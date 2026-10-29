using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>Routes surviving original button events to the existing game; never runs decompiled controllers.</summary>
public class OriginalMenuBridge : MonoBehaviour
{
    public static bool EnableImportedMenu = true;
    Canvas canvas;
    GameHUD hud;
    public static bool Visible {get;private set;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        Visible=false;
        if(!EnableImportedMenu)return;
        var prefab=Resources.Load<GameObject>("OriginalUI/MainMenu_Menu");
        if(prefab==null || FindAnyObjectByType<OriginalMenuBridge>()!=null)return;
        var go=new GameObject("OriginalMenuCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var bridge=go.AddComponent<OriginalMenuBridge>();bridge.canvas=go.GetComponent<Canvas>();bridge.canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var panel=Instantiate(prefab,go.transform,false);
        foreach(var image in panel.GetComponentsInChildren<Image>(true))
            if(image.sprite==null && image.color==Color.white)image.color=Color.clear;
        foreach(var image in panel.GetComponentsInChildren<RawImage>(true))
            if(image.texture==null)image.color=Color.clear;
        int connected=0;
        OriginalMenuAction playAction=null;
        foreach(var action in panel.GetComponentsInChildren<OriginalMenuAction>(true))
        {
            var button=action.GetComponent<Button>();if(button==null)continue;
            var method=action.method;
            button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>bridge.hud?.NavigateOriginal(method));connected++;
            if(method=="GoToCharacterSelection")playAction=action;
        }
        if(playAction!=null)
        {
            var coop=Instantiate(playAction.gameObject,playAction.transform.parent);
            coop.name="Cooperativo LAN";
            coop.GetComponent<OriginalMenuAction>().method="GoToCoop";
            var label=coop.GetComponentInChildren<Text>(true);if(label!=null)label.text="COOPERATIVO LAN";
            var button=coop.GetComponent<Button>();
            button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>bridge.hud?.NavigateOriginal("GoToCoop"));
        }
        if(connected<5 || playAction==null){Debug.LogWarning("Menú original incompleto: se conserva el menú compatible.");Destroy(go);return;}
        if(EventSystem.current==null){var events=new GameObject("UIEventSystem",typeof(EventSystem),typeof(StandaloneInputModule));events.transform.SetParent(go.transform);}
        go.SetActive(true);bridge.canvas.enabled=false;
    }
    void Update(){hud=hud!=null?hud:FindAnyObjectByType<GameHUD>();Visible=hud!=null && hud.AtMainMenu;canvas.enabled=Visible;}
    void OnDestroy(){Visible=false;}
}
