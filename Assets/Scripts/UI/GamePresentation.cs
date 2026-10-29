using System;
using System.Collections.Generic;
using UnityEngine;

// Shared presentation primitives. No textures or fonts are allocated per repaint.
public static class MegabonkUI
{
    public static readonly Color Ink = new Color(.025f, .038f, .065f);
    public static readonly Color PanelColor = new Color(.055f, .078f, .115f, .97f);
    public static readonly Color Cyan = new Color(.33f, .91f, .87f);
    public static readonly Color Gold = new Color(1f, .73f, .37f);
    public static readonly Color Muted = new Color(.57f, .66f, .75f);
    public static readonly Color White = new Color(.92f, .95f, .97f);
    static readonly Dictionary<int, GUIStyle> styles = new Dictionary<int, GUIStyle>();
    static Font extractedFont;
    static Texture2D extractedButton;
    static Texture2D extractedWindow;
    static Texture2D extractedSlot;
    static Texture2D extractedBackground;
    static Texture2D extractedLogo;
    static Texture2D forestMapIcon;
    static Texture2D desertMapIcon;
    static GUIStyle windowFrameStyle;
    static GUIStyle buttonFrameStyle;
    static GUIStyle slotFrameStyle;
    static readonly Dictionary<string, string> cleanLabels = new Dictionary<string, string>();
    public static float Scale => Mathf.Max(.1f, Mathf.Min(Screen.width / 1600f, Screen.height / 900f));
    public static float Width => Screen.width / Scale;
    public static float Height => Screen.height / Scale;

    public static GUIStyle Style(int size, bool bold = false, TextAnchor alignment = TextAnchor.UpperLeft)
    {
        int key = size * 100 + (bold ? 10 : 0) + (int)alignment;
        if (!styles.TryGetValue(key, out var style))
        {
            if (extractedFont == null) extractedFont = Resources.Load<Font>("ExtractedVisuals/Font/alagard");
            style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                alignment = alignment, wordWrap = true, richText = false, padding = new RectOffset(0, 0, 0, 0) };
            style.normal.textColor = Color.white;
            if (extractedFont != null) style.font = extractedFont;
            styles[key] = style;
        }
        return style;
    }

    public static string Clean(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        // Text labels remain readable with Unity's built-in font on every OS.
        if (!cleanLabels.TryGetValue(text, out string clean))
        {
            clean = System.Text.RegularExpressions.Regex.Replace(text, @"[^\u0020-\u024F\n%+×−]", "").Trim();
            cleanLabels[text] = clean;
        }
        return clean;
    }

    public static void Text(Rect rect, string text, int size, Color color, bool bold = false, TextAnchor alignment = TextAnchor.UpperLeft)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.Label(rect, text, Style(size, bold, alignment));
        GUI.color = previous;
    }

    public static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    static Texture2D Art(ref Texture2D cache, string path)
    {
        if (cache == null) cache = Resources.Load<Texture2D>(path);
        return cache;
    }

    static void DrawArt(Rect rect, Texture2D texture, Color tint = default(Color), ScaleMode mode = ScaleMode.StretchToFill)
    {
        if (texture == null) return;
        Color old = GUI.color;
        GUI.color = tint == default(Color) ? Color.white : tint;
        GUI.DrawTexture(rect, texture, mode, true);
        GUI.color = old;
    }

    static void DrawSlicedFrame(Rect rect, Texture2D texture, ref GUIStyle style, Color tint)
    {
        if (texture == null) return;
        if (style == null)
        {
            // Both original sprites declare a 20 px nine-slice border in their .meta files.
            style = new GUIStyle(GUIStyle.none) { border = new RectOffset(20, 20, 20, 20) };
            style.normal.background = texture;
        }
        Color previous = GUI.color;
        GUI.color = tint;
        GUI.Box(rect, GUIContent.none, style);
        GUI.color = previous;
    }

    public static void Panel(Rect r, Color accent)
    {
        Fill(new Rect(r.x + 4, r.y + 7, r.width, r.height), new Color(0, 0, 0, .18f));
        Fill(r, PanelColor);
        Fill(new Rect(r.x, r.y, r.width, 1), new Color(.24f, .32f, .4f, .7f));
        Fill(new Rect(r.x, r.y, 3, r.height), accent);
    }

    // Marco inspirado en interfaces RPG pixeladas: doble borde metálico y remaches.
    public static void MetalPanel(Rect r, string title = null)
    {
        var frame = Art(ref extractedWindow, "ExtractedVisuals/Texture2D/BorderWindow");
        Fill(new Rect(r.x+5,r.y+6,r.width,r.height),new Color(0,0,0,.38f));
        Fill(r,new Color(.14f,.17f,.155f,.98f));
        if (frame != null) DrawSlicedFrame(r, frame, ref windowFrameStyle, Color.white);
        else
        {
            Fill(new Rect(r.x,r.y,r.width,3),new Color(.70f,.72f,.67f));
            Fill(new Rect(r.x,r.yMax-3,r.width,3),new Color(.37f,.39f,.36f));
            Fill(new Rect(r.x,r.y,3,r.height),new Color(.70f,.72f,.67f));
            Fill(new Rect(r.xMax-3,r.y,3,r.height),new Color(.37f,.39f,.36f));
        }
        if(!string.IsNullOrEmpty(title))
        {
            Fill(new Rect(r.x+7,r.y+7,r.width-14,38),new Color(.12f,.29f,.24f));
            Fill(new Rect(r.x+7,r.y+44,r.width-14,2),new Color(.62f,.65f,.59f));
            Text(new Rect(r.x+22,r.y+12,r.width-70,28),title,19,White,true,TextAnchor.MiddleLeft);
            Text(new Rect(r.xMax-48,r.y+9,30,30),"×",28,new Color(.95f,.12f,.10f),true,TextAnchor.MiddleCenter);
        }
    }

    public static void SlotFrame(Rect r, Color fill)
    {
        Fill(r, fill);
        var slot = Art(ref extractedSlot, "ExtractedVisuals/Texture2D/Border2");
        if (slot != null) DrawSlicedFrame(r, slot, ref slotFrameStyle, Color.white);
        else
        {
            Fill(new Rect(r.x,r.y,r.width,2),new Color(.7f,.73f,.68f));
            Fill(new Rect(r.x,r.y,2,r.height),new Color(.7f,.73f,.68f));
        }
    }

    public static bool PixelButton(Rect r, string text, bool blue = true)
    {
        bool hover=r.Contains(Event.current.mousePosition);
        Color baseColor=blue?new Color(.05f,.29f,.49f):new Color(.58f,.29f,.025f);
        Fill(r,hover?Color.Lerp(baseColor,Color.white,.18f):baseColor);
        Fill(new Rect(r.x,r.y,r.width,3),new Color(.75f,.82f,.82f));
        Fill(new Rect(r.x,r.y,3,r.height),new Color(.65f,.72f,.72f));
        Fill(new Rect(r.x,r.yMax-3,r.width,3),new Color(.015f,.05f,.08f));
        Fill(new Rect(r.xMax-3,r.y,3,r.height),new Color(.015f,.05f,.08f));
        Text(r,text,17,Color.white,true,TextAnchor.MiddleCenter);
        return GUI.Button(r,GUIContent.none,GUIStyle.none);
    }

    public static bool StoneButton(Rect r, string text)
    {
        if (extractedButton == null) extractedButton = Resources.Load<Texture2D>("ExtractedVisuals/Texture2D/MyButtonNew");
        if (extractedButton != null)
        {
            if (buttonFrameStyle == null)
            {
                buttonFrameStyle = new GUIStyle(GUIStyle.none) { border = new RectOffset(7, 7, 7, 7) };
                buttonFrameStyle.normal.background = extractedButton;
            }
            DrawSlicedFrame(r, extractedButton, ref buttonFrameStyle,
                r.Contains(Event.current.mousePosition) ? new Color(.55f,.59f,.58f) : new Color(.39f,.43f,.42f));
            Text(r, text, 22, Color.white, true, TextAnchor.MiddleCenter);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }
        bool hover=r.Contains(Event.current.mousePosition);
        Fill(new Rect(r.x-2,r.y-2,r.width+4,r.height+6),Color.black);
        Fill(r,hover?new Color(.36f,.38f,.37f):new Color(.23f,.25f,.24f));
        Fill(new Rect(r.x,r.y,r.width,3),new Color(.72f,.74f,.69f));
        Fill(new Rect(r.x,r.y,3,r.height),new Color(.64f,.66f,.62f));
        Fill(new Rect(r.x,r.yMax-3,r.width,3),new Color(.055f,.06f,.055f));
        Fill(new Rect(r.xMax-3,r.y,3,r.height),new Color(.055f,.06f,.055f));
        Text(new Rect(r.x+2,r.y+3,r.width,r.height),text,23,Color.black,true,TextAnchor.MiddleCenter);
        Text(r,text,23,Color.white,true,TextAnchor.MiddleCenter);
        return GUI.Button(r,GUIContent.none,GUIStyle.none);
    }

    public static bool Button(Rect r, string text, Color accent, bool primary = false)
    {
        bool hover = r.Contains(Event.current.mousePosition);
        Fill(r, primary ? Color.Lerp(accent, Color.white, hover ? .18f : 0f) : (hover ? new Color(.12f, .18f, .23f) : PanelColor));
        Fill(new Rect(r.x, r.yMax - 1, r.width, 1), accent * .7f);
        Text(r, text, 16, primary ? Ink : White, true, TextAnchor.MiddleCenter);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    public static void Line(Vector2 a, Vector2 b, Color color, float thickness = 1)
    {
        Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y, b.x-a.x) * Mathf.Rad2Deg, a);
        Fill(new Rect(a.x, a.y, Vector2.Distance(a,b), thickness), color);
        GUI.matrix = old;
    }

    public static void Orbit(Vector2 center, float radius, Color color)
    {
        if (Event.current.type != EventType.Repaint) return;
        for (int i=0; i<64; i++)
        {
            float a=i*Mathf.PI/32, b=(i+1)*Mathf.PI/32;
            Line(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,
                center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,color);
        }
    }

    public static void Background(bool transparent = false)
    {
        var art = Art(ref extractedBackground, "ExtractedVisuals/Texture2D/background_Gray");
        Fill(new Rect(0,0,Width,Height), new Color(Ink.r,Ink.g,Ink.b,transparent ? .94f : 1));
        if (art != null) DrawArt(new Rect(0,0,Width,Height), art, new Color(1f,1f,1f,transparent ? .16f : .27f));
        for (int i=0; i<18; i++)
            Fill(new Rect(Width*.52f+i*24,0,24,Height),new Color(.08f,.3f,.35f,.015f+i*.003f));
        var center = new Vector2(Width*.78f, Height*.43f);
        Orbit(center,250,new Color(.3f,.8f,.8f,.12f));
        Orbit(center,270,new Color(.3f,.8f,.8f,.06f));
        for (int i=0;i<18;i++)
        {
            float x=(i*173f+67)%Width;
            float y=(i*109f+Mathf.Sin(Time.unscaledTime*.18f+i)*12)%Height;
            Fill(new Rect(x,y,2,2),new Color(.65f,.85f,.9f,.18f));
        }
        Fill(new Rect(48,Height-50,Width-96,1),new Color(.25f,.35f,.44f,.5f));
    }

    // Deja visible el bosque 3D real que ya está detrás de la interfaz.
    public static void MenuWorld()
    {
        Fill(new Rect(0,0,Width,Height),new Color(.015f,.11f,.15f,.24f));
    }

    public static void Logo(Rect rect)
    {
        var logo = Art(ref extractedLogo, "ExtractedVisuals/Texture2D/MegabonkLogo");
        if (logo != null) DrawArt(rect, logo, Color.white, ScaleMode.ScaleToFit);
        else Text(rect, "MEGABONK", 80, Color.white, true, TextAnchor.MiddleCenter);
    }

    public static bool MapIcon(Rect rect, int mapIndex)
    {
        Texture2D art = mapIndex == 0
            ? Art(ref forestMapIcon, "ExtractedVisuals/Texture2D/ForestIconBig")
            : mapIndex == 1 ? Art(ref desertMapIcon, "ExtractedVisuals/Texture2D/DesertIconBig") : null;
        if (art == null) return false;
        DrawArt(rect, art, Color.white, ScaleMode.ScaleToFit);
        return true;
    }

    public static void Meter(Rect r, float value, Color color)
    {
        Fill(r,new Color(.12f,.16f,.21f));
        Fill(new Rect(r.x,r.y,r.width*Mathf.Clamp01(value),r.height),color);
    }

    public static void Glyph(Rect r, string text, Color color)
    {
        Fill(r,new Color(color.r*.18f,color.g*.18f,color.b*.18f));
        Text(r,text,22,color,true,TextAnchor.MiddleCenter);
    }
}

