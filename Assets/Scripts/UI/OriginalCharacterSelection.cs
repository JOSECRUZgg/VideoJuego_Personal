using UnityEngine;

public partial class GameHUD
{
    Vector2 originalCharacterScroll;
    CharacterPreview3D originalPreview;
    void DrawOriginalPreview(Rect rect, OriginalContentCatalog.Character character)
    {
        if (originalPreview == null) originalPreview = gameObject.AddComponent<CharacterPreview3D>();
        originalPreview.Draw(rect, character);
    }
    void DrawOriginalCharacterSelector(GameManager gm)
    {
        MegabonkUI.MenuWorld();
        float w = MegabonkUI.Width, h = MegabonkUI.Height;
        var catalog = OriginalContentCatalog.Instance;
        var selected = OriginalContentCatalog.Selected ?? catalog.characters[0];
        OriginalContentCatalog.Select(selected);
        var left = new Rect(32, 58, 500, h - 112);
        var right = new Rect(w - 456, 98, 424, h - 210);
        MegabonkUI.MetalPanel(left, "SELECCIÓN DE PERSONAJE"); MegabonkUI.MetalPanel(right);
        var view = new Rect(left.x + 20, left.y + 64, left.width - 40, left.height - 88);
        originalCharacterScroll = GUI.BeginScrollView(view, originalCharacterScroll,
            new Rect(0, 0, view.width - 24, Mathf.Ceil(catalog.characters.Length / 4f) * 104));
        for (int i = 0; i < catalog.characters.Length; i++)
        {
            var character = catalog.characters[i];
            var rect = new Rect(i % 4 * 104, i / 4 * 104, 94, 94);
            MegabonkUI.SlotFrame(rect, character.id == selected.id ? new Color(.08f,.35f,.45f) : new Color(.27f,.3f,.29f));
            if (character.icon != null) GUI.DrawTexture(new Rect(rect.x+7,rect.y+4,80,66), character.icon, ScaleMode.ScaleToFit);
            MegabonkUI.Text(new Rect(rect.x+3,rect.y+72,88,20),character.displayName,12,Color.white,true,TextAnchor.MiddleCenter);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) { OriginalContentCatalog.Select(character); selected = character; }
        }
        GUI.EndScrollView();
        float middleLeft = left.xMax + 12, middleWidth = Mathf.Max(100, right.x - middleLeft - 12);
        DrawOriginalPreview(new Rect(middleLeft, 120, middleWidth, h - 300), selected);
        MegabonkUI.Text(new Rect(middleLeft,h-166,middleWidth,34),selected.displayName,25,Color.white,true,TextAnchor.MiddleCenter);
        MegabonkUI.Text(new Rect(middleLeft,h-124,middleWidth,40),"Arrastra con el mouse para girar 360°",14,MegabonkUI.Muted,false,TextAnchor.MiddleCenter);
        MegabonkUI.Text(new Rect(right.x+22,right.y+24,right.width-44,50),selected.displayName,28,Color.white,true);
        var weapon = UpgradeCatalog.Get(MegabonkBuild.StartingWeapon(gm.selectedCharacter));
        MegabonkUI.Text(new Rect(right.x+22,right.y+100,right.width-44,45),"Modelo y materiales originales",17,MegabonkUI.Muted);
        MegabonkUI.Text(new Rect(right.x+22,right.y+170,300,30),"ARMA INICIAL",17,MegabonkUI.Gold,true);
        if (weapon != null)
        {
            MegabonkIconProvider.DrawUpgradeIcon(new Rect(right.x+22,right.y+210,64,64),weapon);
            MegabonkUI.Text(new Rect(right.x+104,right.y+212,right.width-128,32),weapon.name,21,MegabonkUI.Gold,true);
            MegabonkUI.Text(new Rect(right.x+22,right.y+285,right.width-44,65),weapon.description,16,Color.white);
        }
        if (weapon != null && weapon.id == "mb_revolver" && selected.originalWeapon != "WeaponRevolver")
            MegabonkUI.Text(new Rect(right.x+22,right.y+415,right.width-44,95),
                "Este modelo utiliza el revólver disponible. Su arma original todavía no tiene una mecánica propia integrada.",14,MegabonkUI.Muted);
        else MegabonkUI.Text(new Rect(right.x+22,right.y+355,right.width-44,Mathf.Max(30,right.height-440)),"PASIVA · "+CharacterPassives.Description(selected.id),14,MegabonkUI.Muted);
        if (MegabonkUI.PixelButton(new Rect(right.x+112,right.yMax-74,200,52),"CONFIRMAR",true)) currentMenuScreen=MenuSubScreen.MapSelect;
        if (MegabonkUI.PixelButton(new Rect(32,h-46,170,34),"VOLVER",false)) currentMenuScreen=MenuSubScreen.Main;
    }
}
