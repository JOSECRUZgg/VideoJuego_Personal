using UnityEngine;

/// <summary>
/// Provee y renderiza iconos de hechizos 8-bit de "Unique Free 8-bit Spells 16x16"
/// para las cartas de mejoras y HUD.
/// Selecciona exactamente UN cuadrado de 16x16 de la plantilla.
/// </summary>
public static class SpellIconProvider
{
    private static Texture2D s_SheetTexture;
    private static bool s_Loaded;

    public static void EnsureLoaded()
    {
        if (s_Loaded && s_SheetTexture != null) return;
        s_Loaded = true;

#if UNITY_EDITOR
        s_SheetTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Free 8-bit Spells/Spritesheets/spritesheets.png");
        if (s_SheetTexture != null)
        {
            s_SheetTexture.filterMode = FilterMode.Point;
        }
#endif
    }

    public static void DrawIcon(Rect r, string upgradeId, string category, Color tint)
    {
        EnsureLoaded();

        if (s_SheetTexture != null)
        {
            if (s_SheetTexture.filterMode != FilterMode.Point)
                s_SheetTexture.filterMode = FilterMode.Point;

            int totalCols = 5;
            int totalRows = 11;
            int totalIcons = totalCols * totalRows;

            string idLower = (upgradeId ?? "").ToLower();
            string catLower = (category ?? "").ToLower();

            int iconIdx;
            if (idLower.Contains("fire") || idLower.Contains("pyro") || catLower.Contains("fuego") || catLower.Contains("llama"))
                iconIdx = 0; // Bola de fuego
            else if (idLower.Contains("ice") || idLower.Contains("frost") || catLower.Contains("hielo"))
                iconIdx = 5; // Fragmento de escarcha
            else if (idLower.Contains("electro") || idLower.Contains("spark") || catLower.Contains("rayo") || catLower.Contains("trueno"))
                iconIdx = 2; // Chispa eléctrica
            else if (idLower.Contains("shield") || idLower.Contains("health") || catLower.Contains("defensa") || catLower.Contains("vida"))
                iconIdx = 16; // Escudo / Corazón
            else if (idLower.Contains("pierce") || idLower.Contains("bullet") || catLower.Contains("proyectil") || catLower.Contains("rafaga"))
                iconIdx = 10; // Flecha mágica / bala
            else if (idLower.Contains("summon") || idLower.Contains("skel") || idLower.Contains("necro"))
                iconIdx = 25; // Calavera / nigromancia
            else if (idLower.Contains("speed") || idLower.Contains("dash") || catLower.Contains("velocidad"))
                iconIdx = 12; // Viento / velocidad
            else
            {
                int hash = Mathf.Abs(((upgradeId ?? "") + (category ?? "")).GetHashCode());
                iconIdx = hash % totalIcons;
            }

            int col = iconIdx % totalCols;
            int row = iconIdx / totalCols; // 0 = fila superior

            float cellW = 16f / 80f;
            float cellH = 16f / 176f;
            float u = col * cellW;
            float v = 1f - (row + 1) * cellH; // GUI UV: 0 abajo, 1 arriba

            Rect texCoords = new Rect(u, v, cellW, cellH);

            Color prevColor = GUI.color;

            // Fondo oscuro con brillo sutil
            GUI.color = new Color(0.04f, 0.06f, 0.1f, 0.90f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);

            // Borde con color de la carta
            GUI.color = tint;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - 2, r.width, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, 2, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x + r.width - 2, r.y, 2, r.height), Texture2D.whiteTexture);

            // Icono 16x16 centrado con proporciones pixel art
            GUI.color = Color.white;
            float pad = Mathf.Max(4f, r.width * 0.12f);
            Rect iconInner = new Rect(r.x + pad, r.y + pad, r.width - pad * 2, r.height - pad * 2);
            GUI.DrawTextureWithTexCoords(iconInner, s_SheetTexture, texCoords);

            GUI.color = prevColor;
            return;
        }

        // Fallback gráfico si no hay textura
        MegabonkUI.Glyph(r, "+", tint);
    }
}