public partial class GameHUD
{
    void MenuHeading(string eyebrow, string title)
    {
        MegabonkUI.MenuWorld();
        var header=new Rect(48,34,Mathf.Min(880,MegabonkUI.Width-96),126);
        MegabonkUI.MetalPanel(header,eyebrow.ToUpper());
        MegabonkUI.Text(new Rect(72,88,header.width-48,45),title,29,MegabonkUI.White,true);
    }

    void MenuBack()
    {
        if(MegabonkUI.Button(new Rect(64,MegabonkUI.Height-106,260,52),"VOLVER  /  ESC",MegabonkUI.Muted)) currentMenuScreen=MenuSubScreen.Main;
    }

    void DrawModernLoadout(GameManager gm)
    {
        if (OriginalContentCatalog.Instance != null && OriginalContentCatalog.Instance.characters.Length > 0)
        { DrawOriginalCharacterSelector(gm); return; }
        MegabonkUI.MenuWorld();
        float w=MegabonkUI.Width,h=MegabonkUI.Height;
        var left=new Rect(32,58,500,h-112); var right=new Rect(w-488,98,424,h-210);
        MegabonkUI.MetalPanel(left,"SELECCIÓN DE PERSONAJE");
        MegabonkUI.MetalPanel(right);
        float cell=92, gap=12, startX=left.x+25,startY=left.y+66;
        for(int i=0;i<20;i++)
        {
            int col=i%4,row=i/4; var r=new Rect(startX+col*(cell+gap),startY+row*(cell+gap),cell,cell);
            bool available=i<Characters.Length;
            bool selected=available&&gm.selectedCharacter==Characters[i].type;
            MegabonkUI.SlotFrame(r,available?new Color(.32f,.37f,.37f):new Color(.025f,.028f,.027f));
            if (selected) MegabonkUI.Fill(new Rect(r.x+4,r.yMax-6,r.width-8,2),MegabonkUI.Cyan);
            if(available)
            {
                var hero=Characters[i];
                if(!MegabonkIconProvider.DrawCharacterIcon(new Rect(r.x+8,r.y+5,cell-16,cell-27),hero.name)) MegabonkUI.Glyph(new Rect(r.x+14,r.y+10,cell-28,cell-35),hero.name.Substring(0,2),hero.color);
                MegabonkUI.Text(new Rect(r.x+3,r.y+69,cell-6,18),hero.name,11,MegabonkUI.White,true,TextAnchor.MiddleCenter);
                if(GUI.Button(r,GUIContent.none,GUIStyle.none))
                {
                    gm.selectedOriginalCharacter = "";
                    gm.selectedCharacter = hero.type;
                }
            }
            else MegabonkUI.Text(r,"?",30,new Color(.04f,.05f,.045f),true,TextAnchor.MiddleCenter);
        }
        var heroSelected=Characters[(int)gm.selectedCharacter];
        float previewX=w*.52f;
        MegabonkUI.Text(new Rect(previewX-160,160,320,25),"VISTA PREVIA",14,MegabonkUI.Gold,true,TextAnchor.MiddleCenter);
        if(!MegabonkIconProvider.DrawCharacterIcon(new Rect(previewX-115,205,230,270),heroSelected.name)) MegabonkUI.Glyph(new Rect(previewX-100,220,200,230),heroSelected.name.Substring(0,2),heroSelected.color);
        MegabonkUI.Text(new Rect(previewX-220,505,440,32),heroSelected.name,27,MegabonkUI.White,true,TextAnchor.MiddleCenter);
        MegabonkUI.Text(new Rect(previewX-220,540,440,24),heroSelected.title,14,heroSelected.color,true,TextAnchor.MiddleCenter);
        MegabonkUI.Text(new Rect(right.x+22,right.y+24,right.width-44,34),heroSelected.name,25,MegabonkUI.White,true);
        MegabonkUI.Text(new Rect(right.x+22,right.y+67,right.width-44,42),"Rango 1  •  0 carreras",14,MegabonkUI.Muted,true);
        MegabonkUI.Text(new Rect(right.x+22,right.y+124,right.width-44,75),heroSelected.passive,16,MegabonkUI.White);
        var weapon=UpgradeCatalog.Get(MegabonkBuild.StartingWeapon(gm.selectedCharacter));
        MegabonkUI.Fill(new Rect(right.x+20,right.y+222,right.width-40,1),new Color(.55f,.58f,.53f));
        MegabonkUI.Text(new Rect(right.x+22,right.y+240,270,25),"ARMA INICIAL",13,MegabonkUI.Gold,true);
        MegabonkIconProvider.DrawUpgradeIcon(new Rect(right.x+24,right.y+274,58,58),weapon);
        MegabonkUI.Text(new Rect(right.x+96,right.y+274,right.width-118,25),weapon.name,18,MegabonkUI.Gold,true);
        MegabonkUI.Text(new Rect(right.x+96,right.y+302,right.width-118,42),weapon.description,13,MegabonkUI.White);
        MegabonkUI.Text(new Rect(right.x+22,right.y+370,right.width-44,80),"Combate automático en todas las direcciones.\nConsigue más armas y tomos al subir de nivel.",15,MegabonkUI.Muted);
        if(MegabonkUI.PixelButton(new Rect(right.x+126,right.yMax-74,190,52),"CONFIRMAR",true)) currentMenuScreen=MenuSubScreen.MapSelect;
        if(MegabonkUI.PixelButton(new Rect(32,h-46,150,34),"VOLVER",false)) currentMenuScreen=MenuSubScreen.Main;
    }

