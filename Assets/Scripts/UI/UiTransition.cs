using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Single loading gate. Existing runs generate in-place; real scene loads use LoadSceneAsync.</summary>
public class UiTransition : MonoBehaviour
{
    public static bool Busy {get;private set;}
    static UiTransition instance;
    string status;
    float progress=-1;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){Busy=false;instance=null;}
    static UiTransition Get(){if(instance==null){var go=new GameObject("UITransition");DontDestroyOnLoad(go);instance=go.AddComponent<UiTransition>();}return instance;}
    public static void Begin(GameManager gm,Action action)
    {if(Busy)return; Get().StartCoroutine(Get().Prepare(gm,action));}
    IEnumerator Prepare(GameManager gm,Action action)
    {
        Busy=true;progress=-1;status="Preparando expedición…";Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        yield return null; yield return new WaitForEndOfFrame();
        try {action();}
        catch(Exception error){Debug.LogException(error);if(gm!=null)gm.ReturnToMainMenu();}
        finally {Busy=false;}
    }
    public static void LoadScene(string scene)
    {
        if(Busy)return;
        if(!Application.CanStreamedLevelBeLoaded(scene)){Debug.LogError("Escena no incluida en Build Settings: "+scene);return;}
        Get().StartCoroutine(Get().Load(scene));
    }
    IEnumerator Load(string scene)
    {
        Busy=true;status="Cargando escena…";progress=0;Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        yield return null;
        AsyncOperation operation=null;
        try{operation=SceneManager.LoadSceneAsync(scene);}catch(Exception error){Debug.LogException(error);}
        if(operation!=null) while(!operation.isDone){progress=Mathf.Clamp01(operation.progress/.9f);yield return null;}
        Busy=false;
        Time.timeScale=GameManager.Instance!=null && GameManager.Instance.gameState==GameManager.GameState.Playing?1:0;
    }
    void OnGUI()
    {
        if(!Busy)return;
        int depth=GUI.depth;var matrix=GUI.matrix;GUI.depth=-1000;GUI.matrix=Matrix4x4.Scale(Vector3.one*MegabonkUI.Scale);
        try
        {
            MegabonkUI.Fill(new Rect(0,0,MegabonkUI.Width,MegabonkUI.Height),new Color(.05f,.08f,.07f));
            var rect=new Rect(MegabonkUI.Width/2-280,MegabonkUI.Height/2-70,560,140);
            MegabonkUI.MetalPanel(rect,"CARGANDO");
            MegabonkUI.Text(new Rect(rect.x+24,rect.y+65,512,40),status+(progress>=0?" "+Mathf.RoundToInt(progress*100)+"%":""),22,Color.white,true,TextAnchor.MiddleCenter);
        }
        finally{GUI.depth=depth;GUI.matrix=matrix;}
    }
}
