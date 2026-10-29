using UnityEngine;

public partial class GameHUD
{
    string lanAddress = "192.168.1.";
    Vector2 lanCharactersScroll;

    void DrawLanLobby(GameManager gm)
    {
        MegabonkUI.MenuWorld();
        float w = MegabonkUI.Width, h = MegabonkUI.Height;
        var lobby = LanLobby.Instance;
        MegabonkUI.MetalPanel(new Rect(32, 54, w - 64, h - 110), "COOPERATIVO LAN / VPN · DOS JUGADORES");
        MegabonkUI.Text(new Rect(66, 115, w - 132, 34),
            "Conecta dos computadoras en la misma red o VPN privada. Cada jugador elige su personaje.",
            18, MegabonkUI.White);
        if (lobby == null)
        {
            MegabonkUI.Text(new Rect(66, 175, w - 132, 30), "Inicializando la conexión LAN…", 18, MegabonkUI.Muted);
            return;
        }

        float left = 66, mid = w * .51f;
        MegabonkUI.MetalPanel(new Rect(left, 165, mid - left - 24, 235), "CONEXIÓN");
        if (lobby.Role == LanLobby.Seat.Offline)
        {
            if (MegabonkUI.Button(new Rect(left + 20, 225, 225, 50), "CREAR PARTIDA", MegabonkUI.Cyan)) lobby.Host();
            MegabonkUI.Text(new Rect(left + 20, 296, 145, 30), "IP del anfitrión:", 16, MegabonkUI.White);
            lanAddress = GUI.TextField(new Rect(left + 170, 292, 175, 34), lanAddress, 15);
            if (MegabonkUI.Button(new Rect(left + 355, 286, 225, 48), "UNIRSE", MegabonkUI.Gold)) lobby.Join(lanAddress.Trim());
        }
        else
        {
            MegabonkUI.Text(new Rect(left + 20, 225, mid - left - 64, 30),
                lobby.Role == LanLobby.Seat.Host ? "ANFITRIÓN · IPv4: " + lobby.HostAddress + " · puerto " + LanLobby.Port : "INVITADO · " + lobby.HostAddress,
                18, MegabonkUI.Cyan, true);
            if (MegabonkUI.Button(new Rect(left + 20, 290, 230, 44), "DESCONECTAR", MegabonkUI.Muted)) lobby.Stop();
        }
        MegabonkUI.Text(new Rect(left + 20, 348, mid - left - 64, 36), lobby.Status, 15, MegabonkUI.White);

        MegabonkUI.MetalPanel(new Rect(mid, 165, w - mid - 66, 235), "EQUIPO");
        MegabonkUI.Text(new Rect(mid + 22, 226, w - mid - 110, 28),
            "TÚ: " + (OriginalContentCatalog.Selected?.displayName ?? gm.selectedCharacter.ToString()) +
            (lobby.LocalReady ? "  ✓ LISTO" : "  · SIN CONFIRMAR"), 17, MegabonkUI.White, true);
        MegabonkUI.Text(new Rect(mid + 22, 272, w - mid - 110, 28),
            "COMPAÑERO: " + (lobby.Connected ? lobby.PeerCharacter : "ESPERANDO…") +
            (lobby.PeerReady ? "  ✓ LISTO" : ""), 17, MegabonkUI.White, true);

        MegabonkUI.MetalPanel(new Rect(left, 424, mid - left - 24, h - 590), "TU PERSONAJE");
        var catalog = OriginalContentCatalog.Instance;
        if (catalog != null && catalog.characters != null && catalog.characters.Length > 0)
        {
            float viewWidth = mid - left - 64;
            var view = new Rect(left + 20, 486, viewWidth, h - 664);
            lanCharactersScroll = GUI.BeginScrollView(view, lanCharactersScroll,
                new Rect(0, 0, viewWidth - 22, Mathf.Ceil(catalog.characters.Length / 4f) * 116));
            for (int i = 0; i < catalog.characters.Length; i++)
            {
                var hero = catalog.characters[i];
                if (hero == null) continue;
                var r = new Rect(i % 4 * 115, i / 4 * 116, 100, 101);
                MegabonkUI.SlotFrame(r, gm.selectedOriginalCharacter == hero.id ? MegabonkUI.Cyan : MegabonkUI.Muted);
                if (hero.icon != null) GUI.DrawTexture(new Rect(r.x + 10, r.y + 5, 80, 72), hero.icon, ScaleMode.ScaleToFit);
                MegabonkUI.Text(new Rect(r.x + 2, r.y + 78, 96, 18), hero.displayName, 11, MegabonkUI.White, true, TextAnchor.MiddleCenter);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { OriginalContentCatalog.Select(hero); lobby.CharacterChanged(); }
            }
            GUI.EndScrollView();
        }
        else
        {
            for (int i = 0; i < Characters.Length; i++)
            {
                var hero = Characters[i];
                if (MegabonkUI.Button(new Rect(left + 20 + (i % 3) * 170, 492 + (i / 3) * 66, 156, 48),
                    hero.name, hero.color, gm.selectedCharacter == hero.type))
                { gm.selectedOriginalCharacter = ""; gm.selectedCharacter = hero.type; lobby.CharacterChanged(); }
            }
        }

        MegabonkUI.MetalPanel(new Rect(mid, 424, w - mid - 66, h - 590), "MAPA Y DIFICULTAD");
        string[] maps = { "BOSQUE", "DESIERTO", "MONTAÑA NEVADA" };
        for (int i = 0; i < maps.Length; i++)
        {
            var row = new Rect(mid + 22, 488 + i * 56, 210, 44);
            if (lobby.Role == LanLobby.Seat.Host)
            {
                if (MegabonkUI.Button(row, maps[i], MegabonkUI.Cyan, (int)gm.selectedMap == i))
                { gm.selectedMap = (GameManager.MapType)i; lobby.ConfigurationChanged(); }
            }
            else MegabonkUI.Text(row, ((int)gm.selectedMap == i ? "✓  " : "    ") + maps[i],
                16, (int)gm.selectedMap == i ? MegabonkUI.Cyan : MegabonkUI.Muted, true);
        }
        for (int i = 1; i <= 3; i++)
        {
            var row = new Rect(mid + 260, 488 + (i - 1) * 56, 145, 44);
            if (lobby.Role == LanLobby.Seat.Host)
            {
                if (MegabonkUI.Button(row, "NIVEL " + i, MegabonkUI.Gold, (int)gm.selectedTier == i))
                { gm.selectedTier = (GameManager.DifficultyTier)i; lobby.ConfigurationChanged(); }
            }
            else MegabonkUI.Text(row, ((int)gm.selectedTier == i ? "✓  " : "    ") + "NIVEL " + i,
                16, (int)gm.selectedTier == i ? MegabonkUI.Gold : MegabonkUI.Muted, true);
        }
        MegabonkUI.Text(new Rect(mid + 22, 680, w - mid - 112, 44),
            lobby.Role == LanLobby.Seat.Guest ? "El anfitrión elige el mapa y el nivel." : "Elige el mapa y el nivel para los dos.",
            15, MegabonkUI.Muted);

        if (lobby.Connected && MegabonkUI.Button(new Rect(left + 20, h - 148, 230, 50),
            lobby.LocalReady ? "CANCELAR LISTO" : "ESTOY LISTO", MegabonkUI.Cyan))
            lobby.SetReady(!lobby.LocalReady);
        MegabonkUI.Text(new Rect(left + 280, h - 145, w - left - 380, 48),
            lobby.LocalReady && lobby.PeerReady
                ? lobby.Role == LanLobby.Seat.Host ? "Ambos listos: puedes iniciar la expedición." : "Ambos listos: esperando al anfitrión."
                : "Ambos jugadores deben confirmar que están listos.",
            16, lobby.LocalReady && lobby.PeerReady ? MegabonkUI.Gold : MegabonkUI.Muted);
        if (lobby.Role == LanLobby.Seat.Host && lobby.Connected && lobby.LocalReady && lobby.PeerReady &&
            MegabonkUI.Button(new Rect(w - 290, h - 149, 225, 50), "INICIAR LOS DOS", MegabonkUI.Gold))
            lobby.StartTogether();
        if (MegabonkUI.Button(new Rect(32, h - 48, 180, 36), "VOLVER", MegabonkUI.Muted))
        { lobby.Stop(); currentMenuScreen = MenuSubScreen.Main; }
    }
}