    void DrawMapSelection(GameManager gm)
    {
        MegabonkUI.MenuWorld(); float w=MegabonkUI.Width,h=MegabonkUI.Height;
        var left=new Rect(32,58,500,h-112); var right=new Rect(w-488,100,424,h-212);
        MegabonkUI.MetalPanel(left,"SELECCIÓN DE MAPA"); MegabonkUI.MetalPanel(right);
        string[] mapTitles={"BOSQUE","DESIERTO","MONTAÑA NEVADA"};
        string[] descriptions={"Un lugar verde lleno de árboles, pájaros y buena onda. También todo intentará matarte.","Un lugar caliente, seco y polvoriento. Los élites no perdonan.","Explora la montaña de nieve. Los enemigos llegan por etapas."};
        for(int i=0;i<descriptions.Length;i++)
        {
            var r=new Rect(left.x+24,left.y+70+i*112,left.width-48,96); bool selected=(int)gm.selectedMap==i;
            MegabonkUI.Fill(r,selected?new Color(.05f,.28f,.50f):new Color(.07f,.09f,.085f));
            MegabonkUI.Fill(new Rect(r.x,r.y,r.width,2),selected?MegabonkUI.Cyan:new Color(.42f,.45f,.41f));
            var iconRect = new Rect(r.x+12,r.y+7,82,82);
            if (!MegabonkUI.MapIcon(iconRect, i)) MegabonkUI.Glyph(iconRect,"NIEVE",new Color(.65f,.85f,1f));
            MegabonkUI.Text(new Rect(r.x+108,r.y+10,r.width-120,24),mapTitles[i],19,selected?MegabonkUI.Cyan:MegabonkUI.Gold,true);
            MegabonkUI.Text(new Rect(r.x+108,r.y+37,r.width-120,53),descriptions[i],14,MegabonkUI.White);
            if(GUI.Button(r,GUIContent.none,GUIStyle.none)) gm.selectedMap=(GameManager.MapType)i;
        }
        string mapName=gm.selectedMap==GameManager.MapType.Forest?"Bosque":gm.selectedMap==GameManager.MapType.Desert?"Desierto":"Montaña Nevada";
        Color mapColor=gm.selectedMap==GameManager.MapType.Forest?new Color(.25f,.75f,.38f):gm.selectedMap==GameManager.MapType.Desert?new Color(.95f,.55f,.20f):new Color(.68f,.86f,1f);
        var selectedHero=Characters[(int)gm.selectedCharacter];
        if (OriginalContentCatalog.Selected != null)
            DrawOriginalPreview(new Rect(w*.5f-170,160,340,390), OriginalContentCatalog.Selected);
        else if (!MegabonkIconProvider.DrawCharacterIcon(new Rect(w*.5f-115,205,230,270),selectedHero.name))
            MegabonkUI.Glyph(new Rect(w*.5f-115,205,230,270),selectedHero.name,mapColor);
        MegabonkUI.Text(new Rect(w*.5f-180,555,360,30),OriginalContentCatalog.Selected?.displayName ?? selectedHero.name,24,MegabonkUI.White,true,TextAnchor.MiddleCenter);
        MegabonkUI.Text(new Rect(right.x+22,right.y+22,right.width-44,28),mapName,25,MegabonkUI.Gold,true);
        MegabonkUI.Text(new Rect(right.x+22,right.y+57,right.width-44,24),"Nivel 1  •  0 carreras",14,MegabonkUI.Muted,true);
        MegabonkUI.Fill(new Rect(right.x+20,right.y+94,right.width-40,1),new Color(.6f,.62f,.56f));
        MegabonkUI.Text(new Rect(right.x+22,right.y+112,180,24),"RÉCORD     --",14,MegabonkUI.White,true);
        MegabonkUI.Text(new Rect(right.x+218,right.y+112,180,24),"SPEEDRUN     --",14,MegabonkUI.White,true);
        for(int i=0;i<3;i++)
        {
            var r=new Rect(right.x+22,right.y+158+i*48,right.width-44,40); bool selected=(int)gm.selectedTier==i+1;
            MegabonkUI.Fill(r,selected?new Color(.04f,.25f,.42f):new Color(.035f,.045f,.04f));
            MegabonkUI.Text(new Rect(r.x+14,r.y+8,180,24),$"□  NIVEL {i+1}",16,selected?MegabonkUI.Cyan:MegabonkUI.White,true);
            MegabonkUI.Text(new Rect(r.x+265,r.y+8,110,24),$"{1f+i*.1f:0.0}x",15,MegabonkUI.Gold,true,TextAnchor.MiddleRight);
            if(GUI.Button(r,GUIContent.none,GUIStyle.none)) gm.selectedTier=(GameManager.DifficultyTier)(i+1);
        }
        MegabonkUI.Text(new Rect(right.x+22,right.y+320,right.width-44,55),$"Etapas: 3\n{gm.SilverRewardMultiplier:0.0}x incremento de plata",16,MegabonkUI.White,true,TextAnchor.MiddleCenter);
        if (gm.selectedMap == GameManager.MapType.SnowyMountain)
            MegabonkUI.Text(new Rect(left.x+24,left.yMax-63,left.width-48,38),"Snowy Mountain - Terrain por artfromheath · CC BY 4.0",13,MegabonkUI.White);
        if(MegabonkUI.PixelButton(new Rect(right.x+112,right.yMax-138,200,48),gm.challengeModifier?"DESAFÍO: ACTIVO":"DESAFÍOS",false)) gm.challengeModifier=!gm.challengeModifier;
        if(MegabonkUI.PixelButton(new Rect(right.x+112,right.yMax-78,200,52),"CONFIRMAR",true)) gm.StartGame();
        if(MegabonkUI.PixelButton(new Rect(32,h-46,170,34),"VOLVER",false)) currentMenuScreen=MenuSubScreen.StartRun;
    }

