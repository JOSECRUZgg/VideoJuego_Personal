using UnityEngine;

public partial class GameHUD
{
    bool pauseSettings;
    bool settingFullscreen;
    int resolutionIndex;
    Resolution[] availableResolutions;
    Vector2 questScroll;
    GameManager.GameState previousUiState;
    void Update()
    {
        var gm=GameManager.Instance;if(gm==null)return;
        if(gm.gameState!=previousUiState)
        {
            if(gm.gameState!=GameManager.GameState.Paused)pauseSettings=false;
            if(gm.gameState==GameManager.GameState.MainMenu)currentMenuScreen=MenuSubScreen.Main;
            previousUiState=gm.gameState;
        }
    }
    public bool ClosePauseSettings()
    {if(!pauseSettings)return false;CloseSettings(false);return true;}
    public void NavigateOriginal(string method)
    {
        switch(method)
        {
            case "GoToCharacterSelection":currentMenuScreen=MenuSubScreen.StartRun;break;
            case "GoToCoop":currentMenuScreen=MenuSubScreen.LanLobby;break;
            case "GoToSettings":currentMenuScreen=MenuSubScreen.Settings;break;
            case "GoToShop":currentMenuScreen=MenuSubScreen.Shop;break;
            case "GoToQuests":currentMenuScreen=MenuSubScreen.Quests;break;
            case "GoToCredits":currentMenuScreen=MenuSubScreen.Credits;break;
            case "GoToLeaderboards":currentMenuScreen=MenuSubScreen.Leaderboards;break;
            case "GoToUnlocks":currentMenuScreen=MenuSubScreen.Unlocks;break;
            case "GoToMenu":currentMenuScreen=MenuSubScreen.Main;break;
            case "ExitGame":Application.Quit();break;
        }
    }
    public bool AtMainMenu => GameManager.Instance!=null && GameManager.Instance.gameState==GameManager.GameState.MainMenu && currentMenuScreen==MenuSubScreen.Main && !UiTransition.Busy;
    void RestorePreferences()
    {
        settingFOV=PlayerPrefs.GetFloat("UI.FOV",settingFOV);
        settingSensitivity=PlayerPrefs.GetFloat("UI.Sensitivity",settingSensitivity);
        settingInvertY=PlayerPrefs.GetInt("UI.InvertY",settingInvertY?1:0)==1;
        settingVolume=PlayerPrefs.GetFloat("UI.Volume",settingVolume);
        settingQualityLevel=Mathf.Clamp(PlayerPrefs.GetInt("UI.Quality",settingQualityLevel),0,Mathf.Max(0,QualitySettings.names.Length-1));
        settingFullscreen=PlayerPrefs.GetInt("UI.Fullscreen",Screen.fullScreen?1:0)==1;
        availableResolutions=Screen.resolutions; resolutionIndex=-1;
        int width=PlayerPrefs.GetInt("UI.Width",Screen.width),height=PlayerPrefs.GetInt("UI.Height",Screen.height);
        for(int i=0;i<availableResolutions.Length;i++) if(availableResolutions[i].width==width && availableResolutions[i].height==height)resolutionIndex=i;
        if(resolutionIndex<0)
        {
            // Preserve the current window size when a saved display mode is unavailable.
            resolutionIndex=availableResolutions.Length;
            System.Array.Resize(ref availableResolutions,resolutionIndex+1);
            availableResolutions[resolutionIndex]=new Resolution {width=Screen.width,height=Screen.height};
        }
        ApplySettings();
    }
    void SavePreferences()
    {
        PlayerPrefs.SetFloat("UI.FOV",settingFOV);PlayerPrefs.SetFloat("UI.Sensitivity",settingSensitivity);
        PlayerPrefs.SetInt("UI.InvertY",settingInvertY?1:0);PlayerPrefs.SetFloat("UI.Volume",settingVolume);
        PlayerPrefs.SetInt("UI.Quality",settingQualityLevel);PlayerPrefs.SetInt("UI.Fullscreen",settingFullscreen?1:0);
        // Do not change the editor Game view or silently select an unsupported resolution.
        if(!Application.isEditor && availableResolutions!=null && availableResolutions.Length>0)
        {
            var r=availableResolutions[Mathf.Clamp(resolutionIndex,0,availableResolutions.Length-1)];
            Screen.SetResolution(r.width,r.height,settingFullscreen);
            PlayerPrefs.SetInt("UI.Width",r.width);PlayerPrefs.SetInt("UI.Height",r.height);
        }
        PlayerPrefs.Save();
    }
    void CloseSettings(bool apply)
    {
        if(apply)ApplySettings();else RestorePreferences();
        pauseSettings=false;currentMenuScreen=MenuSubScreen.Main;
    }
    void DrawQuestPanel()
    {
        MenuHeading("MISIONES","Progreso de desbloqueos");
        var view=new Rect(64,205,MegabonkUI.Width-128,MegabonkUI.Height-340);
        var items=MegabonkRelicManager.Catalog;
        questScroll=GUI.BeginScrollView(view,questScroll,new Rect(0,0,view.width-24,items.Count*82));
        for(int i=0;i<items.Count;i++)
        {
            var item=items[i];float y=i*82;bool unlocked=ExpeditionProgress.IsUnlocked(item);
            MegabonkUI.MetalPanel(new Rect(0,y,view.width-32,74));
            MegabonkIconProvider.DrawItemIcon(new Rect(12,y+10,48,48),item.id);
            MegabonkUI.Text(new Rect(80,y+8,420,25),item.name,19,item.bannerColor,true);
            MegabonkUI.Text(new Rect(80,y+37,view.width-360,27),ExpeditionProgress.Requirement(item),15,MegabonkUI.White);
            MegabonkUI.Text(new Rect(view.width-250,y+24,200,28),unlocked?"DESBLOQUEADO":"EN PROGRESO",15,unlocked?MegabonkUI.Cyan:MegabonkUI.Muted,true);
        }
        GUI.EndScrollView();MenuBack();
    }
    void DrawCreditsPanel()
    {
        MenuHeading("CRÉDITOS","Recursos y reconstrucción");
        MegabonkUI.MetalPanel(new Rect(64,210,MegabonkUI.Width-128,390),"CRÉDITOS");
        MegabonkUI.Text(new Rect(100,290,MegabonkUI.Width-200,270),
            "MEGABONK: recursos originales reutilizados desde la exportación facilitada por el usuario. Los derechos pertenecen a sus autores.\n\nSnowy Mountain - Terrain: artfromheath · CC BY 4.0.\nEfectos SpellVFX: recursos Kenney CC0.\n\nUnityShooter: adaptación funcional de interfaces y sistemas. Esta reconstrucción no es una versión oficial de MEGABONK.",22,MegabonkUI.White);
        MenuBack();
    }
}
