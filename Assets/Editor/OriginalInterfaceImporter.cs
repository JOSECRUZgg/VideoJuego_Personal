using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Imports visual RectTransform hierarchies only. Never imports unimplemented game/Steam scripts.</summary>
[InitializeOnLoad]
public static class OriginalInterfaceImporter
{
    const string Source=@"C:\Users\JOSEC\Desktop\MATERIAS UT\10MO CUATRIMESTRE\JUAN\MEGABONK\ExportedProject\Assets";
    const string Output="Assets/Resources/OriginalUI";
    const string Revision="2";
    const string RevisionFile="Assets/Resources/OriginalUI/import-version.txt";
    class Node {public string id,name,rect,parent,body;public bool active;public List<string> components=new List<string>();}
    static OriginalInterfaceImporter(){EditorApplication.delayCall+=Ensure;}
    static void Ensure(){if(Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)return;if(EditorApplication.isCompiling || EditorApplication.isUpdating){EditorApplication.delayCall+=Ensure;return;}if((!File.Exists(RevisionFile) || File.ReadAllText(RevisionFile)!=Revision) && Directory.Exists(Source))Import();}
    static string Field(string text,string name)=>Regex.Match(text,@"(?m)^\s*"+Regex.Escape(name)+@": ([^\r\n]*)").Groups[1].Value.Trim();
    static string Ref(string text,string name)=>Regex.Match(text,Regex.Escape(name)+@": \{fileID: (-?\d+)").Groups[1].Value;
    static float Number(string value,float fallback=0)=>float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var n)?n:fallback;
    static Vector2 Vec(string text,string field,Vector2 fallback){var m=Regex.Match(text,Regex.Escape(field)+@": \{x: ([^,]+), y: ([^}]+)\}");return m.Success?new Vector2(Number(m.Groups[1].Value),Number(m.Groups[2].Value)):fallback;}
    static Color Tint(string text,string field){var m=Regex.Match(text,Regex.Escape(field)+@": \{r: ([^,]+), g: ([^,]+), b: ([^,]+), a: ([^}]+)\}");return m.Success?new Color(Number(m.Groups[1].Value),Number(m.Groups[2].Value),Number(m.Groups[3].Value),Number(m.Groups[4].Value)):Color.white;}
    static T ResourceFor<T>(string text,string field) where T:UnityEngine.Object
    {
        string guid=Regex.Match(text,Regex.Escape(field)+@": \{[^\r\n]*guid: ([a-f0-9]{32})").Groups[1].Value;
        if(guid.Length==0)return null;
        using(var md5=MD5.Create())guid=BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes("UnityShooter.Visuals."+guid))).Replace("-","").ToLowerInvariant();
        return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
    }
    static void ImportLayout(GameObject go,string body)
    {
        // These fileIDs identify the UGUI components in the supplied export DLL.
        string script=Ref(body,"m_Script");
        if(script=="1297475563" || script=="-405508275")
        {
            HorizontalOrVerticalLayoutGroup group=script=="1297475563"?(HorizontalOrVerticalLayoutGroup)go.AddComponent<VerticalLayoutGroup>():go.AddComponent<HorizontalLayoutGroup>();
            group.padding=new RectOffset((int)Number(Field(body,"m_Left")),(int)Number(Field(body,"m_Right")),(int)Number(Field(body,"m_Top")),(int)Number(Field(body,"m_Bottom")));
            group.childAlignment=(TextAnchor)(int)Number(Field(body,"m_ChildAlignment"));group.spacing=Number(Field(body,"m_Spacing"));
            group.childControlWidth=Field(body,"m_ChildControlWidth")=="1";group.childControlHeight=Field(body,"m_ChildControlHeight")=="1";
            group.childForceExpandWidth=Field(body,"m_ChildForceExpandWidth")=="1";group.childForceExpandHeight=Field(body,"m_ChildForceExpandHeight")=="1";
            group.childScaleWidth=Field(body,"m_ChildScaleWidth")=="1";group.childScaleHeight=Field(body,"m_ChildScaleHeight")=="1";group.reverseArrangement=Field(body,"m_ReverseArrangement")=="1";group.enabled=Field(body,"m_Enabled")!="0";
        }
        if(body.Contains("m_IgnoreLayout:"))
        {
            var layout=go.AddComponent<LayoutElement>();layout.ignoreLayout=Field(body,"m_IgnoreLayout")=="1";
            layout.minWidth=Number(Field(body,"m_MinWidth"),-1);layout.minHeight=Number(Field(body,"m_MinHeight"),-1);
            layout.preferredWidth=Number(Field(body,"m_PreferredWidth"),-1);layout.preferredHeight=Number(Field(body,"m_PreferredHeight"),-1);
            layout.flexibleWidth=Number(Field(body,"m_FlexibleWidth"),-1);layout.flexibleHeight=Number(Field(body,"m_FlexibleHeight"),-1);layout.layoutPriority=(int)Number(Field(body,"m_LayoutPriority"),1);layout.enabled=Field(body,"m_Enabled")!="0";
        }
        if(body.Contains("m_HorizontalFit:"))
        {var fit=go.AddComponent<ContentSizeFitter>();fit.horizontalFit=(ContentSizeFitter.FitMode)(int)Number(Field(body,"m_HorizontalFit"));fit.verticalFit=(ContentSizeFitter.FitMode)(int)Number(Field(body,"m_VerticalFit"));fit.enabled=Field(body,"m_Enabled")!="0";}
        if(body.Contains("m_CellSize:") && body.Contains("m_ConstraintCount:"))
        {
            var grid=go.AddComponent<GridLayoutGroup>();grid.cellSize=Vec(body,"m_CellSize",new Vector2(100,100));grid.spacing=Vec(body,"m_Spacing",Vector2.zero);
            grid.padding=new RectOffset((int)Number(Field(body,"m_Left")),(int)Number(Field(body,"m_Right")),(int)Number(Field(body,"m_Top")),(int)Number(Field(body,"m_Bottom")));
            grid.childAlignment=(TextAnchor)(int)Number(Field(body,"m_ChildAlignment"));grid.startCorner=(GridLayoutGroup.Corner)(int)Number(Field(body,"m_StartCorner"));grid.startAxis=(GridLayoutGroup.Axis)(int)Number(Field(body,"m_StartAxis"));
            grid.constraint=(GridLayoutGroup.Constraint)(int)Number(Field(body,"m_Constraint"));grid.constraintCount=Mathf.Max(1,(int)Number(Field(body,"m_ConstraintCount"),2));grid.enabled=Field(body,"m_Enabled")!="0";
        }
    }
    static Sprite SpriteFor(string text)
    {
        string guid=Regex.Match(text,@"m_Sprite: \{[^\r\n]*guid: ([a-f0-9]{32})").Groups[1].Value;
        if(guid.Length==0)return null;
        using(var md5=MD5.Create())guid=BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes("UnityShooter.Visuals."+guid))).Replace("-","").ToLowerInvariant();
        string path=AssetDatabase.GUIDToAssetPath(guid);return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    [MenuItem("Tools/Megabonk/Importar jerarquías UI originales")]
    public static void Import()
    {
        Directory.CreateDirectory(Output);Directory.CreateDirectory("Docs");
        var report=new StringBuilder("# Auditoría de interfaces originales\n\nImportador v2. Se preservan jerarquías RectTransform, anclajes, escalas, orden, actividad, disposición horizontal/vertical/cuadrícula, dimensiones de layout, imágenes y RawImage disponibles. TMP se adapta a Text/alagard con alineación. No se importan controladores vacíos, Steam, eventos destructivos ni animaciones con bindings no verificados.\n\n");
        foreach(var scene in new[]{"MainMenu","LoadingScreen","GeneratedMap","FinalBossMap"})
        {
            string path=Path.Combine(Source,"Scenes",scene+".unity");if(!File.Exists(path)){report.AppendLine("Falta "+path);continue;}
            var blocks=new Dictionary<string,string>();var nodes=new Dictionary<string,Node>();var rects=new Dictionary<string,Node>();
            foreach(Match m in Regex.Matches(File.ReadAllText(path),@"(?ms)^--- !u!(\d+) &(-?\d+)\r?\n(.*?)(?=^--- !u!|\z)"))
            {
                string id=m.Groups[2].Value,body=m.Groups[3].Value;blocks[id]=body;
                if(m.Groups[1].Value=="1")
                {
                    var node=new Node{id=id,name=Field(body,"m_Name"),active=Field(body,"m_IsActive")!="0"};
                    foreach(Match c in Regex.Matches(body,@"component: \{fileID: (-?\d+)\}"))node.components.Add(c.Groups[1].Value);
                    nodes[id]=node;
                }
            }
            foreach(var node in nodes.Values) foreach(var id in node.components)
                if(blocks.TryGetValue(id,out var body) && body.StartsWith("RectTransform:")){node.rect=id;node.parent=Ref(body,"m_Father");node.body=body;rects[id]=node;}
            string[] names=scene=="MainMenu"?new[]{"Menu","W_Settings","W_Shop","W_Character","W_Maps","W_LeaderboardsFull","W_Quests2NEW","W_Credits"}:scene=="LoadingScreen"?new[]{"Canvas"}:new[]{"Main","W_Settings","HUD","DeathScreen","TierVictoryScreen","LevelupScreen"};
            foreach(string name in names)
            {
                var root=rects.Values.FirstOrDefault(n=>n.name==name && (name!="Main" || Hierarchy(n,rects).Contains("PauseUI")));
                if(root==null){report.AppendLine(scene+" / "+name+": no encontrado.");continue;}
                int count=0,imagesMissing=0,actions=0;
                Func<Node,Transform,GameObject> create=null;
                create=(node,parent)=>
                {
                    count++;var go=new GameObject(node.name,typeof(RectTransform));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);
                    rt.anchorMin=Vec(node.body,"m_AnchorMin",Vector2.zero);rt.anchorMax=Vec(node.body,"m_AnchorMax",Vector2.one);rt.pivot=Vec(node.body,"m_Pivot",Vector2.one*.5f);
                    rt.sizeDelta=Vec(node.body,"m_SizeDelta",Vector2.zero);rt.anchoredPosition=Vec(node.body,"m_AnchoredPosition",Vector2.zero);
                    var scale=Regex.Match(node.body,@"m_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}");
                    if(scale.Success)rt.localScale=new Vector3(Number(scale.Groups[1].Value,1),Number(scale.Groups[2].Value,1),Number(scale.Groups[3].Value,1));
                    foreach(var id in node.components)
                    {
                        if(!blocks.TryGetValue(id,out var body))continue;
                        ImportLayout(go,body);
                        if(body.Contains("m_UVRect:") && body.Contains("m_Texture:"))
                        {var raw=go.AddComponent<RawImage>();raw.texture=ResourceFor<Texture>(body,"m_Texture");raw.color=Tint(body,"m_Color");raw.raycastTarget=false;raw.enabled=Field(body,"m_Enabled")!="0";}
                        if(body.Contains("m_Sprite:"))
                        {var image=go.GetComponent<Image>()??go.AddComponent<Image>();image.sprite=SpriteFor(body);image.color=Tint(body,"m_Color");image.raycastTarget=false;if(image.sprite==null && body.Contains("m_Sprite: {fileID: 0}")==false)imagesMissing++;image.type=(Image.Type)Mathf.Clamp((int)Number(Field(body,"m_Type")),0,3);image.preserveAspect=Field(body,"m_PreserveAspect")=="1";image.fillAmount=Number(Field(body,"m_FillAmount"),1);image.enabled=Field(body,"m_Enabled")!="0";}
                        if(body.Contains("m_fontSize:") && body.Contains("m_text:"))
                        {
                            var text=go.GetComponent<Text>();
                            if(text==null && go.GetComponent<Graphic>()!=null)
                            {
                                var label=new GameObject("ImportedLabel",typeof(RectTransform),typeof(Text));
                                var labelRect=label.GetComponent<RectTransform>();labelRect.SetParent(rt,false);labelRect.anchorMin=Vector2.zero;labelRect.anchorMax=Vector2.one;labelRect.sizeDelta=Vector2.zero;
                                text=label.GetComponent<Text>();
                            }
                            text=text??go.AddComponent<Text>();text.text=Field(body,body.Contains("m_text:")?"m_text":"m_Text").Trim('"');
                            text.font=Resources.Load<Font>("ExtractedVisuals/Font/alagard");text.fontSize=Mathf.Clamp(Mathf.RoundToInt(Number(Field(body,"m_fontSize"),24)),8,150);
                            text.color=Tint(body,"m_fontColor");text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;
                            int horizontal=(int)Number(Field(body,"m_HorizontalAlignment"),2),vertical=(int)Number(Field(body,"m_VerticalAlignment"),512);
                            int column=horizontal==1?0:horizontal==4?2:1,row=vertical==256?0:vertical==1024?2:1;
                            text.alignment=(TextAnchor)(row*3+column);
                            text.enabled=Field(body,"m_Enabled")!="0";
                        }
                        var method=Regex.Match(body,@"m_MethodName: (GoToCharacterSelection|GoToSettings|GoToShop|GoToQuests|GoToCredits|GoToLeaderboards|GoToUnlocks|GoToMenu|ExitGame)\b");
                        if(method.Success && go.GetComponent<OriginalMenuAction>()==null)
                        {var button=go.GetComponent<Button>()??go.AddComponent<Button>();var graphic=go.GetComponent<Graphic>()??go.AddComponent<Image>();graphic.raycastTarget=true;button.targetGraphic=graphic;go.AddComponent<OriginalMenuAction>().method=method.Groups[1].Value;actions++;}
                    }
                    foreach(Match child in Regex.Matches(node.body,@"(?m)^  - \{fileID: (-?\d+)\}"))if(rects.TryGetValue(child.Groups[1].Value,out var next))create(next,rt);
                    go.SetActive(node==root || node.active);return go;
                };
                GameObject obj=null;
                try
                {
                    obj=create(root,null);
                    if(scene=="MainMenu" && name=="Menu")
                    {
                        // Source fun-fact is placeholder text, not live project data.
                        var fact=obj.transform.Find("Logo/Funfact");if(fact!=null)fact.gameObject.SetActive(false);
                        foreach(var action in obj.GetComponentsInChildren<OriginalMenuAction>(true))
                        {
                            string label=MenuLabel(action.method);if(label==null)continue;
                            var texts=action.GetComponentsInChildren<Text>(true);if(texts.Length>0)texts[0].text=label;
                        }
                    }
                    PrefabUtility.SaveAsPrefabAsset(obj,Output+"/"+scene+"_"+name+".prefab");
                }
                finally{if(obj!=null)UnityEngine.Object.DestroyImmediate(obj);}
                report.AppendLine(scene+" / "+Hierarchy(root,rects)+": "+count+" nodos; "+actions+" acciones; "+imagesMissing+" imágenes sin sprite resuelto.");
            }
        }
        report.AppendLine("\nImportador v2: conserva escalas, RawImage, disposición horizontal/vertical, LayoutElement, ContentSizeFitter y tipo de Image. El puente de menú sigue deshabilitado hasta validar en Play. Los otros prefabs son plantillas: faltan conexiones de datos y eventos.");
        File.WriteAllText("Docs/OriginalInterfaceAudit.md",report.ToString());AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        File.WriteAllText(RevisionFile,Revision);
    }
    static string Hierarchy(Node node,Dictionary<string,Node> rects){var names=new List<string>();var visited=new HashSet<string>();while(node!=null && visited.Add(node.id)){names.Add(node.name);rects.TryGetValue(node.parent??"",out node);}names.Reverse();return string.Join("/",names);}
    static string MenuLabel(string method)
    {
        switch(method){case "GoToCharacterSelection":return "JUGAR";case "GoToSettings":return "AJUSTES";case "GoToShop":return "TIENDA";case "GoToQuests":return "MISIONES";case "GoToCredits":return "CRÉDITOS";case "GoToLeaderboards":return "CLASIFICACIÓN LOCAL";case "GoToUnlocks":return "DESBLOQUEOS";case "GoToMenu":return "VOLVER";case "ExitGame":return "SALIR";default:return null;}
    }
}