    int unlockTab;
    Vector2 unlockScroll;
    string unlockDescription = "Pasa el cursor por un elemento para consultar su función.";
    void DrawUnlocks(GameManager gm)
    {
        MegabonkUI.MenuWorld(); float w=MegabonkUI.Width,h=MegabonkUI.Height;
        float panelWidth=Mathf.Min(w-96,1240);
        var panel=new Rect((w-panelWidth)*.5f,42,panelWidth,h-94); MegabonkUI.MetalPanel(panel,"DESBLOQUEOS");
        string[] tabs={"PERSONAJES","ARMAS","TOMOS","OBJETOS"};
        for(int i=0;i<tabs.Length;i++)
        {
            var tab=new Rect(panel.x+24+i*142,panel.y+55,130,39); bool active=unlockTab==i;
            if(active) MegabonkUI.Fill(new Rect(tab.x,tab.yMax-3,tab.width,3),MegabonkUI.Cyan);
            MegabonkUI.Text(tab,tabs[i],16,active?MegabonkUI.White:MegabonkUI.Muted,true,TextAnchor.MiddleCenter);
            if(GUI.Button(tab,GUIContent.none,GUIStyle.none)) { unlockTab=i; unlockScroll=Vector2.zero; }
        }
        MegabonkUI.Fill(new Rect(panel.x+14,panel.y+102,panel.width-28,1),new Color(.68f,.70f,.65f));
        const int columns=9;
        float gap=10;
        float cell=Mathf.Floor((panel.width-72-(columns-1)*gap)/columns);
        var definitions = UpgradeCatalog.AllUpgrades.FindAll(d => d.category == (unlockTab == 1 ? "ARMA" : "TOMO"));
        var items = MegabonkRelicManager.Catalog;
        int total=unlockTab==0?20:unlockTab==3?items.Count:definitions.Count;
        unlockScroll = GUI.BeginScrollView(new Rect(panel.x+25,panel.y+116,panel.width-50,panel.height-246),unlockScroll,
            new Rect(0,0,panel.width-75,Mathf.CeilToInt((float)total/columns)*(cell+gap)));
        for(int i=0;i<total;i++)
        {
            var r=new Rect((i%columns)*(cell+gap),(i/columns)*(cell+gap),cell,cell);
            bool unlocked=unlockTab==0 ? i<Characters.Length : unlockTab!=3 || ExpeditionProgress.IsUnlocked(items[i]);
            MegabonkUI.SlotFrame(r,unlocked?new Color(.34f,.40f,.40f):new Color(.045f,.05f,.048f));
            string description;
            if(unlockTab==0)
            {
                if (i < Characters.Length)
                {
                    var hero=Characters[i];
                    MegabonkIconProvider.DrawCharacterIcon(new Rect(r.x+8,r.y+6,cell-16,cell-28),hero.name);
                    MegabonkUI.Text(new Rect(r.x+3,r.y+cell-22,cell-6,18),hero.name,11,MegabonkUI.White,true,TextAnchor.MiddleCenter);
                    description=hero.name+" · "+hero.passive;
                }
                else
                {
                    MegabonkUI.Fill(new Rect(r.x+9,r.y+8,cell-18,cell-16),new Color(.025f,.029f,.027f));
                    MegabonkUI.Text(new Rect(r.x+3,r.y+cell-22,cell-6,18),"BLOQUEADO",10,MegabonkUI.Muted,true,TextAnchor.MiddleCenter);
                    description="Personaje aún no disponible en esta versión.";
                }
            }
            else if(unlockTab==3)
            {
                Color previous=GUI.color;
                if(!unlocked) GUI.color=new Color(.15f,.15f,.15f);
                MegabonkIconProvider.DrawItemIcon(new Rect(r.x+8,r.y+8,cell-16,cell-16),items[i].id);
                GUI.color=previous;
                description=items[i].name+" · "+items[i].description+"\n"+ExpeditionProgress.Requirement(items[i]);
            }
            else
            {
                MegabonkIconProvider.DrawUpgradeIcon(new Rect(r.x+8,r.y+8,cell-16,cell-16),definitions[i]);
                description=definitions[i].name+" · "+definitions[i].description;
            }
            if(r.Contains(Event.current.mousePosition)) unlockDescription=description;
        }
        GUI.EndScrollView();
        MegabonkUI.Fill(new Rect(panel.x+12,panel.yMax-120,panel.width-24,108),new Color(.065f,.075f,.068f));
        MegabonkUI.Fill(new Rect(panel.x+14,panel.yMax-120,panel.width-28,2),new Color(.68f,.70f,.65f));
        MegabonkUI.Fill(new Rect(panel.x+28,panel.yMax-96,78,78),new Color(.30f,.35f,.35f));
        MegabonkUI.Text(new Rect(panel.x+126,panel.yMax-92,panel.width-160,64),unlockDescription,16,MegabonkUI.White);
        if(GUI.Button(new Rect(panel.xMax-52,panel.y+8,38,37),GUIContent.none,GUIStyle.none)) currentMenuScreen=MenuSubScreen.Main;
    }

    void DrawModernSettings(GameManager gm)
    {
        MenuHeading("PREFERENCIAS","Ajusta tu experiencia");
        float cw=(MegabonkUI.Width-176)/3;
        for(int i=0;i<3;i++) MegabonkUI.MetalPanel(new Rect(64+i*(cw+24),220,cw,440));
        float x=90;
        MegabonkUI.Text(new Rect(x,250,cw-52,35),"IMAGEN",22,MegabonkUI.White,true);
        MegabonkUI.Text(new Rect(x,320,cw-52,28),$"Campo de visión   {settingFOV:0}°",18,MegabonkUI.Muted);
        settingFOV=GUI.HorizontalSlider(new Rect(x,370,cw-52,24),settingFOV,60,110);
        MegabonkUI.Text(new Rect(x,430,cw-52,30),"Calidad gráfica",18,MegabonkUI.Muted);
        string[] qualities=QualitySettings.names;
        settingQualityLevel=Mathf.Clamp(settingQualityLevel,0,qualities.Length-1);
        if(MegabonkUI.Button(new Rect(x,480,cw-52,54),qualities[settingQualityLevel]+"   >",MegabonkUI.Cyan)) settingQualityLevel=(settingQualityLevel+1)%qualities.Length;
        x=90+cw+24;
        MegabonkUI.Text(new Rect(x,250,cw-52,35),"CONTROLES",22,MegabonkUI.White,true);
        MegabonkUI.Text(new Rect(x,320,cw-52,28),$"Sensibilidad   {settingSensitivity:0.0}",18,MegabonkUI.Muted);
        settingSensitivity=GUI.HorizontalSlider(new Rect(x,370,cw-52,24),settingSensitivity,.3f,6f);
        if(MegabonkUI.Button(new Rect(x,430,cw-52,52),"INVERTIR Y   /   "+(settingInvertY?"SÍ":"NO"),MegabonkUI.Cyan,settingInvertY)) settingInvertY=!settingInvertY;
        MegabonkUI.Text(new Rect(x,504,cw-52,70),"Las armas de la build atacan automáticamente. Este ajuste no cambia sus objetivos.",16,MegabonkUI.Muted);
        x=90+2*(cw+24);
        MegabonkUI.Text(new Rect(x,250,cw-52,35),"AUDIO",22,MegabonkUI.White,true);
        MegabonkUI.Text(new Rect(x,320,cw-52,28),$"Volumen general   {settingVolume*100:0}%",18,MegabonkUI.Muted);
        settingVolume=GUI.HorizontalSlider(new Rect(x,370,cw-52,24),settingVolume,0,1);
        MegabonkUI.Text(new Rect(x,430,cw-52,120),"Aplica los ajustes antes de volver a la arena.\n\nTAB o ESC pausa la partida.",18,MegabonkUI.Muted);
        if(MegabonkUI.Button(new Rect(90,560,cw-52,42),"PANTALLA COMPLETA: "+(settingFullscreen?"SÍ":"NO"),MegabonkUI.Cyan))settingFullscreen=!settingFullscreen;
        if(availableResolutions!=null && availableResolutions.Length>0)
        {var r=availableResolutions[resolutionIndex];if(MegabonkUI.Button(new Rect(90,610,cw-52,36),r.width+" × "+r.height,MegabonkUI.Cyan))resolutionIndex=(resolutionIndex+1)%availableResolutions.Length;}
        if(MegabonkUI.Button(new Rect(64,MegabonkUI.Height-106,260,52),"VOLVER",MegabonkUI.Muted))CloseSettings(false);
        if(MegabonkUI.Button(new Rect(MegabonkUI.Width-474,MegabonkUI.Height-106,410,52),"APLICAR Y VOLVER",MegabonkUI.Cyan,true)) CloseSettings(true);
    }

    void DrawModernStatistics(GameManager gm)
    {
        MenuHeading("REGISTRO DEL SUPERVIVIENTE","Cada expedición cuenta");
        string[] labels={"MEJOR PUNTUACIÓN","MÁXIMAS BAJAS","MAYOR SUPERVIVENCIA","EXPEDICIONES","JEFES ELIMINADOS","OBJETIVO"};
        string[] values={gm.highestScore.ToString("N0"),gm.highestKills.ToString(),$"{Mathf.FloorToInt(gm.bestTimeSurvived/60):00}:{Mathf.FloorToInt(gm.bestTimeSurvived%60):00}",gm.totalRunsAttempted.ToString(),gm.bossesSlain.ToString(),"10:00"};
        float cw=(MegabonkUI.Width-176)/3;
        for(int i=0;i<6;i++)
        {
            var r=new Rect(64+(i%3)*(cw+24),230+(i/3)*216,cw,190);
            MegabonkUI.MetalPanel(r);
            MegabonkUI.Text(new Rect(r.x+28,r.y+28,cw-56,30),labels[i],13,MegabonkUI.Muted,true);
            MegabonkUI.Text(new Rect(r.x+28,r.y+82,cw-56,74),values[i],48,MegabonkUI.White,true);
        }
        MenuBack();
    }

    string abilitySearch = "";
    int abilityFilter;
    string selectedAbility;
    readonly List<UpgradeDefinition> visibleAbilities = new List<UpgradeDefinition>();

    void DrawModernHub(GameManager gm)
    {
        MegabonkUI.MenuWorld();
        float w=MegabonkUI.Width, h=MegabonkUI.Height;
        // Columna izquierda: misiones, sin elementos ajenos a la interfaz del juego.
        var quests=new Rect(36,38,400,310);
        string[] questNames={"CAZADOR","DERROTAR JEFES","EXPLORADOR","ENJAMBRE FINAL"};
        string[] questText={"Elimina 150 enemigos entre partidas","Derrota 10 jefes","Inicia 5 expediciones","Sobrevive 60 s al enjambre final"};
        int[] questValue={Mathf.Min(ExpeditionProgress.Kills,150),Mathf.Min(gm.bossesSlain,10),Mathf.Min(gm.totalRunsAttempted,5),Mathf.Min(Mathf.FloorToInt(ExpeditionProgress.Swarm),60)};
        int[] questGoal={150,10,5,60};
        for(int i=0;i<4;i++)
        {
            float y=quests.y+i*80; var r=new Rect(quests.x,y,quests.width,70);
            MegabonkUI.MetalPanel(r);
            string[] questIcons={"bone","boss_buster","key","ghost"};
            MegabonkIconProvider.DrawItemIcon(new Rect(r.x+9,r.y+9,50,50),questIcons[i]);
            MegabonkUI.Text(new Rect(r.x+70,r.y+7,r.width-80,19),questNames[i],15,MegabonkUI.Gold,true);
            MegabonkUI.Text(new Rect(r.x+70,r.y+27,r.width-80,17),questText[i],13,MegabonkUI.White);
            MegabonkUI.Text(new Rect(r.xMax-90,r.y+48,76,17),$"{questValue[i]} / {questGoal[i]}",11,MegabonkUI.Cyan,true,TextAnchor.MiddleRight);
            MegabonkUI.Meter(new Rect(r.x+70,r.y+54,r.width-170,5),(float)questValue[i]/questGoal[i],new Color(.2f,.9f,.25f));
        }

        // Centro: marca y navegación principal, con los botones apilados de la referencia.
        MegabonkUI.Logo(new Rect(w*.5f-320,80,640,130));
        float menuX=w*.5f-130;
        if(MegabonkUI.StoneButton(new Rect(menuX+30,h*.39f,200,76),"JUGAR")) currentMenuScreen=MenuSubScreen.StartRun;
        if(MegabonkUI.StoneButton(new Rect(menuX-20,h*.39f+102,300,68),"DESBLOQUEOS")) currentMenuScreen=MenuSubScreen.Unlocks;
        if(MegabonkUI.StoneButton(new Rect(menuX+18,h*.39f+196,224,68),"MISIONES")) currentMenuScreen=MenuSubScreen.Quests;
        if(MegabonkUI.StoneButton(new Rect(menuX+32,h*.39f+290,196,68),"TIENDA")) currentMenuScreen=MenuSubScreen.Shop;
        if(MegabonkUI.StoneButton(new Rect(menuX+18,h*.39f+378,224,58),"COOPERATIVO LAN")) currentMenuScreen=MenuSubScreen.LanLobby;

        // Acciones secundarias, alineadas a la esquina inferior izquierda.
        if(MegabonkUI.StoneButton(new Rect(28,h-274,170,46),"AJUSTES")) currentMenuScreen=MenuSubScreen.Settings;
        if(MegabonkUI.StoneButton(new Rect(28,h-218,170,46),"ESPAÑOL")) { }
        if(MegabonkUI.StoneButton(new Rect(28,h-162,170,46),"TOGGLER")) currentMenuScreen=MenuSubScreen.Toggler;
        if(MegabonkUI.StoneButton(new Rect(28,h-106,170,46),"CRÉDITOS")) currentMenuScreen=MenuSubScreen.Credits;
        if(MegabonkUI.StoneButton(new Rect(28,h-50,170,38),"SALIR")) QuitGame();
        if(Event.current.type==EventType.KeyDown && Event.current.keyCode==KeyCode.Return)
        { currentMenuScreen=MenuSubScreen.StartRun; Event.current.Use(); }

        // Columna derecha: clasificación, sin datos externos ni avatares de terceros.
        var board=new Rect(w-388,h*.39f,370,h*.55f); MegabonkUI.MetalPanel(board,"CLASIFICACIÓN");
        if(MegabonkUI.StoneButton(new Rect(board.x+54,board.y-76,260,52),"Todos los rankings")) currentMenuScreen=MenuSubScreen.Leaderboards;
        MegabonkUI.Text(new Rect(board.x+18,board.y+52,300,22),"RÉCORD LOCAL · SIN CONEXIÓN",13,MegabonkUI.White,true);
        MegabonkUI.Fill(new Rect(board.x+16,board.y+79,board.width-32,1),new Color(.65f,.67f,.61f));
        MegabonkUI.Text(new Rect(board.x+20,board.y+92,55,18),"RANGO",10,MegabonkUI.Muted,true);
        MegabonkUI.Text(new Rect(board.x+86,board.y+92,145,18),"JUGADOR",10,MegabonkUI.Muted,true);
        MegabonkUI.Text(new Rect(board.x+235,board.y+92,75,18),"BAJAS",10,MegabonkUI.Muted,true,TextAnchor.MiddleRight);
        string[] names={"TÚ"};
        for(int i=0;i<names.Length;i++)
        {
            float y=board.y+114+i*34; MegabonkUI.Fill(new Rect(board.x+16,y,board.width-32,28),i==0?new Color(.12f,.30f,.47f):new Color(.17f,.19f,.18f));
            MegabonkUI.Text(new Rect(board.x+23,y+5,48,18),$"#{i+1}",12,MegabonkUI.White,true);
            MegabonkUI.Text(new Rect(board.x+86,y+5,140,18),names[i],12,MegabonkUI.White,true);
            MegabonkUI.Text(new Rect(board.x+230,y+5,78,18),i==0?gm.highestKills.ToString("N0"):"---",12,MegabonkUI.Gold,true,TextAnchor.MiddleRight);
        }
        MegabonkUI.Text(new Rect(w-355,18,320,26),$"PLATA  {gm.silver:N0}",20,MegabonkUI.Gold,true,TextAnchor.MiddleRight);
    }

    Vector2 shopScroll;
    void DrawMetaShop(GameManager gm)
    {
        MenuHeading("META-PROGRESIÓN", "Tienda de plata");
        var meta=MetaProgression.Instance;
        if(meta==null) { MegabonkUI.Text(new Rect(64,190,700,40),"Cargando progreso...",20,MegabonkUI.Muted); return; }
        MegabonkUI.Text(new Rect(64,170,800,34),$"PLATA DISPONIBLE: {gm.silver:N0}",22,MegabonkUI.Gold,true);
        string[] ids={"damage","speed","vitality","luck","weapon_slot","tome_slot"};
        string[] names={"PODER PERMANENTE","AGILIDAD PERMANENTE","VITALIDAD PERMANENTE","SUERTE PERMANENTE","ESPACIO DE ARMA","ESPACIO DE TOMO"};
        string[] descriptions={"+5% daño inicial por nivel.","+4% velocidad inicial por nivel.","+1 corazón inicial por nivel.","+5% suerte inicial por nivel.","Permite llevar 5 armas en vez de 4.","Permite llevar 5 tomos en vez de 4."};
        float gridWidth = Mathf.Min(MegabonkUI.Width-128, 1240f);
        float cardWidth = (gridWidth-48f)/3f;
        var shopView = new Rect(64,226,MegabonkUI.Width-128,Mathf.Max(160,MegabonkUI.Height-300));
        shopScroll=GUI.BeginScrollView(shopView,shopScroll,new Rect(0,0,gridWidth,480));
        for(int i=0;i<ids.Length;i++)
        {
            float x=(i%3)*(cardWidth+24); float y=(i/3)*238;
            var r=new Rect(x,y,cardWidth,220); int rank=meta.GetRank(ids[i]); bool max=rank>=meta.GetMaxRank(ids[i]);
            MegabonkUI.MetalPanel(r);
            MegabonkUI.Text(new Rect(x+20,y+22,cardWidth-40,44),names[i],18,MegabonkUI.White,true);
            MegabonkUI.Text(new Rect(x+20,y+76,cardWidth-40,30),$"Nivel {rank} / {meta.GetMaxRank(ids[i])}",16,MegabonkUI.Gold,true);
            MegabonkUI.Text(new Rect(x+20,y+112,cardWidth-40,48),descriptions[i],15,MegabonkUI.Muted);
            string label=max?"MEJORA COMPLETA":$"COMPRAR  /  {meta.GetCost(ids[i])} PLATA";
            if(MegabonkUI.Button(new Rect(x+20,y+166,cardWidth-40,42),label,max?MegabonkUI.Cyan:MegabonkUI.Gold,!max)) meta.Purchase(ids[i],gm);
        }
        GUI.EndScrollView();
        MegabonkUI.Text(new Rect(350,MegabonkUI.Height-94,MegabonkUI.Width-414,38),"Las compras se guardan y se aplican desde la siguiente expedición.",15,MegabonkUI.Muted);
        MenuBack();
    }

    Vector2 togglerScroll;
    void DrawToggler(GameManager gm)
    {
        MenuHeading("TOGGLER", "Controla las opciones de nivel");
        var meta=MetaProgression.Instance;
        MegabonkUI.Text(new Rect(64,170,1250,42),"Desactiva mejoras o tomos que no quieras recibir. Se guardan para futuras partidas.",17,MegabonkUI.Muted);
        if(meta==null) return;
        var view=new Rect(64,226,MegabonkUI.Width-128,MegabonkUI.Height-360);
        var all=UpgradeCatalog.AllUpgrades;
        togglerScroll=GUI.BeginScrollView(view,togglerScroll,new Rect(0,0,view.width-22,Mathf.Ceil(all.Count/3f)*112+10));
        for(int i=0;i<all.Count;i++)
        {
            var u=all[i]; int col=i%3,row=i/3; var r=new Rect(col*((view.width-34)/3+12),row*112,(view.width-34)/3,96);
            bool disabled=meta.IsDisabled(u.id); MegabonkUI.MetalPanel(r);
            MegabonkUI.Text(new Rect(r.x+16,r.y+14,r.width-32,28),MegabonkUI.Clean(u.name),17,MegabonkUI.White,true);
            MegabonkUI.Text(new Rect(r.x+16,r.y+47,r.width-32,20),u.type==UpgradeType.BossTome?"TOMO":"MEJORA",12,u.color,true);
            if(MegabonkUI.PixelButton(new Rect(r.x+16,r.y+70,r.width-32,20),disabled?"ACTIVAR":"DESACTIVAR",disabled)) meta.ToggleUpgrade(u.id);
        }
        GUI.EndScrollView();
        MenuBack();
    }

    void DrawModernPause(GameManager gm)
    {
        string heroName=OriginalContentCatalog.Selected?.displayName??gm.selectedCharacter.ToString();
        MegabonkUI.Background(true);
        float w=MegabonkUI.Width,h=MegabonkUI.Height;
        XPSystem xp=XPSystem.Instance;
        MegabonkUI.Text(new Rect(54,42,800,25),"EXPEDICIÓN EN PAUSA",13,MegabonkUI.Cyan,true);
        MegabonkUI.Text(new Rect(54,78,900,65),"Tu arsenal de habilidades",40,MegabonkUI.White,true);
        if(MegabonkUI.Button(new Rect(w-288,60,234,52),"REANUDAR  /  TAB",MegabonkUI.Cyan,true)) gm.Resume();
        if(MegabonkUI.Button(new Rect(w-288,119,112,36),"AJUSTES",MegabonkUI.Muted))pauseSettings=true;
        if(MegabonkUI.Button(new Rect(w-170,119,116,36),"REINICIAR",MegabonkUI.Muted))gm.RestartGame();
        MegabonkUI.Text(new Rect(54,150,1300,32),$"{heroName}    /    NIVEL {xp?.level ?? 1}    /    OLEADA {spawner?.currentWave ?? 0}    /    {Mathf.FloorToInt(gm.gameTime/60):00}:{Mathf.FloorToInt(gm.gameTime%60):00}    /    {gm.kills} BAJAS",16,MegabonkUI.Muted);
        float leftW=w-560;
        string[] tabs={"ADQUIRIDAS","CATÁLOGO","SINERGIAS","TOMOS"};
        for(int i=0;i<tabs.Length;i++)
            if(MegabonkUI.Button(new Rect(54+i*176,204,164,44),tabs[i],MegabonkUI.Cyan,abilityFilter==i))
            { abilityFilter=i; skillsOverviewScrollPos=Vector2.zero; }
        MegabonkUI.Text(new Rect(54,267,90,24),"BUSCAR",12,MegabonkUI.Muted,true);
        abilitySearch=GUI.TextField(new Rect(148,260,leftW-148,38),abilitySearch,64);
        visibleAbilities.Clear();
        foreach(var def in UpgradeCatalog.AllUpgrades)
        {
            bool owned=xp!=null && xp.HasUpgrade(def.id);
            if(abilityFilter==0&&!owned || abilityFilter==2&&def.type!=UpgradeType.Synergy || abilityFilter==3&&def.category!="TOMO") continue;
            if(abilitySearch.Length>0 && def.name.IndexOf(abilitySearch,StringComparison.OrdinalIgnoreCase)<0 && def.category.IndexOf(abilitySearch,StringComparison.OrdinalIgnoreCase)<0) continue;
            visibleAbilities.Add(def);
        }
        var viewport=new Rect(54,320,leftW-32,h-402);
        float cardW=(viewport.width-34)/2, rowH=132;
        skillsOverviewScrollPos=GUI.BeginScrollView(viewport,skillsOverviewScrollPos,new Rect(0,0,viewport.width-20,Mathf.Max(viewport.height,Mathf.Ceil(visibleAbilities.Count/2f)*rowH)));
        for(int i=0;i<visibleAbilities.Count;i++)
        {
            var def=visibleAbilities[i];
            var r=new Rect((i%2)*(cardW+12),(i/2)*rowH,cardW,118);
            bool owned=xp!=null&&xp.HasUpgrade(def.id);
            MegabonkUI.Panel(r,owned?def.color:MegabonkUI.Muted);
            if(selectedAbility==def.id) MegabonkUI.Fill(new Rect(r.x,r.y,r.width,3),MegabonkUI.Cyan);
            MegabonkIconProvider.DrawUpgradeIcon(new Rect(r.x+16,r.y+14,48,48),def);
            int realLevel=MegabonkBuild.Instance!=null?MegabonkBuild.Instance.Level(def.id):0;
            MegabonkUI.Text(new Rect(r.x+16,r.y+66,60,26),realLevel>0?"NV "+realLevel:"NUEVA",12,def.color,true);
            MegabonkUI.Text(new Rect(r.x+80,r.y+16,r.width-96,48),MegabonkUI.Clean(def.name),18,MegabonkUI.White,true);
            MegabonkUI.Text(new Rect(r.x+80,r.y+72,r.width-96,30),owned?"ADQUIRIDA  /  "+MegabonkUI.Clean(def.category):"POR DESCUBRIR",12,owned?MegabonkUI.Cyan:MegabonkUI.Muted);
            if(GUI.Button(r,GUIContent.none,GUIStyle.none)) selectedAbility=def.id;
        }
        if(visibleAbilities.Count==0)
        {
            MegabonkUI.Text(new Rect(30,60,viewport.width-60,50),"Todavía no hay habilidades aquí",25,MegabonkUI.White,true);
            MegabonkUI.Text(new Rect(30,125,viewport.width-60,90),"Recoge experiencia para elegir mejoras al subir de nivel.\nExplora el catálogo para planear tu siguiente elección.",18,MegabonkUI.Muted);
        }
        GUI.EndScrollView();
        float dx=w-486;
        MegabonkUI.Panel(new Rect(dx,204,432,h-286),MegabonkUI.Gold);
        var chosen=UpgradeCatalog.Get(selectedAbility??"");
        if(chosen!=null)
        {
            MegabonkUI.Text(new Rect(dx+28,232,376,26),"DETALLE DE HABILIDAD",12,MegabonkUI.Gold,true);
            MegabonkUI.Text(new Rect(dx+28,276,376,85),MegabonkUI.Clean(chosen.name),29,MegabonkUI.White,true);
            MegabonkUI.Text(new Rect(dx+28,377,376,160),MegabonkUI.Clean(chosen.description),19,MegabonkUI.Muted);
            string state=xp!=null&&xp.HasUpgrade(chosen.id)?"ADQUIRIDA":"SE OBTIENE DURANTE LA EXPEDICIÓN";
            MegabonkUI.Text(new Rect(dx+28,550,376,48),state,13,MegabonkUI.Cyan,true);
            string requirements="";
            if(!string.IsNullOrEmpty(chosen.prerequisiteId)) requirements="Requiere: "+(UpgradeCatalog.Get(chosen.prerequisiteId)?.name??chosen.prerequisiteId);
            if(chosen.synergyRequirements!=null) foreach(string id in chosen.synergyRequirements) requirements+="\n"+(UpgradeCatalog.Get(id)?.name??id);
            MegabonkUI.Text(new Rect(dx+28,608,376,115),MegabonkUI.Clean(requirements),16,MegabonkUI.Muted);
        }
        else
        {
            MegabonkUI.Text(new Rect(dx+28,235,376,30),"ESTADO DEL SUPERVIVIENTE",13,MegabonkUI.Gold,true);
            MegabonkUI.Text(new Rect(dx+28,288,376,55),heroName.ToUpper(),32,MegabonkUI.White,true);
            float currentLuck = MegabonkRelicManager.Instance != null ? MegabonkRelicManager.Instance.luckPercent : 0f;
            MegabonkUI.Text(new Rect(dx+28,364,376,264),$"Vida   {playerHealth.currentHealth} / {playerHealth.maxHealth}\nEscudo   {playerHealth.ShieldPercent:0}%\n\nSuerte   +{currentLuck:0.#}%\n\nDaño base   x{xp?.GetCurrentDamageMultiplier() ?? 1:0.00}\nCadencia   x{(xp?.GetCurrentFireRateMultiplier() ?? 1)*(CharacterPassives.Instance?.Rate??1):0.00}\nVelocidad   x{(xp?.GetCurrentSpeedMultiplier() ?? 1)*(CharacterPassives.Instance?.Speed??1):0.00}",20,MegabonkUI.White);
            MegabonkUI.Text(new Rect(dx+28,646,376,90),"Selecciona una tarjeta para ver su efecto y sus requisitos.",18,MegabonkUI.Muted);
        }
        MegabonkUI.Text(new Rect(54,h-36,950,24),"TAB / ESC  Reanudar     /     RUEDA  Desplazarse     /     CLIC  Inspeccionar",13,MegabonkUI.Muted);
        if(MegabonkUI.Button(new Rect(w-288,h-42,234,32),"VOLVER AL MENÚ",MegabonkUI.Muted)) gm.ReturnToMainMenu();
    }

    void DrawModernHUD(GameManager gm)
    {
        float w = MegabonkUI.Width, h = MegabonkUI.Height;
        var xp = XPSystem.Instance;

        // ══════════════════════════════════════════════════════════════
        // 1. PANEL DE VITALIDAD POR CORAZONCITOS (Abajo Izquierda)
        // ══════════════════════════════════════════════════════════════
        int maxH = playerHealth != null ? playerHealth.maxHearts : 4;
        int curH = playerHealth != null ? playerHealth.currentHearts : 4;
        bool lowHp = curH <= 1;

        MegabonkUI.Panel(new Rect(28, h - 130, 340, 100), lowHp ? new Color(1f, 0.32f, 0.36f) : MegabonkUI.Cyan);

        string statusText = "VITALIDAD";
        if (xp != null && xp.holyShieldActive)
            statusText = "ESCUDO SAGRADO";
        else if (xp != null && xp.HasUpgrade("vitality_4") && xp.immortalHeartReady)
            statusText = "INMORTAL LISTO";
        else if (playerHealth != null && playerHealth.isBurning)
            statusText = "QUEMADURA DRAGÓN";

        MegabonkUI.Text(new Rect(48, h - 116, 170, 18), statusText, 12, MegabonkUI.Muted, true);
        MegabonkUI.Text(new Rect(220, h - 116, 128, 18), $"{curH} / {maxH} ❤", 12, lowHp ? new Color(1f, 0.35f, 0.4f) : MegabonkUI.Cyan, true, TextAnchor.MiddleRight);

        // Renderizado de Corazoncitos
        float startHeartX = 46f;
        float heartY = h - 90f;
        int maxPerRow = 7;
        float heartSlotSize = maxH > 7 ? 24f : 30f;
        float heartGap = maxH > 7 ? 4f : 7f;
        int heartFontSize = maxH > 7 ? 18 : 24;

        float lowHealthPulse = lowHp ? (0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 10f)) : 1f;
        Color heartFilledColor = new Color(1f, 0.22f * lowHealthPulse, 0.30f * lowHealthPulse, 0.98f);
        Color heartEmptyColor = new Color(0.28f, 0.35f, 0.44f, 0.45f);

        for (int i = 0; i < maxH; i++)
        {
            int row = i / maxPerRow;
            int col = i % maxPerRow;
            float hx = startHeartX + col * (heartSlotSize + heartGap);
            float hy = heartY + row * (heartSlotSize + 2f);
            bool filled = i < curH;

            // Ranura oscura estilizada de fondo
            MegabonkUI.Fill(new Rect(hx, hy, heartSlotSize, heartSlotSize), new Color(0.04f, 0.07f, 0.11f, 0.85f));
            MegabonkUI.Fill(new Rect(hx, hy + heartSlotSize - 1f, heartSlotSize, 1f), filled ? new Color(1f, 0.2f, 0.3f, 0.5f) : new Color(0.2f, 0.25f, 0.35f, 0.3f));

            // Corazoncito gráfico
            MegabonkUI.Text(new Rect(hx, hy - 2f, heartSlotSize, heartSlotSize), filled ? "♥" : "♡", heartFontSize, filled ? heartFilledColor : heartEmptyColor, true, TextAnchor.MiddleCenter);
        }

        // ══════════════════════════════════════════════════════════════
        // 2. PANEL DE ARMA Y MUNICIÓN (Abajo Derecha)
        // ══════════════════════════════════════════════════════════════
        if (playerShooting != null)
        {
            MegabonkUI.Panel(new Rect(w - 368, h - 130, 340, 100), MegabonkUI.Gold);
            MegabonkUI.Text(new Rect(w - 348, h - 113, 300, 22), playerShooting.currentWeapon?.name.ToUpper() ?? "ARMA", 12, MegabonkUI.Muted, true);
            MegabonkUI.Text(new Rect(w - 348, h - 83, 300, 44), playerShooting.isReloading ? "RECARGANDO…" : $"{playerShooting.currentAmmo:00} / {playerShooting.magazineSize:00}", 28, MegabonkUI.Gold, true);
        }

        // ══════════════════════════════════════════════════════════════
        // 3. RELOJ Y OLEADA (Arriba Centro)
        // ══════════════════════════════════════════════════════════════
        MegabonkUI.Panel(new Rect(w / 2 - 145, 28, 290, 80), gm.finalSwarmActive ? new Color(1f, 0.28f, 0.28f) : MegabonkUI.Cyan);
        MegabonkUI.Text(new Rect(w / 2 - 120, 38, 240, 38), $"{Mathf.FloorToInt(gm.gameTime / 60):00}:{Mathf.FloorToInt(gm.gameTime % 60):00}", 30, MegabonkUI.White, true, TextAnchor.MiddleCenter);
        StageDirector stages = StageDirector.Instance;
        string expeditionState = gm.finalSwarmActive
            ? $"FINAL SWARM  +{Mathf.FloorToInt(gm.finalSwarmTime):00}s"
            : stages != null && stages.FinalEncounterActive ? "JEFE FINAL  •  REY FANTASMA"
            : stages != null && stages.PortalActive ? $"ETAPA {stages.CurrentStage}/3  •  PORTAL ABIERTO"
            : stages != null && stages.BossGateActive ? $"ETAPA {stages.CurrentStage}/3  •  JEFE INVOCADO"
            : stages != null ? $"ETAPA {stages.CurrentStage}/3  •  ACTIVA EL ARCO DEL JEFE"
            : spawner != null && spawner.isResting ? $"PRÓXIMA OLEADA  {Mathf.CeilToInt(spawner.restTimeLeft)}s"
            : $"OLEADA {spawner?.currentWave ?? 0}   /   {EnemyHealth3D.AliveCount} ENEMIGOS";
        MegabonkUI.Text(new Rect(w / 2 - 135, 80, 270, 22), expeditionState, 12, gm.finalSwarmActive ? new Color(1f, 0.45f, 0.45f) : MegabonkUI.Cyan, true, TextAnchor.MiddleCenter);

        // ══════════════════════════════════════════════════════════════
        // 4. LATERAL IZQUIERDO: HABILIDADES (Level-up / Tomos / Sinergias)
        // ══════════════════════════════════════════════════════════════
        if (xp != null)
        {
            // Barra de XP inferior al centro
            MegabonkUI.Text(new Rect(w / 2 - 250, h - 78, 500, 24), $"NIVEL {xp.level}    /    {xp.currentXP} DE {xp.maxXP} XP", 13, MegabonkUI.Muted, true, TextAnchor.MiddleCenter);
            MegabonkUI.Meter(new Rect(w / 2 - 250, h - 43, 500, 4), (float)xp.currentXP / Mathf.Max(1, xp.maxXP), MegabonkUI.Cyan);

            int abilityCount = xp.acquiredUpgradeIds.Count;
            int displayCount = Mathf.Min(8, abilityCount);
            float panelH = Mathf.Max(112f, 96f + (displayCount == 0 ? 30f : displayCount * 30f));

            MegabonkUI.Panel(new Rect(28, 28, 340, panelH), MegabonkUI.Cyan);
            MegabonkUI.Text(new Rect(48, 40, 300, 24), $"{gm.kills:000} BAJAS   /   {gm.gold:N0} ORO", 16, MegabonkUI.White, true);
            MegabonkUI.Text(new Rect(48, 70, 300, 22), $"HABILIDADES  [{abilityCount}]   •   TAB", 12, MegabonkUI.Cyan, true);

            if (abilityCount == 0)
            {
                MegabonkUI.Text(new Rect(48, 98, 300, 22), "Sin habilidades aún", 12, MegabonkUI.Muted);
            }
            else
            {
                int idx = 0;
                foreach (var def in UpgradeCatalog.AllUpgrades)
                {
                    if (!xp.HasUpgrade(def.id)) continue;
                    if (idx >= 8) break;

                    float rowY = 98f + idx * 30f;
                    MegabonkUI.Fill(new Rect(44, rowY + 3f, 3, 18), def.color);

                    string timer = xp.GetAbilityTimerText(def.id);
                    if (!string.IsNullOrEmpty(timer))
                    {
                        MegabonkUI.Text(new Rect(56, rowY, 190, 24), MegabonkUI.Clean(def.name), 13, MegabonkUI.White);
                        MegabonkUI.Text(new Rect(245, rowY, 105, 24), timer, 11, def.color, true, TextAnchor.MiddleRight);
                    }
                    else
                    {
                        MegabonkUI.Text(new Rect(56, rowY, 290, 24), MegabonkUI.Clean(def.name), 13, MegabonkUI.Muted);
                    }
                    idx++;
                }

                if (abilityCount > 8)
                {
                    MegabonkUI.Text(new Rect(56, 98f + 8 * 30f, 290, 20), $"+ {abilityCount - 8} más en [TAB]", 11, MegabonkUI.Cyan);
                }
            }
        }

        // ══════════════════════════════════════════════════════════════
        // 5. LATERAL DERECHO: OBJETOS DE MEGABONK (Reliquias de Cofres)
        // ══════════════════════════════════════════════════════════════
        float rightX = w - 368f;
        MegabonkRelicManager relicMgr = MegabonkRelicManager.Instance;
        if (relicMgr != null)
        {
            int totalRelics = 0;
            List<KeyValuePair<string, int>> userRelics = new List<KeyValuePair<string, int>>();
            foreach (var kvp in relicMgr.acquiredRelics)
            {
                if (kvp.Value > 0)
                {
                    totalRelics += kvp.Value;
                    userRelics.Add(kvp);
                }
            }

            int displayRelics = Mathf.Min(8, userRelics.Count);
            float relicPanelH = Mathf.Max(116f, 96f + (userRelics.Count == 0 ? 36f : displayRelics * 30f));
            relicPanelH = Mathf.Min(relicPanelH, h - 145f - 175f);

            MegabonkUI.Panel(new Rect(rightX, 175, 340, relicPanelH), MegabonkUI.Gold);

            float luck = relicMgr.luckPercent;
            Color luckCol = luck > 0f ? new Color(0.25f, 1f, 0.45f) : MegabonkUI.Muted;
            MegabonkUI.Text(new Rect(rightX + 18, 188, 160, 20), $"SUERTE +{luck:0.#}%", 12, luckCol, true);
            MegabonkUI.Text(new Rect(rightX + 175, 188, 147, 20), $"OBJETOS [{totalRelics}]", 12, MegabonkUI.Gold, true, TextAnchor.MiddleRight);
            MegabonkUI.Fill(new Rect(rightX + 18, 214, 304, 1), new Color(0.3f, 0.38f, 0.45f, 0.6f));

            if (userRelics.Count == 0)
            {
                MegabonkUI.Text(new Rect(rightX + 20, 224, 300, 20), "Sin objetos de cofres", 12, MegabonkUI.Muted);
                MegabonkUI.Text(new Rect(rightX + 20, 244, 300, 20), "Derrota Élites o Jefes para obtenerlos", 11, new Color(0.5f, 0.58f, 0.66f, 0.7f));
            }
            else
            {
                for (int i = 0; i < displayRelics; i++)
                {
                    var kvp = userRelics[i];
                    var rData = MegabonkRelicManager.GetRelicData(kvp.Key);
                    Color rColor = rData != null ? rData.bannerColor : MegabonkUI.Gold;
                    string rName = rData != null ? rData.name : kvp.Key.Replace('_', ' ');

                    float rowY = 222f + i * 30f;
                    MegabonkUI.Fill(new Rect(rightX + 18, rowY + 3f, 3, 18), rColor);

                    // Nombre del objeto limpio para fuentes estándar
                    MegabonkUI.Text(new Rect(rightX + 28, rowY, 235, 24), MegabonkUI.Clean(rName), 13, MegabonkUI.White);
                    // Cantidad apilada
                    MegabonkUI.Text(new Rect(rightX + 265, rowY, 55, 24), $"x{kvp.Value}", 12, rColor, true, TextAnchor.MiddleRight);
                }

                if (userRelics.Count > 8)
                {
                    MegabonkUI.Text(new Rect(rightX + 28, 222f + 8 * 30f, 290, 20), $"+ {userRelics.Count - 8} objetos más activos", 11, MegabonkUI.Gold);
                }
            }
        }

        DrawBossBar();
        DrawMinimap();
    }

    void DrawModernEnd(GameManager gm, bool victory)
    {
        MegabonkUI.Background();
        float x=MegabonkUI.Width/2-400,y=MegabonkUI.Height/2-290;
        Color accent=victory?MegabonkUI.Gold:new Color(1,.4f,.35f);
        MegabonkUI.Panel(new Rect(x,y,800,560),accent);
        MegabonkUI.Text(new Rect(x+40,y+38,720,25),victory?"EXPEDICIÓN COMPLETADA":"FIN DE LA EXPEDICIÓN",13,accent,true,TextAnchor.MiddleCenter);
        MegabonkUI.Text(new Rect(x+40,y+100,720,80),victory?"EXPEDICIÓN COMPLETADA":"FIN DE LA EXPEDICIÓN",42,MegabonkUI.White,true,TextAnchor.MiddleCenter);
        MegabonkUI.Text(new Rect(x+60,y+205,680,100),$"{gm.score:N0} PUNTOS     /     {gm.kills} BAJAS\n\nOLEADA {gm.wavesReached}     /     {Mathf.FloorToInt(gm.gameTime/60):00}:{Mathf.FloorToInt(gm.gameTime%60):00}",22,MegabonkUI.Muted,false,TextAnchor.MiddleCenter);
        if(MegabonkUI.Button(new Rect(x+160,y+405,480,60),"VOLVER AL MENÚ  /  R",accent,true)) gm.ReturnToMainMenu();
        if(MegabonkUI.Button(new Rect(x+160,y+330,480,55),"REINTENTAR",MegabonkUI.Muted))gm.RestartGame();
        if(Event.current.type==EventType.KeyDown && Event.current.keyCode==KeyCode.R) { gm.ReturnToMainMenu(); Event.current.Use(); }
    }
}
